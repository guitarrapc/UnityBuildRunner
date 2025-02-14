[![dotnet-build](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-build.yaml/badge.svg)](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-build.yaml)
[![release](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-release.yaml/badge.svg)](https://github.com/guitarrapc/UnityBuildRunner/actions/workflows/dotnet-release.yaml)
[![NuGet](https://img.shields.io/nuget/v/UnityBuildRunner.Core.svg?label=UnityBuildRunner.Core%20nuget)](https://www.nuget.org/packages/UnityBuildRunner.Core)
[![NuGet](https://img.shields.io/nuget/v/UnityBuildRunner.svg?label=UnityBuildRunner%20nuget)](https://www.nuget.org/packages/UnityBuildRunner)

## UnityBuildRunner

This tool enable you stdout Unity Build log on windows,  and control Timeout.

> **Note**: Linux/macOS don't need use this tool, just pass `-logfile -` argument to see log on stdout.

# Motivation

Windows Unity BatchBuild not provide Unity Build log standard output option. This small tool provide realtime stdout build log output and build timeout control.

# Installation

You can install as .NET Global Tool or .NET Tool.

```sh
# install as global tool
dotnet tool install -g UnityBuildRunner

# install to project's .config
dotnet tool install UnityBuildRunner
```

Or, you can install as nuget package library.

```sh
Install-Package UnityBuildRunner.Core
```

# Usage

## CLI (Help)

You can run installed tool via `UnityBuildRunner` (.NET Global Tool) or `dotnet UnityBuildRunner` (.NET Tool) command.

```sh
$ UnityBuildRunner --help
Usage: run [options...]

Options:
  -unity-path, --unity-path <String>    Full Path to the Unity executable, leave empty when use 'UnityPath' Environment variables instead. (Default: )
  -timeout, --timeout <String>          Timeout for Unity Build. (Default: 02:00:00)
```

## CLI (Basic)

All you need to do is pass unity's path as `-u UnityPath` and leave other argments as is.

> [Unity \- Manual: Command line arguments](https://docs.unity3d.com/2018.3/Documentation/Manual/CommandLineArguments.html)

If you are running Unity batch build like this.

```sh
$ "C:/Program Files/Unity/Hub/Editor/6000.0.12f1/Editor/Unity.exe" -quit -batchmode -nographics -silent-crashes -logfile "log.log" -buildTarget "StandaloneWindows64" -projectPath "C:/github/UnityBuildRunner/sandbox/Sandbox.Unity" -executeMethod "BuildUnity.BuildGame"
```

Then, append `UnityBuildRunner --unity-path <UnityPath>` or `dotnet UnityBuildRunner --unity-path <UnityPath>` to existing command, that's all.

  ```sh
  # .NET Global Tool
  $ UnityBuildRunner --unity-path "C:/Program Files/Unity/Hub/Editor/6000.0.12f1/Editor/Unity.exe" -quit -batchmode -nographics -silent-crashes -logfile "log.log" -buildTarget "StandaloneWindows64" -projectPath "C:/github/UnityBuildRunner/sandbox/Sandbox.Unity" -executeMethod "BuildUnity.BuildGame"

  # .NET Tool
  $ dotnet UnityBuildRunner --unity-path "C:/Program Files/Unity/Hub/Editor/6000.0.12f1/Editor/Unity.exe" -quit -batchmode -nographics -silent-crashes -logfile "log.log" -buildTarget "StandaloneWindows64" -projectPath "C:/github/UnityBuildRunner/sandbox/Sandbox.Unity" -executeMethod "BuildUnity.BuildGame"
  ```

> **Note**: Another way to specifying UnityPath is via Environment Variable `UnityPath`.

  ```sh
  # Environment Variables
  $ set UnityPath=C:/Program Files/Unity/Hub/Editor/6000.0.12f1/Editor/Unity.exe

  # .NET Global Tool
  $ UnityBuildRunner -quit -batchmode -nographics -silent-crashes -logfile "log.log" -buildTarget "StandaloneWindows64" -projectPath "C:/github/UnityBuildRunner/sandbox/Sandbox.Unity" -executeMethod "BuildUnity.BuildGame"

  # .NET Tool
  $ dotnet UnityBuildRunner -quit -batchmode -nographics -silent-crashes -logfile "log.log" -buildTarget "StandaloneWindows64" -projectPath "C:/github/UnityBuildRunner/sandbox/Sandbox.Unity" -executeMethod "BuildUnity.BuildGame"
  ```

## Library (Basic)

You can use as Library as well. This is sample code to run Unity Build.

```csharp
// Parse settings from argument
var settings = DefaultSettings.Parse(args, @"C:/Program Files/Unity/Hub/Editor/6000.0.12f1/Editor/Unity.exe", TimeSpan.FromMinutes(30));
using var cts = settings.CreateCancellationTokenSource();

// Run build
IBuilder builder = new DefaultBuilder(settings, logger);
await builder.BuildAsync(cts.Token);

// ExitCode is UnityBuildRunner and respect Unity's ExitCode.
Console.WriteLine(builder.ExitCode);
```

## FAQ

**What happen when passing `-logFile -` argument?**

Unity.exe not generate log file when passing `-` as log file name. Therefore UnityBuildRunner replace `-` to temporary log file `unitybuild.log` instead.
