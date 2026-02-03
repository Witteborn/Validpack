namespace Validpack.Models;

/// <summary>
/// Konfiguration für den Scanner (Whitelist/Blacklist)
/// </summary>
public class Configuration
{
    /// <summary>
    /// Pakete auf der Whitelist werden nicht geprüft.
    /// Nützlich für interne Pakete oder bekannte False Positives.
    /// </summary>
    public List<string> Whitelist { get; set; } = new();
    
    /// <summary>
    /// Pakete auf der Blacklist werden sofort als Problem markiert.
    /// Nützlich um bestimmte Pakete in Projekten zu verbieten.
    /// </summary>
    public List<string> Blacklist { get; set; } = new();
    
    /// <summary>
    /// Prüft ob ein Paketname auf der Whitelist steht (case-insensitive)
    /// </summary>
    public bool IsWhitelisted(string packageName)
    {
        return Whitelist.Any(w => 
            string.Equals(w, packageName, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Prüft ob ein Paketname auf der Blacklist steht (case-insensitive)
    /// </summary>
    public bool IsBlacklisted(string packageName)
    {
        return Blacklist.Any(b => 
            string.Equals(b, packageName, StringComparison.OrdinalIgnoreCase));
    }
}
