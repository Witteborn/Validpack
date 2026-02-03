using Validpack.Models;
using Validpack.Parsers;

namespace Validpack.Tests.Parsers;

public class CratesParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CratesParser _parser;

    public CratesParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"CratesParserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _parser = new CratesParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void CanParse_CargoToml_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("Cargo.toml"));
        Assert.True(_parser.CanParse("CARGO.TOML"));
        Assert.True(_parser.CanParse("/some/path/Cargo.toml"));
    }

    [Fact]
    public void CanParse_OtherFiles_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("package.json"));
        Assert.False(_parser.CanParse("Cargo.lock"));
        Assert.False(_parser.CanParse("cargo.toml.bak"));
    }

    [Fact]
    public void Parse_SimpleDependencies_ExtractsCorrectly()
    {
        var cargoToml = @"
[package]
name = ""my-app""
version = ""0.1.0""

[dependencies]
serde = ""1.0""
tokio = ""1.28""
";
        var filePath = Path.Combine(_tempDir, "Cargo.toml");
        File.WriteAllText(filePath, cargoToml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "serde" && d.Version == "1.0");
        Assert.Contains(deps, d => d.Name == "tokio" && d.Version == "1.28");
        Assert.All(deps, d => Assert.Equal(DependencyType.Crates, d.Type));
    }

    [Fact]
    public void Parse_TableDependencies_ExtractsCorrectly()
    {
        var cargoToml = @"
[dependencies]
tokio = { version = ""1.0"", features = [""full""] }
serde = { version = ""1.0"", features = [""derive""] }
";
        var filePath = Path.Combine(_tempDir, "Cargo.toml");
        File.WriteAllText(filePath, cargoToml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "tokio" && d.Version == "1.0");
        Assert.Contains(deps, d => d.Name == "serde" && d.Version == "1.0");
    }

    [Fact]
    public void Parse_DevDependencies_ExtractsCorrectly()
    {
        var cargoToml = @"
[dependencies]
serde = ""1.0""

[dev-dependencies]
criterion = ""0.5""
";
        var filePath = Path.Combine(_tempDir, "Cargo.toml");
        File.WriteAllText(filePath, cargoToml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "serde");
        Assert.Contains(deps, d => d.Name == "criterion");
    }

    [Fact]
    public void Parse_PathDependencies_Skipped()
    {
        var cargoToml = @"
[dependencies]
serde = ""1.0""
local-lib = { path = ""../local-lib"" }
";
        var filePath = Path.Combine(_tempDir, "Cargo.toml");
        File.WriteAllText(filePath, cargoToml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("serde", deps[0].Name);
    }

    [Fact]
    public void Parse_GitDependencies_Skipped()
    {
        var cargoToml = @"
[dependencies]
serde = ""1.0""
my-git-dep = { git = ""https://github.com/user/repo"" }
";
        var filePath = Path.Combine(_tempDir, "Cargo.toml");
        File.WriteAllText(filePath, cargoToml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("serde", deps[0].Name);
    }

    [Fact]
    public void Parse_Comments_IgnoresCorrectly()
    {
        var cargoToml = @"
[dependencies]
# This is a comment
serde = ""1.0""
# tokio = ""1.0""
";
        var filePath = Path.Combine(_tempDir, "Cargo.toml");
        File.WriteAllText(filePath, cargoToml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("serde", deps[0].Name);
    }

    [Fact]
    public void FindFiles_SkipsTargetDir()
    {
        var targetDir = Path.Combine(_tempDir, "target", "debug");
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(_tempDir, "Cargo.toml"), "[package]");
        File.WriteAllText(Path.Combine(targetDir, "Cargo.toml"), "[package]");

        var files = _parser.FindFiles(_tempDir).ToList();

        Assert.Single(files);
        Assert.DoesNotContain(files, f => f.Contains("target"));
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmpty()
    {
        var filePath = Path.Combine(_tempDir, "Cargo.toml");
        File.WriteAllText(filePath, "");

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }
}
