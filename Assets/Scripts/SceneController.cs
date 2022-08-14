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
  // [Header("Guidance Scene")]
  // TODO: Add guidance buttons.
  
  /***** Public Variables *****/
  public enum SceneState {
    MainMenu,
    CreateTutorial,
    CreateStep,
    StepRecording,
    StepPlaying
  };
  [HideInInspector]
  public SceneState state;
  
  [HideInInspector]
  public TutorialStore tutorialStore;
  
  /***** Static Reference *****/
  private static SceneController _instance;
  public static SceneState State {
    get => _instance.state;
    set => _instance.state = value;
  }
  public static TutorialStore TutorialStore => _instance.tutorialStore;

  /*** Unity Methods ***/
  void Awake() {
    if(!_instance) _instance = this;
    else Destroy(gameObject);
  }
  
  void Start() {
    state          = SceneState.MainMenu;
    tutorialStore = TutorialStore.Load();
  }
  
  void Update() {
    switch (state) {
      case SceneState.MainMenu:
        mainMenu.SetActive(true);
        
        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);
        
        // TODO: Add guidance buttons
        break;
      case SceneState.CreateTutorial:
        mainMenu.SetActive(false);
        
        createTutorialButton.SetActive(true);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(false);
        
        // TODO: Add guidance buttons
        break;
      case SceneState.CreateStep:
      case SceneState.StepPlaying:
        mainMenu.SetActive(false);
        
        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(true);
        stopStepRecordingButton.SetActive(false);
        
        // TODO: Add guidance buttons
        break;
      case SceneState.StepRecording:
        mainMenu.SetActive(false);
        
        createTutorialButton.SetActive(false);
        stopTutorialButton.SetActive(false);
        stopStepRecordingButton.SetActive(true);
        
        // TODO: Add guidance buttons
        break;
    }
  }
  
  /***** Button Handlers *****/
  /*** Main Menu Scene ***/
  public void OnAuthoringButtonPress() {
    Debug.Log("OnAuthoringButtonPress()");
    state = SceneState.CreateTutorial;
  }

  public void OnGuidanceButtonPress() {
    Debug.Log("OnGuidanceButtonPress()");
    throw new NotImplementedException();
  }
  
  /*** Authoring Scene ***/
  public void OnCreateTutorialButtonPress() {
    Debug.Log("OnCreateTutorialButtonPress()");
    tutorialStore.CreateTutorial();
    state = SceneState.CreateStep;
  }
  public void OnStopTutorialButtonPress() {
    Debug.Log("OnStopTutorialButtonPress()");
    state = SceneState.MainMenu;
  }
  public void OnStopStepRecordingButtonPress() {
    Debug.Log("OnStopStepRecordingButtonPress()");
    // TODO: Stop Recording Step (via other script)
    state = SceneState.CreateStep;
  }
  
  /*** Guidance Scene ***/
  // TODO: Add guidance button handlers
}
