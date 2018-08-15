using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Diagnostics;
using System.Timers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CascableBasicUserInputExample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        CascableBasicUserInput ble = new CascableBasicUserInput();
        GPIO gpio = new GPIO(13, 20, 10, 7);

        public MainPage()
        {
            this.InitializeComponent();
            gpio.GpioContinueButtonDown += Gpio_GpioContinueButtonDown;
            gpio.GpioContinueButtonUp += Gpio_GpioContinueButtonUp;
            gpio.GpioStopButtonDown += Gpio_GpioStopButtonDown;
            gpio.GpioStopButtonUp += Gpio_GpioStopButtonUp;
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
