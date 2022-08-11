using System;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using UnityEngine;
using UnityEngine.Video;

/**
 * This class is responsible to naming, positioning and handling the video player on the MRTK 2 ToolTip prefab.
 */
public class VideoToolTipController : MonoBehaviour {
  public RecordSceneController.TooltipDetails tooltipDetails;
  public VideoPlayer                          videoPlayer;

  void Awake() {
    // Error handling in case this controller is added to a GameObject which doe snot have a ToolTip component.
    // This is required since this Controller is specifically build to be used with the ToolTip component.
    if(gameObject.GetComponent<ToolTip>() == null) {
      Debug.LogError("VideoTutorialController requires a ToolTip component");
    }
    
    // Error handling in case this GameObject does not have an Interactable component.
    // This is required to play/pause the video when clicking on the VideoPlayer.
    if(gameObject.GetComponent<Interactable>() == null) {
      Debug.LogError("VideoTutorialController requires an Interactable component");
    }
  }

  void Start() {
    /*** ToolTip Configuration ***/
    // At minimum, a ToolTip will be instantiated with at least a `name` and `globalPose` property.
    // Set these properties.
    gameObject.name = tooltipDetails.name;
    GetComponent<ToolTip>().ToolTipText = tooltipDetails.name;
    gameObject.transform.SetGlobalPose(tooltipDetails.globalPose);
    
    // We must explicitly check if there is a `videoFilePath` in case this is a new ToolTip and the
    // video is currently being recorded.
    if(!String.IsNullOrEmpty(tooltipDetails.videoFilePath)) SetupVideoPlayer(tooltipDetails.videoFilePath);
    // Otherwise hide the VideoPlayer.
    else videoPlayer.gameObject.SetActive(false);
  }

  void Update() {
    // If the VideoPlayer GameObject is not active and there is a `videoFilePath` then show it.
    // And setup the video player
    if(!videoPlayer.gameObject.activeSelf && !String.IsNullOrEmpty(tooltipDetails.videoFilePath)) {
      videoPlayer.gameObject.SetActive(true);
      SetupVideoPlayer(tooltipDetails.videoFilePath);
    }
    
    // If the local rotation of the VideoPlayer is not 0, 0, 180 then reset it to 0, 0, 180.
    if(videoPlayer.transform.localRotation != Quaternion.Euler(0, 0, 180)) {
      videoPlayer.transform.localRotation = Quaternion.Euler(0, 0, 180);
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
    }));
  }
  
  #region Public Methods
  /** Handles the OnClick event for the VideoPlayer */
  public void OnClick() {
    Debug.Log("ToolTip Clicked");
    // When the video is not playing, play it.
    if(!videoPlayer.isPlaying) {
      videoPlayer.Play();
    }
    // When the video is playing, pause it.
    else {
      videoPlayer.Pause();
    }
  }
  #endregion
}