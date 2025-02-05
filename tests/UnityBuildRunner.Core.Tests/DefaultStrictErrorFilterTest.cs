namespace UnityBuildRunner.Core.Tests;

public class DefaultStrictErrorFilterTest
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
    public void DetectCSharpCompileError(params string[] inputs)
    {
        var errorFilter = new DefaultStrictErrorFilter();
        var results = new List<string>();
        foreach (var input in inputs)
        {
            errorFilter.Filter(input, result => results.Add(result.MatchPattern));
        }

        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("Multiple Unity instances cannot open the same project.")]
    [InlineData("Unity has not been activated")]
    public void DetectUnityError(params string[] inputs)
    {
        var errorFilter = new DefaultStrictErrorFilter();
        var results = new List<string>();
        foreach (var input in inputs)
        {
            errorFilter.Filter(input, result => results.Add(result.MatchPattern));
        }
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData(
        "Compiling shader \"Shader Graphs/UrpFoo\" pass \"\" (vp)",
        "    Full variant space:         2",
        "    After settings filtering:   2",
        "    After built-in stripping:   2",
        "    After scriptable stripping: 2",
        "    Processed in 0.02 seconds",
        "    starting compilation...",
        "    finished in 0.22 seconds. Local cache hits 0 (0.00s CPU time), remote cache hits 0 (0.00s CPU time), compiled 2 variants (0.42s CPU time), skipped 0 variants",
        "    Prepared data for serialisation in 0.00s",
        "Serialized binary data for shader Shader Graphs/UrpTriplanar in 0.00s",
        "    glcore (total internal programs: 21, unique: 21)",
        "    vulkan (total internal programs: 34, unique: 34)",
        "Shader error in 'Shader Graphs/UrpFoo': Compilation failed (other error) 'out of memory during compilation")]
    public void SkipShaderError(params string[] inputs)
    {
        var errorFilter = new DefaultStrictErrorFilter();
        var results = new List<string>();
        foreach (var input in inputs)
        {
            errorFilter.Filter(input, result => results.Add(result.MatchPattern));
        }
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData(
        "Unloading 64 Unused Serialized files (Serialized files now loaded: 0)",
        "System memory in use before: 63.0 MB.", "DisplayProgressbar: Unity Package Manager")]
    public void SkipNormalMessage(params string[] inputs)
    {
        var errorFilter = new DefaultStrictErrorFilter();
        var results = new List<string>();
        foreach (var input in inputs)
        {
            errorFilter.Filter(input, result => results.Add(result.MatchPattern));
        }
        Assert.Empty(results);
    }
}
