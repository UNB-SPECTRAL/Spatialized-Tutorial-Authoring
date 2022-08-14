using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public class TutorialListController : MonoBehaviour {
  public GridObjectCollection gridObjectCollection;
  public GameObject           tutorialListButtonPrefab;
  
  void Start() {
    var tutorials = SceneController.TutorialStore.tutorials;
    
    // For each tutorial, create a button and add it to the list.
    foreach (var tutorial in tutorials) {
      var tutorialListButton = Instantiate(tutorialListButtonPrefab, gridObjectCollection.transform);

      var tutorialListButtonConfigHelper = tutorialListButton.GetComponent<ButtonConfigHelper>();
      
      tutorialListButtonConfigHelper.MainLabelText = tutorial.name;
      tutorialListButtonConfigHelper.OnClick.AddListener(() => SceneController.Instance.OnTutorialButtonPress(tutorial));
    }
    
    // Update the GridObjectCollection to reflect the new number of buttons.
    gridObjectCollection.UpdateCollection();
  }
}
