using System;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using UnityEngine;
using UnityEngine.Video;

/*** Import Helpers ***/
using StepDetails = RecordSceneController.StepStore.StepDetails;

/**
 * This class is responsible to naming, positioning and handling the video player on the MRTK 2 ToolTip prefab.
 */
public class StepController : MonoBehaviour {
  public StepDetails stepDetails;
  public VideoPlayer videoPlayer;
  
  private ToolTip _toolTip;

  void Awake() {
    // Error handling in case this controller is added to a GameObject which doe snot have a ToolTip component.
    // This is required since this Controller is specifically build to be used with the ToolTip component.
    if(gameObject.GetComponent<ToolTip>() == null) {
      Debug.LogError("StepController requires a ToolTip component");
    }
    
    // Error handling in case this GameObject does not have an Interactable component.
    // This is required to play/pause the video when clicking on the VideoPlayer.
    if(gameObject.GetComponent<Interactable>() == null) {
      Debug.LogError("StepController requires an Interactable component");
    }
  }

  void Start() {
    /*** Component References ***/
    _toolTip = gameObject.GetComponent<ToolTip>();
    
    /*** ToolTip Configuration ***/
    // At minimum, a ToolTip will be instantiated with at least a `name` and `globalPose` property.
    // Set these properties.
    // TODO: Create two fields for the GameObject name and the ToolTip text.
    name = stepDetails.name.Split(':')[0];
    _toolTip.ToolTipText = stepDetails.name;
    transform.SetGlobalPose(stepDetails.globalPose);
    
    // We must explicitly check if there is a `videoFilePath` in case this is a new Step and the
    // video is currently being recorded.
    if(!string.IsNullOrEmpty(stepDetails.videoFilePath)) SetupVideoPlayer(stepDetails.videoFilePath);
    // Otherwise hide the VideoPlayer.
    else videoPlayer.gameObject.SetActive(false);
  }

  void Update() {
    // If the VideoPlayer GameObject is not active and there is a `videoFilePath` then show it.
    // And setup the video player
    if(!videoPlayer.gameObject.activeSelf && !String.IsNullOrEmpty(stepDetails.videoFilePath)) {
      videoPlayer.gameObject.SetActive(true);
      SetupVideoPlayer(stepDetails.videoFilePath);
    }
    
    // If the local rotation of the VideoPlayer is not 0, 0, 180 then reset it to 0, 0, 180.
    if(videoPlayer.transform.localRotation != Quaternion.Euler(0, 0, 180)) {
      videoPlayer.transform.localRotation = Quaternion.Euler(0, 0, 180);
    }
    
    /*** Update Text ***/
    // If the current ToolTip text does not match the StepDetails.name field, update it.
    if(_toolTip.ToolTipText != stepDetails.name) {
      _toolTip.ToolTipText = stepDetails.name;
    }
  }
  
  /** Given a url, setup the VideoPlayer with a thumbnail image. */
  void SetupVideoPlayer(string url) {
    videoPlayer.url         = url;
    videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
    // TODO: Might need this when rendering on the HoloLens.
    // videoPlayer.targetCameraAlpha = 0.5f;

    // BUG: Rotate the video since its playing upside down for some reason.
    videoPlayer.transform.rotation = Quaternion.Euler(videoPlayer.transform.rotation.x, videoPlayer.transform.rotation.y, 180);
    
    // Finally generate a thumbnail of the video after ToolTip script has completed (wait 0.2s)
    // BUG: The ToolTip prefab is somehow disabling the VideoPlayer and causing an "Cannot Prepare a disabled VideoPlayer" error.
    StartCoroutine(Utilities.WaitForSecondsAnd(0.2f, () => {
      // The step required to generate a video thumbnail based on the first frame of the video.
      // This code was inspired by: https://forum.unity.com/threads/how-to-extract-frames-from-a-video.853687
      videoPlayer.Stop();
      videoPlayer.renderMode = VideoRenderMode.APIOnly;
      videoPlayer.Prepare();
      videoPlayer.prepareCompleted += (source) => {
        // Debug.Log("Video prepared");
        videoPlayer.Pause();
      };
      videoPlayer.sendFrameReadyEvents = true;
      videoPlayer.frameReady += (source, frameIndex) => {
        // Debug.Log("Frame Ready");
        var thumbnail = source.texture;
        videoPlayer.GetComponent<Renderer>().material.mainTexture = thumbnail;
      };
      
      // When setting up the VideoPlayer we want to be notified when the video
      // is ended to notify the RecordSceneController.
      videoPlayer.loopPointReached += (source) => {
        Debug.Log(stepDetails.name.Split(':')[0] + " video ended");
        RecordSceneController.Instance.state = RecordSceneController.State.Idle;
      };
    }));
  }
  
  #region Public Methods
  /**
   * When the RecordSceneController is in IDLE state, handle the OnClick event
   * for the Tooltip. This will either play or pause the VideoPlayer.
   */
  public void OnClick() {
    // FIXME: Logging since there seems to be multiple clicks on a ToolTip
    Debug.Log(stepDetails.name.Split(':')[0] + " Clicked");
    
    // When the RecordSceneController is in RECORDING state, exit
    if(RecordSceneController.CurrentState == RecordSceneController.State.Recording) {
      Debug.Log("Recording state, exiting");
      return;
    }

    // When the RecordSceneController is in IDLE state, AND the VideoPlayer is
    // NOT playing, then play the VideoPlayer.
    if(
        RecordSceneController.CurrentState == RecordSceneController.State.Idle
        && videoPlayer.isPlaying == false
      ) {
      // Notify the RecordSceneController that a video is playing
      RecordSceneController.Instance.state = RecordSceneController.State.Playing;
      
      Debug.Log("Video is not playing. Playing video");
      
      videoPlayer.Play();
    }
    // When the RecordSceneController is in PLAYING state, AND the VideoPlayer
    // IS playing, then this ToolTip must be playing the video and thus we can
    // pause the VideoPlayer.
    else if(
        RecordSceneController.CurrentState == RecordSceneController.State.Playing
        && videoPlayer.isPlaying
      ) {
      // Notify the RecordSceneController that a video is not playing
      RecordSceneController.Instance.state = RecordSceneController.State.Idle;
      
      Debug.Log("Video is playing. Pausing video");
      
      videoPlayer.Pause();
    }
    else {
      Debug.Log("No case matched. Exiting");
      Debug.Log(RecordSceneController.CurrentState);
      Debug.Log(videoPlayer.isPlaying);  
    }
  }
  #endregion
}