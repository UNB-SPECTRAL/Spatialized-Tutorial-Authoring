using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Tools;
using UnityEngine;

/*** Import Helpers ***/
using StepDetails = RecordSceneController.StepStore.StepDetails;

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
   
  #region Unity Editor Fields
  /** The GameObject to instantiate when "Mark"ing a location */
  public GameObject stepPrefab;
  /**
   * The parent GameObject that all `tooltipPrefab`'s will be instantiated under.
   *
   * TODO: Determine should be the parent for correct WLT support.
   */
  public Transform stepPrefabParent;
  
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
  private StepStore _stepStore;
  
  /** Reference to the DictationHandler script which is used to toggle speech-to-text */
  private DictationHandler _dictationHandler;
  #endregion
  
  #region Unity Methods
  /**
   * In the Unity Awake method, we do the following actions:
   * - Set the static reference to the instantiated RecordSceneController if none exists. Otherwise, destroy this GameObject.
   * - Load the ToolTipStore from storage.
   */
  private void Awake() {
    if(Instance == null) {
      // Save the reference to the instance
      Instance = this;
      // Load ToolTip data from storage
      // TODO: Rename this.
      _stepStore = StepStore.Load();
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
    // Capture the transcript hypothesis in real-time to determine if the keyword
    // "End Marking" is being said to stop the recording. This has to be done this
    // way since the MRTK keyword recognizer is turned off when the DictationHandler
    // is recording.
    _dictationHandler.OnDictationHypothesis.AddListener(OnDictationHypothesis);
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
      StepDetails stepDetails = new StepDetails() {
        globalPose = pose
      };
      InstantiateToolTip(stepDetails);
    
      // Save the ToolTip to the storage
      _stepStore.Add(stepDetails);
    
      // Start recording
      StartRecording(stepDetails);
    }
  }

  public void OnPointerDown(MixedRealityPointerEventData eventData) {}

  public void OnPointerUp(MixedRealityPointerEventData eventData) {}

  public void OnPointerDragged(MixedRealityPointerEventData eventData) {}
  #endregion IMixedRealityPointerHandler
  
  #region Private Methods
  /** Given a pose, create a ToolTip and save it to disks */
  private void InstantiateToolTip(StepDetails stepDetails) {
    // Instantiate a ToolTip component with it's parent being the MixedRealityPlayspace
    // TODO: Does this still have to be the case? Maybe we can move it under it's own GameObject?
    GameObject toolTipGo = Instantiate(stepPrefab, stepPrefabParent);
    
    // If the name is missing, generate a unique name
    if (String.IsNullOrEmpty(stepDetails.name)) {
      stepDetails.name = "Step " + (_stepStore.Count + 1);
    }
    
    // Configure the ToolTip
    toolTipGo.GetComponent<StepController>().stepDetails = stepDetails;

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
  private void StartRecording(StepDetails stepDetails) {
    // Change the state to RECORDING
    state = State.Recording;
    
    /*** Start Recording ***/
    // Start video recording (pass along the filename)
    CameraProvider.StartRecording(stepDetails.name);
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
    StepDetails stepDetails = new StepDetails() {
      globalPose = pose
    };
    InstantiateToolTip(stepDetails);
    
    // Save the ToolTip to the storage
    _stepStore.Add(stepDetails);
    
    // Start recording
    StartRecording(stepDetails);
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
    string videoFilePath = CameraProvider.StopRecording();
    // Stop the dictation recording
    _dictationHandler.StopRecording();
    
    /*** Update State ***/
    // Once the video has been stopped, change the state to `Idle`.
    state = State.Idle;

    // While the recording is in progress, associated the video file path with the tooltip
    _stepStore.UpdateLastTooltipVideoFilePath(videoFilePath);
  }
  
  /** Instantiate ToolTips from store */
  public void LoadToolTips() {
    Debug.Log("Load Steps");

    foreach (StepDetails toolTipDetails in _stepStore.steps) {
      Debug.Log("Found " + toolTipDetails.name.Split(':')[0] + " in store");

      // Before instantiation, check if the tooltip is already in the scene.
      GameObject existingTooltip = GameObject.Find(toolTipDetails.name);

      // If the ToolTip does exist, skip it.
      if (existingTooltip != null) {
        Debug.Log(toolTipDetails.name.Split(':')[0] + " already exists in scene.");
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
    _stepStore.Reset();

    // Delete all files in the Streaming Assets directory.
    string[] videoFiles = Directory.GetFiles(Application.streamingAssetsPath);
    foreach (string videoFile in videoFiles) {
      File.Delete(videoFile);
    }
    
    // Also delete all GameObjects tagged as ToolTip so that they don't appear anymore.
    GameObject[] tooltips = GameObject.FindGameObjectsWithTag("Step");
    foreach (GameObject tooltip in tooltips) {
      Destroy(tooltip);
    }
  }
  #endregion
  
  #region Dictation Event Handler
  /**
   * When the DictationHandler is recording, this method will be given it's hypothesized transcript
   * which we use to determine if the keyword "End Marking" is being said to stop the recording.
   */
  void OnDictationHypothesis(string transcript) {
    // We found that "End Marking" is sometimes transcribed as "And Marking"
    // so we are just now looking for the keyword "Marking" instead.
    if (transcript.ToLower().Contains("marking")) {
      EndMarking();
    }
  }
  
  
  /** When `DictationHandler.StopRecording` is called, this function will be given the final transcript */
  void OnDictationComplete(string transcript) {
    // FIXME: Logging
    Debug.Log("Dictation Complete: " + transcript);
    
    // Associate this transcript to the latest ToolTip
    _stepStore.UpdateLastStepTranscript(transcript);
  }

  void OnDictationError(string error) {
    Debug.LogError("DictationHandler Error: " + error);
  }

  #endregion

  #region Serialized Classes
  /** The model used to store Steps */
  [Serializable]
  public class StepStore {
    /*** Public Static Variables ***/
    /**
     * Since this is a data file, there is a 1-1 mapping between data on disk and in memory.
     * This field is used to associated the two.
     *
     * Since the this class will be serialized, we include a `.json` extension.
     */
    public static string FileName = "steps" + ".json";
    
    /*** Instance Public Variables ***/
    /** Field which stores all the steps */
    public List<StepDetails> steps = new List<StepDetails>();
    
    /*** Helpers ***/
    public int Count => steps.Count;
    public StepDetails GetLastStep() => steps[Count - 1];
  
    /** Add a new ToolTip to the store and saves it to disk */
    public void Add(StepDetails stepDetails) {
      steps.Add(stepDetails);
      
      // Save data to disk
      Save();
    }
    
    /** Update the video file path for a ToolTip */
    public void UpdateLastTooltipVideoFilePath(string videoFilePath) {
      // Get the last ToolTip in the list
      StepDetails lastStepDetails = GetLastStep();
      
      // Update the value
      lastStepDetails.videoFilePath = videoFilePath;
      
      // Save data to disk
      Save();
    }
    
    public void UpdateLastStepTranscript(string transcript) {
      StepDetails lastStepDetails = GetLastStep();
      
      // Update the value
      lastStepDetails.transcript = transcript;
      
      // TODO: Can we make this reactive?
      // When updating the transcript, also update the text so that we can include
      // 20 characters of transcript text in the UI.
      lastStepDetails.name += ": " + transcript.Substring(0, (Math.Min(16, transcript.Length))) + "...";
      
      // Save data to disk
      Save();
    }
    
    /** Reset this StepStore instance */
    public void Reset() {
      steps.Clear();
      Save();
    }
    
    /*** Persistence Helpers ***/
    /** TODO: It would be great to have this be called when any setters are called. */
    private void Save() {
      string filePath       = Path.Combine(Application.streamingAssetsPath, FileName);
      string serializedData = JsonUtility.ToJson(this, true);
      File.WriteAllText(filePath, serializedData);
    }
  
    /** Load the StepStore date from disk */
    public static StepStore Load() {
      // Try to load the data from disk since it could have been deleted.
      try {
        string filePath       = Path.Combine(Application.streamingAssetsPath, FileName);
        string serializedData = File.ReadAllText(filePath);  
        return JsonUtility.FromJson<StepStore>(serializedData);
      } catch (FileNotFoundException) {
        Debug.Log("No step date found. Creating new data.");
        
        // When no step data is found, we create a new instance.
        StepStore stepStore = new StepStore();
        
        // And save that instance to disk.
        stepStore.Save();
        
        // Return the new instance.
        return new StepStore();
      }
    }
    
    #region Child Class
    /** Stores step information */
    [Serializable]
    public class StepDetails {
      /** The name of the Step which will be shown on the Step prefab */
      public string name;
      /** The global Unity position of the Step. This only works if using the Microsoft World Locking Toolkit */
      public Pose globalPose;
      /** The path to the video file */
      public string videoFilePath;
      /**
     * The transcript of the recorded video.
     *
     * TODO: Technically the recording and video are done at the same time so
     * there could be some text that does not make it in the video or text that
     * is missing from the video due to timing. It would be great to send the
     * audio from the recorded video to the DictationHandler so that they match.
     */
      public string transcript; // The transcript of the video.
    }
    #endregion
  }
  #endregion
}