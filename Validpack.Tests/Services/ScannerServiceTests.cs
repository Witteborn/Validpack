using Validpack.Models;
using Validpack.Services;

namespace Validpack.Tests.Services;

public class ScannerServiceTests : IDisposable
{
    private readonly string _tempDir;

    public ScannerServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ScannerServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task ScanAsync_EmptyDirectory_ReturnsEmptyResult()
    {
        var config = new Configuration();
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Empty(result.AllDependencies);
        Assert.Empty(result.UniqueDependencies);
        Assert.Empty(result.ValidationResults);
        Assert.False(result.HasProblems);
    }

    [Fact]
    public async Task ScanAsync_SetsScannedPath()
    {
        var config = new Configuration();
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Equal(Path.GetFullPath(_tempDir), result.ScannedPath);
    }

    [Fact]
    public async Task ScanAsync_FindsPackageJson()
    {
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
        var config = new Configuration();
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Single(result.ScannedFiles);
        Assert.Contains(result.ScannedFiles, f => f.EndsWith("package.json"));
    }

    [Fact]
    public async Task ScanAsync_FindsCsproj()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.csproj"), "<Project></Project>");
        var config = new Configuration();
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Single(result.ScannedFiles);
        Assert.Contains(result.ScannedFiles, f => f.EndsWith(".csproj"));
    }

    [Fact]
    public async Task ScanAsync_WhitelistedPackage_MarkedAsWhitelisted()
    {
        var packageJson = @"{
            ""dependencies"": {
                ""internal-package"": ""1.0.0""
            }
        }";
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJson);

        var config = new Configuration
        {
            Whitelist = new List<string> { "internal-package" }
        };
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Single(result.ValidationResults);
        Assert.Equal(ValidationStatus.Whitelisted, result.ValidationResults[0].Status);
        Assert.False(result.HasProblems);
    }

    [Fact]
    public async Task ScanAsync_BlacklistedPackage_MarkedAsBlacklisted()
    {
        var packageJson = @"{
            ""dependencies"": {
                ""bad-package"": ""1.0.0""
            }
        }";
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJson);

        var config = new Configuration
        {
            Blacklist = new List<string> { "bad-package" }
        };
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Single(result.ValidationResults);
        Assert.Equal(ValidationStatus.Blacklisted, result.ValidationResults[0].Status);
        Assert.True(result.HasProblems);
    }

    [Fact]
    public async Task ScanAsync_BlacklistTakesPriority_OverWhitelist()
    {
        var packageJson = @"{
            ""dependencies"": {
                ""conflict-package"": ""1.0.0""
            }
        }";
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJson);

        var config = new Configuration
        {
            Whitelist = new List<string> { "conflict-package" },
            Blacklist = new List<string> { "conflict-package" }
        };
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Single(result.ValidationResults);
        Assert.Equal(ValidationStatus.Blacklisted, result.ValidationResults[0].Status);
    }

    [Fact]
    public async Task ScanAsync_DuplicateDependencies_AreDeduplicated()
    {
        // Create two package.json files with the same dependency
        var subDir = Path.Combine(_tempDir, "subproject");
        Directory.CreateDirectory(subDir);

        var packageJson = @"{
            ""dependencies"": {
                ""shared-dep"": ""1.0.0""
            }
        }";
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJson);
        File.WriteAllText(Path.Combine(subDir, "package.json"), packageJson);

        var config = new Configuration
        {
            Whitelist = new List<string> { "shared-dep" } // Whitelist to avoid API calls
        };
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Equal(2, result.AllDependencies.Count); // Both found
        Assert.Single(result.UniqueDependencies); // But only one unique
        Assert.Single(result.ValidationResults); // Only validated once
    }

    [Fact]
    public async Task ScanAsync_MultipleDependencyTypes_AllFound()
    {
        // Create npm and nuget files
        var packageJson = @"{
            ""dependencies"": {
                ""npm-dep"": ""1.0.0""
            }
        }";
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
            <ItemGroup>
                <PackageReference Include=""NuGet.Dep"" Version=""1.0.0"" />
            </ItemGroup>
        </Project>";

        File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJson);
        File.WriteAllText(Path.Combine(_tempDir, "Test.csproj"), csproj);

        var config = new Configuration
        {
            Whitelist = new List<string> { "npm-dep", "NuGet.Dep" }
        };
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Equal(2, result.ScannedFiles.Count);
        Assert.Equal(2, result.AllDependencies.Count);
        Assert.Contains(result.AllDependencies, d => d.Type == DependencyType.Npm);
        Assert.Contains(result.AllDependencies, d => d.Type == DependencyType.NuGet);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ScanAsync_ExistingNpmPackage_MarkedAsValid()
    {
        var packageJson = @"{
            ""dependencies"": {
                ""lodash"": ""^4.17.21""
            }
        }";
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJson);

        var config = new Configuration();
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Single(result.ValidationResults);
        Assert.Equal(ValidationStatus.Valid, result.ValidationResults[0].Status);
        Assert.False(result.HasProblems);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ScanAsync_NonExistentPackage_MarkedAsNotFound()
    {
        var packageJson = @"{
            ""dependencies"": {
                ""this-package-definitely-does-not-exist-xyz123"": ""1.0.0""
            }
        }";
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJson);

        var config = new Configuration();
        var scanner = new ScannerService(config);

        var result = await scanner.ScanAsync(_tempDir);

        Assert.Single(result.ValidationResults);
        Assert.Equal(ValidationStatus.NotFound, result.ValidationResults[0].Status);
        Assert.True(result.HasProblems);
    }
}
