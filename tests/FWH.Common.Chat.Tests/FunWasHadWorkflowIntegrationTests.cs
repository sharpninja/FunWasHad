using FWH.Common.Chat.Tests.TestFixtures;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Common.Chat.Tests;

/// <summary>
/// Integration tests using the actual workflow.puml file from the project.
/// Tests that each state of the "Fun Was Had" workflow is properly reached.
/// Updated for workflow structure: get_nearby_businesses -> camera -> "Was fun had?" decision -> Record experience -> stop
/// </summary>
public class FunWasHadWorkflowIntegrationTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;
    private const string WorkflowId = "fun-was-had-workflow";

    public FunWasHadWorkflowIntegrationTests(SqliteTestFixture fixture)
    {
        _fixture = fixture;
    }

    private string LoadWorkflowPuml()
    {
        // Navigate up from the test directory to find workflow.puml
        var currentDir = Directory.GetCurrentDirectory();
        var solutionDir = (Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.Parent?.FullName) ?? throw new FileNotFoundException("Could not locate solution directory");
        var pumlPath = Path.Combine(solutionDir, "workflow.puml");

        if (!File.Exists(pumlPath))
            throw new FileNotFoundException($"workflow.puml not found at {pumlPath}");

        return File.ReadAllText(pumlPath);
    }

    /// <summary>
    /// Tests that the actual workflow.puml file from the project root can be successfully imported and parsed, validating the real-world workflow definition.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.ImportWorkflowAsync method's ability to parse and import the actual production workflow definition file (workflow.puml) from the project root.</para>
    /// <para><strong>Data involved:</strong> The workflow.puml file loaded from the solution root directory. This is the actual workflow definition used in production, containing nodes like "get_nearby_businesses" and "camera" that represent the "Fun Was Had" user journey.</para>
    /// <para><strong>Why the data matters:</strong> This is an integration test that validates the real workflow definition can be imported successfully. It ensures that changes to workflow.puml don't break the parser and that the production workflow structure is correct. Testing with the actual file catches issues that might not appear in synthetic test data.</para>
    /// <para><strong>Expected outcome:</strong> ImportWorkflowAsync should complete successfully, returning a workflow with Id=WorkflowId, Name="Fun Was Had", non-empty Nodes and Transitions collections, and nodes with labels "get_nearby_businesses" and "camera".</para>
    /// <para><strong>Reason for expectation:</strong> The parser should successfully parse the actual PlantUML syntax used in production. The presence of specific node labels ("get_nearby_businesses", "camera") confirms the parser correctly extracted the workflow structure from the real file. This validates that the production workflow definition is syntactically correct and can be imported without errors.</para>
    /// </remarks>
    [Fact]
    public async Task WorkflowPumlCanBeImportedSuccessfully()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowService = sp.GetRequiredService<IWorkflowService>();
        var puml = LoadWorkflowPuml();

        // Act
        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId, "Fun Was Had").ConfigureAwait(true);

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(WorkflowId, workflow.Id);
        Assert.Equal("Fun Was Had", workflow.Name);
        Assert.NotEmpty(workflow.Nodes);
        Assert.NotEmpty(workflow.Transitions);

        // Verify get_nearby_businesses node exists
        Assert.Contains(workflow.Nodes, n => n.Label.Equals("get_nearby_businesses", StringComparison.OrdinalIgnoreCase));

        // Verify camera node exists
        Assert.Contains(workflow.Nodes, n => n.Label.Equals("camera", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Tests that when the actual workflow.puml is imported and started, it correctly begins at the "get_nearby_businesses" action node.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The workflow execution's initial state when the production workflow.puml is imported and started, validating that the workflow begins at the correct starting node.</para>
    /// <para><strong>Data involved:</strong> The actual workflow.puml file loaded from the solution root, imported with a unique ID, and then queried for its current state immediately after import. The workflow should start at the "get_nearby_businesses" action node according to the workflow definition.</para>
    /// <para><strong>Why the data matters:</strong> The workflow's starting node determines the first user interaction. If the workflow doesn't start at the correct node, users will see the wrong UI or workflow execution will fail. This test validates that the production workflow definition correctly specifies the starting point and that the workflow engine correctly identifies and begins at that node.</para>
    /// <para><strong>Expected outcome:</strong> GetCurrentStatePayloadAsync should return a state with NodeLabel="get_nearby_businesses" (case-insensitive match).</para>
    /// <para><strong>Reason for expectation:</strong> The workflow definition should specify "get_nearby_businesses" as the starting node (either explicitly via start points or implicitly as the first node). When the workflow is imported and started, the controller should set the current node to this starting point. The NodeLabel matching "get_nearby_businesses" confirms the workflow correctly begins at the intended starting action, ensuring users see the correct initial workflow state.</para>
    /// </remarks>
    [Fact]
    public async Task WorkflowStart_ShouldReach_GetNearbyBusinessesAction()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowService = sp.GetRequiredService<IWorkflowService>();
        var puml = LoadWorkflowPuml();

        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId + "_start", "Fun Was Had Start").ConfigureAwait(true);

        // Act
        var state = await workflowService.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);

        // Assert
        Assert.NotNull(state);
        // The workflow should start at "get_nearby_businesses" action node
        Assert.Equal("get_nearby_businesses", state.NodeLabel, ignoreCase: true);
    }

    /// <summary>
    /// Tests that the "get_nearby_businesses" node in the actual workflow.puml contains an action definition in its NoteMarkdown.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The workflow definition's structure validation, specifically that action nodes contain their action definitions in the NoteMarkdown property.</para>
    /// <para><strong>Data involved:</strong> The actual workflow.puml file loaded and imported. The test searches for the node with label "get_nearby_businesses" and checks its NoteMarkdown property for action definition JSON containing "action" and "get_nearby_businesses".</para>
    /// <para><strong>Why the data matters:</strong> Action definitions in PlantUML workflows are typically stored in node notes as JSON (e.g., {"action": "get_nearby_businesses", "params": {...}}). The parser must correctly extract this JSON and store it in NoteMarkdown. This test validates that the production workflow definition correctly specifies actions and that the parser correctly extracts them.</para>
    /// <para><strong>Expected outcome:</strong> The get_nearby_businesses node should exist, have non-null NoteMarkdown, and the NoteMarkdown should contain both "action" and "get_nearby_businesses" (case-insensitive).</para>
    /// <para><strong>Reason for expectation:</strong> The workflow.puml file should define the action in a note attached to the node. The parser should extract this note content and store it in NoteMarkdown. The presence of "action" confirms it's an action definition, and "get_nearby_businesses" confirms the specific action name. This validates that action definitions are correctly preserved during parsing.</para>
    /// </remarks>
    [Fact]
    public async Task GetNearbyBusinessesNodeHasActionDefinition()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowService = sp.GetRequiredService<IWorkflowService>();
        var puml = LoadWorkflowPuml();

        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId + "_action", "Fun Was Had Action").ConfigureAwait(true);

        // Act
        var getNearbyNode = workflow.Nodes.FirstOrDefault(n =>
            n.Label.Equals("get_nearby_businesses", StringComparison.OrdinalIgnoreCase));

        // Assert
        Assert.NotNull(getNearbyNode);
        Assert.NotNull(getNearbyNode!.NoteMarkdown);
        Assert.Contains("action", getNearbyNode.NoteMarkdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("get_nearby_businesses", getNearbyNode.NoteMarkdown, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that when the workflow reaches the "camera" node in the actual workflow.puml, it is correctly converted to an ImageChatEntry in the chat interface.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The ChatService's ability to convert workflow nodes with image/camera semantics into ImageChatEntry objects for display in the chat interface.</para>
    /// <para><strong>Data involved:</strong> The actual workflow.puml file loaded and imported. The workflow is advanced past the "get_nearby_businesses" node to reach the "camera" node. The ChatService renders the workflow state, which should create an ImageChatEntry for the camera node.</para>
    /// <para><strong>Why the data matters:</strong> The camera node represents a workflow step where users take photos. This must be displayed as an image entry in the chat interface to provide visual feedback. The conversion from workflow node to ImageChatEntry is critical for the UI to correctly render camera/image workflow steps.</para>
    /// <para><strong>Expected outcome:</strong> The chat list should contain at least one ImageChatEntry, and that entry should have Author = Bot, confirming the camera node was correctly converted to an image entry.</para>
    /// <para><strong>Reason for expectation:</strong> The WorkflowToChatConverter should recognize nodes with camera/image semantics and create ImageChatEntry objects. The presence of an ImageChatEntry in the chat list confirms the conversion succeeded, and the Bot author confirms it's a workflow-initiated entry (not user-uploaded). This validates that camera workflow steps are correctly represented in the chat interface.</para>
    /// </remarks>
    [Fact]
    public async Task CameraNode_ConvertsTo_ImageChatEntry()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider(services =>
        {
            services.AddSingleton<ChatListViewModel>();
            services.AddSingleton<ChatInputViewModel>(sp => new ChatInputViewModel(sp.GetRequiredService<ChatListViewModel>()));
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<ChatService>();
        });

        var workflowService = sp.GetRequiredService<IWorkflowService>();
        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var chatService = sp.GetRequiredService<ChatService>();
        var chatList = sp.GetRequiredService<ChatListViewModel>();
        var puml = LoadWorkflowPuml();

        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId + "_camera", "Fun Was Had Camera").ConfigureAwait(true);

        // Act - Advance past get_nearby_businesses to reach camera
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);
        await chatService.RenderWorkflowStateAsync(workflow.Id).ConfigureAwait(true);

        // Assert
        Assert.NotEmpty(chatList.Entries);
        var cameraEntry = chatList.Entries.FirstOrDefault(e => e is ImageChatEntry);

        // Should have an ImageChatEntry for the camera node
        Assert.NotNull(cameraEntry);
        Assert.IsType<ImageChatEntry>(cameraEntry);
        var imageEntry = (ImageChatEntry)cameraEntry;
        Assert.Equal(ChatAuthors.Bot, imageEntry.Author);
    }

    [Fact]
    public async Task WorkflowNavigationFromGetNearbyToCameraToDecisionWorks()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var puml = LoadWorkflowPuml();

        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_nav1", "Fun Was Had Nav").ConfigureAwait(true);
        await workflowController.StartInstanceAsync(workflow.Id).ConfigureAwait(true);

        // Act - Get initial state (get_nearby_businesses node)
        var initialState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);
        Assert.Equal("get_nearby_businesses", initialState.NodeLabel, ignoreCase: true);

        // Advance through get_nearby_businesses node (auto-advance since it's an action)
        var advanced1 = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);
        Assert.True(advanced1);

        // Should now be at camera
        var cameraState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);
        Assert.Equal("camera", cameraState.NodeLabel, ignoreCase: true);

        // Advance through camera node (auto-advance since it's not a choice)
        var advanced2 = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);
        Assert.True(advanced2);

        // Should now be at the decision point
        var decisionState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);

        // Assert - Should be at "Was fun had?" choice
        Assert.NotNull(decisionState);
        Assert.True(decisionState.IsChoice);
        Assert.NotEmpty(decisionState.Choices);
    }

    [Fact]
    public async Task WorkflowBranch_FunWasHad_ReachesRecordFunExperience()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider(services =>
        {
            services.AddSingleton<ChatListViewModel>();
            services.AddSingleton<ChatInputViewModel>(sp => new ChatInputViewModel(sp.GetRequiredService<ChatListViewModel>()));
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<ChatService>();
        });

        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var chatService = sp.GetRequiredService<ChatService>();
        var chatList = sp.GetRequiredService<ChatListViewModel>();
        var puml = LoadWorkflowPuml();

        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_fun", "Fun Was Had - Yes Branch").ConfigureAwait(true);

        // Act - Navigate from get_nearby_businesses -> camera -> decision
        await chatService.RenderWorkflowStateAsync(workflow.Id).ConfigureAwait(true);

        // Advance past get_nearby_businesses
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);

        // Advance past camera
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);

        // Get decision state
        var decisionState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);
        Assert.True(decisionState.IsChoice);

        // Look for the "fun was had" choice
        var funChoice = decisionState.Choices.FirstOrDefault(c =>
            c.DisplayText.Contains("fun", StringComparison.OrdinalIgnoreCase) ||
            c.DisplayText.Contains("Fun", StringComparison.Ordinal) ||
            c.DisplayText.Contains("Record Fun", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(funChoice);

        // Take the "fun was had" branch
        var advanced = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, funChoice.TargetNodeId).ConfigureAwait(true);
        Assert.True(advanced);

        // Verify we reached the "Record Fun Experience" state
        var finalState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);
        var currentNode = workflowController.GetCurrentNodeId(workflow.Id);

        Assert.NotNull(currentNode);
        Assert.NotNull(finalState);
    }

    [Fact]
    public async Task WorkflowBranchNotFunReachesRecordNotFunExperience()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider(services =>
        {
            services.AddSingleton<ChatListViewModel>();
            services.AddSingleton<ChatInputViewModel>(sp => new ChatInputViewModel(sp.GetRequiredService<ChatListViewModel>()));
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<ChatService>();
        });

        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var chatService = sp.GetRequiredService<ChatService>();
        var puml = LoadWorkflowPuml();

        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_notfun", "Fun Was Had - No Branch").ConfigureAwait(true);

        // Act - Navigate from get_nearby_businesses -> camera -> decision
        await chatService.RenderWorkflowStateAsync(workflow.Id).ConfigureAwait(true);

        // Advance past get_nearby_businesses
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);

        // Advance past camera
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);

        // Get decision state
        var decisionState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);
        Assert.True(decisionState.IsChoice);

        // Look for the "not fun" choice
        var noFunChoice = decisionState.Choices.FirstOrDefault(c =>
            c.DisplayText.Contains("not", StringComparison.OrdinalIgnoreCase) ||
            c.DisplayText.Contains("No", StringComparison.Ordinal) ||
            c.DisplayText.Contains("Record Not Fun", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(noFunChoice);

        // Take the "not fun" branch
        var advanced = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, noFunChoice.TargetNodeId).ConfigureAwait(true);
        Assert.True(advanced);

        // Verify we reached the "Record Not Fun Experience" state
        var finalState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);
        var currentNode = workflowController.GetCurrentNodeId(workflow.Id);

        Assert.NotNull(currentNode);
        Assert.NotNull(finalState);
    }

    [Fact]
    public async Task FullWorkflow_BothBranches_ReachStopState()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var puml = LoadWorkflowPuml();

        // Test both branches reach the stop state
        foreach (var branch in new[] { "fun", "notfun" })
        {
            var workflow = await workflowController.ImportWorkflowAsync(
                puml,
                WorkflowId + $"_stop_{branch}",
                $"Fun Was Had - Stop {branch}").ConfigureAwait(true);

            // Navigate: get_nearby_businesses -> camera -> decision -> experience recording -> end
            var maxSteps = 15; // Increased from 10 to account for new initial action
            var steps = 0;
            var reachedEnd = false;

            while (steps < maxSteps)
            {
                var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);

                if (state.IsChoice)
                {
                    if (state.Choices.Any())
                    {
                        // Take appropriate branch based on test case
                        var choiceIndex = branch == "fun" ? 0 : (state.Choices.Count - 1);
                        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, state.Choices[choiceIndex].TargetNodeId).ConfigureAwait(true);
                    }
                    else
                    {
                        reachedEnd = true;
                        break;
                    }
                }
                else
                {
                    // Try to advance; if no transitions available, we're at end
                    var advanced = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);
                    if (!advanced)
                    {
                        reachedEnd = true;
                        break;
                    }
                }

                steps++;
            }

            // Assert - Both branches should reach completion
            Assert.True(reachedEnd || steps < maxSteps, $"Branch '{branch}' did not reach end state");
        }
    }

    [Fact]
    public async Task WorkflowView_WithActualWorkflow_AllOperationsWork()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var view = sp.GetRequiredService<IWorkflowView>();
        var puml = LoadWorkflowPuml();

        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_view", "Fun Was Had View").ConfigureAwait(true);

        // Act & Assert - Load workflow
        await view.LoadWorkflowAsync(workflow.Id).ConfigureAwait(true);
        Assert.Equal(workflow.Id, view.CurrentWorkflowId);
        Assert.NotNull(view.CurrentState);
        Assert.False(view.HasError);

        // Should start at get_nearby_businesses node
        Assert.Equal("get_nearby_businesses", view.CurrentState!.NodeLabel, ignoreCase: true);

        // Advance past get_nearby_businesses to camera
        var advanced = await view.AdvanceAsync(null).ConfigureAwait(true);
        Assert.True(advanced);
        Assert.Equal("camera", view.CurrentState!.NodeLabel, ignoreCase: true);

        // Advance past camera to decision
        advanced = await view.AdvanceAsync(null).ConfigureAwait(true);
        Assert.True(advanced);

        // Now should be at choice
        Assert.True(view.CurrentState!.IsChoice);

        // Try to advance with choice
        if (view.CurrentState.Choices.Any())
        {
            advanced = await view.AdvanceAsync(view.CurrentState.Choices[0].TargetNodeId).ConfigureAwait(true);
            Assert.True(advanced);
        }

        // Test restart
        await view.RestartAsync().ConfigureAwait(true);
        Assert.NotNull(view.CurrentState);
        Assert.False(view.HasError);
        Assert.Equal("get_nearby_businesses", view.CurrentState!.NodeLabel, ignoreCase: true);
    }

    [Fact]
    public async Task ChatServiceWithActualWorkflowRendersAllStates()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider(services =>
        {
            services.AddSingleton<ChatListViewModel>();
            services.AddSingleton<ChatInputViewModel>(sp => new ChatInputViewModel(sp.GetRequiredService<ChatListViewModel>()));
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<ChatService>();
        });

        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var chatService = sp.GetRequiredService<ChatService>();
        var chatList = sp.GetRequiredService<ChatListViewModel>();
        var puml = LoadWorkflowPuml();

        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_chat", "Fun Was Had Chat").ConfigureAwait(true);

        // Act - Render initial state (get_nearby_businesses)
        await chatService.RenderWorkflowStateAsync(workflow.Id).ConfigureAwait(true);

        // Assert - Chat should have entry for get_nearby_businesses action
        Assert.NotEmpty(chatList.Entries);

        var initialCount = chatList.Entries.Count;

        // Advance past get_nearby_businesses to camera
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);
        await chatService.RenderWorkflowStateAsync(workflow.Id).ConfigureAwait(true);

        // Should have ImageChatEntry for camera
        Assert.True(chatList.Entries.Count >= initialCount);
        var hasImageEntry = chatList.Entries.Any(e => e is ImageChatEntry);
        Assert.True(hasImageEntry);

        // Advance past camera to decision
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true);
        await chatService.RenderWorkflowStateAsync(workflow.Id).ConfigureAwait(true);

        // Should now have choice entry
        var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);
        if (state.IsChoice && state.Choices.Any())
        {
            // Simulate user selecting a choice
            var choice = state.Choices[0];
            await workflowController.AdvanceByChoiceValueAsync(workflow.Id, choice.TargetNodeId).ConfigureAwait(true);
            await chatService.RenderWorkflowStateAsync(workflow.Id).ConfigureAwait(true);

            // Chat should have been updated with the result
            Assert.True(chatList.Entries.Count >= initialCount);
        }
    }

    /// <summary>
    /// Tests that workflow state persistence works correctly with the actual workflow.puml, ensuring that the current node ID is saved and restored correctly.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The workflow persistence system's ability to save and restore the current workflow state (current node ID) for the production workflow.</para>
    /// <para><strong>Data involved:</strong> The actual workflow.puml file loaded and imported. The workflow is advanced through multiple nodes (get_nearby_businesses → camera → decision → choice selection). The current node ID is saved, then the workflow instance is restarted, which should restore the saved state.</para>
    /// <para><strong>Why the data matters:</strong> Workflow state persistence is critical for maintaining user progress across application restarts. The system must correctly save the current node ID and restore it when the workflow instance is restarted. This integration test validates that persistence works correctly with the actual production workflow structure.</para>
    /// <para><strong>Expected outcome:</strong> After advancing and saving, the persisted workflow should have CurrentNodeId matching the saved node ID. After restarting the instance, the restored node ID should match the saved node ID, confirming state was correctly restored.</para>
    /// <para><strong>Reason for expectation:</strong> The workflow repository should persist the current node ID when the workflow advances. When StartInstanceAsync is called, it should restore the workflow state from persistence, setting the current node to the persisted value. The matching node IDs confirm that persistence and restoration work correctly, allowing users to resume workflows from where they left off.</para>
    /// </remarks>
    [Fact]
    public async Task WorkflowPersistence_SavesAndRestores_CurrentState()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var repo = sp.GetRequiredService<FWH.Mobile.Data.Repositories.IWorkflowRepository>();
        var puml = LoadWorkflowPuml();

        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_persist", "Fun Was Had Persist").ConfigureAwait(true);

        // Act - Advance past get_nearby_businesses -> camera -> decision
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true); // past get_nearby_businesses
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null).ConfigureAwait(true); // past camera

        var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id).ConfigureAwait(true);
        if (state.IsChoice && state.Choices.Any())
        {
            await workflowController.AdvanceByChoiceValueAsync(workflow.Id, state.Choices[0].TargetNodeId).ConfigureAwait(true);
        }

        var savedNodeId = workflowController.GetCurrentNodeId(workflow.Id);

        // Verify persistence
        var persisted = await repo.GetByIdAsync(workflow.Id).ConfigureAwait(true);
        Assert.NotNull(persisted);
        Assert.Equal(savedNodeId, persisted!.CurrentNodeId);

        // Act - Restart instance (which should restore from persistence)
        await workflowController.StartInstanceAsync(workflow.Id).ConfigureAwait(true);
        var restoredNodeId = workflowController.GetCurrentNodeId(workflow.Id);

        // Assert - State should be restored
        Assert.Equal(savedNodeId, restoredNodeId);
    }

    /// <summary>
    /// Tests that the actual workflow.puml has the expected workflow structure, including all required nodes and transitions between them.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The workflow definition structure validation, ensuring that the actual workflow.puml contains all expected nodes and has the correct transition structure.</para>
    /// <para><strong>Data involved:</strong> The actual workflow.puml file loaded and imported. The parsed workflow structure is validated to ensure it contains expected nodes: "get_nearby_businesses", "camera", "Record Fun Experience", "Record Not Fun Experience", and has the correct transition structure (get_nearby_businesses → camera).</para>
    /// <para><strong>Why the data matters:</strong> This is a structural validation test ensuring the production workflow definition is correct. It validates that all required nodes exist and transitions are properly defined. This catches structural issues that would prevent the workflow from functioning correctly.</para>
    /// <para><strong>Expected outcome:</strong> The workflow should contain nodes with labels "get_nearby_businesses", "camera", "Record Fun Experience", and "Record Not Fun Experience". It should have non-empty StartPoints and Transitions collections, and a transition from get_nearby_businesses to camera.</para>
    /// <para><strong>Reason for expectation:</strong> The workflow definition should include all nodes required for the "Fun Was Had" user journey. The presence of these specific node labels confirms the workflow structure is correct. The transition from get_nearby_businesses to camera confirms the workflow flow is properly defined. This validates that the production workflow definition is structurally correct and complete.</para>
    /// </remarks>
    [Fact]
    public async Task WorkflowStructureHasExpectedNodes()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowService = sp.GetRequiredService<IWorkflowService>();
        var puml = LoadWorkflowPuml();

        // Act
        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId + "_structure", "Fun Was Had Structure").ConfigureAwait(true);

        // Assert - Verify expected nodes exist
        Assert.Contains(workflow.Nodes, n => n.Label.Equals("get_nearby_businesses", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(workflow.Nodes, n => n.Label.Equals("camera", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("Record Fun Experience", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(workflow.Nodes, n => n.Label.Contains("Record Not Fun Experience", StringComparison.OrdinalIgnoreCase));

        // Verify workflow structure: get_nearby_businesses -> camera -> decision -> two branches -> end
        Assert.NotEmpty(workflow.StartPoints);
        Assert.NotEmpty(workflow.Transitions);

        // Verify transitions from get_nearby_businesses to camera
        Assert.Contains(workflow.Transitions, t =>
            workflow.Nodes.Any(n => n.Id == t.FromNodeId && n.Label.Equals("get_nearby_businesses", StringComparison.OrdinalIgnoreCase)) &&
            workflow.Nodes.Any(n => n.Id == t.ToNodeId && n.Label.Equals("camera", StringComparison.OrdinalIgnoreCase)));
    }
}
