namespace Validpack.Models;

/// <summary>
/// Gesamtergebnis eines Scans
/// </summary>
public class ScanResult
{
    /// <summary>
    /// Pfad zum gescannten Verzeichnis
    /// </summary>
    public required string ScannedPath { get; init; }
    
    /// <summary>
    /// Zeitpunkt des Scans
    /// </summary>
    public DateTime ScanTime { get; init; } = DateTime.Now;
    
    /// <summary>
    /// Liste aller gefundenen Abhängigkeiten
    /// </summary>
    public List<Dependency> AllDependencies { get; init; } = new();
    
    /// <summary>
    /// Liste der deduplizierten Abhängigkeiten (für API-Calls)
    /// </summary>
    public List<Dependency> UniqueDependencies { get; init; } = new();
    
    /// <summary>
    /// Validierungsergebnisse
    /// </summary>
    public List<ValidationResult> ValidationResults { get; init; } = new();
    
    /// <summary>
    /// Gefundene Projektdateien (package.json, .csproj)
    /// </summary>
    public List<string> ScannedFiles { get; init; } = new();
    
    /// <summary>
    /// Gibt an, ob Probleme gefunden wurden
    /// </summary>
    public bool HasProblems => ValidationResults.Any(r => r.HasProblem);
    
    /// <summary>
    /// Anzahl der Pakete, die nicht gefunden wurden
    /// </summary>
    public int NotFoundCount => ValidationResults.Count(r => r.Status == ValidationStatus.NotFound);
    
    /// <summary>
    /// Anzahl der Pakete auf der Blacklist
    /// </summary>
    public int BlacklistedCount => ValidationResults.Count(r => r.Status == ValidationStatus.Blacklisted);
    
    /// <summary>
    /// Anzahl der validen Pakete
    /// </summary>
    public int ValidCount => ValidationResults.Count(r => r.Status == ValidationStatus.Valid);
    
    /// <summary>
    /// Anzahl der Pakete auf der Whitelist
    /// </summary>
    public int WhitelistedCount => ValidationResults.Count(r => r.Status == ValidationStatus.Whitelisted);
}
