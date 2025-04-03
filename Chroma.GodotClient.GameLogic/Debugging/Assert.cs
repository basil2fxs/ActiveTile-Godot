using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MysticClue.Chroma.GodotClient.GameLogic.Debugging;

/// <summary>
/// Various guard conditions to check input parameter constraints, and other assumptions.
///
/// These should be used when we want to throw in Debug, but report and continue in Release.
/// For cases where it's not possible to continue, simply throw an exception.
/// </summary>
public static class Assert
{
    public static IErrorReporter? ErrorReporter { get; set; }

    /// <summary>
    /// Throw an exception only in Debug mode.
    /// </summary>
    [System.Diagnostics.Conditional("DEBUG")]
    private static void DebugThrow(Exception ex) { throw ex; }

    /// <summary>
    /// Report an exception.
    ///
    /// This may throw, print, send an RPC, or do nothing, depending on the current environment.
    /// </summary>
    /// <param name="ex"></param>
    public static void Report(Exception ex)
    {
        DebugThrow(ex);
        ErrorReporter?.Report(ex);
    }

    public static bool ReportNull([NotNullWhen(false)] object? param, [CallerArgumentExpression("param")] string paramName = "")
    {
        if (param == null)
        {
            Report(new ArgumentNullException(paramName));
            return true;
        }
        return false;
    }

    public static bool That(bool value, [CallerArgumentExpression("value")] string expression = "")
    {
        if (!value)
        {
            Report(new ArgumentException($"Assertion failed: {expression}"));
        }
        return value;
    }

    public static bool Min(ref double param, double min, [CallerArgumentExpression("param")] string paramName = "")
    {
        if (param < min)
        {
            Report(new ArgumentOutOfRangeException(paramName, $"{param} < {min}"));
            param = min;
            return false;
        }
        return true;
    }

    public static bool Min(ref float param, float min, [CallerArgumentExpression("param")] string paramName = "")
    {
        if (param < min)
        {
            Report(new ArgumentOutOfRangeException(paramName, $"{param} < {min}"));
            param = min;
            return false;
        }
        return true;
    }

    public static bool Min(ref int param, int min, [CallerArgumentExpression("param")] string paramName = "")
    {
        if (param < min)
        {
            Report(new ArgumentOutOfRangeException(paramName, $"{param} < {min}"));
            param = min;
            return false;
        }
        return true;
    }

    public static bool Max(ref double param, double max, [CallerArgumentExpression("param")] string paramName = "")
    {
        if (param > max)
        {
            Report(new ArgumentOutOfRangeException(paramName, $"{param} > {max}"));
            param = max;
            return false;
        }
        return true;
    }

    public static bool Max(ref float param, float max, [CallerArgumentExpression("param")] string paramName = "")
    {
        if (param > max)
        {
            Report(new ArgumentOutOfRangeException(paramName, $"{param} > {max}"));
            param = max;
            return false;
        }
        return true;
    }

    public static bool Max(ref int param, int max, [CallerArgumentExpression("param")] string paramName = "")
    {
        if (param > max)
        {
            Report(new ArgumentOutOfRangeException(paramName, $"{param} > {max}"));
            param = max;
            return false;
        }
        return true;
    }

    public static bool Clamp(ref double param, double min, double max, [CallerArgumentExpression("param")] string paramName = "")
    {
        return Min(ref param, min, paramName) && Max(ref param, max, paramName);
    }

    public static bool Clamp(ref float param, float min, float max, [CallerArgumentExpression("param")] string paramName = "")
    {
        return Min(ref param, min, paramName) && Max(ref param, max, paramName);
    }

    public static bool Clamp(ref int param, int min, int max, [CallerArgumentExpression("param")] string paramName = "")
    {
        return Min(ref param, min, paramName) && Max(ref param, max, paramName);
    }
}
