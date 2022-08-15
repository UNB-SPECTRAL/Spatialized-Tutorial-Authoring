using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public class TutorialListController : MonoBehaviour {
  public GridObjectCollection gridObjectCollection;
  
  public GameObject           tutorialListButtonPrefab;

  public GameObject           backButton;

  void Start() {
    Debug.Log("TutorialListController.Start()");
    // Setup the back button once.
    backButton.GetComponent<ButtonConfigHelper>().OnClick.AddListener(() => {
      SceneController.Instance.OnTutorialListBackButtonPress();
    });
  }
  
  // Each time we enable this component, we need to rebuild the list of 
  // tutorial in case some were deleted.
  void OnEnable() {
    Debug.Log("TutorialListController.OnEnable()");
    // Delete all the children of the grid object collection.
    foreach (Transform child in gridObjectCollection.transform) {
      Destroy(child.gameObject);
    }
    
    var tutorials = SceneController.TutorialStore.tutorials;
    
    // For each tutorial, create a button and add it to the list.
    foreach (var tutorial in tutorials) {
      // Instantiate a new button
      var tutorialListButton = Instantiate(tutorialListButtonPrefab, gridObjectCollection.transform);
      
      // Name the button
      tutorialListButton.name = tutorial.name;
      
      var tutorialListButtonController = tutorialListButton.GetComponent<TutorialListButtonController>();
      
      // Update the tutorial button
      tutorialListButtonController.tutorialButtonConfigHelper.MainLabelText = tutorial.name;
      tutorialListButtonController.tutorialButtonConfigHelper.OnClick.AddListener(() => SceneController.Instance.OnTutorialListButtonPress(tutorial));
      
      // Update the delete button
      tutorialListButtonController.deleteTutorialButtonConfigHelper.OnClick.AddListener(() => {
        SceneController.Instance.OnTutorialDeleteButtonPress(tutorial);
        // Call the OnEnable method again to rebuild the list.
        OnEnable();
      }); ;
    }
    
    // Update the GridObjectCollection to reflect the new number of buttons.
    Debug.Log("TutorialListController.OnEnable(): Updating the GridObjectCollection");
    gridObjectCollection.UpdateCollection();

    StartCoroutine(Utilities.WaitForSecondsAnd(0.2f, () => {
      Debug.Log("TutorialListController.OnEnable(): Updating the GridObjectCollection In Coroutine");
      gridObjectCollection.UpdateCollection();
    }));
  }
}
