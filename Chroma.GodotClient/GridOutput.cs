using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;
using MysticClue.Chroma.GodotClient.Games;
using System;
using System.Net;

namespace MysticClue.Chroma.GodotClient;

/// <summary>
/// Generates the pixels for the grid hardware and pushes them.
/// 
/// This uses an internal SubViewport that hosts and renders the actual game
/// at a higher resolution than needed. We then use a sprite with supersampling
/// shader to downsize it to the output resolution. This allows us to use
/// regular Godot rendering while producing nicely pixellated output.
/// </summary>
public partial class GridOutput : SubViewport, IGameView
{
    public virtual SubViewport HardwareView => this;
    public virtual SubViewport FullResolutionView => GetNode<SubViewport>("RenderViewport");

    public ResolvedGridSpecs? GridSpecs { get; private set; }
    public Vector2I GridSize => new Vector2I(GridSpecs?.Width ?? 0, GridSpecs?.Height ?? 0);
    public Vector2I OutputSize => (GridSpecs?.PixelsPerUnit ?? 0) * GridSize;

    IGameHardwareInterface? _hardwareInterface;

    private GridGame? _game;

    public override void _Process(double delta)
    {
        _hardwareInterface?.PutFrame(new ImageFrameView(GetTexture().GetImage()));
    }

    public void SetGrid(ResolvedGridSpecs grid, IPEndPoint? localEndpoint)
    {
        ArgumentNullException.ThrowIfNull(grid);

        GridSpecs = grid;
        Size = OutputSize;

        // Render 4x4 pixels for each grid output pixel.
        const int SuperSampling = 4;

        var viewport = FullResolutionView;
        viewport.Size = SuperSampling * OutputSize;

        var camera = viewport.GetNode<Camera2D>("RenderViewportCamera");
        camera.Zoom = SuperSampling * GridSpecs.PixelsPerUnit * new Vector2(1, 1);
        camera.Position = 0.5f * GridSize.ToVector2();

        var sprite = GetNode<Sprite2D>("RenderViewportSprite");
        sprite.Scale = new Vector2(1, 1) / SuperSampling;

        var outputCamera = GetNode<Camera2D>("GridOutputCamera");

        // Construct data needed by shader.
        // It's symmetric so we only specify the positive quadrant. Shader will handle sampling all quadrants.
        // Coefficients should add up to 0.25;
        var sampleCoefficients = new float[] { 0.1f, 0.06f, 0.06f, 0.03f };
        // Locations are relative to the fragment UV, which is in the center of the sprite pixel.
        // We get the UV pixel size of the viewport texture, and provide offsets that end up in the center
        // of adjacent viewport texture pixels.
        var pixelSize = new Vector2(1f / viewport.Size.X, 1f / viewport.Size.Y);
        var sampleLocations = new Vector2[] {
            0.5f * pixelSize, new(0.5f * pixelSize.X, 1.5f * pixelSize.Y),
            new(1.5f * pixelSize.X, 0.5f * pixelSize.Y), 1.5f * pixelSize};

        var shaderMat = sprite.Material as ShaderMaterial;
        if (shaderMat != null)
        {
            shaderMat.SetShaderParameter("SAMPLE_COEFFICIENTS", sampleCoefficients);
            shaderMat.SetShaderParameter("SAMPLE_LOCATIONS", sampleLocations);
        }


        if (GridSpecs.UseSerial) _hardwareInterface = new SerialGameHardwareInterface(GridSpecs, new SerialPortFactory());
        else if (localEndpoint != null) _hardwareInterface = new UdpGameHardwareInterface(GridSpecs, localEndpoint);
    }

    public void SetGame(GridGame? game)
    {
        var container = GetNode<Node2D>("RenderViewport/GridGameContainer");
        foreach (var c in container.GetChildren()) { c.QueueFree(); }

        _game = game;
        if (_game != null)
        {
            container.AddChild(game);
            if (_hardwareInterface != null)
            {
                _hardwareInterface.UpdateSensorCallback = _game.UpdateSensor;
            }
        }
    }

    public virtual void DebugInput(Vector2 position, bool pressed)
    {
        position /= GridSpecs?.PixelsPerUnit ?? 1;
        _game?.DebugInput(position, pressed);
    }

    private sealed class ImageFrameView : IFrameView
    {
        private Image _image;
        public ImageFrameView(Image image)
        {
            _image = image;
        }
        public (byte, byte, byte) GetPixel(int x, int y)
        {
            var c = _image.GetPixel(x, y);
            return (IntensityLookup[c.R8], IntensityLookup[c.G8], IntensityLookup[c.B8]);
        }

        // Godot renders 2D assuming all colors are linear. However, the final values are assumed
        // to be sRGB by the OS when displayed. So we're effectively rendering in sRGB space.
        // (See https://github.com/godotengine/godot/issues/48039).
        // The LED tiles are actually pretty linear in their output intensity, so if we push these
        // values directly they won't look the same as we see on a screen. Instead we want to
        // compress the values with the goal of making them perceptually linear in the range 0-254,
        // (the hardware format uses FF as a header and can't accept them as values).
        // After trying a Gamma curve (i ^ gamma), and an exponential (2 ^ (k * i)), it turned out
        // a simple quadratic looked the best in the room.
        static byte[] IntensityLookup = new byte[256];
        static ImageFrameView()
        {
            for (int i = 0; i < 256; ++i)
            {
                IntensityLookup[i] = (byte)(254.0 * i * i / 255 / 255);
            }
        }
    }
}
