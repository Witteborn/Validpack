using System.Xml.Linq;
using Validpack.Models;

namespace Validpack.Parsers;

/// <summary>
/// Parser für Maven pom.xml Dateien
/// </summary>
public class MavenParser : IProjectParser
{
    public DependencyType DependencyType => DependencyType.Maven;
    public string FilePattern => "pom.xml";
    
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
            if (ShouldSkipPath(file))
                continue;
            yield return file;
        }
    }
    
    private bool ShouldSkipPath(string path)
    {
        // Maven build-Verzeichnisse überspringen
        var skipDirs = new[] { "target", ".mvn", ".git" };
        
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
        
        XDocument? doc = null;
        try
        {
            doc = XDocument.Load(filePath);
        }
        catch
        {
            yield break;
        }
        
        if (doc.Root == null)
            yield break;
        
        // Maven Namespace (falls vorhanden)
        XNamespace? ns = doc.Root.GetDefaultNamespace();
        
        // Alle dependency Elemente finden
        var dependencies = doc.Descendants()
            .Where(e => e.Name.LocalName == "dependency");
        
        foreach (var dep in dependencies)
        {
            var groupId = dep.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "groupId")?.Value;
            var artifactId = dep.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "artifactId")?.Value;
            var version = dep.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "version")?.Value;
            
            // groupId und artifactId sind erforderlich
            if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(artifactId))
                continue;
            
            // Maven Properties wie ${project.version} überspringen
            if (groupId.Contains("${") || artifactId.Contains("${"))
                continue;
            
            // Dependency-Name ist groupId:artifactId
            var name = $"{groupId}:{artifactId}";
            
            yield return new Dependency(name, version, DependencyType.Maven, filePath);
        }
    }
}
