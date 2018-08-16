using System;
using Windows.Devices.Gpio;
using System.Diagnostics;
using Windows.UI.Core;

namespace CascableBasicUserInputExample
{
    /// <summary>
    /// The GPIO class implements the button handlers and LED indicators on our Raspberry Pi. It reads
    /// the input pins for button presses, and turns on the output pins to light up LEDs as desired.
    /// </summary>
    class GPIO: IDisposable
    {
        private GpioController controller;
        private GpioPin continueButtonPin;
        private GpioPin continueLedPin;
        private GpioPin stopButtonPin;
        private GpioPin stopLedPin;
        private CoreDispatcher eventDispatcher;

        /// <summary>
        /// Whether the continue button is currently pressed.
        /// </summary>
        public bool continueButtonDown { get; private set; } = false;

        /// <summary>
        /// Whether the continue LED is currently illuminated.
        /// </summary>
        public bool continueLedOn { get; private set; } = false;

        /// <summary>
        /// Whether the stop button is currently pressed.
        /// </summary>
        public bool stopButtonDown { get; private set; } = false;

        /// <summary>
        /// Whether the stop LED is currently illuminated.
        /// </summary>
        public bool stopLedOn { get; private set; } = false;
        
        /// <summary>
        /// Fired when the continue button is pressed.
        /// </summary>
        public event EventHandler GpioContinueButtonDown;

        /// <summary>
        /// Fired when the continue button is released.
        /// </summary>
        public event EventHandler GpioContinueButtonUp;

        /// <summary>
        /// Fired when the stop button is pressed.
        /// </summary>
        public event EventHandler GpioStopButtonDown;

        /// <summary>
        /// Fired when the stop button is released.
        /// </summary>
        public event EventHandler GpioStopButtonUp;

        /// <summary>
        /// Create a new GPIO object. The object will immediately try to open GPIO and start listening to input pin state.
        /// </summary>
        /// <param name="continueButtonPinNumber">The GPIO input pin that the continue button is connected to.</param>
        /// <param name="continueLedPinNumber">The GPIO output pin that the continue LED is connected to.</param>
        /// <param name="stopButtonPinNumber">The GPIO input pin that the stop button is connected to.</param>
        /// <param name="stopLedPinNumber">The GPIO output pin that the stop LED is connected to.</param>
        /// <param name="dispatcher">The CoreDispatcher object to use to trigger events.</param>
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

        /// <summary>
        /// Turn on (or off) the continue LED.
        /// </summary>
        public void setContinueLedOn(bool isOn)
        {
            continueLedOn = isOn;
            if (continueLedPin != null)
            {
                continueLedPin.Write(isOn ? GpioPinValue.High : GpioPinValue.Low);
            }
        }

        /// <summary>
        /// Turn on (or off) the stop LED.
        /// </summary>
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
            GpioPinValue currentValue = sender.Read();
            await eventDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                bool wasDown = continueButtonDown;
                continueButtonDown = (currentValue == GpioPinValue.Low);
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
            GpioPinValue currentValue = sender.Read();
            await eventDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                bool wasDown = stopButtonDown;
                stopButtonDown = (currentValue == GpioPinValue.Low);
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
