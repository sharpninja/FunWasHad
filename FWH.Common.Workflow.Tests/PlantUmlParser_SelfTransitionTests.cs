using System.Linq;
using Xunit;
using FWH.Common.Workflow;

namespace FWH.Common.Workflow.Tests;

public class PlantUmlParser_SelfTransitionTests
{
    [Fact]
    public void Parse_IfDoesNotCreateUnconditionalSelfTransitions()
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
    public void Parse_LoopMayCreateConditionalSelfTransitionOnly()
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
