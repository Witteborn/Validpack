using Validpack;

namespace Validpack.Tests.Cli;

[Collection("Console")]
public class ProgramTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;

    public ProgramTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ProgramTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _originalOut = Console.Out;
        _originalError = Console.Error;
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task Main_WithHelpFlag_ReturnsZero()
    {
        var exitCode = await Program.Main(["--help"]);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithShortHelpFlag_ReturnsZero()
    {
        var exitCode = await Program.Main(["-h"]);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithHelpFlag_PrintsHelp()
    {
        using var writer = new StringWriter();
        Console.SetOut(writer);

        await Program.Main(["--help"]);

        var output = writer.ToString();
        Assert.Contains("Validpack", output);
        Assert.Contains("--help", output);
        Assert.Contains("--config", output);
        Assert.Contains("--verbose", output);
    }

    [Fact]
    public async Task Main_WithoutArguments_ReturnsErrorCode()
    {
        using var writer = new StringWriter();
        Console.SetError(writer);

        var exitCode = await Program.Main([]);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task Main_WithoutArguments_PrintsError()
    {
        using var writer = new StringWriter();
        Console.SetError(writer);

        await Program.Main([]);

        var output = writer.ToString();
        Assert.Contains("Fehler", output);
    }

    [Fact]
    public async Task Main_WithNonExistentPath_ReturnsErrorCode()
    {
        using var writer = new StringWriter();
        Console.SetError(writer);

        var exitCode = await Program.Main(["/nonexistent/path/that/does/not/exist"]);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task Main_WithUnknownOption_ReturnsErrorCode()
    {
        using var writer = new StringWriter();
        Console.SetError(writer);

        var exitCode = await Program.Main(["--unknown-option", _tempDir]);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task Main_WithInvalidOutputFormat_ReturnsErrorCode()
    {
        using var writer = new StringWriter();
        Console.SetError(writer);

        var exitCode = await Program.Main([_tempDir, "--output", "xml"]);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task Main_WithEmptyDirectory_ReturnsZero()
    {
        // Empty directory = no dependencies = no problems
        var exitCode = await Program.Main([_tempDir]);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithValidNpmProject_Scans()
    {
        // Create a simple package.json with existing packages
        var packageJson = @"{
            ""dependencies"": {
                ""lodash"": ""^4.17.21""
            }
        }";
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJson);

        using var writer = new StringWriter();
        Console.SetOut(writer);

        var exitCode = await Program.Main([_tempDir]);

        // Exit code depends on whether lodash is found (network-dependent)
        // We just check it runs without crashing
        Assert.True(exitCode == 0 || exitCode == 1);
    }

    [Fact]
    public async Task Main_WithJsonOutput_ReturnsJson()
    {
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");

        using var writer = new StringWriter();
        Console.SetOut(writer);

        await Program.Main([_tempDir, "--output", "json"]);

        var output = writer.ToString();
        Assert.Contains("{", output);
        Assert.Contains("scannedPath", output);
    }

    [Fact]
    public async Task Main_WithConfigOption_UsesConfig()
    {
        // Create config that whitelists everything
        var configPath = Path.Combine(_tempDir, "custom-config.json");
        File.WriteAllText(configPath, @"{
            ""whitelist"": [""test-package""]
        }");

        // Create package.json with the whitelisted package
        var packageJson = @"{
            ""dependencies"": {
                ""test-package"": ""1.0.0""
            }
        }";
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), packageJson);

        var exitCode = await Program.Main([_tempDir, "--config", configPath]);

        // Whitelisted packages should not cause failure
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithVerboseFlag_ProducesMoreOutput()
    {
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");

        using var writer = new StringWriter();
        Console.SetOut(writer);

        await Program.Main([_tempDir, "--verbose"]);

        var output = writer.ToString();
        Assert.Contains("Scanne Verzeichnis", output);
    }
}
