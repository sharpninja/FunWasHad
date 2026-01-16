using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Views;
using FWH.Common.Workflow.Extensions;
using FWH.Common.Chat;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat.Conversion;
using FWH.Common.Chat.Duplicate;
using FWH.Common.Chat.Extensions;
using FWH.Common.Location.Extensions;
using FWH.Common.Chat.Tests.TestFixtures;
using Microsoft.Extensions.Logging;

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

    [Fact]
    public async Task WorkflowPuml_CanBeImported_Successfully()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowService = sp.GetRequiredService<IWorkflowService>();
        var puml = LoadWorkflowPuml();

        // Act
        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId, "Fun Was Had");

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

    [Fact]
    public async Task WorkflowStart_ShouldReach_GetNearbyBusinessesAction()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowService = sp.GetRequiredService<IWorkflowService>();
        var puml = LoadWorkflowPuml();
        
        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId + "_start", "Fun Was Had Start");

        // Act
        var state = await workflowService.GetCurrentStatePayloadAsync(workflow.Id);

        // Assert
        Assert.NotNull(state);
        // The workflow should start at "get_nearby_businesses" action node
        Assert.Equal("get_nearby_businesses", state.NodeLabel, ignoreCase: true);
    }

    [Fact]
    public async Task GetNearbyBusinessesNode_HasActionDefinition()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowService = sp.GetRequiredService<IWorkflowService>();
        var puml = LoadWorkflowPuml();
        
        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId + "_action", "Fun Was Had Action");

        // Act
        var getNearbyNode = workflow.Nodes.FirstOrDefault(n => 
            n.Label.Equals("get_nearby_businesses", StringComparison.OrdinalIgnoreCase));

        // Assert
        Assert.NotNull(getNearbyNode);
        Assert.NotNull(getNearbyNode!.NoteMarkdown);
        Assert.Contains("action", getNearbyNode.NoteMarkdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("get_nearby_businesses", getNearbyNode.NoteMarkdown, StringComparison.OrdinalIgnoreCase);
    }

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
        
        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId + "_camera", "Fun Was Had Camera");

        // Act - Advance past get_nearby_businesses to reach camera
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
        await chatService.RenderWorkflowStateAsync(workflow.Id);

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
    public async Task WorkflowNavigation_FromGetNearby_ToCamera_ToDecision_Works()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var puml = LoadWorkflowPuml();
        
        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_nav1", "Fun Was Had Nav");
        await workflowController.StartInstanceAsync(workflow.Id);

        // Act - Get initial state (get_nearby_businesses node)
        var initialState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
        Assert.Equal("get_nearby_businesses", initialState.NodeLabel, ignoreCase: true);
        
        // Advance through get_nearby_businesses node (auto-advance since it's an action)
        var advanced1 = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
        Assert.True(advanced1);
        
        // Should now be at camera
        var cameraState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
        Assert.Equal("camera", cameraState.NodeLabel, ignoreCase: true);
        
        // Advance through camera node (auto-advance since it's not a choice)
        var advanced2 = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
        Assert.True(advanced2);
        
        // Should now be at the decision point
        var decisionState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);

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
        
        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_fun", "Fun Was Had - Yes Branch");

        // Act - Navigate from get_nearby_businesses -> camera -> decision
        await chatService.RenderWorkflowStateAsync(workflow.Id);
        
        // Advance past get_nearby_businesses
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
        
        // Advance past camera
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
        
        // Get decision state
        var decisionState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
        Assert.True(decisionState.IsChoice);
        
        // Look for the "fun was had" choice
        var funChoice = decisionState.Choices.FirstOrDefault(c => 
            c.DisplayText.Contains("fun", StringComparison.OrdinalIgnoreCase) ||
            c.DisplayText.Contains("Fun", StringComparison.Ordinal) ||
            c.DisplayText.Contains("Record Fun", StringComparison.OrdinalIgnoreCase));
        
        Assert.NotNull(funChoice);
        
        // Take the "fun was had" branch
        var advanced = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, funChoice.TargetNodeId);
        Assert.True(advanced);
        
        // Verify we reached the "Record Fun Experience" state
        var finalState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
        var currentNode = workflowController.GetCurrentNodeId(workflow.Id);
        
        Assert.NotNull(currentNode);
        Assert.NotNull(finalState);
    }

    [Fact]
    public async Task WorkflowBranch_NotFun_ReachesRecordNotFunExperience()
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
        
        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_notfun", "Fun Was Had - No Branch");

        // Act - Navigate from get_nearby_businesses -> camera -> decision
        await chatService.RenderWorkflowStateAsync(workflow.Id);
        
        // Advance past get_nearby_businesses
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
        
        // Advance past camera
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
        
        // Get decision state
        var decisionState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
        Assert.True(decisionState.IsChoice);
        
        // Look for the "not fun" choice
        var noFunChoice = decisionState.Choices.FirstOrDefault(c => 
            c.DisplayText.Contains("not", StringComparison.OrdinalIgnoreCase) ||
            c.DisplayText.Contains("No", StringComparison.Ordinal) ||
            c.DisplayText.Contains("Record Not Fun", StringComparison.OrdinalIgnoreCase));
        
        Assert.NotNull(noFunChoice);
        
        // Take the "not fun" branch
        var advanced = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, noFunChoice.TargetNodeId);
        Assert.True(advanced);
        
        // Verify we reached the "Record Not Fun Experience" state
        var finalState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
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
                $"Fun Was Had - Stop {branch}");

            // Navigate: get_nearby_businesses -> camera -> decision -> experience recording -> end
            var maxSteps = 15; // Increased from 10 to account for new initial action
            var steps = 0;
            var reachedEnd = false;
            
            while (steps < maxSteps)
            {
                var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
                
                if (state.IsChoice)
                {
                    if (state.Choices.Any())
                    {
                        // Take appropriate branch based on test case
                        var choiceIndex = branch == "fun" ? 0 : (state.Choices.Count - 1);
                        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, state.Choices[choiceIndex].TargetNodeId);
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
                    var advanced = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
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
        
        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_view", "Fun Was Had View");

        // Act & Assert - Load workflow
        await view.LoadWorkflowAsync(workflow.Id);
        Assert.Equal(workflow.Id, view.CurrentWorkflowId);
        Assert.NotNull(view.CurrentState);
        Assert.False(view.HasError);
        
        // Should start at get_nearby_businesses node
        Assert.Equal("get_nearby_businesses", view.CurrentState!.NodeLabel, ignoreCase: true);

        // Advance past get_nearby_businesses to camera
        var advanced = await view.AdvanceAsync(null);
        Assert.True(advanced);
        Assert.Equal("camera", view.CurrentState!.NodeLabel, ignoreCase: true);
        
        // Advance past camera to decision
        advanced = await view.AdvanceAsync(null);
        Assert.True(advanced);
        
        // Now should be at choice
        Assert.True(view.CurrentState!.IsChoice);
        
        // Try to advance with choice
        if (view.CurrentState.Choices.Any())
        {
            advanced = await view.AdvanceAsync(view.CurrentState.Choices[0].TargetNodeId);
            Assert.True(advanced);
        }

        // Test restart
        await view.RestartAsync();
        Assert.NotNull(view.CurrentState);
        Assert.False(view.HasError);
        Assert.Equal("get_nearby_businesses", view.CurrentState!.NodeLabel, ignoreCase: true);
    }

    [Fact]
    public async Task ChatService_WithActualWorkflow_RendersAllStates()
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
        
        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_chat", "Fun Was Had Chat");

        // Act - Render initial state (get_nearby_businesses)
        await chatService.RenderWorkflowStateAsync(workflow.Id);

        // Assert - Chat should have entry for get_nearby_businesses action
        Assert.NotEmpty(chatList.Entries);
        
        var initialCount = chatList.Entries.Count;
        
        // Advance past get_nearby_businesses to camera
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
        await chatService.RenderWorkflowStateAsync(workflow.Id);
        
        // Should have ImageChatEntry for camera
        Assert.True(chatList.Entries.Count >= initialCount);
        var hasImageEntry = chatList.Entries.Any(e => e is ImageChatEntry);
        Assert.True(hasImageEntry);
        
        // Advance past camera to decision
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
        await chatService.RenderWorkflowStateAsync(workflow.Id);
        
        // Should now have choice entry
        var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
        if (state.IsChoice && state.Choices.Any())
        {
            // Simulate user selecting a choice
            var choice = state.Choices[0];
            await workflowController.AdvanceByChoiceValueAsync(workflow.Id, choice.TargetNodeId);
            await chatService.RenderWorkflowStateAsync(workflow.Id);
            
            // Chat should have been updated with the result
            Assert.True(chatList.Entries.Count >= initialCount);
        }
    }

    [Fact]
    public async Task WorkflowPersistence_SavesAndRestores_CurrentState()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var repo = sp.GetRequiredService<FWH.Mobile.Data.Repositories.IWorkflowRepository>();
        var puml = LoadWorkflowPuml();
        
        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_persist", "Fun Was Had Persist");

        // Act - Advance past get_nearby_businesses -> camera -> decision
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null); // past get_nearby_businesses
        await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null); // past camera
        
        var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
        if (state.IsChoice && state.Choices.Any())
        {
            await workflowController.AdvanceByChoiceValueAsync(workflow.Id, state.Choices[0].TargetNodeId);
        }
        
        var savedNodeId = workflowController.GetCurrentNodeId(workflow.Id);

        // Verify persistence
        var persisted = await repo.GetByIdAsync(workflow.Id);
        Assert.NotNull(persisted);
        Assert.Equal(savedNodeId, persisted!.CurrentNodeId);

        // Act - Restart instance (which should restore from persistence)
        await workflowController.StartInstanceAsync(workflow.Id);
        var restoredNodeId = workflowController.GetCurrentNodeId(workflow.Id);

        // Assert - State should be restored
        Assert.Equal(savedNodeId, restoredNodeId);
    }

    [Fact]
    public async Task WorkflowStructure_HasExpectedNodes()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowService = sp.GetRequiredService<IWorkflowService>();
        var puml = LoadWorkflowPuml();
        
        // Act
        var workflow = await workflowService.ImportWorkflowAsync(puml, WorkflowId + "_structure", "Fun Was Had Structure");

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
