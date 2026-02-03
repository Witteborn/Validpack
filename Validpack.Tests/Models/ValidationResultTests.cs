using Validpack.Models;

namespace Validpack.Tests.Models;

public class ValidationResultTests
{
    [Theory]
    [InlineData(ValidationStatus.NotFound, true)]
    [InlineData(ValidationStatus.Blacklisted, true)]
    [InlineData(ValidationStatus.Valid, false)]
    [InlineData(ValidationStatus.Whitelisted, false)]
    [InlineData(ValidationStatus.Error, false)]
    public void HasProblem_ReturnsCorrectValue(ValidationStatus status, bool expectedHasProblem)
    {
        var dep = new Dependency("test", "1.0", DependencyType.Npm, "file");
        var result = new ValidationResult(dep, status);

        Assert.Equal(expectedHasProblem, result.HasProblem);
    }

    [Fact]
    public void ValidationResult_StoresDependency()
    {
        var dep = new Dependency("my-package", "2.0.0", DependencyType.PyPi, "requirements.txt");
        var result = new ValidationResult(dep, ValidationStatus.Valid, "Package exists");

        Assert.Equal(dep, result.Dependency);
        Assert.Equal(ValidationStatus.Valid, result.Status);
        Assert.Equal("Package exists", result.Message);
    }

    [Fact]
    public void ValidationResult_MessageCanBeNull()
    {
        var dep = new Dependency("test", "1.0", DependencyType.Npm, "file");
        var result = new ValidationResult(dep, ValidationStatus.Valid);

        Assert.Null(result.Message);
    }
}
