using Validpack.Validators;

namespace Validpack.Tests.Validators;

/// <summary>
/// Integration tests that make real API calls.
/// These tests verify that the validators work correctly with the actual registries.
/// Note: These tests require internet connectivity and may be slow.
/// Run with: dotnet test --filter "Category=Integration"
/// Exclude with: dotnet test --filter "Category!=Integration"
/// </summary>
[Trait("Category", "Integration")]
public class ValidatorIntegrationTests
{
    // ==================== NPM Validator ====================

    [Fact]
    public async Task NpmValidator_ExistingPackage_ReturnsTrue()
    {
        var validator = new NpmValidator();
        
        var result = await validator.ValidateAsync("lodash");
        
        Assert.True(result);
    }

    [Fact]
    public async Task NpmValidator_ScopedPackage_ReturnsTrue()
    {
        var validator = new NpmValidator();
        
        var result = await validator.ValidateAsync("@types/node");
        
        Assert.True(result);
    }

    [Fact]
    public async Task NpmValidator_NonExistentPackage_ReturnsFalse()
    {
        var validator = new NpmValidator();
        
        var result = await validator.ValidateAsync("this-package-definitely-does-not-exist-xyz123abc");
        
        Assert.False(result);
    }

    [Fact]
    public async Task NpmValidator_EmptyName_ReturnsFalse()
    {
        var validator = new NpmValidator();
        
        var result = await validator.ValidateAsync("");
        
        Assert.False(result);
    }

    // ==================== NuGet Validator ====================

    [Fact]
    public async Task NuGetValidator_ExistingPackage_ReturnsTrue()
    {
        var validator = new NuGetValidator();
        
        var result = await validator.ValidateAsync("Newtonsoft.Json");
        
        Assert.True(result);
    }

    [Fact]
    public async Task NuGetValidator_CaseInsensitive_ReturnsTrue()
    {
        var validator = new NuGetValidator();
        
        var result = await validator.ValidateAsync("NEWTONSOFT.JSON");
        
        Assert.True(result);
    }

    [Fact]
    public async Task NuGetValidator_NonExistentPackage_ReturnsFalse()
    {
        var validator = new NuGetValidator();
        
        var result = await validator.ValidateAsync("FakePackage.DoesNotExist.XYZ123");
        
        Assert.False(result);
    }

    // ==================== PyPI Validator ====================

    [Fact]
    public async Task PyPiValidator_ExistingPackage_ReturnsTrue()
    {
        var validator = new PyPiValidator();
        
        var result = await validator.ValidateAsync("requests");
        
        Assert.True(result);
    }

    [Fact]
    public async Task PyPiValidator_NonExistentPackage_ReturnsFalse()
    {
        var validator = new PyPiValidator();
        
        var result = await validator.ValidateAsync("this-package-definitely-does-not-exist-xyz123");
        
        Assert.False(result);
    }

    // ==================== Crates Validator ====================

    [Fact]
    public async Task CratesValidator_ExistingPackage_ReturnsTrue()
    {
        var validator = new CratesValidator();
        
        var result = await validator.ValidateAsync("serde");
        
        Assert.True(result);
    }

    [Fact]
    public async Task CratesValidator_NonExistentPackage_ReturnsFalse()
    {
        var validator = new CratesValidator();
        
        var result = await validator.ValidateAsync("this-crate-definitely-does-not-exist-xyz123");
        
        Assert.False(result);
    }

    // ==================== Maven Validator ====================

    [Fact]
    public async Task MavenValidator_ExistingPackage_ReturnsTrue()
    {
        var validator = new MavenValidator();
        
        var result = await validator.ValidateAsync("com.google.guava:guava");
        
        Assert.True(result);
    }

    [Fact]
    public async Task MavenValidator_NonExistentPackage_ReturnsFalse()
    {
        var validator = new MavenValidator();
        
        var result = await validator.ValidateAsync("com.fake.company:nonexistent-artifact");
        
        Assert.False(result);
    }

    [Fact]
    public async Task MavenValidator_InvalidFormat_ReturnsFalse()
    {
        var validator = new MavenValidator();
        
        // Missing artifactId
        var result = await validator.ValidateAsync("com.google.guava");
        
        Assert.False(result);
    }
}
