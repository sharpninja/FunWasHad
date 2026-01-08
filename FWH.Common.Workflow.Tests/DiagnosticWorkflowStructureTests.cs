using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using FWH.Common.Workflow;

namespace FWH.Common.Workflow.Tests;

/// <summary>
/// Diagnostic tests to understand workflow structure from actual workflow.puml
/// </summary>
public class DiagnosticWorkflowStructureTests
{
    private readonly ITestOutputHelper _output;

    public DiagnosticWorkflowStructureTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_ActualWorkflow_Structure()
    {
        // Arrange - Load actual workflow.puml
        var currentDir = Directory.GetCurrentDirectory();
        var solutionDir = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.FullName 
            ?? throw new FileNotFoundException("Could not locate solution directory");
        var pumlPath = Path.Combine(solutionDir, "workflow.puml");
        
        if (!File.Exists(pumlPath))
            throw new FileNotFoundException($"workflow.puml not found at {pumlPath}");
        
        var pumlContent = File.ReadAllText(pumlPath);
        _output.WriteLine("=== WORKFLOW.PUML CONTENT ===");
        _output.WriteLine(pumlContent);
        _output.WriteLine("");

        // Act - Parse the workflow
        var parser = new PlantUmlParser(pumlContent);
        var def = parser.Parse("debug-workflow", "Debug");

        // Assert & Log - Output complete structure
        _output.WriteLine("=== PARSED WORKFLOW STRUCTURE ===");
        _output.WriteLine($"Workflow ID: {def.Id}");
        _output.WriteLine($"Workflow Name: {def.Name}");
        _output.WriteLine("");

        _output.WriteLine("--- START POINTS ---");
        foreach (var start in def.StartPoints)
        {
            _output.WriteLine($"  StartPoint: NodeId={start.NodeId}");
        }
        _output.WriteLine("");

        _output.WriteLine("--- NODES ---");
        foreach (var node in def.Nodes)
        {
            _output.WriteLine($"  Node: Id='{node.Id}', Label='{node.Label}'");
            if (!string.IsNullOrWhiteSpace(node.NoteMarkdown))
            {
                _output.WriteLine($"        NoteMarkdown='{node.NoteMarkdown.Substring(0, Math.Min(50, node.NoteMarkdown.Length))}...'");
            }
            if (!string.IsNullOrWhiteSpace(node.JsonMetadata))
            {
                _output.WriteLine($"        JsonMetadata='{node.JsonMetadata.Substring(0, Math.Min(50, node.JsonMetadata.Length))}...'");
            }
        }
        _output.WriteLine("");

        _output.WriteLine("--- TRANSITIONS ---");
        foreach (var transition in def.Transitions)
        {
            var conditionStr = string.IsNullOrWhiteSpace(transition.Condition) ? "" : $" [Condition: {transition.Condition}]";
            _output.WriteLine($"  Transition: From='{transition.FromNodeId}' -> To='{transition.ToNodeId}'{conditionStr}");
        }
        _output.WriteLine("");

        // Test CalculateStartNode logic
        _output.WriteLine("--- START NODE CALCULATION TEST ---");
        var startPointNode = def.StartPoints.Count > 0 
            ? def.Nodes.FirstOrDefault(n => n.Id == def.StartPoints[0].NodeId)
            : def.Nodes.FirstOrDefault();
        
        if (startPointNode != null)
        {
            _output.WriteLine($"Start Point Node: Id='{startPointNode.Id}', Label='{startPointNode.Label}'");
            
            var outgoing = def.Transitions.Where(t => t.FromNodeId == startPointNode.Id).ToList();
            _output.WriteLine($"Outgoing transitions from start: {outgoing.Count}");
            
            if (outgoing.Count == 1)
            {
                var targetId = outgoing[0].ToNodeId;
                var targetNode = def.Nodes.FirstOrDefault(n => n.Id == targetId);
                _output.WriteLine($"Single transition target: Id='{targetNode?.Id}', Label='{targetNode?.Label}'");
                
                // Check if start node should be skipped
                var shouldSkip = startPointNode.Label == null ||
                               string.Equals(startPointNode.Label, "start", StringComparison.OrdinalIgnoreCase) ||
                               string.IsNullOrWhiteSpace(startPointNode.Label) ||
                               startPointNode.Id.StartsWith("start", StringComparison.OrdinalIgnoreCase);
                
                _output.WriteLine($"Should skip start node? {shouldSkip}");
                _output.WriteLine($"  - Label is null: {startPointNode.Label == null}");
                _output.WriteLine($"  - Label equals 'start': {string.Equals(startPointNode.Label, "start", StringComparison.OrdinalIgnoreCase)}");
                _output.WriteLine($"  - Label is whitespace: {string.IsNullOrWhiteSpace(startPointNode.Label)}");
                _output.WriteLine($"  - ID starts with 'start': {startPointNode.Id.StartsWith("start", StringComparison.OrdinalIgnoreCase)}");
            }
        }

        // This test always passes - it's just for diagnostics
        Assert.True(true, "Diagnostic test - check output for workflow structure");
    }

    [Fact]
    public void Debug_SimpleWorkflow_WithStartKeyword()
    {
        // Test with a simple workflow that uses start keyword
        var puml = @"@startuml
start;
:camera;
:next step;
stop;
@enduml";

        _output.WriteLine("=== SIMPLE WORKFLOW WITH START ===");
        _output.WriteLine(puml);
        _output.WriteLine("");

        var parser = new PlantUmlParser(puml);
        var def = parser.Parse("simple-start", "Simple");

        _output.WriteLine("--- PARSED STRUCTURE ---");
        _output.WriteLine($"StartPoints: {def.StartPoints.Count}");
        _output.WriteLine($"Nodes: {def.Nodes.Count}");
        _output.WriteLine($"Transitions: {def.Transitions.Count}");
        _output.WriteLine("");

        foreach (var node in def.Nodes)
        {
            _output.WriteLine($"  Node: Id='{node.Id}', Label='{node.Label}'");
        }

        Assert.True(true, "Diagnostic test - check output");
    }
}
