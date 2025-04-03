using Godot;
using System;
using System.Globalization;

namespace MysticClue.Chroma.GodotClient.UI;

public partial class TimeDisplay : Label
{
    private double _time;
    public double Time { get => _time; set => Update(value); }

    private bool _pulse;
    public bool Pulse
    {
        get => _pulse;
        set
        {
            _pulse = value;
            if (_pulse && !_pulsing) DoPulse();
        }
    }
    private bool _pulsing;

    private void DoPulse()
    {
        if (!_pulse)
        {
            _pulsing = false;
            return;
        }
        _pulsing = true;

        var currentSize = GetThemeFontSize("font_size");
        var largeSize = (int)(currentSize * 1.2);
        var setSize = Callable.From<int>((size) => AddThemeFontSizeOverride("font_size", size));
        var t = CreateTween();
        t.TweenMethod(setSize, currentSize, largeSize, 0.5f);
        t.TweenMethod(setSize, largeSize, currentSize, 0.5f);
        t.Finished += DoPulse;
    }

    public void Update(double time)
    {
        _time = time;

        if (_time < 0)
            Text = "Time's Up!";
        else
            Text = TimeSpan.FromSeconds(_time).ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }

    public void TimesUp() => Time = -1;
}
