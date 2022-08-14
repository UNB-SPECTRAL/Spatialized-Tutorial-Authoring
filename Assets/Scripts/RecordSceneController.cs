using System.IO;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Tools;
using UnityEngine;

/*** Import Helpers ***/
using Tutorial = TutorialStore.Tutorial;
using StepDetails = TutorialStore.Tutorial.StepDetails;
using SceneState = SceneController.SceneState;

/**
 * Captures "Air Click" events and instantiates/saves a ToolTip at that location.
 *
 * TODO: Rename to ActionController. Given a state, enable/disable actions.
 */
public class RecordSceneController : InputSystemGlobalHandlerListener, IMixedRealityPointerHandler {
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
  #endregion
  
  /***** Private Variables *****/
  #region Private Variables
  /*** CreateStep State ***/
  private SpeechInputHandler _speechInputHandler;
  private Interactable       _interactable;
  /*** StepRecording State ***/
  private DictationHandler _dictationHandler;
  #endregion
  
  #region Unity Methods
  /**
   * In the Unity Awake method, we do the following actions:
   * - Set the static reference to the instantiated RecordSceneController if none exists. Otherwise, destroy this GameObject.
   * - Load the ToolTipStore from storage.
   */
  private void Awake() {
    // Set the state to CreateTutorial

    /*** Component Validation ***/
    // Validate that it has a SpeechHandler
    if(GetComponent<SpeechInputHandler>() == null) {
      Debug.LogError("RecordSceneController requires a SpeechInputHandler component.");
    }
    // Validate that is has a DictationHandler
    if (GetComponent<DictationHandler>() == null) {
      Debug.LogError("RecordSceneController requires a DictationHandler component.");
    }
    
    /*** Component References ***/
    _speechInputHandler = GetComponent<SpeechInputHandler>();
    _interactable       = GetComponent<Interactable>();
    
    _dictationHandler   = GetComponent<DictationHandler>();
    
    /*** Component Reference & Setup ***/
    Debug.Log("Setting Up DictationHandler Callbacks");
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
  
  void Update() {
    switch (SceneController.State) {
      case SceneState.MainMenu: {
        /*** Create Step State ***/
        if(_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        if(_interactable.enabled) _interactable.enabled             = false;
        
        break;
      }
      case SceneState.CreateTutorial: {
        /*** Create Step State ***/
        if(_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        if(_interactable.enabled) _interactable.enabled             = false;
        
        break;
      }
      case SceneState.CreateStep: {
        /*** Create Step State ***/
        if(!_speechInputHandler.enabled) _speechInputHandler.enabled = true;
        if(!_interactable.enabled) _interactable.enabled             = true;
        
        break;
      }
      case SceneState.StepRecording: {
        /*** Create Step State ***/
        if(_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        if(_interactable.enabled) _interactable.enabled             = false;
        
        break;
      }
      case SceneState.StepPlaying: {
        /*** Create Step State ***/
        if(_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        if(_interactable.enabled) _interactable.enabled             = false;
        
        break;
      }
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
   *
   * TODO: Replace this with an interaction handler with `global` mode.
   */
  public void OnPointerClicked(MixedRealityPointerEventData eventData) {
    // If we are not in the Idle State, don't do anything.
    /*if (SceneController.State != SceneState.CreateStep) {
      Debug.Log("OnPointerClicked(): Not in Idle State"); 
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
    }*/
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
  private void CreateStep() {
    Debug.Log("CreateStep()");
    
    // Get the primary pointer location
    Vector3    position = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.Details.Point;
    Quaternion rotation = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Rotation;
    Pose       pose     = new Pose(position, rotation);
    
    // Create a step in the TutorialSore
    StepDetails stepDetails = SceneController.TutorialStore.AddStep(pose);

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
    // FIXME:
    //state = State.StepRecording;
    //Debug.Log("In State: " + state);
    
    /*** Start Recording ***/
    // Start video recording (pass along the filename)
    CameraProvider.StartRecording(stepDetails.name);
    // Start a dictation recording
    _dictationHandler.StartRecording(); 
  }
  #endregion
  
  #region Public Methods
  
  /** When saying "Mark" */
  public void OnVoiceCommandMark() {
    if (SceneController.State != SceneState.CreateStep) return; // Only allow this in the CreateStep state.
    if(ClickedAGameObject() != null) return; // Don't allow this if the user has clicked on a game object.
    
    Debug.Log("VoiceCommandMark()");
    
    // TODO: This function will find the pose at the CurrentPointerTarget
    // CreateStep();
  }
  
  private GameObject ClickedAGameObject() {
    return CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.CurrentPointerTarget;
  }
  
  /** When "Air Click"ing */
  public void AirClickMark() {
    if (SceneController.State != SceneState.CreateStep) return; // Only allow this in the CreateStep state.
    if (ClickedAGameObject() != null) { // Pass click event to child so that children elements can be clicked.
      Interactable clickedGoInteractable = ClickedAGameObject().GetComponentInParent<Interactable>();
      if (clickedGoInteractable != null) clickedGoInteractable.OnClick.Invoke();
      return;
    }

    Debug.Log("AirClickMark()");

    // Get the pose for the primary pointer when saying "Mark".
    Vector3    position = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.Details.Point;
    Quaternion rotation = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Rotation;
    Pose       pose     = new Pose(position, rotation);
  
    // Create a Step and start recording.
    // FIXME: 
    // CreateStep(pose);
  }
  
  /**
   * When the user is in `Recording` state, stop recording and save the video to the storage.
   */
  public void EndMarking() {
    if (SceneController.State != SceneState.StepRecording) {
      Debug.Log("End Marking(): ERROR - Not in Recording State");
      return;  
    }

    Debug.Log("End Marking()");
    
    /*** Stop Recording ***/
    // Stop video recording and save associate the video to the Step.
    SceneController.TutorialStore.UpdateLastStep("videoFilePath", CameraProvider.StopRecording());
    // Stop the dictation recording
    _dictationHandler.StopRecording();
    
    
    /*** Update State ***/
    // Once the recording has been stopped, change the state to `CreateStep`
    // FIXME:
    // state = State.CreateStep;
    // Debug.Log("In State: " + state);
  }
  
  /**
   * Instantiate ToolTips from store.
   * TODO: Move this to the Guidance scene.
   */
  public void LoadTutorial(int tutorialId) {
    Debug.Log("LoadTutorial(" + tutorialId + ")");
    
    // Find the tutorial matching the tutorialId
    Tutorial tutorial = SceneController.TutorialStore.GetTutorial(tutorialId);
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
    SceneController.TutorialStore.Reset();
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
    SceneController.TutorialStore.UpdateLastStep("transcript", transcript);
  }

  private void OnDictationError(string error) {
    Debug.LogError("OnDictationError: ERROR - " + error);
  }
  #endregion
}