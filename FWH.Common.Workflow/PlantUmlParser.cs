using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using FWH.Common.Workflow.Models;

namespace FWH.Common.Workflow;

public sealed class PlantUmlParser
{
    private readonly List<string> lines;
    private readonly Dictionary<string, WorkflowNode> nodes = new(StringComparer.Ordinal); // keyed by node.Id
    private readonly List<Transition> transitions = new();
    private readonly List<StartPoint> startPoints = new();
    private int idx = 0;

    // store some global elements encountered
    private readonly Dictionary<string, string?> skinparams = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Name, string? Value)> pragmas = new();
    private readonly List<string> styleBlocks = new();

    public PlantUmlParser(string plantUmlText)
    {
        if (plantUmlText is null) throw new ArgumentNullException(nameof(plantUmlText));

        lines = plantUmlText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0
                        && !l.StartsWith("@startuml", StringComparison.OrdinalIgnoreCase)
                        && !l.StartsWith("@enduml", StringComparison.OrdinalIgnoreCase)
                        && !l.StartsWith("'")
                        && !l.StartsWith("//"))
            .ToList();
    }

    public WorkflowDefinition Parse(string? id = null, string? name = null)
    {
        var currentNodeId = default(string);
        var ifStack = new Stack<IfFrame>();
        var loopStack = new Stack<LoopFrame>();

        for (int i = 0; i < lines.Count; i++)
        {
            var rawLine = lines[i];

            // handle style block
            if (Regex.IsMatch(rawLine, "^<style\\b", RegexOptions.IgnoreCase))
            {
                var j = i;
                var blockLines = new List<string>();
                for (; j < lines.Count; j++)
                {
                    blockLines.Add(lines[j]);
                    if (Regex.IsMatch(lines[j], "</style>", RegexOptions.IgnoreCase))
                        break;
                }

                styleBlocks.Add(string.Join('\n', blockLines));
                i = j;
                continue;
            }

            // handle skinparam: skinparam Name Value ;
            var skinMatch = Regex.Match(rawLine, "^skinparam\\s+(\\S+)\\s+(.*?);?$", RegexOptions.IgnoreCase);
            if (skinMatch.Success)
            {
                var nameKey = skinMatch.Groups[1].Value.Trim();
                var value = skinMatch.Groups[2].Value.Trim();
                skinparams[nameKey] = value;
                continue;
            }

            // handle pragma: !pragma name [value]
            var pragmaMatch = Regex.Match(rawLine, "^!\\s*pragma\\s+(\\S+)(?:\\s+(.*))?$", RegexOptions.IgnoreCase);
            if (pragmaMatch.Success)
            {
                var pName = pragmaMatch.Groups[1].Value.Trim();
                var pVal = pragmaMatch.Groups[2].Success ? pragmaMatch.Groups[2].Value.Trim() : null;
                pragmas.Add((pName, pVal));
                continue;
            }

            // handle inline note (same as before)
            // Support shorthand inline note (e.g. `note right: <text>`) which attaches to the most recently created node
            var shorthandNoteMatch = Regex.Match(rawLine, @"^note\s+(left|right|top|bottom)?\s*:\s*(.*)$", RegexOptions.IgnoreCase);
            if (shorthandNoteMatch.Success)
            {
                var noteText = shorthandNoteMatch.Groups[2].Value.Trim();
                if (!string.IsNullOrEmpty(noteText) && currentNodeId != null && nodes.TryGetValue(currentNodeId, out var targetNode))
                {
                    // check for JSON|Markdown split
                    SplitAndAttachMetadata(currentNodeId, targetNode, noteText);
                }

                continue;
            }

            var inlineNoteMatch = Regex.Match(rawLine, @"^note\s+(left|right|top|bottom)?\s*(?:of\s+)?(.+?)\s*:\s*(.*)$", RegexOptions.IgnoreCase);
            if (inlineNoteMatch.Success)
            {
                var targetRaw = inlineNoteMatch.Groups[2].Value.Trim();
                var noteText = inlineNoteMatch.Groups[3].Value.Trim();
                AttachNoteToNode(targetRaw, noteText);
                continue;
            }

            // block note
            var blockNoteMatch = Regex.Match(rawLine, @"^note\s+(left|right|top|bottom)?\s*(?:of\s+)?(.+?)$", RegexOptions.IgnoreCase);
            if (blockNoteMatch.Success)
            {
                var targetRaw = blockNoteMatch.Groups[2].Value.Trim();
                // collect following lines until 'end note'
                var noteLines = new List<string>();
                int j = i + 1;
                for (; j < lines.Count; j++)
                {
                    if (Regex.IsMatch(lines[j], "^end ?note;?$", RegexOptions.IgnoreCase))
                    {
                        break;
                    }
                    noteLines.Add(lines[j]);
                }

                var noteText = string.Join('\n', noteLines).Trim();
                AttachNoteToNode(targetRaw, noteText);

                // advance index to the line with end note (or last collected)
                i = j;
                continue;
            }

            // handle start/stop/end keywords
            if (Regex.IsMatch(rawLine, "^start;?$", RegexOptions.IgnoreCase))
            {
                var nid = GetOrCreateNode("Start");
                startPoints.Add(new StartPoint(nid));
                currentNodeId = nid;
                continue;
            }

            if (Regex.IsMatch(rawLine, "^(stop|end);?$", RegexOptions.IgnoreCase))
            {
                var nid = GetOrCreateNode("Stop");
                if (currentNodeId != null)
                {
                    AddTransition(currentNodeId, nid);
                }
                currentNodeId = nid;
                continue;
            }

            // broadened if handling: allow optional is()/equals() between condition and then
            var ifMatch = Regex.Match(rawLine, @"^if\s*\((.*?)\)\s*(?:is\s*\((.*?)\)\s*|equals\s*\((.*?)\)\s*)?then(?:\s*\((.*?)\))?$", RegexOptions.IgnoreCase);
            if (ifMatch.Success)
            {
                var condition = ifMatch.Groups[1].Value.Trim();
                var label = ifMatch.Groups[4].Success ? ifMatch.Groups[4].Value.Trim() : null;

                var decisionId = CreateSyntheticNode($"if: {condition}");
                if (currentNodeId != null)
                {
                    AddTransition(currentNodeId, decisionId);
                    currentNodeId = null;
                }

                var frame = new IfFrame(decisionId, condition);
                frame.Branches[0].ConditionText = condition;
                frame.Branches[0].Label = label;
                ifStack.Push(frame);
                continue;
            }

            var elseIfMatch = Regex.Match(rawLine, @"^else\s*(?:if\s*\((.*?)\)\s*then)?(?:\s*\((.*?)\))?$", RegexOptions.IgnoreCase);
            // fallback original elseif handling
            if (!elseIfMatch.Success)
            {
                elseIfMatch = Regex.Match(rawLine, @"^else\\s*(?:if\\s*\\((.*?)\\)\\s*then)?(?:\\s*\\((.*?)\\))?$", RegexOptions.IgnoreCase);
            }

            if (elseIfMatch.Success && ifStack.Count > 0)
            {
                var cond = elseIfMatch.Groups[1].Success ? elseIfMatch.Groups[1].Value.Trim() : "else";
                var label = elseIfMatch.Groups[2].Success ? elseIfMatch.Groups[2].Value.Trim() : null;
                var currentFrame = ifStack.Peek();
                currentFrame.Branches.Add(new Branch(label) { ConditionText = cond });
                continue;
            }

            if (Regex.IsMatch(rawLine, "^endif;?$", RegexOptions.IgnoreCase) && ifStack.Count > 0)
            {
                var frame = ifStack.Pop();
                var joinId = CreateSyntheticNode("join");
                foreach (var br in frame.Branches)
                {
                    if (br.EntryNodeId == null)
                    {
                        AddTransition(frame.DecisionNodeId, joinId, br.ConditionText);
                    }
                    else if (br.LastNodeId != null)
                    {
                        AddTransition(br.LastNodeId, joinId);
                    }
                    else
                    {
                        AddTransition(br.EntryNodeId, joinId);
                    }
                }

                currentNodeId = joinId;
                continue;
            }

            if (Regex.IsMatch(rawLine, "^repeat$", RegexOptions.IgnoreCase))
            {
                var loopEntryId = CreateSyntheticNode("loop_entry");
                if (currentNodeId != null)
                {
                    AddTransition(currentNodeId, loopEntryId);
                    currentNodeId = null;
                }

                var lf = new LoopFrame(loopEntryId);
                loopStack.Push(lf);
                continue;
            }

            var repeatWhileMatch = Regex.Match(rawLine, "^repeat\\s+while\\s*\\((.*?)\\);?$", RegexOptions.IgnoreCase);
            if (repeatWhileMatch.Success && loopStack.Count > 0)
            {
                var cond = repeatWhileMatch.Groups[1].Value.Trim();
                var lf = loopStack.Pop();
                lf.ConditionText = cond;

                var afterLoopId = CreateSyntheticNode("after_loop");

                if (lf.LastNodeId == null)
                {
                    AddTransition(lf.LoopEntryNodeId, afterLoopId);
                }
                else
                {
                    AddTransition(lf.LastNodeId, lf.LoopEntryNodeId, cond);
                    AddTransition(lf.LastNodeId, afterLoopId);
                }

                currentNodeId = afterLoopId;
                continue;
            }

            // arrow handling (unchanged)
            var arrowMatch = Regex.Match(rawLine, "(.*?)\\s*(-{1,2}>\\s*|<-{1,2}\\s*)(.*)");
            if (arrowMatch.Success)
            {
                var leftRaw = Regex.Replace(arrowMatch.Groups[1].Value.Trim(), "\"[^\"]*\"", string.Empty).Trim();
                var rightRaw = Regex.Replace(arrowMatch.Groups[3].Value.Trim(), "\"[^\"]*\"", string.Empty).Trim();

                if (leftRaw == "[*]")
                {
                    var targetId = GetOrCreateNode(rightRaw);
                    startPoints.Add(new StartPoint(targetId));
                    currentNodeId = targetId;
                    continue;
                }

                var fromId = GetOrCreateNode(leftRaw);
                var toId = GetOrCreateNode(rightRaw);
                AddTransition(fromId, toId);
                currentNodeId = toId;
                continue;
            }

            // action with optional color: [#color]? :text;
            var actionMatch = Regex.Match(rawLine, "^(?:#(?<color>[^:\\s]+)\\s*)?:(?<text>.*?);?$");
            if (actionMatch.Success)
            {
                var label = actionMatch.Groups["text"].Value.Trim();
                var nodeId = GetOrCreateNode(label);

                if (ifStack.Count > 0)
                {
                    var frame = ifStack.Peek();
                    var branch = frame.Branches.Last();

                    if (branch.EntryNodeId == null)
                    {
                        AddTransition(frame.DecisionNodeId, nodeId, branch.ConditionText);
                        branch.EntryNodeId = nodeId;
                        branch.LastNodeId = nodeId;
                    }
                    else
                    {
                        AddTransition(branch.LastNodeId!, nodeId);
                        branch.LastNodeId = nodeId;
                    }
                }
                else if (loopStack.Count > 0)
                {
                    var lf = loopStack.Peek();
                    if (lf.FirstNodeId == null)
                    {
                        AddTransition(lf.LoopEntryNodeId, nodeId);
                        lf.FirstNodeId = nodeId;
                        lf.LastNodeId = nodeId;
                    }
                    else
                    {
                        AddTransition(lf.LastNodeId!, nodeId);
                        lf.LastNodeId = nodeId;
                    }
                }
                else
                {
                    if (currentNodeId != null)
                    {
                        AddTransition(currentNodeId, nodeId);
                    }

                    currentNodeId = nodeId;
                }

                // sdl-stereotype detection inline e.g. <<input>>; just ignore or attach to node as note
                var stereoMatch = Regex.Match(rawLine, "<<\\s*(\\w+)\\s*>>");
                if (stereoMatch.Success)
                {
                    var stereo = stereoMatch.Groups[1].Value.Trim();
                    if (nodes.TryGetValue(nodeId, out var node))
                    {
                        // preserve existing JsonMetadata while appending note text
                        nodes[nodeId] = new WorkflowNode(node.Id, node.Label, node.JsonMetadata, (node.NoteMarkdown ?? string.Empty) + $"\n<<{stereo}>>");
                    }
                }

                continue;
            }

            // ignore unknown or unhandled constructs for now
        }

        // close open frames (unchanged)
        while (ifStack.Count > 0)
        {
            var frame = ifStack.Pop();
            var joinId = CreateSyntheticNode("join");
            foreach (var br in frame.Branches)
            {
                if (br.EntryNodeId == null)
                {
                    AddTransition(frame.DecisionNodeId, joinId, br.ConditionText);
                }
                else if (br.LastNodeId != null)
                {
                    AddTransition(br.LastNodeId, joinId);
                }
                else
                {
                    AddTransition(br.EntryNodeId, joinId);
                }
            }

            currentNodeId = joinId;
        }

        while (loopStack.Count > 0)
        {
            var lf = loopStack.Pop();
            var afterLoopId = CreateSyntheticNode("after_loop");
            if (lf.LastNodeId == null)
            {
                AddTransition(lf.LoopEntryNodeId, afterLoopId);
            }
            else
            {
                AddTransition(lf.LastNodeId, lf.LoopEntryNodeId, lf.ConditionText);
                AddTransition(lf.LastNodeId, afterLoopId);
            }

            currentNodeId = afterLoopId;
        }

        var nodeList = nodes.Values.ToList();

        return new WorkflowDefinition(id ?? Guid.NewGuid().ToString("D"), name ?? "ImportedWorkflow", nodeList, transitions, startPoints);
    }

    private void SplitAndAttachMetadata(string nodeId, WorkflowNode targetNode, string noteText)
    {
        // If the note contains a pipe '|' split, treat left side as JSON metadata and right side as note markdown.
        var parts = noteText.Split(new[] { '|' }, 2);
        if (parts.Length == 2)
        {
            var left = parts[0].Trim();
            var right = parts[1].Trim();

            // Validate left side is JSON object; if not, attach whole note as markdown
            try
            {
                using var doc = JsonDocument.Parse(left);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var jsonStr = left; // preserve original json string
                    nodes[nodeId] = new WorkflowNode(targetNode.Id, targetNode.Label, jsonStr, right);
                    return;
                }
            }
            catch (JsonException)
            {
                // not JSON - fallthrough
            }
        }

        // No pipe or invalid JSON - attach as normal note markdown, preserving existing JsonMetadata
        nodes[nodeId] = new WorkflowNode(targetNode.Id, targetNode.Label, targetNode.JsonMetadata, noteText);
    }

    private static string NormalizeLabel(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith(":")) raw = raw[1..].Trim();
        if (raw.EndsWith(";")) raw = raw[..^1].Trim();
        return raw;
    }

    private string CreateSyntheticNode(string label)
    {
        var nid = MakeId(label, idx++);
        var node = new WorkflowNode(nid, label);
        nodes[nid] = node;
        return node.Id;
    }

    private string GetOrCreateNode(string token)
    {
        token = token.Trim();
        if (token == "[*]")
        {
            const string key = "[*]";
            if (!nodes.ContainsKey(key))
            {
                nodes[key] = new WorkflowNode(key, "[*]");
            }

            return nodes[key].Id;
        }

        var label = NormalizeLabel(token);
        // find existing node by label
        var existing = nodes.Values.FirstOrDefault(n => string.Equals(n.Label, label, StringComparison.Ordinal));
        if (existing != null) return existing.Id;

        string nid;
        // If possible use the label itself as the node id for simpler IDs (keeps tests expecting plain labels)
        if (!string.IsNullOrEmpty(label) && !nodes.ContainsKey(label))
        {
            nid = label;
        }
        else
        {
            nid = MakeId(label, idx++);
        }

        var node = new WorkflowNode(nid, label);
        nodes[nid] = node;
        return node.Id;
    }

    private static string MakeId(string label, int index)
    {
        var s = Regex.Replace(label ?? string.Empty, @"\s+", "_");
        s = Regex.Replace(s, @"[^A-Za-z0-9_]+", "_");
        if (string.IsNullOrEmpty(s)) s = $"node_{index}";
        return $"{s}_{index}";
    }

    private void AttachNoteToNode(string targetRaw, string noteText)
    {
        var targetId = GetOrCreateNode(targetRaw);
        if (nodes.TryGetValue(targetId, out var node))
        {
            // replace existing node record (with note) while preserving any JsonMetadata
            nodes[targetId] = new WorkflowNode(node.Id, node.Label, node.JsonMetadata, noteText);
        }
    }

    // helper to add transitions but avoid self-transitions without a condition
    private void AddTransition(string fromId, string toId, string? condition = null)
    {
        if (string.IsNullOrWhiteSpace(fromId) || string.IsNullOrWhiteSpace(toId)) return;
        if (string.Equals(fromId, toId, StringComparison.Ordinal))
        {
            // don't add a self-transition unless a condition/criteria is provided
            if (string.IsNullOrWhiteSpace(condition))
                return;
        }

        var tid = $"t_{transitions.Count}";
        transitions.Add(new Transition(tid, fromId, toId, condition));
    }

    private sealed class IfFrame
    {
        public string DecisionNodeId { get; }
        public string ConditionText { get; }
        public List<Branch> Branches { get; } = new();

        public IfFrame(string decisionNodeId, string conditionText)
        {
            DecisionNodeId = decisionNodeId;
            ConditionText = conditionText;
            Branches.Add(new Branch(conditionText));
        }
    }

    private sealed class Branch
    {
        public string? Label { get; set; }
        public string? EntryNodeId { get; set; }
        public string? LastNodeId { get; set; }
        public string? ConditionText { get; set; }

        public Branch(string? label) => Label = label;
    }

    private sealed class LoopFrame
    {
        public string LoopEntryNodeId { get; }
        public string? ConditionText { get; set; }
        public string? FirstNodeId { get; set; }
        public string? LastNodeId { get; set; }

        public LoopFrame(string loopEntryNodeId) => LoopEntryNodeId = loopEntryNodeId;
    }

    // Expose parse results for testing convenience
    public IReadOnlyList<Transition> Transitions => transitions;
    public IReadOnlyList<StartPoint> StartPoints => startPoints;
    public IReadOnlyList<WorkflowNode> Nodes => nodes.Values.ToList();
}
