using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityBuildRunner.Core;

public interface IErrorFilter
{
    /// <summary>
    /// Filter value and treat on match
    /// </summary>
    /// <param name="value"></param>
    /// <param name="onMatch"></param>
    public void Filter(string value, Action<ErrorFilterResult> onMatch);
}

public record ErrorFilterResult(string Message, string MatchPattern);

/// <summary>
/// Error filter without Shader errors
/// </summary>
public class DefaultErrorFilter : IErrorFilter
{
    private readonly IReadOnlyList<Regex> regexes;

    public DefaultErrorFilter()
    {
        var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled;
        regexes = ErrorMessages.CsharpErrors
            .Concat(ErrorMessages.UnityErrors)
            .Select(x => new Regex(x, options))
            .ToArray();
    }

    public void Filter(string message, Action<ErrorFilterResult> onMatch)
    {
        foreach (var regex in regexes)
        {
            if (regex.IsMatch(message))
            {
                onMatch.Invoke(new ErrorFilterResult(message, regex.ToString()));
            }
        }
    }
}

/// <summary>
/// Error filter include Shader errors
/// </summary>
public class DefaultStrictErrorFilter : IErrorFilter
{
    private readonly IReadOnlyList<Regex> regexes;

    public DefaultStrictErrorFilter()
    {
        var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled;
        regexes = ErrorMessages.CsharpErrors
            .Concat(ErrorMessages.ShaderErrors)
            .Concat(ErrorMessages.UnityErrors)
            .Select(x => new Regex(x, options))
            .ToArray();
    }

    public void Filter(string message, Action<ErrorFilterResult> onMatch)
    {
        foreach (var regex in regexes)
        {
            if (regex.IsMatch(message))
            {
                onMatch.Invoke(new ErrorFilterResult(message, regex.ToString()));
            }
        }
    }
}

file static class ErrorMessages
{
    /// <summary>
    /// C# Error pattern
    /// </summary>
    public static readonly string[] CsharpErrors = new[]
    {
        "compilationhadfailure: True",
        "DisplayProgressNotification: Build Failed",
        @"error CS\d+",
        "Error building Player because scripts had compiler errors",
    };
    /// <summary>
    /// Shader Error pattern
    /// </summary>
    public static readonly string[] ShaderErrors = new[]
    {
        // Shader Error
        "Compilation failed",
    };
    /// <summary>
    /// Unity Error pattern
    /// </summary>
    public static readonly string[] UnityErrors = new[]
    {
        // Unity can open single Unity.exe process for same project path.
        "Multiple Unity instances cannot open the same project.",
        // License should be activated before build.
        "Unity has not been activated",
    };
}
