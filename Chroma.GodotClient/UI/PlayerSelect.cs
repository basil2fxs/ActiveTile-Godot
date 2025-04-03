using Godot;
using System;

namespace MysticClue.Chroma.GodotClient.UI;

/// <summary>
/// UI that manages player selection.
/// 
/// At the moment it just lets you choose the number of players.
/// Eventually this UI will handle tagging in and out, and maybe queueing.
/// </summary>
public partial class PlayerSelect : VBoxContainer
{
    public const int MaxSupportedPlayers = 7;

    [Signal]
    public delegate void PlayerCountSelectedEventHandler(int playerCount);

    private static string[] Emoji = [
        "🧒\n1 Player", "👧👩‍🦰\n2 Players", "👨‍👩‍👦\n3 Players", "👨🏼‍👩🏼‍👧🏼‍👦🏼\n4 Players",
        "👨🏿‍👦🏿👩🏻‍👧🏻‍👦🏻\n5 Players", "👨🏽‍👩🏽‍👧🏽👨‍👨‍👧\n6 Players", "👨‍👩‍👧‍👧👩‍👩‍👦\n7 Players"];

    public override void _Ready()
    {
        var hFlow = GetNode<HFlowContainer>("HFlowContainer");
        for (int i = 1; i <= MaxSupportedPlayers; i++)
        {
            var playerCountCapture = i;
            var margin = new MarginContainer();
            int marginSize = 50;
            margin.AddThemeConstantOverride("margin_top", marginSize);
            margin.AddThemeConstantOverride("margin_left", marginSize);
            margin.AddThemeConstantOverride("margin_bottom", marginSize);
            margin.AddThemeConstantOverride("margin_right", marginSize);
            hFlow.AddChild(margin);
            var b = margin.AddButton(Emoji[i-1]);
            b.AddThemeFontSizeOverride("font_size", 60);
            b.CustomMinimumSize = new Vector2(0, 240);
            b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            b.Pressed += () => EmitSignal(SignalName.PlayerCountSelected, playerCountCapture);
        }
    }
}
