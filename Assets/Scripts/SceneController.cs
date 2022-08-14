using System;
using UnityEngine;

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
    Guidance
  };

  [HideInInspector]
  private SceneState state;

  [HideInInspector]
  public TutorialStore tutorialStore;

  /***** Static Reference *****/
  private static SceneController _instance;

  public static SceneState State {
    get => _instance.state;
    set {
    _instance.state = value;
    _instance.UpdateState(value);
    }
  }

  public static TutorialStore TutorialStore => _instance.tutorialStore;

  /*** Unity Methods ***/
  void Awake() {
    if (!_instance) _instance = this;
    else Destroy(gameObject);
  }

  void Start() {
    state         = SceneState.MainMenu;
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
        break;
      case SceneState.CreateTutorial:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(true);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(false);
        break;
      case SceneState.CreateStep:
      case SceneState.StepPlaying:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(true);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(false);
        break;
      case SceneState.StepRecording:
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(true);

        tutorialList.SetActive(false);
        break;
      case SceneState.Guidance:
        Debug.Log("State: Guidance");
        mainMenu.SetActive(false);

        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);

        tutorialList.SetActive(true);
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
  // TODO: Add guidance button handlers
}