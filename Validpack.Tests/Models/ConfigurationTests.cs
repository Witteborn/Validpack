using Validpack.Models;

namespace Validpack.Tests.Models;

public class ConfigurationTests
{
    [Fact]
    public void IsWhitelisted_ExactMatch_ReturnsTrue()
    {
        var config = new Configuration
        {
            Whitelist = new List<string> { "internal-package", "company-lib" }
        };

        Assert.True(config.IsWhitelisted("internal-package"));
        Assert.True(config.IsWhitelisted("company-lib"));
    }

    [Fact]
    public void IsWhitelisted_CaseInsensitive_ReturnsTrue()
    {
        var config = new Configuration
        {
            Whitelist = new List<string> { "Internal-Package" }
        };

        Assert.True(config.IsWhitelisted("internal-package"));
        Assert.True(config.IsWhitelisted("INTERNAL-PACKAGE"));
        Assert.True(config.IsWhitelisted("Internal-Package"));
    }

    [Fact]
    public void IsWhitelisted_NotInList_ReturnsFalse()
    {
        var config = new Configuration
        {
            Whitelist = new List<string> { "package-a" }
        };

        Assert.False(config.IsWhitelisted("package-b"));
        Assert.False(config.IsWhitelisted(""));
        Assert.False(config.IsWhitelisted("package-a-extended"));
    }

    [Fact]
    public void IsWhitelisted_EmptyList_ReturnsFalse()
    {
        var config = new Configuration();

        Assert.False(config.IsWhitelisted("any-package"));
    }

    [Fact]
    public void IsBlacklisted_ExactMatch_ReturnsTrue()
    {
        var config = new Configuration
        {
            Blacklist = new List<string> { "Newtonsoft.Json", "moment" }
        };

        Assert.True(config.IsBlacklisted("Newtonsoft.Json"));
        Assert.True(config.IsBlacklisted("moment"));
    }

    [Fact]
    public void IsBlacklisted_CaseInsensitive_ReturnsTrue()
    {
        var config = new Configuration
        {
            Blacklist = new List<string> { "Newtonsoft.Json" }
        };

        Assert.True(config.IsBlacklisted("newtonsoft.json"));
        Assert.True(config.IsBlacklisted("NEWTONSOFT.JSON"));
    }

    [Fact]
    public void IsBlacklisted_NotInList_ReturnsFalse()
    {
        var config = new Configuration
        {
            Blacklist = new List<string> { "bad-package" }
        };

        Assert.False(config.IsBlacklisted("good-package"));
    }

    [Fact]
    public void IsBlacklisted_EmptyList_ReturnsFalse()
    {
        var config = new Configuration();

        Assert.False(config.IsBlacklisted("any-package"));
    }

    [Fact]
    public void DefaultConfiguration_HasEmptyLists()
    {
        var config = new Configuration();

        Assert.NotNull(config.Whitelist);
        Assert.NotNull(config.Blacklist);
        Assert.Empty(config.Whitelist);
        Assert.Empty(config.Blacklist);
    }
}
