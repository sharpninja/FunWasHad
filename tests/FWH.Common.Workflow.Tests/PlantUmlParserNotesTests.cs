using System.Linq;
using FWH.Common.Workflow;
using Xunit;

namespace FWH.Common.Workflow.Tests;

public class PlantUmlParserNotesTests
{
    [Fact]
    public void Parse_InlineNote_AttachesNoteToNode()
    {
        var input = @"@startuml
[*] --> Start
note right of Start : This is *markdown* content
@enduml";

        var parser = new PlantUmlParser(input);
        var def = parser.Parse("id_notes", "notes");

        Assert.Contains(def.Nodes, n => n.Label == "Start" && n.NoteMarkdown != null && n.NoteMarkdown.Contains("markdown"));
    }

    [Fact]
    public void Parse_BlockNote_AttachesNoteToNode()
    {
        var input = @"@startuml
:Start;
note left of Start
This is line one
This is line two
end note
@enduml";

        var parser = new PlantUmlParser(input);
        var def = parser.Parse("id_notes_block", "notes_block");

        var node = def.Nodes.FirstOrDefault(n => n.Label == "Start");
        Assert.NotNull(node);
        Assert.False(string.IsNullOrWhiteSpace(node!.NoteMarkdown));
        Assert.Contains("line one", node.NoteMarkdown);
        Assert.Contains("line two", node.NoteMarkdown);
    }
}
