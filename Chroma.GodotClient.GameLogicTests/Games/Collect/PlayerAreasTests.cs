using MysticClue.Chroma.GodotClient.GameLogic.Games.Collect;
using System.Collections.Generic;
using System.Drawing;

namespace MysticClue.Chroma.GodotClient.GameLogicTests.Games.Collect;

public class PlayerAreasTests
{
    private static Dictionary<(int, int), int[]> TestSplitHorizontallyData = new()
    {
        [(1, 1)] = [0],
        [(1, 2)] = [0],
        [(2, 2)] = [0, 1],
        [(2, 3)] = [0, 2],
        [(2, 4)] = [0, 2],
        [(3, 3)] = [0, 1, 2],
        [(3, 4)] = [1, 2, 3],
        [(3, 5)] = [0, 2, 4],
        [(3, 6)] = [0, 2, 4],
        [(3, 7)] = [1, 3, 5],
        [(3, 8)] = [0, 3, 6],
        [(4, 4)] = [0, 1, 2, 3],
        [(4, 5)] = [0, 1, 3, 4],
        [(4, 6)] = [1, 2, 3, 4],
        [(4, 7)] = [0, 2, 4, 6],
        [(4, 8)] = [0, 2, 4, 6],
        [(4, 9)] = [0, 2, 5, 7],
        [(4, 10)] = [1, 3, 5, 7],
        [(4, 11)] = [0, 3, 6, 9],
        [(5, 5)] = [0, 1, 2, 3, 4],
        [(5, 6)] = [1, 2, 3, 4, 5],
        [(5, 7)] = [1, 2, 3, 4, 5],
        [(5, 8)] = [2, 3, 4, 5, 6],
        [(5, 9)] = [0, 2, 4, 6, 8],
        [(6, 6)] = [0, 1, 2, 3, 4, 5],
        [(6, 7)] = [0, 1, 2, 4, 5, 6],
        [(6, 8)] = [0, 1, 3, 4, 6, 7],
        [(6, 9)] = [1, 2, 3, 5, 6, 7],
        [(6, 10)] = [1, 2, 4, 5, 7, 8],
        [(6, 11)] = [0, 2, 4, 6, 8, 10],
    };

    public static IEnumerable<object[]> TestSplitHorizontallyArgs()
    {
        foreach (var k in TestSplitHorizontallyData.Keys)
        {
            yield return [k.Item1, k.Item2];
        }
    }

    [Theory]
    [MemberData(nameof(TestSplitHorizontallyArgs))]
    public void TestSplitHorizontally(int playerCount, int width)
    {
        var expectedStarts = TestSplitHorizontallyData[(playerCount, width)];
        var pa = PlayerAreas.SplitHorizontally(width, 1, playerCount);
        Assert.Equal(width / playerCount, pa.PlayerAreaSize.Width);
        Assert.Equal(1, pa.PlayerAreaSize.Height);

        var got = Enumerable.Range(0, playerCount).Select(i => pa.Get(i).X).ToList();
        Assert.Equal(expectedStarts, got);
    }

    // Players , Grid Width, Max Width
    private static Dictionary<(int, int, int), int[]> TestSplitHorizontallyWithMaxWidthData = new()
    {
        [(1, 1, 1)] = [0],
        [(1, 2, 1)] = [1],
        [(1, 2, 2)] = [0],

        [(2, 2, 1)] = [0, 1],
        [(2, 3, 1)] = [0, 2],
        [(2, 4, 1)] = [1, 2],
        [(2, 2, 2)] = [0, 1],
        [(2, 3, 2)] = [0, 2],
        [(2, 4, 2)] = [0, 2],
        [(2, 2, 3)] = [0, 1],
        [(2, 3, 3)] = [0, 2],
        [(2, 4, 3)] = [0, 2],
        [(2, 2, 4)] = [0, 1],
        [(2, 3, 4)] = [0, 2],
        [(2, 4, 4)] = [0, 2],
        [(3, 3, 1)] = [0, 1, 2],
        [(3, 4, 1)] = [1, 2, 3],
        [(3, 5, 1)] = [0, 2, 4],
        [(3, 6, 1)] = [2, 3, 4],
        [(3, 7, 1)] = [1, 3, 5],
        [(3, 8, 1)] = [3, 4, 5],
        [(3, 3, 2)] = [0, 1, 2],
        [(3, 4, 2)] = [1, 2, 3],
        [(3, 5, 2)] = [0, 2, 4],
        [(3, 6, 2)] = [0, 2, 4],
        [(3, 7, 2)] = [1, 3, 5],
        [(3, 8, 2)] = [0, 3, 6],
        [(3, 3, 3)] = [0, 1, 2],
        [(3, 4, 3)] = [1, 2, 3],
        [(3, 5, 3)] = [0, 2, 4],
        [(3, 6, 3)] = [0, 2, 4],
        [(3, 7, 3)] = [1, 3, 5],
        [(3, 8, 3)] = [0, 3, 6],
        [(3, 3, 4)] = [0, 1, 2],
        [(3, 4, 4)] = [1, 2, 3],
        [(3, 5, 4)] = [0, 2, 4],
        [(3, 6, 4)] = [0, 2, 4],
        [(3, 7, 4)] = [1, 3, 5],
        [(3, 8, 4)] = [0, 3, 6],
        [(4, 4, 1)] = [0, 1, 2, 3],
        [(4, 5, 1)] = [0, 1, 3, 4],
        [(4, 6, 1)] = [1, 2, 3, 4],
        [(4, 7, 1)] = [0, 2, 4, 6],
        [(4, 8, 1)] = [2, 3, 4, 5],
        [(4, 9, 1)] = [1, 3, 5, 7],
        [(4, 10, 1)] = [3, 4, 5, 6],
        [(4, 11, 1)] = [2, 4, 6, 8],
        [(4, 4, 2)] = [0, 1, 2, 3],
        [(4, 5, 2)] = [0, 1, 3, 4],
        [(4, 6, 2)] = [1, 2, 3, 4],
        [(4, 7, 2)] = [0, 2, 4, 6],
        [(4, 8, 2)] = [0, 2, 4, 6],
        [(4, 9, 2)] = [0, 2, 5, 7],
        [(4, 10, 2)] = [1, 3, 5, 7],
        [(4, 11, 2)] = [0, 3, 6, 9],
        [(4, 4, 3)] = [0, 1, 2, 3],
        [(4, 5, 3)] = [0, 1, 3, 4],
        [(4, 6, 3)] = [1, 2, 3, 4],
        [(4, 7, 3)] = [0, 2, 4, 6],
        [(4, 8, 3)] = [0, 2, 4, 6],
        [(4, 9, 3)] = [0, 2, 5, 7],
        [(4, 10, 3)] = [1, 3, 5, 7],
        [(4, 11, 3)] = [0, 3, 6, 9],
        [(4, 4, 4)] = [0, 1, 2, 3],
        [(4, 5, 4)] = [0, 1, 3, 4],
        [(4, 6, 4)] = [1, 2, 3, 4],
        [(4, 7, 4)] = [0, 2, 4, 6],
        [(4, 8, 4)] = [0, 2, 4, 6],
        [(4, 9, 4)] = [0, 2, 5, 7],
        [(4, 10, 4)] = [1, 3, 5, 7],
        [(4, 11, 4)] = [0, 3, 6, 9],
        [(5, 5, 1)] = [0, 1, 2, 3, 4],
        [(5, 6, 1)] = [1, 2, 3, 4, 5],
        [(5, 7, 1)] = [1, 2, 3, 4, 5],
        [(5, 8, 1)] = [2, 3, 4, 5, 6],
        [(5, 9, 1)] = [0, 2, 4, 6, 8],
        [(5, 5, 2)] = [0, 1, 2, 3, 4],
        [(5, 6, 2)] = [1, 2, 3, 4, 5],
        [(5, 7, 2)] = [1, 2, 3, 4, 5],
        [(5, 8, 2)] = [2, 3, 4, 5, 6],
        [(5, 9, 2)] = [0, 2, 4, 6, 8],
        [(5, 5, 3)] = [0, 1, 2, 3, 4],
        [(5, 6, 3)] = [1, 2, 3, 4, 5],
        [(5, 7, 3)] = [1, 2, 3, 4, 5],
        [(5, 8, 3)] = [2, 3, 4, 5, 6],
        [(5, 9, 3)] = [0, 2, 4, 6, 8],
        [(5, 5, 4)] = [0, 1, 2, 3, 4],
        [(5, 6, 4)] = [1, 2, 3, 4, 5],
        [(5, 7, 4)] = [1, 2, 3, 4, 5],
        [(5, 8, 4)] = [2, 3, 4, 5, 6],
        [(5, 9, 4)] = [0, 2, 4, 6, 8],
        [(6, 6, 1)] = [0, 1, 2, 3, 4, 5],
        [(6, 7, 1)] = [0, 1, 2, 4, 5, 6],
        [(6, 8, 1)] = [0, 1, 3, 4, 6, 7],
        [(6, 9, 1)] = [1, 2, 3, 5, 6, 7],
        [(6, 10, 1)] = [1, 2, 4, 5, 7, 8],
        [(6, 11, 1)] = [0, 2, 4, 6, 8, 10],
        [(6, 6, 2)] = [0, 1, 2, 3, 4, 5],
        [(6, 7, 2)] = [0, 1, 2, 4, 5, 6],
        [(6, 8, 2)] = [0, 1, 3, 4, 6, 7],
        [(6, 9, 2)] = [1, 2, 3, 5, 6, 7],
        [(6, 10, 2)] = [1, 2, 4, 5, 7, 8],
        [(6, 11, 2)] = [0, 2, 4, 6, 8, 10],
        [(6, 6, 3)] = [0, 1, 2, 3, 4, 5],
        [(6, 7, 3)] = [0, 1, 2, 4, 5, 6],
        [(6, 8, 3)] = [0, 1, 3, 4, 6, 7],
        [(6, 9, 3)] = [1, 2, 3, 5, 6, 7],
        [(6, 10, 3)] = [1, 2, 4, 5, 7, 8],
        [(6, 11, 3)] = [0, 2, 4, 6, 8, 10],
        [(6, 6, 4)] = [0, 1, 2, 3, 4, 5],
        [(6, 7, 4)] = [0, 1, 2, 4, 5, 6],
        [(6, 8, 4)] = [0, 1, 3, 4, 6, 7],
        [(6, 9, 4)] = [1, 2, 3, 5, 6, 7],
        [(6, 10, 4)] = [1, 2, 4, 5, 7, 8],
        [(6, 11, 4)] = [0, 2, 4, 6, 8, 10],
    };

    public static IEnumerable<object[]> TestSplitHorizontallyWithMaxWidthArgs()
    {
        foreach (var k in TestSplitHorizontallyWithMaxWidthData.Keys)
        {
            yield return [k.Item1, k.Item2, k.Item3];
        }
    }

    [Theory]
    [MemberData(nameof(TestSplitHorizontallyWithMaxWidthArgs))]
    public void TestSplitHorizontallyWithMaxWidth(int playerCount, int gridWidth, int maxPlayerWidth)
    {
        var expectedStarts = TestSplitHorizontallyWithMaxWidthData[(playerCount, gridWidth, maxPlayerWidth)];
        var pa = PlayerAreas.SplitHorizontally(gridWidth, 1, playerCount, maxPlayerWidth);

        var expectedPlayerWidth = Math.Min(maxPlayerWidth, gridWidth / playerCount);
        Assert.Equal(expectedPlayerWidth, pa.PlayerAreaSize.Width);
        Assert.Equal(1, pa.PlayerAreaSize.Height);

        var got = Enumerable.Range(0, playerCount).Select(i => pa.Get(i).X).ToList();
        Assert.Equal(expectedStarts, got);
    }
}
