using Validpack.Models;
using Validpack.Parsers;

namespace Validpack.Tests.Parsers;

public class NuGetParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly NuGetParser _parser;

    public NuGetParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"NuGetParserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _parser = new NuGetParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void CanParse_CsprojFile_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("Project.csproj"));
        Assert.True(_parser.CanParse("PROJECT.CSPROJ"));
        Assert.True(_parser.CanParse("/some/path/MyApp.csproj"));
    }

    [Fact]
    public void CanParse_OtherFiles_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("package.json"));
        Assert.False(_parser.CanParse("project.fsproj"));
        Assert.False(_parser.CanParse("app.config"));
    }

    [Fact]
    public void Parse_PackageReferences_ExtractsCorrectly()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
            <ItemGroup>
                <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
                <PackageReference Include=""Serilog"" Version=""3.0.0"" />
            </ItemGroup>
        </Project>";
        var filePath = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(filePath, csproj);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "Newtonsoft.Json" && d.Version == "13.0.1");
        Assert.Contains(deps, d => d.Name == "Serilog" && d.Version == "3.0.0");
        Assert.All(deps, d => Assert.Equal(DependencyType.NuGet, d.Type));
    }

    [Fact]
    public void Parse_VersionAsChildElement_ExtractsCorrectly()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
            <ItemGroup>
                <PackageReference Include=""Microsoft.Extensions.Logging"">
                    <Version>8.0.0</Version>
                </PackageReference>
            </ItemGroup>
        </Project>";
        var filePath = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(filePath, csproj);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("Microsoft.Extensions.Logging", deps[0].Name);
        Assert.Equal("8.0.0", deps[0].Version);
    }

    [Fact]
    public void Parse_NoVersion_StillExtracts()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
            <ItemGroup>
                <PackageReference Include=""SomePackage"" />
            </ItemGroup>
        </Project>";
        var filePath = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(filePath, csproj);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("SomePackage", deps[0].Name);
        Assert.Null(deps[0].Version);
    }

    [Fact]
    public void Parse_WithNamespace_ExtractsCorrectly()
    {
        var csproj = @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
            <ItemGroup>
                <PackageReference Include=""TestPackage"" Version=""1.0.0"" />
            </ItemGroup>
        </Project>";
        var filePath = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(filePath, csproj);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("TestPackage", deps[0].Name);
    }

    [Fact]
    public void Parse_EmptyProject_ReturnsEmpty()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk""></Project>";
        var filePath = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(filePath, csproj);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }

    [Fact]
    public void Parse_InvalidXml_ReturnsEmpty()
    {
        var filePath = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(filePath, "not valid xml");

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }

    [Fact]
    public void FindFiles_SkipsBinAndObj()
    {
        var binDir = Path.Combine(_tempDir, "bin", "Debug");
        var objDir = Path.Combine(_tempDir, "obj");
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(objDir);

        File.WriteAllText(Path.Combine(_tempDir, "App.csproj"), "<Project/>");
        File.WriteAllText(Path.Combine(binDir, "Generated.csproj"), "<Project/>");
        File.WriteAllText(Path.Combine(objDir, "Generated.csproj"), "<Project/>");

        var files = _parser.FindFiles(_tempDir).ToList();

        Assert.Single(files);
        Assert.DoesNotContain(files, f => f.Contains("bin") || f.Contains("obj"));
    }

    [Fact]
    public void Parse_MultipleItemGroups_ExtractsAll()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
            <ItemGroup>
                <PackageReference Include=""Package1"" Version=""1.0.0"" />
            </ItemGroup>
            <ItemGroup>
                <PackageReference Include=""Package2"" Version=""2.0.0"" />
            </ItemGroup>
        </Project>";
        var filePath = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(filePath, csproj);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
    }
}
