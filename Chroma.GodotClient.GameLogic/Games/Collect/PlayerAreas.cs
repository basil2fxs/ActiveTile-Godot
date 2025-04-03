using System.Drawing;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Collect;

/// <summary>
/// Manages a set of regions on a grid, one for each player.
///
/// Assumes regions are all the same size.
/// </summary>
public class PlayerAreas
{
    public Size PlayerAreaSize { get; private set; }
    private Point[] _playerAreas;
    public Point Get(int pi) => _playerAreas[pi];
    public Rectangle GetRect(int pi) => new Rectangle(_playerAreas[pi], PlayerAreaSize);

    private PlayerAreas(Size playerAreaSize, Point[] playerAreas)
    {
        PlayerAreaSize = playerAreaSize;
        _playerAreas = playerAreas;
    }

    public static PlayerAreas SplitHorizontally(int gridWidth, int gridHeight, int playerCount, int maxWidth = int.MaxValue)
    {
        // Calculate how wide each player area can be, and where to put them on the grid.
        // We want to be symmetric where possible.
        var width = Math.Min(maxWidth, gridWidth / playerCount);
        var playerAreaSize = new Size(width, gridHeight);
        var playerAreas = new Point[playerCount];
        for (int i = 0; i < playerCount; ++i) { playerAreas[i] = new Point(); }
        // Add the gap before each player.
        int remainder = gridWidth - (width * playerCount);
        while (remainder > 0)
        {
            if (playerCount % (remainder + 1) == 0)
            {
                int step = playerCount / (remainder + 1);
                for (int i = step; i < playerCount; i += step)
                {
                    playerAreas[i].X += 1;
                }
                break;
            }
            else
            {
                playerAreas[0].X += 1;
                remainder -= 2;
            }
        }
        // Add base width to each player to make _playerArea absolute.
        int cumulative = 0;
        for (int i = 0; i < playerCount; ++i)
        {
            cumulative += playerAreas[i].X;
            playerAreas[i].X = cumulative;
            cumulative += playerAreaSize.Width;
        }

        return new PlayerAreas(playerAreaSize, playerAreas);
    }

    /// <summary>
    /// Converts an absolute grid position into a relative position in a player area.
    /// </summary>
    /// <returns>(player index, player X, player Y) or (-1, x, y) if not found.</returns>
    public (int, int, int) GetPlayerRelative(int x, int y)
    {
        for (int pi = 0; pi < _playerAreas.Length; ++pi)
        {
            var playerArea = GetRect(pi);
            if (playerArea.Contains(x, y))
            {
                return (pi, x - playerArea.X, y - playerArea.Y);
            }
        }
        return (-1, x, y);
    }

}
