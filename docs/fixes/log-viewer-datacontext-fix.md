# Log Viewer Fix - DataContext Not Set

## Problem

Log messages were not displaying in the `LogViewerControl` because the `DataContext` was never properly set.

## Root Cause

The `LogViewerViewModel` was being passed from `App.axaml.cs` to `MainWindow` via the `Tag` property, but the code in `MainWindow.axaml.cs` was unable to find the `LogViewerControl` to set its `DataContext`.

### View Hierarchy
```
MainWindow
  └── MainView (Content)
       └── DockPanel
            ├── LogViewerControl (Name="LogViewer")
            ├── ChatInputControl
            └── ChatListControl
```

The original code was trying to find `LogViewerControl` directly on the `MainWindow`:
```csharp
var logViewer = this.FindControl<LogViewerControl>("LogViewer");
```

This failed because `LogViewerControl` is nested inside `MainView`, not directly in `MainWindow`.

## Solution

### 1. Added Name to MainView
Updated `MainWindow.axaml` to give the `MainView` a name so it can be found:
```xml
<views:MainView x:Name="MainView" />
```

### 2. Fixed FindControl Logic
Updated `MainWindow.axaml.cs` to search for the control in the correct location:
```csharp
// LogViewerControl is inside MainView, so we need to find it recursively
var mainView = this.FindControl<MainView>("MainView") ?? this.Content as MainView;
var logViewer = mainView?.FindControl<LogViewerControl>("LogViewer");
if (logViewer != null)
{
    logViewer.DataContext = logViewerVm;
}
```

This code:
1. Finds the `MainView` control (or uses `Content` as fallback)
2. Searches within `MainView` for the `LogViewerControl`
3. Sets the `DataContext` to the `LogViewerViewModel`

## How Logging Works

### Registration (App.axaml.cs)
```csharp
// Create log store and register provider
var logStore = new AvaloniaLogStore(maxEntries: 1000);
services.AddSingleton(logStore);

services.AddLogging(builder =>
{
    builder.AddProvider(new AvaloniaLoggerProvider(logStore));
});

// Register ViewModel
services.AddSingleton<LogViewerViewModel>();
```

### Logging Flow
```
ILogger.LogInformation("Message")
    ↓
AvaloniaLoggerProvider
    ↓
AvaloniaLogger.Log()
    ↓
AvaloniaLogStore.Add()
    ↓
Dispatcher.UIThread.Post() → Add to ObservableCollection
    ↓
LogViewerViewModel.Entries (bound to UI)
    ↓
LogViewerControl displays messages
```

### Thread Safety
The `AvaloniaLogStore` ensures thread safety by:
1. Checking if already on UI thread: `Dispatcher.UIThread.CheckAccess()`
2. If on UI thread: Add directly
3. If on background thread: Post to UI thread via `Dispatcher.UIThread.Post()`

## Testing

After this fix, log messages should now display correctly in the log viewer control at the bottom of the main window.

To test:
1. Run the application
2. Trigger any action that generates logs (e.g., GPS location update, workflow execution)
3. Verify logs appear in the log viewer control
4. Click "Clear" button to verify it clears the logs

## Related Files

- `src/FWH.Mobile/FWH.Mobile/Views/MainWindow.axaml` - Added name to MainView
- `src/FWH.Mobile/FWH.Mobile/Views/MainWindow.axaml.cs` - Fixed FindControl logic
- `src/FWH.Mobile/FWH.Mobile/Logging/AvaloniaLogStore.cs` - Thread-safe log storage
- `src/FWH.Mobile/FWH.Mobile/Logging/AvaloniaLoggerProvider.cs` - Logging provider
- `src/FWH.Mobile/FWH.Mobile/ViewModels/LogViewerViewModel.cs` - ViewModel
- `src/FWH.Mobile/FWH.Mobile/Views/LogViewerControl.axaml` - UI control

## Note About UIThread Tests

The tests in `AvaloniaLoggerProviderTests.cs` and `AvaloniaLogStoreTests.cs` that depended on `Dispatcher.UIThread` were removed because:
1. Avalonia's dispatcher is not initialized in unit test environments
2. Tests would hang waiting for the UI thread
3. The core logging logic can be tested without UI thread dependencies

For integration testing of the logging UI, run the application manually and verify logs display correctly.
