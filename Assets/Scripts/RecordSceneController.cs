using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using Microsoft.MixedReality.WorldLocking.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;

/*** Import Helpers ***/
using Tutorial = RecordSceneController.TutorialStore.Tutorial;
using StepDetails = RecordSceneController.TutorialStore.Tutorial.StepDetails;

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

  /***** Unity Editor Fields *****/
  #region Unity Editor Fields
  /** The GameObject to instantiate when "Mark"ing a location */
  public GameObject stepPrefab;
  /**
   * The parent GameObject that all `tooltipPrefab`'s will be instantiated under.
   *
   * TODO: Determine should be the parent for correct WLT support.
   */
  public Transform stepPrefabParent;
  
  [Header("Buttons")]
  // When this button is pressed, we create a new tutorial. It only renders in the `CreateTutorial` state.
  public GameObject createTutorialButton;
  /** When this button is pressed, we return back to the `Main Menu` scene */
  public GameObject stopTutorialButton;
  /** When this button is pressed, we stop the step recording */
  public GameObject stopStepRecordingButton;

  /**
   * Represents the state that the scene is in. This is used to make sure only certain actions can be
   * performed during certain states. This was implemented to resolve the situation during a RECORDING
   * state a user could "Mark" many locations. Or watch many videos at the same time.
   */
  [HideInInspector] 
  public State state;
  public enum State {
    /**
     * This state is the default state when rendering the scene.
     * This state allows the user to create a tutorial and then start to record steps.
     * This state can only move to the `CreateStep` state.
     * @default state. 
     */
    CreateTutorial,
    /**
     * This state is when we have create a tutorial and the user is about to "Mark" and record
     * steps.
     * This state can transition to:
     * - `StepRecording` state when "Mark"ing a location
     * - `StepPlaying` state when the user clicks on a step (to view the video).
     */
    CreateStep,
    /** Represents when a "Mark" action has been performed and the user is currently recording a video. */
    StepRecording,
    /**
     * Represents when any video is being played. This is used so that we don't start a recording while
     * a video is being played.
     */
    StepPlaying,
    
  }
  #endregion
  
  /***** Private Variables *****/
  #region Private Variables
  /** Instance of the TutorialStore */
  private TutorialStore _tutorialStore;
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
    if(Instance == null) Instance = this;
    else Destroy(this);
    
    /*** Instantiate a new TutorialStore (will try to load date from memory) ***/
    _tutorialStore = TutorialStore.Load();
    
    // Set the state to CreateTutorial
    state = State.CreateTutorial;
    Debug.Log("In State: " + state);

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
    Debug.Log("Setting Up DictationHandler Callbacks");
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
    
    /*** Enable World Locking ***/
    Debug.Log("Enabling World Locking Toolkit");
    var settings = WorldLockingManager.GetInstance().Settings;
    settings.Enabled                           = true;
    WorldLockingManager.GetInstance().Settings = settings;
  }
  
  /**
   * In the Unity Update method, we do the following actions:
   * - Handle the active states of the three UI buttons based on the RECORDING state.
   */
  private void Update() {
    // Handle showing the "Start Tutorial" button when in the CreateTutorial state.
    if(state == State.CreateTutorial) {
      createTutorialButton.SetActive(true);
      stopTutorialButton.SetActive(false);
      stopStepRecordingButton.SetActive(false);
    } 
    // Handle showing the "Stop Tutorial" button when in the CreateStep or StepPlaying state.
    else if (state == State.CreateStep || state == State.StepPlaying) {
      createTutorialButton.SetActive(false);
      stopTutorialButton.SetActive(true);
      stopStepRecordingButton.SetActive(false);
    } 
    // Handle showing the "Stop Recording" when in the "StepRecording" state.
    else if (state == State.StepRecording) {
      createTutorialButton.SetActive(false);
      stopTutorialButton.SetActive(false);
      stopStepRecordingButton.SetActive(true);
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
    if (state != State.CreateStep) {
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
      
      // Create a new ToolTip at the hit location.
      CreateStep(pose);
    }
  }

  public void OnPointerDown(MixedRealityPointerEventData eventData) {}

  public void OnPointerUp(MixedRealityPointerEventData eventData) {}

  public void OnPointerDragged(MixedRealityPointerEventData eventData) {}
  #endregion IMixedRealityPointerHandler
  
  #region Private Methods
  
  /**
   * Create a new Step for a Tutorial, instantiates that Step and
   * start a recording (video, audio).
   */
  private void CreateStep(Pose pose) {
    Debug.Log("CreateStep()");
    
    // Create a step in the TutorialSore
    StepDetails stepDetails = _tutorialStore.AddStep(pose);

    // Create the Step GameObject
    InstantiateStep(stepDetails);
    
    // Start recording
    StartRecording(stepDetails);
  }

  /** Given StepDetails, instantiate a Step in the scene. */
  private void InstantiateStep(StepDetails stepDetails) {
    // Instantiate the Step GameObject with the correct parent.
    var toolTipGo = Instantiate(stepPrefab, stepPrefabParent);

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
    // Change the state to `Step Recording`
    state = State.StepRecording;
    Debug.Log("In State: " + state);
    
    /*** Start Recording ***/
    // Start video recording (pass along the filename)
    CameraProvider.StartRecording(stepDetails.name);
    // Start a dictation recording
    _dictationHandler.StartRecording(); 
  }
  #endregion
  
  #region Public Methods
  /** When the user presses the "Start", create a new Tutorial and start recording steps */
  public void OnCreateTutorial() {
    // We must be in the "Create Tutorial" state.
    if (state != State.CreateTutorial) {
      Debug.LogError("OnCreateTutorial(): Not in \"Create Tutorial\" State. In State: " + state);
      return;
    }
    
    Debug.Log("OnCreateTutorial()");
    
    // Create a new tutorial
    _tutorialStore.AddTutorial();
    
    // Enter into "CreateStep" state
    state = State.CreateStep;
    Debug.Log("In State: " + state);
  }
  
  /** When the user pressed the "Stop" button, we return to the `Main Menu` scene */
  public void OnStopTutorial() {
    // We must be in the "Create Step" state.
    if (state != State.CreateStep) {
      Debug.LogError("OnStopTutorial: Not in \"Create Step\" State");
      return;
    }
    
    Debug.Log("Disabling World Locking Toolkit");
    var settings = WorldLockingManager.GetInstance().Settings;
    settings.Enabled = false;
    
    // Return to the Main Menu scene
    SceneManager.LoadScene("Main Menu");
  }

  /**
   * When in IDLE state and the user says the keyword "Mark", create a ToolTip
   * at the current location ans start a recording.
   */
  public void Mark() {
    // If we are not in the Idle State, don't do anything.
    if (state != State.CreateStep) {
      Debug.Log("Speech Recognized: \"Mark\": Not in Idle State");
      return;
    }
    
    Debug.Log("Speech Recognized: \"Mark\"");
    
    // Get the pose for the primary pointer when saying "Mark".
    Vector3    position = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.Details.Point;
    Quaternion rotation = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Rotation;
    Pose       pose     = new Pose(position, rotation);
    
    // Create a Step and start recording.
    CreateStep(pose);
  }
  
  /**
   * When the user is in `Recording` state, stop recording and save the video to the storage.
   */
  public void EndMarking() {
    if (state != State.StepRecording) {
      Debug.Log("End Marking(): ERROR - Not in Recording State");
      return;  
    }

    Debug.Log("End Marking()");
    
    /*** Stop Recording ***/
    // Stop video recording and save associate the video to the Step.
    _tutorialStore.UpdateLastStep("videoFilePath", CameraProvider.StopRecording());
    // Stop the dictation recording
    _dictationHandler.StopRecording();
    
    
    /*** Update State ***/
    // Once the recording has been stopped, change the state to `CreateStep`.
    state = State.CreateStep;
    Debug.Log("In State: " + state);
  }
  
  /**
   * Instantiate ToolTips from store.
   * TODO: Move this to the Guidance scene.
   */
  public void LoadTutorial(int tutorialId) {
    Debug.Log("LoadTutorial(" + tutorialId + ")");
    
    // Find the tutorial matching the tutorialId
    Tutorial tutorial = _tutorialStore.GetTutorial(tutorialId);
    if (tutorial == null) {
      Debug.LogError("LoadTutorial(): ERROR - No tutorial found with id " + tutorialId);
      return;
    }

    foreach (StepDetails toolTipDetails in tutorial.steps) {
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
      InstantiateStep(toolTipDetails);
    }
  }

  /**
   * Remove all the Tutorial data, videos and GameObjects in the scene.
   * TODO: Move this to the Guidance scene.
   */
  public void ResetTutorials() {
    Debug.Log("ResetTutorials()");

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
    
    // Reset the TutorialStore
    _tutorialStore.Reset();
  }
  #endregion
  
  #region Dictation Event Handler
  /**
   * When the DictationHandler is recording, this method will be given it's hypothesized transcript
   * which we use to determine if the keyword "End Marking" is being said to stop the recording.
   */
  private void OnDictationHypothesis(string transcript) {
    // We found that "End Marking" is sometimes transcribed as "And Marking"
    // so we are just now looking for the keyword "Marking" instead.
    if (transcript.ToLower().Contains("marking")) {
      EndMarking();
    }
  }
  
  /**
   * When `DictationHandler.StopRecording` is called, this function will be
   * given the final transcript and associate it to the last Step.
   */
  private void OnDictationComplete(string transcript) {
    Debug.Log("OnDictationComplete(): " + transcript);

    // Associate this transcript to the latest ToolTip
    _tutorialStore.UpdateLastStep("transcript", transcript);
  }

  private void OnDictationError(string error) {
    Debug.LogError("OnDictationError: ERROR - " + error);
  }
  #endregion

  /** Tutorial Store stores all tutorials and their steps. */
  // TODO: Move this into another file.
  #region TutorialStore
  [Serializable]
  public class TutorialStore {
    private const string FileName = "tutorials.json";
    
    public List<Tutorial> tutorials = new List<Tutorial>();
    
    /** Loads the TutorialStore from disk. This should be used instead of instantiation */
    public static TutorialStore Load() {
      // Try to load the data from disk since it could have been deleted.
      try {
        string        filePath       = Path.Combine(Application.streamingAssetsPath, FileName);
        string        serializedData = File.ReadAllText(filePath);
        return JsonUtility.FromJson<TutorialStore>(serializedData);
      }
      catch (FileNotFoundException) {
        Debug.Log("Cannot find " + FileName + " in " + Application.streamingAssetsPath + ". Creating new file.");
        
        // If no file exists, create a new one.
        TutorialStore tutorialStore = new TutorialStore();
        
        // Save the newly created file
        tutorialStore.Save();
        
        // Return the newly created file.
        return tutorialStore;
      }
    }

    /** Create a new tutorial */
    public void AddTutorial() {
      // Instantiate a new tutorial using the existing count at the ID + 1.
      Tutorial tutorial = new Tutorial(tutorials.Count + 1);
      
      tutorials.Add(tutorial);
      
      Save();
    }
    
    /** Create a new step for the latest tutorial. */
    public StepDetails AddStep(Pose pose) {
      Debug.Log("TutorialStore.AddStep()");
      
      // Get the latest tutorial
      Tutorial latestTutorial = tutorials[tutorials.Count - 1];
      
      // Add the step to the latest tutorial
      StepDetails stepDetails = latestTutorial.AddStep(pose);
      
      // Persist the data to disk.
      Save();
      
      return stepDetails;
    }
    
    public StepDetails UpdateLastStep(string key, string value) {
      // Get the latest tutorial
      Tutorial latestTutorial = tutorials[tutorials.Count - 1];
      
      // Update the last step in the latest tutorial
      StepDetails latestStepDetails = latestTutorial.UpdateLastStep(key, value);
      
      // Persist the data to disk.
      Save();

      return latestStepDetails;
    }
    
    public Tutorial GetTutorial(int tutorialId) {
      return tutorials.Find(tutorial => tutorial.id == tutorialId);
    }
    
    public void Reset() {
      tutorials = new List<Tutorial>();
        
      // Persist this change to the filesystem.
      Save();
    }

    private void Save() {
      string filePath       = Path.Combine(Application.streamingAssetsPath, FileName);
      string serializedData = JsonUtility.ToJson(this, true);
      File.WriteAllText(filePath, serializedData);
    }
    
    [Serializable]
    public class Tutorial {
      public int               id; // e.g. 1
      public string            name; // e.g. Tutorial 1
      public List<StepDetails> steps; // e.g. []
      
      /** Constructor */
      public Tutorial(int id) {
        this.id = id;
        name = "Tutorial " + id;
        steps = new List<StepDetails>();
      }
      
      /** Add a step to a tutorial */
      public StepDetails AddStep(Pose pose) {
        StepDetails step = new StepDetails(steps.Count + 1, pose);
        steps.Add(step);
        return step;
      }
      
      public StepDetails UpdateLastStep(string key, string value) {
        // Get the last stepDetails
        var lastStepDetails = steps[steps.Count - 1];

        switch (key) {
          case "videoFilePath":
            lastStepDetails.videoFilePath = value;
            break;
          case "transcript":
            lastStepDetails.transcript = value;
            // When updating the transcript, also update the text so that we can
            // include 15 characters of transcript text in the UI.
            lastStepDetails.name += ": " + value.Substring(0, (Math.Min(15, value.Length))) + "...";
            break;
          default:
            throw new Exception("StepDetails does not have a key named " + key + " that can be set.");
        }

        return lastStepDetails;
      }
      
      [Serializable]
      public class StepDetails {
        public int    id;   // e.g. 1
        public string name; // e.g. Step 1
        public Pose   globalPose;
        public string videoFilePath;
        public string transcript;
        
        /** Constructor */
        public StepDetails(int id, Pose pose) {
          this.id = id;
          name = "Step " + id;
          globalPose = pose;
        }
      }
    }
  }
  #endregion
}