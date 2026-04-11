using System.IO;
using System.Text.Json;
using Insait_Audio_Player.Models;

namespace Insait_Audio_Player.Services;

public sealed class AppSettingsRepository
{
    private readonly string _filePath = Path.Combine(AppDataPaths.DataDirectory, "app-settings.json");

    public AppSettingsState Load()
    {
        if (!File.Exists(_filePath))
        {
            return new AppSettingsState();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.AppSettingsState) ?? new AppSettingsState();
        }
        catch
        {
            Recreate();
            return new AppSettingsState();
        }
    }

    public void SaveLanguage(string languageCode)
    {
        var state = Load();
        state.LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.ToLowerInvariant();
        var json = JsonSerializer.Serialize(state, AppJsonSerializerContext.Default.AppSettingsState);
        File.WriteAllText(_filePath, json);
    }

    private void Recreate()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch
        {
            // ignored
        }
    }
}

