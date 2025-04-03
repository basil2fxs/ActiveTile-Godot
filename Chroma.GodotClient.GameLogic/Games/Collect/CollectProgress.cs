using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using System.Drawing;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Collect;

/// <summary>
/// Tracks progress of each player toward collecting all targets.
///
/// Targets appear in random order, but the same order for all players.
/// When a target is collected, the player scores in that column,
/// and the next target in that column is spawned.
/// Once all columns are full, the player is done.
/// </summary>
public class CollectProgress
{
    // How much space to avoid score indicator:
    public const int ForwardSpace = 3;
    public const int BackwardSpace = 2;

    // Order in which to spawn targets. We only store Y-coordinate
    int[][] _nextTargetByColumn;
    int[][] _nextTargetIndexByPlayerColumn;
    // Number of targets visible in each column.
    int[][] _targetsByPlayerColumn;
    int[][] _scoreByPlayerColumn;

    public int NextTarget(int player, int column)
    {
        Assert.Clamp(ref player, 0, _nextTargetIndexByPlayerColumn.Length, nameof(player));
        var nextTarget = _nextTargetIndexByPlayerColumn[player];
        Assert.Clamp(ref column, 0, nextTarget.Length, nameof(column));
        return nextTarget[column];
    }
    public int TotalTargets { get; init; }
    public int TargetsPerColumn { get; init; }
    public bool AllTargetsTaken(int player)
    {
        Assert.Clamp(ref player, 0, _nextTargetIndexByPlayerColumn.Length, nameof(player));
        return _nextTargetIndexByPlayerColumn[player].Sum() == TotalTargets;
    }
    public bool AllTargetsTaken(int player, int column) => NextTarget(player, column) == TargetsPerColumn;
    public int TargetsByPlayerColumn(int player, int column) => _targetsByPlayerColumn[player][column];
    public int ScoreByPlayerColumn(int player, int column) => _scoreByPlayerColumn[player][column];
    public bool AllTargetsScored(int player)
    {
        if (!AllTargetsTaken(player)) { return false; }

        return _targetsByPlayerColumn[player].Sum() == 0;
    }

    public CollectProgress(int playerCount, Size playerAreaSize, Random rand)
    {
        int playerWidth = playerAreaSize.Width;
        int playerHeight = playerAreaSize.Height;

        TotalTargets = playerWidth * playerHeight;
        TargetsPerColumn = playerHeight;

        // _nextTarget contains an entry for each location in a complete player area.
        // We want to randomize their order, but ensure they're balanced between columns.
        // First shuffle within each column, then shuffle each group of playerWidth.
        _nextTargetByColumn = new int[playerWidth][];
        for (int x = 0; x < playerWidth; ++x)
        {
            _nextTargetByColumn[x] = new int[playerHeight];
            RandomizeTargets(_nextTargetByColumn[x], rand);
        }

        _nextTargetIndexByPlayerColumn = new int[playerCount][];
        _targetsByPlayerColumn = new int[playerCount][];
        _scoreByPlayerColumn = new int[playerCount][];
        for (int i = 0; i < playerCount; ++i)
        {
            _nextTargetIndexByPlayerColumn[i] = new int[playerWidth];
            _targetsByPlayerColumn[i] = new int[playerWidth];
            _scoreByPlayerColumn[i] = new int[playerWidth];
        }
    }

    // Randomly select targets that are not too close to the index,
    // and not equal to the previous one.
    private static void RandomizeTargets(int[] array, Random rand)
    {
        static bool withinSpace(int index, int value) =>
            index - BackwardSpace - 1 < value && value < index + ForwardSpace;

        var possible = new int[array.Length];
        for (int i = 0; i < array.Length; ++i) possible[i] = i;
        void swap(int i, int j) => (possible[i], possible[j]) = (possible[j], possible[i]);

        int last = -1;
        for (int i = 0; i < array.Length; ++i)
        {
            // Move ineligible to the back.
            int backMarker = array.Length - 1;
            for (int j = 0; j <= backMarker; ++j)
            {
                while ((possible[j] == last || withinSpace(i, possible[j])) && j <= backMarker)
                {
                    swap(j, backMarker);
                    backMarker--;
                }
            }

            array[i] = possible[rand.Next(backMarker + 1)];
            last = array[i];
        }
    }

    public Point TakeNextTarget(int player, int column)
    {
        if (AllTargetsTaken(player))
        {
            Assert.Report(new InvalidOperationException($"Cannot take next target of finished player {player}"));
            return new();
        }

        var nt = _nextTargetByColumn[column][NextTarget(player, column)];
        _nextTargetIndexByPlayerColumn[player][column] += 1;
        _targetsByPlayerColumn[player][column]++;
        return new(column, nt);
    }

    public void ScorePlayerColumn(int player, int column)
    {
        Assert.Clamp(ref player, 0, _targetsByPlayerColumn.Length, nameof(player));
        Assert.Clamp(ref column, 0, _targetsByPlayerColumn[player].Length, nameof(column));

        _targetsByPlayerColumn[player][column]--;
        _scoreByPlayerColumn[player][column]++;
    }
}
