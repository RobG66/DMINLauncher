using DMINLauncher.Enums;
using DMINLauncher.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DMINLauncher.ViewModels;

public partial class MainWindowViewModel
{
    #region Public

    public void OnClosing()
    {
        try { SaveDefaultConfig(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"OnClosing error: {ex.Message}"); }
    }

    #endregion

    #region Load

    private void LoadSavedPaths()
    {
        _wadDir           = "";
        _engineFilePath   = "";

        var configFile = Path.Combine(Directory.GetCurrentDirectory(), "dminlauncher.cfg");

        try
        {
            if (!File.Exists(configFile)) return;

            var cfg = CfgFileService.ReadFile(configFile);

            if (cfg.TryGetValue("Paths", out var paths))
            {
                foreach (var kvp in paths)
                {
                    switch (kvp.Key.ToLowerInvariant())
                    {
                        case "wads" when !string.IsNullOrEmpty(kvp.Value) && Directory.Exists(kvp.Value):
                            _wadDir = kvp.Value; break;
                        case "enginefilepath" when !string.IsNullOrEmpty(kvp.Value):
                            _engineFilePath = kvp.Value; break;
                        case "engineflatpak" when !string.IsNullOrEmpty(kvp.Value):
                            _engineFlatpakPath = kvp.Value; break;
                    }
                }
            }

            if (cfg.TryGetValue("Main", out var main) &&
                main.TryGetValue("usefilepathengine", out var v) &&
                bool.TryParse(v, out var useFile))
            {
                _useFilePathEngine = useFile;
                _useFlatpakEngine  = !useFile;
            }
        }
        catch { }

        DataDirectory = _wadDir;
        this.RaisePropertyChanged(nameof(UseFilePathEngine));
        this.RaisePropertyChanged(nameof(UseFlatpakEngine));
        this.RaisePropertyChanged(nameof(EngineExecutable));
    }

    private void LoadDefaultSettings(string configFile)
    {
        if (!File.Exists(configFile)) return;

        try
        {
            string? savedBaseGame      = null;
            var savedMods              = new List<string>();
            var warnings               = new List<string>();
            Difficulty? savedDifficulty = null;
            string? savedMapName       = null;
            var cfg                    = CfgFileService.ReadFile(configFile);

            if (cfg.TryGetValue("Paths", out var pathsSection))
            {
                if (pathsSection.TryGetValue("wads",           out var w)) DataDirectory      = w;
                if (pathsSection.TryGetValue("enginefilepath", out var e)) _engineFilePath    = e;
                if (pathsSection.TryGetValue("engineflatpak",  out var f)) _engineFlatpakPath = f;

                this.RaisePropertyChanged(nameof(EngineExecutable));
                this.RaisePropertyChanged(nameof(CanLaunchGame));
            }

            if (cfg.TryGetValue("Main", out var mainSection))
            {
                if (mainSection.TryGetValue("usefilepathengine", out var ufp))
                    UseFilePathEngine = ufp.Equals("true", StringComparison.OrdinalIgnoreCase);
                if (mainSection.TryGetValue("basegame", out var bg))
                    savedBaseGame = bg?.Replace('\\', '/');
            }

            // Apply IWAD first so its setter can initialise maps/difficulty defaults,
            // then game-section values will overwrite those defaults below.
            if (!string.IsNullOrEmpty(savedBaseGame))
            {
                SelectedBaseGame = WadFiles.FirstOrDefault(w =>
                    w.RelativePath.Equals(savedBaseGame, StringComparison.OrdinalIgnoreCase));

                if (SelectedBaseGame != null && !File.Exists(SelectedBaseGame.FullPath))
                {
                    warnings.Add($"IWAD not found: {savedBaseGame}");
                    SelectedBaseGame = null;
                }
                else if (SelectedBaseGame == null)
                {
                    warnings.Add($"IWAD not found: {savedBaseGame}");
                }
            }

            SelectedBaseGame ??= WadFiles.FirstOrDefault(w =>
                w.RelativePath.Equals("doom2.wad", StringComparison.OrdinalIgnoreCase));

            // Apply game settings after IWAD so they override the setter's defaults.
            if (cfg.TryGetValue("Game", out var gameSection))
            {
                foreach (var kvp in gameSection)
                {
                    switch (kvp.Key)
                    {
                        case "difficulty"  when int.TryParse(kvp.Value, out var d):  savedDifficulty    = (Difficulty)d;   break;
                        case "startmap":                                              savedMapName       = kvp.Value;       break;
                        case "gametype"    when int.TryParse(kvp.Value, out var gt): GameType           = (GameType)gt;    break;
                        case "playercount" when int.TryParse(kvp.Value, out var pc): PlayerCount        = pc;              break;
                        case "networkmode" when int.TryParse(kvp.Value, out var nm): NetworkMode        = (NetworkMode)nm; break;
                        case "hexenclass"  when int.TryParse(kvp.Value, out var hc): HexenClass         = (HexenClass)hc; break;
                    }
                }

                if (savedDifficulty.HasValue) SelectedDifficulty = savedDifficulty.Value;
                if (savedMapName != null)      SelectedMapName    = savedMapName;
            }

            if (cfg.TryGetValue("Switches", out var sw))
            {
                bool B(string key) => sw.TryGetValue(key, out var v) && v.Equals("true", StringComparison.OrdinalIgnoreCase);
                int  I(string key, int def) => sw.TryGetValue(key, out var v) && int.TryParse(v, out var r) ? r : def;

                AvgSwitch        = B("avg");
                FastSwitch       = B("fast");
                NoMonstersSwitch = B("nomonsters");
                RespawnSwitch    = B("respawn");
                TimerSwitch      = B("timer");
                TimerMinutes     = I("timerminutes", 20);
                TurboSwitch      = B("turbo");
                TurboSpeed       = I("turbospeed",   100);
            }

            if (cfg.TryGetValue("UI", out var ui) &&
                ui.TryGetValue("zoomlevel", out var z) &&
                int.TryParse(z, out var zoom))
            {
                ZoomLevel = Math.Clamp(zoom, 50, 200);
            }

            if (cfg.TryGetValue("Mods", out var modsSection))
            {
                foreach (var kvp in modsSection.OrderBy(x => x.Key))
                    if (!string.IsNullOrEmpty(kvp.Value))
                        savedMods.Add(kvp.Value.Replace('\\', '/'));
            }

            // Apply mods
            foreach (var modPath in savedMods)
            {
                var mod = ModFiles.FirstOrDefault(m =>
                    m.RelativePath.Equals(modPath, StringComparison.OrdinalIgnoreCase));

                if (mod == null) { warnings.Add($"WAD not found: {modPath}"); continue; }

                if (File.Exists(mod.FullPath))
                {
                    ModFiles.Remove(mod);
                    mod.LoadOrder = SelectedModFiles.Count;
                    SelectedModFiles.Add(mod);
                }
                else
                {
                    warnings.Add($"WAD not found: {modPath}");
                }
            }

            if (warnings.Any())
            {
                StatusMessage      = $"⚠️ Configuration warnings:\n{string.Join("\n", warnings)}";
                StatusMessageColor = "Orange";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    #endregion

    #region Save

    private void SaveDefaultConfig()
    {
        var configFile = Path.Combine(Directory.GetCurrentDirectory(), "dminlauncher.cfg");
        SaveConfigFile(configFile);
    }

    private void SaveConfigFile(string configFile)
    {
        try
        {
            var sections = new Dictionary<string, Dictionary<string, string>>
            {
                ["Paths"] = new()
                {
                    { "wads",           _wadDir           },
                    { "enginefilepath", _engineFilePath   },
                    { "engineflatpak",  _engineFlatpakPath }
                },
                ["Main"] = new()
                {
                    { "usefilepathengine", UseFilePathEngine.ToString() },
                    { "basegame",          SelectedBaseGame?.RelativePath ?? "" }
                },
                ["Game"] = new()
                {
                    { "difficulty",  ((int)SelectedDifficulty).ToString() },
                    { "startmap",    SelectedMapName                      },
                    { "gametype",    ((int)GameType).ToString()           },
                    { "playercount", PlayerCount.ToString()               },
                    { "networkmode", ((int)NetworkMode).ToString()        },
                    { "hexenclass",  ((int)HexenClass).ToString()        }
                },
                ["Switches"] = new()
                {
                    { "avg",          AvgSwitch.ToString()        },
                    { "fast",         FastSwitch.ToString()       },
                    { "nomonsters",   NoMonstersSwitch.ToString() },
                    { "respawn",      RespawnSwitch.ToString()    },
                    { "timer",        TimerSwitch.ToString()      },
                    { "timerminutes", TimerMinutes.ToString()     },
                    { "turbo",        TurboSwitch.ToString()      },
                    { "turbospeed",   TurboSpeed.ToString()       }
                },
                ["UI"] = new()
                {
                    { "zoomlevel", ZoomLevel.ToString() }
                }
            };

            var mods = new Dictionary<string, string>();
            int i = 0;
            foreach (var mod in SelectedModFiles.OrderBy(m => m.LoadOrder))
                mods[$"mod{i++}"] = mod.RelativePath;
            sections["Mods"] = mods;

            CfgFileService.WriteFile(configFile, sections);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private async Task AskToSaveCfgConfig()
    {
        try
        {
            var topLevel = GetMainWindow();
            if (topLevel == null) return;

            var baseName    = SelectedBaseGame?.RelativePath?.Replace(".wad", "") ?? "config";
            var gameTypeStr = GameType == GameType.SinglePlayer ? "" : $"-{GameType.ToString().ToLower()}";
            var modStr      = SelectedModFiles.Count > 0 ? $"-{SelectedModFiles.Count}mods" : "";

            var saveOptions = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title               = "Save Config File",
                SuggestedFileName   = $"{baseName}{gameTypeStr}{modStr}.cfg",
                DefaultExtension    = "cfg",
                ShowOverwritePrompt = true,
                FileTypeChoices     = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Configuration File") { Patterns = new[] { "*.cfg" } }
                }
            };
            var programDir = Directory.GetCurrentDirectory();
            if (Directory.Exists(programDir))
            {
                try { saveOptions.SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(PathToFileUri(programDir)); }
                catch { }
            }
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);

            if (file == null) return;

            SaveConfigFile(file.Path.LocalPath);
            StatusMessage      = $"✅ Config saved to: {file.Name}";
            StatusMessageColor = "LimeGreen";
        }
        catch (Exception ex)
        {
            StatusMessage      = $"❌ Save error: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    private async Task AskToLoadCfgConfig()
    {
        try
        {
            var topLevel = GetMainWindow();
            if (topLevel == null) return;

            var openOptions = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title          = "Load Config File",
                AllowMultiple  = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Configuration File") { Patterns = new[] { "*.cfg" }  },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")          { Patterns = new[] { "*.*" }    }
                }
            };
            var programDir = Directory.GetCurrentDirectory();
            if (Directory.Exists(programDir))
            {
                try { openOptions.SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(PathToFileUri(programDir)); }
                catch { }
            }
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(openOptions);

            if (files.Count == 0) return;

            LoadDefaultSettings(files[0].Path.LocalPath);
            StatusMessage      = $"✅ Config loaded from: {files[0].Name}";
            StatusMessageColor = "LimeGreen";
        }
        catch (Exception ex)
        {
            StatusMessage      = $"❌ Load error: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    #endregion

    #region Reset

    private void ResetSettings()
    {
        var currentIwadPath = SelectedBaseGame?.RelativePath;

        if (IsBatocera)
        {
            _engineFilePath   = File.Exists("/usr/bin/gzdoom") ? "/usr/bin/gzdoom" : "";
            UseFilePathEngine = true;
            this.RaisePropertyChanged(nameof(EngineExecutable));
            _wadDir       = Directory.Exists("/userdata/roms/gzdoom") ? "/userdata/roms/gzdoom" : "";
            DataDirectory = _wadDir;
        }

        if (!string.IsNullOrEmpty(_wadDir))
        {
            RefreshWadFiles();
            RefreshModFiles();
        }

        SelectedDifficulty = Difficulty.Normal;
        GameType           = GameType.SinglePlayer;
        PlayerCount        = 1;
        HexenClass         = HexenClass.Fighter;
        NetworkMode        = NetworkMode.None;
        IpAddress          = "";

        AvgSwitch = FastSwitch = NoMonstersSwitch = RespawnSwitch = TimerSwitch = TurboSwitch = false;
        TimerMinutes = 20;
        TurboSpeed   = 100;

        foreach (var mod in ModFiles) mod.IsSelected = false;
        SelectedModFiles.Clear();

        foreach (var f in DmFlags1List) f.IsChecked = false;
        foreach (var f in DmFlags2List) f.IsChecked = false;
        foreach (var f in DmFlags3List) f.IsChecked = false;

        if (!string.IsNullOrEmpty(currentIwadPath))
            SelectedBaseGame = WadFiles.FirstOrDefault(w =>
                w.RelativePath.Equals(currentIwadPath, StringComparison.OrdinalIgnoreCase));

        SelectedMapName    = "MAP01";
        StatusMessage      = IsBatocera
            ? "✅ Settings reset to Batocera defaults (paths and IWAD preserved)"
            : "✅ Settings reset to defaults (paths and IWAD preserved)";
        StatusMessageColor = "LimeGreen";
    }

    #endregion

    #region Helpers

    private void ApplyDmFlags(string key, int value)
    {
        var list = key.ToLower() switch
        {
            "dmflags"  => DmFlags1List,
            "dmflags2" => DmFlags2List,
            "dmflags3" => DmFlags3List,
            _          => null
        };

        if (list == null) return;
        foreach (var flag in list)
            flag.IsChecked = (value & flag.BitValue) != 0;
    }

    #endregion
}
