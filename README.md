# Spatial Tutorials

Using Microsoft's HoloLens World Locking Toolkit features to world lock spatial tutorials

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

##### *Why are there 3 Spatial Observers in my project?*

https://github.com/microsoft/MixedRealityToolkit-Unity/issues/10417