using Validpack.Models;
using Validpack.Parsers;

namespace Validpack.Tests.Parsers;

public class NpmParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly NpmParser _parser;

    public NpmParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"NpmParserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _parser = new NpmParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void CanParse_PackageJson_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("package.json"));
        Assert.True(_parser.CanParse("PACKAGE.JSON"));
        Assert.True(_parser.CanParse("/some/path/package.json"));
    }

    [Fact]
    public void CanParse_OtherFiles_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("package.lock"));
        Assert.False(_parser.CanParse("packages.json"));
        Assert.False(_parser.CanParse(".csproj"));
    }

    [Fact]
    public void Parse_SimpleDependencies_ExtractsCorrectly()
    {
        var packageJson = @"{
            ""dependencies"": {
                ""lodash"": ""^4.17.21"",
                ""express"": ""4.18.2""
            }
        }";
        var filePath = Path.Combine(_tempDir, "package.json");
        File.WriteAllText(filePath, packageJson);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "lodash" && d.Version == "^4.17.21");
        Assert.Contains(deps, d => d.Name == "express" && d.Version == "4.18.2");
        Assert.All(deps, d => Assert.Equal(DependencyType.Npm, d.Type));
    }

    [Fact]
    public void Parse_DevDependencies_ExtractsCorrectly()
    {
        var packageJson = @"{
            ""devDependencies"": {
                ""jest"": ""^29.0.0"",
                ""typescript"": ""5.0.0""
            }
        }";
        var filePath = Path.Combine(_tempDir, "package.json");
        File.WriteAllText(filePath, packageJson);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "jest");
        Assert.Contains(deps, d => d.Name == "typescript");
    }

    [Fact]
    public void Parse_AllDependencyTypes_ExtractsAll()
    {
        var packageJson = @"{
            ""dependencies"": { ""dep1"": ""1.0.0"" },
            ""devDependencies"": { ""dev1"": ""2.0.0"" },
            ""peerDependencies"": { ""peer1"": ""3.0.0"" },
            ""optionalDependencies"": { ""opt1"": ""4.0.0"" }
        }";
        var filePath = Path.Combine(_tempDir, "package.json");
        File.WriteAllText(filePath, packageJson);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(4, deps.Count);
        Assert.Contains(deps, d => d.Name == "dep1");
        Assert.Contains(deps, d => d.Name == "dev1");
        Assert.Contains(deps, d => d.Name == "peer1");
        Assert.Contains(deps, d => d.Name == "opt1");
    }

    [Fact]
    public void Parse_LocalReferences_SkipsCorrectly()
    {
        var packageJson = @"{
            ""dependencies"": {
                ""lodash"": ""^4.17.21"",
                ""local-pkg"": ""file:../local"",
                ""git-pkg"": ""git+https://github.com/user/repo.git"",
                ""link-pkg"": ""link:../linked""
            }
        }";
        var filePath = Path.Combine(_tempDir, "package.json");
        File.WriteAllText(filePath, packageJson);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("lodash", deps[0].Name);
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmpty()
    {
        var filePath = Path.Combine(_tempDir, "package.json");
        File.WriteAllText(filePath, "{}");

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsEmpty()
    {
        var filePath = Path.Combine(_tempDir, "package.json");
        File.WriteAllText(filePath, "not valid json");

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }

    [Fact]
    public void Parse_NonExistentFile_ReturnsEmpty()
    {
        var deps = _parser.Parse("/nonexistent/package.json").ToList();

        Assert.Empty(deps);
    }

    [Fact]
    public void FindFiles_SkipsNodeModules()
    {
        // Create directory structure
        var nodeModulesDir = Path.Combine(_tempDir, "node_modules", "some-package");
        Directory.CreateDirectory(nodeModulesDir);
        
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
        File.WriteAllText(Path.Combine(nodeModulesDir, "package.json"), "{}");

        var files = _parser.FindFiles(_tempDir).ToList();

        Assert.Single(files);
        Assert.DoesNotContain(files, f => f.Contains("node_modules"));
    }

    [Fact]
    public void Parse_ScopedPackages_ExtractsCorrectly()
    {
        var packageJson = @"{
            ""dependencies"": {
                ""@angular/core"": ""^16.0.0"",
                ""@types/node"": ""^20.0.0""
            }
        }";
        var filePath = Path.Combine(_tempDir, "package.json");
        File.WriteAllText(filePath, packageJson);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "@angular/core");
        Assert.Contains(deps, d => d.Name == "@types/node");
    }
}
