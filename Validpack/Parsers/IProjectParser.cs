using Validpack.Models;

namespace Validpack.Parsers;

/// <summary>
/// Interface für Projekt-Parser
/// </summary>
public interface IProjectParser
{
    /// <summary>
    /// Typ der Abhängigkeiten, die dieser Parser findet
    /// </summary>
    DependencyType DependencyType { get; }
    
    /// <summary>
    /// Dateiname/-muster nach dem gesucht wird
    /// </summary>
    string FilePattern { get; }
    
    /// <summary>
    /// Prüft ob eine Datei von diesem Parser verarbeitet werden kann
    /// </summary>
    bool CanParse(string filePath);
    
    /// <summary>
    /// Parst eine Projektdatei und extrahiert Abhängigkeiten
    /// </summary>
    IEnumerable<Dependency> Parse(string filePath);
    
    /// <summary>
    /// Findet alle relevanten Dateien in einem Verzeichnis (rekursiv)
    /// </summary>
    IEnumerable<string> FindFiles(string directory);
}
