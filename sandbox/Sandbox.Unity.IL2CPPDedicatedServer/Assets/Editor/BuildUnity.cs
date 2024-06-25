using UnityEditor;
using UnityEditor.Build;

public class BuildUnity
{
    [MenuItem("MyTools/Build Windows IL2CPP Player")]
    public static void BuildWindowsIl2cppPlayer()
    {
        var path = $"artifacts/Build.il2cpp-player.exe";
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

        var buildPlayerOptions = new BuildPlayerOptions();

        // Platform & Architecture
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Player;

        // IL2CPP
        EnableIl2Cpp(NamedBuildTarget.Standalone);

        // Path and Scenes
        buildPlayerOptions.locationPathName = path;
        buildPlayerOptions.scenes = scenes;

        // Build
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("MyTools/Build Windows IL2CPP DedicatedServer")]
    public static void BuildWindowsIl2cppServer()
    {
        var path = $"artifacts/Build.il2cpp-dedicatedserver.exe";
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

        var buildPlayerOptions = new BuildPlayerOptions();

        // Platform & Architecture
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;

        // IL2CPP
        EnableIl2Cpp(NamedBuildTarget.Server);

        // Path and Scenes
        buildPlayerOptions.locationPathName = path;
        buildPlayerOptions.scenes = scenes;

        // Build
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("MyTools/Build Linux IL2CPP Player")]
    public static void BuildLinuxIl2cppPlayer()
    {
        var path = $"artifacts/Build.il2cpp-player.x86_64";
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

        var buildPlayerOptions = new BuildPlayerOptions();

        // Platform & Architecture
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Player;

        // IL2CPP
        EnableIl2Cpp(NamedBuildTarget.Standalone);

        // Path and Scenes
        buildPlayerOptions.locationPathName = path;
        buildPlayerOptions.scenes = scenes;

        // Build
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("MyTools/Build Linux IL2CPP DedicatedServer")]
    public static void BuildLinuxIl2cppServer()
    {
        var path = $"artifacts/Build.il2cpp-dedicatedserver.x86_64";
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

        var buildPlayerOptions = new BuildPlayerOptions();

        // Platform & Architecture
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;

        // IL2CPP
        EnableIl2Cpp(NamedBuildTarget.Server);

        // Path and Scenes
        buildPlayerOptions.locationPathName = path;
        buildPlayerOptions.scenes = scenes;

        // Build
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    private static void EnableIl2Cpp(NamedBuildTarget buildTarget)
    {
        // IL2CPP Code Generation = Faster runtime
        PlayerSettings.SetIl2CppCodeGeneration(buildTarget, Il2CppCodeGeneration.OptimizeSpeed);
    }
}
