using FWH.Common.Chat.Conversion;
using FWH.Common.Chat.Duplicate;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Chat;

/// <summary>
/// Orchestrates chat operations and workflow integration.
/// Single Responsibility: Coordinate chat UI and workflow interactions.
/// </summary>
public class ChatService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ChatViewModel _chatViewModel;
    private readonly ILogger<ChatService> _logger;
    private readonly IWorkflowService? _workflowService;
    private readonly IWorkflowToChatConverter _converter;
    private readonly IChatDuplicateDetector _duplicateDetector;

    private string? _currentWorkflowId;
    private string? _currentUserId;
    private string? _currentTenantId;
    private string? _currentCorrelationId;

    public ChatService(
        IServiceProvider serviceProvider,
        IWorkflowToChatConverter converter,
        IChatDuplicateDetector duplicateDetector,
        ILogger<ChatService>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _duplicateDetector = duplicateDetector ?? throw new ArgumentNullException(nameof(duplicateDetector));
        _chatViewModel = serviceProvider.GetRequiredService<ChatViewModel>();
        _logger = FWH.Common.Workflow.Logging.LoggerHelpers.ResolveLogger<ChatService>(serviceProvider, logger);
        _workflowService = serviceProvider.GetService<IWorkflowService>();
    }

    public async Task StartAsync()
    {
        _chatViewModel.ChatInput.ChoiceSubmitted += OnChoiceSubmitted;
        _chatViewModel.ChatInput.TextSubmitted += OnTextSubmitted;
        _chatViewModel.ChatInput.ImageCaptured += OnImageCaptured;

        var chat = _chatViewModel.ChatList;
        chat.Reset();

        // Add initial welcome message
        chat.AddEntry(new TextChatEntry(
            FWH.Common.Chat.ViewModels.ChatAuthors.Bot,
            "Welcome! Let's capture your fun experiences."));

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Render the current workflow state to the chat and wire choice selections to advance the workflow.
    /// For non-choice states, waits for user text input before advancing.
    /// Optional userId, tenantId and correlationId allow callers to supply contextual fields to be included in logging scopes.
    /// </summary>
    public async Task RenderWorkflowStateAsync(string workflowId, string? userId = null, string? tenantId = null, string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(workflowId)) throw new ArgumentNullException(nameof(workflowId));

        if (_workflowService == null)
        {
            _logger.LogWarning("WorkflowService not registered; cannot render workflow state for {WorkflowId}", workflowId);
            return;
        }

        correlationId ??= Guid.NewGuid().ToString();

        // Store current workflow context for text submission handler
        _currentWorkflowId = workflowId;
        _currentUserId = userId;
        _currentTenantId = tenantId;
        _currentCorrelationId = correlationId;

        _logger.LogDebug("RenderWorkflowStateAsync START - WorkflowId={WorkflowId} CorrelationId={CorrelationId}", workflowId, correlationId);

        var scopeDict = new System.Collections.Generic.Dictionary<string, object> { ["WorkflowId"] = workflowId, ["Operation"] = "RenderState", ["CorrelationId"] = correlationId };
        if (!string.IsNullOrWhiteSpace(userId)) scopeDict["UserId"] = userId;
        if (!string.IsNullOrWhiteSpace(tenantId)) scopeDict["TenantId"] = tenantId;

        using var scope = _logger.BeginScope(scopeDict);

        WorkflowStatePayload payload;
        try
        {
            payload = await _workflowService.GetCurrentStatePayloadAsync(workflowId).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Unknown workflow", StringComparison.Ordinal))
        {
            _logger.LogWarning(ex, "Workflow {WorkflowId} not found", workflowId);
            // Add error message to chat instead of crashing
            _chatViewModel.ChatList.AddEntry(new TextChatEntry(
                FWH.Common.Chat.ViewModels.ChatAuthors.Bot,
                $"Sorry, I couldn't find that workflow."));
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow state for {WorkflowId}", workflowId);
            return;
        }

        var entry = _converter.ConvertToEntry(payload, workflowId);

        _logger.LogDebug("RenderWorkflowStateAsync - Built entry for WorkflowId={WorkflowId} IsChoice={IsChoice} PayloadType={PayloadType}",
            workflowId, payload.IsChoice, entry.Payload.PayloadType);

        // If entry is a choice, subscribe to ChatInput.ChoiceSubmitted once so the selection is handled exactly once.
        if (entry is ChoiceChatEntry)
        {
            EventHandler<ChoicesItem?>? inputHandler = null;
            inputHandler = async (s, selected) =>
            {
                _logger.LogDebug("Selection handler ENTER - WorkflowId={WorkflowId} SelectedValue={Selected}", workflowId, selected?.ChoiceValue ?? "(null)");

                // Unsubscribe immediately to handle a single selection for this rendered state
                _chatViewModel.ChatInput.ChoiceSubmitted -= inputHandler!;

                var advDict = new System.Collections.Generic.Dictionary<string, object> { ["WorkflowId"] = workflowId, ["ChoiceValue"] = selected?.ChoiceValue ?? "null", ["CorrelationId"] = correlationId, ["Operation"] = "AdvanceByChoice" };
                if (!string.IsNullOrWhiteSpace(userId)) advDict["UserId"] = userId;
                if (!string.IsNullOrWhiteSpace(tenantId)) advDict["TenantId"] = tenantId;

                using var advScope = _logger.BeginScope(advDict);
                try
                {
                    var chosenValue = selected?.ChoiceValue ?? selected?.DisplayOrder as object;
                    var advanced = await _workflowService.AdvanceByChoiceValueAsync(workflowId, chosenValue).ConfigureAwait(false);
                    if (!advanced)
                    {
                        _logger.LogWarning("AdvanceByChoiceValueAsync returned false for workflow {WorkflowId} with value {Value}", workflowId, chosenValue);
                        return;
                    }

                    _logger.LogDebug("Selection handler ADVANCED - WorkflowId={WorkflowId} ChosenValue={Value}", workflowId, chosenValue);

                    // after advancing, render the new state
                    await RenderWorkflowStateAsync(workflowId, userId, tenantId, correlationId).ConfigureAwait(false);

                    // Diagnostic: emit debug-scoped chat list contents
                    try
                    {
                        var list = _chatViewModel.ChatList;
                        _logger.LogDebug("After render EntriesCount={Count}", list.Entries.Count);
                        for (int i = 0; i < list.Entries.Count; i++)
                        {
                            var e = list.Entries[i];
                            _logger.LogDebug("Entry[{Index}] Type={Type} PayloadType={PayloadType}", i, e.GetType().Name, e.Payload.PayloadType);
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error advancing workflow {WorkflowId}", workflowId);
                }
                finally
                {
                    _logger.LogDebug("Selection handler EXIT - WorkflowId={WorkflowId}", workflowId);
                }
            };

            // Attach handler to ChatInput.ChoiceSubmitted which is raised by ChatInputViewModel when a choice is selected.
            _chatViewModel.ChatInput.ChoiceSubmitted += inputHandler;
        }
        else if (entry.Payload.PayloadType == PayloadTypes.Image)
        {
            // For image/camera nodes, the workflow will auto-advance when image is captured
            _logger.LogDebug("Image/camera node rendered - waiting for image capture to advance workflow");
        }
        else
        {
            // For non-choice, non-image states (TextChatEntry), the workflow waits for user text input
            // The OnTextSubmitted handler will advance the workflow when user responds
            _logger.LogDebug("Non-choice state rendered - waiting for user text input to advance workflow");
        }

        // Append entry to chat list using duplicate detector
        try
        {
            var chatList = _chatViewModel.ChatList;
            var last = chatList.Current;

            if (!_duplicateDetector.IsDuplicate(entry, last))
            {
                chatList.AddEntry(entry);
                _logger.LogDebug("Appended entry to ChatList - WorkflowId={WorkflowId} CurrentCount={Count}", workflowId, chatList.Entries.Count);
            }
            else
            {
                _logger.LogDebug("Skipped adding duplicate entry for WorkflowId={WorkflowId}", workflowId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add chat entry for workflow {WorkflowId}", workflowId);
        }
        _logger.LogDebug("RenderWorkflowStateAsync END - WorkflowId={WorkflowId} CorrelationId={CorrelationId}", workflowId, correlationId);
    }

    /// <summary>
    /// Create chat entries for workflow nodes that include note markdown.
    /// </summary>
    public void PopulateFromWorkflow(WorkflowDefinition def)
    {
        ArgumentNullException.ThrowIfNull(def);

        var chat = _chatViewModel.ChatList;

        foreach (var node in def.Nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.NoteMarkdown))
            {
                // Normalize whitespace
                var text = NormalizeNoteMarkdown(node.NoteMarkdown);
                chat.AddEntry(new TextChatEntry(FWH.Common.Chat.ViewModels.ChatAuthors.Bot, text));
            }
        }
    }

    private static string NormalizeNoteMarkdown(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            var l = lines[i].Trim();
            if (l.StartsWith('|')) l = l[1..].TrimStart();
            if (l.StartsWith('"') && l.EndsWith('"')) l = l[1..^1];
            lines[i] = l;
        }
        return string.Join('\n', lines).Trim();
    }

    private async void OnTextSubmitted(object? sender, string text)
    {
        if (string.IsNullOrWhiteSpace(_currentWorkflowId))
        {
            _logger.LogDebug("OnTextSubmitted - no active workflow");
            return;
        }

        if (_workflowService == null)
        {
            _logger.LogWarning("OnTextSubmitted - WorkflowService not available");
            return;
        }

        _logger.LogDebug("OnTextSubmitted - Text='{Text}' WorkflowId={WorkflowId}", text, _currentWorkflowId);

        try
        {
            // Add user's text response to chat
            _chatViewModel.ChatList.AddEntry(new TextChatEntry(
                FWH.Common.Chat.ViewModels.ChatAuthors.User,
                text));

            // Get current state to check if we can advance
            var currentState = await _workflowService.GetCurrentStatePayloadAsync(_currentWorkflowId).ConfigureAwait(false);

            if (!currentState.IsChoice)
            {
                // For non-choice nodes, try to auto-advance with null (single path)
                _logger.LogDebug("Attempting to advance non-choice workflow state");
                var advanced = await _workflowService.AdvanceByChoiceValueAsync(_currentWorkflowId, null).ConfigureAwait(false);

                if (advanced)
                {
                    _logger.LogDebug("Workflow advanced successfully after user text input");
                    // Render the next state
                    await RenderWorkflowStateAsync(
                        _currentWorkflowId,
                        _currentUserId,
                        _currentTenantId,
                        _currentCorrelationId).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogWarning("Failed to advance workflow after user text input");
                }
            }
            else
            {
                _logger.LogDebug("Current state is a choice - user should select from options, not submit text");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling text submission for workflow {WorkflowId}", _currentWorkflowId);
        }
    }

    private async void OnImageCaptured(object? sender, byte[] imageBytes)
    {
        if (string.IsNullOrWhiteSpace(_currentWorkflowId))
        {
            _logger.LogDebug("OnImageCaptured - no active workflow");
            return;
        }

        if (_workflowService == null)
        {
            _logger.LogWarning("OnImageCaptured - WorkflowService not available");
            return;
        }

        _logger.LogDebug("OnImageCaptured - ImageSize={Size} WorkflowId={WorkflowId}", imageBytes.Length, _currentWorkflowId);

        try
        {
            // Image was captured, advance the workflow
            // Camera nodes are treated as non-choice nodes that auto-advance
            _logger.LogDebug("Attempting to advance workflow after image capture");
            var advanced = await _workflowService.AdvanceByChoiceValueAsync(_currentWorkflowId, null).ConfigureAwait(false);

            if (advanced)
            {
                _logger.LogDebug("Workflow advanced successfully after image capture");
                // Render the next state
                await RenderWorkflowStateAsync(
                    _currentWorkflowId,
                    _currentUserId,
                    _currentTenantId,
                    _currentCorrelationId).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("Failed to advance workflow after image capture");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling image capture for workflow {WorkflowId}", _currentWorkflowId);
        }
    }

    private void OnChoiceSubmitted(object? sender, FWH.Common.Chat.ViewModels.ChoicesItem? e)
    {
        // This is now handled inline in RenderWorkflowStateAsync for better context
        _logger.LogDebug("OnChoiceSubmitted event fired (handled inline in RenderWorkflowStateAsync)");
    }
}
