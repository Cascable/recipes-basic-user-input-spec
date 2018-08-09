using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Diagnostics;
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
        GPIO gpio = new GPIO(5);

        public MainPage()
        {
            this.InitializeComponent();
            gpio.GpioButtonDown += Gpio_GpioButtonDown;
            gpio.GpioButtonUp += Gpio_GpioButtonUp;
        }

        private void Gpio_GpioButtonUp(object sender, EventArgs e)
        {
            if (ble.connected)
            {
                ble.updateContinueButtonState(CascableBasicUserInput.ButtonState.Up);
            }
        }

        private void Gpio_GpioButtonDown(object sender, EventArgs e)
        {
            if (ble.connected)
            {
                ble.updateContinueButtonState(CascableBasicUserInput.ButtonState.Down);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            gpio.Dispose();
            gpio = null;
            ble.Dispose();
            ble = null;
            Debug.WriteLine("Stopped");
        }
    }
}
