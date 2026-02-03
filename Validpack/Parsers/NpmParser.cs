using System.Text.Json;
using Validpack.Models;

namespace Validpack.Parsers;

/// <summary>
/// Parser für npm package.json Dateien
/// </summary>
public class NpmParser : IProjectParser
{
    public DependencyType DependencyType => DependencyType.Npm;
    public string FilePattern => "package.json";
    
    public bool CanParse(string filePath)
    {
        return Path.GetFileName(filePath).Equals(FilePattern, StringComparison.OrdinalIgnoreCase);
    }
    
    public IEnumerable<string> FindFiles(string directory)
    {
        if (!Directory.Exists(directory))
            yield break;
            
        foreach (var file in Directory.EnumerateFiles(directory, FilePattern, SearchOption.AllDirectories))
        {
            // node_modules Verzeichnisse überspringen
            if (file.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.AltDirectorySeparatorChar}node_modules{Path.AltDirectorySeparatorChar}"))
            {
                continue;
            }
            
            yield return file;
        }
    }
    
    public IEnumerable<Dependency> Parse(string filePath)
    {
        if (!File.Exists(filePath))
            yield break;
            
        string json;
        try
        {
            json = File.ReadAllText(filePath);
        }
        catch
        {
            yield break;
        }
        
        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch
        {
            yield break;
        }
        
        using (doc)
        {
            var root = doc.RootElement;
            
            // dependencies
            foreach (var dep in ParseDependencySection(root, "dependencies", filePath))
            {
                yield return dep;
            }
            
            // devDependencies
            foreach (var dep in ParseDependencySection(root, "devDependencies", filePath))
            {
                yield return dep;
            }
            
            // peerDependencies
            foreach (var dep in ParseDependencySection(root, "peerDependencies", filePath))
            {
                yield return dep;
            }
            
            // optionalDependencies
            foreach (var dep in ParseDependencySection(root, "optionalDependencies", filePath))
            {
                yield return dep;
            }
        }
    }
    
    private IEnumerable<Dependency> ParseDependencySection(JsonElement root, string sectionName, string filePath)
    {
        if (!root.TryGetProperty(sectionName, out var section))
            yield break;
            
        if (section.ValueKind != JsonValueKind.Object)
            yield break;
            
        foreach (var property in section.EnumerateObject())
        {
            var name = property.Name;
            var version = property.Value.GetString();
            
            // Paketnamen die mit "file:", "link:", "git:", etc. beginnen überspringen
            // Das sind lokale oder Git-Referenzen, keine Registry-Pakete
            if (!string.IsNullOrEmpty(version) && 
                (version.StartsWith("file:") || 
                 version.StartsWith("link:") || 
                 version.StartsWith("git:") ||
                 version.StartsWith("git+") ||
                 version.StartsWith("github:") ||
                 version.StartsWith("http:") ||
                 version.StartsWith("https:")))
            {
                continue;
            }
            
            yield return new Dependency(name, version, DependencyType.Npm, filePath);
        }
    }
}
