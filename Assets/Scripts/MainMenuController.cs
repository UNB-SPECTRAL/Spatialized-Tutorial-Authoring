using Microsoft.MixedReality.WorldLocking.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour {

  void Start() {
    Debug.Log("Enabling World Locking Toolkit");
    var settings = WorldLockingManager.GetInstance().Settings;
    settings.Enabled                           = true;
    WorldLockingManager.GetInstance().Settings = settings;
  }
  
  public void OnAuthoringButtonPress() {
    Debug.Log("Disabling World Locking Toolkit");
    var settings = WorldLockingManager.GetInstance().Settings;
    settings.Enabled                           = false;
    WorldLockingManager.GetInstance().Settings = settings;
    
    SceneManager.LoadScene("Authoring");
  }

  public void OnGuidanceButtonPress() {
    Debug.Log("Disabling World Locking Toolkit");
    var settings = WorldLockingManager.GetInstance().Settings;
    settings.Enabled                           = false;
    WorldLockingManager.GetInstance().Settings = settings;
    
    SceneManager.LoadScene("Guidance");
  }
}