using PlantUmlRender;
using Xunit;

namespace PlantUmlRender.Tests;

public class IsUnderBaseTests
{
    [Fact]
    public void IsUnderBase_SameDir_ReturnsTrue()
    {
        var dir = Path.GetTempPath();
        Assert.True(Program.IsUnderBase(dir, dir));
    }

    [Fact]
    public void IsUnderBase_SubDir_ReturnsTrue()
    {
        var baseDir = Path.GetTempPath();
        var sub = Path.Combine(baseDir, "sub");
        Assert.True(Program.IsUnderBase(sub, baseDir));
    }

    [Fact]
    public void IsUnderBase_FileInSub_ReturnsTrue()
    {
        var baseDir = Path.GetTempPath();
        var file = Path.Combine(baseDir, "sub", "file.txt");
        Assert.True(Program.IsUnderBase(file, baseDir));
    }

    [Fact]
    public void IsUnderBase_SiblingDir_ReturnsFalse()
    {
        var baseDir = Path.GetTempPath();
        var parent = Path.GetDirectoryName(baseDir.TrimEnd(Path.DirectorySeparatorChar)) ?? baseDir;
        var sibling = Directory.GetDirectories(parent).FirstOrDefault(d => !string.Equals(d, baseDir, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(sibling)) return; // skip if no sibling
        Assert.False(Program.IsUnderBase(sibling, baseDir));
    }

    [Fact]
    public void IsUnderBase_ParentDir_ReturnsFalse()
    {
        var baseDir = Path.GetTempPath();
        var sub = Path.Combine(baseDir, "x");
        Assert.False(Program.IsUnderBase(baseDir, sub));
    }
}
