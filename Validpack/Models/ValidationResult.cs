namespace Validpack.Models;

/// <summary>
/// Ergebnis der Validierung einer einzelnen Abhängigkeit
/// </summary>
public record ValidationResult(
    Dependency Dependency,
    ValidationStatus Status,
    string? Message = null)
{
    /// <summary>
    /// Gibt an, ob die Validierung ein Problem gefunden hat
    /// </summary>
    public bool HasProblem => Status == ValidationStatus.NotFound || Status == ValidationStatus.Blacklisted;
}
