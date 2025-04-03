using Godot;

namespace MysticClue.Chroma.GodotClient.UI;

public partial class RatioContainer : SplitContainer
{
    private float _ratio = 0.5f;

    [Export]
    public float Ratio { get => _ratio; set { _ratio = value; Update(); } }

    public override void _Ready()
    {
        base._Ready();

        ItemRectChanged += Update;
        Update();
    }

    private void Update()
    {
        var size = GetRect().Size;
        SplitOffset = (int)(_ratio * (Vertical ? size.Y : size.X));
    }
}
