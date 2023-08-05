using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
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
  public GameObject confirmStepPositionButton;
  // @deprecated for experiment #3
  public GameObject stopStepRecordingButton;

  /*** Guidance Scene ***/
  [Header("Guidance Scene")]
  public GameObject tutorialList;
  public GameObject tutorialListBackButton;
  public GameObject chevron; // Used for indicating the next step.

  /***** Public Variables *****/
  // https://mermaid.live/edit#pako:eNqNksGKwjAQhl-lDHizlz32sCC67ElYtounXIZm1ECbSjpZkdJ3d2obq7YFcwiZf74Z_kxSQ1ZqggQqRqaNwYPDIv7_UDaStUVjt2R9FMef0dqRIH-eS2cwnwB2hs4hXSnbEYtFtPJ8FNEeOuW5z0PrlOkUygblBqSMjtvol7LS6Zded2wI3-V-crzMUeFyY0991Yz3sdl3zc0VPAzz2xuNNqNOeBr5_RHakmoOGG4V8jd86gWngdHYXsQpG90eFiyhIFeg0fLx6jangI9UkIJEjpr26HNWoGwjqD9pGcOXNuILkr04oyWguEwvNoOEnacA9f-3p5or3Rj0OA
  public enum SceneState {
    /* Main Menu */
    MainMenu,

    /* Authoring */
    CreateTutorial, // MainMenu -- Click "Authoring" --> CreateTutorial
    CreateStep,     // CreateTutorial -- Click "Start new Tutorial" --> CreateStep (Able to say "Mark" and "Tap To Place")
    ConfirmStepPosition, // CreateStep -- "Tap to place" --> StartRecording (Must click "Start Recording" to start recording)
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
      ActionController.Instance.UpdateState(value);
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
        confirmStepPositionButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);
        
        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        chevron.SetActive(false);
        break;
      case SceneState.CreateTutorial:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(true);
        stepList.SetActive(false);
        confirmStepPositionButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);
 
        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        chevron.SetActive(false);
        break;
      // TODO: Figure out what does the `CreateStepPlaying` state do?
      case SceneState.CreateStep:
      case SceneState.CreateStepPlaying:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stepList.SetActive(true);
        confirmStepPositionButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);
        
        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        chevron.SetActive(false);
        break;
      
      // After the user creates a step, they must confirm the position of the step.
      case SceneState.ConfirmStepPosition:
        mainMenu.SetActive(false);
        
        createTutorialButton.SetActive(false);
        stepList.SetActive(true);
        confirmStepPositionButton.SetActive(true);
        stopStepRecordingButton.SetActive(false);
        
        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        chevron.SetActive(false);
        break;
      
      // @deprecated for experiment #3
      case SceneState.CreateStepRecording:
        Debug.LogError("HoloTuts: SceneState.CreateStepRecording is deprecated.");
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stepList.SetActive(false);
        confirmStepPositionButton.SetActive(false);
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
        confirmStepPositionButton.SetActive(false);
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
        confirmStepPositionButton.SetActive(false);
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

  /*** Authoring Scene ***/
  public void OnCreateTutorialButtonPress() {
    Debug.Log("OnCreateTutorialButtonPress()");
    tutorialStore.CreateTutorial();
    State = SceneState.CreateStep;
  }

  /** Used by the StepList to complete a tutorial authoring */
  public void OnStopTutorialButtonPress() {
    Debug.Log("OnStopTutorialButtonPress()");
    // Delete the the last step if it does not have a video since it could have
    // been "tap to place" but not recorded.
    ActionController.Instance.DeleteStepWithNoVideo();
    // Hide steps when returning to main menu.
    ActionController.Instance.RemoveStepsFromScene(); 
    State = SceneState.MainMenu;
  }
    
  public void OnConfirmStepPositionButtonPress() {
    Debug.Log("HoloTuts: OnConfirmStepPositionButtonPress()");
    
    // Stop all video players before starting a new step recording since this
    // can cause issues with the recording.
    if (Instance._activeStepController != null) {
      Debug.Log("Video player is playing. Stopping video player.");
      // Pause the video (it could already be paused)
      // Not calling PauseOrStopVideo() because we don't want to update the
      // scene state and then update it back.
      Instance._activeStepController.videoPlayer.Pause();
    }
    
    // ActionController.Instance.StartRecording();
    
    // Update the SceneState to `CreateStep`.
    State = SceneState.CreateStep;
  }

  public void OnStopStepRecordingButtonPress() {
    Debug.Log("OnStopStepRecordingButtonPress()");
    
    ActionController.Instance.EndMarking(); // End recording
    stepList.GetComponent<StepListController>().OnEnable(); // Update the step list.
    
    State = SceneState.CreateStep;
  }

  public void OnDeleteStepButtonPress(StepDetails stepDetails) {
    Debug.Log("OnDeleteStepButtonPress()");
    
    ActionController.Instance.DeleteStepWithNoVideo(); // Delete the "Air Clicked" step
    ActionController.Instance.DeleteStep(stepDetails); // Delete a step
    stepList.GetComponent<StepListController>().OnEnable(); // Update the step list.
    
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
   * - In the "StartStepRecordinc" state.
   * - In the "View Steps" state (since we can view a step recording).
   * - In the "View Step Playing" state.
   */
  public static bool CanClickStep() {
    return State == SceneState.CreateStep
           || State == SceneState.CreateStepPlaying
           || State == SceneState.ConfirmStepPosition
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
    
    // Delete all partial markings
    ActionController.Instance.DeleteStepWithNoVideo();
    
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
    if (State == SceneState.CreateStep || State == SceneState.ConfirmStepPosition) State     = SceneState.CreateStepPlaying;
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
    // NOTE: Commenting this out since we want to go back to the ConfirmStepPosition
    // if (State == SceneState.CreateStepPlaying) State    = SceneState.CreateStep;
    if      (State == SceneState.CreateStepPlaying) State = SceneState.ConfirmStepPosition;
    else if (State == SceneState.ViewStepPlaying)   State = SceneState.ViewSteps;
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
      return;
    }
    
    // If there are no more steps to view, remove the directional target.
    Instance.chevron.GetComponent<DirectionalIndicator>().DirectionalTarget = null;
  }
}