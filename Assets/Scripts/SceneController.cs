using System;
using Microsoft.MixedReality.Toolkit;
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
  
  /***** Scene State *****/
  public enum SceneState {
    MainMenu,
    CreateTutorial,
    CreateStep,
    StepRecording,
    StepViewing
  };
  [HideInInspector]
  public SceneState state;
  
  /***** Static Reference *****/
  public static SceneController Instance;

  /*** Unity Methods ***/
  void Awake() {
    if(!Instance) Instance = this;
    else Destroy(gameObject);
  }
  
  void Start() {
    state = SceneState.MainMenu;
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
      case SceneState.StepViewing:
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
    // TODO: Create Tutorial (via other script)
    state = SceneState.CreateStep;
  }
  public void OnStopTutorialButtonPress() {
    Debug.Log("OnStopTutorialButtonPress()");
    state = SceneState.MainMenu;
  }
  public void OnStopStepRecordingButtonPress() {
    Debug.Log("OnStopStepRecordingButtonPress()");
    // TODO: Stop Recording Step (via other script)
    state = SceneState.MainMenu;
  }
  
  /*** Guidance Scene ***/
  // TODO: Add guidance button handlers
}
