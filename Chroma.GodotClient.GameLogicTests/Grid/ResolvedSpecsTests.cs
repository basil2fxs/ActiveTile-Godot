using MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using System.Drawing;
using System.Net;

namespace MysticClue.Chroma.GodotClient.GameLogicTests.Grid;

public class ResolvedSpecsTests
{
    [Fact]
    public void TestGridSpecs()
    {
        var gridSpecs = new GridSpecs(
            Width: 14,
            Height: 11,
            // TODO: support larger pixels per unit.
            // This would require defining the format for pushing multiple pixels to each tile,
            // and the orientation of tiles with respect to grid coordinates.
            PixelsPerUnit: 1,
            // TODO: support column-wise.
            ColumnWise: false,
            OutputChains: [
                new(
                    Endpoint: new IPEndPoint(IPAddress.Any, 2301),
                    SerialPort: null,
                    ConnectedAtEnd: true,
                    FirstIndex: 0,
                    LastIndex: 3
                ),
                new(new IPEndPoint(IPAddress.Any, 2302), null, true, 4, 7),
                new(new IPEndPoint(IPAddress.Any, 2303), null, true, 8, 10)
            ]
        );

        var resolved = new ResolvedGridSpecs(gridSpecs);
        // Check there is 1-1 correspondence between grid and output chains.
        for (int x = 0; x < gridSpecs.Width; ++x)
        {
            for (int y = 0; y < gridSpecs.Height; ++y)
            {
                var (i, t) = resolved.ChainFromGrid(x, y);
                Assert.Equal((x, y), resolved.GridFromChain(i)[t]);
            }
        }
        for (int i = 0; i < gridSpecs.OutputChains.Count; ++i)
        {
            var chain = resolved.GridFromChain(i);
            for (int t = 0; t < chain.Length; ++t)
            {
                var (x, y) = chain[t];
                Assert.Equal((i, t), resolved.ChainFromGrid(x, y));
            }
        }
    }

    [Fact]
    public void TestNoWaypoint()
    {
        GameSpecs.MovingBlock mb1 = new(new(1, 1), [new(1, 1)], GameSpecs.RouteReturn.MoveToStart);
        var gameSpecs = new GameSpecs(3, 3, [], [mb1]);

        var forward = GameSpecs.BlockMovement.Forward;
        var gameState = new GameState(gameSpecs);
        Assert.Equal([new(new(1, 1), 1, forward, forward)], gameState.DangerPositions);
        gameState.Step();
        Assert.Equal([new(new(1, 1), 1, forward, forward)], gameState.DangerPositions);
    }

    [Fact]
    public void TestOneWaypoint()
    {
        GameSpecs.MovingBlock mb1 = new(new(1, 1), [new(2, 0)], GameSpecs.RouteReturn.MoveToStart);
        var gameSpecs = new GameSpecs(3, 3, [], [mb1]);

        var forward = GameSpecs.BlockMovement.Forward;
        var gameState = new GameState(gameSpecs);
        Assert.Equal([new(new(2, 0), 1, forward, forward)], gameState.DangerPositions);
        gameState.Step();
        Assert.Equal([new(new(2, 0), 1, forward, forward)], gameState.DangerPositions);
    }

    [Fact]
    public void TestGameStateSmall()
    {
        Size size = new(1, 1);
        GameSpecs.StaticBlock b = new(size, new(0, 0));
        GameSpecs.MovingBlock mb = new(size, [new(1, 0), new(2, 0)], GameSpecs.RouteReturn.ReverseAtEnd);
        var forward = GameSpecs.BlockMovement.Forward;
        var gameState = new GameState(new(3, 1, [b], [mb]));
        Assert.Equal([new(new(1, 0), 1, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] { { true }, { true }, { false } }, gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(2, 0), 0, GameSpecs.BlockMovement.Reversing, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] { { true }, { false }, { true } }, gameState.GetInaccessible());
    }

    [Fact]
    public void TestGameStateTeleport()
    {
        GameSpecs.MovingBlock mb = new(new(1, 1), [new(0, 0), new(2, 0)], GameSpecs.RouteReturn.TeleportToStart);
        var forward = GameSpecs.BlockMovement.Forward;
        var gameState = new GameState(new(3, 1, [], [mb]));
        Assert.Equal([new(new(0, 0), 1, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] { { true }, { false }, { false } }, gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(1, 0), 1, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] { { false }, { true }, { false } }, gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(2, 0), 0, GameSpecs.BlockMovement.Teleporting, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] { { false }, { false }, { true } }, gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(0, 0), 1, forward, GameSpecs.BlockMovement.Teleporting)], gameState.DangerPositions);
        Assert.Equal(new bool[,] { { true }, { false }, { false } }, gameState.GetInaccessible());
    }

    [Fact]
    public void TestGameStateLarge()
    {
        var forward = GameSpecs.BlockMovement.Forward;
        var reverse = GameSpecs.BlockMovement.Reversing;
        Size size = new(2, 1);
        GameSpecs.StaticBlock b = new(size, new(1, 1));
        GameSpecs.MovingBlock mb1 = new(size, [new(0, 2), new(1, 1), new(3, 0)], GameSpecs.RouteReturn.ReverseAtEnd);
        GameSpecs.MovingBlock mb2 = new(size, [new(3, 2), new(2, 1), new(0, 2)], GameSpecs.RouteReturn.MoveToStart);
        var gameState = new GameState(new(4, 3, [b], [mb1, mb2]));
        Assert.Equal([new(new(0, 2), 1, forward, forward), new(new(3, 2), 1, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] {
                { false, false, true  },
                { false, true,  true  },
                { false, true,  false },
                { false, false, true  } },
            gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(1, 1), 2, forward, forward), new(new(2, 1), 2, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] {
                { false, false, false },
                { false, true,  false },
                { false, true,  false },
                { false, true,  false } },
            gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(2, 0), 2, forward, forward), new(new(1, 2), 2, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] {
                { false, false, false },
                { false, true,  true  },
                { true,  true,  true  },
                { true,  false, false } },
            gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(3, 0), 1, reverse, forward), new(new(0, 2), 0, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] {
                { false, false, true  },
                { false, true,  true  },
                { false, true,  false },
                { true,  false, false } },
            gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(2, 1), 1, reverse, reverse), new(new(1, 2), 0, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] {
                { false, false, false },
                { false, true,  true  },
                { false, true,  true  },
                { false, true,  false } },
            gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(1, 1), 0, reverse, reverse), new(new(2, 2), 0, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] {
                { false, false, false },
                { false, true,  false },
                { false, true,  true  },
                { false, false, true  } },
            gameState.GetInaccessible());
        gameState.Step();
        Assert.Equal([new(new(0, 2), 1, forward, reverse), new(new(3, 2), 1, forward, forward)], gameState.DangerPositions);
        Assert.Equal(new bool[,] {
                { false, false, true  },
                { false, true,  true  },
                { false, true,  false },
                { false, false, true  } },
            gameState.GetInaccessible());
    }

    [Fact]
    public void TestOutOfBoundsRoutes()
    {
        Size size = new(2, 2);
        GameSpecs.MovingBlock mb1 = new(size, [new(-2, -2), new(3, 3), new(-2, 3), new(3, -2)], GameSpecs.RouteReturn.MoveToStart);
        var gameSpecs = new GameSpecs(3, 3, [], [mb1]);

        var forward = GameSpecs.BlockMovement.Forward;
        var gameState = new GameState(gameSpecs);
        var offgrid = new bool[,] {
                { false, false, false },
                { false, false, false },
                { false, false, false } };
        Assert.Equal([new(new(-2, -2), 1, forward, forward)], gameState.DangerPositions);
        Assert.Equal(offgrid, gameState.GetInaccessible());
        gameState.Step();
        gameState.Step();
        gameState.Step();
        gameState.Step();
        gameState.Step();
        Assert.Equal([new(new(3, 3), 2, forward, forward)], gameState.DangerPositions);
        Assert.Equal(offgrid, gameState.GetInaccessible());
        gameState.Step();
        gameState.Step();
        gameState.Step();
        gameState.Step();
        gameState.Step();
        Assert.Equal([new(new(-2, 3), 3, forward, forward)], gameState.DangerPositions);
        Assert.Equal(offgrid, gameState.GetInaccessible());
        gameState.Step();
        gameState.Step();
        gameState.Step();
        gameState.Step();
        gameState.Step();
        Assert.Equal([new(new(3, -2), 0, forward, forward)], gameState.DangerPositions);
        Assert.Equal(offgrid, gameState.GetInaccessible());
    }
}
