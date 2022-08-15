using UnityEngine;
using UnityEngine.Video;
using Tutorial = TutorialStore.Tutorial;

/** Handler for the applications menus and buttons */
public class SceneController : MonoBehaviour {
  /***** Unity Editor *****/
  /*** Main Menu Scene ***/
  [Header("Main Menu Scene")] 
  public GameObject mainMenu;

  /*** Authoring Scene ***/
  [Header("Authoring Scene")] 
  public GameObject createTutorialButton;
  public GameObject stopTutorialButton;
  public GameObject stopStepRecordingButton;
  
  /*** Guidance Scene ***/
  [Header("Guidance Scene")] 
  public GameObject tutorialList;
  public GameObject tutorialListBackButton;

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
    ViewTutorials,    // When choosing a tutorial to view its steps
    ViewSteps,        // When viewing steps of a tutorial
    ViewStepPlaying,  // When viewing a step video of a tutorial
  };

  [HideInInspector]
  private SceneState state;

  [HideInInspector]
  public TutorialStore tutorialStore;

  /***** Static Reference *****/
  public static SceneController Instance;

  public static SceneState State {
    get => Instance.state;
    set {
    Instance.state = value;
    Instance.UpdateState(value);
    }
  }

  public static TutorialStore TutorialStore => Instance.tutorialStore;

  /*** Unity Methods ***/
  void Awake() {
    if (!Instance) Instance = this;
    else Destroy(gameObject);
  }

  void Start() {
    State         = SceneState.MainMenu;
    tutorialStore = TutorialStore.Load();
  }
  
  /*** Private Methods ***/
  void UpdateState(SceneState state) {
    switch (state) {
      case SceneState.MainMenu:
        mainMenu.SetActive(true);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        break;
      case SceneState.CreateTutorial:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(true);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        break;
      case SceneState.CreateStep:
      case SceneState.CreateStepPlaying:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(true);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        break;
      case SceneState.CreateStepRecording:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(true);

        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        break;
      case SceneState.ViewTutorials:
        Debug.Log("State: " + state);
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(true);
        tutorialListBackButton.SetActive(false);
        break;
      case SceneState.ViewSteps:
      case SceneState.ViewStepPlaying:
        Debug.Log("State: " + state);
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(true);
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

  public void OnStopTutorialButtonPress() {
    Debug.Log("OnStopTutorialButtonPress()");
    ActionController.Instance.RemoveSteps(); // Hide steps when returning to main menu.
    State = SceneState.MainMenu;
  }

  public void OnStopStepRecordingButtonPress() {
    Debug.Log("OnStopStepRecordingButtonPress()");
    ActionController.Instance.EndMarking(); // End recording
    State = SceneState.CreateStep;
  }

  /*** Guidance Scene ***/
  public void OnTutorialListButtonPress(Tutorial tutorial) {
    Debug.Log("OnTutorialListButtonClick(" + tutorial.name + ")");
    ActionController.Instance.LoadTutorial(tutorial); // Load tutorial to the scene
    State = SceneState.ViewSteps;
  }

  public void OnTutorialDeleteButtonPress(Tutorial tutorial) {
    Debug.Log("OnTutorialDeleteButtonPress(" + tutorial.name + ")");
    TutorialStore.DeleteTutorial(tutorial.id); // Delete tutorial from the store
    State = SceneState.ViewTutorials;
  }
  
  public void OnTutorialListBackButtonPress() {
    Debug.Log("OnTutorialListBacButtonClick()");
    State = SceneState.MainMenu;
  }

  public void OnViewStepsBackButtonPress() {
    Debug.Log("OnTutorialBackButtonPress()");
    ActionController.Instance.RemoveSteps(); // Unload tutorial steps from the scene
    State = SceneState.ViewTutorials;
  }
  
  /***** Static Helpers *****/
  /**
   * A step can only be clicked in the following cases:
   * - In the "Create Step" state
   * - In the "Create Step Playing" state while the step video is playing.
   * - In the "View Tutorial" state (since we can view a step recording)
   * - In the "View Step Playing" state while the step video is playing.
   */
  public static bool CanClickStep(VideoPlayer videoPlayer) {
    if (State == SceneState.CreateStep || State == SceneState.ViewSteps) return true;
    if(State == SceneState.CreateStepPlaying && videoPlayer.isPlaying) return true;
    if (State == SceneState.ViewStepPlaying && videoPlayer.isPlaying) return true;
    return false;
  }
  
  /**
   * When a step video is being played, update the state accordingly:
   * - If in "Create Step", then "Create Step Playing"
   * - if in "View Tutorial", then "View Step Playing"
   */
  public static void PlayingStepVideo() {
    if (State == SceneState.CreateStep) State = SceneState.CreateStepPlaying;
    if (State == SceneState.ViewSteps)   State = SceneState.ViewStepPlaying;
    else Debug.LogError("PlayingStepVideo() called in an invalid state: " + State);
  }
  
  /**
   * When a step video is being paused, update the state accordingly:
   * - If in "Create Step Playing", then "Create Step"
   * - if in "View Step Playing", then "View Tutorial"
   */
  public static void PausingOrStopStepVideo() {
    if (State == SceneState.CreateStepPlaying) State = SceneState.CreateStep;
    if (State == SceneState.ViewStepPlaying)   State = SceneState.ViewSteps;
    else Debug.LogError("PlayingStepVideo() called in an invalid state: " + State);
  }
}