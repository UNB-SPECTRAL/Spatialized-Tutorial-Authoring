using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class VideoTutorialController : MonoBehaviour {
  public RecordSceneController.TooltipDetails tooltipDetails;
  public VideoPlayer                          videoPlayer;

  // Start is called before the first frame update
  void Start() {
    gameObject.name = tooltipDetails.name;
    gameObject.transform.SetGlobalPose(tooltipDetails.globalPose);
    
    videoPlayer.url = tooltipDetails.videoFilePath;
    
    // Rotate the video since its playing upside down.
    videoPlayer.transform.rotation = Quaternion.Euler(0, 0, 180);
    
    // ****** How To Play A Video ******
    /*videoPlayer.Prepare();
    videoPlayer.prepareCompleted += (VideoPlayer source) => {
      Debug.Log("Video prepared");
      source.Play();
    };*/
    
    
    // ***** How To Generate A Thumbnail Based On The First Frame Of The Video *****
    videoPlayer.Stop();
    videoPlayer.renderMode = VideoRenderMode.APIOnly;
    videoPlayer.Prepare();
    videoPlayer.prepareCompleted += (VideoPlayer source) => {
      Debug.Log("Video prepared");
      videoPlayer.Pause();
    };
    videoPlayer.sendFrameReadyEvents = true;
    videoPlayer.frameReady += (VideoPlayer source, long frameIndex) => {
      Debug.Log("Frame Ready");
      var thumbnail = source.texture;
      videoPlayer.GetComponent<Renderer>().material.mainTexture = thumbnail;
    };
  }
}