using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.WebCam;

/** Abstraction around interacting with the Camera API in Unity. */
public class CameraProvider : MonoBehaviour {
  /** Private Static Variables */
  #region Private Static Variables
  private static CameraProvider _instance; // Used for`_instance.StartCoroutine`
  
  private static VideoCapture _videoCapture; // Can only have one active at a time
  private static string       _fileName; // Name of the file to save to
  private static string       _filePath; // Name of the file to save to
  
  private static Resolution   _cameraResolution; // The best camera resolution found.

  /** List of standard resolutions that we should aim for when recording a video */
  private static readonly Resolution[] StandardResolutions = {
    new Resolution() { width = 1280, height = 720 },  // 720p   
    new Resolution() { width = 1920, height = 1080 }, // 1080p 
    new Resolution() { width = 2560, height = 1440 }, // 2Kp   
    new Resolution() { width = 3840, height = 2160 }, // 4K 
  };
  #endregion

  #region Unity Methods
  private void Awake() {
    /*** Validation ***/
    // Check if this script it attached to the `Main Camera` game object
    if (gameObject.name != "Main Camera") {
      Debug.LogError("CameraProvider must be attached to the `Main Camera` Game Object");
      throw new Exception("CameraProvider must be attached to the `Main Camera` Game Object");
    }
    
    // Check if there is a Microphone available for recording
    if (!Microphone.devices.Any()) {
      Debug.LogError("CameraProvider requires that a Microphone is available");
      throw new Exception("CameraProvider requires that a Microphone is available");
    }

    // Then save a reference to the instance of this script
    if (_instance == null) _instance = this;
    else Destroy(gameObject);
  }
  
  /** This Unity method determines the best camera resolution that is at least 30FPS that the camera can support. */
  private void Start() {
    // Get the optimal camera resolution to decrease file size that is at least 30 FPS.
    _cameraResolution = FindOptimalCameraResolution();
    Debug.Log("Camera Resolution: " + _cameraResolution.width + "x" + _cameraResolution.height + "@" + _cameraResolution.refreshRate + "hz");
  }
  #endregion

  #region Public Static Methods
  public static void StartRecording(string fileName) {
    if (_videoCapture is { IsRecording: true }) {
      Debug.LogError("VideoRecorder.StartRecording: There is already a recording in progress");
      return;
    }

    if (string.IsNullOrEmpty(fileName)) {
      Debug.LogError("VideoRecorder.StartRecording: Missing file name");
      return;
    }

    // Sanitize the filename by replacing spaces with underscores and lower casing the string
    _fileName = fileName.Replace(" ", "_").ToLower() + ".mp4";
    // Generate and save the file path
    _filePath = Path.Combine(Application.streamingAssetsPath, _fileName);
    
    // Start the video capture process
    VideoCapture.CreateAsync(OnCreateAsync);
  }
  
  /** Stops the video recording and return the file path to the video. */
  public static string StopRecording() {
    if (_videoCapture is { IsRecording: false }) {
      Debug.LogError("VideoRecorder.StopRecording: There is no recording in progress");
      return null;
    }
    // Stop the video capture process
    _videoCapture.StopRecordingAsync(OnStopRecordingAsync);

    return _filePath;
  }
  #endregion

  #region Private Static Methods
  /**
   * Find the minimum resolution that matches the standard resolution that this camera can support
   * to reduce file size.
   */
  private static Resolution FindOptimalCameraResolution() {
    // Get a list or support resolution
    Resolution[] cameraResolutions = VideoCapture
      .SupportedResolutions
      // This call is very important as there are many duplicate resolutions when interacting with the Unity API  
      .Distinct()
      .ToArray();
    
    // Update each resolutions refresh rate (its not included in the previous call)
    for (var i = 0; i < cameraResolutions.Length; i++) {
      cameraResolutions[i].refreshRate = (int)VideoCapture.GetSupportedFrameRatesForResolution(cameraResolutions[i]).Max();
      // Debug.Log($"Resolution: {cameraResolutions[i].width}x{cameraResolutions[i].height}@{cameraResolutions[i].refreshRate}");
    }
    
    // For each standard resolution, find the closest resolution that the camera can support
    foreach (var standardResolution in StandardResolutions) {
      Resolution potentialCameraResolution = cameraResolutions.FirstOrDefault(r => r.width == standardResolution.width && r.height == standardResolution.height);
      if(potentialCameraResolution.width != 0) {
        return potentialCameraResolution;
      }
    }
    
    // Otherwise return the lower 30FPS resolution in the list.
    return cameraResolutions.OrderByDescending(r => r.width * r.height).Reverse().First(r => r.refreshRate >= 30);
  }
  
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
    _fileName = null;
    _filePath = null;
  }
  #endregion
}