using System;

namespace UnityBuildRunner.Core;
internal class BuildErrorFoundException : Exception
{
    public override string Message => message;
    private string message;
    public BuildErrorFoundException(string message)
    {
        this.message = message;
    }
}
