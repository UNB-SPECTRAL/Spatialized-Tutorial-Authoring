using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Tools;
using UnityEngine;

/**
 * This class captures "Air Click"s events and instantiates a game object at that location.
 *
 * TODO: Tooltip positions are relative to the starting head pose. This is a problem since
 * it will load ToolTip that are in front of the user always in front when in World Space
 * its against a wall. We need to see if we can use the WorlLock managers or context to
 * help with finding the world position and not the unity position.
 */
public class RecordSceneController : InputSystemGlobalHandlerListener, IMixedRealityPointerHandler  {
  /***** Public Variables *****/
  // Represents a prefab which will be rendered at the air click location
  public GameObject tooltip;
  
  /***** Private Variables *****/
  private readonly TooltipStore _tooltipStore;
  
  /***** Constructor *****/
  public RecordSceneController() {
    // When instantiating this class, load all the anchors from storage.
    _tooltipStore = GetTooltips();
  }

  #region InputSystemGlobalHandlerListener Implementation
    protected override void RegisterHandlers() {
      // ReSharper disable once Unity.NoNullPropagation
      MixedRealityToolkit.Instance?.GetService<IMixedRealityInputSystem>()
        ?.RegisterHandler<IMixedRealityPointerHandler>(this);
    }

    protected override void UnregisterHandlers() {
      // ReSharper disable once Unity.NoNullPropagation
      MixedRealityToolkit.Instance?.GetService<IMixedRealityInputSystem>()
        ?.UnregisterHandler<IMixedRealityPointerHandler>(this);
    }
  #endregion InputSystemGlobalHandlerListener Implementation
  
  #region IMixedRealityPointerHandler
    /** Handles click events. */
    public void OnPointerClicked(MixedRealityPointerEventData eventData) {
      
      // Get the hit point
      Vector3 hitPoint = eventData.Pointer.Result.Details.Point;
      // Instantiate a tooltip at the hit point.
      GameObject newTooltip = Instantiate(tooltip, hitPoint, Quaternion.identity);
      // And world lock it.
      ToggleWorldAnchor twa = newTooltip.AddComponent<ToggleWorldAnchor>();
      twa.AlwaysLock = true;
      
      // Update the text shown on the tooltip
      newTooltip.GetComponent<ToolTip>().ToolTipText = "Tooltip #" + (_tooltipStore.tooltipDetailsList.Count + 1);

      // And save this sphere for later use
      SaveTooltip(newTooltip);
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
    // Re-Instantiate ToolTip from store
    if(_tooltipStore.tooltipDetailsList.Count > 0) {
      foreach(TooltipDetails toolTipDetails in _tooltipStore.tooltipDetailsList) {
        GameObject newToolTip = Instantiate(tooltip, toolTipDetails.position, Quaternion.identity);
        newToolTip.GetComponent<ToolTip>().ToolTipText = toolTipDetails.text;
      }
    }
  }

  TooltipStore GetTooltips() {
    string serializedData = File.ReadAllText(Application.streamingAssetsPath + "/tooltips.json");
    return JsonUtility.FromJson<TooltipStore>(serializedData);
  }
  
  private void SaveTooltip(GameObject toolTip) {
    // Get tooltip details from the Tooltip prefab
    string toolTipText = toolTip.GetComponent<ToolTip>().ToolTipText;
    Vector3 toolTipPosition = toolTip.transform.position;
    
    // Add toolTip details to the Tooltip Store
    TooltipDetails tooltipDetails = new TooltipDetails {
      text = toolTipText,
      position = toolTipPosition
    };
    _tooltipStore.tooltipDetailsList.Add(tooltipDetails);
    
    // Persist the TooltipStore
    string serializedData = JsonUtility.ToJson(_tooltipStore, true);
    File.WriteAllText(Application.streamingAssetsPath + "/tooltips.json", serializedData);
  }
  
  [Serializable]
  public class TooltipStore {
    public List<TooltipDetails> tooltipDetailsList = new List<TooltipDetails>();
  }
  
  /**
   * Class which stores tooltip details used to instantiate a Tooltip (
   *   https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/tooltip?view=mrtkunity-2022-05
   * ) prefab.
   */
  [Serializable]
  public class TooltipDetails {
    public string text;
    public Vector3 position;
  }
}