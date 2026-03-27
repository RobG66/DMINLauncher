using DMINLauncher.Models;
using DMINLauncher.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DMINLauncher.ViewModels;

public partial class MainWindowViewModel
{
    #region WAD / Mod Scanning

    private void RefreshWadFiles()
    {
        WadFiles.Clear();

        if (!Directory.Exists(_wadDir))
        {
            StatusMessage      = $"⚠️ Data directory not found: {_wadDir}";
            StatusMessageColor = "Orange";
            return;
        }

        var wads = Directory.GetFiles(_wadDir, "*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".wad", StringComparison.OrdinalIgnoreCase))
            .Select(f => new WadFile
            {
                FullPath     = f,
                RelativePath = Path.GetRelativePath(_wadDir, f).Replace('\\', '/'),
                LastModified = File.GetLastWriteTime(f)
            })
            .OrderBy(w => w.RelativePath, StringComparer.OrdinalIgnoreCase);

        foreach (var wad in wads)
            WadFiles.Add(wad);
    }

    private void RefreshModFiles()
    {
        ModFiles.Clear();
        if (!Directory.Exists(_wadDir)) return;

        var modFiles = new List<WadFile>();
        try { CollectModFiles(_wadDir, modFiles); } catch { }

        foreach (var mod in modFiles.OrderBy(m => m.RelativePath))
            ModFiles.Add(mod);
    }

    private void CollectModFiles(string directory, List<WadFile> modFiles)
    {
        try
        {
            var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".wad",  StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".pk3",  StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".pk7",  StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".ipk3", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                try
                {
                    var info = Services.WadParser.Parse(file);
                    if (info.IsValid && info.WadType.Equals("IWAD", StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                catch { }

                modFiles.Add(new WadFile
                {
                    FullPath     = file,
                    RelativePath = Path.GetRelativePath(directory, file).Replace('\\', '/'),
                    LastModified = File.GetLastWriteTime(file)
                });
            }
        }
        catch { }
    }

    private void LoadMapsFromIWAD(WadFile iwad)
    {
        try
        {
            AvailableMaps.Clear();

            var info = Services.WadParser.Parse(iwad.FullPath);
            if (!info.IsValid || info.MapNames.Count == 0) return;

            foreach (var map in info.MapNames)
                AvailableMaps.Add(map);

            if (string.IsNullOrEmpty(SelectedMapName) || !AvailableMaps.Contains(SelectedMapName))
                SelectedMapName = AvailableMaps[0];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMapsFromIWAD: {ex.Message}");
            AvailableMaps.Add("E1M1");
            SelectedMapName = "E1M1";
        }
    }

    #endregion

    #region Difficulty Names

    private void UpdateDifficultyNames()
    {
        DifficultyOptions.Clear();

        var name = SelectedBaseGame?.RelativePath.ToLowerInvariant() ?? "";

        if (name.Contains("doom2") || name.Contains("plutonia") ||
            name.Contains("tnt")   || name.Contains("doom.wad") || name.Contains("doom1"))
        {
            DifficultyOptions.Add("I'm Too Young To Die");
            DifficultyOptions.Add("Hey, Not Too Rough");
            DifficultyOptions.Add("Hurt Me Plenty");
            DifficultyOptions.Add("Ultra-Violence");
            DifficultyOptions.Add("Nightmare!");
        }
        else if (name.Contains("heretic"))
        {
            DifficultyOptions.Add("Thou Needeth A Wet-Nurse");
            DifficultyOptions.Add("Yellowbellies-R-Us");
            DifficultyOptions.Add("Bringest Them Oneth");
            DifficultyOptions.Add("Thou Art A Smite-Meister");
            DifficultyOptions.Add("Black Plague Possesses Thee");
        }
        else if (name.Contains("hexen"))
        {
            DifficultyOptions.Add("Training");
            DifficultyOptions.Add("Squire");
            DifficultyOptions.Add("Knight");
            DifficultyOptions.Add("Warrior");
            DifficultyOptions.Add("Titan");
        }
        else
        {
            DifficultyOptions.Add("Very Easy");
            DifficultyOptions.Add("Easy");
            DifficultyOptions.Add("Normal");
            DifficultyOptions.Add("Hard");
            DifficultyOptions.Add("Nightmare");
        }
    }

    #endregion

    #region Mod Load Order

    private void AddModToLoadOrder()
    {
        if (SelectedAvailableMod == null) return;
        var mod = SelectedAvailableMod;
        ModFiles.Remove(mod);
        mod.LoadOrder = SelectedModFiles.Count;
        SelectedModFiles.Add(mod);
        SelectedAvailableMod = ModFiles.FirstOrDefault();
    }

    private void RemoveModFromLoadOrder()
    {
        if (SelectedLoadOrderMod == null) return;
        var mod = SelectedLoadOrderMod;
        SelectedModFiles.Remove(mod);
        mod.LoadOrder = 0;

        int idx = 0;
        while (idx < ModFiles.Count &&
               string.Compare(ModFiles[idx].RelativePath, mod.RelativePath, StringComparison.OrdinalIgnoreCase) < 0)
            idx++;
        ModFiles.Insert(idx, mod);

        RenumberLoadOrder();
        SelectedLoadOrderMod = SelectedModFiles.FirstOrDefault();
    }

    private void MoveModUp()
    {
        if (SelectedLoadOrderMod == null) return;
        var idx = SelectedModFiles.IndexOf(SelectedLoadOrderMod);
        if (idx <= 0) return;
        var mod = SelectedLoadOrderMod;
        SelectedModFiles.RemoveAt(idx);
        SelectedModFiles.Insert(idx - 1, mod);
        RenumberLoadOrder();
        SelectedLoadOrderMod = mod;
    }

    private void MoveModDown()
    {
        if (SelectedLoadOrderMod == null) return;
        var idx = SelectedModFiles.IndexOf(SelectedLoadOrderMod);
        if (idx < 0 || idx >= SelectedModFiles.Count - 1) return;
        var mod = SelectedLoadOrderMod;
        SelectedModFiles.RemoveAt(idx);
        SelectedModFiles.Insert(idx + 1, mod);
        RenumberLoadOrder();
        SelectedLoadOrderMod = mod;
    }

    private void RenumberLoadOrder()
    {
        for (int i = 0; i < SelectedModFiles.Count; i++)
            SelectedModFiles[i].LoadOrder = i;
    }

    #endregion

    #region Directory & Engine Pickers

    private async Task ChangeDataDirectory()
    {
        try
        {
            var topLevel = GetMainWindow();
            if (topLevel == null) { StatusMessage = "❌ Cannot access main window"; StatusMessageColor = "Red"; return; }

            var result = await TryOpenFolderPicker(topLevel, "Select WADs & Mods Directory", _wadDir);
            if (string.IsNullOrWhiteSpace(result)) return;

            if (!Directory.Exists(result))
            {
                StatusMessage      = $"⚠️ Directory does not exist: {result}";
                StatusMessageColor = "Orange";
                return;
            }

            if (IsFlatpakPath(EngineExecutable, out _))
            {
                var oldDir    = _wadDir;
                _wadDir       = result;
                var confirmed = await ShowFlatpakPermissionWarning(topLevel);

                if (!confirmed)
                {
                    _wadDir           = oldDir;
                    EngineExecutable  = "";
                    UseFilePathEngine = true;
                    StatusMessage      = "⚠️ WADs directory change cancelled. Flatpak engine cleared.";
                    StatusMessageColor = "Orange";
                    return;
                }
            }

            _wadDir       = result;
            DataDirectory = result;
            RefreshWadFiles();
            RefreshModFiles();
            SaveDefaultConfig();
            StatusMessage      = $"✅ WADs directory changed to: {result}";
            StatusMessageColor = "LimeGreen";
        }
        catch (Exception ex)
        {
            StatusMessage      = $"❌ Error: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    private async Task ChangeEngineExecutable()
    {
        try
        {
            var topLevel = GetMainWindow();
            if (topLevel == null) { StatusMessage = "❌ Cannot access main window"; StatusMessageColor = "Red"; return; }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title          = "Select Engine Executable",
                AllowMultiple  = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Doom Engine")
                    {
                        Patterns = OperatingSystem.IsWindows() ? new[] { "*.exe" } : new[] { "*" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count == 0) return;

            var path = files[0].Path.LocalPath;
            if (!File.Exists(path)) { StatusMessage = $"⚠️ File does not exist: {path}"; StatusMessageColor = "Orange"; return; }

            EngineExecutable   = path;
            SaveDefaultConfig();
            StatusMessage      = $"✅ Engine set to: {Path.GetFileName(path)}";
            StatusMessageColor = "LimeGreen";
        }
        catch (Exception ex)
        {
            StatusMessage      = $"❌ Error: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    #endregion

    #region DMFlags

    private void UpdateDmFlagsLists()
    {
        DmFlags1List.Clear();
        DmFlags1Col1.Clear();
        DmFlags1Col2.Clear();
        DmFlags1Col3.Clear();

        var sorted1    = NormalizeAndSortDmFlags(DmFlagsData.DmFlags1);
        int total1     = sorted1.Count;
        const int cols = 3;
        int rows1      = (total1 + cols - 1) / cols;

        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows1; row++)
            {
                int index = row + col * rows1;
                if (index >= total1) continue;

                var flag = new DmFlag { Label = sorted1[index].Key, BitValue = sorted1[index].Value };
                DmFlags1List.Add(flag);
                if      (col == 0) DmFlags1Col1.Add(flag);
                else if (col == 1) DmFlags1Col2.Add(flag);
                else               DmFlags1Col3.Add(flag);
            }
        }

        DmFlags2List.Clear();
        AddFlagsInColumnOrder(DmFlags2List, NormalizeAndSortDmFlags(DmFlagsData.DmFlags2), 3);

        DmFlags3List.Clear();
        AddFlagsInColumnOrder(DmFlags3List, NormalizeAndSortDmFlags(DmFlagsData.DmFlags3), 3);
    }

    private static List<KeyValuePair<string, int>> NormalizeAndSortDmFlags(Dictionary<string, int> source) =>
        source
            .Select(kvp => new KeyValuePair<string, int>(kvp.Key.Trim(), kvp.Value))
            .OrderBy(kvp => kvp.Key, StringComparer.InvariantCultureIgnoreCase)
            .ToList();

    private static void AddFlagsInColumnOrder(
        System.Collections.ObjectModel.ObservableCollection<DmFlag> target,
        List<KeyValuePair<string, int>> sorted,
        int columnCount)
    {
        if (sorted.Count == 0) return;
        int rowCount = (sorted.Count + columnCount - 1) / columnCount;

        for (int row = 0; row < rowCount; row++)
            for (int col = 0; col < columnCount; col++)
            {
                int idx = col * rowCount + row;
                if (idx < sorted.Count)
                    target.Add(new DmFlag { Label = sorted[idx].Key, BitValue = sorted[idx].Value });
            }
    }

    #endregion
}
