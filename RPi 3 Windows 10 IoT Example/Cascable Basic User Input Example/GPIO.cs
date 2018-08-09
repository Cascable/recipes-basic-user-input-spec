using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using System.Diagnostics;

namespace CascableBasicUserInputExample
{
    class GPIO: IDisposable
    {
        private GpioController controller;
        private GpioPin buttonPin;
        public bool buttonDown { get; private set; } = false;

        public event EventHandler GpioButtonDown;
        public event EventHandler GpioButtonUp;

        public GPIO(int buttonPinNumber)
        {
            controller = GpioController.GetDefault();
            if (controller == null)
            {
                Debug.WriteLine("GPIO failed!");
                throw new Exception();
            }

            buttonPin = controller.OpenPin(buttonPinNumber);
            buttonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            buttonDown = (buttonPin.Read() == GpioPinValue.Low);
            buttonPin.ValueChanged += ButtonPin_ValueChanged;
            Debug.WriteLine("Started GPIO");
        }

        private void ButtonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            bool wasDown = buttonDown;
            buttonDown = (sender.Read() == GpioPinValue.Low);
            if (wasDown != buttonDown)
            {
                if (buttonDown) {
                    GpioButtonDown?.Invoke(this, null);
                } else
                {
                    GpioButtonUp?.Invoke(this, null);
                }
            }
        }

        ~GPIO() {
            Dispose();
        }

        public void Dispose()
        {
           if (buttonPin != null)
            {
                buttonPin.Dispose();
                buttonPin = null;
            }
           if (controller != null)
            {
                controller = null;
            }
        }
    }
}
