using System.Text.Json;

namespace MysticClue.Chroma.GodotClient.GameLogic;

public static class JsonConfig
{
    private static JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static T? FromJsonString<T>(string jsonString)
    {
        return JsonSerializer.Deserialize<T>(jsonString, Options);
    }

    public static string ToJsonString<T>(T data)
    {
        return JsonSerializer.Serialize(data, Options);
    }
}
