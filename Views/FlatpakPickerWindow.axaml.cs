using Avalonia.Controls;
using DMINLauncher.Models;
using System.Collections.Generic;

namespace DMINLauncher.Views;

public partial class FlatpakPickerWindow : Window
{
    public string? SelectedAppId { get; private set; }

    public FlatpakPickerWindow() { InitializeComponent(); }

    public FlatpakPickerWindow(List<FlatpakApp> apps)
    {
        InitializeComponent();

        this.FindControl<TextBlock>("HeaderText")!.Text = $"Select a Flatpak application ({apps.Count} found):";
        this.FindControl<ListBox>("AppListBox")!.ItemsSource = apps;

        this.FindControl<Button>("AddButton")!.Click += (_, _) =>
        {
            if (this.FindControl<ListBox>("AppListBox")!.SelectedItem is FlatpakApp app)
            {
                SelectedAppId = app.AppId;
                Close();
            }
        };

        this.FindControl<Button>("CancelButton")!.Click += (_, _) => Close();
    }
}
