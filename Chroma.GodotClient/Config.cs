using System;
using Godot;
using MysticClue.Chroma.GodotClient.GameLogic;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;

namespace MysticClue.Chroma.GodotClient;

public partial class Config : Node
{
    public HardwareSpec? Hardware { get; private set; }

    public LocalSettings LocalSettings { get; private set; } = new();

    private static string LocalSettingsPath = "user://localSettings.json";

    public override void _Ready()
    {
        Hardware = TryReadFile<HardwareSpec>("user://hardware.json") ?? ReadFile<HardwareSpec>("res://hardware.json");
        var localSettings = TryReadFile<LocalSettings>(LocalSettingsPath) ?? TryReadFile<LocalSettings>("res://localSettings.json");
        if (localSettings != null) LocalSettings = localSettings;
    }

    private static T? ReadFile<T>(string resource)
    {
        using var file = FileAccess.Open(resource, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"Error reading {resource}: {FileAccess.GetOpenError()}");
        }
        if (file.GetError() != Error.Ok)
        {
            throw new InvalidOperationException($"Error reading {resource}: {file.GetError()}");
        }
        GD.Print($"Using {typeof(T).Name} at {ProjectSettings.GlobalizePath(resource)}");
        return JsonConfig.FromJsonString<T>(file.GetAsText());
    }

    private static T? TryReadFile<T>(string resource)
    {
        using var file = FileAccess.Open(resource, FileAccess.ModeFlags.Read);
        if (file == null || file.GetError() != Error.Ok) { return default; }
        GD.Print($"Using {typeof(T).Name} at {ProjectSettings.GlobalizePath(resource)}");
        return JsonConfig.FromJsonString<T>(file.GetAsText());
    }

    private static void WriteFile<T>(string resource, T data)
    {
        using var file = FileAccess.Open(resource, FileAccess.ModeFlags.Write);
        if (file == null || file.GetError() != Error.Ok)
            return;

        file.StoreString(JsonConfig.ToJsonString(data));
    }

    public void WriteLocalSettings() => WriteFile(LocalSettingsPath, LocalSettings);
}
