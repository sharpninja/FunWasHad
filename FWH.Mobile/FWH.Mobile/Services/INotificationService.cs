namespace FWH.Mobile.Services;

/// <summary>
/// Service for displaying notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows an error notification to the user.
    /// </summary>
    /// <param name="message">The error message to display</param>
    /// <param name="title">Optional title for the notification</param>
    void ShowError(string message, string? title = null);

    /// <summary>
    /// Shows a success notification to the user.
    /// </summary>
    /// <param name="message">The success message to display</param>
    /// <param name="title">Optional title for the notification</param>
    void ShowSuccess(string message, string? title = null);

    /// <summary>
    /// Shows an informational notification to the user.
    /// </summary>
    /// <param name="message">The information message to display</param>
    /// <param name="title">Optional title for the notification</param>
    void ShowInfo(string message, string? title = null);

    /// <summary>
    /// Shows a warning notification to the user.
    /// </summary>
    /// <param name="message">The warning message to display</param>
    /// <param name="title">Optional title for the notification</param>
    void ShowWarning(string message, string? title = null);
}
