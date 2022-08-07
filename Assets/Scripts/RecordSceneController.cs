using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using Microsoft.MixedReality.WorldLocking.Tools;
using UnityEngine;
using UnityEngine.Serialization;

/** Captures "Air Click" events and instantiates/saves a ToolTip at that location. */
public class RecordSceneController : InputSystemGlobalHandlerListener, IMixedRealityPointerHandler {
  #region Public Variables
  // Represents a prefab which will be rendered at the air click location
  public GameObject tooltipPrefab;

  // Represents the parent of all ToolTips
  public Transform mixedRealityPlayspace;
  #endregion

  #region Private Variables
  // Stores the ToolTip pose and text for each air click
  private readonly ToolTipStore _toolTipStore;
  #endregion

  #region Constructor
  public RecordSceneController() {
    // When instantiating this class, load all the anchors from storage.
    _toolTipStore = GetData();
  }
  #endregion

  #region InputSystemGlobalHandlerListener Implementation
  protected override void RegisterHandlers() {
    // ReSharper disable once Unity.NoNullPropagation
    MixedRealityToolkit.Instance?.GetService<IMixedRealityInputSystem>()
      ?.RegisterHandler<IMixedRealityPointerHandler>(this);
  }

  protected override void UnregisterHandlers() {
    // ReSharper disable once Unity.NoNullPropagation
    MixedRealityToolkit.Instance?.GetService<IMixedRealityInputSystem>()
      ?.UnregisterHandler<IMixedRealityPointerHandler>(this);
  }
  #endregion InputSystemGlobalHandlerListener Implementation

  #region IMixedRealityPointerHandler
  /** Handles click events. */
  public void OnPointerClicked(MixedRealityPointerEventData eventData) {
    // Once a click event is received, we capture the hit location and rotation to create a Pose.
    Vector3    hitLocation = eventData.Pointer.Result.Details.Point;
    Quaternion hitRotation = eventData.Pointer.Rotation;
    Pose       globalPose  = new Pose(hitLocation, hitRotation);

    // Next, instantiate a tooltipPrefab with the parent of the MixedRealityPlayspace.
    GameObject newTooltip = Instantiate(tooltipPrefab, mixedRealityPlayspace);
    // Name the tooltip
    newTooltip.name = "Tooltip " + (_toolTipStore.tooltipDetails.Count + 1);
    // Add the tooltip text
    newTooltip.GetComponent<ToolTip>().ToolTipText = newTooltip.name;
    // Set its pose
    newTooltip.transform.SetGlobalPose(globalPose);
    // Add world locking
    ToggleWorldAnchor twa = newTooltip.AddComponent<ToggleWorldAnchor>();
    twa.AlwaysLock = true;

    // Then, save the tooltip details to storage for persistence.
    SaveTooltip(newTooltip);
  }

  public void OnPointerDown(MixedRealityPointerEventData eventData) {
    // Debug.Log("RecordSceneController.OnPointerDown");
  }

  public void OnPointerUp(MixedRealityPointerEventData eventData) {
    // Debug.Log("RecordSceneController.OnPointerUp");
  }

  public void OnPointerDragged(MixedRealityPointerEventData eventData) {
    // Debug.Log("RecordSceneController.OnPointerDragged");
  }
  #endregion IMixedRealityPointerHandler

  private void SaveTooltip(GameObject toolTip) {
    // Get tooltip details from the Tooltip prefab
    string name       = toolTip.GetComponent<ToolTip>().ToolTipText;
    Pose   globalPose = toolTip.transform.GetGlobalPose();

    // Add toolTip details to the Tooltip Store
    TooltipDetails tooltipDetails = new TooltipDetails {
      name       = name,
      globalPose = globalPose
    };
    _toolTipStore.tooltipDetails.Add(tooltipDetails);

    // Persist the TooltipStore
    SaveData(_toolTipStore);
  }

  #region Button Handlers
  /** Instantiate ToolTips from store */
  public void LoadToolTips() {
    Debug.Log("Load Anchors");

    foreach (TooltipDetails toolTipDetails in _toolTipStore.tooltipDetails) {
      Debug.Log("Found Tooltip: " + toolTipDetails.name + " in store");

      // Before instantiation, check if the tooltip is already in the scene.
      GameObject existingTooltip = GameObject.Find(toolTipDetails.name);

      // If the ToolTip does exist, skip it.
      if (existingTooltip != null) {
        Debug.Log(toolTipDetails.name + " already exists in scene.");
        continue;
      }
      // Otherwise, instantiate it.
      else {
        // Instantiate the tooltip using the given prefab and set its parent to the playspace.
        GameObject newTooltip = Instantiate(tooltipPrefab, mixedRealityPlayspace);
        // Name the tooltip
        newTooltip.name = toolTipDetails.name;
        // Set it's pose
        newTooltip.transform.SetGlobalPose(toolTipDetails.globalPose);
        // And world lock it.
        ToggleWorldAnchor twa = newTooltip.AddComponent<ToggleWorldAnchor>();
        twa.AlwaysLock = true;
        // And add text
        newTooltip.GetComponent<ToolTip>().ToolTipText = toolTipDetails.name;
      }
    }
  }

  /** Removes all the stored tooltips. */
  public void ResetTooltips() {
    _toolTipStore.tooltipDetails.Clear();
    // Persist the TooltipStore
    SaveData(_toolTipStore);
  }

  public void StartRecording() {
    Debug.Log("Start Recording Button Pressed");
    
    // Find what is the last ToolTip name that was created.
    string lastTooltipName = _toolTipStore.GetLastToolTip().name;
    
    // Use this name for the video file
    string sanitizedFilename = VideoRecordingProvider.Start(lastTooltipName);
    
    // Save the sanitized filename to the ToolTipStore
    _toolTipStore.tooltipDetails.Last().videoFilePath = sanitizedFilename;
    // TODO: This should be a provider as well
    SaveData(_toolTipStore);
  }
  #endregion

  #region Data Persistence Helpers
  ToolTipStore GetData() {
    string serializedData = File.ReadAllText(Application.streamingAssetsPath + "/tooltips.json");
    return JsonUtility.FromJson<ToolTipStore>(serializedData);
  }

  void SaveData(ToolTipStore toolTipStore) {
    string filePath = Path.Combine(Application.streamingAssetsPath, toolTipStore.fileName);

    string serializedData = JsonUtility.ToJson(_toolTipStore, true);

    File.WriteAllText(filePath, serializedData);
  }
  #endregion

  #region Serialized Classes
  [Serializable]
  public class ToolTipStore {
    // Represents the device file name to store the tooltip data.
    public  string               fileName       = "tooltips.json";
    public List<TooltipDetails> tooltipDetails = new List<TooltipDetails>();
    
    /*** Helpers ***/
    public TooltipDetails GetLastToolTip() {
      return tooltipDetails[tooltipDetails.Count - 1];
    }
  }

  /**
     * Class which stores tooltip details used to instantiate a Tooltip (
     *   https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/tooltip?view=mrtkunity-2022-05
     * ) prefab.
     */
  [Serializable]
  public class TooltipDetails {
    public  string name; // The name of the tooltip (used to name the GameObject and the text used on the game object).
    public  Pose   globalPose; // The global location and rotation of the game object.
    public string videoFilePath; // File path of the associated video.
  }
  #endregion
}