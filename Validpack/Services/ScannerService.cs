using Validpack.Models;
using Validpack.Parsers;
using Validpack.Validators;

namespace Validpack.Services;

/// <summary>
/// Haupt-Service für das Scannen von Projekten
/// </summary>
public class ScannerService
{
    private readonly List<IProjectParser> _parsers;
    private readonly Dictionary<DependencyType, IPackageValidator> _validators;
    private readonly Configuration _config;
    private readonly bool _verbose;
    
    public ScannerService(Configuration config, bool verbose = false)
    {
        _config = config;
        _verbose = verbose;
        
        _parsers = new List<IProjectParser>
        {
            new NpmParser(),
            new NuGetParser(),
            new PyPiParser(),
            new CratesParser(),
            new MavenParser(),
            new GradleParser()
        };
        
        _validators = new Dictionary<DependencyType, IPackageValidator>
        {
            { DependencyType.Npm, new NpmValidator() },
            { DependencyType.NuGet, new NuGetValidator() },
            { DependencyType.PyPi, new PyPiValidator() },
            { DependencyType.Crates, new CratesValidator() },
            { DependencyType.Maven, new MavenValidator() },
            { DependencyType.Gradle, new GradleValidator() }
        };
    }
    
    /// <summary>
    /// Scannt ein Verzeichnis auf Abhängigkeiten und validiert sie
    /// </summary>
    public async Task<ScanResult> ScanAsync(string directory)
    {
        var result = new ScanResult
        {
            ScannedPath = Path.GetFullPath(directory)
        };
        
        // Schritt 1: Alle Projektdateien finden und parsen
        Log($"Scanne Verzeichnis: {result.ScannedPath}");
        
        foreach (var parser in _parsers)
        {
            var files = parser.FindFiles(directory).ToList();
            result.ScannedFiles.AddRange(files);
            
            foreach (var file in files)
            {
                Log($"  Gefunden: {Path.GetRelativePath(directory, file)}");
                var dependencies = parser.Parse(file);
                result.AllDependencies.AddRange(dependencies);
            }
        }
        
        Log($"\nGefundene Abhängigkeiten: {result.AllDependencies.Count}");
        
        // Schritt 2: Deduplizieren
        var uniqueDeps = result.AllDependencies
            .GroupBy(d => d.Key)
            .Select(g => g.First())
            .ToList();
        result.UniqueDependencies.AddRange(uniqueDeps);
        
        Log($"Eindeutige Abhängigkeiten: {result.UniqueDependencies.Count}");
        
        // Schritt 3: Validieren
        Log("\nValidiere Abhängigkeiten...");
        
        foreach (var dep in result.UniqueDependencies)
        {
            var validationResult = await ValidateDependencyAsync(dep);
            result.ValidationResults.Add(validationResult);
            
            var statusSymbol = validationResult.Status switch
            {
                ValidationStatus.Valid => "[OK]",
                ValidationStatus.NotFound => "[NICHT GEFUNDEN]",
                ValidationStatus.Blacklisted => "[BLACKLISTED]",
                ValidationStatus.Whitelisted => "[WHITELIST]",
                ValidationStatus.Error => "[FEHLER]",
                _ => "[?]"
            };
            
            Log($"  {statusSymbol} {dep.Type}: {dep.Name}");
        }
        
        return result;
    }
    
    private async Task<ValidationResult> ValidateDependencyAsync(Dependency dep)
    {
        // Blacklist-Check (hat Priorität)
        if (_config.IsBlacklisted(dep.Name))
        {
            return new ValidationResult(dep, ValidationStatus.Blacklisted, 
                "Paket steht auf der Blacklist");
        }
        
        // Whitelist-Check
        if (_config.IsWhitelisted(dep.Name))
        {
            return new ValidationResult(dep, ValidationStatus.Whitelisted,
                "Paket steht auf der Whitelist (übersprungen)");
        }
        
        // API-Validierung
        if (!_validators.TryGetValue(dep.Type, out var validator))
        {
            return new ValidationResult(dep, ValidationStatus.Error,
                $"Kein Validator für {dep.Type}");
        }
        
        var exists = await validator.ValidateAsync(dep.Name);
        
        if (exists == true)
        {
            return new ValidationResult(dep, ValidationStatus.Valid,
                "Paket existiert in der Registry");
        }
        
        if (exists == false)
        {
            return new ValidationResult(dep, ValidationStatus.NotFound,
                "WARNUNG: Paket existiert nicht in der Registry!");
        }
        
        return new ValidationResult(dep, ValidationStatus.Error,
            "Fehler bei der API-Abfrage");
    }
    
    private void Log(string message)
    {
        if (_verbose)
        {
            Console.WriteLine(message);
        }
    }
}
