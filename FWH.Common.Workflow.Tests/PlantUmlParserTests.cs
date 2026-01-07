using System.Linq;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Models;
using Xunit;

namespace FWH.Common.Workflow.Tests;

public class PlantUmlParserTests
{
    [Fact]
    public void Parse_SimpleSequence_ShouldProduceNodesAndTransitions()
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

    [Fact]
    public void Parse_IfElse_ShouldCreateDecisionAndJoinTransitions()
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

    [Fact]
    public void Parse_RepeatWhile_ShouldCreateLoopBackEdge()
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
