using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class OnBeforeBuild : IPreprocessBuildWithReport {
  public int callbackOrder => 0;

  public void OnPreprocessBuild(BuildReport report) {
    // Debug.Log("OnPreprocessBuild");
    //
    // // Delete all files in the Streaming Assets directory.
    // // TODO: Remove this is we want to include files in the build.
    // Debug.Log("Deleting all files in streaming assets folder");
    // var videoFiles = Directory.GetFiles(Application.streamingAssetsPath);
    // foreach (var videoFile in videoFiles) { File.Delete(videoFile); }
  }
}
