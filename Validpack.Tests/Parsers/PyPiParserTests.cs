using Validpack.Models;
using Validpack.Parsers;

namespace Validpack.Tests.Parsers;

public class PyPiParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PyPiParser _parser;

    public PyPiParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PyPiParserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _parser = new PyPiParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void CanParse_RequirementsTxt_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("requirements.txt"));
        Assert.True(_parser.CanParse("requirements-dev.txt"));
        Assert.True(_parser.CanParse("REQUIREMENTS.TXT"));
    }

    [Fact]
    public void CanParse_PyprojectToml_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("pyproject.toml"));
        Assert.True(_parser.CanParse("PYPROJECT.TOML"));
    }

    [Fact]
    public void Parse_SimpleRequirements_ExtractsCorrectly()
    {
        var requirements = @"
requests==2.28.0
flask>=2.0.0
numpy
pandas~=1.5.0
";
        var filePath = Path.Combine(_tempDir, "requirements.txt");
        File.WriteAllText(filePath, requirements);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(4, deps.Count);
        Assert.Contains(deps, d => d.Name == "requests");
        Assert.Contains(deps, d => d.Name == "flask");
        Assert.Contains(deps, d => d.Name == "numpy");
        Assert.Contains(deps, d => d.Name == "pandas");
        Assert.All(deps, d => Assert.Equal(DependencyType.PyPi, d.Type));
    }

    [Fact]
    public void Parse_CommentsAndEmptyLines_IgnoresCorrectly()
    {
        var requirements = @"
# This is a comment
requests==2.28.0

# Another comment
flask>=2.0.0
";
        var filePath = Path.Combine(_tempDir, "requirements.txt");
        File.WriteAllText(filePath, requirements);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
    }

    [Fact]
    public void Parse_SkipsLocalAndGitReferences()
    {
        var requirements = @"
requests==2.28.0
-e ./local-package
git+https://github.com/user/repo.git
-r other-requirements.txt
./another-local
";
        var filePath = Path.Combine(_tempDir, "requirements.txt");
        File.WriteAllText(filePath, requirements);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("requests", deps[0].Name);
    }

    [Fact]
    public void Parse_InlineComments_HandlesCorrectly()
    {
        var requirements = @"
requests==2.28.0  # HTTP library
flask>=2.0.0 # Web framework
";
        var filePath = Path.Combine(_tempDir, "requirements.txt");
        File.WriteAllText(filePath, requirements);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "requests");
        Assert.Contains(deps, d => d.Name == "flask");
    }

    [Fact]
    public void Parse_ExtrasInPackageName_ExtractsCorrectly()
    {
        var requirements = @"
requests[security]==2.28.0
celery[redis,mongodb]>=5.0
";
        var filePath = Path.Combine(_tempDir, "requirements.txt");
        File.WriteAllText(filePath, requirements);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "requests");
        Assert.Contains(deps, d => d.Name == "celery");
    }

    [Fact]
    public void FindFiles_SkipsVirtualEnvs()
    {
        var venvDir = Path.Combine(_tempDir, "venv", "lib");
        var dotVenvDir = Path.Combine(_tempDir, ".venv");
        Directory.CreateDirectory(venvDir);
        Directory.CreateDirectory(dotVenvDir);

        File.WriteAllText(Path.Combine(_tempDir, "requirements.txt"), "requests");
        File.WriteAllText(Path.Combine(venvDir, "requirements.txt"), "internal");
        File.WriteAllText(Path.Combine(dotVenvDir, "requirements.txt"), "internal");

        var files = _parser.FindFiles(_tempDir).ToList();

        Assert.Single(files);
        Assert.DoesNotContain(files, f => f.Contains("venv"));
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmpty()
    {
        var filePath = Path.Combine(_tempDir, "requirements.txt");
        File.WriteAllText(filePath, "");

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }
}
