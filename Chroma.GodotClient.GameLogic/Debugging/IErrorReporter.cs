namespace MysticClue.Chroma.GodotClient.GameLogic.Debugging;

public interface IErrorReporter
{
    public void Report(Exception ex);
}
