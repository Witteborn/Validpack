using System.Text.RegularExpressions;
using Validpack.Models;

namespace Validpack.Parsers;

/// <summary>
/// Parser für Python requirements.txt und pyproject.toml Dateien
/// </summary>
public class PyPiParser : IProjectParser
{
    public DependencyType DependencyType => DependencyType.PyPi;
    public string FilePattern => "requirements*.txt";
    
    // Regex zum Extrahieren des Paketnamens aus requirements.txt Zeilen
    // Matches: package, package==1.0, package>=1.0, package[extra], etc.
    private static readonly Regex PackageNameRegex = new(
        @"^([a-zA-Z0-9][-a-zA-Z0-9._]*)",
        RegexOptions.Compiled);
    
    public bool CanParse(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase) ||
               fileName.StartsWith("requirements", StringComparison.OrdinalIgnoreCase) && 
               fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase);
    }
    
    public IEnumerable<string> FindFiles(string directory)
    {
        if (!Directory.Exists(directory))
            yield break;
        
        // requirements*.txt Dateien finden
        foreach (var file in Directory.EnumerateFiles(directory, "requirements*.txt", SearchOption.AllDirectories))
        {
            if (ShouldSkipPath(file))
                continue;
            yield return file;
        }
        
        // pyproject.toml Dateien finden
        foreach (var file in Directory.EnumerateFiles(directory, "pyproject.toml", SearchOption.AllDirectories))
        {
            if (ShouldSkipPath(file))
                continue;
            yield return file;
        }
    }
    
    private bool ShouldSkipPath(string path)
    {
        // Python virtual environments und Cache-Verzeichnisse überspringen
        var skipDirs = new[] { "venv", ".venv", "env", ".env", "__pycache__", ".tox", "site-packages", ".git" };
        
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
        
        var fileName = Path.GetFileName(filePath);
        
        if (fileName.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var dep in ParsePyProjectToml(filePath))
                yield return dep;
        }
        else
        {
            foreach (var dep in ParseRequirementsTxt(filePath))
                yield return dep;
        }
    }
    
    private IEnumerable<Dependency> ParseRequirementsTxt(string filePath)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch
        {
            yield break;
        }
        
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            
            // Leere Zeilen und Kommentare überspringen
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;
            
            // Referenzen auf andere Dateien überspringen (-r, -c)
            if (line.StartsWith("-r") || line.StartsWith("-c") || line.StartsWith("--"))
                continue;
            
            // Editierbare Installs überspringen (-e)
            if (line.StartsWith("-e") || line.StartsWith("--editable"))
                continue;
            
            // Git, HTTP und lokale Pfade überspringen
            if (line.StartsWith("git+") || line.StartsWith("http://") || 
                line.StartsWith("https://") || line.StartsWith("file:") ||
                line.StartsWith(".") || line.StartsWith("/"))
                continue;
            
            // Inline-Kommentare entfernen
            var commentIndex = line.IndexOf('#');
            if (commentIndex > 0)
                line = line.Substring(0, commentIndex).Trim();
            
            // Paketnamen extrahieren
            var match = PackageNameRegex.Match(line);
            if (match.Success)
            {
                var packageName = match.Groups[1].Value;
                
                // Version extrahieren (falls vorhanden)
                string? version = null;
                var versionMatch = Regex.Match(line, @"[=<>!~]=?\s*([^\s,;#]+)");
                if (versionMatch.Success)
                    version = versionMatch.Groups[1].Value;
                
                yield return new Dependency(packageName, version, DependencyType.PyPi, filePath);
            }
        }
    }
    
    private IEnumerable<Dependency> ParsePyProjectToml(string filePath)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch
        {
            yield break;
        }
        
        bool inDependenciesSection = false;
        bool inArrayDependencies = false;
        
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            
            // Sektion erkennen
            if (line.StartsWith('['))
            {
                inDependenciesSection = line.Equals("[project]", StringComparison.OrdinalIgnoreCase) ||
                                        line.Contains("dependencies", StringComparison.OrdinalIgnoreCase);
                inArrayDependencies = false;
                continue;
            }
            
            if (!inDependenciesSection)
                continue;
            
            // dependencies = [...] Array Start
            if (line.StartsWith("dependencies", StringComparison.OrdinalIgnoreCase) && line.Contains('['))
            {
                inArrayDependencies = true;
                
                // Einzeilige Definition: dependencies = ["package1", "package2"]
                if (line.Contains(']'))
                {
                    foreach (var dep in ExtractPackagesFromLine(line, filePath))
                        yield return dep;
                    inArrayDependencies = false;
                }
                continue;
            }
            
            // Innerhalb des Arrays
            if (inArrayDependencies)
            {
                if (line.Contains(']'))
                {
                    foreach (var dep in ExtractPackagesFromLine(line, filePath))
                        yield return dep;
                    inArrayDependencies = false;
                }
                else
                {
                    foreach (var dep in ExtractPackagesFromLine(line, filePath))
                        yield return dep;
                }
            }
        }
    }
    
    private IEnumerable<Dependency> ExtractPackagesFromLine(string line, string filePath)
    {
        // Extrahiere Strings aus Anführungszeichen
        var matches = Regex.Matches(line, @"""([^""]+)""|'([^']+)'");
        
        foreach (Match match in matches)
        {
            var packageSpec = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            
            // Paketnamen aus der Spezifikation extrahieren
            var nameMatch = PackageNameRegex.Match(packageSpec);
            if (nameMatch.Success)
            {
                var packageName = nameMatch.Groups[1].Value;
                
                // Version extrahieren
                string? version = null;
                var versionMatch = Regex.Match(packageSpec, @"[=<>!~]=?\s*([^\s,;\]]+)");
                if (versionMatch.Success)
                    version = versionMatch.Groups[1].Value;
                
                yield return new Dependency(packageName, version, DependencyType.PyPi, filePath);
            }
        }
    }
}
