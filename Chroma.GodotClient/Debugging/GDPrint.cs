using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MysticClue.Chroma.GodotClient.Debugging;

public static class GDPrint
{
    public static void WithTimestamp(string text)
    {
        var ts = TimeSpan.FromMilliseconds(Time.GetTicksMsec());
        GD.Print($"{ts:d\\d\\ hh\\hmm\\mss\\s}: {text}");
    }

    public static void LogState(
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        object? arg6 = null,
        object? arg7 = null,
        [CallerArgumentExpression("arg1")] string arg1Exp = "<unknown>",
        [CallerArgumentExpression("arg2")] string arg2Exp = "<unknown>",
        [CallerArgumentExpression("arg3")] string arg3Exp = "<unknown>",
        [CallerArgumentExpression("arg4")] string arg4Exp = "<unknown>",
        [CallerArgumentExpression("arg5")] string arg5Exp = "<unknown>",
        [CallerArgumentExpression("arg6")] string arg6Exp = "<unknown>",
        [CallerArgumentExpression("arg7")] string arg7Exp = "<unknown>",
        [CallerMemberName] string memberName = "<unknown member name>",
        [CallerFilePath] string sourceFilePath = "<unknown file path>",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        IEnumerable<(string, object?)> argYield()
        {
            yield return (arg1Exp, arg1);
            yield return (arg2Exp, arg2);
            yield return (arg3Exp, arg3);
            yield return (arg4Exp, arg4);
            yield return (arg5Exp, arg5);
            yield return (arg6Exp, arg6);
            yield return (arg7Exp, arg7);
        }

        WithTimestamp(
            $"{sourceFilePath}:{sourceLineNumber} {memberName}\n" +
            string.Concat(
                argYield()
                    .Where(a => a.Item2 != null)
                    .Select(a => $"  {a.Item1} = {a.Item2}\n")
            ));
    }
}
