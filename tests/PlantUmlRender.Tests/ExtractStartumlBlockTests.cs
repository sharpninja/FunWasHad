using PlantUmlRender;
using Xunit;

namespace PlantUmlRender.Tests;

public class ExtractStartumlBlockTests
{
    [Fact]
    public void ExtractStartumlBlock_ValidBlock_ReturnsBlock()
    {
        var text = "pre @startuml\nx->y\n@enduml post";
        var r = Program.ExtractStartumlBlock(text);
        Assert.NotNull(r);
        Assert.StartsWith("@startuml", r, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("@enduml", r, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("x->y", r);
    }

    [Fact]
    public void ExtractStartumlBlock_NoStartuml_ReturnsNull()
    {
        var text = "foo @enduml bar";
        Assert.Null(Program.ExtractStartumlBlock(text));
    }

    [Fact]
    public void ExtractStartumlBlock_NoEnduml_ReturnsNull()
    {
        var text = "@startuml\nfoo";
        Assert.Null(Program.ExtractStartumlBlock(text));
    }

    [Fact]
    public void ExtractStartumlBlock_EndumlBeforeStartuml_ReturnsNull()
    {
        var text = "@enduml\n@startuml\nx\n@enduml";
        Assert.Null(Program.ExtractStartumlBlock(text));
    }

    [Fact]
    public void ExtractStartumlBlock_EmptyBlock_ReturnsBlock()
    {
        var text = "@startuml\n@enduml";
        var r = Program.ExtractStartumlBlock(text);
        Assert.NotNull(r);
        Assert.Contains("@startuml", r, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@enduml", r, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractStartumlBlock_FirstBlock_WhenMultiple()
    {
        var text = "@startuml\nA\n@enduml\n@startuml\nB\n@enduml";
        var r = Program.ExtractStartumlBlock(text);
        Assert.NotNull(r);
        Assert.Contains("A", r);
        Assert.DoesNotContain("B", r);
    }

    [Fact]
    public void ExtractStartumlBlock_CaseInsensitive()
    {
        var text = "@STARTUML\na\n@ENDUML";
        var r = Program.ExtractStartumlBlock(text);
        Assert.NotNull(r);
        Assert.Contains("a", r);
    }
}
