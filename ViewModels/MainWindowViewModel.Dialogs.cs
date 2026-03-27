using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DMINLauncher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DMINLauncher.ViewModels;

public partial class MainWindowViewModel
{
    #region Zoom

    private void ZoomIn()    => ZoomLevel = Math.Min(200, ZoomLevel + 5);
    private void ZoomOut()   => ZoomLevel = Math.Max(50,  ZoomLevel - 5);
    private void ResetZoom() => ZoomLevel = 100;

    #endregion

    #region Exit

    private void ExitApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private async Task ShowExitConfirmation()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;
        await ConfirmExitAsync(topLevel);
    }

    // Returns true if the caller should proceed with closing (user chose Exit or Save and Exit).
    public async Task<bool> ConfirmExitAsync(Window owner)
    {
        var dialog = new Views.ThreeButtonDialogWindow(
            title:         "Exit",
            message:       "Exit the application?",
            buttons:       [
                new Views.DialogButton("Cancel",       "cancel", "#404040"),
                new Views.DialogButton("Exit",         "exit",   "#7A1A1A"),
                new Views.DialogButton("Save and Exit","save",   "#2D5A1B", IsBold: true),
            ],
            defaultResult: "save");
        await dialog.ShowDialog(owner);

        if (dialog.Result == "save")      { SaveDefaultConfig(); return true; }
        if (dialog.Result == "exit")      { return true; }
        return false;
    }

    #endregion

    #region Flatpak

    private async Task AddCustomFlatpak()
    {
        if (!OperatingSystem.IsLinux())
        {
            StatusMessage = "⚠️ Flatpak is only available on Linux"; StatusMessageColor = "Orange"; return;
        }

        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        StatusMessage = "🔍 Scanning for Flatpak applications..."; StatusMessageColor = "Yellow";

        var psi = new ProcessStartInfo
        {
            FileName               = "flatpak",
            Arguments              = "list --app --columns=application,name",
            RedirectStandardOutput = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        var proc = Process.Start(psi);
        if (proc == null) { StatusMessage = "❌ Failed to run flatpak command"; StatusMessageColor = "Red"; return; }

        var output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        if (string.IsNullOrWhiteSpace(output))
        {
            StatusMessage = "ℹ️ No Flatpak applications found"; StatusMessageColor = "Orange"; return;
        }

        var apps = new List<(string appId, string name)>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t', 2);
            apps.Add(parts.Length >= 2 ? (parts[0].Trim(), parts[1].Trim()) : (parts[0].Trim(), parts[0].Trim()));
        }

        if (apps.Count == 0) { StatusMessage = "ℹ️ No Flatpak applications found"; StatusMessageColor = "Orange"; return; }

        var flatpakApps = apps
            .Select(a => new Models.FlatpakApp(a.appId, a.name))
            .ToList();

        var picker = new Views.FlatpakPickerWindow(flatpakApps);
        await picker.ShowDialog(topLevel);

        var selectedId = picker.SelectedAppId;
        if (string.IsNullOrEmpty(selectedId)) { StatusMessage = ""; return; }

        if (!await ShowFlatpakPermissionWarning(topLevel))
        {
            StatusMessage = "❌ Flatpak selection cancelled"; StatusMessageColor = "Orange"; return;
        }

        EngineExecutable   = $"flatpak:{selectedId}";
        SaveDefaultConfig();
        StatusMessage      = $"✅ Engine set to {selectedId} (flatpak)";
        StatusMessageColor = "LimeGreen";
    }

    private async Task<bool> ShowFlatpakPermissionWarning(Window parent)
    {
        var message =
            $"ℹ️  Flatpak Permissions\n\n" +
            $"The Flatpak will be granted temporary access to:\n" +
            $"  •  WADs folder:  {_wadDir}\n" +
            $"  •  Display (X11/Wayland)\n" +
            $"  •  GPU device (DRI)\n" +
            $"  •  Audio (PulseAudio)\n\n" +
            "Permissions are removed when the game closes.\n\n" +
            "Do you want to proceed?";

        var dialog = new Views.ThreeButtonDialogWindow(
            title:         "Flatpak Permissions",
            message:       message,
            buttons:       [
                new Views.DialogButton("Cancel",  "cancel",  "#404040"),
                new Views.DialogButton("Proceed", "proceed", "#7A5A1A", IsBold: true),
            ],
            defaultResult: "proceed");
        await dialog.ShowDialog(parent);
        return dialog.Result == "proceed";
    }

    #endregion

    #region Platform Helpers

    private static Window? GetMainWindow() =>
        Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d
            ? d.MainWindow : null;

    private static async Task<string?> TryOpenFolderPicker(Window parent, string title, string? startPath = null)
    {
        try
        {
            var opts = new FolderPickerOpenOptions { Title = title, AllowMultiple = false };

            if (!string.IsNullOrWhiteSpace(startPath) && Directory.Exists(startPath))
            {
                try { opts.SuggestedStartLocation = await parent.StorageProvider.TryGetFolderFromPathAsync(PathToFileUri(startPath)); }
                catch { }
            }

            var folders = await parent.StorageProvider.OpenFolderPickerAsync(opts);
            return folders.Count > 0 ? folders[0].Path.LocalPath : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TryOpenFolderPicker: {ex.Message}");
            return null;
        }
    }

    private static Uri PathToFileUri(string path)
    {
        var normalized = Path.GetFullPath(path).Replace('\\', '/').TrimStart('/');
        return new Uri("file:///" + normalized);
    }

    private static Button MakeButton(string content, string hexColor, int width = 120,
        bool isCancel = false, bool isDefault = false) =>
        new()
        {
            Content    = content,
            Width      = width,
            Background = new SolidColorBrush(Color.Parse(hexColor)),
            IsCancel   = isCancel,
            IsDefault  = isDefault,
            Margin     = new Avalonia.Thickness(5)
        };

    private static async Task<string?> ShowTextInputDialog(
        Window parent, string title, string prompt, string currentValue, string watermark)
    {
        string? result = null;
        var dialog     = new Window { Title = title, Width = 600, Height = 200, CanResize = false };
        var textBox    = new TextBox { Text = currentValue, Watermark = watermark, Margin = new Avalonia.Thickness(10) };
        var okBtn      = MakeButton("OK",     "#406020");
        var cancelBtn  = MakeButton("Cancel", "#604040", isCancel: true);

        okBtn.Click     += (_, _) => { result = textBox.Text; dialog.Close(); };
        cancelBtn.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin   = new Avalonia.Thickness(10),
            Spacing  = 10,
            Children =
            {
                new TextBlock { Text = prompt,                    FontWeight = FontWeight.Bold },
                new TextBlock { Text = $"Current: {currentValue}", FontSize = 11, Foreground = Brushes.Gray },
                textBox,
                new StackPanel
                {
                    Orientation         = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children            = { cancelBtn, okBtn }
                }
            }
        };

        await dialog.ShowDialog(parent);
        return result;
    }

    #endregion
}
