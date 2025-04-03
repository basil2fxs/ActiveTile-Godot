using MysticClue.Chroma.GodotClient.GameLogic.Games.Collect;
using System.Drawing;

namespace MysticClue.Chroma.GodotClient.GameLogicTests.Games.Collect;

public class CollectProgressTests
{
    private sealed class NotRandom : Random
    {
        public override int Next(int minValue, int maxValue) => int.Min(minValue + 1, maxValue - 1);
    }

    [Fact]
    public void TestCollectProgress()
    {
        var cp = new CollectProgress(2, new(3, 5), new NotRandom());
        Assert.Equal(15, cp.TotalTargets);
        Assert.Equal(5, cp.TargetsPerColumn);
        for (int pi = 0; pi < 2; ++pi)
        {
            var targets = new List<Point>();
            for (int x = 0; x < 3; ++x)
            {
                for (int y = 0; y < 5; ++y)
                {
                    Assert.False(cp.AllTargetsTaken(pi));
                    targets.Add(cp.TakeNextTarget(pi, x));
                }
                Assert.Equal(5, cp.NextTarget(pi, x));
            }
            Assert.True(cp.AllTargetsTaken(pi));
        }
    }

    [Fact]
    public void TestRandomizeTargets()
    {
        int height = 50;
        var yResults = new int[height];
        int samples = 2000;
        for (int k = 0; k < samples; ++k)
        {
            var cp = new CollectProgress(1, new(1, height), new Random(k * 100));
            int previous = -1;
            for (int y = 0; y < height; ++y)
            {
                var nt = cp.TakeNextTarget(0, 0);
                Assert.NotEqual(previous, nt.Y);
                previous = nt.Y;
                Assert.False(
                    y - CollectProgress.BackwardSpace - 1 < nt.Y && nt.Y < y + CollectProgress.ForwardSpace,
                    $"{y - CollectProgress.BackwardSpace - 1} < {nt.Y} < {y + CollectProgress.ForwardSpace}");
                yResults[nt.Y]++;
            }
        }
        Assert.True(yResults.All(y => int.Abs(samples - y) < samples / 5),
                    $"y should all be within 20% of {samples}, got {string.Join(' ', yResults)}");
    }
}
