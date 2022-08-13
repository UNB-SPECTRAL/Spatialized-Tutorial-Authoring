using UnityEngine;

public class ButtonStopController : MonoBehaviour {
  public MeshRenderer iconMeshRenderer;

  private float timer;

  // Start is called before the first frame update
  void Start() {
    // Hide the button until a recording is in progress.
    gameObject.SetActive(false);
  }

  // Update is called once per frame
  void Update() {
    // Flash the button icon if a recording is in progress.
    if (CameraProvider.IsRecording) BlinkIcon();
    else {
      timer                    = 0;
      iconMeshRenderer.enabled = true;
    }
  }

  private void BlinkIcon() {
    timer += Time.deltaTime;
    
    if (Time.timeSinceLevelLoad % 2 <= 1.0f) {
      iconMeshRenderer.enabled = true;
    }
    else {
      iconMeshRenderer.enabled = false;
    }
  }
}