using Xunit;

namespace PlantUmlRender.Tests;

public class IsUnderBaseTests
{
    [Fact]
    public void IsUnderBaseSameDirReturnsTrue()
    {
        var dir = Path.GetTempPath();
        Assert.True(Program.IsUnderBase(dir, dir));
    }

    [Fact]
    public void IsUnderBaseSubDirReturnsTrue()
    {
        var baseDir = Path.GetTempPath();
        var sub = Path.Combine(baseDir, "sub");
        Assert.True(Program.IsUnderBase(sub, baseDir));
    }

    [Fact]
    public void IsUnderBaseFileInSubReturnsTrue()
    {
        var baseDir = Path.GetTempPath();
        var file = Path.Combine(baseDir, "sub", "file.txt");
        Assert.True(Program.IsUnderBase(file, baseDir));
    }

    [Fact]
    public void IsUnderBaseSiblingDirReturnsFalse()
    {
        var baseDir = Path.GetTempPath();
        var parent = Path.GetDirectoryName(baseDir.TrimEnd(Path.DirectorySeparatorChar)) ?? baseDir;
        var sibling = Directory.GetDirectories(parent).FirstOrDefault(d => !string.Equals(d, baseDir, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(sibling)) return; // skip if no sibling
        Assert.False(Program.IsUnderBase(sibling, baseDir));
    }

    [Fact]
    public void IsUnderBaseParentDirReturnsFalse()
    {
        var baseDir = Path.GetTempPath();
        var sub = Path.Combine(baseDir, "x");
        Assert.False(Program.IsUnderBase(baseDir, sub));
    }
}
