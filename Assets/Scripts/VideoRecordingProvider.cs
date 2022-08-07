using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.WebCam;

/**
 * Provides video recording functionality to the Unity scene.
 */
public class VideoRecordingProvider : MonoBehaviour {
  #region Private Static Variables
  private static VideoRecordingProvider _instance; // Used for`_instance.StartCoroutine`
  
  private static VideoCapture _videoCapture; // Can only have one active at a time
  private static string       _fileName; // Name of the file to save to

  private static Resolution[] _cameraResolutions; // The resolutions that the camera can support
  private static Resolution   _cameraResolution; // The best camera resolution found.
  #endregion

  #region Unity Methods
  private void Awake() {
    // Check if this script it attached to the `Main Camera` game object
    if (gameObject.name != "Main Camera") {
      Debug.LogError("VideoRecordingProvider must be attached to the `Main Camera` game object");
      Destroy(gameObject);
      return;
    }

    // Then save a reference to the instance of this script
    if (_instance == null) {
      _instance = this;
    }
    else {
      Destroy(gameObject);
    }
  }
  
  private void Start() {
    // Determines the best camera resolution to use for recording a video.
    _cameraResolutions = VideoCapture
      .SupportedResolutions
      .Distinct()
      .ToArray();
    for (var i = 0; i < _cameraResolutions.Length; i++) {
      _cameraResolutions[i].refreshRate = (int)VideoCapture.GetSupportedFrameRatesForResolution(_cameraResolutions[i]).Max();
      // Debug.Log($"Resolution: {cameraResolutions[i].width}x{cameraResolutions[i].height}@{cameraResolutions[i].refreshRate}");
    }
    
    _cameraResolution = _cameraResolutions.OrderByDescending(r => r.width * r.height).First(r => r.refreshRate >= 30);
    
    Debug.Log("Best camera resolution is: " + _cameraResolution.width + "x" + _cameraResolution.height + "@" + _cameraResolution.refreshRate + "hz");
  }
  #endregion

  #region Public Static Methods
  public static string Start(string fileName) {
    if (_videoCapture is { IsRecording: true }) {
      Debug.LogError("VideoRecorder.StartRecording: There is already a recording in progress");
      return null;
    }

    if (string.IsNullOrEmpty(fileName)) {
      Debug.LogError("VideoRecorder.StartRecording: Missing file name");
      return null;
    }

    // Sanitize the filename by replacing spaces with underscores and lower casing the string
    _fileName = fileName.Replace(" ", "_").ToLower() + ".mp4";
    // Start the video capture process
    VideoCapture.CreateAsync(OnCreateAsync);
    // Return the filename to the caller
    return Path.Combine(Application.streamingAssetsPath, _fileName);
  }
  #endregion

  #region Private Static Methods
  private static void OnCreateAsync(VideoCapture videoCapture) {
    if (videoCapture == null) {
      Debug.LogError("Failed to create VideoCapture object");
      return;
    }
    
    // Save the reference to created VideoCapture instance.
    _videoCapture = videoCapture;
    
    // Setup the camera parameters.
    CameraParameters cameraParameters = new CameraParameters {
      hologramOpacity        = 0.0f,
      frameRate              = _cameraResolution.refreshRate,
      cameraResolutionWidth  = _cameraResolution.width,
      cameraResolutionHeight = _cameraResolution.height,
      pixelFormat            = CapturePixelFormat.BGRA32,
    };

    _videoCapture.StartVideoModeAsync(cameraParameters, VideoCapture.AudioState.MicAudio, OnStartVideoModeAsync);
  }

  private static void OnStartVideoModeAsync(VideoCapture.VideoCaptureResult result) {
    if (result.success == false) {
      Debug.LogError("StartVideoModeAsync: Failed");
      return;
    }

    Debug.Log("StartVideoModeAsync: Success");

    string filePath = Path.Combine(Application.streamingAssetsPath, _fileName);
    _videoCapture.StartRecordingAsync(filePath, OnStartRecordingAsync);
  }

  private static void OnStartRecordingAsync(VideoCapture.VideoCaptureResult result) {
    if (result.success == false) {
      Debug.LogError("VideoRecorder.StartRecordingAsync: Failed");
      return;
    }

    Debug.Log("VideoRecorder.StartRecordingAsync: Success");
    // FIXME: Stop the recording after 5 seconds.
    _instance.StartCoroutine(
      WaitForSecondsAnd(
        10,
        () => { _videoCapture.StopRecordingAsync(OnStopRecordingAsync); }));
  }

  private static void OnStopRecordingAsync(VideoCapture.VideoCaptureResult result) {
    if (result.success == false) {
      Debug.LogError("VideoRecorder.StopRecordingAsync: Failed");
      return;
    }

    Debug.Log("VideoRecorder.StopRecordingAsync: Success");
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