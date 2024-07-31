using System.IO;
using UnityEditor;
using UnityEditor.Build;

public class BuildUnityMenu
{
    private static readonly string[] scenes = new[] { "Assets/Scenes/SampleScene.unity" };

    [MenuItem("Build/Build Windows IL2CPP Player")]
    public static void BuildWindowsIl2cppPlayer()
    {
        var path = "bin/WindowsPlayer/Build.il2cpp-player.exe";

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

        // Cleanup
        CleanupDontship(path);
    }

    [MenuItem("Build/Build Windows IL2CPP DedicatedServer")]
    public static void BuildWindowsIl2cppServer()
    {
        var path = "bin/WindowsServer/Build.il2cpp-server.exe";

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

        // Cleanup
        CleanupDontship(path);
    }

    [MenuItem("Build/Build Linux IL2CPP Player")]
    public static void BuildLinuxIl2cppPlayer()
    {
        var path = "bin/LinuxPlayer/Build.il2cpp-player.x86_64";

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

        // Cleanup
        CleanupDontship(path);
    }

    [MenuItem("Build/Build Linux IL2CPP DedicatedServer")]
    public static void BuildLinuxIl2cppServer()
    {
        var path = "bin/LinuxServer/Build.il2cpp-server.x86_64";

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

        // Cleanup
        CleanupDontship(path);
    }

    /// <summary>
    /// Enable IL2CPP ScriptingBackend
    /// </summary>
    /// <param name="buildTarget"></param>
    private static void EnableIl2Cpp(NamedBuildTarget buildTarget)
    {
        // IL2CPP Code Generation = OptimizeSize => faster build than OptimizeSpeed
        PlayerSettings.SetScriptingBackend(buildTarget, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(buildTarget, ManagedStrippingLevel.Minimal);
        PlayerSettings.SetIl2CppCompilerConfiguration(buildTarget, Il2CppCompilerConfiguration.Debug);
        PlayerSettings.SetIl2CppCodeGeneration(buildTarget, Il2CppCodeGeneration.OptimizeSize);
    }

    /// <summary>
    /// Enable Mono ScriptingBackend
    /// </summary>
    /// <param name="buildTarget"></param>
    private static void EnableMono(NamedBuildTarget buildTarget)
    {
        PlayerSettings.SetScriptingBackend(buildTarget, ScriptingImplementation.Mono2x);
    }

    /// <summary>
    /// Build Cleanup
    /// </summary>
    /// <param name="locationPathName"></param>
    private static void CleanupDontship(string locationPathName)
    {
        // Cleanup IL2CPP debugger information directories
        var dir = Path.GetDirectoryName(locationPathName);
        var fileName = Path.GetFileNameWithoutExtension(locationPathName);
        var removeTargets = new[] { $"{dir}/{fileName}_BackUpThisFolder_ButDontShipItWithYourGame" };

        foreach (var target in removeTargets)
        {
            if (!Directory.Exists(target)) continue;
            Directory.Delete(target, true);
        }
    }
}
