using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Experimental.InteractiveElement;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using Microsoft.MixedReality.WorldLocking.Tools;
using UnityEditor;
using UnityEngine;

/** Captures "Air Click" events and instantiates/saves a ToolTip at that location. */
public class RecordSceneController : InputSystemGlobalHandlerListener, IMixedRealityPointerHandler {
  #region Public Variables
  // Represents the prefab to render when marking a location.
  public GameObject tooltipPrefab;
  // Represents the parent of all ToolTips
  public Transform mixedRealityPlayspace;

  [Header("Buttons")] 
  public GameObject load;
  public GameObject reset;
  public GameObject stop;
  #endregion

  #region Private Variables
  // Reference to the ToolTip Store.
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
  /**
   * When clicking, mark the location as if the user said "Mark"
   *
   * Note that we do not want to create a tooltip if we have clicked on a ToolTip.
   */
  public void OnPointerClicked(MixedRealityPointerEventData eventData) {
    Debug.Log("OnPointerClicked");
    
    // If the user clicked on a ToolTip, do not create a new one.
    GameObject clickedGo = eventData.Pointer.Result.CurrentPointerTarget;
    if (clickedGo != null && clickedGo.GetComponentInParent<ToolTip>() != null) {
      Debug.Log("OnPointerClicked: Clicked on a ToolTip. Exiting");
      return; 
    }

    // Once a click event is received, we capture the hit location and rotation to create a Pose.
    Vector3    position = eventData.Pointer.Result.Details.Point;
    Quaternion rotation = eventData.Pointer.Rotation;
    Pose       pose  = new Pose(position, rotation);

    // Create a ToolTip at the pose
    GameObject toolTip = CreateToolTip(pose);
    
    // Start recording
    StartRecording(toolTip);
  }

  public void OnPointerDown(MixedRealityPointerEventData eventData) {}

  public void OnPointerUp(MixedRealityPointerEventData eventData) {}

  public void OnPointerDragged(MixedRealityPointerEventData eventData) {}
  #endregion IMixedRealityPointerHandler
  
  #region Private Methods
  /** Given a pose, create a ToolTip and save it to disks */
  private GameObject CreateToolTip(Pose pose) {
    // Instantiate a ToolTip component with it's parent being the MixedRealityPlayspace
    // TODO: Does this still have to be the case? Maybe we can move it under it's own GameObject?
    GameObject toolTipGo = Instantiate(tooltipPrefab, mixedRealityPlayspace);
    
    // Configure the ToolTip
    toolTipGo.name                                = "Tooltip " + (_toolTipStore.Count() + 1);
    toolTipGo.GetComponent<ToolTip>().ToolTipText = toolTipGo.name;
    toolTipGo.transform.SetGlobalPose(pose);
    
    // Add world locking
    // TODO: Is this needed anymore?
    toolTipGo.AddComponent<ToggleWorldAnchor>().AlwaysLock = true;

    // Save the ToolTip to the storage
    _toolTipStore.Add(toolTipGo);

    return toolTipGo;
  }
  
  /** Given a ToolTip, start recording a video for it */
  private void StartRecording(GameObject toolTip) {
    // Hide the Reset and Load buttons
    reset.SetActive(false);
    load.SetActive(false);
    stop.SetActive(true);
    
    // Start the recording and save the video to a file with the same name as the toolTip.
    // e.g. "tooltip_1.mp4" since we remove spaces, capitalization and add a .mp4 extension.
    string sanitizedFilePath = VideoRecordingProvider.Start(toolTip.name);
    
    // While the recording is in progress, associated the video file path with the tooltip
    _toolTipStore.UpdateVideoFilePath(toolTip, sanitizedFilePath);
  }
  #endregion
  
  #region Public Methods
  /** When the user says "Mark", this will create a ToolTip at the primary pointer pose and start recording */
  public void Mark() {
    Debug.Log("Speech Recognized: \"Mark\"");
    
    // Get the pose for the primary pointer when saying "Mark".
    Vector3    position = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.Details.Point;
    Quaternion rotation = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Rotation;
    Pose       pose     = new Pose(position, rotation);
    
    // Create a ToolTip at the pose
    GameObject toolTip = CreateToolTip(pose);
    
    // Start recording
    StartRecording(toolTip);
  }
  
  /** When the user says "End Marking", this will stop the recording */
  public void EndMarking() {
    // Show the Reset and Load buttons
    reset.SetActive(true);
    load.SetActive(true);
    stop.SetActive(false);
    
    Debug.Log("Speech Recognized: \"End Marking\"");
    VideoRecordingProvider.Stop();
  }
  
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

  /** Reset ToolTip data (on disk as well) and delete all tooltip recordings. */
  public void ResetTooltips() {
    // Reset the ToolTipStore and persist it.
    _toolTipStore.tooltipDetails.Clear();
    // Persist the TooltipStore
    SaveData(_toolTipStore);
    
    // Delete all files in thee Streaming Assets directory.
    string[] videoFiles = Directory.GetFiles(Application.streamingAssetsPath);
    foreach (string videoFile in videoFiles) {
      File.Delete(videoFile);
    }
  }
  #endregion

  #region Data Persistence Helpers
  // TODO: This can be in it's own class.
  ToolTipStore GetData() {
    try {
      string serializedData = File.ReadAllText(Application.streamingAssetsPath + "/tooltips.json");  
      return JsonUtility.FromJson<ToolTipStore>(serializedData);
    } catch (FileNotFoundException) {
      Debug.Log("No Tooltip data found. Creating new data.");
      return new ToolTipStore();
    }
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
    public int Count() => tooltipDetails.Count;
    public TooltipDetails GetLastToolTip() => tooltipDetails[tooltipDetails.Count - 1];
  
    /** Add a new ToolTip to the store and saves it to disk */
    public void Add(GameObject toolTip) {
      tooltipDetails.Add(new TooltipDetails {
        name       = toolTip.name,
        globalPose = toolTip.transform.GetGlobalPose()
      });
      
      // Save the data to disk
      string filePath       = Path.Combine(Application.streamingAssetsPath, fileName);
      string serializedData = JsonUtility.ToJson(this, true);
      File.WriteAllText(filePath, serializedData);
    }
    
    /** Update the video file path for a ToolTip */
    public void UpdateVideoFilePath(GameObject toolTip, string videoFilePath) {
      // Find the tooltip in the store
      TooltipDetails tooltipDetails = this.tooltipDetails.Find(td => td.name == toolTip.name);
      
      // Update the value
      tooltipDetails.videoFilePath = videoFilePath;
      
      // Save the data to disk
      // TODO: This is duplicated
      string filePath       = Path.Combine(Application.streamingAssetsPath, fileName);
      string serializedData = JsonUtility.ToJson(this, true);
      File.WriteAllText(filePath, serializedData);
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