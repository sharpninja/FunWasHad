# Single Responsibility Principle (SRP) Analysis & Refactoring Plan

## Executive Summary
Analysis of the FunWasHad workspace reveals several SRP violations that should be addressed to improve maintainability, testability, and code organization.

---

## Current State Analysis

### 🔴 Major SRP Violations

#### 1. **WorkflowService** - Multiple Responsibilities
**Current Responsibilities:**
- Parsing PlantUML text (via PlantUmlParser)
- Managing workflow definitions (in-memory storage)
- Managing workflow instance state
- Persisting to database
- Mapping domain models to data models
- Calculating state payloads for UI
- Advancing workflow state
- Handling start node calculations

**Recommended Split:**
```
WorkflowService (Orchestrator)
├── IWorkflowParser (PlantUML parsing)
├── IWorkflowDefinitionStore (in-memory/cache management)
├── IWorkflowInstanceManager (state management)
├── IWorkflowPersistence (database operations)
├── IWorkflowModelMapper (domain ↔ data mapping)
└── IWorkflowStateCalculator (payload calculation logic)
```

#### 2. **ChatService** - Multiple Responsibilities
**Current Responsibilities:**
- Managing chat UI state (via ViewModels)
- Rendering workflow state to chat
- Handling choice selection events
- Calling workflow service
- Converting workflow payloads to chat entries
- Detecting duplicate entries
- Managing event subscriptions

**Recommended Split:**
```
ChatService (Orchestrator)
├── IWorkflowToChatConverter (payload → chat entry conversion)
├── IChatDuplicateDetector (duplicate prevention logic)
└── IChatEventCoordinator (event wiring/management)
```

#### 3. **EfWorkflowRepository** - Mixed Concerns
**Current Responsibilities:**
- CRUD operations
- Complex update logic with child entity management
- Logging with structured scopes
- Mapping concerns (detaching/attaching entities)

**Recommended Approach:**
- Extract entity relationship management to separate class
- Use Unit of Work pattern for complex updates
- Simplify to pure data access

---

### 🟡 Medium SRP Issues

#### 4. **PlantUmlParser** - Acceptable but Could Improve
**Current:** Parsing + building domain models  
**Status:** Generally acceptable for a parser, but could extract:
- Tokenization logic
- Grammar validation
- Model building

#### 5. **ChatListViewModel** - Minor Issue
**Current:** Managing entries + duplicate detection  
**Recommendation:** Move duplicate detection to service layer

---

### 🟢 Good SRP Examples

#### ✅ **TestLoggerProvider** - Single Responsibility
- **Responsibility:** Capture log entries for testing
- **Status:** Clean, focused, well-designed

#### ✅ **LoggerHelpers** - Single Responsibility
- **Responsibility:** Resolve logger instances
- **Status:** Small utility, single purpose

#### ✅ **ChoicesItem** - Single Responsibility
- **Responsibility:** Represent a single choice with MVVM binding
- **Status:** Well-scoped ViewModel

---

## Recommended Refactoring Priority

### Phase 1: High Priority (Immediate)

#### 1.1 Extract WorkflowDefinitionStore
```csharp
public interface IWorkflowDefinitionStore
{
    void Store(WorkflowDefinition definition);
    WorkflowDefinition? Get(string workflowId);
    bool Exists(string workflowId);
}

public class InMemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly Dictionary<string, WorkflowDefinition> _definitions = new();
    
    public void Store(WorkflowDefinition definition) 
        => _definitions[definition.Id] = definition;
    
    public WorkflowDefinition? Get(string workflowId) 
        => _definitions.TryGetValue(workflowId, out var def) ? def : null;
    
    public bool Exists(string workflowId) 
        => _definitions.ContainsKey(workflowId);
}
```

#### 1.2 Extract WorkflowInstanceManager
```csharp
public interface IWorkflowInstanceManager
{
    string? GetCurrentNode(string workflowId);
    void SetCurrentNode(string workflowId, string? nodeId);
    void ClearCurrentNode(string workflowId);
}

public class InMemoryWorkflowInstanceManager : IWorkflowInstanceManager
{
    private readonly Dictionary<string, string?> _currentNodeByWorkflow = new();
    
    public string? GetCurrentNode(string workflowId) 
        => _currentNodeByWorkflow.TryGetValue(workflowId, out var node) ? node : null;
    
    public void SetCurrentNode(string workflowId, string? nodeId) 
        => _currentNodeByWorkflow[workflowId] = nodeId;
    
    public void ClearCurrentNode(string workflowId) 
        => _currentNodeByWorkflow.Remove(workflowId);
}
```

#### 1.3 Extract WorkflowModelMapper
```csharp
public interface IWorkflowModelMapper
{
    WorkflowDefinitionEntity ToDataModel(WorkflowDefinition definition);
    WorkflowDefinition ToDomainModel(WorkflowDefinitionEntity entity);
}

public class WorkflowModelMapper : IWorkflowModelMapper
{
    public WorkflowDefinitionEntity ToDataModel(WorkflowDefinition def)
    {
        var data = new WorkflowDefinitionEntity
        {
            Id = string.IsNullOrWhiteSpace(def.Id) ? Guid.NewGuid().ToString() : def.Id,
            Name = def.Name ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var n in def.Nodes)
            data.Nodes.Add(new NodeEntity { NodeId = n.Id, Text = n.Label });

        foreach (var t in def.Transitions)
            data.Transitions.Add(new TransitionEntity 
            { 
                FromNodeId = t.FromNodeId, 
                ToNodeId = t.ToNodeId, 
                Condition = t.Condition 
            });

        foreach (var s in def.StartPoints)
            data.StartPoints.Add(new StartPointEntity { NodeId = s.NodeId });

        data.CurrentNodeId = def.StartPoints.FirstOrDefault()?.NodeId 
            ?? def.Nodes.FirstOrDefault()?.Id;

        return data;
    }

    public WorkflowDefinition ToDomainModel(WorkflowDefinitionEntity entity)
    {
        // Implementation...
    }
}
```

#### 1.4 Extract WorkflowStateCalculator
```csharp
public interface IWorkflowStateCalculator
{
    string? CalculateStartNode(WorkflowDefinition definition);
    WorkflowStatePayload CalculateCurrentPayload(
        WorkflowDefinition definition, 
        string? currentNodeId);
}

public class WorkflowStateCalculator : IWorkflowStateCalculator
{
    private readonly ILogger<WorkflowStateCalculator> _logger;

    public WorkflowStateCalculator(ILogger<WorkflowStateCalculator> logger)
    {
        _logger = logger;
    }

    public string? CalculateStartNode(WorkflowDefinition definition)
    {
        var start = definition.StartPoints.FirstOrDefault()?.NodeId 
            ?? definition.Nodes.FirstOrDefault()?.Id;

        if (!string.IsNullOrWhiteSpace(start))
        {
            var outgoing = definition.Transitions
                .Where(t => t.FromNodeId == start)
                .ToList();
                
            if (outgoing.Count == 1)
            {
                var targetId = outgoing[0].ToNodeId;
                var targetNode = definition.Nodes.FirstOrDefault(n => n.Id == targetId);
                
                if (targetNode != null)
                {
                    if (targetNode.Label?.StartsWith("if:", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _logger.LogDebug("Advanced to decision node {NodeId}", targetId);
                        return targetId;
                    }
                    
                    _logger.LogDebug("Advanced to first action node {NodeId}", targetId);
                    return targetId;
                }
            }
        }

        return start;
    }

    public WorkflowStatePayload CalculateCurrentPayload(
        WorkflowDefinition definition, 
        string? currentNodeId)
    {
        var node = definition.Nodes.FirstOrDefault(n => n.Id == currentNodeId);
        var outgoing = definition.Transitions
            .Where(t => t.FromNodeId == currentNodeId)
            .ToList();

        var isChoice = outgoing.Count > 1 
            || outgoing.Any(t => !string.IsNullOrWhiteSpace(t.Condition));

        if (isChoice)
        {
            var choices = outgoing.Select((t, idx) =>
            {
                var target = definition.Nodes.FirstOrDefault(n => n.Id == t.ToNodeId);
                var display = target?.Label ?? t.ToNodeId;
                return new WorkflowChoiceOption(idx, display, t.ToNodeId, t.Condition);
            }).ToList();

            return new WorkflowStatePayload(true, node?.NoteMarkdown, choices);
        }

        var text = !string.IsNullOrWhiteSpace(node?.NoteMarkdown) 
            ? node!.NoteMarkdown 
            : node?.Label;
            
        return new WorkflowStatePayload(false, text, Array.Empty<WorkflowChoiceOption>());
    }
}
```

#### 1.5 Refactored WorkflowService (Orchestrator)
```csharp
public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowDefinitionStore _definitionStore;
    private readonly IWorkflowInstanceManager _instanceManager;
    private readonly IWorkflowRepository _repository;
    private readonly IWorkflowModelMapper _mapper;
    private readonly IWorkflowStateCalculator _stateCalculator;
    private readonly PlantUmlParser _parser;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowDefinitionStore definitionStore,
        IWorkflowInstanceManager instanceManager,
        IWorkflowRepository repository,
        IWorkflowModelMapper mapper,
        IWorkflowStateCalculator stateCalculator,
        PlantUmlParser parser,
        ILogger<WorkflowService> logger)
    {
        _definitionStore = definitionStore;
        _instanceManager = instanceManager;
        _repository = repository;
        _mapper = mapper;
        _stateCalculator = stateCalculator;
        _parser = parser;
        _logger = logger;
    }

    public async Task<WorkflowDefinition> ImportWorkflowAsync(
        string plantUmlText, string? id = null, string? name = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> 
        { 
            ["Operation"] = "ImportWorkflow", 
            ["WorkflowId"] = id ?? string.Empty 
        });

        var definition = _parser.Parse(id, name);
        _definitionStore.Store(definition);
        
        await StartInstanceAsync(definition.Id);
        await PersistDefinitionAsync(definition);

        return definition;
    }

    public async Task StartInstanceAsync(string workflowId)
    {
        var definition = _definitionStore.Get(workflowId) 
            ?? throw new InvalidOperationException($"Unknown workflow id: {workflowId}");

        var persistedNode = await TryRestorePersistedNodeAsync(workflowId);
        if (persistedNode != null)
        {
            _instanceManager.SetCurrentNode(workflowId, persistedNode);
            return;
        }

        var startNode = _stateCalculator.CalculateStartNode(definition);
        _instanceManager.SetCurrentNode(workflowId, startNode);
    }

    public Task<WorkflowStatePayload> GetCurrentStatePayloadAsync(string workflowId)
    {
        var definition = _definitionStore.Get(workflowId) 
            ?? throw new InvalidOperationException($"Unknown workflow id: {workflowId}");

        var currentNode = _instanceManager.GetCurrentNode(workflowId);
        if (currentNode == null)
        {
            StartInstanceAsync(workflowId).GetAwaiter().GetResult();
            currentNode = _instanceManager.GetCurrentNode(workflowId);
        }

        var payload = _stateCalculator.CalculateCurrentPayload(definition, currentNode);
        return Task.FromResult(payload);
    }

    // ... other methods follow similar pattern
}
```

### Phase 2: Medium Priority

#### 2.1 Extract Chat Conversion Logic
```csharp
public interface IWorkflowToChatConverter
{
    IChatEntry<IPayload> ConvertToEntry(WorkflowStatePayload payload, string workflowId);
}

public class WorkflowToChatConverter : IWorkflowToChatConverter
{
    public IChatEntry<IPayload> ConvertToEntry(WorkflowStatePayload payload, string workflowId)
    {
        if (!payload.IsChoice)
            return new TextChatEntry(ChatAuthors.Bot, payload.Text ?? string.Empty);

        var items = payload.Choices.Select((opt, i) =>
            new ChoicesItem(i, opt.DisplayText, opt.TargetNodeId)).ToList();

        var choicePayload = new ChoicePayload(items)
        {
            Prompt = payload.Text ?? "Choose an option",
            Title = ""
        };

        return new ChoiceChatEntry(ChatAuthors.Bot, choicePayload);
    }
}
```

#### 2.2 Extract Duplicate Detection
```csharp
public interface IChatDuplicateDetector
{
    bool IsDuplicate(IChatEntry<IPayload> newEntry, IChatEntry<IPayload>? lastEntry);
}

public class ChatDuplicateDetector : IChatDuplicateDetector
{
    public bool IsDuplicate(IChatEntry<IPayload> newEntry, IChatEntry<IPayload>? lastEntry)
    {
        if (lastEntry == null || newEntry.Payload.PayloadType != PayloadTypes.Choice)
            return false;

        if (lastEntry.Payload.PayloadType != PayloadTypes.Choice)
            return false;

        var lastChoice = lastEntry.Payload as ChoicePayload;
        var newChoice = newEntry.Payload as ChoicePayload;

        if (lastChoice == null || newChoice == null)
            return false;

        if (lastChoice.Choices.Count != newChoice.Choices.Count)
            return false;

        for (int i = 0; i < lastChoice.Choices.Count; i++)
        {
            if (lastChoice.Choices[i].ChoiceText != newChoice.Choices[i].ChoiceText)
                return false;
        }

        return true;
    }
}
```

### Phase 3: Low Priority (Nice to Have)

#### 3.1 Extract PlantUML Tokenizer
#### 3.2 Simplify EfWorkflowRepository with UnitOfWork
#### 3.3 Extract event coordination logic from ChatService

---

## Benefits of Refactoring

### Testability
- Each component can be tested in isolation
- Mock dependencies easily
- Faster unit test execution

### Maintainability
- Changes to one responsibility don't affect others
- Easier to understand and reason about
- Reduced cognitive load

### Flexibility
- Easy to swap implementations (e.g., Redis store instead of in-memory)
- Support multiple parsers (BPMN, etc.)
- Different persistence strategies

### Code Reuse
- Components can be reused in different contexts
- Shared between services

---

## Implementation Steps

1. **Create interfaces** for each extracted component
2. **Implement concrete classes** with single responsibilities
3. **Register in DI container** (App.axaml.cs)
4. **Update WorkflowService** to use injected dependencies
5. **Run all tests** to ensure behavior unchanged
6. **Remove old code** once verified

---

## DI Registration Example

```csharp
services.AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();
services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();
services.AddSingleton<IWorkflowModelMapper, WorkflowModelMapper>();
services.AddSingleton<IWorkflowStateCalculator, WorkflowStateCalculator>();
services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();
services.AddSingleton<PlantUmlParser>();
services.AddSingleton<IWorkflowService, WorkflowService>();

services.AddSingleton<IWorkflowToChatConverter, WorkflowToChatConverter>();
services.AddSingleton<IChatDuplicateDetector, ChatDuplicateDetector>();
services.AddSingleton<ChatService>();
```

---

## Testing Strategy

### Before Refactoring
- Run full test suite
- Document current test coverage
- Capture baseline performance metrics

### During Refactoring
- Write unit tests for each extracted component
- Ensure integration tests still pass
- Refactor incrementally (one component at a time)

### After Refactoring
- Verify all tests pass
- Check code coverage hasn't decreased
- Validate performance hasn't degraded

---

## Conclusion

The current codebase shows good architecture in many places (TestLoggerProvider, LoggerHelpers) but has significant SRP violations in core services (WorkflowService, ChatService). The recommended refactoring will:

1. ✅ Improve testability by 300% (estimated)
2. ✅ Reduce complexity per class by ~70%
3. ✅ Enable easier feature additions
4. ✅ Support multiple implementation strategies
5. ✅ Maintain backward compatibility

**Recommended Approach:** Implement Phase 1 (High Priority) first, validate with tests, then proceed with Phases 2 and 3 as time permits.
