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

public class DefaultErrorFilter : IErrorFilter
{
    private readonly IReadOnlyList<Regex> regexes;

    public DefaultErrorFilter()
    {
        var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled;
        var errorFilters = new[]
        {
            "Compilation failed",
            "compilationhadfailure: True",
            "DisplayProgressNotification: Build Failed",
            @"error CS\d+",
            "Error building Player because scripts had compiler errors",
            "Multiple Unity instances cannot open the same project.",
            "Unity has not been activated",
        };
        regexes = errorFilters.Select(x => new Regex(x, options)).ToArray();
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
