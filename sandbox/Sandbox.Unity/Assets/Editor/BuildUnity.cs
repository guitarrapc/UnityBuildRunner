using UnityEditor;

public class BuildUnity
{
    [MenuItem("MyTools/Windows Build With Postprocess")]
    public static void BuildGame()
    {
        var path = "artifacts/BuiltGame.exe";
        string[] levels = new string[] { "Assets/Scenes/SampleScene.unity" };

        // Build player.
        BuildPipeline.BuildPlayer(levels, path, BuildTarget.StandaloneWindows, BuildOptions.None);
    }
}
