using Xunit;
using System;
using System.Linq;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Models;

namespace FWH.Common.Workflow.Tests;

/// <summary>
/// Tests for edge cases and error conditions in PlantUmlParser
/// </summary>
public class PlantUmlParserEdgeCaseTests
{
    [Fact]
    public void Parser_EmptyPlantUml_ReturnsEmptyWorkflow()
    {
        // Arrange
        var emptyPuml = "@startuml\n@enduml";
        var parser = new PlantUmlParser(emptyPuml);

        // Act
        var workflow = parser.Parse("empty-test", "Empty Test");

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("empty-test", workflow.Id);
        Assert.Equal("Empty Test", workflow.Name);
        Assert.Empty(workflow.Nodes);
        Assert.Empty(workflow.Transitions);
    }

    [Fact]
    public void Parser_OnlyWhitespace_ReturnsEmptyWorkflow()
    {
        // Arrange
        var whitespacePuml = "@startuml\n\n   \n\t\n@enduml";
        var parser = new PlantUmlParser(whitespacePuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Empty(workflow.Nodes);
    }

    [Fact]
    public void Parser_MismatchedIfEndif_AutoCloses()
    {
        // Arrange - Missing endif
        var malformedPuml = @"
@startuml
if (x) then
  :A;
:B;
@enduml";
        var parser = new PlantUmlParser(malformedPuml);

        // Act
        var workflow = parser.Parse();

        // Assert - Should auto-close and create valid workflow
        Assert.NotNull(workflow);
        Assert.NotEmpty(workflow.Nodes);
        // Should have A, B, and synthetic nodes (decision, join)
        Assert.True(workflow.Nodes.Count >= 2);
    }

    [Fact]
    public void Parser_MismatchedRepeatWhile_AutoCloses()
    {
        // Arrange - Missing repeat while
        var malformedPuml = @"
@startuml
repeat
  :A;
:B;
@enduml";
        var parser = new PlantUmlParser(malformedPuml);

        // Act
        var workflow = parser.Parse();

        // Assert - Should auto-close loop
        Assert.NotNull(workflow);
        Assert.NotEmpty(workflow.Nodes);
    }

    [Fact]
    public void Parser_NestedLoopsThreeLevels_ParsesCorrectly()
    {
        // Arrange
        var nestedPuml = @"
@startuml
repeat
  repeat
    repeat
      :Inner;
    repeat while (inner?)
    :Middle;
  repeat while (middle?)
  :Outer;
repeat while (outer?)
:End;
@enduml";
        var parser = new PlantUmlParser(nestedPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        // Should have Inner, Middle, Outer, End nodes plus loop synthetic nodes
        var innerNode = workflow.Nodes.FirstOrDefault(n => n.Label == "Inner");
        var middleNode = workflow.Nodes.FirstOrDefault(n => n.Label == "Middle");
        var outerNode = workflow.Nodes.FirstOrDefault(n => n.Label == "Outer");
        var endNode = workflow.Nodes.FirstOrDefault(n => n.Label == "End");

        Assert.NotNull(innerNode);
        Assert.NotNull(middleNode);
        Assert.NotNull(outerNode);
        Assert.NotNull(endNode);
    }

    [Fact]
    public void Parser_CircularTransitions_DoesNotInfiniteLoop()
    {
        // Arrange
        var circularPuml = @"
@startuml
A --> B
B --> A
@enduml";
        var parser = new PlantUmlParser(circularPuml);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var workflow = parser.Parse();
        sw.Stop();

        // Assert - Should complete quickly
        Assert.True(sw.Elapsed.TotalSeconds < 1, $"Parsing took {sw.Elapsed.TotalSeconds} seconds");
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.Nodes.Count);
        Assert.Equal(2, workflow.Transitions.Count); // A->B and B->A
    }

    [Fact]
    public void Parser_UnicodeCharactersInLabels_PreservesCorrectly()
    {
        // Arrange
        var unicodePuml = @"
@startuml
:Hello 世界 🌍;
:Café ☕;
:Привет 👋;
@enduml";
        var parser = new PlantUmlParser(unicodePuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("世界"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("🌍"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("Café"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("☕"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("Привет"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("👋"));
    }

    [Fact]
    public void Parser_SpecialRegexCharsInLabels_EscapedProperly()
    {
        // Arrange
        var specialCharsPuml = @"
@startuml
:Test $100;
:Value (with) parentheses;
:Data [in] brackets;
:Match * wildcard;
:Question?;
:Plus+Minus-;
@enduml";
        var parser = new PlantUmlParser(specialCharsPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("$100"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("(with)"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("[in]"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("*"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("?"));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("+"));
    }

    [Fact]
    public void Parser_JsonInNote_ParsesAsPlainText()
    {
        // Arrange
        var jsonNotePuml = @"
@startuml
:Test Action;
note right: {""action"": ""SendMessage"", ""params"": {""text"": ""Hello""}}
@enduml";
        var parser = new PlantUmlParser(jsonNotePuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        var nodeWithNote = workflow.Nodes.FirstOrDefault(n => n.NoteMarkdown != null);
        Assert.NotNull(nodeWithNote);
        Assert.Contains("action", nodeWithNote!.NoteMarkdown);
        Assert.Contains("SendMessage", nodeWithNote.NoteMarkdown);
    }

    [Fact]
    public void Parser_SingleQuoteComments_IgnoredCompletely()
    {
        // Arrange
        var commentedPuml = @"
@startuml
' This is a comment
:Action A;
' Another comment
:Action B;
@enduml";
        var parser = new PlantUmlParser(commentedPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.Nodes.Count);
        Assert.DoesNotContain(workflow.Nodes, n => n.Label.Contains("comment"));
    }

    [Fact]
    public void Parser_DoubleSlashComments_IgnoredCompletely()
    {
        // Arrange
        var commentedPuml = @"
@startuml
// This is a comment
:Action A;
// Another comment
:Action B;
@enduml";
        var parser = new PlantUmlParser(commentedPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.Nodes.Count);
        Assert.DoesNotContain(workflow.Nodes, n => n.Label.Contains("comment"));
    }

    [Fact]
    public void Parser_MultipleStartPoints_CreatesMultipleStarts()
    {
        // Arrange
        var multiStartPuml = @"
@startuml
[*] --> A
[*] --> B
:A;
:B;
@enduml";
        var parser = new PlantUmlParser(multiStartPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.StartPoints.Count);
    }

    [Fact]
    public void Parser_NoStartPoint_WorkflowStillValid()
    {
        // Arrange
        var noStartPuml = @"
@startuml
:Action A;
:Action B;
A --> B
@enduml";
        var parser = new PlantUmlParser(noStartPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.NotEmpty(workflow.Nodes);
        // StartPoints may be empty, which is valid
    }

    [Fact]
    public void Parser_ComplexIfWithMultipleElseIf_ParsesAllBranches()
    {
        // Arrange
        var complexIfPuml = @"
@startuml
if (x == 1) then (yes)
  :Branch 1;
elseif (x == 2) then (yes)
  :Branch 2;
elseif (x == 3) then (yes)
  :Branch 3;
else (no)
  :Default;
endif
@enduml";
        var parser = new PlantUmlParser(complexIfPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Contains(workflow.Nodes, n => n.Label == "Branch 1");
        Assert.Contains(workflow.Nodes, n => n.Label == "Branch 2");
        Assert.Contains(workflow.Nodes, n => n.Label == "Branch 3");
        Assert.Contains(workflow.Nodes, n => n.Label == "Default");
    }

    [Fact]
    public void Parser_NestedIfStatements_ParsesCorrectly()
    {
        // Arrange
        var nestedIfPuml = @"
@startuml
if (outer) then (yes)
  :Outer Yes;
  if (inner) then (yes)
    :Inner Yes;
  else (no)
    :Inner No;
  endif
else (no)
  :Outer No;
endif
@enduml";
        var parser = new PlantUmlParser(nestedIfPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Contains(workflow.Nodes, n => n.Label == "Outer Yes");
        Assert.Contains(workflow.Nodes, n => n.Label == "Inner Yes");
        Assert.Contains(workflow.Nodes, n => n.Label == "Inner No");
        Assert.Contains(workflow.Nodes, n => n.Label == "Outer No");
    }

    [Fact]
    public void Parser_BlockNoteWithMultipleLines_PreservesAllLines()
    {
        // Arrange
        var blockNotePuml = @"
@startuml
:Test Action;
note right
This is line 1
This is line 2
This is line 3
end note
@enduml";
        var parser = new PlantUmlParser(blockNotePuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        var nodeWithNote = workflow.Nodes.FirstOrDefault(n => n.NoteMarkdown != null);
        Assert.NotNull(nodeWithNote);
        Assert.Contains("line 1", nodeWithNote!.NoteMarkdown);
        Assert.Contains("line 2", nodeWithNote.NoteMarkdown);
        Assert.Contains("line 3", nodeWithNote.NoteMarkdown);
    }

    [Fact]
    public void Parser_SkinparamStatements_DoesNotBreakParsing()
    {
        // Arrange
        var skinparamPuml = @"
@startuml
skinparam backgroundColor #EEEEEE
skinparam defaultFontSize 14
:Action A;
:Action B;
@enduml";
        var parser = new PlantUmlParser(skinparamPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.Nodes.Count);
    }

    [Fact]
    public void Parser_StyleBlocks_DoesNotBreakParsing()
    {
        // Arrange
        var stylePuml = @"
@startuml
<style>
activityDiagram {
  BackgroundColor #EEEEEE
}
</style>
:Action A;
:Action B;
@enduml";
        var parser = new PlantUmlParser(stylePuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.Nodes.Count);
    }

    [Fact]
    public void Parser_PragmaStatements_DoesNotBreakParsing()
    {
        // Arrange
        var pragmaPuml = @"
@startuml
!pragma teoz true
:Action A;
:Action B;
@enduml";
        var parser = new PlantUmlParser(pragmaPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.Nodes.Count);
    }

    [Fact]
    public void Parser_VeryLongWorkflow_ParsesEfficiently()
    {
        // Arrange - Create workflow with 1000 nodes
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("[*] --> Node0");
        
        for (int i = 0; i < 999; i++)
        {
            sb.AppendLine($":Node{i};");
            sb.AppendLine($"Node{i} --> Node{i + 1}");
        }
        
        sb.AppendLine(":Node999;");
        sb.AppendLine("@enduml");

        var parser = new PlantUmlParser(sb.ToString());

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var workflow = parser.Parse();
        sw.Stop();

        // Assert
        Assert.NotNull(workflow);
        Assert.True(workflow.Nodes.Count >= 1000, $"Expected >= 1000 nodes, got {workflow.Nodes.Count}");
        Assert.True(sw.Elapsed.TotalSeconds < 5, $"Parsing took {sw.Elapsed.TotalSeconds} seconds (expected < 5s)");
    }

    [Fact]
    public void Parser_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlantUmlParser(null!));
    }

    [Fact]
    public void Parser_ActionWithColorSyntax_ParsesLabel()
    {
        // Arrange
        var colorPuml = @"
@startuml
#palegreen:Success Action;
#pink:Error Action;
@enduml";
        var parser = new PlantUmlParser(colorPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Contains(workflow.Nodes, n => n.Label == "Success Action");
        Assert.Contains(workflow.Nodes, n => n.Label == "Error Action");
    }

    [Fact]
    public void Parser_StereotypeInAction_DoesNotBreakParsing()
    {
        // Arrange
        var stereotypePuml = @"
@startuml
:Action A <<input>>;
:Action B <<output>>;
@enduml";
        var parser = new PlantUmlParser(stereotypePuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.Nodes.Count);
    }

    [Fact]
    public void Parser_MixedArrowStyles_AllParsed()
    {
        // Arrange
        var arrowsPuml = @"
@startuml
A -> B
B --> C
C <- D
D <-- E
@enduml";
        var parser = new PlantUmlParser(arrowsPuml);

        // Act
        var workflow = parser.Parse();

        // Assert
        Assert.NotNull(workflow);
        Assert.True(workflow.Nodes.Count >= 4);
        Assert.True(workflow.Transitions.Count >= 4);
    }
}
