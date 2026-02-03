using Validpack.Models;
using Validpack.Utils;

namespace Validpack.Validators;

/// <summary>
/// Validator für Rust Crates über die crates.io API
/// </summary>
public class CratesValidator : IPackageValidator
{
    private const string CratesIoApiBaseUrl = "https://crates.io/api/v1/crates/";
    
    public DependencyType DependencyType => DependencyType.Crates;
    
    public async Task<bool?> ValidateAsync(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return false;
        
        // Crate-Namen sind case-sensitive, aber wir verwenden lowercase
        var url = $"{CratesIoApiBaseUrl}{packageName}";
        
        return await HttpHelper.CheckUrlExistsAsync(url);
    }
}
