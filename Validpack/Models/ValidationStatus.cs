namespace Validpack.Models;

/// <summary>
/// Status der Paketvalidierung
/// </summary>
public enum ValidationStatus
{
    /// <summary>
    /// Paket existiert in der Registry
    /// </summary>
    Valid,
    
    /// <summary>
    /// Paket existiert nicht in der Registry (Supply Chain Attack Risiko)
    /// </summary>
    NotFound,
    
    /// <summary>
    /// Paket ist auf der Blacklist
    /// </summary>
    Blacklisted,
    
    /// <summary>
    /// Paket ist auf der Whitelist (wird übersprungen)
    /// </summary>
    Whitelisted,
    
    /// <summary>
    /// Fehler bei der Validierung (z.B. API nicht erreichbar)
    /// </summary>
    Error
}
