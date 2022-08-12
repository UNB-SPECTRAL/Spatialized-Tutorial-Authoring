using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Tools;
using UnityEngine;

/** Captures "Air Click" events and instantiates/saves a ToolTip at that location. */
public class RecordSceneController : InputSystemGlobalHandlerListener, IMixedRealityPointerHandler {
  #region Public Static Variables
  /**
   * Static reference to the instantiated RecordSceneController. This allows
   * other GameObjects to access the instantiated RecordSceneController without
   * the need to pass it a reference.
   */
  public static RecordSceneController Instance;
  
  /**
   * Static reference to the instantiated RecordSceneController's `state` field.
   * This is used in a few GameObjects to skip writing `RecordSceneController.Instance.state`
   * and just write `RecordSceneController.CurrentState`.
   *
   * TODO: Improvement could be to rename this to State and update the internal State reference
   * to something else.
   */
  public static State CurrentState => Instance.state;
  #endregion
   
  #region Public Variables
  /** The GameObject to instantiate when "Mark"ing a location */
  public GameObject tooltipPrefab;
  /** The parent GameObject that all `tooltipPrefab`'s will be instantiated under. */
  public Transform mixedRealityPlayspace;
  
  /**
   * In the Record scene, there are currently 3 buttons which are used to "work" the scene.
   * A reference to each of them is defined below.
   *
   * TODO: We might change this in the future as we update the UI of this scene.
   */
  [Header("Buttons")] 
  public GameObject loadButton;
  public GameObject resetButton;
  public GameObject stopButton;
  
  /**
   * Represents the state that the scene is in. This is used to make sure only certain actions can be
   * performed during certain states. This was implemented to resolve the situation during a RECORDING
   * state a user could "Mark" many locations. Or watch many videos at the same time.
   */
  [HideInInspector]
  public State state = State.Idle;
  public enum State {
    /** Represents when a "Mark" action has been performed and the user is currently recording a video. */
    Recording,
    /**
     * Represents when any video is being played. This is used so that we don't start a recording while
     * a video is being played.
     */
    Playing,
    /**
     * Represents tha base state of the application. This means that a user can either "Mark" or view
     * a video.
     */
    Idle,
  }
  #endregion

  #region Private Variables
  /**
   * Instance reference to the ToolTipStore.
   *
   * TODO: This should be moved into it's own file. Something like `ToolTipStore.data.cs` where the
   * `.data` extension specifies that this will be stored in Streaming Assets. This can be observable
   * as well so anytime that we call `set` we update the persistent stored version.
   */
  private ToolTipStore _toolTipStore;
  
  /** Reference to the DictationHandler script which is used to toggle speech-to-text */
  private DictationHandler _dictationHandler;
  #endregion
  
  #region Unity Methods
  /**
   * In the Unity Awake method, we do the following actions:
   * - Set the static reference to the instantiated RecordSceneController if none exists. Otherwise, destroy this GameObject.
   * - Load the ToolTipStore from storage.
   */
  void Awake() {
    if(Instance == null) {
      // Save the reference to the instance
      Instance = this;
      // Load ToolTip data from storage
      // TODO: Rename this.
      _toolTipStore = GetData();
    }
    else Destroy(this);
    
    /*** Component Validation ***/
    // Validate that it has a SpeechHandler
    if(GetComponent<SpeechInputHandler>() == null) {
      Debug.LogError("RecordSceneController requires a SpeechInputHandler component.");
    }
    // Validate that is has a DictationHandler
    if (GetComponent<DictationHandler>() == null) {
      Debug.LogError("RecordSceneController requires a DictationHandler component.");
    }
    
    /*** Component Reference & Setup ***/
    // Save a reference to the DictationHandler
    _dictationHandler = GetComponent<DictationHandler>();
    // Add an event listener when the DictationHandler stops recording.
    _dictationHandler.OnDictationComplete.AddListener(OnDictationComplete);
    // Add an event listener when the DictationHandler has an error
    _dictationHandler.OnDictationError.AddListener(OnDictationError);
  }
  
  /**
   * In the Unity Update method, we do the following actions:
   * - Handle the active states of the three UI buttons based on the RECORDING state.
   */
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
  
  /** TODO: Remove this section and use the InteractionHandler Global approach */
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
  /** TODO: If the above section is removed, this section can also be removed */
  /**
   * In IDLE state, when clicking on any surface except another ToolTip, create a new ToolTip at that location
   * and start recording a video.
   */
  public void OnPointerClicked(MixedRealityPointerEventData eventData) {
    // If we are not in the Idle State, don't do anything.
    if (state != State.Idle) {
      Debug.Log("OnPointerClicked: Not in Idle State"); 
    } else {
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
      Pose       pose     = new Pose(position, rotation);

      // Create a ToolTip at the pose
      ToolTipDetails toolTipDetails = new ToolTipDetails() {
        globalPose = pose
      };
      InstantiateToolTip(toolTipDetails);
    
      // Save the ToolTip to the storage
      _toolTipStore.Add(toolTipDetails);
    
      // Start recording
      StartRecording(toolTipDetails);
    }
  }

  public void OnPointerDown(MixedRealityPointerEventData eventData) {}

  public void OnPointerUp(MixedRealityPointerEventData eventData) {}

  public void OnPointerDragged(MixedRealityPointerEventData eventData) {}
  #endregion IMixedRealityPointerHandler
  
  #region Private Methods
  /** Given a pose, create a ToolTip and save it to disks */
  private void InstantiateToolTip(ToolTipDetails toolTipDetails) {
    // Instantiate a ToolTip component with it's parent being the MixedRealityPlayspace
    // TODO: Does this still have to be the case? Maybe we can move it under it's own GameObject?
    GameObject toolTipGo = Instantiate(tooltipPrefab, mixedRealityPlayspace);
    
    // If the name is missing, generate a unique name
    if (String.IsNullOrEmpty(toolTipDetails.name)) {
      toolTipDetails.name = "Tooltip " + (_toolTipStore.Count() + 1);
    }
    
    // Configure the ToolTip
    toolTipGo.GetComponent<VideoToolTipController>().toolTipDetails = toolTipDetails;

    // Add world locking
    // TODO: Is this needed anymore?
    toolTipGo.AddComponent<ToggleWorldAnchor>().AlwaysLock = true;
  }
  
  /** Given a ToolTipDetail, start recording a video for it */
  /**
   * Once a ToolTip has been instantiated, this method is called to start the recording process.
   * The recording process includes:
   * - Changing the state to `Recording`
   * - Starting a video (camera/microphone) recording
   * - Starting a speech to text (via Unity Dictation) recording
   */
  private void StartRecording(ToolTipDetails toolTipDetails) {
    // Change the state to RECORDING
    state = State.Recording;
    
    /*** Start Recording ***/
    // Start video recording (pass along the filename)
    VideoRecordingProvider.StartRecording(toolTipDetails.name);
    // Start a dictation recording
    _dictationHandler.StartRecording(); 
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
    ToolTipDetails toolTipDetails = new ToolTipDetails() {
      globalPose = pose
    };
    InstantiateToolTip(toolTipDetails);
    
    // Save the ToolTip to the storage
    _toolTipStore.Add(toolTipDetails);
    
    // Start recording
    StartRecording(toolTipDetails);
  }
  
  /**
   * When the user is in `Recording` state, stop recording and save the video to the storage.
   */
  public void EndMarking() {
    if (state != State.Recording) {
      Debug.Log("Speech Recognized: \"End Marking\": Not in Recording State");
      return;  
    }

    Debug.Log("Speech Recognized: \"End Marking\"");
    
    /*** Stop Recording ***/
    // Stop video recording (this method returns the file path of the video)
    string videoFilePath = VideoRecordingProvider.StopRecording();
    // Stop the dictation recording
    _dictationHandler.StopRecording();
    
    /*** Update State ***/
    // Once the video has been stopped, change the state to `Idle`.
    state = State.Idle;

    // While the recording is in progress, associated the video file path with the tooltip
    _toolTipStore.UpdateLastTooltipVideoFilePath(videoFilePath);
  }
  
  /** Instantiate ToolTips from store */
  public void LoadToolTips() {
    Debug.Log("Load Anchors");

    foreach (ToolTipDetails toolTipDetails in _toolTipStore.tooltips) {
      Debug.Log("Found Tooltip: " + toolTipDetails.name + " in store");

      // Before instantiation, check if the tooltip is already in the scene.
      GameObject existingTooltip = GameObject.Find(toolTipDetails.name);

      // If the ToolTip does exist, skip it.
      if (existingTooltip != null) {
        Debug.Log(toolTipDetails.name + " already exists in scene.");
        continue;
      }
      
      // Otherwise, instantiate it.
      // Instantiate the tooltip using the given prefab and set its parent to the playspace.
        InstantiateToolTip(toolTipDetails);
    }
  }

  /** Reset ToolTip data (on disk as well) and delete all tooltip recordings. */
  public void ResetTooltips() {
    // Reset the ToolTipStore and persist it.
    _toolTipStore.tooltips.Clear();
    // Persist the TooltipStore
    SaveData(_toolTipStore);
    
    // Delete all files in the Streaming Assets directory.
    string[] videoFiles = Directory.GetFiles(Application.streamingAssetsPath);
    foreach (string videoFile in videoFiles) {
      File.Delete(videoFile);
    }
    
    // Also delete all GameObjects tagged as ToolTip so that they don't appear anymore.
    GameObject[] tooltips = GameObject.FindGameObjectsWithTag("ToolTip");
    foreach (GameObject tooltip in tooltips) {
      Destroy(tooltip);
    }
  }
  #endregion
  
  #region Dictation Event Handler
  /** When `DictationHandler.StopRecording` is called, this function will be given the final transcript */
  void OnDictationComplete(string transcript) {
    // FIXME: Logging
    Debug.Log("Dictation Complete: " + transcript);
    
    // Associate this transcript to the latest ToolTip
    _toolTipStore.UpdateLastToolTipTranscript(transcript);
  }

  void OnDictationError(string error) {
    Debug.LogError("DictationHandler Error: " + error);
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
  
  /** The model used to store all the ToolTip information which is persisted to storage. */
  [Serializable]
  public class ToolTipStore {
    /**
     * All persistent classes should include a `fileName` field so that we can automatically
     * store the file without specifying it's name.
     */
    public  string              fileName       = "tooltips.json";
    /** All ToolTip details are stores in this list. */
    public List<ToolTipDetails> tooltips = new List<ToolTipDetails>();
    
    /*** Helpers ***/
    public int Count() => tooltips.Count;
    public ToolTipDetails GetLastToolTip() => tooltips[Count() - 1];
  
    /** Add a new ToolTip to the store and saves it to disk */
    public void Add(ToolTipDetails toolTipDetails) {
      tooltips.Add(toolTipDetails);
      
      // Save data to disk
      Save();
    }
    
    /** Update the video file path for a ToolTip */
    public void UpdateLastTooltipVideoFilePath(string videoFilePath) {
      // Get the last ToolTip in the list
      ToolTipDetails lastToolTipDetails = GetLastToolTip();
      
      // Update the value
      lastToolTipDetails.videoFilePath = videoFilePath;
      
      // Save data to disk
      Save();
    }

    /** Easily allows the last ToolTip to be update with the latest transcription text */
    public void UpdateLastToolTipTranscript(string transcript) {
      // Get the last ToolTipDetails
      ToolTipDetails lastToolTipDetails = GetLastToolTip();
      
      // Update the value
      lastToolTipDetails.transcript = transcript;
      
      // Save data to disk
      Save();
    }
    
    /*** Persistence Helpers ***/
    /** TODO: It would be great to have this be called when any setters are called. */
    private void Save() {
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
  public class ToolTipDetails {
    public string name; // The name of the tooltip (used to name the GameObject and the text used on the game object).
    public Pose   globalPose; // The global location and rotation of the game object.
    public string videoFilePath; // File path of the associated video.
    public string transcript; // The transcript of the video.
  }
  #endregion
}