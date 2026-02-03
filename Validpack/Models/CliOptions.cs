namespace Validpack.Models;

/// <summary>
/// Kommandozeilenoptionen
/// </summary>
public class CliOptions
{
    /// <summary>
    /// Pfad zum zu scannenden Verzeichnis
    /// </summary>
    public string? Path { get; set; }
    
    /// <summary>
    /// Pfad zur Konfigurationsdatei
    /// </summary>
    public string ConfigFile { get; set; } = "validpack.json";
    
    /// <summary>
    /// Ausgabeformat (console, json)
    /// </summary>
    public string OutputFormat { get; set; } = "console";
    
    /// <summary>
    /// Detaillierte Ausgabe
    /// </summary>
    public bool Verbose { get; set; }
    
    /// <summary>
    /// Hilfe anzeigen
    /// </summary>
    public bool ShowHelp { get; set; }
    
    /// <summary>
    /// Validierungsfehler
    /// </summary>
    public List<string> Errors { get; } = new();
    
    /// <summary>
    /// Gibt an, ob die Optionen gültig sind
    /// </summary>
    public bool IsValid => Errors.Count == 0 && !ShowHelp && !string.IsNullOrEmpty(Path);
}
