using Xunit;

namespace PlantUmlRender.Tests;

public class ParseArgsTests
{
    [Fact]
    public void ParseArgsEmptyReturnsDefaultsAndEmptyFiles()
    {
        var (outputDir, wantSvg, wantPng, files, unknown) = Program.ParseArgs(Array.Empty<string>());
        Assert.Equal(".", outputDir);
        Assert.True(wantSvg);
        Assert.True(wantPng);
        Assert.Empty(files);
        Assert.Null(unknown);
    }

    [Fact]
    public void ParseArgsSingleFileAddsToFiles()
    {
        var (_, _, _, files, _) = Program.ParseArgs(new[] { "a.puml" });
        Assert.Single(files);
        Assert.Equal("a.puml", files[0]);
    }

    [Fact]
    public void ParseArgsMultipleFilesAddsAll()
    {
        var (_, _, _, files, _) = Program.ParseArgs(new[] { "a.puml", "b.puml" });
        Assert.Equal(2, files.Count);
        Assert.Equal("a.puml", files[0]);
        Assert.Equal("b.puml", files[1]);
    }

    [Fact]
    public void ParseArgsOutputShortSetsOutputDir()
    {
        var (outputDir, _, _, _, _) = Program.ParseArgs(new[] { "-o", "out", "x.puml" });
        Assert.Equal("out", outputDir);
    }

    [Fact]
    public void ParseArgsOutputLongSetsOutputDir()
    {
        var (outputDir, _, _, _, _) = Program.ParseArgs(new[] { "--output", "out", "x.puml" });
        Assert.Equal("out", outputDir);
    }

    [Fact]
    public void ParseArgsFormatSvgOnlySvg()
    {
        var (_, wantSvg, wantPng, _, _) = Program.ParseArgs(new[] { "-f", "svg", "x.puml" });
        Assert.True(wantSvg);
        Assert.False(wantPng);
    }

    [Fact]
    public void ParseArgsFormatPngOnlyPng()
    {
        var (_, wantSvg, wantPng, _, _) = Program.ParseArgs(new[] { "-f", "png", "x.puml" });
        Assert.False(wantSvg);
        Assert.True(wantPng);
    }

    [Fact]
    public void ParseArgsFormatBothBoth()
    {
        var (_, wantSvg, wantPng, _, _) = Program.ParseArgs(new[] { "-f", "both", "x.puml" });
        Assert.True(wantSvg);
        Assert.True(wantPng);
    }

    [Fact]
    public void ParseArgsFormatUnknownSetsBothAndUnknownValue()
    {
        var (_, wantSvg, wantPng, _, unknown) = Program.ParseArgs(new[] { "-f", "foo", "x.puml" });
        Assert.True(wantSvg);
        Assert.True(wantPng);
        Assert.Equal("foo", unknown);
    }

    [Fact]
    public void ParseArgsMixedCombinesCorrectly()
    {
        var (outputDir, wantSvg, wantPng, files, _) = Program.ParseArgs(new[] { "-o", "dir", "-f", "png", "a.puml", "b.puml" });
        Assert.Equal("dir", outputDir);
        Assert.False(wantSvg);
        Assert.True(wantPng);
        Assert.Equal(2, files.Count);
        Assert.Equal("a.puml", files[0]);
        Assert.Equal("b.puml", files[1]);
    }

    [Fact]
    public void ParseArgsOptionsAfterFilesStillAddsFiles()
    {
        var (_, _, _, files, _) = Program.ParseArgs(new[] { "a.puml", "-o", "x" });
        Assert.Single(files);
        Assert.Equal("a.puml", files[0]);
        // -o x would be consumed as -o with value x; "x" is not a positional
    }

    [Fact]
    public void ParseArgsIgnoresOptionsKeepsNonOptions()
    {
        var (_, _, _, files, _) = Program.ParseArgs(new[] { "a.puml", "--other", "b.puml" });
        Assert.Equal(2, files.Count);
        Assert.Equal("a.puml", files[0]);
        Assert.Equal("b.puml", files[1]);
    }
}
