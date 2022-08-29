using System;
using System.Collections.Generic;
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
 * Given a state, enable/disable actions.
 * 
 * This controller is the workhorse of the application handling all actions
 * that are performed based on the state the the SceneController determines
 * we are in.
 *
 * The entry point of the application is the SceneController.
 */
public class ActionController : MonoBehaviour {
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
  private static ActionController _instance;
  public static ActionController Instance => _instance;

  public static TutorialStore TutorialStore => SceneController.TutorialStore;
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

    _dictationHandler = GetComponent<DictationHandler>();

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

  public void UpdateState(SceneState state) {
    switch (state) {
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
      case SceneState.StartStepRecording: {
        /*** Start Step Recording State ***/
        // TODO: Do we still want to listen to mark?
        if (_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        // Keep this active so that we can keep calling `createStep()` to update
        // the step's location.
        if (!_interactable.enabled) _interactable.enabled             = true;

        break;
      }
      // TODO: Add new state
      case SceneState.CreateStepRecording: {
        /*** Create Step State ***/
        if (_speechInputHandler.enabled) _speechInputHandler.enabled = false;
        if (_interactable.enabled) _interactable.enabled             = false;

        break;
      }
      case SceneState.CreateStepPlaying: {
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
    // Only allow this function to be called when we are in the CreateStep or
    // StartStepRecording state.
    if (SceneController.State != SceneState.CreateStep && SceneController.State != SceneState.StartStepRecording) {
      Debug.LogError("CreateStep() can only be called when in the CreateStep or StartStepRecording state.");
      return;
    }
    Debug.Log("CreateStep()");

    // Get the primary pointer location
    Vector3    position = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.Details.Point;
    Quaternion rotation = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Rotation;
    Pose       pose     = new Pose(position, rotation);

    StepDetails stepDetails;
    if (SceneController.State == SceneState.CreateStep) {
      // Create a new step in the TutorialStore if we are in the CreateStep state.
      stepDetails = TutorialStore.CreateStep(pose);
      // And instantiate the step in the scene
      InstantiateStep(stepDetails);
    }
    else {
      // Otherwise, update the last step location
      stepDetails = TutorialStore.UpdateLastStep("globalPose", pose);
      // TODO: See if the memory reference can just update the position...
    }

    // Update the state to indicate that we are in the StartStepRecording state.
    SceneController.State = SceneState.StartStepRecording;
    Debug.Log("State: " + SceneController.State);
  }

  /** Given StepDetails, instantiate a Step in the scene and return it's reference. */
  private GameObject InstantiateStep(StepDetails stepDetails) {
    // Instantiate the Step GameObject with the correct parent.
    var toolTipGo = Instantiate(stepPrefab, stepPrefabParent);

    // Configure the ToolTip
    toolTipGo.GetComponent<StepController>().stepDetails = stepDetails;

    // Add world locking
    // TODO: Is this needed anymore?
    toolTipGo.AddComponent<ToggleWorldAnchor>().AlwaysLock = true;
    
    return toolTipGo;
  }

  /** Given a ToolTipDetail, start recording a video for it */
  /**
   * Once a ToolTip has been instantiated, this method is called to start the recording process.
   * The recording process includes:
   * - Changing the state to `Recording`
   * - Starting a video (camera/microphone) recording
   * - Starting a speech to text (via Unity Dictation) recording
   */
  public void StartRecording() {
    // When recording, change the state
    SceneController.State = SceneState.CreateStepRecording;
    
    // Get the last step to associate the recording to.
    StepDetails lastStep = TutorialStore.LastStep();

    // Start video recording (pass along the video file name)
    Debug.Log("Video Recording: Starting");
    CameraProvider.StartRecording(lastStep.id);

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
    if (ClickedInteractable() != null) return; // Don't allow if clicked on a game object.
    if (ClickedStep() != null) return; // Don't allow if clicked on a Step.

    Debug.Log("VoiceCommandMark()");
    
    // Create a new Step and start recording
    CreateStep();
    StartRecording();
  }

  /** When "Air Click"ing */
  public void OnAirClickMark() {
    if (SceneController.State != SceneState.CreateStep && SceneController.State != SceneState.StartStepRecording) {
      Debug.Log("OnAirClickMark() called in incorrect state: " + SceneController.State);
      return; // Only allow this in the CreateStep state.
    }

    if (ClickedInteractable() != null) { // Pass event to interactable if exist
      Interactable clickedGoInteractable = ClickedInteractable();
      clickedGoInteractable.OnClick.Invoke();
      return;
    }
    
    if (ClickedStep() != null) { // Pass event to Step if exist
      StepController stepController = ClickedStep();
      stepController.OnClick();
      return;
    }

    Debug.Log("OnAirClickMark()");

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
   * Deletes the last step from the TutorialStore if there is no video associated
   * with it.
   *
   * This is only used when ending a tutorial through the Authoring flow where
   * a step could be placed but not recorded.
   */
  public void DeleteStepWithNoVideo() {
    if (SceneController.State != SceneState.CreateStep && SceneController.State != SceneState.StartStepRecording) {
      Debug.LogError("DeleteStepWithNoVideo(): ERROR - Not in correct state: " + SceneController.State);
      return;
    }
    
    Debug.Log("DeleteStepWithNoVideo");

    // Get the last step of the latest tutorial.
    StepDetails lastStep = TutorialStore.LastStep();
    
    // If there is no video associated with the step, delete the step.
    if (lastStep.videoFilePath == null) {
      TutorialStore.DeleteStep(lastStep.id);
    }
    
    // Otherwise, do nothing.
  }

  /**
   * Delete a Step from the Unity Scene and the TutorialStore.
   *
   * If this step is not the last step, then update all subsequent steps:
   * - ID
   * - Name
   * - VideoFilePath
   *
   * TODO: Test this method
   */
  public void DeleteStep(StepDetails stepDetails) {
    // Validate that we are in the right state to delete a step
    if (SceneController.State != SceneState.CreateStep && SceneController.State != SceneState.StartStepRecording) {
      Debug.LogError("DeleteStep(): ERROR - Not in correct state: " + SceneController.State);
      return;
    }

    Debug.Log("DeleteStep(Step " + stepDetails.id + ")");

    // Remove all steps shown in the Unity scene
    Tutorial tutorial = TutorialStore.FindTutorialForStep(stepDetails.id);
    foreach (var step in tutorial.steps) {
      GameObject stepToDestroy = GameObject.Find(step.id);
      if (stepToDestroy != null) {
        StepController stepToDestroyStepController = stepToDestroy.GetComponent<StepController>();
        stepToDestroyStepController.isBeingDestroyed = true; // So that we can know that it will be destroyed within the game loop.
        Destroy(stepToDestroy);
      }
    }

    // Delete step from TutorialStore
    TutorialStore.DeleteStep(stepDetails.id);

    // Instantiate all Steps to the Unity Scene (without the deleted one)
    LoadTutorial(tutorial);
  }

  /** Render tutorial steps */
  public List<GameObject> LoadTutorial(Tutorial tutorial) {
    Debug.Log("LoadTutorial(" + tutorial.name + ")");
    
    // Create a GameObject list to store the reference of all instantiated steps.
    List<GameObject> stepGameObjects = new List<GameObject>();

    // For each step in the tutorial, instantiate it to the scene.
    foreach (StepDetails stepDetails in tutorial.steps) {
      Debug.Log("Found Step " + stepDetails.id + " in store");

      // Before instantiation, check if the step is already in the scene.
      GameObject existingStep = GameObject.Find(stepDetails.name);

      // If the Step does exist
      if (existingStep != null) {
        StepController stepToDestroyStepController = existingStep.GetComponent<StepController>();
        // And it will not be destroyed, then continue
        if (stepToDestroyStepController != null && !stepToDestroyStepController.isBeingDestroyed) {
          Debug.Log(stepDetails.id + " already exists in scene. Skipping");
          continue;
        }
      }

      // Otherwise, instantiate it.
      // Instantiate the tooltip using the given prefab and set its parent to the playspace.
      stepGameObjects.Add(InstantiateStep(stepDetails));
    }
    
    // Return the sorted game objects (first element is the first step)
    stepGameObjects.Sort((a, b) => {
      var aId = a.GetComponent<StepController>().stepDetails.id;
      var bId = b.GetComponent<StepController>().stepDetails.id;
      return string.Compare(aId, bId, StringComparison.Ordinal);
    });
    return stepGameObjects;
  }

  /** Remove Step GameObject from scene. */
  public void RemoveStepsFromScene() {
    Debug.Log("RemoveStepsFromScene()");
    GameObject[] steps = GameObject.FindGameObjectsWithTag("Step");
    foreach (GameObject tooltip in steps) {
      Destroy(tooltip);
    }
  }

  /**
   * An easy way to delete all the data in the streaming assets folder
   */
  public void ResetTutorials() {
    Debug.Log("ResetTutorials()");
    
    // Delete all tutorials from the TutorialStore
    foreach(var tutorial in TutorialStore.tutorials.ToList()) {
      TutorialStore.DeleteTutorial(tutorial.id);
    }

    // Delete all files in the Streaming Assets directory.
    string[] files = Directory.GetFiles(Application.streamingAssetsPath);
    foreach (string file in files) { File.Delete(file); }

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
  private Interactable ClickedInteractable() {
    GameObject clickedGo = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.CurrentPointerTarget;

    // Check if interactable
    if(clickedGo != null && clickedGo.GetComponent<Interactable>() != null) {
      return clickedGo.GetComponent<Interactable>();
    }
    
    // Check if parent is interactable
    if(clickedGo != null && clickedGo.GetComponentInParent<Interactable>() != null) {
      return clickedGo.transform.parent.GetComponent<Interactable>();
    }

    return null;
  }

  private StepController ClickedStep() {
    GameObject clickedGo = CoreServices.InputSystem.FocusProvider.PrimaryPointer.Result.CurrentPointerTarget;

    if (clickedGo != null && clickedGo.GetComponentInParent<StepController>() != null) {
      return clickedGo.GetComponentInParent<StepController>();
    }

    return null;
  }
}