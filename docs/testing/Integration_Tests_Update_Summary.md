# Integration Tests Update for New Workflow Structure

**Date:** 2026-01-08  
**Status:** ✅ **COMPLETE**

---

## Overview

Updated the `FunWasHadWorkflowIntegrationTests` to correctly test the new workflow structure that includes the `get_nearby_businesses` initial action state.

---

## Changes Made

### File Modified
`FWH.Common.Chat.Tests\FunWasHadWorkflowIntegrationTests.cs`

### What Changed

#### 1. Updated Workflow Flow Expectations

**Before:**
```
START → camera → decision → record → STOP
```

**After:**
```
START → get_nearby_businesses → camera → decision → record → STOP
```

#### 2. Updated All Test Methods

**Test: `WorkflowStart_ShouldReach_GetNearbyBusinessesAction`**
- **Before:** Expected workflow to start at `camera`
- **After:** Expects workflow to start at `get_nearby_businesses`

```csharp
// Assert
Assert.NotNull(state);
// The workflow should start at "get_nearby_businesses" action node
Assert.Equal("get_nearby_businesses", state.NodeLabel, ignoreCase: true);
```

---

**Test: `GetNearbyBusinessesNode_HasActionDefinition` (NEW)**
- Added test to verify the action definition exists on the node
- Checks for JSON action metadata

```csharp
[Fact]
public async Task GetNearbyBusinessesNode_HasActionDefinition()
{
    // Verify node has action definition with parameters
    Assert.Contains("action", getNearbyNode.NoteMarkdown, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("get_nearby_businesses", getNearbyNode.NoteMarkdown, StringComparison.OrdinalIgnoreCase);
}
```

---

**Test: `CameraNode_ConvertsTo_ImageChatEntry`**
- **Before:** Expected camera as first node
- **After:** Advances past `get_nearby_businesses` first, then renders camera

```csharp
// Act - Advance past get_nearby_businesses to reach camera
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
await chatService.RenderWorkflowStateAsync(workflow.Id);
```

---

**Test: `WorkflowNavigation_FromGetNearby_ToCamera_ToDecision_Works` (RENAMED)**
- **Before:** `WorkflowNavigation_FromCamera_ToDecision_Works`
- **After:** Updated name and logic to include all three stages

```csharp
// Step 1: Verify start at get_nearby_businesses
var initialState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
Assert.Equal("get_nearby_businesses", initialState.NodeLabel, ignoreCase: true);

// Step 2: Advance to camera
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
var cameraState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
Assert.Equal("camera", cameraState.NodeLabel, ignoreCase: true);

// Step 3: Advance to decision
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
var decisionState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
Assert.True(decisionState.IsChoice);
```

---

**Test: `WorkflowBranch_FunWasHad_ReachesRecordFunExperience`**
- **Before:** Advanced past 1 node (camera)
- **After:** Advances past 2 nodes (get_nearby_businesses → camera)

```csharp
// Navigate from get_nearby_businesses -> camera -> decision
await chatService.RenderWorkflowStateAsync(workflow.Id);

// Advance past get_nearby_businesses
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);

// Advance past camera
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);

// Now at decision...
```

---

**Test: `WorkflowBranch_NotFun_ReachesRecordNotFunExperience`**
- Same changes as `WorkflowBranch_FunWasHad`
- Advances through both initial nodes before reaching decision

---

**Test: `FullWorkflow_BothBranches_ReachStopState`**
- **Before:** `maxSteps = 10`
- **After:** `maxSteps = 15` (increased to account for additional node)

```csharp
// Navigate: get_nearby_businesses -> camera -> decision -> experience recording -> end
var maxSteps = 15; // Increased from 10 to account for new initial action
```

---

**Test: `WorkflowView_WithActualWorkflow_AllOperationsWork`**
- **Before:** Expected start at `camera`
- **After:** Expects start at `get_nearby_businesses`, then advances through both nodes

```csharp
// Should start at get_nearby_businesses node
Assert.Equal("get_nearby_businesses", view.CurrentState!.NodeLabel, ignoreCase: true);

// Advance past get_nearby_businesses to camera
var advanced = await view.AdvanceAsync(null);
Assert.True(advanced);
Assert.Equal("camera", view.CurrentState!.NodeLabel, ignoreCase: true);

// Advance past camera to decision
advanced = await view.AdvanceAsync(null);
Assert.True(advanced);
Assert.True(view.CurrentState!.IsChoice);
```

**Restart Test:**
```csharp
await view.RestartAsync();
Assert.Equal("get_nearby_businesses", view.CurrentState!.NodeLabel, ignoreCase: true);
```

---

**Test: `ChatService_WithActualWorkflow_RendersAllStates`**
- **Before:** Expected `ImageChatEntry` as first entry
- **After:** Advances past initial action first, then checks for camera rendering

```csharp
// Render initial state (get_nearby_businesses)
await chatService.RenderWorkflowStateAsync(workflow.Id);
Assert.NotEmpty(chatList.Entries);

// Advance past get_nearby_businesses to camera
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
await chatService.RenderWorkflowStateAsync(workflow.Id);

// Should have ImageChatEntry for camera
var hasImageEntry = chatList.Entries.Any(e => e is ImageChatEntry);
Assert.True(hasImageEntry);
```

---

**Test: `WorkflowPersistence_SavesAndRestores_CurrentState`**
- **Before:** Advanced past 1 node
- **After:** Advances past 2 nodes before testing persistence

```csharp
// Advance past get_nearby_businesses -> camera -> decision
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null); // past get_nearby_businesses
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null); // past camera
```

---

**Test: `WorkflowStructure_HasExpectedNodes`**
- **Before:** Only verified `camera` node exists
- **After:** Verifies both `get_nearby_businesses` and `camera` nodes exist

```csharp
// Assert - Verify expected nodes exist
Assert.Contains(workflow.Nodes, n => n.Label.Equals("get_nearby_businesses", StringComparison.OrdinalIgnoreCase));
Assert.Contains(workflow.Nodes, n => n.Label.Equals("camera", StringComparison.OrdinalIgnoreCase));
Assert.Contains(workflow.Nodes, n => n.Label.Contains("Record Fun Experience", StringComparison.OrdinalIgnoreCase));
Assert.Contains(workflow.Nodes, n => n.Label.Contains("Record Not Fun Experience", StringComparison.OrdinalIgnoreCase));
```

**New Assertion:**
```csharp
// Verify transitions from get_nearby_businesses to camera
Assert.Contains(workflow.Transitions, t => 
    workflow.Nodes.Any(n => n.Id == t.FromNodeId && n.Label.Equals("get_nearby_businesses", StringComparison.OrdinalIgnoreCase)) &&
    workflow.Nodes.Any(n => n.Id == t.ToNodeId && n.Label.Equals("camera", StringComparison.OrdinalIgnoreCase)));
```

---

## Test Coverage

### Updated Tests: 11

1. ✅ `WorkflowPuml_CanBeImported_Successfully` - Verifies parsing includes new node
2. ✅ `WorkflowStart_ShouldReach_GetNearbyBusinessesAction` - Verifies start state
3. ✅ `GetNearbyBusinessesNode_HasActionDefinition` - NEW - Verifies action metadata
4. ✅ `CameraNode_ConvertsTo_ImageChatEntry` - Updated navigation
5. ✅ `WorkflowNavigation_FromGetNearby_ToCamera_ToDecision_Works` - Renamed & updated
6. ✅ `WorkflowBranch_FunWasHad_ReachesRecordFunExperience` - Updated navigation
7. ✅ `WorkflowBranch_NotFun_ReachesRecordNotFunExperience` - Updated navigation
8. ✅ `FullWorkflow_BothBranches_ReachStopState` - Increased max steps
9. ✅ `WorkflowView_WithActualWorkflow_AllOperationsWork` - Updated expectations
10. ✅ `ChatService_WithActualWorkflow_RendersAllStates` - Updated entry checks
11. ✅ `WorkflowPersistence_SavesAndRestores_CurrentState` - Updated navigation
12. ✅ `WorkflowStructure_HasExpectedNodes` - Added new node assertions

---

## Key Patterns

### Pattern 1: Advancing Past Initial Action

All tests that need to reach camera or beyond now use:

```csharp
// Start at get_nearby_businesses (initial state)
var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
Assert.Equal("get_nearby_businesses", state.NodeLabel, ignoreCase: true);

// Advance past get_nearby_businesses
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);

// Now at camera
var cameraState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
Assert.Equal("camera", cameraState.NodeLabel, ignoreCase: true);
```

### Pattern 2: Chat Rendering After Action

Tests that render chat states now account for the action node:

```csharp
// Render initial state (get_nearby_businesses action)
await chatService.RenderWorkflowStateAsync(workflow.Id);

// Advance to camera
await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
await chatService.RenderWorkflowStateAsync(workflow.Id);

// Check for camera-specific chat entry (ImageChatEntry)
var hasImageEntry = chatList.Entries.Any(e => e is ImageChatEntry);
Assert.True(hasImageEntry);
```

### Pattern 3: Workflow Structure Verification

Added verification of node relationships:

```csharp
// Verify both nodes exist
Assert.Contains(workflow.Nodes, n => n.Label.Equals("get_nearby_businesses", StringComparison.OrdinalIgnoreCase));
Assert.Contains(workflow.Nodes, n => n.Label.Equals("camera", StringComparison.OrdinalIgnoreCase));

// Verify transition exists between them
Assert.Contains(workflow.Transitions, t => 
    workflow.Nodes.Any(n => n.Id == t.FromNodeId && n.Label.Equals("get_nearby_businesses", StringComparison.OrdinalIgnoreCase)) &&
    workflow.Nodes.Any(n => n.Id == t.ToNodeId && n.Label.Equals("camera", StringComparison.OrdinalIgnoreCase)));
```

---

## Test Execution

### To Run Tests

```bash
# Run all integration tests
dotnet test FWH.Common.Chat.Tests --filter "FullyQualifiedName~FunWasHadWorkflowIntegrationTests"

# Run specific test
dotnet test FWH.Common.Chat.Tests --filter "FullyQualifiedName~WorkflowStart_ShouldReach_GetNearbyBusinessesAction"
```

### Expected Results

All 12 tests should pass:
- ✅ Workflow imports successfully
- ✅ Starts at correct node (`get_nearby_businesses`)
- ✅ Navigates through all nodes correctly
- ✅ Both branches (fun/not fun) reach their targets
- ✅ Chat rendering works with new structure
- ✅ Persistence/restore works correctly
- ✅ Workflow structure is validated

---

## Compatibility

### Backward Compatibility

These tests are **NOT backward compatible** with the old workflow structure (without `get_nearby_businesses` node). If you need to test an older version:

1. Check out the previous workflow.puml
2. Use the previous test version
3. Or add conditional logic to detect workflow version

### Forward Compatibility

Tests are designed to work with future additions:
- Action nodes can be added
- New branches can be added to decision
- Chat rendering handles any entry type

---

## Dependencies

### Required Services

Tests depend on these being registered:

1. **IWorkflowService** - Workflow operations
2. **IWorkflowController** - Workflow control
3. **ChatService** - Chat rendering
4. **ChatListViewModel** - Chat state
5. **IWorkflowRepository** - Persistence (via SqliteTestFixture)

### Optional Services

These are NOT required for basic tests but enable full functionality:

- **IGpsService** - GPS location (mocked in tests)
- **ILocationService** - Business search (mocked in tests)
- **INotificationService** - User notifications (mocked in tests)

---

## Troubleshooting

### Issue: Test Fails at Start State

**Symptom:**
```
Assert.Equal() Failure
Expected: get_nearby_businesses
Actual: camera
```

**Cause:** Workflow.puml may not have been updated

**Solution:**
1. Verify workflow.puml has `get_nearby_businesses` as first node
2. Clean and rebuild solution
3. Check workflow.puml is being loaded from correct location

---

### Issue: Test Hangs or Times Out

**Symptom:** Test never completes, times out after 30+ seconds

**Cause:** Workflow action executor may be waiting for action handler

**Solution:**
1. Ensure `GetNearbyBusinessesActionHandler` is registered in test DI
2. Mock the GPS and Location services
3. Check for infinite loops in workflow definition

---

### Issue: Navigation Test Fails

**Symptom:**
```
Assert.True() Failure
Expected: True
Actual: False
```

**Cause:** Workflow may not advance past action node

**Solution:**
1. Verify action completes successfully (doesn't return error status)
2. Check workflow has valid transitions
3. Ensure action handler returns non-null result

---

## Summary

✅ **All integration tests updated** to work with new workflow structure  
✅ **12 tests** covering end-to-end workflow execution  
✅ **New test added** for action node verification  
✅ **Comprehensive coverage** of navigation, rendering, and persistence  
✅ **Production-ready** tests that verify real-world scenarios

The integration tests now correctly validate the complete workflow including the new `get_nearby_businesses` initial action state!

---

**Document Version:** 1.0  
**Author:** GitHub Copilot  
**Date:** 2026-01-08  
**Status:** ✅ Complete
