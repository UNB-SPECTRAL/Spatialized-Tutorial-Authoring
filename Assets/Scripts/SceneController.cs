using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using Microsoft.MixedReality.WorldLocking.Core;
using UnityEngine;
using Tutorial = TutorialStore.Tutorial;
using StepDetails = TutorialStore.Tutorial.StepDetails;

/** Handler for the applications menus and buttons */
public class SceneController : MonoBehaviour {
  /***** Unity Editor *****/
  /*** Main Menu Scene ***/
  [Header("Main Menu Scene")]
  public GameObject mainMenu;

  /*** Authoring Scene ***/
  [Header("Authoring Scene")]
  public GameObject createTutorialButton;
  public GameObject stepList;
  public GameObject stopStepRecordingButton;

  /*** Guidance Scene ***/
  [Header("Guidance Scene")]
  public GameObject tutorialList;
  public GameObject tutorialListBackButton;
  public GameObject chevron; // Used for indicating the next step.

  /***** Public Variables *****/
  public enum SceneState {
    /* Main Menu */
    MainMenu,

    /* Authoring */
    CreateTutorial,
    CreateStep, // When we can "Mark" a step, view a step recording or end the tutorial.
    CreateStepRecording,
    CreateStepPlaying,

    /* Guidance */
    ViewTutorials, // When choosing a tutorial to view its steps
    ViewSteps, // When viewing steps of a tutorial
    ViewStepPlaying, // When viewing a step video of a tutorial
  };

  [HideInInspector]
  public TutorialStore tutorialStore;

  /***** Public Static Reference *****/
  public static SceneController Instance;

  public static SceneState State {
    get => Instance._state;
    set {
      Instance._state = value;
      Instance.UpdateState(value);
    }
  }

  public static TutorialStore TutorialStore => Instance.tutorialStore;

  /***** Private Variables *****/
  private SceneState       _state; // Holds the current state of the scene.
  private List<GameObject> stepGameObjects; // Holds a reference to instantiated step game objects.

  // Holds the StepController which is currently playing a video. This is used
  // to quickly reference a StepController without needing to search for it.
  private StepController _activeStepController;

  /*** Unity Methods ***/
  void Awake() {
    if (!Instance) Instance = this;
    else Destroy(gameObject);
  }

  void Start() {
    State         = SceneState.MainMenu;
    tutorialStore = TutorialStore.Load();
    
    /***** Initialization *****/
    // Set the Gaze Pointer to always be on (since it is not when hand cursors
    // are enabled).
    PointerUtils.SetGazePointerBehavior(PointerBehavior.AlwaysOn);
    
    // Disable the HoloLens profiler
    CoreServices.DiagnosticsSystem.ShowProfiler = false;
  }

  /*** Private Methods ***/
  void UpdateState(SceneState state) {
    switch (state) {
      case SceneState.MainMenu:
        mainMenu.SetActive(true);
        
        createTutorialButton.SetActive(false);
        stepList.SetActive(false);
        stopStepRecordingButton.SetActive(false);
        
        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        chevron.SetActive(false);
        break;
      case SceneState.CreateTutorial:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(true);
        stepList.SetActive(false);
        stopStepRecordingButton.SetActive(false);
 
        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        chevron.SetActive(false);
        break;
      case SceneState.CreateStep:
      case SceneState.CreateStepPlaying:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stepList.SetActive(true);
        stopStepRecordingButton.SetActive(false);
        
        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        chevron.SetActive(false);
        break;
      case SceneState.CreateStepRecording:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stepList.SetActive(false);
        stopStepRecordingButton.SetActive(true);

        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        chevron.SetActive(false);
        break;
      case SceneState.ViewTutorials:
        Debug.Log("State: " + state);
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stepList.SetActive(false);
        stopStepRecordingButton.SetActive(false);
        
        tutorialList.SetActive(true);
        tutorialListBackButton.SetActive(false);
        chevron.SetActive(false);
        break;
      case SceneState.ViewSteps:
      case SceneState.ViewStepPlaying:
        Debug.Log("State: " + state);
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stepList.SetActive(false);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(true);
        chevron.SetActive(true); UpdateChevron(); // Enable and update the chevron directional target.
        break;
    }
  }

  /***** Button Handlers *****/
  /*** Main Menu Scene ***/
  public void OnAuthoringButtonPress() {
    Debug.Log("OnAuthoringButtonPress()");
    State = SceneState.CreateTutorial;
  }

  public void OnGuidanceButtonPress() {
    Debug.Log("OnGuidanceButtonPress()");
    State = SceneState.ViewTutorials;
  }

  public void OnResetButtonPress() {
    Debug.Log("OnResetButtonPress()");
    ActionController.Instance.ResetTutorials(); // Delete all the data
    State = SceneState.MainMenu;
  }

  /*** Authoring Scene ***/
  public void OnCreateTutorialButtonPress() {
    Debug.Log("OnCreateTutorialButtonPress()");
    tutorialStore.CreateTutorial();
    State = SceneState.CreateStep;
  }

  public void OnStopTutorialButtonPress() {
    Debug.Log("OnStopTutorialButtonPress()");
    ActionController.Instance.RemoveStepsFromScene(); // Hide steps when returning to main menu.
    State = SceneState.MainMenu;
  }

  public void OnStopStepRecordingButtonPress() {
    Debug.Log("OnStopStepRecordingButtonPress()");
    ActionController.Instance.EndMarking(); // End recording
    State = SceneState.CreateStep;
  }

  public void OnDeleteStepButtonPress(StepDetails stepDetails) {
    Debug.Log("OnDeleteStepButtonPress()");
    ActionController.Instance.DeleteStep(stepDetails); // Delete a step
    State = SceneState.CreateStep;
  }
  
  /*** Guidance Scene ***/
  public void OnTutorialListButtonPress(Tutorial tutorial) {
    Debug.Log("OnTutorialListButtonClick(" + tutorial.name + ")");
    stepGameObjects = ActionController.Instance.LoadTutorial(tutorial); // Load tutorial to the scene
    State = SceneState.ViewSteps;
  }

  public void OnTutorialDeleteButtonPress(Tutorial tutorial) {
    Debug.Log("OnTutorialDeleteButtonPress(" + tutorial.name + ")");
    TutorialStore.DeleteTutorial(tutorial.id); // Delete tutorial from the store
    State = SceneState.ViewTutorials;
  }

  public void OnTutorialListBackButtonPress() {
    Debug.Log("OnTutorialListBackButtonClick()");
    State = SceneState.MainMenu;
  }

  public void OnViewStepsBackButtonPress() {
    Debug.Log("OnViewStepsBackButtonPress()");
    ActionController.Instance.RemoveStepsFromScene(); // Unload tutorial steps from the scene
    State = SceneState.ViewTutorials;
  }

  /***** Static Helpers *****/
  /**
   * A step can only be clicked in the following cases:
   * - In the "Create Step" state.
   * - In the "Create Step Playing" state.
   * - In the "View Steps" state (since we can view a step recording).
   * - In the "View Step Playing" state.
   */
  public static bool CanClickStep() {
    return State == SceneState.CreateStep
           || State == SceneState.CreateStepPlaying
           || State == SceneState.ViewSteps
           || State == SceneState.ViewStepPlaying;
  }

  /**
   * When a StepController is about to play a video, it calls this method to
   * notify the SceneController. This does the following:
   * - Pauses any active videos.
   * - Update the activeStepController reference to quickly pause the video
   * if one is playing while another StepController is pressed.
   * - Update the SceneController step.
   */
  public static void PlayStepVideo(StepController stepController) {
    // Update the Chevron to point to the next step
    UpdateChevron();
    
    // Check if there is an existing stepController that is/was playing a video.
    if (Instance._activeStepController != null) {
      // Pause the video (it could already be paused)
      // Not calling PauseOrStopVideo() because we don't want to update the
      // scene state and then update it back.
      Instance._activeStepController.videoPlayer.Pause();
    }

    // Replace the active stepController with the new one.
    Instance._activeStepController = stepController;

    // Update the state if needed (since we could be pausing an active video and
    // would already be in these states).
    if (State == SceneState.CreateStep) State     = SceneState.CreateStepPlaying;
    else if (State == SceneState.ViewSteps) State = SceneState.ViewStepPlaying;
  }

  /**
   * When a StepController is about to pause/stop playing a video, it calls this
   * method to do the following:
   * - Update the activeStepController reference to null.
   * - Update the SceneController state.
   */
  public static void PauseOrStopStepVideo() {
    // Remove the reference to the active stepController since the StepController
    // who has called this function is not longer playing a video and thus does
    // not need to be referenced. Note that we pause videos in this class via
    // referencing the `videoPlayer.Pause()` and therefore do not call this 
    // function.
    Instance._activeStepController = null;

    // Update the state accordingly.
    if (State == SceneState.CreateStepPlaying) State    = SceneState.CreateStep;
    else if (State == SceneState.ViewStepPlaying) State = SceneState.ViewSteps;
    else Debug.LogError("PauseOrStopStepVideo() called in an invalid state: " + State);
  }
  
  /** Updates the Chevron directional target */
  static void UpdateChevron() {
    // Only update the Chevron if it is active.
    if (Instance.chevron.activeSelf == false) return;
    
    // Before enabling, set the `directionalTarget` to the first un-viewed step
    foreach (var stepGameObject in Instance.stepGameObjects) {
      if (stepGameObject.GetComponent<StepController>().isViewed) continue;
      Instance.chevron.GetComponent<DirectionalIndicator>().DirectionalTarget = stepGameObject.transform;
    }
  }
}