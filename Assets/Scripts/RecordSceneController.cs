using System.IO;
using System.Linq;
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
public class RecordSceneController : MonoBehaviour {
  
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

  #region Private Variables
  /*** CreateStep State ***/
  private SpeechInputHandler _speechInputHandler;

  private Interactable _interactable;

  /*** StepRecording State ***/
  private DictationHandler _dictationHandler;
  #endregion

  #region Static References
  private static RecordSceneController _instance;
  public static RecordSceneController Instance => _instance;
  #endregion

  #region Unity Methods
  /**
   * In the Unity Awake method, we do the following actions:
   * - Set the static reference to the instantiated RecordSceneController if none exists. Otherwise, destroy this GameObject.
   * - Load the ToolTipStore from storage.
   */
  void Awake() {
    if (_instance == null) _instance = this;
    else Destroy(gameObject);
  }

  void Start() {
    /*** Component References ***/
    _speechInputHandler = GetComponent<SpeechInputHandler>();
    _interactable       = GetComponent<Interactable>();

    _dictationHandler   = GetComponent<DictationHandler>();

    /*** Dictation Handler Event Setup ***/
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
        if (_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        if (_interactable.enabled) _interactable.enabled             = false;

        break;
      }
      case SceneState.CreateTutorial: {
        /*** Create Step State ***/
        if (_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        if (_interactable.enabled) _interactable.enabled             = false;

        break;
      }
      case SceneState.CreateStep: {
        /*** Create Step State ***/
        if (!_speechInputHandler.enabled) _speechInputHandler.enabled = true;
        if (!_interactable.enabled) _interactable.enabled             = true;

        break;
      }
      case SceneState.StepRecording: {
        /*** Create Step State ***/
        if (_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        if (_interactable.enabled) _interactable.enabled             = false;

        break;
      }
      case SceneState.StepPlaying: {
        /*** Create Step State ***/
        if (_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        if (_interactable.enabled) _interactable.enabled             = false;

        break;
      }
    }
  }
  #endregion

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
    // When recording, change the state
    SceneController.State = SceneState.StepRecording;

    // Start video recording (pass along the video file name)
    Debug.Log("Video Recording: Starting");
    int    tutorialId    = SceneController.TutorialStore.tutorials.Last().id;
    string videoFileName = "tutorial " + tutorialId + " " + stepDetails.name;
    CameraProvider.StartRecording(videoFileName);

    // Start speech-to-text recording
    // TODO: We should use the Unity Dictation API since we can dispose and release the resources.
    // TODO: Or disable this component and see if it releases the resources like the AudioClips.
    Debug.Log("Speech-To-Text: Starting");
    _dictationHandler.StartRecording();
  }
  #endregion

  #region Public Methods
  /*** Create Step State ***/
  /** When saying "Mark" */
  public void OnVoiceCommandMark() {
    if (SceneController.State != SceneState.CreateStep) return; // Only allow this in the CreateStep state.
    if (ClickedButton() != null) return; // Don't allow if clicked on a game object.
    if (ClickedStep() != null) return; // Don't allow if clicked on a Step.

    Debug.Log("VoiceCommandMark()");

    CreateStep();
  }

  /** When "Air Click"ing */
  public void AirClickMark() {
    if (SceneController.State != SceneState.CreateStep) return; // Only allow this in the CreateStep state.
    if (ClickedStep() != null) { // Don't create step on another step. Pass the click along
      StepController stepController = ClickedStep();
      stepController.OnClick();
      return;
    }
    if (ClickedButton() != null) { // Pass click event to child so that children elements can be clicked.
      Interactable clickedGoInteractable = ClickedButton().GetComponentInParent<Interactable>();
      if (clickedGoInteractable != null) clickedGoInteractable.OnClick.Invoke();
      return;
    }

    Debug.Log("AirClickMark()");

    CreateStep();
  }

  /**
   * When the user is in `Recording` state, stop recording and save the video to the storage.
   */
  public void EndMarking() {
    Debug.Log("End Marking()");

    /*** Stop Recording ***/
    // Stop video recording and save associate the video to the Step.
    Debug.Log("Video Recording: Stopping");
    SceneController.TutorialStore.UpdateLastStep("videoFilePath", CameraProvider.StopRecording());

    // Stop the dictation recording
    Debug.Log("Speech-To-Text: Stopping");
    _dictationHandler.StopRecording();

    /*** Update State ***/
    // Once the recording has been stopped, change the state to `CreateStep`
    SceneController.State = SceneState.CreateStep;
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

  /** Remove Step GameObject from scene. */
  public void RemoveSteps() {
    Debug.Log("RemoveSteps()");
    GameObject[] steps = GameObject.FindGameObjectsWithTag("Step");
    foreach (GameObject tooltip in steps) {
      Destroy(tooltip);
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
    RemoveSteps();

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

  /***** Private Methods *****/
  // TODO: Maybe name this "ClickedInteractable" and pass along the click.
  private GameObject ClickedButton() {
    GameObject clickedGo = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.CurrentPointerTarget;
    
    if(clickedGo != null && clickedGo.CompareTag("Button")) {
      return clickedGo;
    }

    return null;
  }
  
  private StepController ClickedStep() {
    GameObject clickedGo = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.CurrentPointerTarget;
    
    if( clickedGo != null && clickedGo.GetComponentInParent<StepController>() != null) {
      return clickedGo.GetComponentInParent<StepController>();
    }

    return null;
  }
}