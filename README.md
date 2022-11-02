## UnityBuildRunner

[![dotnet-build](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-build.yaml/badge.svg)](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-build.yaml) [![release](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-release.yaml/badge.svg)](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-release.yaml)

[![NuGet](https://img.shields.io/nuget/v/UnityBuildRunner.Core.svg?label=UnityBuildRunner.Core%20nuget)](https://www.nuget.org/packages/UnityBuildRunner.Core) [![NuGet](https://img.shields.io/nuget/v/UnityBuildRunner.svg?label=UnityBuildRunner%20nuget)](https://www.nuget.org/packages/UnityBuildRunner)

This tool enable you stdout Unity Build log on windows,  and control Timeout.

> **Note**: Linux/macOS don't need use this tool, just pass non string with `-logfile` argument to see log on stdout.

# Motivation

Unity Batch Build for Windows still not provide Unity Build log StdOut option, wheres macOS can check stdout with `-logfile` + no argument.
This small tool provide realtime stdout logging for Jenkins, VSTS and others.

# Installation

Install with .NET Global Tool is the minimum cost.

```bash
dotnet tool install -g UnityBuildRunner
```

Also provided as a library.

```bash
Install-Package UnityBuildRunner.Core
```

# Usage

```
$ UnityBuildRunner --help
Usage: full [options...]

Options:
  -u, --unity-path <String>    Full Path to the Unity.exe (Default: )
  -t, --timeout <String>        (Default: 00:60:00)
```

## Basic

All you need to do is pass unity's path as `-u UnityPath` and leave other argments as is.

> [Unity \- Manual: Command line arguments](https://docs.unity3d.com/2018.3/Documentation/Manual/CommandLineArguments.html)

If you are running Unity batch build like this.

```bash
"C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe" -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\workspace\Source\Repos\MRTKSample\Unity" -logfile "log.log" -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
```

Then, append `UnityBuildRunner --unity-path ` to existing command, that's all.

```bash
UnityBuildRunner --unity-path "C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe" -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\workspace\Source\Repos\MRTKSample\Unity" -logfile "log.log" -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
```

## Specifying UnityPath

You can pass Unity Path via Argument or EnvironmentVariables.

1. Argument: Add argument `--unity-path <PATH_TO_UNITY>` (or `-u <PATH_TO_UNITY>` for short parameter).
1. Environment Variable: Set `UnityPath` Environment Variable. UnityBuildRunner automatically load it if argument is not specified.

**Examples**

Pass Unity Path via Argument `--unity-path`.

```bash
UnityBuildRunner --unity-path "C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe" -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\workspace\Source\Repos\MRTKSample\Unity" -logfile "log.log" -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
```

Pass Unity Path via EnvironmentVariables `UnityPath`.

```bash
set UnityPath=C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe
UnityBuildRunner -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\workspace\Source\Repos\MRTKSample\Unity" -logfile "log.log" -executeMethod "HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
```

## Library

You can use this library as your tool chain.

```csharp
ISettings settings = Settings.Parse(args, "path/to/unity/exe");
IBuilder builder = new Builder();
builder.BuildAsync(settings, TimeSpan.FromMinutes(30));
```

# TODO

- [x] dotnet global command
- [x] core logic as nuget
