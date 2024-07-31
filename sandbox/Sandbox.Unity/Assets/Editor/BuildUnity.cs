using UnityEditor;

public class BuildUnity
{
    [MenuItem("Build/Windows Build With Postprocess")]
    public static void BuildGame()
    {
        var path = "bin/BuiltGame.exe";
        string[] levels = new string[] { "Assets/Scenes/SampleScene.unity" };

        // Build player.
        BuildPipeline.BuildPlayer(levels, path, BuildTarget.StandaloneWindows, BuildOptions.None);
    }
}
