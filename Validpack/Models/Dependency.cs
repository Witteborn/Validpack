namespace Validpack.Models;

/// <summary>
/// Repräsentiert eine Projektabhängigkeit
/// </summary>
public record Dependency(
    string Name,
    string? Version,
    DependencyType Type,
    string SourceFile)
{
    /// <summary>
    /// Eindeutiger Schlüssel für Deduplizierung (Name + Type)
    /// </summary>
    public string Key => $"{Type}:{Name.ToLowerInvariant()}";
}
