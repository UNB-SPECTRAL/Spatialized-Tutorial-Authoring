using System;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

/*** Import Helpers ***/
using StepDetails = TutorialStore.Tutorial.StepDetails;
using SceneState = SceneController.SceneState;

/**
 * This class is responsible to naming, positioning and handling the video player on the MRTK 2 ToolTip prefab.
 *
 * TODO: When playing another video, unprepare all other videos to save on memory.
 */
public class StepController : MonoBehaviour {
  /*** Unity Editor ***/
  [Header("Game Objects")]
  public GameObject videoPlayerGameObject; // The video player game object.
  public GameObject deleteButton; // Delete button game object
  public GameObject title; // Title game object
  
  [Header("Background")]
  public GameObject background;       // The step background game object.
  public Material   unviewedMaterial; // The normal material of the step.
  public Material   viewedMaterial;   // The highlighted material of the step.
  
  /*** Public Variables ***/
  [HideInInspector]
  public StepDetails stepDetails;
  public bool isBeingDestroyed;
  public bool isViewed;
  
  /*** Private Variables ***/
  private ToolTip    _toolTip;
  
  // For handling the Step height change.
  private GameObject _contentParent; // The content parent game object.
  private float      _contentParentYPosition; // Original Y position of the content parent.
  
  /** Video Player **/
  private Renderer _videoPlayerRenderer; // The video player renderer.
  private VideoPlayer _videoPlayer;
  private Texture2D _thumbnailImage; // We need to load the image as a texture to show it.
  private bool _isVideoPlaying; // Is the video playing? This is used since isPlaying takes a few frames to update.

  private void Awake() {
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

  private void Start() {
    /*** Component References ***/
    _toolTip = gameObject.GetComponent<ToolTip>();
    _contentParent = gameObject.transform.Find("Pivot/ContentParent").gameObject;
    _contentParentYPosition = _contentParent.transform.localPosition.y;
    
    _videoPlayer = videoPlayerGameObject.GetComponent<VideoPlayer>();
    _videoPlayerRenderer = videoPlayerGameObject.GetComponent<Renderer>();
      
    /*** Background Update ***/
    // Set the background material to the un-viewed material.
    background.GetComponent<Renderer>().material = unviewedMaterial;

    /*** Step Configuration ***/
    // At minimum, a ToolTip will be instantiated with at least a `name` and `globalPose` property.
    // Set these properties.
    name                 = stepDetails.id;
    _toolTip.ToolTipText = stepDetails.name;
    transform.SetGlobalPose(stepDetails.globalPose);
    title.GetComponent<TextMeshPro>().text = stepDetails.id.Split('_').Last();
    
    /*** Init ***/
    // Each Step will have a StepDetails on creation.
    // Use the `thumbnailFilePath` to setup the image.
    if (string.IsNullOrEmpty(stepDetails.thumbnailFilePath) == false) {
      // Get the Step thumbnail image
      var thumbnailImageData = File.ReadAllBytes(stepDetails.thumbnailFilePath);
      _thumbnailImage = new Texture2D(2, 2);
      _thumbnailImage.LoadImage(thumbnailImageData);
      _videoPlayerRenderer.material.mainTexture = _thumbnailImage;
    }
    // Use the `videoFilePath` to setup the video player.
    if(string.IsNullOrEmpty(stepDetails.videoFilePath) == false) {
      SetupVideoPlayer(stepDetails.videoFilePath);
    }
    
    /*** Setup Delete Button ***/
    deleteButton.SetActive(false); // Hide the button
    // NOTE: For experiment #3, even though we are adding a button press listener, we don't enable the button at app.
    // deleteButton.GetComponent<ButtonConfigHelper>().OnClick.AddListener(() => {
    //   SceneController.Instance.OnDeleteStepButtonPress(stepDetails);
    // }); // Setup the listener
  }
  
  void Update() {
    // When the video is not playing, show the thumbnail image
    if (_isVideoPlaying == false) {
      _videoPlayerRenderer.material.mainTexture = _thumbnailImage;
    }

    // // If the VideoPlayer GameObject is not active and there is a `videoFilePath` then show it.
    // // And setup the video player
    // if(!videoPlayerGameObject.gameObject.activeSelf && !String.IsNullOrEmpty(stepDetails.videoFilePath)) {
    //   videoPlayerGameObject.gameObject.SetActive(true);
    //   SetupVideoPlayer(stepDetails.videoFilePath);
    // }
    
    // If the local rotation of the VideoPlayer is not 0, 0, 180 then reset it to 0, 0, 180.
    if(videoPlayerGameObject.transform.localRotation != Quaternion.Euler(0, 0, 180)) {
      videoPlayerGameObject.transform.localRotation = Quaternion.Euler(0, 0, 180);
    }
    
    /*** Update Text ***/
    // If the current ToolTip text does not match the StepDetails.name field, update it.
    if(_toolTip.ToolTipText != stepDetails.name) {
      _toolTip.ToolTipText = stepDetails.name;
    }
    
    /*** Update Delete Button Visibility ***/
    // If we are in the CreateStep state and the delete button is not visible, make it visible
    // NOTE: Experiment #3 will not allow Steps to be deleted...
    // if(
    //   SceneController.State == SceneState.CreateStep
    //   && deleteButton.activeSelf == false
    // ) deleteButton.SetActive(true);
    
    /*** Update Height ***/
    // If another StepController is playing a video, increase this Step's height
    // so that is does not overlap the other StepController. Only do this once.
    if (
      (SceneController.State == SceneState.CreateStepPlaying || SceneController.State == SceneState.ViewStepPlaying)
      && _isVideoPlaying == false
    ) {
      if (Math.Abs(_contentParent.transform.localPosition.y - _contentParentYPosition) < 0.001f) {
        Debug.Log("HoloTuts: " + SceneController.State);
        Debug.Log("HoloTuts: " +_videoPlayer.isPlaying);
        
        Debug.Log(stepDetails.id + " increasing height");
        _contentParent.transform.localPosition = new Vector3(0, 0.5f, 0);   
      }
    }
    else if(Math.Abs(_contentParent.transform.localPosition.y - _contentParentYPosition) > 0.001f) {
      Debug.Log("HoloTuts: " + SceneController.State);
      Debug.Log("HoloTuts: " +_videoPlayer.isPlaying);
      Debug.Log(stepDetails.id + " decreasing height");
      _contentParent.transform.localPosition = new Vector3(0, _contentParentYPosition, 0);
    }
    
    /*** Update Position ***/
    // If the position of the Step has changes, change it
    if (transform.GetGlobalPose() != stepDetails.globalPose) {
      Debug.Log("Updating position of " + stepDetails.id);
      transform.SetGlobalPose(stepDetails.globalPose);
    }
  }
  
  /** Given a url, setup the VideoPlayer with a thumbnail image. */
  private void SetupVideoPlayer(string url) {
    _videoPlayer.url               = url;
    _videoPlayer.aspectRatio       = VideoAspectRatio.FitInside;
    /* TODO: What we should do in the future is render an image component above the video player
     * so that we can show the thumbnail image vs loading the video to get the thumbnail image.
     */
    //videoPlayer.renderMode        = VideoRenderMode.APIOnly; 
    _videoPlayer.targetCameraAlpha = 0.8f;

    // BUG: Rotate the video since its playing upside down for some reason.
    var rotation           = videoPlayerGameObject.transform.rotation;
    rotation                       = Quaternion.Euler(rotation.x, rotation.y, 180);
    videoPlayerGameObject.transform.rotation = rotation;

    // Finally generate a thumbnail of the video after ToolTip script has completed (wait 0.2s)
    // BUG: The ToolTip prefab is somehow disabling the VideoPlayer and causing an "Cannot Prepare a disabled VideoPlayer" error.
    // StartCoroutine(Utilities.WaitForSecondsAnd(0.2f, () => {
    //   // To generate a thumbnail, we are essentially preparing the video (similar
    //   // to pressing play) then pausing the video (which renders the first frame)
    //   // and then showing the videoPlayer mesh.
    //   // A more performant approach would be to follow the link below with
    //   // updating the frame and saving this as an image to the un-preparing the
    //   // video afterwards. TODO: Reference HoloVision to see how they did it.
    //   // This code was inspired by:
    //   // https://forum.unity.com/threads/how-to-extract-frames-from-a-video.853687
    //   // https://forum.unity.com/threads/how-to-extract-frames-from-a-video.853687/
    //   // https://stackoverflow.com/questions/68232628/white-frame-before-video-plays-in-unity-instead-of-a-custom-thumbnail
    //   // https://forum.unity.com/threads/create-thumbnail-from-video.769655/
    //   // https://forum.unity.com/threads/how-to-get-video-thumbnails-without-running-the-video.753419/
    //
    //   // Preparing the video so that we can load the first frame
    //   videoPlayer.Prepare();
    //   videoPlayer.prepareCompleted += (source) => {
    //     Debug.Log("Video prepared");
    //     
    //     // Show the first frame of the video, and not the frame 0 since on the
    //     // HoloLens the first frame is a grey frame.
    //     videoPlayer.frame = 1;
    //     
    //     // This renders the first frame of the video onto the VideoPlayer mesh.
    //     videoPlayer.Pause();
    //   };
    //   
    //   // This worked when in APIOnly Mode.
    //   /*videoPlayer.sendFrameReadyEvents = true;
    //   videoPlayer.frameReady += (source, frameIndex) => {
    //     Debug.Log("Frame " + frameIndex + " Ready");
    //     
    //     // Once a frame is ready, we use it to extract a thumbnail image.
    //     // TODO: An optimization could be to generate a thumbnail image .jpg
    //     // so that we don't have to load all the videos.
    //     var thumbnail                                             = source.texture;
    //     videoPlayer.GetComponent<Renderer>().material.mainTexture = thumbnail;
    //   };*/
    //   
    //   // When setting up the VideoPlayer we want to be notified when the video
    //   // is ended to notify the RecordSceneController.
    //   videoPlayer.loopPointReached += (source) => {
    //     Debug.Log("Step " + stepDetails.id + " video ended");
    //     
    //     // Once the video has ended, reset the video to show the first frame.
    //     // frame = 0 is sometimes a grey frame on the HoloLens.
    //     videoPlayer.frame = 1;
    //     
    //     // Notify the SceneController
    //     SceneController.PauseOrStopStepVideo();
    //
    //     // Stop the video
    //     // TODO: This was trying to un-prepare the video to save on RAM.
    //     // Note that Stop will remove it from memory.
    //     // See https://forum.unity.com/threads/close-video-player-and-free-memory.491749/
    //     // videoPlayer.Stop();
    //     
    //     // Add the thumbnail to the Step
    //     // videoPlayer.GetComponent<Renderer>().material.mainTexture = _thumbnail;
    //   };
    // }));
  }

  #region Public Methods

  public void StopVideo() {
    // Update local state
    _isVideoPlaying = false;
    
    _videoPlayer.Stop();
  }
    
  /**
   * When the RecordSceneController is in IDLE state, handle the OnClick event
   * for the Tooltip. This will either play or pause the VideoPlayer.
   */
  public void OnClick() {
    // Check if a click event can be handled
    if(!SceneController.CanClickStep()) {
      Debug.LogWarning("Step " + stepDetails.id + " OnClick(): ERROR - In Incorrect State: " + SceneController.State);
      return;
    }
    
    Debug.Log("Step " + stepDetails.id + " Clicked");
    
    // Update the background material to show that the Step has been viewed.
    isViewed = true;
    background.GetComponent<Renderer>().material = viewedMaterial;

    // If the video is not playing, play it.
    if(_isVideoPlaying == false) {
      // Update local state
      _isVideoPlaying = true;
        
      // Play the video
      Debug.Log("Playing Video");
      _videoPlayer.Play();
        
      // Notify the SceneController so that any existing videos can be stopped.
      Debug.Log("HoloTuts:StepController: Calling SceneController.PlayStepVideo()");
      SceneController.PlayStepVideo(this);
    }
    // If the video is not playing, pause it
    else {
      // Update local state
      _isVideoPlaying = false;
      
      // Pause the video
      Debug.Log("Stopping Video");
      _videoPlayer.Stop();
      
      // Notify the SceneController that this video is not being played anymore
      Debug.Log("HoloTuts:StepController: Calling SceneController.PauseOrStopStepVideo()");
      SceneController.PauseOrStopStepVideo();
    }
  }
  #endregion 
}