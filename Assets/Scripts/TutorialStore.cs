using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    Debug.Log("HoloTuts: TutorialStore.Load()");

    // Generate the file path
    var filePath = Path.Combine(Application.streamingAssetsPath, FileName);
    Debug.Log("HoloTuts: filePath: " + filePath);

    // If the file path does not exist, create a new file and save it.
    if (!File.Exists(filePath)) {
      // If not, create one.
      Debug.Log("HoloTuts: Cannot find " + FileName + " in " + Application.streamingAssetsPath +
                ". Creating new file.");

      // If no file exists, create a new one.
      TutorialStore tutorialStore = new TutorialStore();

      // Save the newly created file
      // FIXME: When the file does not exist, it cannot be created we found. There is more research that is needed to 
      // validate this.
      tutorialStore.Save();

      Debug.Log("TutorialStore: Loaded");
      
      // Return the newly created file.
      return tutorialStore;
    }
    
    // If it does exist, load the file.
    Debug.Log("HoloTuts: Found " + FileName + " in " + Application.streamingAssetsPath + ". Loading file.");
    
    var serializedData = File.ReadAllText(filePath);
    
    Debug.Log("TutorialStore: Loaded");
    
    return JsonUtility.FromJson<TutorialStore>(serializedData);
  }

  /** Create a new tutorial and delete existing tutorial videos */
  public void CreateTutorial() {
    // Instantiate a new tutorial with a unique id
    int latestTutorialId                       = 0;
    if (tutorials.Count != 0) latestTutorialId = Int32.Parse(tutorials[tutorials.Count - 1].id.Split('_')[1]);
    Tutorial tutorial                          = new Tutorial(latestTutorialId + 1);

    // NOTE: For experiment #3, we will not be storing videos in streaming assets and thus don't need to delete them.
    // Delete existing tutorial videos that are prefixed with the new tutorial ID.
    // Since there is sometimes remaining videos files on the HoloLens from 
    // previous recordings on the Windows machine.
    // string[] videoFiles = Directory.GetFiles(
    //   Application.streamingAssetsPath, "tutorial_" + tutorial.id + "*.mp4"
    // );
    // foreach (string videoFile in videoFiles) {
    //   File.Delete(videoFile);
    // }

    tutorials.Add(tutorial);

    Save();
  }

  /** Create a new step given a pose for the latest tutorial. */
  public StepDetails CreateStep(Pose pose) {
    // Get the latest tutorial
    Tutorial latestTutorial = tutorials[tutorials.Count - 1];

    // Add the step to the latest tutorial
    StepDetails stepDetails = latestTutorial.AddStep(pose);

    // Persist the data to disk.
    Save();

    return stepDetails;
  }

  /** Given a stepID, find the tutorial which contains the step. */
  public Tutorial FindTutorialForStep(string stepId) {
    // For each tutorial
    foreach (Tutorial tutorial in tutorials) {
      // For each step in the tutorial
      foreach (var step in tutorial.steps) {
        // If the step ID matches the step ID we are looking for, return the tutorial.
        if (step.id == stepId) {
          return tutorial;
        }
      }
    }

    throw new Exception("Could not find tutorial for step " + stepId);
  }
  
  public Tutorial LastTutorial() {
    return tutorials[tutorials.Count - 1];
  }

  public StepDetails LastStep() {
    Tutorial lastTutorial = LastTutorial();
    
    int lastStepIndex = lastTutorial.steps.Count - 1;
    return lastStepIndex < 0 ? null : lastTutorial.steps[lastStepIndex];
  }

  public StepDetails UpdateLastStep(string key, object value) {
    // Get the latest tutorial
    Tutorial latestTutorial = LastTutorial();

    // Update the last step in the latest tutorial
    StepDetails latestStepDetails = latestTutorial.UpdateLastStep(key, value);

    // Persist the data to disk.
    Save();

    return latestStepDetails;
  }

  /** Deletes a tutorial, all its steps and videos */
  public void DeleteTutorial(string tutorialId) {
    // Find the tutorial with the given ID
    Tutorial tutorial = tutorials.Find(t => t.id == tutorialId);

    // Delete each step in the tutorial including it's video.
    foreach (var step in tutorial.steps.ToList()) {
      DeleteStep(step.id);
    }

    // Delete the tutorial from the list of tutorials.
    tutorials.RemoveAll(t => t.id == tutorialId);

    // Persist the data to disk.
    Save();
  }

  /** Delete a step and it's video */
  public void DeleteStep(string stepId) {
    // Iterate over each tutorial and attempt to delete the matching step.
    foreach (Tutorial tutorial in tutorials) {
      tutorial.DeleteStep(stepId);
    }

    // Persist the data to disk
    Save();
  }

  public void Reset() {
    tutorials = new List<Tutorial>();

    // Persist this change to the filesystem.
    Save();
  }

  private void Save() {
    Debug.Log("HoloTuts: TutorialStore.Save()");
    string filePath       = Path.Combine(Application.streamingAssetsPath, FileName);
    string serializedData = JsonUtility.ToJson(this, true);
    File.WriteAllText(filePath, serializedData);
  }

  [Serializable]
  public class Tutorial {
    public string            id; // e.g. "tutorial_1"
    public string            name; // e.g. "Tutorial 1"
    public List<StepDetails> steps; // e.g. []

    /** Constructor */
    public Tutorial(int id) {
      this.id = "tutorial_" + id;
      name    = "Tutorial " + id;
      steps   = new List<StepDetails>();
    }

    /** Add a step to a tutorial */
    public StepDetails AddStep(Pose pose) {
      Debug.Log("HoloTuts: TutorialStore.AddStep()");
        
      // Create the Step with the video file
      StepDetails step = new StepDetails(id, steps.Count + 1, pose);
      steps.Add(step); // Add it to the steps
      
      // Return the step for good measure
      return step;
    }

    public StepDetails UpdateLastStep(string key, object value) {
      // Get the last stepDetails
      var lastStepDetails = steps[steps.Count - 1];

      switch (key) {
        case "videoFilePath":
          Debug.LogError("Should not be updating `videoFilePath` via `UpdateLastStep`");
          string videoFilePath = (string) value;
          lastStepDetails.videoFilePath = videoFilePath;
          break;
        case "transcript":
          string transcript = (string) value;
          lastStepDetails.transcript = transcript;
          // When updating the transcript, also update the text so that we can
          // include 15 characters of transcript text in the UI.
          lastStepDetails.name += ": " + transcript.Substring(0, (Math.Min(15, transcript.Length))) + "...";
          break;
        case "globalPose":
          Pose globalPose = (Pose) value;
          lastStepDetails.globalPose = globalPose;
          break;
        default:
          throw new Exception("StepDetails does not have a key named " + key + " that can be set.");
      }

      return lastStepDetails;
    }

    /** Delete a step from a tutorial including its video */
    public void DeleteStep(string stepId) {
      // Find the step
      var step = steps.Find(s => s.id == stepId);

      // If it does not exist, bail.
      if (step == null) return;
      
      // NOTE: For Experiment #3, we don't store video files and thus don't need to delete them
      // Delete the video file.
      // FIXME: Update this, since we don't want to delete this file.
      // string videoFilePath = Path.Combine(Application.streamingAssetsPath, stepId + ".mp4");
      // if (File.Exists(videoFilePath)) {
      //   File.Delete(videoFilePath);
      // }

      // Delete the step from the list of steps.
      steps.RemoveAll(s => s.id == stepId);
      
      // NOTE: For Experiment #3, we won't allow deleting of steps.
      // Re-normalize the step data
      // Iterate over each step
      // for (int i = 0; i < steps.Count; i++) {
      //   // Check if the step id is correct
      //   string expectedStepId = id + "_step_" + (i + 1);
      //   string currentStepId  = steps[i].id;
      //   if (expectedStepId == currentStepId) continue; // If it's correct, continue.
      //
      //   // Otherwise, the step is out of sequence
      //   // Update the step id
      //   steps[i].id = expectedStepId;
      //   // Update the step name
      //   steps[i].name = "" + (i + 1);
      //   if (String.IsNullOrEmpty(steps[i].transcript) != true) {
      //     steps[i].name += ": " + steps[i].transcript.Substring(0, (Math.Min(15, steps[i].transcript.Length))) + "...";  
      //   }
      //   // Update the video file path
      //   if (String.IsNullOrEmpty(steps[i].videoFilePath) != true) {
      //     // FIXME: Update this is the we can get access to the `/Downloads/` folder...
      //     Debug.LogError("When deleting a Step, we need to rename the video file in the Downloads folder and not in the streaming assets folder.");
      //     string newVideoFilePath = Path.Combine(Application.streamingAssetsPath, expectedStepId + ".mp4");
      //     File.Move(
      //       Path.Combine(Application.streamingAssetsPath, currentStepId + ".mp4"),
      //       newVideoFilePath
      //     ); 
      //     // Rename the video file
      //     steps[i].videoFilePath = newVideoFilePath;
      //   }
      // }
    }

    [Serializable]
    public class StepDetails {
      public string id; // e.g. "tutorial_1_step_1"
      public string name; // e.g. "1: something..." 
      public Pose   globalPose;
      public string videoFilePath;
      public string transcript;

      /** Constructor */
      public StepDetails(string tutorialId, int id, Pose pose) {
        this.id       = tutorialId + "_step_" + id;
        name          = GetStepName(id);
        globalPose    = pose;
        videoFilePath = Path.Combine(Application.streamingAssetsPath, "Step-" + id + ".mp4");
      }

      private string GetStepName(int stepId) {
        switch (stepId) {
          case 1 : return "Start Here";
          case 2 : return "Grab water bucket 1";
          case 3 : return "6*5*4*3*2\nMonstera-19";
          case 4 : return "6*5*4*3*2\nMonstera-1";
          case 5 : return "6*5*4*3*2\nMonstera-25";
          case 6 : return "6*5*4*3*2\nMonstera-6";
          case 7 : return "6*5*4*3*2\nMonstera-24";
          case 8 : return "2*6*5*4*3\nDraceana-20";
          case 9 : return "2*6*5*4*3\nDraceana-9";
          case 10: return "2*6*5*4*3\nDraceana-17";
          case 11: return "Grab water bucket 2";
          case 12: return "2*6*5*4*3\nDraceana-2";
          case 13: return "2*6*5*4*3\nDraceana-22"; 
          case 14: return "3*2*6*5*4\nSansevieria-18";
          case 15: return "3*2*6*5*4\nSansevieria-10";
          case 16: return "3*2*6*5*4\nSansevieria-5";
          case 17: return "3*2*6*5*4\nSansevieria-21";
          case 18: return "3*2*6*5*4\nSansevieria-15";
          case 19: return "4*3*2*6*5\nFicus-13";
          case 20: return "4*3*2*6*5\nFicus-7";
          case 21: return "4*3*2*6*5\nFicus-12";
          case 22: return "4*3*2*6*5\nFicus-3";
          case 23: return "Grab water bucket 3";
          case 24: return "4*3*2*6*5\nFicus-16";
          case 25: return "5*4*3*2*6\nAnthurium-23";
          case 26: return "5*4*3*2*6\nAnthurium-11";
          case 27: return "5*4*3*2*6\nAnthurium-4";
          case 28: return "5*4*3*2*6\nAnthurium-14";
          case 29: return "5*4*3*2*6\nAnthurium-8";
          case 30: return "Put any waste strip and empty bins here";
          default: return "Missing Text";
        }
      }
    }
  }
}