using Validpack.Models;

namespace Validpack.Tests.Cli;

public class CliOptionsTests
{
    [Fact]
    public void IsValid_WithPathAndNoErrors_ReturnsTrue()
    {
        var options = new CliOptions { Path = "/some/path" };

        Assert.True(options.IsValid);
    }

    [Fact]
    public void IsValid_WithoutPath_ReturnsFalse()
    {
        var options = new CliOptions();

        Assert.False(options.IsValid);
    }

    [Fact]
    public void IsValid_WithErrors_ReturnsFalse()
    {
        var options = new CliOptions { Path = "/some/path" };
        options.Errors.Add("Some error");

        Assert.False(options.IsValid);
    }

    [Fact]
    public void IsValid_WithShowHelp_ReturnsFalse()
    {
        var options = new CliOptions
        {
            Path = "/some/path",
            ShowHelp = true
        };

        Assert.False(options.IsValid);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new CliOptions();

        Assert.Null(options.Path);
        Assert.Equal("validpack.json", options.ConfigFile);
        Assert.Equal("console", options.OutputFormat);
        Assert.False(options.Verbose);
        Assert.False(options.ShowHelp);
        Assert.Empty(options.Errors);
    }

    [Fact]
    public void Errors_CanBeAdded()
    {
        var options = new CliOptions();

        options.Errors.Add("Error 1");
        options.Errors.Add("Error 2");

        Assert.Equal(2, options.Errors.Count);
    }
}
