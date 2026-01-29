using Xunit;

namespace PlantUmlRender.Tests;

public class ExtractStartumlBlockTests
{
    [Fact]
    public void ExtractStartumlBlockValidBlockReturnsBlock()
    {
        var text = "pre @startuml\nx->y\n@enduml post";
        var r = Program.ExtractStartumlBlock(text);
        Assert.NotNull(r);
        Assert.True(r.StartsWith("@startuml", StringComparison.OrdinalIgnoreCase));
        Assert.True(r.EndsWith("@enduml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("x->y", r);
    }

    [Fact]
    public void ExtractStartumlBlockNoStartumlReturnsNull()
    {
        var text = "foo @enduml bar";
        Assert.Null(Program.ExtractStartumlBlock(text));
    }

    [Fact]
    public void ExtractStartumlBlockNoEndumlReturnsNull()
    {
        var text = "@startuml\nfoo";
        Assert.Null(Program.ExtractStartumlBlock(text));
    }

    [Fact]
    public void ExtractStartumlBlockEndumlBeforeStartumlReturnsNull()
    {
        var text = "@enduml\n@startuml\nx\n@enduml";
        Assert.Null(Program.ExtractStartumlBlock(text));
    }

    [Fact]
    public void ExtractStartumlBlockEmptyBlockReturnsBlock()
    {
        var text = "@startuml\n@enduml";
        var r = Program.ExtractStartumlBlock(text);
        Assert.NotNull(r);
        Assert.Contains("@startuml", r, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@enduml", r, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractStartumlBlockFirstBlockWhenMultiple()
    {
        var text = "@startuml\nA\n@enduml\n@startuml\nB\n@enduml";
        var r = Program.ExtractStartumlBlock(text);
        Assert.NotNull(r);
        Assert.Contains("A", r);
        Assert.DoesNotContain("B", r);
    }

    [Fact]
    public void ExtractStartumlBlockCaseInsensitive()
    {
        var text = "@STARTUML\na\n@ENDUML";
        var r = Program.ExtractStartumlBlock(text);
        Assert.NotNull(r);
        Assert.Contains("a", r);
    }
}
