using System.Text.RegularExpressions;
using Validpack.Models;

namespace Validpack.Parsers;

/// <summary>
/// Parser für Rust Cargo.toml Dateien
/// Implementiert einen einfachen TOML-Parser für die relevanten Sektionen
/// </summary>
public class CratesParser : IProjectParser
{
    public DependencyType DependencyType => DependencyType.Crates;
    public string FilePattern => "Cargo.toml";
    
    // Regex für verschiedene Dependency-Formate in Cargo.toml
    // Format 1: package = "version"
    // Format 2: package = { version = "1.0", ... }
    // Format 3: package.version = "1.0"
    private static readonly Regex SimpleDependencyRegex = new(
        @"^([a-zA-Z0-9_-]+)\s*=\s*""([^""]+)""",
        RegexOptions.Compiled);
    
    private static readonly Regex TableDependencyRegex = new(
        @"^([a-zA-Z0-9_-]+)\s*=\s*\{.*?version\s*=\s*""([^""]+)""",
        RegexOptions.Compiled);
    
    private static readonly Regex DottedDependencyRegex = new(
        @"^([a-zA-Z0-9_-]+)\.version\s*=\s*""([^""]+)""",
        RegexOptions.Compiled);
    
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
        // Rust target-Verzeichnis überspringen
        var skipDirs = new[] { "target", ".git" };
        
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
        
        bool inDependencySection = false;
        string? currentTableDependency = null;
        
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            
            // Leere Zeilen und Kommentare überspringen
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;
            
            // Neue Sektion erkennen
            if (line.StartsWith('['))
            {
                // [dependencies], [dev-dependencies], [build-dependencies]
                // [target.'cfg(...)'.dependencies] etc.
                inDependencySection = line.Contains("dependencies", StringComparison.OrdinalIgnoreCase) &&
                                      !line.Contains("[[");  // Exclude [[dependencies]] array tables
                
                // [dependencies.package] Format für detaillierte Dependency
                var tableMatch = Regex.Match(line, @"\[(?:.*\.)?dependencies\.([a-zA-Z0-9_-]+)\]");
                if (tableMatch.Success)
                {
                    currentTableDependency = tableMatch.Groups[1].Value;
                    inDependencySection = true;
                }
                else
                {
                    currentTableDependency = null;
                }
                continue;
            }
            
            if (!inDependencySection)
                continue;
            
            // [dependencies.package] Sektion - Version suchen
            if (currentTableDependency != null)
            {
                var versionMatch = Regex.Match(line, @"^version\s*=\s*""([^""]+)""");
                if (versionMatch.Success)
                {
                    yield return new Dependency(
                        currentTableDependency, 
                        versionMatch.Groups[1].Value, 
                        DependencyType.Crates, 
                        filePath);
                    continue;
                }
            }
            
            // package.version = "1.0" Format
            var dottedMatch = DottedDependencyRegex.Match(line);
            if (dottedMatch.Success)
            {
                var packageName = dottedMatch.Groups[1].Value;
                var version = dottedMatch.Groups[2].Value;
                
                // Nicht path= oder git= Dependencies
                if (!IsLocalOrGitDependency(line))
                {
                    yield return new Dependency(packageName, version, DependencyType.Crates, filePath);
                }
                continue;
            }
            
            // package = { version = "1.0", ... } Format
            var tableDepMatch = TableDependencyRegex.Match(line);
            if (tableDepMatch.Success)
            {
                var packageName = tableDepMatch.Groups[1].Value;
                var version = tableDepMatch.Groups[2].Value;
                
                // Nicht path= oder git= Dependencies
                if (!IsLocalOrGitDependency(line))
                {
                    yield return new Dependency(packageName, version, DependencyType.Crates, filePath);
                }
                continue;
            }
            
            // package = "version" Format (einfachste Form)
            var simpleMatch = SimpleDependencyRegex.Match(line);
            if (simpleMatch.Success)
            {
                var packageName = simpleMatch.Groups[1].Value;
                var version = simpleMatch.Groups[2].Value;
                
                // "version" sollte ein Versionsstring sein, nicht ein Pfad
                // Einfache Heuristik: Versionen enthalten Zahlen
                if (Regex.IsMatch(version, @"\d") && !version.Contains('/') && !version.Contains('\\'))
                {
                    yield return new Dependency(packageName, version, DependencyType.Crates, filePath);
                }
                continue;
            }
            
            // package = { path = "...", ... } oder package = { git = "...", ... }
            // Diese werden als lokale/git Dependencies erkannt und übersprungen
            var inlineTableMatch = Regex.Match(line, @"^([a-zA-Z0-9_-]+)\s*=\s*\{");
            if (inlineTableMatch.Success && !IsLocalOrGitDependency(line))
            {
                // Inline Table ohne version - könnte workspace dependency sein
                // Diese werden übersprungen, da wir die Version brauchen
            }
        }
    }
    
    private bool IsLocalOrGitDependency(string line)
    {
        return line.Contains("path") || 
               line.Contains("git") || 
               line.Contains("workspace = true");
    }
}
