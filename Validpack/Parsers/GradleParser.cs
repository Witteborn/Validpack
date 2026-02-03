using System.Text.RegularExpressions;
using Validpack.Models;

namespace Validpack.Parsers;

/// <summary>
/// Parser für Gradle build.gradle und build.gradle.kts Dateien
/// </summary>
public class GradleParser : IProjectParser
{
    public DependencyType DependencyType => DependencyType.Gradle;
    public string FilePattern => "build.gradle*";
    
    // Gradle dependency configurations
    private static readonly string[] DependencyConfigurations = new[]
    {
        "implementation", "api", "compileOnly", "runtimeOnly",
        "testImplementation", "testCompileOnly", "testRuntimeOnly",
        "androidTestImplementation", "debugImplementation", "releaseImplementation",
        "annotationProcessor", "kapt", "ksp",
        "compile", "runtime", "testCompile", "testRuntime" // Legacy configurations
    };
    
    // Regex for Groovy DSL: implementation 'group:artifact:version' or implementation "group:artifact:version"
    private static readonly Regex GroovyDependencyRegex = new(
        $@"^\s*({string.Join("|", DependencyConfigurations)})\s+['""]([^'""]+)['""]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // Regex for Kotlin DSL: implementation("group:artifact:version")
    private static readonly Regex KotlinDependencyRegex = new(
        $@"^\s*({string.Join("|", DependencyConfigurations)})\s*\(\s*['""]([^'""]+)['""]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // Regex for extracting group:artifact:version (with optional version)
    private static readonly Regex MavenCoordinateRegex = new(
        @"^([^:]+):([^:]+)(?::([^:@]+))?(?:@\w+)?$",
        RegexOptions.Compiled);
    
    public bool CanParse(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.Equals("build.gradle", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("build.gradle.kts", StringComparison.OrdinalIgnoreCase);
    }
    
    public IEnumerable<string> FindFiles(string directory)
    {
        if (!Directory.Exists(directory))
            yield break;
        
        // Find build.gradle files
        foreach (var file in Directory.EnumerateFiles(directory, "build.gradle", SearchOption.AllDirectories))
        {
            if (ShouldSkipPath(file))
                continue;
            yield return file;
        }
        
        // Find build.gradle.kts files
        foreach (var file in Directory.EnumerateFiles(directory, "build.gradle.kts", SearchOption.AllDirectories))
        {
            if (ShouldSkipPath(file))
                continue;
            yield return file;
        }
    }
    
    private bool ShouldSkipPath(string path)
    {
        var skipDirs = new[] { "build", ".gradle", "gradle", ".git" };
        
        foreach (var skipDir in skipDirs)
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}{skipDir}{Path.DirectorySeparatorChar}") ||
                path.Contains($"{Path.AltDirectorySeparatorChar}{skipDir}{Path.AltDirectorySeparatorChar}"))
            {
                return true;
            }
        }
        return false;
    }
    
    public IEnumerable<Dependency> Parse(string filePath)
    {
        if (!File.Exists(filePath))
            yield break;
        
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch
        {
            yield break;
        }
        
        bool inDependenciesBlock = false;
        int braceCount = 0;
        
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            
            // Skip comments
            if (line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*"))
                continue;
            
            // Track dependencies block
            if (line.StartsWith("dependencies", StringComparison.OrdinalIgnoreCase) && line.Contains('{'))
            {
                inDependenciesBlock = true;
                braceCount = 1;
                // Check if dependency is on the same line after {
                var afterBrace = line.Substring(line.IndexOf('{') + 1);
                if (!string.IsNullOrWhiteSpace(afterBrace))
                {
                    var dep = TryParseDependencyLine(afterBrace, filePath);
                    if (dep != null)
                        yield return dep;
                }
                continue;
            }
            
            if (inDependenciesBlock)
            {
                // Track braces
                braceCount += line.Count(c => c == '{');
                braceCount -= line.Count(c => c == '}');
                
                if (braceCount <= 0)
                {
                    inDependenciesBlock = false;
                    continue;
                }
                
                var dep = TryParseDependencyLine(line, filePath);
                if (dep != null)
                    yield return dep;
            }
        }
    }
    
    private Dependency? TryParseDependencyLine(string line, string filePath)
    {
        // Skip local project dependencies
        if (line.Contains("project(") || line.Contains("project ("))
            return null;
        
        // Skip file dependencies
        if (line.Contains("files(") || line.Contains("fileTree("))
            return null;
        
        // Skip variable references
        if (line.Contains("${") || Regex.IsMatch(line, @"\$\w+"))
            return null;
        
        // Try Kotlin DSL first (more specific)
        var match = KotlinDependencyRegex.Match(line);
        if (!match.Success)
        {
            // Try Groovy DSL
            match = GroovyDependencyRegex.Match(line);
        }
        
        if (!match.Success)
            return null;
        
        var dependencyString = match.Groups[2].Value;
        
        // Parse Maven coordinates
        var coordMatch = MavenCoordinateRegex.Match(dependencyString);
        if (!coordMatch.Success)
            return null;
        
        var groupId = coordMatch.Groups[1].Value;
        var artifactId = coordMatch.Groups[2].Value;
        var version = coordMatch.Groups[3].Success ? coordMatch.Groups[3].Value : null;
        
        // Skip if groupId or artifactId contains variables
        if (groupId.Contains("$") || artifactId.Contains("$"))
            return null;
        
        // Format as Maven coordinates: groupId:artifactId
        var name = $"{groupId}:{artifactId}";
        
        return new Dependency(name, version, DependencyType.Gradle, filePath);
    }
}
