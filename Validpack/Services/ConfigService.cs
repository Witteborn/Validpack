using System.Text.Json;
using Validpack.Models;

namespace Validpack.Services;

/// <summary>
/// Service zum Laden und Verwalten der Konfiguration
/// </summary>
public class ConfigService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    
    /// <summary>
    /// Lädt die Konfiguration aus einer JSON-Datei
    /// </summary>
    public Configuration LoadConfiguration(string? configPath)
    {
        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
        {
            return new Configuration();
        }
        
        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Configuration>(json, _jsonOptions);
            return config ?? new Configuration();
        }
        catch
        {
            return new Configuration();
        }
    }
    
    /// <summary>
    /// Erstellt eine Beispiel-Konfigurationsdatei
    /// </summary>
    public void CreateExampleConfig(string path)
    {
        var config = new Configuration
        {
            Whitelist = new List<string>
            {
                "internal-company-package",
                "my-private-package"
            },
            Blacklist = new List<string>
            {
                "Newtonsoft.Json",
                "moment"
            }
        };
        
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(path, json);
    }
}
