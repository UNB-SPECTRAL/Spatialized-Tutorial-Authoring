using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour {
  public void OnRecordButtonPress() {
    SceneManager.LoadScene("Record");
  }

  public void OnReplayButtonPress() {
    Debug.Log("Replay Button Pressed");
  }
}