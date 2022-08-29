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
    try {
      var filePath       = Path.Combine(Application.streamingAssetsPath, FileName);
      var serializedData = File.ReadAllText(filePath);
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

  /** Create a new tutorial and delete existing tutorial videos */
  public void CreateTutorial() {
    // Instantiate a new tutorial with a unique id
    int latestTutorialId                       = 0;
    if (tutorials.Count != 0) latestTutorialId = Int32.Parse(tutorials[tutorials.Count - 1].id.Split('_')[1]);
    Tutorial tutorial                          = new Tutorial(latestTutorialId + 1);

    // Delete existing tutorial videos that are prefixed with the new tutorial ID.
    // Since there is sometimes remaining videos files on the HoloLens from 
    // previous recordings on the Windows machine.
    string[] videoFiles = Directory.GetFiles(
      Application.streamingAssetsPath, "tutorial_" + tutorial.id + "*.mp4"
    );
    foreach (string videoFile in videoFiles) {
      File.Delete(videoFile);
    }

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

  public StepDetails UpdateLastStep(string key, object value) {
    // Get the latest tutorial
    Tutorial latestTutorial = tutorials[tutorials.Count - 1];

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
      StepDetails step = new StepDetails(id, steps.Count + 1, pose);
      steps.Add(step);
      return step;
    }

    public StepDetails UpdateLastStep(string key, object value) {
      // Get the last stepDetails
      var lastStepDetails = steps[steps.Count - 1];

      switch (key) {
        case "videoFilePath":
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

      // If it does not exist, bail out.
      if (step == null) return;

      // Delete the video file.
      string videoFilePath = Path.Combine(Application.streamingAssetsPath, stepId + ".mp4");
      if (File.Exists(videoFilePath)) {
        File.Delete(videoFilePath);
      }

      // Delete the step from the list of steps.
      steps.RemoveAll(s => s.id == stepId);

      // Re-normalize the step data
      // Iterate over each step
      for (int i = 0; i < steps.Count; i++) {
        // Check if the step id is correct
        string expectedStepId = id + "_step_" + (i + 1);
        string currentStepId  = steps[i].id;
        if (expectedStepId == currentStepId) continue; // If it's correct, continue.

        // Otherwise, the step is out of sequence
        // Update the step id
        steps[i].id = expectedStepId;
        // Update the step name
        steps[i].name = "Step " + (i + 1) + ":" + steps[i].name.Split(':')[1];
        // Update the video file path
        string newVideoFilePath = Path.Combine(Application.streamingAssetsPath, expectedStepId + ".mp4");
        File.Move(
          Path.Combine(Application.streamingAssetsPath, currentStepId + ".mp4"),
          newVideoFilePath
        ); // Rename the video file
        steps[i].videoFilePath = newVideoFilePath;
      }
    }

    [Serializable]
    public class StepDetails {
      public string id; // e.g. "tutorial_1_step_1"
      public string name; // e.g. "Step 1: something..." 
      public Pose   globalPose;
      public string videoFilePath;
      public string transcript;

      /** Constructor */
      public StepDetails(string tutorialId, int id, Pose pose) {
        this.id    = tutorialId + "_step_" + id;
        name       = "" + id;
        globalPose = pose;
      }
    }
  }
}