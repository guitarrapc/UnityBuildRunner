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

You can run build by dotnet global tools. 

There 2 choice to pass unity app's path, pass unity path on the argument or via Environment Variables.

```bash
UnityBuildRunner -UnityPath "C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe" -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\workspace\Source\Repos\MRTKSample\Unity" -logfile "log.log" -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
```

```bash
set UnityPath=C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe
UnityBuildRunner -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\workspace\Source\Repos\MRTKSample\Unity" -logfile "log.log" -executeMethod "HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
```

macOS don't need use this tool, just pass non string with `-logfile` argument.

## Motivation

Unity Batch Build for Windows still not provide Unity Build log StdOut option, wheres macOS can check stdout with `-logfile` + no argument.
This small tool provide realtime stdout logging for Jenkins, VSTS and others.

## TODO

- [x] dotnet global command
- [x] core logic as nuget
