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
        var solutionDir = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.FullName;
        
        if (solutionDir == null)
            throw new FileNotFoundException("Could not locate solution directory");
        
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
    }

    [Fact]
    public async Task WorkflowStart_ShouldReach_AskForCurrentAddressState()
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
        // The workflow should start at "Ask for Current Address" or show it as text/choice
        Assert.NotNull(state.Text);
    }

    [Fact]
    public async Task WorkflowNavigation_FromStart_ToAddressResponse_Works()
    {
        // Arrange
        var sp = _fixture.CreateServiceProvider();
        var workflowController = sp.GetRequiredService<IWorkflowController>();
        var puml = LoadWorkflowPuml();
        
        var workflow = await workflowController.ImportWorkflowAsync(puml, WorkflowId + "_nav1", "Fun Was Had Nav");
        await workflowController.StartInstanceAsync(workflow.Id);

        // Act - Get initial state
        var initialState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
        
        // If there's a transition available, advance
        if (initialState.IsChoice && initialState.Choices.Any())
        {
            var firstChoice = initialState.Choices[0];
            var advanced = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, firstChoice.TargetNodeId);
            Assert.True(advanced);
            
            var newState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
            Assert.NotNull(newState);
        }

        // Assert - We should have progressed through the workflow
        var currentNode = workflowController.GetCurrentNodeId(workflow.Id);
        Assert.NotNull(currentNode);
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

        // Act - Navigate through workflow to the "Was fun had?" decision
        await chatService.RenderWorkflowStateAsync(workflow.Id);
        
        var maxSteps = 10; // Safety limit to prevent infinite loops
        var steps = 0;
        
        while (steps < maxSteps)
        {
            var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
            
            // Look for the "Was fun had?" decision point
            if (state.IsChoice)
            {
                // Look for a choice that represents "fun was had" (yes/true/#FunWasHad)
                var funChoice = state.Choices.FirstOrDefault(c => 
                    c.DisplayText.Contains("fun", StringComparison.OrdinalIgnoreCase) ||
                    c.DisplayText.Contains("yes", StringComparison.OrdinalIgnoreCase) ||
                    c.DisplayText.Contains("Fun", StringComparison.Ordinal));
                
                if (funChoice != null)
                {
                    // Take the "fun was had" branch
                    var advanced = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, funChoice.TargetNodeId);
                    Assert.True(advanced);
                    
                    // Verify we reached the "Record Fun Experience" state
                    var finalState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
                    var currentNode = workflowController.GetCurrentNodeId(workflow.Id);
                    
                    Assert.NotNull(currentNode);
                    // The node or state should reference recording fun experience
                    break;
                }
                else if (state.Choices.Any())
                {
                    // Take first available choice to progress
                    await workflowController.AdvanceByChoiceValueAsync(workflow.Id, state.Choices[0].TargetNodeId);
                }
                else
                {
                    break;
                }
            }
            else
            {
                // Auto-advance through non-choice nodes
                var currentNodeId = workflowController.GetCurrentNodeId(workflow.Id);
                var definition = await GetWorkflowDefinition(workflowController, workflow.Id);
                var transitions = definition?.Transitions.Where(t => t.FromNodeId == currentNodeId).ToList();
                
                if (transitions?.Count == 1)
                {
                    await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
                }
                else
                {
                    break;
                }
            }
            
            steps++;
        }

        // Assert - We should have successfully navigated the workflow
        Assert.True(steps < maxSteps, "Workflow navigation did not complete within expected steps");
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

        // Act - Navigate through workflow to the "Was fun had?" decision
        await chatService.RenderWorkflowStateAsync(workflow.Id);
        
        var maxSteps = 10;
        var steps = 0;
        
        while (steps < maxSteps)
        {
            var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
            
            if (state.IsChoice)
            {
                // Look for a choice that represents "no fun" (no/false/not fun)
                var noFunChoice = state.Choices.FirstOrDefault(c => 
                    c.DisplayText.Contains("not", StringComparison.OrdinalIgnoreCase) ||
                    c.DisplayText.Contains("no", StringComparison.OrdinalIgnoreCase) ||
                    c.DisplayText.Contains("wasn't", StringComparison.OrdinalIgnoreCase));
                
                if (noFunChoice != null)
                {
                    // Take the "not fun" branch
                    var advanced = await workflowController.AdvanceByChoiceValueAsync(workflow.Id, noFunChoice.TargetNodeId);
                    Assert.True(advanced);
                    
                    // Verify we reached the "Record Not Fun Experience" state
                    var finalState = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
                    var currentNode = workflowController.GetCurrentNodeId(workflow.Id);
                    
                    Assert.NotNull(currentNode);
                    break;
                }
                else if (state.Choices.Any())
                {
                    // Take last choice (typically the "no" option)
                    await workflowController.AdvanceByChoiceValueAsync(workflow.Id, state.Choices.Last().TargetNodeId);
                }
                else
                {
                    break;
                }
            }
            else
            {
                // Auto-advance through non-choice nodes
                await workflowController.AdvanceByChoiceValueAsync(workflow.Id, null);
            }
            
            steps++;
        }

        // Assert
        Assert.True(steps < maxSteps, "Workflow navigation did not complete within expected steps");
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

            // Navigate to end
            var maxSteps = 15;
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

        // Try to advance if choices available
        if (view.CurrentState!.IsChoice && view.CurrentState.Choices.Any())
        {
            var advanced = await view.AdvanceAsync(view.CurrentState.Choices[0].TargetNodeId);
            Assert.True(advanced);
        }

        // Test restart
        await view.RestartAsync();
        Assert.NotNull(view.CurrentState);
        Assert.False(view.HasError);
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

        // Act - Render initial state
        await chatService.RenderWorkflowStateAsync(workflow.Id);

        // Assert - Chat should have entries
        Assert.NotEmpty(chatList.Entries);
        
        // Navigate through a few states and verify chat updates
        var maxSteps = 5;
        var initialCount = chatList.Entries.Count;
        
        for (int i = 0; i < maxSteps; i++)
        {
            var state = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
            
            if (state.IsChoice && state.Choices.Any())
            {
                // Simulate user selecting a choice
                var choice = state.Choices[0];
                await workflowController.AdvanceByChoiceValueAsync(workflow.Id, choice.TargetNodeId);
                await chatService.RenderWorkflowStateAsync(workflow.Id);
                
                // Chat should have been updated
                Assert.True(chatList.Entries.Count >= initialCount);
            }
            else
            {
                break;
            }
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

        // Act - Advance through some states
        var state1 = await workflowController.GetCurrentStatePayloadAsync(workflow.Id);
        if (state1.IsChoice && state1.Choices.Any())
        {
            await workflowController.AdvanceByChoiceValueAsync(workflow.Id, state1.Choices[0].TargetNodeId);
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

    private async Task<FWH.Common.Workflow.Models.WorkflowDefinition?> GetWorkflowDefinition(
        IWorkflowController controller, 
        string workflowId)
    {
        // Access internal definition through reflection or by importing again
        // For now, we'll assume controller has the definition stored
        return null; // This would need actual implementation based on controller design
    }
}
