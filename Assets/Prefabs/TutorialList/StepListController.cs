using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public class StepListController : MonoBehaviour {
  public GridObjectCollection gridObjectCollection;
  
  public GameObject           tutorialListButtonPrefab;

  public GameObject           backButton;

  void Start() {
    Debug.Log("TutorialListController.Start()");
    // Setup the back button once.
    backButton.GetComponent<ButtonConfigHelper>().OnClick.AddListener(() => {
      SceneController.Instance.OnStopTutorialButtonPress();
    });
  }

  // Each time we enable this component, we need to rebuild the list of 
  // steps in case some were deleted.
  public void OnEnable() {
    Debug.Log("StepListController.OnEnable()");
    // Delete all the children of the grid object collection.
    foreach (Transform child in gridObjectCollection.transform) {
      Destroy(child.gameObject);
    }
    
    // The steps are of the last tutorial
    var steps = SceneController.TutorialStore.tutorials[SceneController.TutorialStore.tutorials.Count - 1].steps;
    
    // For each steps, create a button and add it to the list.
    foreach (var step in steps) {
      // Instantiate a new button
      var tutorialListButton = Instantiate(tutorialListButtonPrefab, gridObjectCollection.transform);
      
      // Name the button
      tutorialListButton.name = step.name;
      
      var tutorialListButtonController = tutorialListButton.GetComponent<TutorialListButtonController>();
      
      // Update the tutorial button
      tutorialListButtonController.tutorialButtonConfigHelper.MainLabelText = step.name;

      // Update the delete button
      tutorialListButtonController.deleteTutorialButtonConfigHelper.OnClick.AddListener(() => {
        SceneController.Instance.OnDeleteStepButtonPress(step);
      }); ;
    }
    
    // Update the GridObjectCollection to reflect the new number of buttons.
    Debug.Log("StepListController.OnEnable(): Updating the GridObjectCollection");
    gridObjectCollection.UpdateCollection();

    StartCoroutine(Utilities.WaitForSecondsAnd(0.2f, () => {
      Debug.Log("SteplListController.OnEnable(): Updating the GridObjectCollection In Coroutine");
      gridObjectCollection.UpdateCollection();
    }));
  }
}
