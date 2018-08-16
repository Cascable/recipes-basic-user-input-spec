using System;
using Windows.Devices.Gpio;
using System.Diagnostics;
using Windows.UI.Core;

namespace CascableBasicUserInputExample
{
    class GPIO: IDisposable
    {
        private GpioController controller;
        private GpioPin continueButtonPin;
        private GpioPin continueLedPin;
        private GpioPin stopButtonPin;
        private GpioPin stopLedPin;
        public bool continueButtonDown { get; private set; } = false;
        public bool continueLedOn { get; private set; } = false;
        public bool stopButtonDown { get; private set; } = false;
        public bool stopLedOn { get; private set; } = false;

        private CoreDispatcher eventDispatcher;
        public event EventHandler GpioContinueButtonDown;
        public event EventHandler GpioContinueButtonUp;
        public event EventHandler GpioStopButtonDown;
        public event EventHandler GpioStopButtonUp;

        public GPIO(int continueButtonPinNumber, int continueLedPinNumber, int stopButtonPinNumber, int stopLedPinNumber, CoreDispatcher dispatcher)
        {
            controller = GpioController.GetDefault();
            if (controller == null)
            {
                Debug.WriteLine("GPIO failed!");
                throw new Exception();
            }

            eventDispatcher = dispatcher;

            continueButtonPin = controller.OpenPin(continueButtonPinNumber);
            continueButtonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            continueButtonDown = (continueButtonPin.Read() == GpioPinValue.Low);
            continueButtonPin.ValueChanged += ContinueButtonPin_ValueChanged;

            continueLedPin = controller.OpenPin(continueLedPinNumber);
            continueLedPin.SetDriveMode(GpioPinDriveMode.Output);
            continueLedPin.Write(continueLedOn ? GpioPinValue.High : GpioPinValue.Low);

            stopButtonPin = controller.OpenPin(stopButtonPinNumber);
            stopButtonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            stopButtonDown = (stopButtonPin.Read() == GpioPinValue.Low);
            stopButtonPin.ValueChanged += StopButtonPin_ValueChanged;

            stopLedPin = controller.OpenPin(stopLedPinNumber);
            stopLedPin.SetDriveMode(GpioPinDriveMode.Output);
            stopLedPin.Write(stopLedOn ? GpioPinValue.High : GpioPinValue.Low);

            Debug.WriteLine("Started GPIO");
        }

        public void setContinueLedOn(bool isOn)
        {
            continueLedOn = isOn;
            if (continueLedPin != null)
            {
                continueLedPin.Write(isOn ? GpioPinValue.High : GpioPinValue.Low);
            }
        }

        public void setStopLedOn(bool isOn)
        {
            stopLedOn = isOn;
            if (stopLedPin != null)
            {
                stopLedPin.Write(isOn ? GpioPinValue.High : GpioPinValue.Low);
            }
        }

        private async void ContinueButtonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            await eventDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                bool wasDown = continueButtonDown;
                continueButtonDown = (sender.Read() == GpioPinValue.Low);
                if (wasDown != continueButtonDown)
                {
                    if (continueButtonDown)
                    {
                        GpioContinueButtonDown?.Invoke(this, null);
                    }
                    else
                    {
                        GpioContinueButtonUp?.Invoke(this, null);
                    }
                }
            });
        }

        private async void StopButtonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            await eventDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                bool wasDown = stopButtonDown;
                stopButtonDown = (sender.Read() == GpioPinValue.Low);
                if (wasDown != stopButtonDown)
                {
                    if (stopButtonDown)
                    {
                        GpioStopButtonDown?.Invoke(this, null);
                    }
                    else
                    {
                        GpioStopButtonUp?.Invoke(this, null);
                    }
                }
            });
        }

        ~GPIO() {
            Dispose();
        }

        public void Dispose()
        {
           if (continueButtonPin != null)
            {
                continueButtonPin.Dispose();
                continueButtonPin = null;
            }
           if (continueLedPin != null)
            {
                continueLedPin.Dispose();
                continueLedPin = null;
            }
            if (stopButtonPin != null)
            {
                stopButtonPin.Dispose();
                stopButtonPin = null;
            }
            if (stopLedPin != null)
            {
                stopLedPin.Dispose();
                stopLedPin = null;
            }
            if (controller != null)
            {
                controller = null;
            }
        }
    }
}
