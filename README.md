<div align="center">
   <h1>Spatial Tuturials</h1>

   <p>Experiment using the Mircrosoft HoloLens to validate whether spatialized tutorials offer an advantage.</p>

   <div>
      <img src="https://img.shields.io/badge/HoloLens%202-0078D4?style=for-the-badge&logo=microsoft&logoColor=white" />
      &nbsp;&nbsp;&nbsp;
      <img src="https://img.shields.io/badge/MRTK%202-0078D4?style=for-the-badge&logo=microsoft&logoColor=white" />
      &nbsp;&nbsp;&nbsp;
      <img src="https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white" />
      &nbsp;&nbsp;&nbsp; 
      <img src="https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white" />
   </div>
</div>

---

## Welcome

Welcome to the HCI Lab's Spatial Tutorials repo. This repo contains the Unity 
code which runs on a Microsoft HoloLens 2.

<!-- TODO: Talk more about the project, it's goals -->

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

To get the project running on the HoloLens 2 there are two ways to do so. The 
first way is to check if this project is already installed on your HoloLens as
the HCI Lab has installed it on a few HoloLens 2 devices. If it is, simply
click on the application to get started.

If the application is not installed on the HoloLens 2 device, then you will
need to first complete the steps in **Windows Machine**


To get started running the project, please follow the installs steps on the 
Microsoft 

## Contributing

<!-- TODO: Add notes for contributing to the project -->
# Documentation

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

##### *Why are there 3 Spatial Observers in my project?*

https://github.com/microsoft/MixedRealityToolkit-Unity/issues/10417

# Troubleshooting

**HoloLens home button does not work**

The HoloLens in application wrist home menu/button should always work. If it does
not, this is usually unrelated to your Unity code. 

To resolve this, you must restart the HoloLens by following these steps:

1. Disconnect the HoloLens from your computer (removing the Type-C cable).
2. Press and hold the power button for 15 seconds
3. Wait 3 seconds
4. Then press the power button again (do not hold)
5. Validate that the HoloLens home button it working. If not, go back to Step #2 as the power button may not have been held down long enough.