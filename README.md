[![dotnet-build](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-build.yaml/badge.svg)](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-build.yaml)
[![release](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-release.yaml/badge.svg)](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-release.yaml)
[![NuGet](https://img.shields.io/nuget/v/UnityBuildRunner.Core.svg?label=UnityBuildRunner.Core%20nuget)](https://www.nuget.org/packages/UnityBuildRunner.Core)
[![NuGet](https://img.shields.io/nuget/v/UnityBuildRunner.svg?label=UnityBuildRunner%20nuget)](https://www.nuget.org/packages/UnityBuildRunner)

## UnityBuildRunner

This tool enable you stdout Unity Build log on windows,  and control Timeout.

> **Note**: Linux/macOS don't need use this tool, just pass `-logfile -` argument to see log on stdout.

# Motivation

Windows Unity BatchBuild not provide Unity Build log StdOut option. This small tool provide realtime stdout build logs and build timeout control.

# Installation

You can install as .NET Global Tool.

```sh
dotnet tool install -g UnityBuildRunner
```

Or, you can install as nuget package library.

```sh
Install-Package UnityBuildRunner.Core
```

# Usage

## CLI (Help)

```
$ UnityBuildRunner --help
Usage: full [options...]

Options:
  -u, --unity-path <String>    Full Path to the Unity.exe (Default: )
  -t, --timeout <String>        (Default: 00:60:00)
```

## CLI (Basic)

All you need to do is pass unity's path as `-u UnityPath` and leave other argments as is.

> [Unity \- Manual: Command line arguments](https://docs.unity3d.com/2018.3/Documentation/Manual/CommandLineArguments.html)

If you are running Unity batch build like this.

  ```sh
  "C:\Program Files\Unity\Hub\Editor\2022.3.3f1\Editor\Unity.exe" -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\git\MRTKSample\Unity" -logfile "log.log" -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
  ```

Then, append `UnityBuildRunner --unity-path <UnityPath>` to existing command, that's all.

  ```sh
  UnityBuildRunner --unity-path "C:\Program Files\Unity\Hub\Editor\2022.3.3f1\Editor\Unity.exe" -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\git\MRTKSample\Unity" -logfile "log.log" -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
  ```

> **Note**: Another way to specifying UnityPath is via Environment Variable `UnityPath`.

  ```sh
  set UnityPath=C:\Program Files\Unity\Hub\Editor\2022.3.3f1\Editor\Unity.exe
  UnityBuildRunner -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\git\MRTKSample\Unity" -logfile "log.log" -executeMethod "HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
  ```

## Library (Basic)

You can use this library as your tool chain.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
// Parse settings from argument
ISettings settings = DefaultSettings.Parse(args, @"C:\Program Files\Unity\Hub\Editor\2022.3.3f1\Editor\Unity.exe");

// Run build
IBuilder builder = new Builder(settings, logger);
builder.BuildAsync(TimeSpan.FromMinutes(30), cts.Token);

// ExitCode respect Unity's ExitCode but may override if timeout or errorfilter detects.
Console.WriteLine(builder.ExitCode);
```

# TODO

- [x] dotnet global command
- [x] core logic as nuget
