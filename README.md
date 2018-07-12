## UnityBuildRunner

## Motivation

Unity Batch Build for Windows still not provide Unity Build log StdOut option, wheres macOS can check stdout with `-logfile` + no argument.
This small tool provide realtime stdout logging for Jenkins, VSTS and others.

## Usage

```bash
set UnityPath=C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe
dotnet UnityBuildRunner.dll -quit -batchmode -buildTarget "WindowsStoreApps" -projectPath "C:\workspace\Source\Repos\MRTKSample\Unity" -logfile "log.log" -executeMethod "HoloToolkit.Unity.HoloToolkitCommands.BuildSLN"
```

macOS don't need use this tool, just pass non string with `-logfile` argument.

## TODO

- [ ] dotnet global command