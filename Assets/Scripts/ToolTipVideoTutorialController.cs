using System;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using UnityEngine;
using UnityEngine.Video;

/**
 * This class is responsible to naming, positioning and handling the video player on the MRTK 2 ToolTip prefab.
 */
public class ToolTipVideoTutorialController : MonoBehaviour {
  public RecordSceneController.TooltipDetails tooltipDetails;
  public VideoPlayer                          videoPlayer;

  void Awake() {
    // Error handling in case this controller is added to a GameObject which doe snot have a ToolTip component.
    if(gameObject.GetComponent<ToolTip>() == null) {
      Debug.LogError("VideoTutorialController requires a ToolTip component");
    }
  }

  void Start() {
    /*** ToolTip Configuration ***/
    // At minimum, a ToolTip will be instantiated with at least a `name` and `globalPose` property.
    // Set these properties.
    gameObject.name = tooltipDetails.name;
    gameObject.transform.SetGlobalPose(tooltipDetails.globalPose);
    
    // We must explicitly check if there is a `videoFilePath` in case this is a new ToolTip and the
    // video is currently being recorded.
    if(!String.IsNullOrEmpty(tooltipDetails.videoFilePath)) SetupVideoPlayer(tooltipDetails.videoFilePath);

    // ****** How To Play A Video ******
    /*videoPlayer.Prepare();
    videoPlayer.prepareCompleted += (VideoPlayer source) => {
      Debug.Log("Video prepared");
      source.Play();
    };*/
  }
  
  /** Given a url, setup the VideoPlayer with a thumbnail image. */
  void SetupVideoPlayer(string url) {
    videoPlayer.url         = url;
    videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
    // TODO: Might need this when rendering on the HoloLens.
    // videoPlayer.targetCameraAlpha = 0.5f;
    
    
    // BUG: Rotate the video since its playing upside down for some reason.
    videoPlayer.transform.rotation = Quaternion.Euler(0, 0, 180);
    
    // Finally generate a thumbnail of the video after ToolTip script has completed (wait 0.2s)
    // BUG: The ToolTip prefab is somehow disabling the VideoPlayer and causing an "Cannot Prepare a disabled VideoPlayer" error.
    StartCoroutine(Utilities.WaitForSecondsAnd(0.2f, () => {
      // The step required to generate a video thumbnail based on the first frame of the video.
      // This code was inspired by: https://forum.unity.com/threads/how-to-extract-frames-from-a-video.853687
      videoPlayer.Stop();
      videoPlayer.renderMode = VideoRenderMode.APIOnly;
      videoPlayer.Prepare();
      videoPlayer.prepareCompleted += (VideoPlayer source) => {
        // Debug.Log("Video prepared");
        videoPlayer.Pause();
      };
      videoPlayer.sendFrameReadyEvents = true;
      videoPlayer.frameReady += (VideoPlayer source, long frameIndex) => {
        // Debug.Log("Frame Ready");
        var thumbnail = source.texture;
        videoPlayer.GetComponent<Renderer>().material.mainTexture = thumbnail;
      };
    }));
  }
}