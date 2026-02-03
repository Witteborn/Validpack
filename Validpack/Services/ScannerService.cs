using System.Text.RegularExpressions;
using Validpack.Models;
using Validpack.Parsers;
using Validpack.Validators;

namespace Validpack.Services;

/// <summary>
/// Main service for scanning projects
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
    /// Scans a directory for dependencies and validates them
    /// </summary>
    public async Task<ScanResult> ScanAsync(string directory)
    {
        var result = new ScanResult
        {
            ScannedPath = Path.GetFullPath(directory)
        };

        // Step 1: Find and parse all project files
        Log($"Scanning directory: {result.ScannedPath}");

        foreach (var parser in _parsers)
        {
            var files = parser.FindFiles(directory)
                .Where(f => !IsExcluded(f, directory))
                .ToList();

            result.ScannedFiles.AddRange(files);

            foreach (var file in files)
            {
                Log($"  Found: {Path.GetRelativePath(directory, file)}");
                var dependencies = parser.Parse(file);
                result.AllDependencies.AddRange(dependencies);
            }
        }

        Log($"\nTotal dependencies: {result.AllDependencies.Count}");

        // Step 2: Deduplicate
        var uniqueDeps = result.AllDependencies
            .GroupBy(d => d.Key)
            .Select(g => g.First())
            .ToList();
        result.UniqueDependencies.AddRange(uniqueDeps);

        Log($"Unique dependencies: {result.UniqueDependencies.Count}");

        // Step 3: Validate
        Log("\nValidating dependencies...");

        foreach (var dep in result.UniqueDependencies)
        {
            var validationResult = await ValidateDependencyAsync(dep);
            result.ValidationResults.Add(validationResult);

            var statusSymbol = validationResult.Status switch
            {
                ValidationStatus.Valid => "[OK]",
                ValidationStatus.NotFound => "[NOT FOUND]",
                ValidationStatus.Blacklisted => "[BLACKLISTED]",
                ValidationStatus.Whitelisted => "[WHITELIST]",
                ValidationStatus.Error => "[ERROR]",
                _ => "[?]"
            };

            Log($"  {statusSymbol} {dep.Type}: {dep.Name}");
        }

        return result;
    }

    private bool IsExcluded(string filePath, string baseDirectory)
    {
        if (_config.Exclude.Count == 0)
            return false;

        var relativePath = Path.GetRelativePath(baseDirectory, filePath)
            .Replace('\\', '/');

        foreach (var pattern in _config.Exclude)
        {
            if (MatchesGlobPattern(relativePath, pattern))
            {
                Log($"  Excluded: {relativePath} (pattern: {pattern})");
                return true;
            }
        }

        return false;
    }

    private static bool MatchesGlobPattern(string path, string pattern)
    {
        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }

    private async Task<ValidationResult> ValidateDependencyAsync(Dependency dep)
    {
        // Blacklist check (takes priority)
        if (_config.IsBlacklisted(dep.Name))
        {
            return new ValidationResult(dep, ValidationStatus.Blacklisted,
                "Package is on the blacklist");
        }

        // Whitelist check
        if (_config.IsWhitelisted(dep.Name))
        {
            return new ValidationResult(dep, ValidationStatus.Whitelisted,
                "Package is on the whitelist (skipped)");
        }

        // API validation
        if (!_validators.TryGetValue(dep.Type, out var validator))
        {
            return new ValidationResult(dep, ValidationStatus.Error,
                $"No validator for {dep.Type}");
        }

        var exists = await validator.ValidateAsync(dep.Name);

        if (exists == true)
        {
            return new ValidationResult(dep, ValidationStatus.Valid,
                "Package exists in registry");
        }

        if (exists == false)
        {
            return new ValidationResult(dep, ValidationStatus.NotFound,
                "WARNING: Package does not exist in registry!");
        }

        return new ValidationResult(dep, ValidationStatus.Error,
            "Error during API request");
    }

    private void Log(string message)
    {
        if (_verbose)
        {
            Console.WriteLine(message);
        }
    }
}
