using Validpack.Models;
using Validpack.Utils;

namespace Validpack.Validators;

/// <summary>
/// Validator für Maven-Pakete über Maven Central
/// </summary>
public class MavenValidator : IPackageValidator
{
    private const string MavenCentralBaseUrl = "https://repo1.maven.org/maven2/";
    
    public DependencyType DependencyType => DependencyType.Maven;
    
    public async Task<bool?> ValidateAsync(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return false;
        
        // packageName ist im Format "groupId:artifactId"
        var parts = packageName.Split(':');
        if (parts.Length != 2)
            return false;
        
        var groupId = parts[0];
        var artifactId = parts[1];
        
        // groupId mit Punkten wird zu Pfad mit Slashes
        // z.B. "com.google.guava" -> "com/google/guava"
        var groupPath = groupId.Replace('.', '/');
        
        // URL zum maven-metadata.xml
        var url = $"{MavenCentralBaseUrl}{groupPath}/{artifactId}/maven-metadata.xml";
        
        return await HttpHelper.CheckUrlExistsAsync(url);
    }
}
