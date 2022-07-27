using UnityEngine;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.WorldLocking.Core;
using Microsoft.MixedReality.WorldLocking.Tools;

public class RecordSceneController : InputSystemGlobalHandlerListener, IMixedRealityPointerHandler {
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
      Debug.Log("RecordSceneController.OnPointerClicked");
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
    
}