using Validpack.Models;
using Validpack.Utils;

namespace Validpack.Validators;

/// <summary>
/// Validator für npm-Pakete über die npm Registry API
/// </summary>
public class NpmValidator : IPackageValidator
{
    private const string NpmRegistryBaseUrl = "https://registry.npmjs.org/";
    
    public DependencyType DependencyType => DependencyType.Npm;
    
    public async Task<bool?> ValidateAsync(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return false;
            
        // npm Paketnamen können @ enthalten (scoped packages)
        // z.B. @angular/core -> URL: https://registry.npmjs.org/@angular%2Fcore
        var encodedName = Uri.EscapeDataString(packageName);
        var url = $"{NpmRegistryBaseUrl}{encodedName}";
        
        return await HttpHelper.CheckUrlExistsAsync(url);
    }
}
