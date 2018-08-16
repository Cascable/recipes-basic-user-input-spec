using System;
using System.Diagnostics;
using System.Timers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CascableBasicUserInputExample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        CascableBasicUserInput ble;
        GPIO gpio;

        public MainPage()
        {
            this.InitializeComponent();
            gpio = new GPIO(13, 21, 10, 7, Dispatcher);
            gpio.GpioContinueButtonDown += Gpio_GpioContinueButtonDown;
            gpio.GpioContinueButtonUp += Gpio_GpioContinueButtonUp;
            gpio.GpioStopButtonDown += Gpio_GpioStopButtonDown;
            gpio.GpioStopButtonUp += Gpio_GpioStopButtonUp;

            ble = new CascableBasicUserInput(Dispatcher);
            ble.StateChanged += Ble_StateChanged;

            StartBlinking();
        }

        private Timer blinkTimer;

        private void StartBlinking()
        {
            if (blinkTimer == null)
            {
                blinkTimer = new Timer(500);
                blinkTimer.AutoReset = true;
                blinkTimer.Elapsed += BlinkTimer_Elapsed;
                blinkTimer.Start();
            }
        }

        private void BlinkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            bool on = !gpio.continueLedOn;
            gpio.setContinueLedOn(on);
            gpio.setStopLedOn(on);
        }

        private void StopBlinking()
        {
            if (blinkTimer != null)
            {
                blinkTimer.Enabled = false;
                blinkTimer.Stop();
                blinkTimer.Dispose();
                blinkTimer = null;
                gpio.setContinueLedOn(false);
                gpio.setStopLedOn(false);
            }
        }

        private void Ble_StateChanged(object sender, EventArgs e)
        {
            if (ble.connected)
            {
                StopBlinking();
                gpio.setContinueLedOn(ble.canContinue);
                gpio.setStopLedOn(ble.canStop);
            } else
            {
                StartBlinking();
            }
        }

        private void Gpio_GpioContinueButtonUp(object sender, EventArgs e)
        {
            Debug.WriteLine("Continue up");
            if (ble.connected)
            {
                ble.updateContinueButtonState(CascableBasicUserInput.ButtonState.Up);
            }
        }

        private void Gpio_GpioContinueButtonDown(object sender, EventArgs e)
        {
            Debug.WriteLine("Continue down");
            if (ble.connected)
            {
                ble.updateContinueButtonState(CascableBasicUserInput.ButtonState.Down);
            }
        }

        private void Gpio_GpioStopButtonUp(object sender, EventArgs e)
        {
            Debug.WriteLine("Stop up");
            if (ble.connected)
            {
                ble.updateStopButtonState(CascableBasicUserInput.ButtonState.Up);
            }
        }

        private void Gpio_GpioStopButtonDown(object sender, EventArgs e)
        {
            Debug.WriteLine("Stop down");
            if (ble.connected)
            {
                ble.updateStopButtonState(CascableBasicUserInput.ButtonState.Down);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            StopBlinking();
            gpio.Dispose();
            gpio = null;
            ble.Dispose();
            ble = null;
            Debug.WriteLine("Stopped");
        }
    }
}
