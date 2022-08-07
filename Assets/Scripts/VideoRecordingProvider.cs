using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.WebCam;

/**
 * Provides video recording functionality to the Unity scene.
 * 
 * FIXME: Must always be attached to the `Main Camera` Game Object.
 */
public class VideoRecordingProvider : MonoBehaviour {
  #region Public Static Variables
    public static VideoRecordingProvider Instance;
  #endregion

  #region Private Static Variables
  private static VideoCapture _videoCapture;   // Can only have one active at a time
  private static string       _fileName     ;   // Name of the file to save to
  #endregion
  
  #region Unity Methods
    private void Awake() {
      if(Instance == null) {
        Instance = this;
      } else {
        Destroy(gameObject);
      }
    }
    
    private void Start() { }
  #endregion
  
  #region Public Static Methods
  public string StartRecording(string fileName) {
    if (_videoCapture != null && _videoCapture.IsRecording) {
      Debug.LogError("VideoRecorder.StartRecording: There is already a recording in progress");
      Console.Out.WriteLine("VideoRecorder.StartRecording: There is already a recording in progress");
      return null;
    }

    if (string.IsNullOrEmpty(fileName)) {
      Debug.LogError("VideoRecorder.StartRecording: Missing file name");
      Console.Out.WriteLine("VideoRecorder.StartRecording: Missing file name");
      return null;
    }

    _fileName = fileName;    
    // Sanitize the filename
    _fileName = fileName.Replace(" ", "_") + ".mp4";
    // Start the video capture process
    VideoCapture.CreateAsync(OnCreateAsync);
    // Return the filename to the caller
    return _fileName;
  }
  #endregion

  #region Private Static Methods
  private static void OnCreateAsync(VideoCapture videoCapture) {
    if (videoCapture == null) {
      Debug.LogError("Failed to create VideoCapture object");
      Console.Out.WriteLine("Failed to create VideoCapture object");
      return;
    }

    _videoCapture = videoCapture;

    // Find the highest resolution and framerate we can use.
    //Resolution cameraResolution = VideoCapture.SupportedResolutions.OrderByDescending(res => res.width * res.height).First();
    Resolution cameraResolution = new Resolution() { width = 1920, height = 1080 };
    float cameraFramerate = VideoCapture.GetSupportedFrameRatesForResolution(cameraResolution)
      .OrderByDescending(fps => fps).First();
    Debug.Log("Using camera framerate " + cameraFramerate);
  
    // Setup the camera parameters.
    CameraParameters cameraParameters = new CameraParameters {
      hologramOpacity        = 0.0f,
      frameRate              = cameraFramerate,
      cameraResolutionWidth  = cameraResolution.width,
      cameraResolutionHeight = cameraResolution.height,
      pixelFormat            = CapturePixelFormat.BGRA32,
    };

    _videoCapture.StartVideoModeAsync(cameraParameters, VideoCapture.AudioState.MicAudio, OnStartVideoModeAsync);
  }
  
  private static void OnStartVideoModeAsync(VideoCapture.VideoCaptureResult result) {
    if (result.success == false) {
      Debug.LogError("StartVideoModeAsync: Failed");
      Console.Out.WriteLine("StartVideoModeAsync: Failed");
      return;
    }
    
    Debug.Log("StartVideoModeAsync: Success");
    Console.Out.WriteLine("StartVideoModeAsync: Success");

    string filePath = Path.Combine(Application.streamingAssetsPath, _fileName);
    _videoCapture.StartRecordingAsync(filePath, OnStartRecordingAsync);
  }
  
  private static void OnStartRecordingAsync(VideoCapture.VideoCaptureResult result) {
    if (result.success == false) {
      Debug.LogError("VideoRecorder.StartRecordingAsync: Failed");
      Console.Out.WriteLine("VideoRecorder.StartRecordingAsync: Failed");
      return;
    }

    Debug.Log("VideoRecorder.StartRecordingAsync: Success");
    Console.Out.WriteLine("VideoRecorder.StartRecordingAsync: Success");
    // FIXME: Stop the recording after 5 seconds.
    Instance.StartCoroutine(
      WaitForSecondsAnd(
        10,
        () => {
          _videoCapture.StopRecordingAsync(OnStopRecordingAsync);
        }));
  }
  
  private static void OnStopRecordingAsync(VideoCapture.VideoCaptureResult result) {
    if (result.success == false) {
      Debug.LogError("VideoRecorder.StopRecordingAsync: Failed");
      Console.Out.WriteLine("VideoRecorder.StopRecordingAsync: Failed");
      return;
    }

    Debug.Log("VideoRecorder.StopRecordingAsync: Success");
    Console.Out.WriteLine("VideoRecorder.StopRecordingAsync: Success");
    _videoCapture.StopVideoModeAsync(OnStopVideoModeAsync);
  }
  
  private static void OnStopVideoModeAsync(VideoCapture.VideoCaptureResult result) {
    _videoCapture.Dispose();
    _videoCapture = null;
  }
  
  #endregion
  
  // TODO: Move this to a static utility class.
  static IEnumerator WaitForSecondsAnd(float seconds, Action action) {
    yield return new WaitForSeconds(seconds);
    action();
  }
}