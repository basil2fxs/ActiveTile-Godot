
using System.Text;

namespace MysticClue.Chroma.GodotClient.GameLogic;

/// <summary>
/// A List that can be easily printed out.
/// Useful as fields in a tuple, which has a default ToString() that prints its fields.
/// </summary>
public class PrintedList<T>: List<T>
{
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append('[');
        for (int i = 0; i < Count; ++i)
        {
            sb.Append(this[i]);
            sb.Append(',');
            sb.Append(' ');
        }
        sb.Append(']');
        return sb.ToString();
    }
}
