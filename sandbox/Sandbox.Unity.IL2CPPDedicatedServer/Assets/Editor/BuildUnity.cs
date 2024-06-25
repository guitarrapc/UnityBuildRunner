using UnityEditor;

public class BuildUnity
{
    [MenuItem("MyTools/Build Windows IL2CPP Player")]
    public static void BuildGame()
    {
        var path = $"artifacts/{PlayerSettings.productName}.il2cpp-player.exe";
        string[] levels = new string[] { "Assets/Scenes/SampleScene.unity" };

        // Build player.
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Player;
        BuildPipeline.BuildPlayer(levels, path, BuildTarget.StandaloneWindows, BuildOptions.None);
    }

    [MenuItem("MyTools/Build Windows IL2CPP DedicatedServer")]
    public static void BuildGame()
    {
        var path = $"artifacts/{PlayerSettings.productName}.il2cpp-dedicatedserver.exe";
        string[] levels = new string[] { "Assets/Scenes/SampleScene.unity" };

        // Build player.
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
        BuildPipeline.BuildPlayer(levels, path, BuildTarget.StandaloneWindows, BuildOptions.None);
    }

    [MenuItem("MyTools/Build Linux IL2CPP Player")]
    public static void BuildGame()
    {
        var path = $"artifacts/{PlayerSettings.productName}.il2cpp-player";
        string[] levels = new string[] { "Assets/Scenes/SampleScene.unity" };

        // Build player.
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Player;
        BuildPipeline.BuildPlayer(levels, path, BuildTarget.StandaloneWindows, BuildOptions.None);
    }

    [MenuItem("MyTools/Build Linux IL2CPP DedicatedServer")]
    public static void BuildGame()
    {
        var path = $"artifacts/{PlayerSettings.productName}.il2cpp-dedicatedserver";
        string[] levels = new string[] { "Assets/Scenes/SampleScene.unity" };

        // Build player.
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
        BuildPipeline.BuildPlayer(levels, path, BuildTarget.StandaloneWindows, BuildOptions.None);
    }
}
