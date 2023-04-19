using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace UnityBuildRunner.Core.Tests;

public class ErrorFilterTest
{
    [Theory]
    [InlineData(
        "-----CompilerOutput:-stdout--exitcode: 1--compilationhadfailure: True--outfile: Temp/Assembly-CSharp.dll",
        "DisplayProgressNotification: Build Failed",
        "Error building Player because scripts had compiler errors",
        @"2018-11-05T00:53:44.2566426Z DisplayProgressNotification: Build Failed
        Error building Player because scripts had compiler errors
        (Filename:  Line: -1)
        Unloading 64 Unused Serialized files (Serialized files now loaded: 0)
        System memory in use before: 63.0 MB.
        System memory in use after: 63.4 MB.

        Unloading 47 unused Assets to reduce memory usage. Loaded Objects now: 5728.
        Total: 13.359500 ms (FindLiveObjects: 1.689200 ms CreateObjectMapping: 0.289900 ms MarkObjects: 11.349100 ms  DeleteObjects: 0.029600 ms)",
        "Compilation failed: 634 error(s), 0 warnings",
        "Assets/Externals/Plugins/Zenject/Source/Binding/Binders/NonLazyBinder.cs(10,16): error CS0246: The type or namespace name `IfNotBoundBinder' could not be found. Are you missing an assembly reference?",
        @"BatchMode: Unity has not been activated with a valid License. Could be a new activation or renewal...
        DisplayProgressbar: Unity license")]
    public void ContainsFilterMessage(params string[] inputs)
    {
        IErrorFilter errorFilter = new DefaultErrorFilter();
        var results = new List<string>();
        foreach (var input in inputs)
        {
            errorFilter.Filter(input, result => results.Add(result.MatchPattern));
        }

        results.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(
        "Unloading 64 Unused Serialized files (Serialized files now loaded: 0)",
        "System memory in use before: 63.0 MB.", "DisplayProgressbar: Unity Package Manager")]
    public void NotContainsFilterMessage(params string[] inputs)
    {
        IErrorFilter errorFilter = new DefaultErrorFilter();
        var results = new List<string>();
        foreach (var input in inputs)
        {
            errorFilter.Filter(input, result => results.Add(result.MatchPattern));
        }
        results.Should().BeEmpty();
    }
}
