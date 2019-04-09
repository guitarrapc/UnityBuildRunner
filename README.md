## UnityBuildRunner

[![CircleCI](https://circleci.com/gh/guitarrapc/UnityBuildRunner.svg?style=svg)](https://circleci.com/gh/guitarrapc/UnityBuildRunner) [![codecov](https://codecov.io/gh/guitarrapc/UnityBuildRunner/branch/master/graph/badge.svg)](https://codecov.io/gh/guitarrapc/UnityBuildRunner) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE) 

[![NuGet](https://img.shields.io/nuget/v/UnityBuildRunner.Core.svg?label=UnityBuildRunner.Core%20nuget)](https://www.nuget.org/packages/UnityBuildRunner.Core) [![NuGet](https://img.shields.io/nuget/v/UnityBuildRunner.svg?label=UnityBuildRunner%20nuget)](https://www.nuget.org/packages/UnityBuildRunner)


## Installation

### CLI

```bash
dotnet tool install -g UnityBuildRunner
```

### Library

```bash
Install-Package UnityBuildRunner.Core
```

## Usage

### CLI

```
Usage: UnityBuildRunner [-UnityPath|-unityPath|-u] [-timeout|-t 00:30:00] [-version] [-help] [args]
E.g., run this: UnityBuildRunner -u UNITYPATH -quit -batchmode -buildTarget WindowsStoreApps -projectPath HOLOLENS_UNITYPROJECTPATH -logfile log.log -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN
E.g., set UnityPath as EnvironmentVariable `UnityPath` & run this: UnityBuildRunner -quit -batchmode -buildTarget WindowsStoreApps -projectPath HOLOLENS_UNITYPROJECTPATH -logfile log.log -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN
```

Only you need to do is pass unity's path as `-u UnityPath` with unity build cli argements as normal.

> [Unity \- Manual: Command line arguments](https://docs.unity3d.com/2018.3/Documentation/Manual/CommandLineArguments.html)

You can run build by dotnet global tools so the installation is minimum cost. This tool will send you cli stdout on windows platform and also control Timeout.

There 2 choice to pass unity app's path.

1. pass unity path on the argument with parameter `-UnityPath or -u`.
1. via Environment Variables `UnityPath`.

```bash
UnityBuildRunner -UnityPath "C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe" -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\workspace\Source\Repos\MRTKSample\Unity" -logfile "log.log" -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
```

```bash
set UnityPath=C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe
UnityBuildRunner -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\workspace\Source\Repos\MRTKSample\Unity" -logfile "log.log" -executeMethod "HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
```

macOS don't need use this tool, just pass non string with `-logfile` argument.

### Library

Only you need to do is pass unity's path as `-u UnityPath` with unity build cli argements as normal.

```csharp
ISettings settings = new Settings();
settings.Parse(args, "path/to/unity/exe");
IBuilder builder = new Builder();
builder.BuildAsync(settings, TimeSpan.FromMinutes(30));
```

## Motivation

Unity Batch Build for Windows still not provide Unity Build log StdOut option, wheres macOS can check stdout with `-logfile` + no argument.
This small tool provide realtime stdout logging for Jenkins, VSTS and others.

## TODO

- [x] dotnet global command
- [x] core logic as nuget
