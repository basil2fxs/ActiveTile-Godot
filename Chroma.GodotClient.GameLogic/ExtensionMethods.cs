using System.Diagnostics.CodeAnalysis;

namespace MysticClue.Chroma.GodotClient.GameLogic;

public static class ExtensionMethods
{
    /// <summary>
    /// Get a value from the dictionary or create a new value and insert it.
    /// </summary>
    /// <returns><code>false</code> if the value was found, <code>true</code> if it was created.</returns>
    public static bool GetOrNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, [NotNull] out TValue value)
    where TValue : new()
    {
        if (!dict.TryGetValue(key, out TValue? val))
        {
            value = new TValue();
            dict.Add(key, value);
            return true;
        }
        else
        {
            value = val!;
            return false;
        }
    }
}
