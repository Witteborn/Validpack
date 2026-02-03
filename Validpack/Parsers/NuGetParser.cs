using System.Xml.Linq;
using Validpack.Models;

namespace Validpack.Parsers;

/// <summary>
/// Parser für NuGet .csproj Dateien
/// </summary>
public class NuGetParser : IProjectParser
{
    public DependencyType DependencyType => DependencyType.NuGet;
    public string FilePattern => "*.csproj";
    
    public bool CanParse(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".csproj", StringComparison.OrdinalIgnoreCase);
    }
    
    public IEnumerable<string> FindFiles(string directory)
    {
        if (!Directory.Exists(directory))
            yield break;
            
        foreach (var file in Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories))
        {
            // bin und obj Verzeichnisse überspringen
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.AltDirectorySeparatorChar}bin{Path.AltDirectorySeparatorChar}") ||
                file.Contains($"{Path.AltDirectorySeparatorChar}obj{Path.AltDirectorySeparatorChar}"))
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
            
        // PackageReference Elemente finden (mit und ohne Namespace)
        var packageReferences = doc.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference");
            
        foreach (var packageRef in packageReferences)
        {
            var name = packageRef.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(name))
                continue;
                
            // Version kann als Attribut oder als Child-Element sein
            var version = packageRef.Attribute("Version")?.Value;
            if (string.IsNullOrEmpty(version))
            {
                version = packageRef.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "Version")?.Value;
            }
            
            yield return new Dependency(name, version, DependencyType.NuGet, filePath);
        }
    }
}
