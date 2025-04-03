using Godot;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.UI;
using System;
using System.Collections.Generic;
using Size = System.Drawing.Size;

namespace MysticClue.Chroma.GodotClient.Games;

/// <summary>
/// Implementations of games on the grid hardware.
///
/// Rendering a subclass of this node should produce what's displayed on the
/// grid where one grid unit corresponds to one unit in 2D space.
/// </summary>
public partial class GridGame : Node2D
{
    private static Color[] _playerColors = [
        Colors.LightBlue, Colors.LightGreen, Colors.LightCoral, Colors.Yellow,
        Colors.Purple, Colors.HotPink, Colors.DarkGreen, Colors.DarkSalmon];
    public static IReadOnlyList<Color> PlayerColors => _playerColors;

    protected AudioManager? AudioManager { get; private set; }

    protected ResolvedGridSpecs GridSpecs { get; init; }

    // The latest data we got from the hardware.
    byte[,] _sensorData;
    // List of coordinates that were pressed since the last time TakePressed() was called.
    List<Vector2I> _pressed = [];

    Flourish _flourish;
    Action? _postFlourish;

    /// <summary>
    /// Common flourish for the intro.
    ///
    /// Can be re-triggered later in the game.
    ///
    /// Subclasses should add this as a child.
    /// </summary>
    protected Flourish Flourish => _flourish;

    /// <summary>
    /// Indicates that superfluous intro animations should be skipped, for debugging.
    /// </summary>
    public bool SkipIntro { get; set; }

    /// <summary>
    /// Reference to the InsideScreen for updating counters or displaying extra stuff.
    ///
    /// This must be set before instantiating GridGame subclasses that use it.
    /// </summary>
    public static InsideScreen? InsideScreen { get; set; }

    public GridGame(ResolvedGridSpecs grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        GridSpecs = grid;
        _sensorData = new byte[grid.Width, grid.Height];
        _flourish = new Flourish(new(grid.Width, grid.Height), new(grid.Width, grid.Height));
        _flourish.Name = "Flourish";
        _flourish.Done += PostFlourish;
    }
    public override void _Ready()
    {
        AudioManager = GetNode<AudioManager>("/root/AudioManager");
        foreach (var s in UsedSounds) { AudioManager.Load(s); }

        base._Ready();
    }

    /// <summary>
    /// Get the list of presses since the last time this was called.
    /// </summary>
    // TODO: This is very primitive for now.
    // Ideally it would track current presses vs new presses and releases.
    // We also need filtering for glitches.
    protected List<Vector2I> TakePressed()
    {
        var p = _pressed;
        _pressed = [];
        return p;
    }

    /// <summary>
    /// Check if a specific grid tile is currently pressed.
    /// </summary>
    protected bool IsPressed(int x, int y)
    {
        if (x < 0 || x > _sensorData.GetLength(0) || y < 0 || y > _sensorData.GetLength(1))
        {
            return false;
        }

        return _sensorData[x, y] == ResolvedGridSpecs.SensorValuePressed;
    }

    public static Polygon2D MakeOverlayPolygon(Vector2 start, Vector2 end, Color color)
    {
        var p = MakePolygon(start, end, color);
        var mat = new CanvasItemMaterial() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        p.Material = mat;
        return p;
    }

    public static Polygon2D MakePolygon(Vector2 start, Vector2 end, Color color)
    {
        var p = new Polygon2D();
        p.Polygon = [start, new(start.X, end.Y), end, new(end.X, start.Y)];
        p.Color = color;
        return p;
    }

    protected static Area2D MakePolygonArea(Size size, Color color)
    {
        return MakePolygonArea(new(), new(size.Width, size.Height), color);
    }

    /// <summary>
    /// Creates a rectangular Area2D with both a Polygon2D child for rendering
    /// and a CollisionShape2D child for physics.
    /// </summary>
    /// <param name="size">Size of the rectangle in grid units.</param>
    /// <param name="color">Color to apply to the Polygon2D.</param>
    public static Area2D MakePolygonArea(Vector2 start, Vector2 end, Color color)
    {
        // Inset the collision shape by a small amount.
        // This helps make movement along the grid more reliable.
        const float Margin = 0.1f;

        var p = MakePolygon(start, end, color);
        var r = new RectangleShape2D();
        Vector2 size = end - start;
        r.Size = new(size.X - 2 * Margin, size.Y - 2 * Margin);
        var c = new CollisionShape2D();
        c.Shape = r;
        c.Position = start + 0.5f * size;
        var a = new Area2D();
        a.AddChild(p);
        a.AddChild(c);
        return a;
    }

    public static Tween FadeOut(Polygon2D p, Color to = new Color(), float seconds = 0.5f)
    {
        var t = p.CreateTween();
        var setColor = Callable.From((Color c) => p.Color = c);
        t.TweenMethod(setColor, p.Color, to, seconds);
        return t;
    }

    /// <summary>
    /// An ease-in animation that briefly flashes white then goes to base color.
    /// </summary>
    public static Tween EaseInFlashWhite(Polygon2D p, double delay = 0)
    {
        return FlashColor(p, new(), Colors.White, p.Color, delay);
    }

    /// <summary>
    /// An animation that briefly flashes a color.
    /// </summary>
    /// <param name="p">Polygon2D to tween.</param>
    /// <param name="from">Initial color.</param>
    /// <param name="color">Flash color.</param>
    /// <param name="to">Final color.</param>
    /// <param name="delay">Time before tween starts.</param>
    /// <param name="initialTweenDuration">Time it takes for tween to transition between colors <paramref name="from"/> and <paramref name="color"/></param>
    /// <param name="finalTweenDuration">Time it takes for tween to transition between colors <paramref name="color"/> and <paramref name="to"/></param>
    public static Tween FlashColor(Polygon2D p, Color from, Color color, Color to, double delay = 0, float initialTweenDuration = 0.2f, float finalTweenDuration = 0.3f)
    {
        var t = p.CreateTween();
        if (delay > 0)
        {
            p.Color = from;
            t.TweenInterval(delay);
        }
        var setColor = Callable.From((Color c) => p.Color = c);
        t.TweenMethod(setColor, from, color, initialTweenDuration);
        t.TweenMethod(setColor, color, to, finalTweenDuration);
        return t;
    }

    public static Tween FlashColorLoop(Polygon2D p, Color from, Color to, float singleTweenDuration, float interval = 0f, int loopTotal = 1)
    {
        var t = p.CreateTween();
        t.SetLoops(loopTotal);
        if (interval > 0)
        {
            p.Color = from;
            t.TweenInterval(interval);
        }
        var setColor = Callable.From((Color c) => p.Color = c);
        t.TweenMethod(setColor, from, to, singleTweenDuration / 3);
        t.TweenMethod(setColor, to, to, singleTweenDuration / 3);
        t.TweenMethod(setColor, to, from, singleTweenDuration / 3);
        return t;
    }

    /// <summary>
    /// Spawn a single grid square, flash a color, then free it.
    /// </summary>
    public static Tween FlashSquare(Node2D parent, Vector2I point, Color initialColor, Color color, bool flashTwice, bool freeAtEnd = true)
    {
        return FlashRectI(parent, point, new(1, 1), initialColor, color, flashTwice, freeAtEnd);
    }

    /// <summary>
    /// Spawn a polygon covering part of the grid, flash a color, then free it.
    /// </summary>
    public static Tween FlashRectI(Node2D parent, Vector2I point, Vector2I size, Color initialColor, Color color, bool flashTwice, bool freeAtEnd = true)
    {
        return FlashRect(parent, point + 0.5f * size.ToVector2(), size, initialColor, color, flashTwice, freeAtEnd);
    }


    /// <summary>
    /// Spawn a polygon, flash a color, then free it.
    /// </summary>
    public static Tween FlashRect(Node2D parent, Vector2 center, Vector2 size, Color initialColor, Color color, bool flashTwice, bool freeAtEnd = true)
    {
        var p = MakeOverlayPolygon(-0.5f * size, 0.5f * size, color);
        p.Position = center;
        parent.AddChild(p);
        var t = p.CreateTween();
        var setColor = Callable.From((Color c) => p.Color = c);
        var time = 0.2f;
        if (flashTwice)
        {
            time = 0.1f;
            t.TweenMethod(setColor, initialColor, color, time);
            t.TweenMethod(setColor, color, initialColor, time);
        }
        t.TweenMethod(setColor, initialColor, color, time);
        if (freeAtEnd)
        {
            t.TweenMethod(setColor, color, new Color(), time + 0.1f);
            t.TweenCallback(Callable.From(p.QueueFree)).SetDelay(0.5f);
        }

        return t;
    }

    /// <summary>
    /// Handle clicks from the debug view by simulating hardware input.
    /// </summary>
    /// <param name="position">Position in grid coordinates.</param>
    public virtual void DebugInput(Vector2 position, bool pressed)
    {
        var p = GridSpecs.ToGridPosition(position);
        if (_sensorData[p.X, p.Y] != ResolvedGridSpecs.SensorValuePressed && pressed)
            _pressed.Add(new(p.X, p.Y));

        _sensorData[p.X, p.Y] = pressed ? ResolvedGridSpecs.SensorValuePressed : ResolvedGridSpecs.SensorValueUnpressed;
    }

    public void UpdateSensor(int x, int y, byte sensorData)
    {
        if (_sensorData[x, y] != ResolvedGridSpecs.SensorValuePressed && sensorData == ResolvedGridSpecs.SensorValuePressed)
        {
            _pressed.Add(new(x, y));
        }
        _sensorData[x, y] = sensorData;
    }

    public virtual string StateString { get; } = "NoState";

    protected virtual IReadOnlyList<Sounds> UsedSounds => [];

    /// <summary>
    /// Implements the initial flourish when players enter the room.
    ///
    /// Subclasses should call this when they enter the flourish state.
    /// </summary>
    /// <param name="postFlourish">Callback when the flourish finishes.</param>
    protected void EnterFlourishState(Action postFlourish)
    {
        _flourish.Show();
        _postFlourish = postFlourish;
        TakePressed();
    }

    private void PostFlourish()
    {
        if (_postFlourish != null)
        {
            _flourish.Hide();
            _postFlourish();
            _postFlourish = null;
            TakePressed();
        }
    }

    /// <summary>
    /// Process subclasses should run after BeginFlourish().
    ///
    /// Handles triggering the flourish (or not if SkipIntro).
    /// </summary>
    protected virtual void ProcessFlourish()
    {
        if (_flourish.Running) { return; }
        if (SkipIntro)
        {
            _flourish.Hide();
            PostFlourish();
            return;
        }

        // Trigger flourish from the first step.
        foreach (var p in TakePressed())
        {
            const float FlourishSeconds = 1.5f;
            _flourish.StartFromIndex(p, FlourishSeconds);
            AudioManager?.Play(Sounds.Flourish_90sGameUi4);
            break;
        }
    }
}
