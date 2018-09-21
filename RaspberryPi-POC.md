# Basic User Input: Raspberry Pi Proof of Concept

The Windows 10 IoT example of the Cascable Recipes Basic User Input is protocol designed to work out-of-the box on our proof-of-concept "Bluetooth Remote Control" hardware project that's constructed from a Raspberry Pi and readily-available hobby components.

**Important:** Cascable customer support cannot assist with this example code and proof of concept project. Experience with hardware and software development is highly recommended.

<p align="center">
<img src="Documentation%20Images/POC.jpg?raw=true">
</p>

### Software Requirements

To use our example as-is, you need access to a Windows 10 PC (or virtual machine) running the newest version of Visual Studio (the free Express version of Visual Studio should work fine).

### Part List

**Note:** At the time of writing (September 2018), Windows 10 IoT does *not* support the Bluetooth LE built into the Raspberry Pi 3 (**Model B+**) - you must use a **Model B** to use the built-in Bluetooth LE.

We've provided links to the relavent parts in this document. However, we do not endorse any particular part or retailer.

- 1x [Raspberry Pi 3 (Model B)](https://www.raspberrypi.org/products/raspberry-pi-3-model-b/), Micro SD card, and power supply
- 1x [ModMyPi Modular RPi 2/3/B+ Case](https://www.modmypi.com/raspberry-pi/cases-183/raspberry-pi-b-plus2-and-3-cases-1122/plastic-cases-1142/modmypi-modular-rpi-b-plus-case-black)
- 1x [ModMyPi Modular RPi Case 10mm Spacer](https://www.modmypi.com/raspberry-pi/cases-183/accessories-1125/modular-case-add-ons-1160/modmypi-modular-rpi-b-plus-case-10mm-spacer-black)*

- 1x [Adafruit Illuminated Pushbutton (16mm, Green, Momentary)](https://www.adafruit.com/product/1440)
- 1x [Adafruit Illuminated Pushbutton (16mm, Red, Momentary)](https://www.adafruit.com/product/1439)
- 8x [Breadboarding Female Jumper Wires](https://www.adafruit.com/product/266)
- 2x 220 to 1000 ohm resistor

- Appropriate tools - soldering iron, heat shrink tubing, drill with 16mm bit, etc

\* We didn't actually use a spacer in the build photographed on this page. However, the connectors on the bottom of the buttons are very close to the Raspberry Pi board. Using the spacer will make the internals of the case a bit more comfortable.

### Project Assembly

In our part list, we used an Adafruit Illuminated Pushbutton which is a button and an LED packaged into one unit, but behaves electrically like two completed separate units - a button and an LED. Therefore, our diagram here shows the button and LED separately. 

The buttons and LEDs are connected to the Raspberry Pi's GPIO pins, which are numbered as follows:

<p align="center">
<img src="Documentation%20Images/gpio-numbers-pi2.png?raw=true" width="500"><br />
<em>Image source: <a href="https://www.raspberrypi.org/documentation/usage/gpio/">Raspberry Pi GPIO Documentation</a></em> 
</p>

The items are connected to the pins described the the table below. The continue button and LED are green, and the stop LED button and LED are red. You can use different pins, but you must modify the example project in `MainPage.xaml.cs` to match - look for the line `gpio = new GPIO(13, 21, 10, 7, Dispatcher);`.

| Description   | GPIO Pin | Notes |
| ------------- | -------- | ----- |
| Continue Button + | 13     | Button pins can usually be swapped. |
| Continue Button - | Ground | Button pins can usually be swapped. |
| Continue LED +    | 21     | Must be wired in series with a resistor. |
| Continue LED -    | Ground |  |
| Stop Button + | 10     | Continue button pins can usually be swapped. |
| Stop Button - | Ground | Continue button pins can usually be swapped. |
| Stop LED +    | 7      | Must be wired in series with a resistor. |
| Stop LED -    | Ground |  |

Once complete, your wiring should look like this:

<p align="center">
<img src="Documentation%20Images/RPi-diagram.png?raw=true" width="350"><br />
</p>

### Software Setup

To get your computer ready, make sure you have Visual Studio installed. You can use the free [Visual Studio Express](https://visualstudio.microsoft.com/vs/express/).

To get your Raspberry Pi ready, follow Microsoft's instructions to [install the latest version of Windows 10 IoT](https://docs.microsoft.com/en-us/windows/iot-core/tutorials/quickstarter/devicesetup).

Once everything is set up, open the **Cascable Basic User Input Example.sln** solution in Visual Studio, and deploy the **Cascable Basic User Input Example** application to your Raspberry Pi device with the `> Remote Machine` button.

If everything is successful, the LEDs on your hardware will begin to blink, indicating that the sample application is waiting for a connection from Cascable. You can use Microsoft's [IoT Dashboard](https://docs.microsoft.com/en-us/windows/iot-core/connect-your-device/iotdashboard) application to open your device's **Device Portal**, and from there set the example application to run automatically when the Raspberry Pi is powered on.

**Note:** From a cold boot, it can take a minute or so for the LEDs to start blinking.

### LED Statuses

- **Both LEDs blinking**: The device is waiting for a connection from Cascable.
- **Green LED on**: You can push the continue button on the device to continue the recipe running in Cascable.
- **Red LED on**: You can push the stop button on the device to stop the recipe running in Cascable.

### Using With Cascable

Once your Raspberry Pi project is assembled and working, use Recipes in Cascable to build and run a recipe containing the **Wait For Input** block. 

When Cascable shows the *Waiting to Continue* screen, use the **Manage…** button at the bottom of the screen to have Cascable search for Bluetooth input accessories and connect to your Raspberry Pi. Once connected, you can use your Raspberry Pi to provide input to Cascable.

<p align="center">
<img src="Documentation%20Images/iPhoneX-RPi-Screenshots.png?raw=true">
</p>
