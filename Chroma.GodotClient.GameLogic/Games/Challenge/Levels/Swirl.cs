using System.Data;
using System.Drawing;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge.Levels;

public class Swirl : IChallengeGameLevel
{
    public string Name => "Swirl";

    public GameSpecs GetGameSpecs(ResolvedGridSpecs grid)
    {
        PrintedList<GameSpecs.StaticBlock> safe = [];
        var centreHeight = grid.Height % 2 == 0 ? 2 : 1;
        var centreWidth = grid.Width % 2 == 0 ? 2 : 1;
        var centreAnchorPoint = new Point(grid.Width / 2 - (centreWidth/2), grid.Height/2 - centreHeight);

        var topBottomInterval = grid.Height / 5;
        var bottomOffset = grid.Height % 2 == 0 ? 1 : 0;

        var leftRightInterval = grid.Width / 5 ;
        var rightOffset = grid.Width % 2 == 0 ? 1 : 0;

        // Top Safe - avoid adding safes at the last layer
        var topCount = 0;
        for (var y = grid.Height / 2 - centreHeight; y > 0; y -= topBottomInterval)
        {
            var currentAnchorPoint = new Point(centreAnchorPoint.X - (topCount), y);
            var currentWidth = centreWidth + (topCount * 2);
            safe.Add(new GameSpecs.StaticBlock(new Size(currentWidth, 1), new Point(currentAnchorPoint.X, y)));
            topCount++;
        }
        // Bottom Safe - avoid adding safes at the last layer
        var bottomCount = 0;
        for (var y = grid.Height / 2 + centreHeight - bottomOffset; y < grid.Height - 1; y += topBottomInterval)
        {
            var currentAnchorPoint = new Point(centreAnchorPoint.X - bottomCount, y);
            var currentWidth = centreWidth + (bottomCount * 2);
            safe.Add(new GameSpecs.StaticBlock(new Size(currentWidth, 1), new Point(currentAnchorPoint.X, y)));
            bottomCount++;
        }
        // Left Safe - avoid adding safes at the last layer
        var leftCount = 0;
        for (var x = grid.Width / 2 - centreWidth; x > 0; x -= leftRightInterval)
        {
            var currentAnchorPoint = new Point(x, centreAnchorPoint.Y - leftCount + 1);
            var currentHeight = centreHeight + (leftCount * 2);
            safe.Add(new GameSpecs.StaticBlock(new Size(1, currentHeight), new Point(x, currentAnchorPoint.Y)));
            leftCount++;
        }
        // Right Safe - avoid adding safes at the last layer
        var rightCount = 0;
        for (var x = grid.Width / 2 + centreWidth - rightOffset; x < grid.Width - 1; x += leftRightInterval)
        {
            var currentAnchorPoint = new Point(x, centreAnchorPoint.Y - rightCount + 1);
            var currentHeight = centreHeight + (rightCount * 2);
            safe.Add(new GameSpecs.StaticBlock(new Size(1, currentHeight), new Point(x, currentAnchorPoint.Y)));
            rightCount++;
        }

        PrintedList<GameSpecs.MovingBlock> danger = [];
        var dangerSize = new Size(1, 1);
        List<Point> waypoints = [];
        var xMin = 0;
        var yMin = 0;
        var xMax = grid.Width - 1;
        var yMax = grid.Height - 1;

        // Set waypoints
        while (xMin != xMax && yMin != yMax)
        {
            var topLeft = new Point(xMin, yMin);
            if (waypoints.Contains(topLeft)) break;
            waypoints.Add(topLeft);

            var topRight = new Point(xMax, yMin);
            if (waypoints.Contains(topRight)) break;
            waypoints.Add(topRight);

            var bottomRight = new Point(xMax, yMax);
            if (waypoints.Contains(bottomRight)) break;
            waypoints.Add(bottomRight);

            var bottomLeft = new Point(xMin, yMax);
            if (waypoints.Contains(bottomLeft)) break;
            waypoints.Add(bottomLeft);

            var lastPoint = new Point(xMin, yMin + 1);
            if (waypoints.Contains(lastPoint)) break;
            waypoints.Add(lastPoint);

            xMin++;
            xMax--;
            yMin++;
            yMax--;
        }

        // Total dangers to spawn is based on grid size and danger length
        var dangerLength = 3;
        var dangerCount = grid.Width * grid.Height / (dangerLength*4);
        // Get offset between first and second waypoints
        var firstWaypoint = waypoints[0];
        var secondWaypoint = waypoints[1];
        var diff = secondWaypoint.X - firstWaypoint.X;
        for (var i = 0; i < dangerCount; i++)
        {
            // Add dangers to add length to each
            for (var k = 0; k < dangerLength; k++)
            {
                // Set initial position to be offscreen based on difference between the first and second waypoint X values
                var first = waypoints.First();
                var initialPosition = new Point(first.X - k - (diff * i), first.Y);
                danger.Add(new GameSpecs.MovingBlock(
                    dangerSize,
                    [..waypoints],
                    initialPosition,
                    1,
                    GameSpecs.RouteReturn.TeleportToStart
                ));
            }
        }
        return new GameSpecs(grid.Width, grid.Height, safe, danger);
    }

    public float ProportionTargets => 0.25f;
}
