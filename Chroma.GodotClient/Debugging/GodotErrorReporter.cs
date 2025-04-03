using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using System;
using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Debugging;

public class GodotErrorReporter : IErrorReporter
{
    HashSet<int> _seen = [];

    public void Report(Exception ex)
    {
        // This is fast and we don't have to store much.
        // It won't be very accurate but collisions are only a problem when we have multiple
        // exceptions, in which case we'll always have one to fix.
        var hash = ex.StackTrace?.GetHashCode() ?? 0;
        if (_seen.Contains(hash)) return;
        _seen.Add(hash);

        GD.PushError(ex);
    }
}
