using Xunit;

namespace FWH.Common.Workflow.Tests;

public class PlantUmlParserNotesTests
{
    /// <summary>
    /// Tests that PlantUmlParser correctly parses inline notes (single-line notes) and attaches them to the corresponding node.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's ability to extract inline notes from PlantUML syntax and attach them to workflow nodes.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with a "Start" node and an inline note "note right of Start : This is *markdown* content". The note uses the inline syntax (colon after node reference) and contains markdown formatting (asterisks for emphasis).</para>
    /// <para><strong>Why the data matters:</strong> Notes in PlantUML workflows provide additional context, documentation, or action metadata for nodes. Inline notes are a common PlantUML syntax for attaching brief annotations. The parser must correctly identify the note, associate it with the correct node, and preserve the note content (including markdown) in the NoteMarkdown property. This enables workflows to display helpful information to users.</para>
    /// <para><strong>Expected outcome:</strong> The parsed workflow should contain a node with Label="Start" and NoteMarkdown containing "markdown" (the note content).</para>
    /// <para><strong>Reason for expectation:</strong> The parser should recognize the "note right of Start" syntax, extract the note text after the colon, and assign it to the Start node's NoteMarkdown property. The presence of "markdown" in the NoteMarkdown confirms the note was correctly parsed and attached. This validates that inline note syntax is supported and note content is preserved.</para>
    /// </remarks>
    [Fact]
    public void ParseInlineNoteAttachesNoteToNode()
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
    public void ParseBlockNoteAttachesNoteToNode()
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
