namespace MysticClue.Chroma.GodotClient.GameLogic.Physics;

public class Grid2DRoute
{
    private List<Grid2DMovement> _remainingMovements = [];

    public int GetRemainingMovementCount()
    {
        return _remainingMovements.Count;
    }

    public Grid2DMovement? GetCurrentMovement()
    {
        return _remainingMovements.FirstOrDefault();
    }

    public void SetRoute(List<Grid2DMovement> newMovements)
    {
        _remainingMovements = newMovements;
    }

    public void CompleteCurrentMovement()
    {
        _remainingMovements.RemoveAt(0);
    }
}
