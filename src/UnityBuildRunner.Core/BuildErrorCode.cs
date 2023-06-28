using System;
using System.Reflection;

namespace UnityBuildRunner.Core;

internal enum BuildErrorCode
{
    [ErrorExitCode(0)]
    Success,
    [ErrorExitCode(1)]
    UnityProcessError,
    [ErrorExitCode(9900)]
    BuildErrorMessageFound,
    [ErrorExitCode(9901)]
    ProcessNull,
    [ErrorExitCode(9902)]
    ProcessImmediatelyExit,
    [ErrorExitCode(9903)]
    ProcessTimeout,
    [ErrorExitCode(9904)]
    OperationCancelled,
    [ErrorExitCode(9905)]
    LogFileNotFound,
    [ErrorExitCode(9999)]
    OtherError,
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class ErrorExitCodeAttribute : Attribute
{
    public int ExitCode { get; }
    public ErrorExitCodeAttribute(int exitCode) => ExitCode = exitCode;
}

internal static class EnumExtensions
{
    /// <summary>
    /// Obtain T Attribute from Enum
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="e"></param>
    /// <returns></returns>
    public static T? GetAttrubute<T>(this Enum e) where T : Attribute
    {
        var field = e.GetType().GetField(e.ToString());
        if (field is not null && field.GetCustomAttribute<T>() is T att)
        {
            return att;
        }
        return null;
    }
}
