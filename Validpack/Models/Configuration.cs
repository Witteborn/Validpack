namespace Validpack.Models;

/// <summary>
/// Configuration for the scanner (whitelist/blacklist/exclude)
/// </summary>
public class Configuration
{
    /// <summary>
    /// Packages on the whitelist are not validated.
    /// Useful for internal packages or known false positives.
    /// </summary>
    public List<string> Whitelist { get; set; } = new();

    /// <summary>
    /// Packages on the blacklist are immediately flagged as problems.
    /// Useful to forbid certain packages in projects.
    /// </summary>
    public List<string> Blacklist { get; set; } = new();

    /// <summary>
    /// Glob patterns for paths to exclude from scanning.
    /// Example: "test-projects/**", "samples/**"
    /// </summary>
    public List<string> Exclude { get; set; } = new();

    /// <summary>
    /// Checks if a package name is on the whitelist (case-insensitive)
    /// </summary>
    public bool IsWhitelisted(string packageName)
    {
        return Whitelist.Any(w =>
            string.Equals(w, packageName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a package name is on the blacklist (case-insensitive)
    /// </summary>
    public bool IsBlacklisted(string packageName)
    {
        return Blacklist.Any(b =>
            string.Equals(b, packageName, StringComparison.OrdinalIgnoreCase));
    }
}
