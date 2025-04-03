using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using MysticClue.Chroma.GodotClient.GameLogic.Physics;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using static Godot.Control;

namespace MysticClue.Chroma.GodotClient;

public static partial class ExtensionMethods
{
    public static Vector2 ToVector2(this Point p) { return new Vector2(p.X, p.Y); }

    public static Vector2 ToVector2(this Size p) { return new Vector2(p.Width, p.Height); }

    public static Vector2 ToVector2(this Vector2I v) { return new Vector2(v.X, v.Y); }

    public static Vector2 ToVector2(this Grid2DMovementDirection direction) => direction switch
    {
        Grid2DMovementDirection.UP => Vector2.Up,
        Grid2DMovementDirection.DOWN => Vector2.Down,
        Grid2DMovementDirection.LEFT => Vector2.Left,
        Grid2DMovementDirection.RIGHT => Vector2.Right,
        _ => Vector2.Zero
    };

    public static Point ToGridPosition(this ResolvedGridSpecs g, Vector2 v) => new(g.ToGridX(v.X), g.ToGridY(v.Y));

    /// <summary>
    /// For each dimension, choose the one with the smaller magnitude.
    /// </summary>
    public static Vector2 LimitTo(this Vector2 v1, Vector2 v2)
    {
        var ret = v1;
        if (float.Abs(v2.X) < float.Abs(v1.X))
        {
            ret.X = v2.X;
        }
        if (float.Abs(v2.Y) < float.Abs(v1.Y))
        {
            ret.Y = v2.Y;
        }
        return ret;
    }

    /// <summary>
    /// Gets the first descendant that implements a type.
    /// </summary>
    public static T? GetChildByType<T>(this Node node, bool recursive = true) where T : Node
    {
        var en = node.GetChildrenByType<T>(recursive).GetEnumerator();
        return en.MoveNext() ? en.Current : null;
    }

    /// <summary>
    /// Returns all descendants that implement a type.
    ///
    /// This is faster and less error-prone than Node.FindChildren().
    /// </summary>
    public static IEnumerable<T> GetChildrenByType<T>(this Node node, bool recursive = true) where T : Node
    {
        for (int i = 0; i < node.GetChildCount(); ++i)
        {
            Node child = node.GetChild(i);
            if (child is T childT) { yield return childT; }
            if (recursive && child.GetChildCount() > 0)
            {
                foreach (T c in child.GetChildrenByType<T>(true))
                {
                    yield return c;
                }
            }
        }
    }

    public static Button AddButton(this Container parent, string name, bool shrink = false)
    {
        var b = new Button() { Name = name, Text = name };
        if (shrink) b.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        parent.AddChild(b);
        return b;
    }

    public static void Press(this Button button)
    {
        button.ButtonPressed = true;
        button.EmitSignal(BaseButton.SignalName.Pressed);
    }

    public static void Toggle(this Button button)
    {
        button.ButtonPressed = !button.ButtonPressed;
        button.EmitSignal(BaseButton.SignalName.Toggled, button.ButtonPressed);
    }

    public static Timer AddTimer(this Node parent, double waitTime) => AddTimer(parent, (float)waitTime);
    public static Timer AddTimer(this Node parent, float waitTime)
    {
        var t = new Timer() { Autostart = true, OneShot = true, WaitTime = waitTime };
        parent.AddChild(t);
        t.Timeout += () => { if (t.OneShot) t.QueueFree(); };
        return t;
    }

    /// <summary>
    /// Randomly select from a list of generic objects up to the total specified. Default of total is 1.
    /// If the list is empty, or the total is 0, this will return an empty list.
    /// </summary>
    /// <param name="rand">Random object</param>
    /// <param name="readonlyItems">List of items to randomly select from</param>
    /// <param name="total">Total items to select</param>
    /// <returns>A randomly selected list of items</returns>
    public static List<T> NextFromList<T>(this Random rand, IReadOnlyList<T> readonlyItems, int total = 1)
    {
        List<T> randomlySelectedItems = [];
        var items = readonlyItems.ToList();

        var validTotal = Math.Min(readonlyItems.Count, total);
        var i = 0;
        while (i < validTotal)
        {
            var index = rand.Next(items.Count);
            randomlySelectedItems.Add(items[index]);

            // Swap randomly selected element with the last, then drop the last element
            items[index] = items.Last();
            items.RemoveAt(items.Count - 1);

            i++;
        }
        return randomlySelectedItems;
    }
}
