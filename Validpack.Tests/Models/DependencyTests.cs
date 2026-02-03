using Validpack.Models;

namespace Validpack.Tests.Models;

public class DependencyTests
{
    [Fact]
    public void Key_SameNameDifferentCase_SameKey()
    {
        var dep1 = new Dependency("Lodash", "1.0.0", DependencyType.Npm, "file1");
        var dep2 = new Dependency("lodash", "2.0.0", DependencyType.Npm, "file2");

        Assert.Equal(dep1.Key, dep2.Key);
    }

    [Fact]
    public void Key_DifferentType_DifferentKey()
    {
        var dep1 = new Dependency("package", "1.0.0", DependencyType.Npm, "file1");
        var dep2 = new Dependency("package", "1.0.0", DependencyType.NuGet, "file2");

        Assert.NotEqual(dep1.Key, dep2.Key);
    }

    [Fact]
    public void Key_ContainsTypeAndName()
    {
        var dep = new Dependency("MyPackage", "1.0.0", DependencyType.PyPi, "file");

        Assert.Equal("PyPi:mypackage", dep.Key);
    }

    [Fact]
    public void Dependency_Record_Equality()
    {
        var dep1 = new Dependency("package", "1.0.0", DependencyType.Npm, "file");
        var dep2 = new Dependency("package", "1.0.0", DependencyType.Npm, "file");

        Assert.Equal(dep1, dep2);
    }

    [Fact]
    public void Dependency_NullVersion_Allowed()
    {
        var dep = new Dependency("package", null, DependencyType.Npm, "file");

        Assert.Null(dep.Version);
        Assert.Equal("package", dep.Name);
    }
}
