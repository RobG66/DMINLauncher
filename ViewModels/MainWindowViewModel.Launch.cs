using DMINLauncher.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMINLauncher.ViewModels;

public partial class MainWindowViewModel
{
    #region Flatpak Helpers

    private static bool IsFlatpakPath(string path, out string appId)
    {
        if (!string.IsNullOrEmpty(path) && path.StartsWith("flatpak:", StringComparison.OrdinalIgnoreCase))
        {
            appId = path["flatpak:".Length..].Trim();
            return !string.IsNullOrEmpty(appId);
        }
        appId = "";
        return false;
    }

    private void GrantFlatpakPermissions(string appId, string wadPath)
    {
        if (string.IsNullOrEmpty(wadPath) || !Directory.Exists(wadPath)) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = "flatpak",
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
            psi.ArgumentList.Add("override");
            psi.ArgumentList.Add("--user");
            psi.ArgumentList.Add($"--filesystem={wadPath}:ro");
            psi.ArgumentList.Add(appId);
            Process.Start(psi)?.WaitForExit();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GrantFlatpakPermissions: {ex.Message}");
        }
    }

    #endregion

    #region Argument Builder

    private List<string> BuildArgs(bool useFullPaths)
    {
        var args = new List<string>();

        if (SelectedBaseGame != null)
        {
            args.Add("-iwad");
            args.Add(useFullPaths ? SelectedBaseGame.FullPath : SelectedBaseGame.RelativePath);
        }

        if (SelectedModFiles.Any())
        {
            args.Add("-file");
            foreach (var mod in SelectedModFiles)
                args.Add(useFullPaths ? mod.FullPath : mod.RelativePath);
        }

        args.Add("-skill");
        args.Add(useFullPaths
            ? ((int)SelectedDifficulty).ToString()
            : ((int)SelectedDifficulty + 1).ToString());

        AppendMapArgs(args, SelectedMapName);
        AppendDmFlagArgs(args, useFullPaths);
        AppendNetworkArgs(args);

        if (ShowHexenClassSelection)
        {
            args.Add("+playerclass");
            args.Add(HexenClass.ToString().ToLower());
        }

        AppendSwitchArgs(args);
        return args;
    }

    private static void AppendMapArgs(List<string> args, string mapName)
    {
        if (string.IsNullOrEmpty(mapName)) return;

        if (mapName.StartsWith("E") && mapName.Length == 4 &&
            char.IsDigit(mapName[1]) && char.IsDigit(mapName[3]))
        {
            args.Add("-warp");
            args.Add(mapName[1].ToString());
            args.Add(mapName[3].ToString());
        }
        else if (mapName.StartsWith("MAP") && mapName.Length >= 5 &&
                 int.TryParse(mapName[3..], out var mapNum))
        {
            args.Add("-warp");
            args.Add(mapNum.ToString());
        }
    }

    private void AppendDmFlagArgs(List<string> args, bool useZDoomStyle)
    {
        var f1 = DmFlags1List.Where(f => f.IsChecked).Sum(f => f.BitValue);
        var f2 = DmFlags2List.Where(f => f.IsChecked).Sum(f => f.BitValue);
        var f3 = DmFlags3List.Where(f => f.IsChecked).Sum(f => f.BitValue);

        if (useZDoomStyle)
        {
            if (f1 != 0) { args.Add("+DMFLAGS");  args.Add(f1.ToString()); }
            if (f2 != 0) { args.Add("+dmflags2"); args.Add(f2.ToString()); }
            if (f3 != 0) { args.Add("+dmflags3"); args.Add(f3.ToString()); }
        }
        else
        {
            if (f1 != 0) { args.Add("+set"); args.Add("dmflags");  args.Add(f1.ToString()); }
            if (f2 != 0) { args.Add("+set"); args.Add("dmflags2"); args.Add(f2.ToString()); }
            if (f3 != 0) { args.Add("+set"); args.Add("dmflags3"); args.Add(f3.ToString()); }
        }
    }

    private void AppendNetworkArgs(List<string> args)
    {
        if (GameType == GameType.SinglePlayer) return;

        if (GameType == GameType.Deathmatch)
            args.Add("-deathmatch");

        if (NetworkMode == NetworkMode.HostLAN || NetworkMode == NetworkMode.HostInternet)
        {
            args.Add("-host");
            args.Add(PlayerCount.ToString());
        }
    }

    private void AppendSwitchArgs(List<string> args)
    {
        if (AvgSwitch)        args.Add("-avg");
        if (FastSwitch)       args.Add("-fast");
        if (NoMonstersSwitch) args.Add("-nomonsters");
        if (RespawnSwitch)    args.Add("-respawn");
        if (TimerSwitch) { args.Add("-timer"); args.Add(TimerMinutes.ToString()); }
        if (TurboSwitch) { args.Add("-turbo"); args.Add(TurboSpeed.ToString());   }
    }

    #endregion

    #region Launch

    private void LaunchGame()
    {
        if (SelectedBaseGame == null)
        {
            StatusMessage = "❌ Please select a base game (IWAD) first"; StatusMessageColor = "Red"; return;
        }

        if (!File.Exists(SelectedBaseGame.FullPath))
        {
            StatusMessage      = $"❌ IWAD file not found:\n{SelectedBaseGame.FullPath}\n\nData directory: {_wadDir}";
            StatusMessageColor = "Red";
            return;
        }

        IsLaunching = true;
        Task.Run(async () =>
        {
            await Task.Delay(10_000);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => IsLaunching = false);
        });

        StatusMessage      = "🎮 Preparing to launch Doom...";
        StatusMessageColor = "Yellow";

        var engineCmd  = EngineExecutable;
        var engineArgs = BuildArgs(useFullPaths: true);

        if (string.IsNullOrEmpty(engineCmd))
        {
            StatusMessage = "❌ Please select an engine executable first"; StatusMessageColor = "Red"; return;
        }

        var psi = new ProcessStartInfo
        {
            UseShellExecute        = false,
            RedirectStandardOutput = false,
            RedirectStandardError  = false,
            CreateNoWindow         = false
        };

        if (IsFlatpakPath(engineCmd, out var flatpakId))
        {
            GrantFlatpakPermissions(flatpakId, _wadDir);
            psi.FileName = "flatpak";
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add(flatpakId);
            foreach (var a in engineArgs) psi.ArgumentList.Add(a);
        }
        else
        {
            if (!File.Exists(engineCmd))
            {
                StatusMessage = $"❌ Engine not found: {engineCmd}"; StatusMessageColor = "Red"; return;
            }

            psi.FileName = engineCmd;
            var dir = Path.GetDirectoryName(engineCmd);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                psi.WorkingDirectory = dir;

            foreach (var a in engineArgs) psi.ArgumentList.Add(a);
        }

        try
        {
            var process = Process.Start(psi);
            if (process == null)
            {
                StatusMessage = "❌ Failed to start Doom engine process"; StatusMessageColor = "Red"; return;
            }

            StatusMessage      = $"✅ Doom launched! PID: {process.Id}";
            StatusMessageColor = "LimeGreen";

            Task.Run(async () =>
            {
                await Task.Delay(2000);
                try
                {
                    if (!process.HasExited) return;
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessage      = $"❌ Doom engine exited immediately (code: {process.ExitCode})";
                        StatusMessageColor = "Red";
                    });
                }
                catch { }
            });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            StatusMessage      = $"❌ Engine not found: {engineCmd}\n{ex.Message}";
            StatusMessageColor = "Red";
        }
        catch (Exception ex)
        {
            StatusMessage      = $"❌ Launch error: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    #endregion

    #region Launch Summary

    private async Task ShowLaunchSummary()
    {
        if (SelectedBaseGame == null)
        {
            StatusMessage = "❌ Please select a base game (IWAD) first"; StatusMessageColor = "Red"; return;
        }

        var args      = BuildArgs(useFullPaths: true);
        var engineCmd = EngineExecutable;
        var sb        = new StringBuilder();

        sb.AppendLine("🎮 LAUNCH CONFIGURATION SUMMARY");
        sb.AppendLine(new string('═', 60));
        sb.AppendLine();
        sb.AppendLine("📁 BASE GAME:");
        sb.AppendLine($"   {SelectedBaseGame.RelativePath}");
        sb.AppendLine();

        if (SelectedModFiles.Any())
        {
            sb.AppendLine("📦 MODS (Load Order):");
            foreach (var mod in SelectedModFiles)
                sb.AppendLine($"   {mod.LoadOrder + 1}. {mod.RelativePath}");
            sb.AppendLine();
        }

        sb.AppendLine("⚙️ GAME SETTINGS:");
        sb.AppendLine($"   Engine: {Path.GetFileName(engineCmd)}");
        sb.AppendLine($"   Difficulty: {SelectedDifficulty}");
        sb.AppendLine($"   Starting Map: {SelectedMapName}");
        sb.AppendLine($"   Game Type: {GameType}");

        if (GameType != GameType.SinglePlayer)
        {
            sb.AppendLine($"   Network Mode: {NetworkMode}");
            sb.AppendLine($"   Players: {PlayerCount}");
        }
        if (ShowHexenClassSelection)
            sb.AppendLine($"   Hexen Class: {HexenClass}");
        sb.AppendLine();

        var switches = new List<string>();
        if (AvgSwitch)        switches.Add("AVG (20 min)");
        if (FastSwitch)       switches.Add("Fast Monsters");
        if (NoMonstersSwitch) switches.Add("No Monsters");
        if (RespawnSwitch)    switches.Add("Respawn");
        if (TimerSwitch)      switches.Add($"Timer ({TimerMinutes} min)");
        if (TurboSwitch)      switches.Add($"Turbo ({TurboSpeed}%)");

        if (switches.Any())
        {
            sb.AppendLine("🚀 RUN SWITCHES:");
            foreach (var s in switches) sb.AppendLine($"   • {s}");
            sb.AppendLine();
        }

        var f1 = DmFlags1List.Where(f => f.IsChecked).Sum(f => f.BitValue);
        var f2 = DmFlags2List.Where(f => f.IsChecked).Sum(f => f.BitValue);
        var f3 = DmFlags3List.Where(f => f.IsChecked).Sum(f => f.BitValue);

        if (f1 != 0 || f2 != 0 || f3 != 0)
        {
            sb.AppendLine("🏴 DMFLAGS:");
            if (f1 != 0) sb.AppendLine($"   • DMFLAGS: {f1}");
            if (f2 != 0) sb.AppendLine($"   • DMFLAGS2: {f2}");
            if (f3 != 0) sb.AppendLine($"   • DMFLAGS3: {f3}");
            sb.AppendLine();
        }

        sb.AppendLine("💻 COMMAND LINE:");
        sb.AppendLine(new string('─', 60));
        WrapLine(sb, $"{engineCmd} {string.Join(" ", args)}", 80);

        try
        {
            var topLevel = GetMainWindow();
            if (topLevel == null) return;

            var vm     = new LaunchSummaryViewModel(sb.ToString());
            var dialog = new Views.LaunchSummaryWindow { DataContext = vm };
            using var sub = vm.CloseCommand.Subscribe(_ => dialog.Close());
            await dialog.ShowDialog(topLevel);
        }
        catch (Exception ex)
        {
            StatusMessage      = $"❌ Error showing summary: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    private static void WrapLine(StringBuilder sb, string line, int width)
    {
        if (line.Length <= width) { sb.AppendLine($"   {line}"); return; }

        var current = "";
        foreach (var word in line.Split(' '))
        {
            if (current.Length + word.Length + 1 > width)
            {
                sb.AppendLine($"   {current}");
                current = word;
            }
            else
            {
                current += (current.Length > 0 ? " " : "") + word;
            }
        }
        if (current.Length > 0) sb.AppendLine($"   {current}");
    }

    #endregion

    #region Batocera Config

    private async Task SaveGZDoomConfig()
    {
        try
        {
            var topLevel = GetMainWindow();
            if (topLevel == null) return;

            var startDir = !string.IsNullOrEmpty(_wadDir) && Directory.Exists(_wadDir)
                ? _wadDir
                : (IsBatocera ? "/userdata/roms/gzdoom" : null);
            Avalonia.Platform.Storage.IStorageFolder? startLocation = null;
            if (!string.IsNullOrEmpty(startDir))
            {
                try { startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(PathToFileUri(startDir)); }
                catch { }
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title                  = "Save Batocera GZDoom Configuration",
                SuggestedFileName      = "My Game.gzdoom",
                DefaultExtension       = "gzdoom",
                ShowOverwritePrompt    = true,
                SuggestedStartLocation = startLocation,
                FileTypeChoices        = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Batocera GZDoom Config")
                    {
                        Patterns = new[] { "*.gzdoom" }
                    }
                }
            });

            if (file == null) return;

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(string.Join(" ", BuildArgs(useFullPaths: false)));

            StatusMessage      = $"✅ Batocera config saved: {file.Name}";
            StatusMessageColor = "LimeGreen";
        }
        catch (Exception ex)
        {
            StatusMessage      = $"❌ Save error: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    private async Task LoadGZDoomConfig()
    {
        try
        {
            var topLevel = GetMainWindow();
            if (topLevel == null) return;

            var openOptions = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title          = "Load Batocera GZDoom Configuration",
                AllowMultiple  = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Batocera GZDoom Config") { Patterns = new[] { "*.gzdoom" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")              { Patterns = new[] { "*.*" }      }
                }
            };

            var startDir = !string.IsNullOrEmpty(_wadDir) && Directory.Exists(_wadDir)
                ? _wadDir
                : (IsBatocera ? "/userdata/roms/gzdoom" : null);
            if (!string.IsNullOrEmpty(startDir))
            {
                try { openOptions.SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(PathToFileUri(startDir)); }
                catch { }
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(openOptions);
            if (files.Count == 0) return;

            var content = await File.ReadAllTextAsync(files[0].Path.LocalPath);
            ApplyGZDoomArgs(content.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (!StatusMessage.StartsWith("⚠️"))
            {
                StatusMessage      = $"✅ GZDoom config loaded: {files[0].Name}";
                StatusMessageColor = "LimeGreen";
            }
        }
        catch (Exception ex)
        {
            StatusMessage      = $"❌ Load error: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    private void ApplyGZDoomArgs(string[] tokens)
    {
        // Reset optional settings before applying
        AvgSwitch = FastSwitch = NoMonstersSwitch = RespawnSwitch = TimerSwitch = TurboSwitch = false;
        TimerMinutes = 20;
        TurboSpeed   = 100;
        GameType     = GameType.SinglePlayer;
        NetworkMode  = NetworkMode.None;
        foreach (var f in DmFlags1List) f.IsChecked = false;
        foreach (var f in DmFlags2List) f.IsChecked = false;
        foreach (var f in DmFlags3List) f.IsChecked = false;

        // Return current mods to the available pool
        foreach (var mod in SelectedModFiles.ToList())
        {
            mod.LoadOrder = 0;
            int ins = 0;
            while (ins < ModFiles.Count &&
                   string.Compare(ModFiles[ins].RelativePath, mod.RelativePath, StringComparison.OrdinalIgnoreCase) < 0)
                ins++;
            ModFiles.Insert(ins, mod);
        }
        SelectedModFiles.Clear();

        var warnings = new List<string>();
        for (int i = 0; i < tokens.Length; i++)
        {
            var t = tokens[i].ToLowerInvariant();

            if (t == "-iwad" && i + 1 < tokens.Length)
            {
                var rel = tokens[++i].Replace('\\', '/');
                var wad = WadFiles.FirstOrDefault(w => w.RelativePath.Equals(rel, StringComparison.OrdinalIgnoreCase));
                if (wad != null) SelectedBaseGame = wad;
                else warnings.Add($"IWAD not found: {rel}");
            }
            else if (t == "-file")
            {
                while (i + 1 < tokens.Length && !tokens[i + 1].StartsWith('-') && !tokens[i + 1].StartsWith('+'))
                {
                    var rel = tokens[++i].Replace('\\', '/');
                    var mod = ModFiles.FirstOrDefault(m => m.RelativePath.Equals(rel, StringComparison.OrdinalIgnoreCase));
                    if (mod != null) { ModFiles.Remove(mod); mod.LoadOrder = SelectedModFiles.Count; SelectedModFiles.Add(mod); }
                    else warnings.Add($"Mod not found: {rel}");
                }
            }
            else if (t == "-skill" && i + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[++i], out var skillVal))
                    SelectedDifficulty = (Difficulty)Math.Clamp(skillVal - 1, 1, 5);
            }
            else if (t == "-warp" && i + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[i + 1], out var w1))
                {
                    i++;
                    if (i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out var w2))
                    {
                        SelectedMapName = $"E{w1}M{w2}";
                        i++;
                    }
                    else
                    {
                        SelectedMapName = $"MAP{w1:D2}";
                    }
                }
            }
            else if (t == "-deathmatch") { GameType = GameType.Deathmatch; }
            else if (t == "-host" && i + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[++i], out var cnt)) { PlayerCount = cnt; NetworkMode = NetworkMode.HostLAN; }
            }
            else if (t == "-avg")        { AvgSwitch        = true; }
            else if (t == "-fast")       { FastSwitch       = true; }
            else if (t == "-nomonsters") { NoMonstersSwitch = true; }
            else if (t == "-respawn")    { RespawnSwitch    = true; }
            else if (t == "-timer" && i + 1 < tokens.Length)
            {
                TimerSwitch = true;
                if (int.TryParse(tokens[++i], out var mins)) TimerMinutes = mins;
            }
            else if (t == "-turbo" && i + 1 < tokens.Length)
            {
                TurboSwitch = true;
                if (int.TryParse(tokens[++i], out var spd)) TurboSpeed = spd;
            }
            else if (t == "+set" && i + 2 < tokens.Length)
            {
                if (int.TryParse(tokens[i + 2], out var sv)) ApplyDmFlags(tokens[i + 1], sv);
                i += 2;
            }
            else if (t == "+dmflags" && i + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[++i], out var fv)) ApplyDmFlags("dmflags", fv);
            }
            else if (t == "+dmflags2" && i + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[++i], out var fv)) ApplyDmFlags("dmflags2", fv);
            }
            else if (t == "+dmflags3" && i + 1 < tokens.Length)
            {
                if (int.TryParse(tokens[++i], out var fv)) ApplyDmFlags("dmflags3", fv);
            }
            else if (t == "+playerclass" && i + 1 < tokens.Length)
            {
                if (Enum.TryParse<HexenClass>(tokens[++i], true, out var hcVal)) HexenClass = hcVal;
            }
        }

        if (warnings.Any())
        {
            StatusMessage      = $"⚠️ Load warnings:\n{string.Join("\n", warnings)}";
            StatusMessageColor = "Orange";
        }
    }

    #endregion
}
