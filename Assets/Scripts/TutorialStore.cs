using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/*** Import Helpers ***/
using StepDetails = TutorialStore.Tutorial.StepDetails;

/** Serializable class for storing Tutorials and their Steps data. */
[Serializable]
public class TutorialStore {
  private const string FileName = "tutorials.json"; // file system name
  
  public List<Tutorial> tutorials = new List<Tutorial>(); // list of tutorials
  
  /** Maybe loads the TutorialStore from disk or instantiates a new one. */
  public static TutorialStore Load() {
    try {
      var        filePath       = Path.Combine(Application.streamingAssetsPath, FileName);
      var        serializedData = File.ReadAllText(filePath);
      Debug.Log("TutorialStore: Loaded");
      return JsonUtility.FromJson<TutorialStore>(serializedData);
    }
    catch (FileNotFoundException) {
      Debug.Log("Cannot find " + FileName + " in " + Application.streamingAssetsPath + ". Creating new file.");
      
      // If no file exists, create a new one.
      TutorialStore tutorialStore = new TutorialStore();
      
      // Save the newly created file
      tutorialStore.Save();
      
      Debug.Log("TutorialStore: Loaded");
      // Return the newly created file.
      return tutorialStore;
    }
  }

  /** Create a new tutorial */
  public void AddTutorial() {
    // Instantiate a new tutorial using the existing count at the ID + 1.
    Tutorial tutorial = new Tutorial(tutorials.Count + 1);
    
    tutorials.Add(tutorial);
    
    Save();
  }
  
  /** Create a new step for the latest tutorial. */
  public StepDetails AddStep(Pose pose) {
    Debug.Log("TutorialStore.AddStep()");
    
    // Get the latest tutorial
    Tutorial latestTutorial = tutorials[tutorials.Count - 1];
    
    // Add the step to the latest tutorial
    StepDetails stepDetails = latestTutorial.AddStep(pose);
    
    // Persist the data to disk.
    Save();
    
    return stepDetails;
  }
  
  public StepDetails UpdateLastStep(string key, string value) {
    // Get the latest tutorial
    Tutorial latestTutorial = tutorials[tutorials.Count - 1];
    
    // Update the last step in the latest tutorial
    StepDetails latestStepDetails = latestTutorial.UpdateLastStep(key, value);
    
    // Persist the data to disk.
    Save();

    return latestStepDetails;
  }
  
  public Tutorial GetTutorial(int tutorialId) {
    return tutorials.Find(tutorial => tutorial.id == tutorialId);
  }
  
  public void Reset() {
    tutorials = new List<Tutorial>();
      
    // Persist this change to the filesystem.
    Save();
  }

  private void Save() {
    string filePath       = Path.Combine(Application.streamingAssetsPath, FileName);
    string serializedData = JsonUtility.ToJson(this, true);
    File.WriteAllText(filePath, serializedData);
  }
  
  [Serializable]
  public class Tutorial {
    public int               id; // e.g. 1
    public string            name; // e.g. Tutorial 1
    public List<StepDetails> steps; // e.g. []
    
    /** Constructor */
    public Tutorial(int id) {
      this.id = id;
      name = "Tutorial " + id;
      steps = new List<StepDetails>();
    }
    
    /** Add a step to a tutorial */
    public StepDetails AddStep(Pose pose) {
      StepDetails step = new StepDetails(steps.Count + 1, pose);
      steps.Add(step);
      return step;
    }
    
    public StepDetails UpdateLastStep(string key, string value) {
      // Get the last stepDetails
      var lastStepDetails = steps[steps.Count - 1];

      switch (key) {
        case "videoFilePath":
          lastStepDetails.videoFilePath = value;
          break;
        case "transcript":
          lastStepDetails.transcript = value;
          // When updating the transcript, also update the text so that we can
          // include 15 characters of transcript text in the UI.
          lastStepDetails.name += ": " + value.Substring(0, (Math.Min(15, value.Length))) + "...";
          break;
        default:
          throw new Exception("StepDetails does not have a key named " + key + " that can be set.");
      }

      return lastStepDetails;
    }
    
    [Serializable]
    public class StepDetails {
      public int    id;   // e.g. 1
      public string name; // e.g. Step 1
      public Pose   globalPose;
      public string videoFilePath;
      public string transcript;
      
      /** Constructor */
      public StepDetails(int id, Pose pose) {
        this.id = id;
        name = "Step " + id;
        globalPose = pose;
      }
    }
  }
}
