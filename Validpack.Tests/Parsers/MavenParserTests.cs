using Validpack.Models;
using Validpack.Parsers;

namespace Validpack.Tests.Parsers;

public class MavenParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MavenParser _parser;

    public MavenParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"MavenParserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _parser = new MavenParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void CanParse_PomXml_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("pom.xml"));
        Assert.True(_parser.CanParse("POM.XML"));
        Assert.True(_parser.CanParse("/some/path/pom.xml"));
    }

    [Fact]
    public void CanParse_OtherFiles_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("package.json"));
        Assert.False(_parser.CanParse("build.gradle"));
        Assert.False(_parser.CanParse("pom.xml.bak"));
    }

    [Fact]
    public void Parse_SimpleDependencies_ExtractsCorrectly()
    {
        var pomXml = @"<?xml version=""1.0""?>
<project>
    <dependencies>
        <dependency>
            <groupId>com.google.guava</groupId>
            <artifactId>guava</artifactId>
            <version>31.0-jre</version>
        </dependency>
        <dependency>
            <groupId>junit</groupId>
            <artifactId>junit</artifactId>
            <version>4.13.2</version>
        </dependency>
    </dependencies>
</project>";
        var filePath = Path.Combine(_tempDir, "pom.xml");
        File.WriteAllText(filePath, pomXml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "com.google.guava:guava" && d.Version == "31.0-jre");
        Assert.Contains(deps, d => d.Name == "junit:junit" && d.Version == "4.13.2");
        Assert.All(deps, d => Assert.Equal(DependencyType.Maven, d.Type));
    }

    [Fact]
    public void Parse_WithNamespace_ExtractsCorrectly()
    {
        var pomXml = @"<?xml version=""1.0""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"">
    <dependencies>
        <dependency>
            <groupId>org.apache.commons</groupId>
            <artifactId>commons-lang3</artifactId>
            <version>3.12.0</version>
        </dependency>
    </dependencies>
</project>";
        var filePath = Path.Combine(_tempDir, "pom.xml");
        File.WriteAllText(filePath, pomXml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("org.apache.commons:commons-lang3", deps[0].Name);
    }

    [Fact]
    public void Parse_NoVersion_StillExtracts()
    {
        var pomXml = @"<?xml version=""1.0""?>
<project>
    <dependencies>
        <dependency>
            <groupId>com.example</groupId>
            <artifactId>some-lib</artifactId>
        </dependency>
    </dependencies>
</project>";
        var filePath = Path.Combine(_tempDir, "pom.xml");
        File.WriteAllText(filePath, pomXml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("com.example:some-lib", deps[0].Name);
        Assert.Null(deps[0].Version);
    }

    [Fact]
    public void Parse_PropertyVersions_Skipped()
    {
        var pomXml = @"<?xml version=""1.0""?>
<project>
    <dependencies>
        <dependency>
            <groupId>${project.groupId}</groupId>
            <artifactId>internal</artifactId>
            <version>1.0.0</version>
        </dependency>
        <dependency>
            <groupId>com.google.guava</groupId>
            <artifactId>guava</artifactId>
            <version>31.0</version>
        </dependency>
    </dependencies>
</project>";
        var filePath = Path.Combine(_tempDir, "pom.xml");
        File.WriteAllText(filePath, pomXml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("com.google.guava:guava", deps[0].Name);
    }

    [Fact]
    public void Parse_NestedDependencies_ExtractsAll()
    {
        var pomXml = @"<?xml version=""1.0""?>
<project>
    <dependencies>
        <dependency>
            <groupId>com.example</groupId>
            <artifactId>dep1</artifactId>
            <version>1.0</version>
        </dependency>
    </dependencies>
    <dependencyManagement>
        <dependencies>
            <dependency>
                <groupId>com.example</groupId>
                <artifactId>dep2</artifactId>
                <version>2.0</version>
            </dependency>
        </dependencies>
    </dependencyManagement>
</project>";
        var filePath = Path.Combine(_tempDir, "pom.xml");
        File.WriteAllText(filePath, pomXml);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
    }

    [Fact]
    public void FindFiles_SkipsTargetDir()
    {
        var targetDir = Path.Combine(_tempDir, "target");
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(_tempDir, "pom.xml"), "<project/>");
        File.WriteAllText(Path.Combine(targetDir, "pom.xml"), "<project/>");

        var files = _parser.FindFiles(_tempDir).ToList();

        Assert.Single(files);
        Assert.DoesNotContain(files, f => f.Contains("target"));
    }

    [Fact]
    public void Parse_InvalidXml_ReturnsEmpty()
    {
        var filePath = Path.Combine(_tempDir, "pom.xml");
        File.WriteAllText(filePath, "not valid xml");

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }

    [Fact]
    public void Parse_EmptyProject_ReturnsEmpty()
    {
        var filePath = Path.Combine(_tempDir, "pom.xml");
        File.WriteAllText(filePath, "<project></project>");

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }
}
