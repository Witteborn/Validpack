using Validpack.Models;

namespace Validpack.Validators;

/// <summary>
/// Interface für Paket-Validatoren
/// </summary>
public interface IPackageValidator
{
    /// <summary>
    /// Typ der Abhängigkeiten, die dieser Validator prüft
    /// </summary>
    DependencyType DependencyType { get; }
    
    /// <summary>
    /// Prüft ob ein Paket in der Registry existiert
    /// </summary>
    /// <returns>True = existiert, False = nicht gefunden, null = Fehler</returns>
    Task<bool?> ValidateAsync(string packageName);
}
