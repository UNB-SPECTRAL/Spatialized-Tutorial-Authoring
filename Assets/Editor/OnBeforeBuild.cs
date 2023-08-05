using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor
{
  public class OnBeforeBuild : IPreprocessBuildWithReport {
    public int callbackOrder => 0;

    /**
   * NOTE: When first publishing this application on a new HoloLens, we should make sure to include the `tutorials.json`
   * file since we can't write to a file that does not exist. We may also not able to create such file. We faced a few
   * issues when we were deleting the file during build, and having no file on the HoloLens device.
   */
    public void OnPreprocessBuild(BuildReport report) {
      Debug.Log("HoloTuts: OnPreprocessBuild()");
    
      /*
       * The reason we are deleting the `tutorials.json` file is so that local tutorials don't overwrite tutorials on the
       * device. This is important since a lot of time is taken to chose the correct world locking positions for each step.
       */
      Debug.Log("HoloTuts: Deleting `tutorials.json` from StreamingAssets");
      var tutorialsFilePath = Path.Combine(Application.streamingAssetsPath, "tutorials.json");
      if (File.Exists(tutorialsFilePath)) {
        try {
          File.Delete(tutorialsFilePath);
          Debug.Log("HoloTuts: Successfully deleted `tutorials.json` file from StreamingAssets");
        }
        catch (IOException e) {
          Debug.LogError("HoloTuts: Cannot delete `tutorials.json` file from StreamingAssets");
          Debug.LogError(e);
        }
      }
      else {
        Debug.Log("HoloTuts: `tutorials.json` file does not exist in StreamingAssets");
      }
    
      // NOTE: Purposefully commenting this out since we want to include video files in the build, but not tutorials
      // Debug.Log("OnPreprocessBuild");
      //
      // // Delete all files in the Streaming Assets directory.
      // // TODO: Remove this is we want to include files in the build.
      // Debug.Log("Deleting all files in streaming assets folder");
      // var videoFiles = Directory.GetFiles(Application.streamingAssetsPath);
      // foreach (var videoFile in videoFiles) { File.Delete(videoFile); }
    }
  }
}
