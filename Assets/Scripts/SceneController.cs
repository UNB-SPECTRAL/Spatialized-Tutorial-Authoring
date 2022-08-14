using System;
using UnityEngine;

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
    CreateStep,
    StepRecording,
    StepPlaying,
    /* Guidance */
    Guidance, // When choosing a tutorial to play
    TutorialViewing, // When viewing steps of a tutorial
    TutorialStepViewing, // When viewing a step video of a tutorial
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
      case SceneState.StepPlaying:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(true);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        break;
      case SceneState.StepRecording:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(true);

        tutorialList.SetActive(false);
        tutorialListBackButton.SetActive(false);
        break;
      case SceneState.Guidance:
        Debug.Log("State: Guidance");
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(true);
        tutorialListBackButton.SetActive(false);
        break;
      case SceneState.TutorialViewing:
        Debug.Log("State: TutorialViewing");
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
    State = SceneState.Guidance;
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
  public void OnTutorialButtonPress(Tutorial tutorial) {
    Debug.Log("OnTutorialButtonClick(" + tutorial.name + ")");
    ActionController.Instance.LoadTutorial(tutorial); // Load tutorial to the scene
    State = SceneState.TutorialViewing;
  }

  public void OnTutorialListBackButtonPress() {
    Debug.Log("OnTutorialBackButtonPress()");
    ActionController.Instance.RemoveSteps(); // Unload tutorial steps from the scene
    State = SceneState.Guidance;
  }
}