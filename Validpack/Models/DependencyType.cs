namespace Validpack.Models;

/// <summary>
/// Typ der Abhängigkeit (Paketmanager)
/// </summary>
public enum DependencyType
{
    Npm,
    NuGet,
    PyPi,
    Crates,
    Maven,
    Gradle
}
