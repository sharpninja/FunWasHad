using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using FWH.Mobile.ViewModels;
using System;

namespace FWH.Mobile.Views;

public partial class LogViewerControl : UserControl
{
    private LogViewerViewModel? _currentViewModel;

    public LogViewerControl()
    {
        InitializeComponent();
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == DataContextProperty)
        {
            // Unsubscribe from previous view model
            if (_currentViewModel != null)
            {
                _currentViewModel.ScrollToEndRequested -= ScrollToEnd;
            }

            // Subscribe to new view model
            if (DataContext is LogViewerViewModel viewModel)
            {
                _currentViewModel = viewModel;
                viewModel.ScrollToEndRequested += ScrollToEnd;
            }
            else
            {
                _currentViewModel = null;
            }
        }
    }

    private void ScrollToEnd()
    {
        if (LogScrollViewer != null)
        {
            // Use Dispatcher to ensure scrolling happens after UI updates are complete
            // Post with Render priority to ensure it happens after layout
            Dispatcher.UIThread.Post(() =>
            {
                LogScrollViewer?.ScrollToEnd();
            }, DispatcherPriority.Render);
        }
    }
}

