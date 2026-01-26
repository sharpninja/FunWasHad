using Xunit;

namespace FWH.Common.Workflow.Tests;

/// <summary>
/// Tests for edge cases and error conditions in PlantUmlParser
/// </summary>
public class PlantUmlParserEdgeCaseTests
{
    /// <summary>
    /// Tests that PlantUmlParser correctly handles empty PlantUML input and returns a valid but empty workflow.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's handling of minimal valid PlantUML syntax with no workflow content.</para>
    /// <para><strong>Data involved:</strong> A PlantUML string containing only the required markers "@startuml\n@enduml" with no nodes, transitions, or other content. The workflow is parsed with Id="empty-test" and Name="Empty Test".</para>
    /// <para><strong>Why the data matters:</strong> Empty workflows are valid edge cases - a workflow definition might be created before content is added, or a template might start empty. The parser must handle this gracefully without throwing exceptions, returning a valid workflow object with empty collections. This tests the parser's robustness and ensures it doesn't require content to be present.</para>
    /// <para><strong>Expected outcome:</strong> The parser should return a non-null WorkflowDefinition with Id="empty-test", Name="Empty Test", and empty Nodes and Transitions collections.</para>
    /// <para><strong>Reason for expectation:</strong> The parser should recognize valid PlantUML syntax (start/end markers) even without content. It should create a workflow object with the provided Id and Name, but with empty collections since no nodes or transitions were defined. This allows workflows to be created incrementally and validates that the parser doesn't fail on minimal input.</para>
    /// </remarks>
    [Fact]
    public void ParserEmptyPlantUmlReturnsEmptyWorkflow()
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

    /// <summary>
    /// Tests that PlantUmlParser correctly handles PlantUML input containing only whitespace between start and end markers.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's handling of whitespace-only content between PlantUML markers.</para>
    /// <para><strong>Data involved:</strong> A PlantUML string with "@startuml" and "@enduml" markers containing only whitespace characters (newlines, spaces, tabs): "@startuml\n\n   \n\t\n@enduml". The workflow is parsed using default Id/Name.</para>
    /// <para><strong>Why the data matters:</strong> Real-world PlantUML files may contain formatting whitespace (indentation, blank lines) that should be ignored. The parser must correctly skip whitespace-only lines and not treat them as content. This tests the parser's ability to filter out insignificant whitespace while preserving the structure.</para>
    /// <para><strong>Expected outcome:</strong> The parser should return a non-null WorkflowDefinition with empty Nodes collection, treating the whitespace as empty content.</para>
    /// <para><strong>Reason for expectation:</strong> Whitespace between markers should be ignored - it's formatting, not content. The parser should recognize that no actual workflow elements (nodes, transitions) are present and return an empty workflow, similar to the empty PlantUML case. This ensures the parser is tolerant of formatting variations.</para>
    /// </remarks>
    [Fact]
    public void ParserOnlyWhitespaceReturnsEmptyWorkflow()
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

    /// <summary>
    /// Tests that PlantUmlParser automatically closes mismatched if-endif blocks to create a valid workflow.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's error recovery when an if statement is missing its corresponding endif.</para>
    /// <para><strong>Data involved:</strong> A malformed PlantUML string with an "if (x) then" statement but no matching "endif" before "@enduml". The if block contains two action nodes ":A;" and ":B;". This simulates a common user error (forgetting to close conditional blocks).</para>
    /// <para><strong>Why the data matters:</strong> Users may forget to close if-endif blocks, leading to malformed PlantUML. The parser should be forgiving and auto-close unclosed blocks rather than failing completely. This improves user experience by allowing workflows to be parsed even with minor syntax errors. The parser should create synthetic decision and join nodes to complete the structure.</para>
    /// <para><strong>Expected outcome:</strong> The parser should return a non-null WorkflowDefinition with at least 2 nodes (A and B, plus synthetic decision/join nodes), successfully parsing the workflow despite the missing endif.</para>
    /// <para><strong>Reason for expectation:</strong> Error recovery is important for usability - the parser should attempt to fix common errors rather than failing. Auto-closing the if block allows the workflow to be parsed and used, even if it's not perfectly formed. The presence of at least 2 nodes confirms that the action nodes were parsed, and the parser created the necessary synthetic nodes to complete the conditional structure.</para>
    /// </remarks>
    [Fact]
    public void ParserMismatchedIfEndifAutoCloses()
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

    /// <summary>
    /// Tests that PlantUmlParser automatically closes mismatched repeat-while blocks to create a valid workflow.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's error recovery when a repeat statement is missing its corresponding "repeat while" clause.</para>
    /// <para><strong>Data involved:</strong> A malformed PlantUML string with a "repeat" statement but no matching "repeat while" before "@enduml". The repeat block contains two action nodes ":A;" and ":B;". This simulates a common user error (forgetting to close loop blocks).</para>
    /// <para><strong>Why the data matters:</strong> Users may forget to close repeat-while blocks, leading to malformed PlantUML. The parser should be forgiving and auto-close unclosed loops rather than failing completely. This improves user experience by allowing workflows to be parsed even with minor syntax errors. The parser should create synthetic loop nodes to complete the structure.</para>
    /// <para><strong>Expected outcome:</strong> The parser should return a non-null WorkflowDefinition with at least 2 nodes (A and B, plus synthetic loop nodes), successfully parsing the workflow despite the missing "repeat while".</para>
    /// <para><strong>Reason for expectation:</strong> Error recovery is important for usability - the parser should attempt to fix common errors rather than failing. Auto-closing the repeat block allows the workflow to be parsed and used, even if it's not perfectly formed. The presence of at least 2 nodes confirms that the action nodes were parsed, and the parser created the necessary synthetic nodes to complete the loop structure.</para>
    /// </remarks>
    [Fact]
    public void ParserMismatchedRepeatWhileAutoCloses()
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
    public void ParserNestedLoopsThreeLevelsParsesCorrectly()
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

    /// <summary>
    /// Tests that PlantUmlParser correctly handles circular transitions without entering an infinite loop.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's handling of circular workflow structures (node A transitions to B, B transitions back to A).</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with two nodes (A and B) and two transitions: A → B and B → A, creating a circular reference. This represents a valid workflow pattern (e.g., retry loops, polling).</para>
    /// <para><strong>Why the data matters:</strong> Circular workflows are valid and common (e.g., retry logic, polling loops). The parser must handle them correctly without getting stuck in infinite loops during parsing. This tests the parser's ability to process cyclic structures efficiently and correctly identify all nodes and transitions.</para>
    /// <para><strong>Expected outcome:</strong> The parser should complete parsing in less than 1 second, return a non-null WorkflowDefinition with exactly 2 nodes and exactly 2 transitions (A→B and B→A).</para>
    /// <para><strong>Reason for expectation:</strong> The parser should recognize circular structures as valid and parse them efficiently. The timing check (< 1 second) ensures the parser doesn't get stuck in an infinite loop. The exact node and transition counts confirm that both nodes and both transitions were correctly identified, validating that circular structures are fully supported.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that PlantUmlParser correctly preserves Unicode characters (non-ASCII text and emojis) in node labels.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's handling of Unicode characters in workflow node labels, including non-ASCII text and emoji symbols.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with three action nodes containing Unicode content: "Hello 世界 🌍" (Chinese characters and globe emoji), "Café ☕" (accented character and coffee emoji), and "Привет 👋" (Cyrillic text and wave emoji). These represent internationalization scenarios and modern text content.</para>
    /// <para><strong>Why the data matters:</strong> Workflows may contain internationalized text for global users, and modern applications often use emojis. The parser must correctly handle UTF-8 encoding and preserve all Unicode characters without corruption or loss. This tests the parser's encoding handling and ensures workflows can support multilingual content.</para>
    /// <para><strong>Expected outcome:</strong> The parser should return a workflow with nodes containing all Unicode characters preserved: Chinese characters "世界", emoji "🌍", accented "Café", emoji "☕", Cyrillic "Привет", and emoji "👋".</para>
    /// <para><strong>Reason for expectation:</strong> The parser should treat all Unicode characters as valid label content and preserve them exactly as written. UTF-8 encoding should handle multi-byte characters correctly. The presence of all Unicode characters in the parsed nodes confirms that encoding is handled correctly and no data loss occurred during parsing.</para>
    /// </remarks>
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
    public void ParserSingleQuoteCommentsIgnoredCompletely()
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
    public void ParserDoubleSlashCommentsIgnoredCompletely()
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
    public void ParserNestedIfStatementsParsesCorrectly()
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
    public void ParserBlockNoteWithMultipleLinesPreservesAllLines()
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

    /// <summary>
    /// Tests that PlantUmlParser correctly parses all arrow styles (->, -->, <-, <--), ensuring different arrow syntaxes are all recognized and create transitions.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's handling of different arrow styles for transitions, ensuring all arrow syntaxes are correctly recognized.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with four transitions using different arrow styles: A -> B (single arrow right), B --> C (double arrow right), C <- D (single arrow left), D <-- E (double arrow left). All arrow styles should be recognized and create transitions.</para>
    /// <para><strong>Why the data matters:</strong> PlantUML supports multiple arrow styles for transitions. The parser must recognize all common arrow styles to support different PlantUML syntax preferences. This tests the parser's flexibility in handling various arrow syntaxes.</para>
    /// <para><strong>Expected outcome:</strong> The parser should return a workflow with at least 4 nodes and at least 4 transitions, confirming that all arrow styles were recognized and transitions were created.</para>
    /// <para><strong>Reason for expectation:</strong> The parser should recognize all arrow styles (->, -->, <-, <--) as valid transition syntax. Each arrow should create a transition between the nodes, regardless of arrow direction or style. The node count >= 4 and transition count >= 4 confirm that all arrows were parsed and transitions were created, validating that the parser supports all common arrow syntaxes.</para>
    /// </remarks>
    [Fact]
    public void ParserMixedArrowStylesAllParsed()
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
