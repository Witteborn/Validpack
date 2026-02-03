using Validpack.Services;

namespace Validpack.Tests.Services;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ConfigService _service;

    public ConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ConfigServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _service = new ConfigService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void LoadConfiguration_ValidFile_ReturnsConfiguration()
    {
        var configPath = Path.Combine(_tempDir, "validpack.json");
        File.WriteAllText(configPath, @"{
            ""whitelist"": [""internal-pkg"", ""company-lib""],
            ""blacklist"": [""bad-pkg""]
        }");

        var config = _service.LoadConfiguration(configPath);

        Assert.Equal(2, config.Whitelist.Count);
        Assert.Contains("internal-pkg", config.Whitelist);
        Assert.Contains("company-lib", config.Whitelist);
        Assert.Single(config.Blacklist);
        Assert.Contains("bad-pkg", config.Blacklist);
    }

    [Fact]
    public void LoadConfiguration_NullPath_ReturnsDefaultConfiguration()
    {
        var config = _service.LoadConfiguration(null);

        Assert.NotNull(config);
        Assert.Empty(config.Whitelist);
        Assert.Empty(config.Blacklist);
    }

    [Fact]
    public void LoadConfiguration_EmptyPath_ReturnsDefaultConfiguration()
    {
        var config = _service.LoadConfiguration("");

        Assert.NotNull(config);
        Assert.Empty(config.Whitelist);
        Assert.Empty(config.Blacklist);
    }

    [Fact]
    public void LoadConfiguration_NonExistentFile_ReturnsDefaultConfiguration()
    {
        var config = _service.LoadConfiguration("/nonexistent/path/config.json");

        Assert.NotNull(config);
        Assert.Empty(config.Whitelist);
        Assert.Empty(config.Blacklist);
    }

    [Fact]
    public void LoadConfiguration_InvalidJson_ReturnsDefaultConfiguration()
    {
        var configPath = Path.Combine(_tempDir, "invalid.json");
        File.WriteAllText(configPath, "this is not valid json {{{");

        var config = _service.LoadConfiguration(configPath);

        Assert.NotNull(config);
        Assert.Empty(config.Whitelist);
        Assert.Empty(config.Blacklist);
    }

    [Fact]
    public void LoadConfiguration_EmptyJson_ReturnsDefaultConfiguration()
    {
        var configPath = Path.Combine(_tempDir, "empty.json");
        File.WriteAllText(configPath, "{}");

        var config = _service.LoadConfiguration(configPath);

        Assert.NotNull(config);
        Assert.Empty(config.Whitelist);
        Assert.Empty(config.Blacklist);
    }

    [Fact]
    public void LoadConfiguration_CaseInsensitiveProperties_Works()
    {
        var configPath = Path.Combine(_tempDir, "case.json");
        File.WriteAllText(configPath, @"{
            ""Whitelist"": [""pkg1""],
            ""BLACKLIST"": [""pkg2""]
        }");

        var config = _service.LoadConfiguration(configPath);

        Assert.Single(config.Whitelist);
        Assert.Single(config.Blacklist);
    }

    [Fact]
    public void CreateExampleConfig_CreatesValidFile()
    {
        var configPath = Path.Combine(_tempDir, "example.json");

        _service.CreateExampleConfig(configPath);

        Assert.True(File.Exists(configPath));

        var config = _service.LoadConfiguration(configPath);
        Assert.NotEmpty(config.Whitelist);
        Assert.NotEmpty(config.Blacklist);
    }

    [Fact]
    public void CreateExampleConfig_FileContainsExpectedContent()
    {
        var configPath = Path.Combine(_tempDir, "example.json");

        _service.CreateExampleConfig(configPath);

        var content = File.ReadAllText(configPath);
        Assert.Contains("internal-company-package", content);
        Assert.Contains("Newtonsoft.Json", content);
    }
}
