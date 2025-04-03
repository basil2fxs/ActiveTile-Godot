using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using System.Runtime.CompilerServices;

namespace MysticClue.Chroma.GodotClient.Debugging;

/// <summary>
/// Godot-specific methods, e.g. that take Vector2.
/// </summary>
public static class GodotAssert
{
    public static void Clamp(ref Vector2 param, Vector2 min, Vector2 max, [CallerArgumentExpression("param")] string paramName = "")
    {
        Min(ref param, min, paramName);
        Max(ref param, max, paramName);
    }

    public static void Clamp(ref Vector2I param, Vector2I min, Vector2I max, [CallerArgumentExpression("param")] string paramName = "")
    {
        Min(ref param, min, paramName);
        Max(ref param, max, paramName);
    }

    public static void Min(ref Vector2 param, Vector2 min, [CallerArgumentExpression("param")] string paramName = "")
    {
        Assert.Min(ref param.X, min.X, $"{paramName}.X");
        Assert.Min(ref param.Y, min.Y, $"{paramName}.Y");
    }

    public static void Min(ref Vector2I param, Vector2I min, [CallerArgumentExpression("param")] string paramName = "")
    {
        Assert.Min(ref param.X, min.X, $"{paramName}.X");
        Assert.Min(ref param.Y, min.Y, $"{paramName}.Y");
    }

    public static void Max(ref Vector2 param, Vector2 max, [CallerArgumentExpression("param")] string paramName = "")
    {
        Assert.Max(ref param.X, max.X, $"{paramName}.X");
        Assert.Max(ref param.Y, max.Y, $"{paramName}.Y");
    }

    public static void Max(ref Vector2I param, Vector2I max, [CallerArgumentExpression("param")] string paramName = "")
    {
        Assert.Max(ref param.X, max.X, $"{paramName}.X");
        Assert.Max(ref param.Y, max.Y, $"{paramName}.Y");
    }
}
