using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public class TutorialListController : MonoBehaviour {
  public GridObjectCollection gridObjectCollection;
  public GameObject           tutorialListButtonPrefab;
  public GameObject           backButton;
  
  void Start() {
    /*** Setup Back Button OnClick ***/
    backButton.GetComponent<ButtonConfigHelper>().OnClick.AddListener(() => {
      SceneController.Instance.OnTutorialListBackButtonPress();
    });
    
    /*** Setup Tutorial List Buttons ***/
    var tutorials = SceneController.TutorialStore.tutorials;
    
    // For each tutorial, create a button and add it to the list.
    foreach (var tutorial in tutorials) {
      var tutorialListButton = Instantiate(tutorialListButtonPrefab, gridObjectCollection.transform);

      var tutorialListButtonConfigHelper = tutorialListButton.GetComponent<ButtonConfigHelper>();
      
      tutorialListButtonConfigHelper.MainLabelText = tutorial.name;
      tutorialListButtonConfigHelper.OnClick.AddListener(() => SceneController.Instance.OnTutorialListButtonPress(tutorial));
    }
    
    // Update the GridObjectCollection to reflect the new number of buttons.
    gridObjectCollection.UpdateCollection();
  }
}
