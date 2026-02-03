using Validpack.Models;
using Validpack.Utils;

namespace Validpack.Validators;

/// <summary>
/// Validator für Python-Pakete über die PyPI API
/// </summary>
public class PyPiValidator : IPackageValidator
{
    private const string PyPiApiBaseUrl = "https://pypi.org/pypi/";
    
    public DependencyType DependencyType => DependencyType.PyPi;
    
    public async Task<bool?> ValidateAsync(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return false;
        
        // PyPI Paketnamen sind case-insensitive und normalisiert
        // Unterstriche und Bindestriche werden gleich behandelt
        var normalizedName = packageName.ToLowerInvariant();
        var url = $"{PyPiApiBaseUrl}{normalizedName}/json";
        
        return await HttpHelper.CheckUrlExistsAsync(url);
    }
}
