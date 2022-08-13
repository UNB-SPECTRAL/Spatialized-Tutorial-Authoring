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
  
  private ToolTip   _toolTip;

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

    /*** Step Configuration ***/
    // At minimum, a ToolTip will be instantiated with at least a `name` and `globalPose` property.
    // Set these properties.
    name                 = "Step " + stepDetails.id;
    _toolTip.ToolTipText = stepDetails.name;
    transform.SetGlobalPose(stepDetails.globalPose);

    // We must explicitly check if there is a `videoFilePath` in case this is a
    // new Step and the video is currently being recorded.
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
    videoPlayer.url               = url;
    videoPlayer.aspectRatio       = VideoAspectRatio.FitInside;
    // TODO: What we can do in the future is just render an image component above or something
    // so that we can show the thumbnail image while we load out the video.
    //videoPlayer.renderMode        = VideoRenderMode.APIOnly; 
    videoPlayer.targetCameraAlpha = 0.5f;

    // BUG: Rotate the video since its playing upside down for some reason.
    videoPlayer.transform.rotation = Quaternion.Euler(videoPlayer.transform.rotation.x, videoPlayer.transform.rotation.y, 180);
    
    // Finally generate a thumbnail of the video after ToolTip script has completed (wait 0.2s)
    // BUG: The ToolTip prefab is somehow disabling the VideoPlayer and causing an "Cannot Prepare a disabled VideoPlayer" error.
    StartCoroutine(Utilities.WaitForSecondsAnd(0.2f, () => {
      // To generate a thumbnail, we are essentially preparing the video (similar
      // to pressing play) then pausing the video (which renders the first frame)
      // and then showing the videoPlayer mesh.
      // A more performant approach would be to follow the link below with
      // updating the frame and saving this as an image to the unpreparing the
      // video afterwards. TODO: Reference HoloVision to see how they did it.
      // This code was inspired by: https://forum.unity.com/threads/how-to-extract-frames-from-a-video.853687
      // https://forum.unity.com/threads/how-to-extract-frames-from-a-video.853687/
      // https://stackoverflow.com/questions/68232628/white-frame-before-video-plays-in-unity-instead-of-a-custom-thumbnail
      // https://forum.unity.com/threads/create-thumbnail-from-video.769655/
      // https://forum.unity.com/threads/how-to-get-video-thumbnails-without-running-the-video.753419/

      // Preparing the video so that we can load the first frame
      videoPlayer.Prepare();
      videoPlayer.prepareCompleted += (source) => {
        Debug.Log("Video prepared");
        
        // This renders the first frame of the video onto the VideoPlayer mesh.
        videoPlayer.Pause();
        
        // Once prepared we render the videoPlayer again
        videoPlayer.GetComponent<Renderer>().enabled = true;
      };
      
      // This worked when in APIOnly Mode.
      /*videoPlayer.sendFrameReadyEvents = true;
      videoPlayer.frameReady += (source, frameIndex) => {
        Debug.Log("Frame " + frameIndex + " Ready");
        
        // Once a frame is ready, we use it to extract a thumbnail image.
        // TODO: An optimization could be to generate a thumbnail image .jpg
        // so that we don't have to load all the videos.
        var thumbnail                                             = source.texture;
        videoPlayer.GetComponent<Renderer>().material.mainTexture = thumbnail;
      };*/
      
      // When setting up the VideoPlayer we want to be notified when the video
      // is ended to notify the RecordSceneController.
      videoPlayer.loopPointReached += (source) => {
        Debug.Log("Step " + stepDetails.id + " video ended. Stopping video.");
        
        // Update the RecordSceneState so other videos can be played
        RecordSceneController.Instance.state = RecordSceneController.State.Idle;
        
        // Once the video is done, render the first frame again.
        videoPlayer.frame = 0;
        
        // Stop the video
        // TODO: This was trying to un-prepare the video to save on RAM.
        // Note that Stop will remove it from memory.
        // See https://forum.unity.com/threads/close-video-player-and-free-memory.491749/
        // videoPlayer.Stop();
        
        // Add the thumbnail to the Step
        // videoPlayer.GetComponent<Renderer>().material.mainTexture = _thumbnail;
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