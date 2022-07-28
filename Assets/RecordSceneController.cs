using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Experimental.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using Microsoft.MixedReality.WorldLocking.Tools;
using UnityEngine;
using Debug = UnityEngine.Debug;

/**
 * This class captures "Air Click"s events and instantiates a game object at that location.
 */
public class RecordSceneController : InputSystemGlobalHandlerListener, IMixedRealityPointerHandler  {
  /***** Public Variables *****/
  // This sphere will be rendered at the click location.
  public GameObject sphere;
  
  /***** Private Variables *****/
  private AnchorStore anchorStore;
  
  /***** Constructor *****/
  public RecordSceneController() {
    anchorStore = GetAnchors();
  }

  #region InputSystemGlobalHandlerListener Implementation
    protected override void RegisterHandlers() {
      MixedRealityToolkit.Instance?.GetService<IMixedRealityInputSystem>()
        ?.RegisterHandler<IMixedRealityPointerHandler>(this);
    }

    protected override void UnregisterHandlers() {
      MixedRealityToolkit.Instance?.GetService<IMixedRealityInputSystem>()
        ?.UnregisterHandler<IMixedRealityPointerHandler>(this);
    }
  #endregion InputSystemGlobalHandlerListener Implementation
  
  #region IMixedRealityPointerHandler
    public void OnPointerClicked(MixedRealityPointerEventData eventData) {
      Debug.Log("Pointer Click");
      
      // Get the hit points of the AirClick
      Vector3 hitPoint = eventData.Pointer.Result.Details.Point;
      // Instantiate a sphere at the hit point.
      GameObject newSphere = Instantiate(sphere, hitPoint, Quaternion.identity);
      // And world lock it.
      ToggleWorldAnchor twa = newSphere.AddComponent<ToggleWorldAnchor>();
      twa.AlwaysLock = true;

      // And save this sphere for later use
      SaveAnchor(newSphere);
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData) {
      // Debug.Log("RecordSceneController.OnPointerDown");
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData) {
      // Debug.Log("RecordSceneController.OnPointerUp");
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData) {
      // Debug.Log("RecordSceneController.OnPointerDragged");
    }
  #endregion IMixedRealityPointerHandler

  public void LoadAnchors() {
    // Re-Instantiate spheres from store
    if(anchorStore.anchors.Count > 0) {
      foreach(Vector3 anchorPosition in anchorStore.anchors) {
        Instantiate(sphere, anchorPosition, Quaternion.identity);
      }
    }
  }

  AnchorStore GetAnchors() {
    string serializedData = File.ReadAllText(Application.streamingAssetsPath + "/anchors.json");
    return JsonUtility.FromJson<AnchorStore>(serializedData);
  }
  
  private void SaveAnchor(GameObject anchor) {
    // Add anchor to anchor store
    anchorStore.anchors.Add(anchor.transform.position);
    // Save anchor store
    string serializedData = JsonUtility.ToJson(anchorStore, true);
    File.WriteAllText(Application.streamingAssetsPath + "/anchors.json", serializedData);
  }
  
  [Serializable]
  public class AnchorStore {
    public List<Vector3> anchors = new List<Vector3>();
  }
}