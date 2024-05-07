# SpaceTüt: Spatial Tutorials

his repo contains the Unity 
code which runs on a Microsoft HoloLens 2

## Getting Started

To get started and run this application, there are two ways to do so. Either
running the application on a Windows machine or running it on a HoloLens. Both
methods are shown below.

### Windows Machine

Running the application on a Windows machine will require the following packages
to be installed:

- Unity Hub
- Mixed Reality Feature Tool

### HoloLens 2

There are two ways to get the project running on the HoloLens 2. The 
first way is to check if this project is already installed on your HoloLens, as
the HCI Lab has installed it on a few HoloLens 2 devices. If it is, simply
click on the application to get started.

If the application is not installed on the HoloLens 2 device, then you will
need to first complete the steps in **Windows Machine**

To get started running the project, please follow the install steps on the 
Microsoft.

## Contributing

To start contributing to this project, please follow these steps to set up your local machine.

### Enable `Developer Mode`

Settings > Privacy & Security > For Developers > Developer Mode > Yes

### Download [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)

Install the following **Workloads**:

- **.NET desktop development**
- **Desktop development with C++**
- **Universal Window Platform (UWP) development**
- - **Window 11 SDK**
- - **USB Device Connectivity**: To deploy and debug the HoleLens over USB
- - **C++ (v143) Universal Windows Platform tools**
- **Game development with Unity**

### Download [HoloLens Emulator](https://go.microsoft.com/fwlink/?linkid=2220897)

Then restart the computer when it asks you to.

### Download [GitHub Desktop](https://desktop.github.com/)

1. Login with your GitHub account
1. Clone the repository
1. Create a new branch off main (if you are going to be developing)
1. Rename the `Spatialized-Video-Authoring` folder to `HoloTuts` (why? due to a Windows limitation of MAX_PATH limit of 255 characters. Some package names are so long that if you use the full folder name the project will not run...)

### Download [Unity Hub](https://unity.com/download)

This application is used to dowload Unity and various Unity Editor versions.

Once downloaded, and have the code cloned to your local machine, open the project via 
Unity Hub > Open > HoloTuts > Open.

<details><summary><b>Missing Editor Version Warning</b></summary>

At this point, if you don't have the correct Unity Editor version, you will see a "Missing Editor Version" warning. Follow the instructions to install the Unity Version
that is required for this project. When installing the Unity Version 2020.3.37f1, make sure to select: 

- **Microsoft Visual Study Community 2019**: This is used to deploy your application onto the HoloLens.
- **Universal Windows Platform Build Support**: This is used to build the Unity HoloLens application.
- **Windows Build Support (IL2CPP)**

</details>

<br />

Once opening the Unity Editor project, ignore the **Unity Editor Update Check** popup as this may cause issues later on. The working version of the application is strongly tied to the chosen Unity Version.

Once you open the project via Unity Hub > Spacialized-Tutorial-Authoring then you need to change teh build target.

1. File
2. Build Settings
3. Universal Window Platform
```
Target Device: HoloLens
Architecture: x64
Build Type: D3D Project
Target SDK Version: Latest Installed
Minimum Platform Version: 10.0.10240.0
Build and Run on: Local Machine
Build configuration: Release
```
4. Switch Platform

### 3. Download the [Mixed Reality Feature Tool](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool)

When opening the Unity project in the Unity Editor, you will see red warnings at the
bottom of the application. These warnings appear since certain packages are missing.

To install the missing packages, follow these steps:

1. Open the **Mixed Reality Feature Tool**
1. Start
1. Chose the **Project Path** of the project by clicking selecting the `Spatialized-Tutorial-Authoring` folder > Open > Open

### 4. Download **[JetBrains Rider Editor](https://www.jetbrains.com/rider/)**

To edit the game object scripts, it is recommended to use this editor vs VS Code and Visual Studio given its great integration and C# language support. 

As a student, you can sign up for the [free educational license](https://www.jetbrains.com/shop/eform/students) which will give you access to the Jetbrain Rider Editor for longer than 30 days



## Documentation

### Spatial Awareness

Spatial Awareness allows the HoloLens to sense the surrounding environment and to react to it. This allows the HoloLens
to place objects "on" the environment and not "through" the environment.
This should never be disabled.

#### Spatial Observers

Spatial Observers make sense of the spatial awareness data and provide a way to react to it. This is also what renders
the visible spatial mesh.

To change the settings of the visible mesh being shown:

1. Click on the `MixedRealityToolkit` Game Object in the Unity Scene Hierarchy.
2. Open the `MixedRealityToolkit` component in the inspector.
3. Click on the `SpatialAwareness` vertical tab.
4. For each of the `SpatialObserver` components under the **Spatial Awareness System Settings** header
   1. Click on the `>` button to expand the component.
   2. Scroll to the bottom section named **Display Settings**
   3. Change the `DisplayOption` to
      any [valid option](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/spatial-awareness/configuring-spatial-awareness-mesh-observer?view=mrtkunity-2022-05#display-settings).

#### Questions

*Why are there 3 Spatial Observers in my project?*

https://github.com/microsoft/MixedRealityToolkit-Unity/issues/10417

## Troubleshooting

**HoloLens home button does not work**

The HoloLens in the application wrist home menu/button should always work. If it does
not work, this is usually unrelated to your Unity code. 

To resolve this, you must restart the HoloLens by following these steps:

1. Disconnect the HoloLens from your computer (removing the Type-C cable).
2. Press and hold the power button for 15 seconds
3. Wait 3 seconds
4. Then press the power button again (do not hold)
5. Validate that the HoloLens home button is working. If not, go back to Step #2, as the power button may not have been held down long enough.
