using System;
using Avalonia.Controls;
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

    protected override void OnClosed(EventArgs e)
    {
        // Save settings when window closes
        if (DataContext is MainWindowViewModel vm)
        {
            vm.OnClosing();
        }
        
        base.OnClosed(e);
    }
}
