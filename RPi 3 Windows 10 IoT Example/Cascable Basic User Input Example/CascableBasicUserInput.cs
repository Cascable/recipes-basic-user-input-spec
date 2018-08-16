using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace CascableBasicUserInputExample
{
    /// <summary>
    /// The CascableBasicUserInputExample class implements the Cascable Basic User Input Bluetooth LE spec using 
    /// Windows' Bluetooth SDKs.
    /// </summary>
    class CascableBasicUserInput : IDisposable
    {
        static Guid cascableBasicUserInputServiceGuid = Guid.Parse("9A43142A-F619-41FF-945D-527023EF5B9D");
        static Guid continueButtonDownCharacteristicGuid = Guid.Parse("EB366CE4-1952-4A55-A319-7E27A865554A");
        static Guid stopButtonDownCharacteristicGuid = Guid.Parse("A83173E8-9546-4D91-A736-B087AE514321");
        static Guid remoteCanContinueCharacteristicGuid = Guid.Parse("941680A1-A4D9-48DF-927B-2E19870F8085");
        static Guid remoteCanStopCharacteristicGuid = Guid.Parse("FD64C111-356F-4968-85C2-2DD979E3878E");
        static Guid userMessageCharacteristicGuid = Guid.Parse("DEFF81CC-0F74-4E86-BA01-F5D34E1C33E6");

        static readonly GattLocalCharacteristicParameters readAndNotifyParameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Read and Notify"
        };

        static readonly GattLocalCharacteristicParameters writeParameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Write | GattCharacteristicProperties.WriteWithoutResponse,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Write"
        };

        GattServiceProviderAdvertisingParameters advertisingParameters = new GattServiceProviderAdvertisingParameters
        {
            IsDiscoverable = true,
            IsConnectable = true
        };

        /// <summary>
        /// Cascable button states are represented as a single byte.
        /// </summary>
        public enum ButtonState : byte
        {
            Up = 0x00,
            Down = 0x01,
            Cancelled = 0x02
        }

        CoreDispatcher eventDispatcher;
        GattServiceProvider cascableService;
        GattLocalCharacteristic continueButtonCharacteristic;
        GattLocalCharacteristic stopButtonCharacteristic;
        GattLocalCharacteristic remoteCanContinueCharacteristic;
        GattLocalCharacteristic remoteCanStopCharacteristic;
        GattLocalCharacteristic userMessageCharacteristic;

        /// <summary>
        /// Triggered when any remote state is changed - connected, canContinue, canStop, and userMessage.
        /// </summary>
        public event EventHandler StateChanged;

        /// <summary>
        /// The current state of the continue button. Set locally.
        /// </summary>
        public ButtonState continueButtonState { get; private set; } = ButtonState.Up;

        /// <summary>
        /// The current state of the stop button. Set locally.
        /// </summary>
        public ButtonState stopButtonState { get; private set; } = ButtonState.Up;

        /// <summary>
        /// If set to <code>true</code>, Cascable is waiting for input, and this device is allowed to continue recipe execution.
        /// </summary>
        public bool canContinue { get; private set; } = false;

        /// <summary>
        /// If set to <code>true</code>, this device is allowed to stop recipe execution.
        /// </summary>
        public bool canStop { get; private set; } = false;

        /// <summary>
        /// The current user message. Can be null.
        /// </summary>
        public String userMessage { get; private set; } = null;

        /// <summary>
        /// Returns <code>true</code> if Cascable is connected to this device, otherwise <code>false</code>.
        /// </summary>
        public bool connected
        {
            get
            {
                // The only required characteristic is the continue button state characteristic, so clients
                // should consider Cascable to be "connected" when they have a subscriber to the continue button state.
                return continueButtonCharacteristic?.SubscribedClients.Count > 0;
            }
        }
        
        public CascableBasicUserInput(CoreDispatcher dispatcher) {
            eventDispatcher = dispatcher;
            setup();
        }

        ~CascableBasicUserInput()
        {
            Dispose();
        }

        public void Dispose()
        {
            // Windows BLE seems to get unreliable if services etc aren't disposed correctly.
            if (continueButtonCharacteristic != null)
            {
                continueButtonCharacteristic.ReadRequested -= readContinueButtonValue;
                continueButtonCharacteristic.SubscribedClientsChanged -= subscribedClientsChanged;
                continueButtonCharacteristic = null;
            }
            if (stopButtonCharacteristic != null)
            {
                stopButtonCharacteristic.ReadRequested -= readStopButtonValue;
                stopButtonCharacteristic = null;
            }
            if (remoteCanContinueCharacteristic != null)
            {
                remoteCanContinueCharacteristic.WriteRequested -= writeRemoteCanContinue;
                remoteCanContinueCharacteristic = null;
            }
            if (remoteCanStopCharacteristic != null)
            {
                remoteCanStopCharacteristic.WriteRequested -= writeRemoteCanStop;
                remoteCanStopCharacteristic = null;
            }
            if (userMessageCharacteristic != null)
            {
                userMessageCharacteristic.WriteRequested -= writeUserMessage;
                userMessageCharacteristic = null;
            }
            if (cascableService != null)
            {
                cascableService.StopAdvertising();
                cascableService = null;
            }
        }

        /// <summary>
        /// Inform connected client(s) of a new continue button state.
        /// </summary>
        /// <param name="newState">The state of the continue button.</param>
        public void updateContinueButtonState(ButtonState newState)
        {
            continueButtonState = newState;
            notifyContinueButtonState();
        }

        /// <summary>
        /// Inform connected client(s) of a new stop button state.
        /// </summary>
        /// <param name="newState">The state of the stop button.</param>
        public void updateStopButtonState(ButtonState newState)
        {
            stopButtonState = newState;
            notifyStopButtonState();
        }

        private async void setup()
        {
            var result = await GattServiceProvider.CreateAsync(cascableBasicUserInputServiceGuid);
            if (result.Error != BluetoothError.Success)
            {
                Debug.WriteLine("Failed to create BLE service!!");
                return;
            }

            cascableService = result.ServiceProvider;

            var continueButtonResult = await cascableService.Service.CreateCharacteristicAsync(continueButtonDownCharacteristicGuid, readAndNotifyParameters);
            if (continueButtonResult.Error != BluetoothError.Success)
            {
                Debug.WriteLine("Failed to create Continue Button Characteristic!!");
                return;
            }

            continueButtonCharacteristic = continueButtonResult.Characteristic;
            continueButtonCharacteristic.ReadRequested += readContinueButtonValue;
            continueButtonCharacteristic.SubscribedClientsChanged += subscribedClientsChanged;

            var stopButtonResult = await cascableService.Service.CreateCharacteristicAsync(stopButtonDownCharacteristicGuid, readAndNotifyParameters);
            if (stopButtonResult.Error != BluetoothError.Success)
            {
                Debug.WriteLine("Failed to create Stop Button Characteristic!!");
                return;
            }

            stopButtonCharacteristic = stopButtonResult.Characteristic;
            stopButtonCharacteristic.ReadRequested += readStopButtonValue;

            var remoteCanContinueResult = await cascableService.Service.CreateCharacteristicAsync(remoteCanContinueCharacteristicGuid, writeParameters);
            if (remoteCanContinueResult.Error != BluetoothError.Success)
            {
                Debug.WriteLine("Failed to create Remote Can Continue Characteristic!!");
                return;
            }

            remoteCanContinueCharacteristic = remoteCanContinueResult.Characteristic;
            remoteCanContinueCharacteristic.WriteRequested += writeRemoteCanContinue;

            var remoteCanStopResult = await cascableService.Service.CreateCharacteristicAsync(remoteCanStopCharacteristicGuid, writeParameters);
            if (remoteCanStopResult.Error != BluetoothError.Success)
            {
                Debug.WriteLine("Failed to create Remote Can Stop Characteristic!!");
                return;
            }

            remoteCanStopCharacteristic = remoteCanStopResult.Characteristic;
            remoteCanStopCharacteristic.WriteRequested += writeRemoteCanStop;

            var userMessageResult = await cascableService.Service.CreateCharacteristicAsync(userMessageCharacteristicGuid, writeParameters);
            if (userMessageResult.Error != BluetoothError.Success)
            {
                Debug.WriteLine("Failed to create User Message Characteristic!!");
                return;
            }

            userMessageCharacteristic = userMessageResult.Characteristic;
            userMessageCharacteristic.WriteRequested += writeUserMessage;

            cascableService.StartAdvertising(advertisingParameters);
            Debug.WriteLine("Service is now advertising!");
        }

        private async void subscribedClientsChanged(GattLocalCharacteristic sender, object args)
        {
            await eventDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Debug.WriteLine("Subscribed clients changed!!");
                StateChanged?.Invoke(this, null);
            });
        }

        // ---- Write Event Handlers ----

        private async void writeRemoteCanContinue(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            var request = await args.GetRequestAsync();
            if (request == null)
            {
                Debug.WriteLine("Write of Can Continue had a null request! Make sure the application manifest allows Bluetooth access.");
                deferral.Complete();
                return;
            }

            // The "Can Continue" characteristic is a single byte of data - 0 for false, and 1 for true.
            var reader = DataReader.FromBuffer(request.Value);
            if (reader.UnconsumedBufferLength > 0)
            {
                var value = reader.ReadByte();
                await eventDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    canContinue = value > 0;
                    Debug.WriteLine("Can Continue is now: {0}", canContinue);
                    StateChanged?.Invoke(this, null);
                });
            }

            if (request.Option == GattWriteOption.WriteWithResponse)
            {
                request.Respond();
            }

            deferral.Complete();
        }

        private async void writeRemoteCanStop(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            var request = await args.GetRequestAsync();
            if (request == null)
            {
                Debug.WriteLine("Write of Can Stop had a null request! Make sure the application manifest allows Bluetooth access.");
                deferral.Complete();
                return;
            }

            // The "Can Stop" characteristic is a single byte of data - 0 for false, and 1 for true.
            var reader = DataReader.FromBuffer(request.Value);
            if (reader.UnconsumedBufferLength > 0)
            {
                var value = reader.ReadByte();
                await eventDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    canStop = value > 0;
                    Debug.WriteLine("Can Stop is now: {0}", canStop);
                    StateChanged?.Invoke(this, null);
                });
            }

            if (request.Option == GattWriteOption.WriteWithResponse)
            {
                request.Respond();
            }

            deferral.Complete();
        }

        private async void writeUserMessage(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            var request = await args.GetRequestAsync();
            if (request == null)
            {
                Debug.WriteLine("Write of User Message had a null request! Make sure the application manifest allows Bluetooth access.");
                deferral.Complete();
                return;
            }

            try
            {
                // The "User Message" characteristic contains a UTF-8 string. 
                // It's valid for the app to send a zero-length payload here, which means "no custom message".
                var bufferCount = request.Value.Length;
                if (bufferCount > 0) {
                    var stringValue = Encoding.UTF8.GetString(request.Value.ToArray());
                    await eventDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        userMessage = stringValue;
                        Debug.WriteLine("User Message is now: {0}", userMessage);
                        StateChanged?.Invoke(this, null);
                    });
                } else
                {
                    // If we have an empty buffer, we should null out the string.
                    await eventDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        userMessage = null;
                        Debug.WriteLine("User Message is now empty");
                        StateChanged?.Invoke(this, null);
                    });
                }
            }
            catch { }

            if (request.Option == GattWriteOption.WriteWithResponse)
            {
                request.Respond();
            }

            deferral.Complete();
        }

        // ---- Notifying ----

        private async void notifyContinueButtonState()
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteByte((byte)continueButtonState);
            await continueButtonCharacteristic.NotifyValueAsync(writer.DetachBuffer());
        }

        private async void notifyStopButtonState()
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteByte((byte)stopButtonState);
            await stopButtonCharacteristic.NotifyValueAsync(writer.DetachBuffer());
        }

        // ---- Read Event Handlers ----

        private async void readContinueButtonValue(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();

            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteByte((byte)continueButtonState);

            var request = await args.GetRequestAsync();
            request.RespondWithValue(writer.DetachBuffer());

            deferral.Complete();
        }

        private async void readStopButtonValue(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();

            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteByte((byte)stopButtonState);

            var request = await args.GetRequestAsync();
            request.RespondWithValue(writer.DetachBuffer());

            deferral.Complete();
        }
    }
}
