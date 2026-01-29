using Xunit;

namespace FWH.Common.Workflow.Tests;

public class PlantUmlParserTests
{
    /// <summary>
    /// Tests that PlantUmlParser correctly parses a simple sequential workflow and produces nodes and transitions.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's ability to parse a basic PlantUML workflow definition and generate a workflow model with nodes and transitions.</para>
    /// <para><strong>Data involved:</strong> A simple PlantUML workflow definition containing: start state [*] transitioning to "Start", two action nodes ("First Action" and "Second Action"), and an end state [*] transitioning from the last action. The workflow ID is "id1" and name is "seq".</para>
    /// <para><strong>Why the data matters:</strong> Simple sequential workflows are the foundation of more complex workflows. This test validates that the parser can correctly identify nodes (start, actions, end) and transitions between them. The [*] notation represents start/end states in PlantUML, which must be correctly parsed and converted to workflow model elements.</para>
    /// <para><strong>Expected outcome:</strong> The parsed workflow definition should not be null, should contain at least one node, at least one transition, and at least one start point with a non-null NodeId.</para>
    /// <para><strong>Reason for expectation:</strong> The parser should extract all workflow elements from the PlantUML syntax. Nodes represent states/actions in the workflow, transitions represent the flow between nodes, and start points indicate where the workflow begins. A valid workflow must have at least one of each to be executable. The start point having a non-null NodeId confirms the parser correctly identified the starting node.</para>
    /// </remarks>
    [Fact]
    public void ParseSimpleSequenceShouldProduceNodesAndTransitions()
    {
        var input = @"@startuml
[*] --> Start
:First Action;
:Second Action;
[*] --> End
@enduml";

        var parser = new PlantUmlParser(input);
        var def = parser.Parse("id1", "seq");

        Assert.NotNull(def);
        Assert.NotEmpty(def.Nodes);
        Assert.NotEmpty(def.Transitions);
        Assert.Contains(def.StartPoints, s => s.NodeId != null);
    }

    /// <summary>
    /// Tests that PlantUmlParser correctly parses if-else conditional logic and creates decision and join transitions.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's ability to parse conditional branching (if-else) statements and generate decision nodes with conditional transitions.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with an if-else structure: condition "IsAdmin" with "yes" and "no" branches, each leading to different action nodes ("Load Admin Dashboard" vs "Load Standard Dashboard"), followed by a common action "Common Tasks" after the endif. The workflow ID is "id2" and name is "iftest".</para>
    /// <para><strong>Why the data matters:</strong> Conditional branching is essential for workflow logic - different paths based on conditions. The parser must correctly identify the if-else structure, create decision nodes, generate conditional transitions (with conditions like "IsAdmin"), and identify the join point where branches converge. This tests the parser's ability to handle complex workflow structures beyond simple sequences.</para>
    /// <para><strong>Expected outcome:</strong> The parsed workflow should contain at least 3 transitions (one for the if condition, one for the else condition, and one for the join), and at least one transition should have a non-null Condition property.</para>
    /// <para><strong>Reason for expectation:</strong> An if-else structure requires: a transition into the if block with a condition, a transition for the else branch (possibly with a negated condition), and transitions out of each branch that join at the endif. The Condition property being non-null on at least one transition confirms the parser correctly extracted the conditional logic from the PlantUML syntax.</para>
    /// </remarks>
    [Fact]
    public void ParseIfElseShouldCreateDecisionAndJoinTransitions()
    {
        var input = @"@startuml
[*]
if (IsAdmin) then (yes)
:Load Admin Dashboard;
else (no)
:Load Standard Dashboard;
endif;
:Common Tasks;
@enduml";
        var parser = new PlantUmlParser(input);
        var def = parser.Parse("id2", "iftest");

        Assert.NotNull(def);
        var transitions = def.Transitions.ToList();
        // expect at least one decision transition and one join
        Assert.True(transitions.Count >= 3);
        Assert.Contains(transitions, t => t.Condition != null);
    }

    /// <summary>
    /// Tests that PlantUmlParser correctly parses repeat-while loop constructs and creates a loop-back transition with a condition.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's ability to parse loop constructs (repeat-while) and generate transitions that loop back to previous nodes.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with a repeat-while loop: a "start" node, a "repeat" block containing an action "do work", a "repeat while (not done)" condition, and a "stop" node. The workflow ID is "id3" and name is "looptest".</para>
    /// <para><strong>Why the data matters:</strong> Loops are essential for iterative workflows (e.g., retry logic, polling, processing lists). The parser must correctly identify the repeat-while structure, create a transition that loops back to the repeat block when the condition is true, and create a transition that exits the loop when the condition is false. This tests the parser's ability to handle cyclic workflow structures.</para>
    /// <para><strong>Expected outcome:</strong> The parsed workflow should contain at least one transition with a non-null Condition property, representing the loop condition.</para>
    /// <para><strong>Reason for expectation:</strong> A repeat-while loop requires a conditional transition that determines whether to loop back or exit. The Condition property being non-null confirms the parser correctly extracted the loop condition ("not done") from the PlantUML syntax and created a conditional transition for the loop control flow.</para>
    /// </remarks>
    [Fact]
    public void ParseRepeatWhileShouldCreateLoopBackEdge()
    {
        var input = @"@startuml
start;
repeat
:do work;
repeat while (not done);
stop;
@enduml";

        var parser = new PlantUmlParser(input);
        var def = parser.Parse("id3", "looptest");

        Assert.NotNull(def);
        Assert.Contains(def.Transitions, t => t.Condition != null);
    }
}
