using System;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using UnityEngine;
using UnityEngine.Video;

/*** Import Helpers ***/
using StepDetails = TutorialStore.Tutorial.StepDetails;
using SceneState = SceneController.SceneState;

/**
 * This class is responsible to naming, positioning and handling the video player on the MRTK 2 ToolTip prefab.
 */
public class StepController : MonoBehaviour {
  /*** Unity Editor ***/
  public VideoPlayer videoPlayer; // The video player game object.
  public GameObject  deleteButton; // The step delete button game object.
  
  /*** Public Variables ***/
  [HideInInspector]
  public StepDetails stepDetails;
  public bool isBeingDestroyed = false;
  
  /*** Private Variables ***/
  private ToolTip _toolTip;

  void Awake() {
    // Error handling in case this controller is added to a GameObject which does not have a ToolTip component.
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
    name                 = stepDetails.id;
    _toolTip.ToolTipText = stepDetails.name;
    transform.SetGlobalPose(stepDetails.globalPose);

    // We must explicitly check if there is a `videoFilePath` in case this is a
    // new Step and the video is currently being recorded.
    if(!string.IsNullOrEmpty(stepDetails.videoFilePath)) SetupVideoPlayer(stepDetails.videoFilePath);
    // Otherwise hide the VideoPlayer.
    else videoPlayer.gameObject.SetActive(false);
    
    /*** Setup Delete Button ***/
    deleteButton.SetActive(false); // Hide the button
    deleteButton.GetComponent<ButtonConfigHelper>().OnClick.AddListener(() => {
      SceneController.Instance.OnDeleteStepButtonPress(stepDetails);
    }); // Setup the listener
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
    
    /*** Update Delete Button Visibility ***/
    // If we are in the CreateStep state and the delete button is not visible, make it visible
    if(
      SceneController.State == SceneState.CreateStep
      && deleteButton.activeSelf == false
    ) deleteButton.SetActive(true);
  }
  
  /** Given a url, setup the VideoPlayer with a thumbnail image. */
  void SetupVideoPlayer(string url) {
    videoPlayer.url               = url;
    videoPlayer.aspectRatio       = VideoAspectRatio.FitInside;
    // TODO: What we can do in the future is just render an image component above or something
    // so that we can show the thumbnail image while we load out the video.
    //videoPlayer.renderMode        = VideoRenderMode.APIOnly; 
    videoPlayer.targetCameraAlpha = 0.8f;

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
        
        // Show the first frame of the video, and not the frame 0 since on the
        // HoloLens the first frame is a grey frame.
        videoPlayer.frame = 1;
        
        // This renders the first frame of the video onto the VideoPlayer mesh.
        videoPlayer.Pause();
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
        Debug.Log("Step " + stepDetails.id + " video ended");
        
        // Once the video has ended, reset the video to show the first frame.
        // frame = 0 is sometimes a grey frame on the HoloLens.
        videoPlayer.frame = 1;
        
        // Notify the SceneController
        SceneController.PausingOrStopStepVideo();

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
    // Check if a click event can be handled
    if(!SceneController.CanClickStep(videoPlayer)) {
      Debug.LogWarning("Step " + stepDetails.id + " OnClick(): ERROR - In Incorrect State");
      return;
    }
    
    Debug.Log("Step " + stepDetails.id + " clicked");

    // If the video is not playing, play it.
    if(videoPlayer.isPlaying == false) {
      // Play the video
      Debug.Log("Playing Video");
      videoPlayer.Play();
      
      // Notify the SceneController
      SceneController.PlayingStepVideo();
    }
    // If the video is not playing, pause it
    else {
      // Pause the video
      Debug.Log("Pausing Video"); 
      videoPlayer.Pause();
      
      // Notify the SceneController
      SceneController.PausingOrStopStepVideo();
    }
  }
  #endregion
}