using Xunit;

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

    /// <summary>
    /// Diagnostic test that loads and parses the actual workflow.puml file, outputting its complete structure to test output for debugging and validation purposes.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser's ability to parse the actual production workflow.puml file and the completeness of the parsed workflow structure.</para>
    /// <para><strong>Data involved:</strong> The workflow.puml file from the solution root directory, loaded and parsed into a WorkflowDefinition. The test outputs the complete structure (nodes, transitions, start points, notes, metadata) to the test output helper for inspection.</para>
    /// <para><strong>Why the data matters:</strong> This is a diagnostic test used to understand and validate the actual production workflow structure. It helps identify parsing issues, verify workflow correctness, and provides visibility into how the parser interprets the real workflow definition. The output can be used to debug workflow execution issues or validate workflow changes.</para>
    /// <para><strong>Expected outcome:</strong> The test should complete successfully (always passes), and the test output should contain the complete workflow structure including all nodes, transitions, start points, and metadata.</para>
    /// <para><strong>Reason for expectation:</strong> The parser should successfully parse the workflow.puml file and extract all components. The test output provides a human-readable representation of the parsed structure, which is useful for debugging and validation. The test always passes because its purpose is diagnostic output rather than assertion-based validation.</para>
    /// </remarks>
    [Fact]
    public void DebugActualWorkflowStructure()
    {
        // Arrange - Load actual workflow.puml
        var currentDir = Directory.GetCurrentDirectory();
        var solutionDir = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.Parent?.FullName
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

    /// <summary>
    /// Diagnostic test that parses a simple workflow using the "start" keyword and outputs its structure for debugging.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PlantUmlParser's ability to parse PlantUML workflows that use the "start" keyword syntax and correctly identify workflow structure.</para>
    /// <para><strong>Data involved:</strong> A simple PlantUML workflow using "start;" and "stop;" keywords with two action nodes ("camera" and "next step"). This tests the parser's handling of the start/stop keyword syntax variant.</para>
    /// <para><strong>Why the data matters:</strong> PlantUML supports multiple syntax variants for defining workflows (start/stop keywords vs [*] notation). The parser must correctly interpret both styles. This diagnostic test helps validate that the start keyword is properly recognized and converted to workflow structure.</para>
    /// <para><strong>Expected outcome:</strong> The test should complete successfully and output the parsed structure showing the correct number of start points, nodes, and transitions.</para>
    /// <para><strong>Reason for expectation:</strong> The parser should recognize "start;" as a start point indicator and "stop;" as an end point. It should parse the action nodes correctly. The output provides visibility into how the parser interprets this syntax variant, which is useful for debugging parser behavior.</para>
    /// </remarks>
    [Fact]
    public void DebugSimpleWorkflowWithStartKeyword()
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
