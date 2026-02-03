using Validpack.Models;
using Validpack.Utils;

namespace Validpack.Validators;

/// <summary>
/// Validator für NuGet-Pakete über die NuGet API v3
/// </summary>
public class NuGetValidator : IPackageValidator
{
    private const string NuGetApiBaseUrl = "https://api.nuget.org/v3-flatcontainer/";
    
    public DependencyType DependencyType => DependencyType.NuGet;
    
    public async Task<bool?> ValidateAsync(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return false;
            
        // NuGet Paketnamen müssen lowercase sein für die API
        var lowercaseName = packageName.ToLowerInvariant();
        var url = $"{NuGetApiBaseUrl}{lowercaseName}/index.json";
        
        return await HttpHelper.CheckUrlExistsAsync(url);
    }
}
