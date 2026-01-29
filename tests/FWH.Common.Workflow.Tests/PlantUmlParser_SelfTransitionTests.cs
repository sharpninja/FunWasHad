using Xunit;

namespace FWH.Common.Workflow.Tests;

public class PlantUmlParserSelfTransitionTests
{
    /// <summary>
    /// Tests that PlantUmlParser does not create unconditional self-transitions (transitions from a node to itself without a condition) when parsing if-else structures.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser.Parse method's handling of if-else conditional structures to ensure it doesn't incorrectly create self-transitions.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with an if-else structure: Start node, conditional "if (cond) then (y)" leading to Then, "else (n)" leading to Else, followed by endif, and explicit transitions from Then and Else to End. This structure should not create any self-transitions.</para>
    /// <para><strong>Why the data matters:</strong> Self-transitions (a node transitioning to itself) are typically only valid for loops with conditions. Unconditional self-transitions would create infinite loops or dead ends in workflows. The parser must correctly handle if-else structures without creating spurious self-transitions. This test validates that conditional parsing doesn't introduce incorrect transitions.</para>
    /// <para><strong>Expected outcome:</strong> The parsed workflow should contain no transitions where FromNodeId equals ToNodeId and the Condition is null or empty (unconditional self-transitions).</para>
    /// <para><strong>Reason for expectation:</strong> The parser should correctly identify the if-else structure and create transitions only between distinct nodes (Start→decision, decision→Then, decision→Else, Then→End, Else→End). It should not create transitions from a node to itself without a condition. The empty selfEdges collection confirms that no invalid self-transitions were created, ensuring the workflow structure is correct and executable.</para>
    /// </remarks>
    [Fact]
    public void ParseIfDoesNotCreateUnconditionalSelfTransitions()
    {
        var input = @"@startuml
:Start;
if (cond) then (y)
:Then;
else (n)
:Else;
endif;
:Then --> :End;
:Else --> :End;
@enduml";

        var parser = new PlantUmlParser(input);
        var def = parser.Parse("t_self", "selftest");

        // Ensure no transitions have From == To with empty condition
        var selfEdges = def.Transitions.Where(t => t.FromNodeId == t.ToNodeId && string.IsNullOrWhiteSpace(t.Condition)).ToList();
        Assert.Empty(selfEdges);
    }

    [Fact]
    public void ParseLoopMayCreateConditionalSelfTransitionOnly()
    {
        var input = @"@startuml
start;
repeat
:work;
repeat while (notdone);
stop;
@enduml";

        var parser = new PlantUmlParser(input);
        var def = parser.Parse("t_loop", "looptest");

        // Only allow self-transitions if they carry a condition; otherwise fail
        var invalid = def.Transitions.Where(t => t.FromNodeId == t.ToNodeId && string.IsNullOrWhiteSpace(t.Condition)).ToList();
        Assert.Empty(invalid);
    }
}
