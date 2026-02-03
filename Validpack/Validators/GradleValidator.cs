using Validpack.Models;

namespace Validpack.Validators;

/// <summary>
/// Validator für Gradle-Pakete.
/// Gradle verwendet Maven-Koordinaten, daher wird der MavenValidator wiederverwendet.
/// </summary>
public class GradleValidator : IPackageValidator
{
    private readonly MavenValidator _mavenValidator = new();
    
    public DependencyType DependencyType => DependencyType.Gradle;
    
    public Task<bool?> ValidateAsync(string packageName)
    {
        // Gradle verwendet das gleiche Format wie Maven (groupId:artifactId)
        // und die gleiche Registry (Maven Central)
        return _mavenValidator.ValidateAsync(packageName);
    }
}
