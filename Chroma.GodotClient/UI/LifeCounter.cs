
using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using System.Text;

namespace MysticClue.Chroma.GodotClient.UI;

/// <summary>
/// A UI element for displaying lives on the inside screen.
/// </summary>
public partial class LifeCounter : Label
{
    private int _currentLives, _maxLives;
    public int CurrentLives { get => _currentLives; set => Update(value, _maxLives); }
    public int MaxLives { get => _maxLives; set => Update(_currentLives, value); }
    public void Increment() => Update(_currentLives + 1, _maxLives);
    public void Decrement() => Update(_currentLives - 1, _maxLives);

    public void Update(int currentLives, int maxLives)
    {
        Assert.Min(ref maxLives, 0);
        Assert.Clamp(ref currentLives, 0, maxLives);

        _currentLives = currentLives;
        _maxLives = maxLives;

        // TODO: Use a sprite and animate gaining/losing lives.
        const string HEART = "❤️";
        Text = new StringBuilder(HEART.Length * _currentLives).Insert(0, HEART, _currentLives).ToString();
    }
}
