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
using UnityEngine.Serialization;

/** Captures "Air Click" events and instantiates/saves a ToolTip at that location. */
public class RecordSceneController : InputSystemGlobalHandlerListener, IMixedRealityPointerHandler {
  #region Public Static Variables
  // Static reference to the current instance of the class.
  public static RecordSceneController Instance;
  public static State CurrentState {
    get => Instance.state;
  }
  #endregion
   
  #region Public Variables
  // Represents the prefab to render when marking a location.
  public GameObject tooltipPrefab;
  // Represents the parent of all ToolTips
  public Transform mixedRealityPlayspace;

  [Header("Buttons")] 
  public GameObject loadButton;
  public GameObject resetButton;
  public GameObject stopButton;

  // Represents the RecordScene state. A user can either be idle, playing a video or recording a video.
  // Each state can only perform certain actions.
  // @default Idle
  [HideInInspector]
  public State state = State.Idle;
  public enum State {
    // When a video is being recorded.
    Recording,
    // When a video is being played.
    Playing,
    Idle,
  }
  #endregion

  #region Private Variables
  // Reference to the ToolTip Store.
  private ToolTipStore _toolTipStore;
  #endregion
  
  #region Unity Methods
  void Awake() {
    if(Instance == null) {
      // Save the reference to the instance
      Instance = this;
      // Load ToolTip data from storage
      // TODO: Rename this.
      _toolTipStore = GetData();
    }
    else Destroy(this);
  }

  void Update() {
    // Handle showing the "Reset" and "Load" buttons when not in RECORDING state
    if (state == State.Recording) {
      loadButton.SetActive(false);
      resetButton.SetActive(false);
      stopButton.SetActive(true);
    }
    else {
      loadButton.SetActive(true);
      resetButton.SetActive(true);
      stopButton.SetActive(false);
    }
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
   * In IDLE state, when clicking on any surface except another ToolTip, create a new ToolTip at that location
   * and start recording a video.
   */
  public void OnPointerClicked(MixedRealityPointerEventData eventData) {
    // If we are not in the Idle State, don't do anything.
    if (state != State.Idle) {
      Debug.Log("OnPointerClicked: Not in Idle State");
      return;
      
    }
    // Log for debugging.
    Debug.Log("OnPointerClicked: Success");
    
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
    TooltipDetails tooltipDetails = new TooltipDetails() {
      globalPose = pose
    };
    InstantiateToolTip(tooltipDetails);
    
    // Save the ToolTip to the storage
    _toolTipStore.Add(tooltipDetails);
    
    // Start recording
    StartRecording(tooltipDetails);
  }

  public void OnPointerDown(MixedRealityPointerEventData eventData) {}

  public void OnPointerUp(MixedRealityPointerEventData eventData) {}

  public void OnPointerDragged(MixedRealityPointerEventData eventData) {}
  #endregion IMixedRealityPointerHandler
  
  #region Private Methods
  /** Given a pose, create a ToolTip and save it to disks */
  private GameObject InstantiateToolTip(TooltipDetails tooltipDetails) {
    // Instantiate a ToolTip component with it's parent being the MixedRealityPlayspace
    // TODO: Does this still have to be the case? Maybe we can move it under it's own GameObject?
    GameObject toolTipGo = Instantiate(tooltipPrefab, mixedRealityPlayspace);
    
    // If the name is missing, generate a unique name
    if (String.IsNullOrEmpty(tooltipDetails.name)) {
      tooltipDetails.name = "Tooltip " + (_toolTipStore.Count() + 1);
    }
    
    // Configure the ToolTip
    toolTipGo.GetComponent<VideoToolTipController>().tooltipDetails = tooltipDetails;

    // Add world locking
    // TODO: Is this needed anymore?
    toolTipGo.AddComponent<ToggleWorldAnchor>().AlwaysLock = true;

    return toolTipGo;
  }
  
  /** Given a ToolTipDetail, start recording a video for it */
  private void StartRecording(TooltipDetails toolTipDetails) {
    // Change the state to RECORDING
    state = State.Recording;

    // Start the recording and pass the name of the file which is the same name as the toolTip.
    // e.g. "tooltip_1.mp4" since we remove spaces, capitalization and add a .mp4 extension.
    VideoRecordingProvider.Start(toolTipDetails.name);
  }
  #endregion
  
  #region Public Methods
  /**
   * When in IDLE state and the user says the keyword "Mark", create a ToolTip
   * at the current location ans start a recording.
   */
  public void Mark() {
    // If we are not in the Idle State, don't do anything.
    if (state != State.Idle) {
      Debug.Log("Speech Recognized: \"Mark\": Not in Idle State");
      return;
    }
    
    Debug.Log("Speech Recognized: \"Mark\"");
    
    // Get the pose for the primary pointer when saying "Mark".
    Vector3    position = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.Details.Point;
    Quaternion rotation = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Rotation;
    Pose       pose     = new Pose(position, rotation);
    
    // Create a ToolTip at the pose
    TooltipDetails tooltipDetails = new TooltipDetails() {
      globalPose = pose
    };
    InstantiateToolTip(tooltipDetails);
    
    // Save the ToolTip to the storage
    _toolTipStore.Add(tooltipDetails);
    
    // Start recording
    StartRecording(tooltipDetails);
  }
  
  /**
   * When the user is in RECORD state, stop recording and save the video to the storage.
   */
  public void EndMarking() {
    if (state != State.Recording) {
      Debug.Log("Speech Recognized: \"End Marking\": Not in Recording State");
      return;  
    }

    Debug.Log("Speech Recognized: \"End Marking\"");

    // When stopping the video, store the video file path to the ToolTip.
    string videoFilePath = VideoRecordingProvider.Stop();

    // Once the video has been stopped, change the state to IDLE.
    state = State.Idle;

    // While the recording is in progress, associated the video file path with the tooltip
    _toolTipStore.UpdateVideoFilePath(_toolTipStore.GetLastToolTip(), videoFilePath);
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
        InstantiateToolTip(toolTipDetails);
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
    public void Add(TooltipDetails toolTipDetails) {
      tooltipDetails.Add(toolTipDetails);
      
      // Save the data to disk
      string filePath       = Path.Combine(Application.streamingAssetsPath, fileName);
      string serializedData = JsonUtility.ToJson(this, true);
      File.WriteAllText(filePath, serializedData);
    }
    
    /** Update the video file path for a ToolTip */
    public void UpdateVideoFilePath(TooltipDetails toolTipDetails, string videoFilePath) {
      // Find the tooltip in the store
      TooltipDetails tooltipDetails = this.tooltipDetails.Find(td => td.name == toolTipDetails.name);
      
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