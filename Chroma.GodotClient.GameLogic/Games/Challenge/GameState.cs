using MysticClue.Chroma.GodotClient.GameLogic.Grid;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge;

/// <summary>
/// Data structures used to implement the challenge game.
///
/// TODO: make Step() move proportionally to target instead of diagonal-first.
/// </summary>
public class GameState
{
    public GameSpecs Specs { get; init; }
    public PrintedList<GameSpecs.MovingBlockCurrentPosition> DangerPositions { get; private set; }

    public GameState(GameSpecs specs)
    {
        ArgumentNullException.ThrowIfNull(specs);

        Specs = specs;
        DangerPositions = new PrintedList<GameSpecs.MovingBlockCurrentPosition>();
        foreach (var mb in Specs.Danger)
        {
            DangerPositions.Add(new GameSpecs.MovingBlockCurrentPosition(mb.InitialPosition, mb.InitialWaypoint, GameSpecs.BlockMovement.Forward, GameSpecs.BlockMovement.Forward));
        }
    }

    public void Step()
    {
        for (int i = 0; i < Specs.Danger.Count; ++i)
        {
            var mb = Specs.Danger[i];
            if (mb.Route.Count <= 1) { continue; }

            var p = DangerPositions[i];
            p.LastMovement = p.NextMovement;
            if (p.NextMovement == GameSpecs.BlockMovement.Teleporting)
            {
                p.Position = mb.Route[p.WaypointIndex];
            }
            else
            {
                p.Position = new(
                    p.Position.X + int.Sign(mb.Route[p.WaypointIndex].X - p.Position.X),
                    p.Position.Y + int.Sign(mb.Route[p.WaypointIndex].Y - p.Position.Y));
            }
            if (p.Position == mb.Route[p.WaypointIndex] && mb.Route.Count > 1)
            {
                if (p.NextMovement == GameSpecs.BlockMovement.Reversing)
                {
                    --p.WaypointIndex;
                    if (p.WaypointIndex == -1)
                    {
                        p.NextMovement = GameSpecs.BlockMovement.Forward;
                        p.WaypointIndex = 1;
                    }
                }
                else
                {
                    ++p.WaypointIndex;
                    p.NextMovement = GameSpecs.BlockMovement.Forward;
                    if (p.WaypointIndex >= mb.Route.Count)
                    {
                        if (mb.RouteReturn == GameSpecs.RouteReturn.ReverseAtEnd)
                        {
                            p.NextMovement = GameSpecs.BlockMovement.Reversing;
                            p.WaypointIndex = mb.Route.Count - 2;
                        }
                        else
                        {
                            if (mb.RouteReturn == GameSpecs.RouteReturn.TeleportToStart)
                            {
                                p.NextMovement = GameSpecs.BlockMovement.Teleporting;
                            }
                            p.WaypointIndex = 0;
                        }
                    }
                }
            }
            DangerPositions[i] = p;
        }
    }

    public bool[,] GetInaccessible()
    {
        var ret = new bool[Specs.GridWidth, Specs.GridHeight];
        foreach (var sb in Specs.Safe)
        {
            for (int x = sb.Position.X; x < sb.Position.X + sb.Size.Width; ++x)
            {
                if (x < 0 || x >= Specs.GridWidth) { continue; }
                for (int y = sb.Position.Y; y < sb.Position.Y + sb.Size.Height; ++y)
                {
                    if (y < 0 || y >= Specs.GridHeight) { continue; }

                    ret[x, y] = true;
                }
            }
        }
        for (int i = 0; i < Specs.Danger.Count; ++i)
        {
            var mb = Specs.Danger[i];
            var p = DangerPositions[i];
            for (int x = p.Position.X; x < p.Position.X + mb.Size.Width; ++x)
            {
                if (x < 0 || x >= Specs.GridWidth) { continue; }
                for (int y = p.Position.Y; y < p.Position.Y + mb.Size.Height; ++y)
                {
                    if (y < 0 || y >= Specs.GridHeight) { continue; }

                    ret[x, y] = true;
                }
            }
        }
        return ret;
    }
}
