# Cascable Recipes: Basic User Input

**Important:** If you are a user of Cascable and are looking for a pre-built solution, we have Recipes Button apps on the iOS App Store and for Mac. This repository for building custom input devices, and requires at least some programming expertise.

## Contents

- [What's This?](#whats-this)
- [The Basics](#the-basics)
- [Example Implementations](#example-implementations)
- [Protocol Reference](#protocol-reference)

## What's This? 

Our camera remote control app [Cascable](https://cascable.se) contains a tool called [Recipes](https://cascable.se/ios/recipes/) that allows users to use a visual editor to build custom automation programs for their camera. Once created, Cascable's Shutter Robot tool will load a recipe and run it against a connected camera.

As of version 3.6, Recipes contains a new block called **Wait For Input**, which is effectively a "Press any key to continue" feature. When encountered, Shutter Robot will wait for input from the user before continuing. A sample recipe might look like this: 

<p align="center">
<img src="Documentation%20Images/Recipes.png?raw=true" width="700">
</p>

When the **Wait For Input** block is encountered, Cascable displays UI similar to the following, and waits for input from the user: 

<p align="center">
<img src="Documentation%20Images/RecipesWaiting.png?raw=true" width="700">
</p>

Input can be given in one of three ways:

- Via the on-screen buttons.

- Via a wired or wireless keyboard connected to the iOS device running Cascable. 

- Via an accessory connected using Bluetooth LE.

This repository contains the specification for the Bluetooth LE protocol, as well as example implementations in Swift (for iOS and Mac) and C# (for Windows 10, including Windows 10 IoT running on a Raspberry Pi).

While the examples here are for very simple push-button accessories, the Bluetooth LE protocol could be used to build very powerful integrations, allowing Cascable to control various hardware devices. For example, with the right equipment, you could use this protocol to integrate the movement of a rig the camera is attached to - perhaps to create animated timelapses or panoramas.

## The Basics

We define a single Bluetooth LE service, **Basic User Input**. To fully implement this service, you must provide five characteristics: 

- **User Message** is written to by the Cascable app, and contains a user-defined message. If your device has a screen, you should display this message. In the screenshot above, the user message is **"To continue the Recipe, tap "Continue Recipe".**

- **Continue Available** is written to by the Cascable app, and is set to `0x01` when the app is waiting for input to continue.

- **Stop Available** is written to by the Cascable app, and is set to `0x01` when the app is waiting for input to continue, and the recipe allows accessories to stop execution. If the recipe does not allow accessories to stop execution, this may never be set to `0x01`.

- **Continue Button State** is subscribed to and read by the Cascable app, and you should notify new values as input changes. When Cascable receives a **down** state followed by an **up** state from your device, it will continue execution of the recipe if available.

- **Stop Button State** is subscribed to and read by the Cascable app, and you should notify new values as input changes. When Cascable receives a **down** state followed by an **up** state from your device, it will stop execution of the recipe if available.

An example flow might look like this:

<img src="Documentation%20Images/BUI-Flow.png?raw=true" width="338">

## Example Implementations

This repository provides example implementations of the Basic User Input protocol for iOS, Mac, and Windows 10 IoT.

The Windows 10 IoT example is designed to work out-of-the box on our proof-of-concept "Bluetooth Remote Control" hardware project that's constructed from a Raspberry Pi and readily-available hobby components.

<p align="center">
<img src="Documentation%20Images/POC.jpg?raw=true">
</p>

For a detailed schematic of how we built ours, see [RaspberryPi-POC.md](RaspberryPi-POC.md).

## Protocol Reference

### Service

| Service | GUID | Description |
| --- | --- | --- |
| Cascable Basic User Input | `9A43142A-F619-41FF-945D-527023EF5B9D` | The Cascable Recipes Basic User Input service. Contains the characteristics defined below. |

### Characteristics

#### User Message

| GUID | Characteristic Kind | Data Type | Valid Values |
| --- | --- | --- | --- |
| `DEFF81CC-0F74-4E86-BA01-F5D34E1C33E6` | Write, Write without response | Any UTF-8 string, including zero-length. |

*User Message* is written to when the user-provided message changes. This value is provided by the user when building their recipe, and can be any UTF-8 string that fits within the maximum payload capacity. If your device has a screen, it's appropriate to display this string as long as **Continue Available** is set to `0x01`.

**Important:** If the user has not provided a custom message, Cascable will write a zero-length string (i.e., zero bytes) to this characteristic. Your implementation must handle this, and display a sensible default message if appropriate.

#### Continue Available

| GUID | Characteristic Kind | Data Type | Valid Values |
| --- | --- | --- | --- |
| `941680A1-A4D9-48DF-927B-2E19870F8085` | Write, Write without response | `UInt8` (i.e., one byte) | `0x00`, `0x01`. |

*Continue Available* is written to when the ability to continue recipe execution is made available or becomes unavailable. When not available, any "continue" buttons should be hidden or disabled (if appropriate), and the user message should be hidden.

If continue is not available but still have an open Bluetooth LE connection, the recipe is executing in Cascable - i.e., taking photos, adjusting camera settings, and so on. If your device has a screen, it may be appropriate to display UI to that effect.

Valid values are as follows: 

- `0x00`: The ability to continue recipe execution is not available.
- `0x01`: The ability to continue recipe execution is available. 

#### Stop Available

| GUID | Characteristic Kind | Data Type | Valid Values |
| --- | --- | --- | --- |
| `FD64C111-356F-4968-85C2-2DD979E3878E` | Write, Write without response | `UInt8` (i.e., one byte) | `0x00`, `0x01`. |

*Stop Available* is written to when the ability to stop recipe execution is made available or becomes unavailable. When not available, any "stop" buttons should be hidden or disabled (if appropriate).

Cascable's Recipe Editor allows the user to disable the ability for remote accessories to stop execution of a recipe, so the ability to stop execution may never become available.

Valid values are as follows: 

- `0x00`: The ability to stop recipe execution is not available.
- `0x01`: The ability to stop recipe execution is available. 

#### Continue Button State

| GUID | Characteristic Kind | Data Type | Valid Values |
| --- | --- | --- | --- |
| `EB366CE4-1952-4A55-A319-7E27A865554A` | Read, Notify | `UInt8` (i.e., one byte) | `0x00`, `0x01`, `0x02`. |

You should notify *Continue Button State* whenever the state of your continue button changes. Cascable may also choose to read this characteristic at any point.

Valid values are as follows: 

- `0x00`: The button is not pressed.
- `0x01`: The button is pressed.
- `0x02`: The button press has been cancelled.

If Cascable receives an `0x01` followed by an `0x00` (i.e., a press and release) from this characteristic, it will treat that as an indication to continue execution of the recipe as long as **Continue Available** is set to `0x01`.

If your device has the concept of cancelling button presses (for example, desktop and mobile platforms allow the user to press a button, but if the user releases the press outside the button it has no effect), notify `0x02` for this characteristic to inform Cascable to ignore the press.

#### Stop Button State

| GUID | Characteristic Kind | Data Type | Valid Values |
| --- | --- | --- | --- |
| `A83173E8-9546-4D91-A736-B087AE514321` | Read, Notify | `UInt8` (i.e., one byte) | `0x00`, `0x01`, `0x02`. |

You should notify *Stop Button State* whenever the state of your stop button changes. Cascable may also choose to read this characteristic at any point.

Valid values are as follows: 

- `0x00`: The button is not pressed.
- `0x01`: The button is pressed.
- `0x02`: The button press has been cancelled.

If Cascable receives an `0x01` followed by an `0x00` (i.e., a press and release) from this characteristic, it will treat that as an indication to stop execution of the recipe as long as **Stop Available** is set to `0x01`.

If your device has the concept of cancelling button presses (for example, desktop and mobile platforms allow the user to press a button, but if the user releases the press outside the button it has no effect), notify `0x02` for this characteristic to inform Cascable to ignore the press.

