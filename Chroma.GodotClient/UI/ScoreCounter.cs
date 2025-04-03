using Godot;

namespace MysticClue.Chroma.GodotClient.UI;

public partial class ScoreCounter : Label
{
    private int _score;
    public int Score { get => _score; set => Update(value); }

    public void Update(int score)
    {
        _score = score;
        Text = $"{score}";
    }
}
