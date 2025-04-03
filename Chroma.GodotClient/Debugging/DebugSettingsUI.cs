using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;

namespace MysticClue.Chroma.GodotClient.Debugging;

public partial class DebugSettingsUI : VBoxContainer
{
    DebugSettings _debugSettings;
    public DebugSettings DebugSettings
    {
        get => _debugSettings;
        set
        {
            _debugSettings = value;
            _showHardwareView.ButtonPressed = _debugSettings.ShowHardwareView;
            _showFullResolution.ButtonPressed = _debugSettings.ShowFullResolution;
            _skipIntro.ButtonPressed = _debugSettings.SkipIntro;
            _separateInsideScreen.ButtonPressed = _debugSettings.SeparateInsideScreen;
            _fullScreen.ButtonPressed = _debugSettings.FullScreen;
        }
    }

    CheckButton _showHardwareView = new() { Name = "ShowHardwareView", Text = "Show hardware view" };
    CheckButton _showFullResolution = new() { Name = "ShowFullResolution", Text = "Show full resolution" };
    CheckButton _skipIntro = new() { Name = "SkipIntro", Text = "Skip intro" };
    CheckButton _separateInsideScreen = new() { Name = "SeparateInsideScreen", Text = "Separate inside screen" };
    CheckButton _fullScreen = new() { Name = "FullScreen", Text = "Full screen" };
    Button _writeLocalSettings = new() { Name = "WriteLocalSettings", Text = "Write settings" };

    public event BaseButton.ToggledEventHandler ShowHardwareViewToggled
    {
        add { _showHardwareView.Toggled += value; }
        remove { _showHardwareView.Toggled -= value; }
    }
    public event BaseButton.ToggledEventHandler ShowFullResolutionToggled
    {
        add { _showFullResolution.Toggled += value; }
        remove { _showFullResolution.Toggled -= value; }
    }
    public event BaseButton.ToggledEventHandler SkipIntroToggled
    {
        add { _skipIntro.Toggled += value; }
        remove { _skipIntro.Toggled -= value; }
    }
    public event BaseButton.ToggledEventHandler SeparateInsideScreenToggled
    {
        add { _separateInsideScreen.Toggled += value; }
        remove { _separateInsideScreen.Toggled -= value; }
    }
    public event BaseButton.ToggledEventHandler FullScreenToggled
    {
        add { _fullScreen.Toggled += value; }
        remove { _fullScreen.Toggled -= value; }
    }

    public DebugSettingsUI()
    {
        _showHardwareView.Toggled += (on) => _debugSettings.ShowHardwareView = on;
        _showFullResolution.Toggled += (on) => _debugSettings.ShowFullResolution = on;
        _skipIntro.Toggled += (on) => _debugSettings.SkipIntro = on;
        _separateInsideScreen.Toggled += (on) => _debugSettings.SeparateInsideScreen = on;
        _fullScreen.Toggled += (on) => _debugSettings.FullScreen = on;

        _writeLocalSettings.Pressed += () => {
            var config = GetNode<Config>("/root/Config");
            config.LocalSettings.DebugSettings = _debugSettings;
            config.WriteLocalSettings();
        };
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AddChild(_showHardwareView);
        AddChild(_showFullResolution);
        AddChild(_skipIntro);
        AddChild(_separateInsideScreen);
        AddChild(_fullScreen);
        AddChild(_writeLocalSettings);
    }
}
