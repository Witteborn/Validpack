using Validpack.Models;
using Validpack.Parsers;

namespace Validpack.Tests.Parsers;

public class GradleParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly GradleParser _parser;

    public GradleParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GradleParserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _parser = new GradleParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void CanParse_BuildGradle_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("build.gradle"));
        Assert.True(_parser.CanParse("BUILD.GRADLE"));
        Assert.True(_parser.CanParse("/some/path/build.gradle"));
    }

    [Fact]
    public void CanParse_BuildGradleKts_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("build.gradle.kts"));
        Assert.True(_parser.CanParse("BUILD.GRADLE.KTS"));
    }

    [Fact]
    public void CanParse_OtherFiles_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("settings.gradle"));
        Assert.False(_parser.CanParse("pom.xml"));
        Assert.False(_parser.CanParse("gradle.properties"));
    }

    [Fact]
    public void Parse_GroovySingleQuotes_ExtractsCorrectly()
    {
        var buildGradle = @"
dependencies {
    implementation 'com.google.guava:guava:31.0-jre'
    testImplementation 'junit:junit:4.13.2'
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "com.google.guava:guava" && d.Version == "31.0-jre");
        Assert.Contains(deps, d => d.Name == "junit:junit" && d.Version == "4.13.2");
        Assert.All(deps, d => Assert.Equal(DependencyType.Gradle, d.Type));
    }

    [Fact]
    public void Parse_GroovyDoubleQuotes_ExtractsCorrectly()
    {
        var buildGradle = @"
dependencies {
    implementation ""org.apache.commons:commons-lang3:3.12.0""
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("org.apache.commons:commons-lang3", deps[0].Name);
        Assert.Equal("3.12.0", deps[0].Version);
    }

    [Fact]
    public void Parse_KotlinDsl_ExtractsCorrectly()
    {
        var buildGradleKts = @"
dependencies {
    implementation(""com.google.guava:guava:31.0-jre"")
    api(""org.slf4j:slf4j-api:2.0.0"")
    testImplementation(""junit:junit:4.13.2"")
}";
        var filePath = Path.Combine(_tempDir, "build.gradle.kts");
        File.WriteAllText(filePath, buildGradleKts);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(3, deps.Count);
        Assert.Contains(deps, d => d.Name == "com.google.guava:guava");
        Assert.Contains(deps, d => d.Name == "org.slf4j:slf4j-api");
        Assert.Contains(deps, d => d.Name == "junit:junit");
    }

    [Fact]
    public void Parse_AllConfigurations_ExtractsCorrectly()
    {
        var buildGradle = @"
dependencies {
    implementation 'group:artifact1:1.0'
    api 'group:artifact2:1.0'
    compileOnly 'group:artifact3:1.0'
    runtimeOnly 'group:artifact4:1.0'
    testImplementation 'group:artifact5:1.0'
    annotationProcessor 'group:artifact6:1.0'
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Equal(6, deps.Count);
    }

    [Fact]
    public void Parse_ProjectDependencies_Skipped()
    {
        var buildGradle = @"
dependencies {
    implementation 'com.google.guava:guava:31.0-jre'
    implementation project(':submodule')
    implementation project("":another-module"")
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("com.google.guava:guava", deps[0].Name);
    }

    [Fact]
    public void Parse_FileDependencies_Skipped()
    {
        var buildGradle = @"
dependencies {
    implementation 'com.google.guava:guava:31.0-jre'
    implementation files('libs/local.jar')
    implementation fileTree(dir: 'libs', include: ['*.jar'])
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
    }

    [Fact]
    public void Parse_VariableReferences_Skipped()
    {
        var buildGradle = @"
dependencies {
    implementation 'com.google.guava:guava:31.0-jre'
    implementation ""com.example:lib:${version}""
    implementation ""com.example:$artifactName:1.0""
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("com.google.guava:guava", deps[0].Name);
    }

    [Fact]
    public void Parse_NoVersion_StillExtracts()
    {
        var buildGradle = @"
dependencies {
    implementation 'com.google.guava:guava'
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("com.google.guava:guava", deps[0].Name);
        Assert.Null(deps[0].Version);
    }

    [Fact]
    public void Parse_WithClassifier_ExtractsCorrectly()
    {
        var buildGradle = @"
dependencies {
    implementation 'com.example:artifact:1.0@jar'
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("com.example:artifact", deps[0].Name);
        Assert.Equal("1.0", deps[0].Version);
    }

    [Fact]
    public void Parse_Comments_IgnoresCorrectly()
    {
        var buildGradle = @"
dependencies {
    // This is a comment
    implementation 'com.google.guava:guava:31.0-jre'
    // implementation 'commented:out:1.0'
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Single(deps);
        Assert.Equal("com.google.guava:guava", deps[0].Name);
    }

    [Fact]
    public void FindFiles_SkipsBuildDir()
    {
        var buildDir = Path.Combine(_tempDir, "build", "generated");
        Directory.CreateDirectory(buildDir);

        File.WriteAllText(Path.Combine(_tempDir, "build.gradle"), "dependencies {}");
        File.WriteAllText(Path.Combine(buildDir, "build.gradle"), "dependencies {}");

        var files = _parser.FindFiles(_tempDir).ToList();

        Assert.Single(files);
        // Check that the file in build directory is not included
        Assert.DoesNotContain(files, f => f.Contains($"{Path.DirectorySeparatorChar}build{Path.DirectorySeparatorChar}"));
    }

    [Fact]
    public void FindFiles_SkipsGradleDir()
    {
        var gradleDir = Path.Combine(_tempDir, ".gradle", "caches");
        Directory.CreateDirectory(gradleDir);

        File.WriteAllText(Path.Combine(_tempDir, "build.gradle"), "dependencies {}");
        File.WriteAllText(Path.Combine(gradleDir, "build.gradle"), "dependencies {}");

        var files = _parser.FindFiles(_tempDir).ToList();

        Assert.Single(files);
        // Check that the file in .gradle directory is not included
        Assert.DoesNotContain(files, f => f.Contains($"{Path.DirectorySeparatorChar}.gradle{Path.DirectorySeparatorChar}"));
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmpty()
    {
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, "");

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }

    [Fact]
    public void Parse_NoDependenciesBlock_ReturnsEmpty()
    {
        var buildGradle = @"
plugins {
    id 'java'
}

repositories {
    mavenCentral()
}";
        var filePath = Path.Combine(_tempDir, "build.gradle");
        File.WriteAllText(filePath, buildGradle);

        var deps = _parser.Parse(filePath).ToList();

        Assert.Empty(deps);
    }
}
