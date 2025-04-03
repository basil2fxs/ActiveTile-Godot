using Godot;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.Games;
using System;

namespace MysticClue.Chroma.GodotClient;

/// <summary>
/// Implements a full-grid animation of a wave emanating from an initial trigger point.
///
/// Used for a signature start-of-game flourish applied to all games.
/// Also useful as an end-of-game animation.
/// </summary>
public partial class Flourish : Node2D
{
    // Store 0.5x the size of each unit because it's easier to use in calculations.
    private Vector2 _halfUnit;

    // Put a limit on how long the animation goes for by setting speed and the maximum distance travelled.
    private float _maxWaveTravel;
    private float _wavefrontSpeed;

    // The animation is not progressive. Each unit's color is a function of its location relative
    // to the trigger point, the time since the animation started, and some source of randomness.
    // This allows us to easily migrate the implementation to a shader later if needed.
    private Vector2 _triggerPoint;
    private double _timeSinceTrigger;

    // Just a simple hue override for now so we can use this for more than the start-of-game flourish.
    private Color? _colorOverride;

    public bool Running { get; private set; }

    [Signal]
    public delegate void DoneEventHandler();

    /// <param name="size">In-game size.</param>
    /// <param name="resolution">Internal animation resolution.</param>
    public Flourish(Vector2 size, Vector2I resolution)
    {
        // Realistically if we ever have to animate more than 100x100, we should migrate to a shader.
        GodotAssert.Min(ref size, new(1, 1));
        GodotAssert.Clamp(ref resolution, new(0, 0), new(100, 100));

        // Limit the travel distance to double the diagonal distance so that, even when the trigger
        // is in the corner, there is enough time for the wave to cross the entire size, and then
        // as much time to fade out.
        _maxWaveTravel = 2f * size.Length();

        // Position polygons by their center so we get correct distances from _triggerPoint.
        _halfUnit = 0.5f * size / resolution;

        for (int x = 0; x < resolution.X; ++x)
        {
            for (int y = 0; y < resolution.Y; ++y)
            {
                var p = GridGame.MakePolygon(-_halfUnit, _halfUnit, new Color());
                p.Position = new Vector2(x, y) * 2 * _halfUnit + _halfUnit;
                AddChild(p);
            }
        }
    }

    /// <summary>
    /// Start flourish from the center of a unit.
    /// </summary>
    /// <param name="triggerIndex">Coordinates of unit.</param>
    /// <param name="durationSeconds">Total animation time.</param>
    /// <param name="colorOverride">Optional specific color. Otherwise multi-colored.</param>
    public void StartFromIndex(Vector2 triggerIndex, float durationSeconds, Color? colorOverride = null)
    {
        Assert.Min(ref durationSeconds, float.MinValue);

        _wavefrontSpeed = _maxWaveTravel / durationSeconds;
        _triggerPoint = triggerIndex * (2f * _halfUnit) + _halfUnit;
        _timeSinceTrigger = 0;
        _colorOverride = colorOverride;
        Running = true;
    }

    public override void _Process(double delta)
    {
        if (!Running) { return; }

        _timeSinceTrigger += delta;
        var wavefront = _wavefrontSpeed * (float)_timeSinceTrigger;
        if (wavefront > _maxWaveTravel)
        {
            Running = false;
            EmitSignal(SignalName.Done);
        }

        // Per-unit random. Assuming iterating over children will be the same order every frame.
        var rand = new Random(54735);
        foreach (Polygon2D p in GetChildren())
        {
            Vector2 relativePosition = p.Position - _triggerPoint;
            p.Color = CalculateColor(wavefront, relativePosition, rand.NextSingle());
        }
    }

    // CalculateColor is the functional representation of the animation.
    // We can add more implementations for different flourish styles.
    private Color CalculateColor(float wavefront, Vector2 relativePosition, float randomNumber)
    {
        var distance = relativePosition.Length();
        var relativeWavefront = wavefront - distance;
        // The color is constructed from an initial hue based on the relative position,
        // and a change in hue as the wavefront passes.
        var initialAngle = 5f * Mathf.Atan2(relativePosition.Y, relativePosition.X);
        float hue;
        if (_colorOverride.HasValue) { hue = _colorOverride.Value.H; }
        else { hue = (initialAngle + -0.2f * relativeWavefront) / Mathf.Tau; }
        // Lightness is just a function of the wavefront.
        // We fade in for a distance of leadUp, and fade out over the entire _maxWaveTravel.
        float maxLightness = 0.5f + 0.5f * randomNumber;
        const float leadUp = 1.5f;
        var lightness = 0f;
        var saturation = 0.9f;
        if (relativeWavefront > -leadUp && relativeWavefront < 0)
        {
            // Linearly ramp up in the leadup.
            lightness = maxLightness + maxLightness * relativeWavefront / leadUp;
        }
        else if (relativeWavefront > 0 && relativeWavefront < _maxWaveTravel)
        {
            // Fade from maxLightness to zero.
            // Zero should be reached at 2 * _maxWaveTravel, or faster based on randomNumber:
            //   fade == 0 when relativeWavefront == 0.
            //   fade >= 1 when relativeWavefront == _maxWaveTravel.
            float fade = (randomNumber + 1) * relativeWavefront / _maxWaveTravel;

            // Ramp down hyperbolically so that, at fade == 0, fadeFactor == 1.
            float steepness = 2f;
            float fadeFactor = 1 / (steepness * fade + 1);
            // Steepness varies from linear to instant in [0,+inf).

            lightness = maxLightness * fadeFactor;
            lightness = float.Clamp(lightness, 0, 1);
        }

        // Add linear ramp down component in absolute wavefront time to ensure lights are
        // off by the time wavefront == 2 * _diagonalDistance:
        lightness *= 1 - wavefront / _maxWaveTravel;

        return Color.FromOkHsl(hue, saturation, lightness);
    }
}
