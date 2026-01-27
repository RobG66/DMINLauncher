using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DMINLauncher.ViewModels;

namespace DMINLauncher.Views;

public partial class MainWindow : Window
{
    private WindowState _previousState = WindowState.Normal;

    public MainWindow()
    {
        InitializeComponent();
        
        // Track window state changes to handle zoom when restoring from maximized
        PropertyChanged += MainWindow_PropertyChanged;
        
        // Wire up application exit event for guaranteed cleanup
        if (Avalonia.Application.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
        {
            lifetime.Exit += OnApplicationExit;
        }
    }

    private void MainWindow_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            var newState = (WindowState)(e.NewValue ?? WindowState.Normal);
            
            // When restoring from Maximized to Normal, reapply the zoom-based window size
            if (_previousState == WindowState.Maximized && newState == WindowState.Normal)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    // Force the window to use the current zoom-based dimensions
                    Width = vm.WindowWidth;
                    Height = vm.WindowHeight;
                }
            }
            
            _previousState = newState;
        }
    }

    private void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        // Perform cleanup when application exits (guaranteed to run on graceful shutdown)
        if (DataContext is MainWindowViewModel vm)
        {
            vm.OnClosing();
        }
        
        // Unsubscribe from events
        if (Avalonia.Application.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
        {
            lifetime.Exit -= OnApplicationExit;
        }
        PropertyChanged -= MainWindow_PropertyChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        // OnClosed is called when window closes, but Exit event handles cleanup
        // Just clean up event handlers here
        PropertyChanged -= MainWindow_PropertyChanged;
        
        base.OnClosed(e);
    }
}
