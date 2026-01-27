using Avalonia.Controls;
using Avalonia.Input;
using DMINLauncher.Data;
using DMINLauncher.Enums;
using DMINLauncher.Models;
using DMINLauncher.Services;
using DMINLauncher.Views;
using Open.Nat;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DMINLauncher.ViewModels;


public class MainWindowViewModel : ReactiveObject
{
    private string _wadDir = "";

    // Application version for titlebar
    public string AppVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "";
        }
    }

    // OS detection for platform-specific features
    public bool IsWindows => OperatingSystem.IsWindows();
    public bool IsLinux => OperatingSystem.IsLinux();
    public bool IsBatocera => Directory.Exists("/userdata/roms/ports");

    public MainWindowViewModel()
    {
        // Load saved paths or use defaults
        LoadSavedPaths();
        
        // Initialize combo box item sources
        DifficultyOptions = new ObservableCollection<string>();
        UpdateDifficultyNames();
        GameTypeOptions = new ObservableCollection<GameType>(
            Enum.GetValues<GameType>());
        NetworkModeOptions = new ObservableCollection<NetworkMode>(
            Enum.GetValues<NetworkMode>());
        HexenClassOptions = new ObservableCollection<HexenClass>(
            Enum.GetValues<HexenClass>());

        RefreshWadFiles();
        RefreshModFiles();
        UpdateDmFlagsLists();

        // Detect if running on Batocera
        bool isBatocera = Directory.Exists("/userdata/roms/ports");

        var configFile = Path.Combine(Directory.GetCurrentDirectory(), "dminlauncher.cfg");

        // If no config file exists and we're on Batocera, create default config
        if (isBatocera && !File.Exists(configFile))
        {
            ResetSettings();
        }

        // Load saved settings
        LoadDefaultSettings(configFile);
        
        // Fetch IP addresses based on loaded network mode
        InitializeNetworkInfo();
        
        // Suspend evmapy on Batocera to prevent hotkey interference
        if (IsBatocera)
        {
            // Perform any required setup for Batocera here
            // Nothing right now
        }

        // LaunchGameCommand can only execute when IWAD and engine are selected and not currently launching
        var canLaunch = this.WhenAnyValue(
            x => x.SelectedBaseGame,
            x => x.EngineExecutable,
            x => x.IsLaunching,
            (iwad, engine, launching) => iwad != null && !string.IsNullOrEmpty(engine) && !launching);
        LaunchGameCommand = ReactiveCommand.Create(LaunchGame, canLaunch);
        ShowLaunchSummaryCommand = ReactiveCommand.CreateFromTask(ShowLaunchSummary);
        RefreshFilesCommand = ReactiveCommand.Create(() =>
        {
            //RefreshWadFiles();
            RefreshModFiles();
            
            // Clear any previous configuration warnings when manually refreshing
            if (StatusMessage.Contains("Configuration warnings", StringComparison.OrdinalIgnoreCase) ||
                StatusMessage.Contains("WAD not found", StringComparison.OrdinalIgnoreCase) ||
                StatusMessage.Contains("IWAD not found", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "✅ Files refreshed";
                StatusMessageColor = "LimeGreen";
            }
        });
        SaveSettingsCommand = ReactiveCommand.CreateFromTask(AskToSavePresetConfig);
        LoadSettingsCommand = ReactiveCommand.CreateFromTask(LoadSettings);
        TestPortForwardingCommand = ReactiveCommand.CreateFromTask(TestPortForwarding);
        AutoConfigurePortCommand = ReactiveCommand.CreateFromTask(AutoConfigurePort);
        ResetSettingsCommand = ReactiveCommand.Create(ResetSettings);
        ChangeDataDirectoryCommand = ReactiveCommand.CreateFromTask(ChangeDataDirectory);
        ChangeEngineExecutableCommand = ReactiveCommand.CreateFromTask(ChangeEngineExecutable);
        AddCustomFlatpakCommand = ReactiveCommand.CreateFromTask(AddCustomFlatpak);
        SaveCurrentSettingsCommand = ReactiveCommand.Create(SaveDefaultConfig);
        
        
        
        // Batocera commands
        SaveBatoceraConfigCommand = ReactiveCommand.CreateFromTask(SaveGZDoomConfig);
        
        // Zoom commands
        ZoomInCommand = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(ZoomOut);
        ResetZoomCommand = ReactiveCommand.Create(ResetZoom);
        
        // Application commands
        ExitCommand = ReactiveCommand.Create(ExitApplication);
        ShowExitConfirmationCommand = ReactiveCommand.CreateFromTask(ShowExitConfirmation);
        
        // Mod load order commands
        AddModToLoadOrderCommand = ReactiveCommand.Create(AddModToLoadOrder);
        RemoveModFromLoadOrderCommand = ReactiveCommand.Create(RemoveModFromLoadOrder);
        MoveModUpCommand = ReactiveCommand.Create(MoveModUp);
        MoveModDownCommand = ReactiveCommand.Create(MoveModDown);

        // React to NetworkMode changes for auto IP detection
        this.WhenAnyValue(x => x.NetworkMode)
            .Skip(1) // Skip initial value
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async mode =>
            {
                switch (mode)
                {
                    case Enums.NetworkMode.HostLAN:
                        await GetLocalIpAddress();
                        break;
                    case Enums.NetworkMode.HostInternet:
                        // Get both local and public IPs for internet hosting
                        await GetLocalIpAddress();
                        await GetPublicIpAddress();
                        break;
                    case Enums.NetworkMode.None:
                        IpAddress = "";
                        LocalIpAddress = "";
                        PublicIpAddress = "";
                        break;
                }
            });
    }
    
    /// <summary>
    /// Called when the application is closing to save current settings and clean up resources
    /// </summary>
    public void OnClosing()
    {
        try
        {
            // Save current settings
            SaveDefaultConfig();       
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during OnClosing: {ex.Message}");
        }
    }

    // Commands
    public ReactiveCommand<Unit, Unit> LaunchGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowLaunchSummaryCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> TestPortForwardingCommand { get; }
    public ReactiveCommand<Unit, Unit> AutoConfigurePortCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ChangeDataDirectoryCommand { get; }
    public ReactiveCommand<Unit, Unit> ChangeEngineExecutableCommand { get; }
    public ReactiveCommand<Unit, Unit> AddCustomFlatpakCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCurrentSettingsCommand { get; }
    
    // Batocera commands
    public ReactiveCommand<Unit, Unit> SaveBatoceraConfigCommand { get; }
    
    
    // Zoom commands
    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetZoomCommand { get; }
    
    // Application commands
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowExitConfirmationCommand { get; }
    
    // Mod load order commands
    public ReactiveCommand<Unit, Unit> AddModToLoadOrderCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveModFromLoadOrderCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveModUpCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveModDownCommand { get; }

    // ComboBox Item Sources (MVVM compliant)
    public ObservableCollection<string> DifficultyOptions { get; }
    public ObservableCollection<GameType> GameTypeOptions { get; }
    public ObservableCollection<NetworkMode> NetworkModeOptions { get; }
    public ObservableCollection<HexenClass> HexenClassOptions { get; }

    // Observable Collections
    public ObservableCollection<WadFile> WadFiles { get; } = new();
    public ObservableCollection<WadFile> ModFiles { get; } = new();
    public ObservableCollection<WadFile> SelectedModFiles { get; } = new();
    public ObservableCollection<DmFlag> DmFlags1List { get; } = new();
    public ObservableCollection<DmFlag> DmFlags1Col1 { get; } = new();
    public ObservableCollection<DmFlag> DmFlags1Col2 { get; } = new();
    public ObservableCollection<DmFlag> DmFlags1Col3 { get; } = new();
    public ObservableCollection<DmFlag> DmFlags2List { get; } = new();
    public ObservableCollection<DmFlag> DmFlags3List { get; } = new();
    public ObservableCollection<string> AvailableMaps { get; } = new();

    // Properties
    private WadFile? _selectedBaseGame;
    public WadFile? SelectedBaseGame
    {
        get => _selectedBaseGame;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedBaseGame, value);
            this.RaisePropertyChanged(nameof(CanLaunchGame));
            if (value != null)
            {
                ShowHexenClassSelection = value.RelativePath.Contains("hexen", StringComparison.OrdinalIgnoreCase);
                LoadMapsFromIWAD(value);
                UpdateDifficultyNames(); // Update difficulty names based on selected game
                
                // Reset to default difficulty and first map when IWAD changes
                SelectedDifficulty = Difficulty.Easy;
                if (AvailableMaps.Count > 0)
                {
                    SelectedMapName = AvailableMaps[0];
                }
                
                // Clear IWAD-related warnings when a valid IWAD is selected
                if (StatusMessage.Contains("IWAD not found", StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "";
                    StatusMessageColor = "White";
                }
            }
            else
            {
                AvailableMaps.Clear();
                SelectedMapName = "";
            }
        }
    }

    private string _engineFilePath = string.Empty;
    private string _engineFlatpakPath = string.Empty;
    
    public string EngineExecutable
    {
        get => _useFilePathEngine ? _engineFilePath : _engineFlatpakPath;
        set
        {
            if (_useFilePathEngine)
            {
                this.RaiseAndSetIfChanged(ref _engineFilePath, value);
            }
            else
            {
                this.RaiseAndSetIfChanged(ref _engineFlatpakPath, value);
            }
            this.RaisePropertyChanged(nameof(EngineExecutable));
            this.RaisePropertyChanged(nameof(CanLaunchGame));
        }
    }
    
    // Engine selection mode (File Path vs Flatpak)
    private bool _useFilePathEngine = true;
    public bool UseFilePathEngine
    {
        get => _useFilePathEngine;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _useFilePathEngine, value) && value)
            {
                _useFlatpakEngine = false;
                this.RaisePropertyChanged(nameof(UseFlatpakEngine));
                this.RaisePropertyChanged(nameof(EngineExecutable));
                this.RaisePropertyChanged(nameof(CanLaunchGame));
            }
        }
    }
    
    private bool _useFlatpakEngine = false;
    public bool UseFlatpakEngine
    {
        get => _useFlatpakEngine;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _useFlatpakEngine, value) && value)
            {
                _useFilePathEngine = false;
                this.RaisePropertyChanged(nameof(UseFilePathEngine));
                this.RaisePropertyChanged(nameof(EngineExecutable));
                this.RaisePropertyChanged(nameof(CanLaunchGame));
            }
        }
    }

    private bool _isLaunching = false;
    public bool IsLaunching
    {
        get => _isLaunching;
        set => this.RaiseAndSetIfChanged(ref _isLaunching, value);
    }

    // Property to check if launch is allowed
    public bool CanLaunchGame => SelectedBaseGame != null && !string.IsNullOrEmpty(EngineExecutable);

    private Difficulty _selectedDifficulty = Difficulty.Normal;
    public Difficulty SelectedDifficulty
    {
        get => _selectedDifficulty;
        set => this.RaiseAndSetIfChanged(ref _selectedDifficulty, value);
    }

    private string _selectedMapName = "";
    public string SelectedMapName
    {
        get => _selectedMapName;
        set => this.RaiseAndSetIfChanged(ref _selectedMapName, value);
    }

    private GameType _gameType = GameType.SinglePlayer;
    public GameType GameType
    {
        get => _gameType;
        set
        {
            this.RaiseAndSetIfChanged(ref _gameType, value);
            this.RaisePropertyChanged(nameof(ShowPlayerCount));
            if (value == GameType.SinglePlayer)
            {
                PlayerCount = 1;
                if (_networkMode != Enums.NetworkMode.None)
                    NetworkMode = Enums.NetworkMode.None;
            }
            else if (PlayerCount == 1)
            {
                PlayerCount = 2;
            }
        }
    }

    public bool ShowPlayerCount => GameType != GameType.SinglePlayer;

    private int _playerCount = 1;
    public int PlayerCount
    {
        get => _playerCount;
        set => this.RaiseAndSetIfChanged(ref _playerCount, Math.Clamp(value, 1, 8));
    }

    private HexenClass _hexenClass = HexenClass.Fighter;
    public HexenClass HexenClass
    {
        get => _hexenClass;
        set => this.RaiseAndSetIfChanged(ref _hexenClass, value);
    }

    private bool _showHexenClassSelection;
    public bool ShowHexenClassSelection
    {
        get => _showHexenClassSelection;
        set => this.RaiseAndSetIfChanged(ref _showHexenClassSelection, value);
    }

    private NetworkMode _networkMode = NetworkMode.None;
    public NetworkMode NetworkMode
    {
        get => _networkMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _networkMode, value);
            this.RaisePropertyChanged(nameof(ShowPortForwardingTest));
            if (value != Enums.NetworkMode.None && GameType == GameType.SinglePlayer)
            {
                GameType = GameType.Cooperative;
            }
            else if (value == Enums.NetworkMode.None)
            {
                GameType = GameType.SinglePlayer;
                PlayerCount = 1;
            }
        }
    }

    public bool ShowPortForwardingTest => NetworkMode == Enums.NetworkMode.HostInternet || NetworkMode == Enums.NetworkMode.Connect;

    private string _ipAddress = "";
    public string IpAddress
    {
        get => _ipAddress;
        set => this.RaiseAndSetIfChanged(ref _ipAddress, value);
    }

    private string _localIpAddress = "";
    public string LocalIpAddress
    {
        get => _localIpAddress;
        set => this.RaiseAndSetIfChanged(ref _localIpAddress, value);
    }

    private string _publicIpAddress = "";
    public string PublicIpAddress
    {
        get => _publicIpAddress;
        set => this.RaiseAndSetIfChanged(ref _publicIpAddress, value);
    }

    private bool _avgSwitch;
    public bool AvgSwitch
    {
        get => _avgSwitch;
        set => this.RaiseAndSetIfChanged(ref _avgSwitch, value);
    }

    private bool _fastSwitch;
    public bool FastSwitch
    {
        get => _fastSwitch;
        set => this.RaiseAndSetIfChanged(ref _fastSwitch, value);
    }

    private bool _noMonstersSwitch;
    public bool NoMonstersSwitch
    {
        get => _noMonstersSwitch;
        set => this.RaiseAndSetIfChanged(ref _noMonstersSwitch, value);
    }

    private bool _respawnSwitch;
    public bool RespawnSwitch
    {
        get => _respawnSwitch;
        set => this.RaiseAndSetIfChanged(ref _respawnSwitch, value);
    }

    private bool _timerSwitch;
    public bool TimerSwitch
    {
        get => _timerSwitch;
        set => this.RaiseAndSetIfChanged(ref _timerSwitch, value);
    }

    private int _timerMinutes = 20;
    public int TimerMinutes
    {
        get => _timerMinutes;
        set => this.RaiseAndSetIfChanged(ref _timerMinutes, value);
    }

    private bool _turboSwitch;
    public bool TurboSwitch
    {
        get => _turboSwitch;
        set => this.RaiseAndSetIfChanged(ref _turboSwitch, value);
    }

    private int _turboSpeed = 100;
    public int TurboSpeed
    {
        get => _turboSpeed;
        set => this.RaiseAndSetIfChanged(ref _turboSpeed, Math.Clamp(value, 10, 255));
    }

    private string _portTestResult = "";
    public string PortTestResult
    {
        get => _portTestResult;
        set => this.RaiseAndSetIfChanged(ref _portTestResult, value);
    }

    private bool _isTestingPort;
    public bool IsTestingPort
    {
        get => _isTestingPort;
        set => this.RaiseAndSetIfChanged(ref _isTestingPort, value);
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private string _statusMessageColor = "White";
    public string StatusMessageColor
    {
        get => _statusMessageColor;
        set => this.RaiseAndSetIfChanged(ref _statusMessageColor, value);
    }

    private int _zoomLevel = 100;
    public int ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            this.RaiseAndSetIfChanged(ref _zoomLevel, value);
            UpdateWindowSize();
        }
    }

    private const double BaseWindowWidth = 850;
    private const double BaseWindowHeight = 700;

    private double _windowWidth = BaseWindowWidth;
    public double WindowWidth
    {
        get => _windowWidth;
        set => this.RaiseAndSetIfChanged(ref _windowWidth, value);
    }

    private double _windowHeight = BaseWindowHeight;
    public double WindowHeight
    {
        get => _windowHeight;
        set => this.RaiseAndSetIfChanged(ref _windowHeight, value);
    }

    private void UpdateWindowSize()
    {
        WindowWidth = BaseWindowWidth * (ZoomLevel / 100.0);
        WindowHeight = BaseWindowHeight * (ZoomLevel / 100.0);
    }

    private string _dataDirectory = "";
    public string DataDirectory
    {
        get => _dataDirectory;
        set => this.RaiseAndSetIfChanged(ref _dataDirectory, value);
    }

    private WadFile? _selectedAvailableMod;
    public WadFile? SelectedAvailableMod
    {
        get => _selectedAvailableMod;
        set => this.RaiseAndSetIfChanged(ref _selectedAvailableMod, value);
    }

    private WadFile? _selectedLoadOrderMod;
    public WadFile? SelectedLoadOrderMod
    {
        get => _selectedLoadOrderMod;
        set => this.RaiseAndSetIfChanged(ref _selectedLoadOrderMod, value);
    }

    private void LoadSavedPaths()
    {
        _wadDir = "";
        _engineFilePath = "";

        var currentDir = Directory.GetCurrentDirectory();
        var configFile = Path.Combine(currentDir, "dminlauncher.cfg");
                
        try
        {
            if (File.Exists(configFile))
            {
                var configData = CfgFileService.ReadFile(configFile);
                
                if (configData.TryGetValue("Paths", out var paths))
                {
                    foreach (var kvp in paths)
                    {
                        var key = kvp.Key.ToLowerInvariant();
                        var value = kvp.Value;
                        
                        if (key == "wads" && !string.IsNullOrEmpty(value) && Directory.Exists(value))
                            _wadDir = value;
                        else if (key == "enginefilepath" && !string.IsNullOrEmpty(value))
                            _engineFilePath = value;
                        else if (key == "engineflatpak" && !string.IsNullOrEmpty(value))
                            _engineFlatpakPath = value;
                    }
                }
                
                if (configData.TryGetValue("Main", out var mainSettings))
                {
                    foreach (var kvp in mainSettings)
                    {
                        var key = kvp.Key.ToLowerInvariant();
                        var value = kvp.Value;
                        
                        if (key == "usefilepathengine" && bool.TryParse(value, out var useFilePath))
                        {
                            _useFilePathEngine = useFilePath;
                            _useFlatpakEngine = !useFilePath;
                        }
                    }
                }
            }
        }
        catch { } // Ignore errors, use defaults
        
        DataDirectory = _wadDir;
        this.RaisePropertyChanged(nameof(UseFilePathEngine));
        this.RaisePropertyChanged(nameof(UseFlatpakEngine));
        this.RaisePropertyChanged(nameof(EngineExecutable));
    }
    
    
    private void SaveDefaultConfig()
    {
        var configFile = Path.Combine(Directory.GetCurrentDirectory(), "dminlauncher.cfg");
        SaveConfigFile(configFile);
    }
    
    private void SaveConfigFile(string configFile)
    {
        try
        {
            var sections = new Dictionary<string, Dictionary<string, string>>();
            
            // [Paths] section - directories and engine paths
            sections["Paths"] = new Dictionary<string, string>
            {
                { "wads", _wadDir },
                { "enginefilepath", _engineFilePath },
                { "engineflatpak", _engineFlatpakPath }
            };
            
            // [Main] section - main settings
            sections["Main"] = new Dictionary<string, string>
            {
                { "usefilepathengine", UseFilePathEngine.ToString() },
                { "basegame", SelectedBaseGame?.RelativePath ?? "" }
            };
            
            // [Game] section - gameplay settings
            sections["Game"] = new Dictionary<string, string>
            {
                { "difficulty", ((int)SelectedDifficulty).ToString() },
                { "startmap", SelectedMapName },
                { "gametype", ((int)GameType).ToString() },
                { "playercount", PlayerCount.ToString() },
                { "networkmode", ((int)NetworkMode).ToString() },
                { "hexenclass", ((int)HexenClass).ToString() }
            };
            
            // [Switches] section - run switches
            sections["Switches"] = new Dictionary<string, string>
            {
                { "avg", AvgSwitch.ToString() },
                { "fast", FastSwitch.ToString() },
                { "nomonsters", NoMonstersSwitch.ToString() },
                { "respawn", RespawnSwitch.ToString() },
                { "timer", TimerSwitch.ToString() },
                { "timerminutes", TimerMinutes.ToString() },
                { "turbo", TurboSwitch.ToString() },
                { "turbospeed", TurboSpeed.ToString() }
            };
            
            // [UI] section - interface settings
            sections["UI"] = new Dictionary<string, string>
            {
                { "zoomlevel", ZoomLevel.ToString() }
            };
            
            // [Mods] section - selected mods in load order
            var mods = new Dictionary<string, string>();
            int modIndex = 0;
            foreach (var mod in SelectedModFiles.OrderBy(m => m.LoadOrder))
            {
                mods[$"mod{modIndex++}"] = mod.RelativePath;
            }
            sections["Mods"] = mods;
            
            CfgFileService.WriteFile(configFile, sections);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private void LoadDefaultSettings(string configFile)
    {
        if (!File.Exists(configFile))
            return;

        try
        {            
            string? savedBaseGame = null;
            var savedMods = new List<string>();
            var warnings = new List<string>();
         
            var configData = CfgFileService.ReadFile(configFile);

            // Load [Paths] section
            if (configData.TryGetValue("Paths", out var pathsSection))
            {
                if (pathsSection.TryGetValue("wads", out var wadsPath))
                    DataDirectory = wadsPath;
                
                if (pathsSection.TryGetValue("enginefilepath", out var enginePath))
                    _engineFilePath = enginePath;
                
                if (pathsSection.TryGetValue("engineflatpak", out var flatpakPath))
                    _engineFlatpakPath = flatpakPath;
            }

            // Load [Main] section
            if (configData.TryGetValue("Main", out var mainSection))
            {
                if (mainSection.TryGetValue("usefilepathengine", out var useFilePath))
                    UseFilePathEngine = useFilePath.Equals("true", StringComparison.OrdinalIgnoreCase);
                
                if (mainSection.TryGetValue("basegame", out var bg))
                    savedBaseGame = bg;
            }

            // Load [Game] section
            if (configData.TryGetValue("Game", out var gameSection))
            {
                foreach (var kvp in gameSection)
                {
                    var key = kvp.Key;
                    var value = kvp.Value;

                    switch (key)
                    {
                        case "difficulty":
                            if (int.TryParse(value, out var diff))
                                SelectedDifficulty = (Enums.Difficulty)diff;
                            break;
                        case "startmap":
                            SelectedMapName = value;
                            break;
                        case "gametype":
                            if (int.TryParse(value, out var gt))
                                GameType = (Enums.GameType)gt;
                            break;
                        case "playercount":
                            if (int.TryParse(value, out var pc))
                                PlayerCount = pc;
                            break;
                        case "networkmode":
                            if (int.TryParse(value, out var nm))
                                NetworkMode = (Enums.NetworkMode)nm;
                            break;
                        case "hexenclass":
                            if (int.TryParse(value, out var hc))
                                HexenClass = (Enums.HexenClass)hc;
                            break;
                    }
                }
            }

            // Load [Switches] section
            if (configData.TryGetValue("Switches", out var switchesSection))
            {
                foreach (var kvp in switchesSection)
                {
                    var key = kvp.Key;
                    var value = kvp.Value;

                    switch (key)
                    {
                        case "avg":
                            AvgSwitch = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "fast":
                            FastSwitch = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "nomonsters":
                            NoMonstersSwitch = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "respawn":
                            RespawnSwitch = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "timer":
                            TimerSwitch = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "timerminutes":
                            if (int.TryParse(value, out var tm))
                                TimerMinutes = tm;
                            break;
                        case "turbo":
                            TurboSwitch = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "turbospeed":
                            if (int.TryParse(value, out var ts))
                                TurboSpeed = ts;
                            break;
                    }
                }
            }

            // Load [UI] section
            if (configData.TryGetValue("UI", out var uiSection))
            {
                if (uiSection.TryGetValue("zoomlevel", out var zoomStr))
                {
                    if (int.TryParse(zoomStr, out var zoom))
                    {
                        ZoomLevel = Math.Clamp(zoom, 50, 200);
                    }
                }
            }

            // Load [Mods] section
            if (configData.TryGetValue("Mods", out var modsSection))
            {
                foreach (var kvp in modsSection.OrderBy(x => x.Key))
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                        savedMods.Add(kvp.Value);
                }
            }
            
            // Apply saved selections
            if (!string.IsNullOrEmpty(savedBaseGame))
            {
                SelectedBaseGame = WadFiles.FirstOrDefault(w => 
                    w.RelativePath.Equals(savedBaseGame, StringComparison.OrdinalIgnoreCase));
                
                // Validate if file exists
                if (SelectedBaseGame != null && !File.Exists(SelectedBaseGame.FullPath))
                {
                    warnings.Add($"IWAD not found: {savedBaseGame}");
                    SelectedBaseGame = null;
                }
                else if (SelectedBaseGame == null && !string.IsNullOrEmpty(savedBaseGame))
                {
                    warnings.Add($"IWAD not found: {savedBaseGame}");
                }
            }
            
            // If no base game selected, auto-select doom2.wad if it exists
            if (SelectedBaseGame == null)
            {
                SelectedBaseGame = WadFiles.FirstOrDefault(w => 
                    w.RelativePath.Equals("doom2.wad", StringComparison.OrdinalIgnoreCase));
            }
            
            // Load saved mods
            foreach (var modPath in savedMods)
            {
                var mod = ModFiles.FirstOrDefault(m => 
                    m.RelativePath.Equals(modPath, StringComparison.OrdinalIgnoreCase));
                
                if (mod != null)
                {
                    // Validate file exists
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
                else
                {
                    warnings.Add($"WAD not found: {modPath}");
                }
            }
            
            // Show warnings if any
            if (warnings.Any())
            {
                StatusMessage = $"⚠️ Configuration warnings:\n{string.Join("\n", warnings)}";
                StatusMessageColor = "Orange";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }


    private void RefreshWadFiles()
    {
        WadFiles.Clear();
        
        if (!Directory.Exists(_wadDir))
        {
            StatusMessage = $"⚠️ Data directory not found: {_wadDir}";
            StatusMessageColor = "Orange";
            return;
        }


        // Scan all subdirectories for IWADs
        var wadFiles = Directory.GetFiles(_wadDir, "*.wad", SearchOption.AllDirectories)
            .Select(f => new WadFile
            {
                FullPath = f,
                RelativePath = Path.GetRelativePath(_wadDir, f),
                LastModified = File.GetLastWriteTime(f)
            })
            .OrderBy(w => w.RelativePath, StringComparer.OrdinalIgnoreCase);

        foreach (var wad in wadFiles)
        {
            WadFiles.Add(wad);
        }
    }

    private void RefreshModFiles()
    {
        ModFiles.Clear();
        if (!Directory.Exists(_wadDir)) return;
              
        var modFiles = new List<WadFile>();
             
        try
        {
            GetModFiles(_wadDir, "", modFiles);
        }
        catch { } // Skip if directory is inaccessible

        // Sort by path
        foreach (var mod in modFiles.OrderBy(m => m.RelativePath))
        {
            ModFiles.Add(mod);
        }
    }

    private void GetModFiles(string directory, string relativePath, List<WadFile> modFiles)
    {
        try
        {
            var files = Directory.GetFiles(directory,"*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".wad", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".pk3", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".pk7", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".ipk3", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                
                // Use WAD parser to check if this is an IWAD
                // IWADs should only appear in base game list, not as mods
                try
                {
                    var wadInfo = Services.WadParser.Parse(file);
                    if (wadInfo.IsValid && wadInfo.WadType.Equals("IWAD", StringComparison.OrdinalIgnoreCase))
                    {
                        // Skip IWADs - they belong in the base game list
                        continue;
                    }
                }
                catch
                {
                    // If we can't parse it, include it anyway (might be a valid mod)
                }
                
                var displayPath = string.IsNullOrEmpty(relativePath) 
                    ? fileName 
                    : Path.Combine(relativePath, fileName);

                modFiles.Add(new WadFile
                {
                    FullPath = file,
                    RelativePath = displayPath,
                    LastModified = File.GetLastWriteTime(file)
                });
            }
        }
        catch { } // Skip inaccessible directories
    }

    private void UpdateDmFlagsLists()
    {
        // DMFLAGS1 (split into three explicit columns to guarantee visual order)
        DmFlags1List.Clear();
        DmFlags1Col1.Clear();
        DmFlags1Col2.Clear();
        DmFlags1Col3.Clear();

        var sorted1 = NormalizeAndSortDmFlags(DmFlagsData.DmFlags1);
        int total1 = sorted1.Count;
        int columnCount = 3;
        int rows1 = (total1 + columnCount - 1) / columnCount;

        for (int col = 0; col < columnCount; col++)
        {
            for (int row = 0; row < rows1; row++)
            {
                int index = row + col * rows1;
                if (index < total1)
                {
                    var kvp = sorted1[index];
                    var flag = new DmFlag { Label = kvp.Key, BitValue = kvp.Value };
                    DmFlags1List.Add(flag);
                    if (col == 0) DmFlags1Col1.Add(flag);
                    else if (col == 1) DmFlags1Col2.Add(flag);
                    else DmFlags1Col3.Add(flag);
                }
            }
        }

        // DMFLAGS2
        DmFlags2List.Clear();
        var sorted2 = NormalizeAndSortDmFlags(DmFlagsData.DmFlags2);
        AddFlagsInColumnOrder(DmFlags2List, sorted2, 3);

        // DMFLAGS3
        DmFlags3List.Clear();
        var sorted3 = NormalizeAndSortDmFlags(DmFlagsData.DmFlags3);
        AddFlagsInColumnOrder(DmFlags3List, sorted3, 3);
    }

    private List<KeyValuePair<string, int>> NormalizeAndSortDmFlags(Dictionary<string, int> source)
    {
        return source
            .Select(kvp => new KeyValuePair<string, int>(kvp.Key.Trim(), kvp.Value))
            .OrderBy(kvp => kvp.Key, StringComparer.InvariantCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Adds items to the target list in an order that displays as column-major when rendered in a UniformGrid.
    /// Sorted items will read alphabetically down column 1, then column 2, then column 3.
    /// </summary>
    private void AddFlagsInColumnOrder(ObservableCollection<DmFlag> targetList, List<KeyValuePair<string, int>> sortedItems, int columnCount)
    {
        if (sortedItems.Count == 0) return;
        
        int rowCount = (sortedItems.Count + columnCount - 1) / columnCount;
        
        // UniformGrid fills left-to-right, row-by-row.
        // To display column-major order, we pick items: col * rowCount + row
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < columnCount; col++)
            {
                int sourceIndex = col * rowCount + row;
                if (sourceIndex < sortedItems.Count)
                {
                    var kvp = sortedItems[sourceIndex];
                    targetList.Add(new DmFlag { Label = kvp.Key, BitValue = kvp.Value });
                }
            }
        }
    }

    public async Task GetLocalIpAddress()
    {
        try
        {
            // Use .NET APIs for cross-platform local IP detection
            await Task.Run(() =>
            {
                string? localIp = null;
                
                // Method 1: Try to connect to external address and get local IP
                try
                {
                    using var socket = new System.Net.Sockets.Socket(
                        System.Net.Sockets.AddressFamily.InterNetwork,
                        System.Net.Sockets.SocketType.Dgram,
                        System.Net.Sockets.ProtocolType.Udp);
                    
                    // Connect to a public DNS (doesn't actually send data)
                    socket.Connect("8.8.8.8", 80);
                    
                    var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                    if (endPoint != null)
                    {
                        localIp = endPoint.Address.ToString();
                    }
                }
                catch
                {
                    // Method 2: Fallback to network interfaces
                    var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            localIp = ip.ToString();
                            break;
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(localIp))
                {
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        LocalIpAddress = localIp;
                        IpAddress = localIp; // Keep for backward compatibility
                    });
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get local IP: {ex.Message}");
        }
    }

    public async Task GetPublicIpAddress()
    {
        try
        {
            using var client = new HttpClient();
            var ip = await client.GetStringAsync("https://api.ipify.org");
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                PublicIpAddress = ip;
                IpAddress = ip; // Keep for backward compatibility
            });
        }
        catch { }
    }

    private bool IsFlatpakPath(string path, out string appId)
    {
        if (!string.IsNullOrEmpty(path) && path.StartsWith("flatpak:", StringComparison.OrdinalIgnoreCase))
        {
            appId = path.Substring("flatpak:".Length).Trim();
            return !string.IsNullOrEmpty(appId);
        }
        appId = "";
        return false;
    }

    private void LaunchGame()
    {
        if (SelectedBaseGame == null)
        {
            StatusMessage = "❌ Please select a base game (IWAD) first";
            StatusMessageColor = "Red";
            System.Diagnostics.Debug.WriteLine("No base game selected");
            return;
        }

        // Verify the IWAD file actually exists
        if (!File.Exists(SelectedBaseGame.FullPath))
        {
            StatusMessage = $"❌ IWAD file not found:\n{SelectedBaseGame.FullPath}\n\n" +
                          $"Data directory: {_wadDir}";
            StatusMessageColor = "Red";
            return;
        }

        // Disable launch button for 10 seconds to prevent double-launching
        IsLaunching = true;
        Task.Run(async () =>
        {
            await Task.Delay(10000);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsLaunching = false;
            });
        });

        StatusMessage = "🎮 Preparing to launch Doom...";
        StatusMessageColor = "Yellow";

        var args = new System.Collections.Generic.List<string>();
        var argsForFile = new System.Collections.Generic.List<string>();

        args.Add("-iwad");
        args.Add(SelectedBaseGame.FullPath);
        
        argsForFile.Add("-iwad");
        argsForFile.Add(SelectedBaseGame.RelativePath);

        
        if (SelectedModFiles.Any())
        {
            args.Add("-file");
            argsForFile.Add("-file");
            
            // Add mods in load order
            foreach (var mod in SelectedModFiles)
            {
                args.Add(mod.FullPath);
                argsForFile.Add(mod.RelativePath);
            }
        }

        args.Add("-skill");
        args.Add(((int)SelectedDifficulty).ToString());
        
        argsForFile.Add("-skill");
        argsForFile.Add(((int)SelectedDifficulty + 1).ToString()); // GZDoom uses 1-5

        // Handle starting map
        if (!string.IsNullOrEmpty(SelectedMapName))
        {
            // Check if it's ExMy format (E1M1) or MAPxx format
            if (SelectedMapName.StartsWith("E") && SelectedMapName.Length == 4)
            {
                // ExMy format: -warp episode map (e.g., E2M3 -> -warp 2 3)
                if (char.IsDigit(SelectedMapName[1]) && char.IsDigit(SelectedMapName[3]))
                {
                    args.Add("-warp");
                    args.Add(SelectedMapName[1].ToString()); // Episode number
                    args.Add(SelectedMapName[3].ToString()); // Map number
                    
                    argsForFile.Add("-warp");
                    argsForFile.Add(SelectedMapName[1].ToString());
                    argsForFile.Add(SelectedMapName[3].ToString());
                }
            }
            else if (SelectedMapName.StartsWith("MAP") && SelectedMapName.Length >= 5)
            {
                // MAPxx format: -warp xx (e.g., MAP07 -> -warp 7)
                var mapNum = SelectedMapName.Substring(3);
                if (int.TryParse(mapNum, out var mapNumber))
                {
                    args.Add("-warp");
                    args.Add(mapNumber.ToString());
                    
                    argsForFile.Add("-warp");
                    argsForFile.Add(mapNumber.ToString());
                }
            }
        }

        var dmFlags1 = DmFlags1List.Where(f => f.IsChecked).Sum(f => f.BitValue);
        var dmFlags2 = DmFlags2List.Where(f => f.IsChecked).Sum(f => f.BitValue);
        var dmFlags3 = DmFlags3List.Where(f => f.IsChecked).Sum(f => f.BitValue);

        if (dmFlags1 != 0)
        {
            args.Add("+DMFLAGS");
            args.Add(dmFlags1.ToString());
            argsForFile.Add("+set");
            argsForFile.Add("dmflags");
            argsForFile.Add(dmFlags1.ToString());
        }
        if (dmFlags2 != 0)
        {
            args.Add("+dmflags2");
            args.Add(dmFlags2.ToString());
            argsForFile.Add("+set");
            argsForFile.Add("dmflags2");
            argsForFile.Add(dmFlags2.ToString());
        }
        if (dmFlags3 != 0)
        {
            args.Add("+dmflags3");
            args.Add(dmFlags3.ToString());
            argsForFile.Add("+set");
            argsForFile.Add("dmflags3");
            argsForFile.Add(dmFlags3.ToString());
        }

        if (GameType != GameType.SinglePlayer)
        {
            if (GameType == GameType.Deathmatch)
            {
                args.Add("-deathmatch");
            }
            if (NetworkMode == Enums.NetworkMode.HostLAN || NetworkMode == Enums.NetworkMode.HostInternet)
            {
                args.Add("-host");
                args.Add(PlayerCount.ToString());
            }
        }

        if (ShowHexenClassSelection)
        {
            args.Add("+playerclass");
            args.Add(HexenClass.ToString().ToLower());
            argsForFile.Add("+playerclass");
            argsForFile.Add(HexenClass.ToString().ToLower());
        }

        if (AvgSwitch)
        {
            args.Add("-avg");
            argsForFile.Add("-avg");
        }
        if (FastSwitch)
        {
            args.Add("-fast");
            argsForFile.Add("-fast");
        }
        if (NoMonstersSwitch)
        {
            args.Add("-nomonsters");
            argsForFile.Add("-nomonsters");
        }
        if (RespawnSwitch)
        {
            args.Add("-respawn");
            argsForFile.Add("-respawn");
        }
        if (TimerSwitch)
        {
            args.Add("-timer");
            args.Add(TimerMinutes.ToString());
            argsForFile.Add("-timer");
            argsForFile.Add(TimerMinutes.ToString());
        }
        if (TurboSwitch)
        {
            args.Add("-turbo");
            args.Add(TurboSpeed.ToString());
            argsForFile.Add("-turbo");
            argsForFile.Add(TurboSpeed.ToString());
        }
                
        
        // Determine engine command and arguments
        string engineCmd = EngineExecutable;
        string[] engineArgs = args.ToArray();

        // Verify engine executable exists
        if (string.IsNullOrEmpty(engineCmd))
        {
            StatusMessage = "❌ Please select an engine executable first";
            StatusMessageColor = "Red";
            return;
        }

        if (IsBatocera)
        {
            // Anything needed for Batocera can be handled here
            // For now, try to do in the run script
        }

        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = false,  // Don't redirect - helps with window display
            RedirectStandardError = false,   // Don't redirect - helps with window display
            CreateNoWindow = false
        };

        // Check if this is a Flatpak engine
        if (IsFlatpakPath(engineCmd, out string flatpakAppId))
        {
            // Grant filesystem permission (if not already granted)
            GrantFlatpakPermissions(flatpakAppId, _wadDir);
            
            // Use flatpak run command
            psi.FileName = "flatpak";
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add(flatpakAppId);
            
            // Add game arguments
            foreach (var arg in engineArgs)
            {
                psi.ArgumentList.Add(arg);
            }
            
            System.Diagnostics.Debug.WriteLine($"Launching Flatpak: flatpak run {flatpakAppId} {string.Join(" ", engineArgs)}");
        }
        else
        {
            // Regular file-based engine
            if (!File.Exists(engineCmd))
            {
                StatusMessage = $"❌ Engine not found: {engineCmd}";
                StatusMessageColor = "Red";
                return;
            }
            
            psi.FileName = engineCmd;
            
            // Set working directory to where the engine executable is located
            var engineDir = Path.GetDirectoryName(engineCmd);
            if (!string.IsNullOrEmpty(engineDir) && Directory.Exists(engineDir))
            {
                psi.WorkingDirectory = engineDir;
            }

            foreach (var arg in engineArgs)
            {
                psi.ArgumentList.Add(arg);
            }
            
            System.Diagnostics.Debug.WriteLine($"Launching: {engineCmd} {string.Join(" ", engineArgs)}");
        }

        // Log the command for debugging
        var cmdLine = IsFlatpakPath(engineCmd, out var appId) 
            ? $"flatpak run {appId} {string.Join(" ", engineArgs)}"
            : $"{engineCmd} {string.Join(" ", engineArgs)}";
        System.Diagnostics.Debug.WriteLine($"Full command: {cmdLine}");

        try
        {
            var process = Process.Start(psi);
            if (process == null)
            {
                StatusMessage = "❌ Failed to start Doom engine process";
                StatusMessageColor = "Red";
                System.Diagnostics.Debug.WriteLine("Failed to start process");
            }
            else
            {
                // Don't wait for process to exit - let it run independently
                StatusMessage = $"✅ Doom launched! PID: {process.Id}\nCommand: {engineCmd}";
                StatusMessageColor = "LimeGreen";
                
                // Check if process is still running after a short delay
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    try
                    {
                        if (process.HasExited)
                        {
                            var stderr = await process.StandardError.ReadToEndAsync();
                            var stdout = await process.StandardOutput.ReadToEndAsync();
                            
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                StatusMessage = $"❌ Doom engine exited immediately (code: {process.ExitCode})\n" +
                                              $"Error: {stderr}\nOutput: {stdout}";
                                StatusMessageColor = "Red";
                            });
                            
                            System.Diagnostics.Debug.WriteLine($"Process exited with code: {process.ExitCode}");
                            System.Diagnostics.Debug.WriteLine($"StdErr: {stderr}");
                            System.Diagnostics.Debug.WriteLine($"StdOut: {stdout}");
                        }
                    }
                    catch { }
                });
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            StatusMessage = $"❌ Doom engine not found. Make sure {engineCmd} is installed.\nError: {ex.Message}";
            StatusMessageColor = "Red";
            System.Diagnostics.Debug.WriteLine($"Launch failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Launch error: {ex.Message}";
            StatusMessageColor = "Red";
            System.Diagnostics.Debug.WriteLine($"Launch error: {ex.Message}");
        }
    }

    private async Task ShowLaunchSummary()
    {
        if (SelectedBaseGame == null)
        {
            StatusMessage = "❌ Please select a base game (IWAD) first";
            StatusMessageColor = "Red";
            return;
        }

        // Build the arguments list (reuse logic from LaunchGame)
        var args = new System.Collections.Generic.List<string>();

        args.Add("-iwad");
        args.Add(SelectedBaseGame.FullPath);

        
        if (SelectedModFiles.Any())
        {
            args.Add("-file");
            foreach (var mod in SelectedModFiles)
            {
                args.Add(mod.FullPath);
            }
        }

        args.Add("-skill");
        args.Add(((int)SelectedDifficulty).ToString());

        // Handle starting map
        if (!string.IsNullOrEmpty(SelectedMapName))
        {
            // Check if it's ExMy format (E1M1) or MAPxx format
            if (SelectedMapName.StartsWith("E") && SelectedMapName.Length == 4)
            {
                // ExMy format: -warp episode map (e.g., E2M3 -> -warp 2 3)
                if (char.IsDigit(SelectedMapName[1]) && char.IsDigit(SelectedMapName[3]))
                {
                    args.Add("-warp");
                    args.Add(SelectedMapName[1].ToString()); // Episode number
                    args.Add(SelectedMapName[3].ToString()); // Map number
                }
            }
            else if (SelectedMapName.StartsWith("MAP") && SelectedMapName.Length >= 5)
            {
                // MAPxx format: -warp xx (e.g., MAP07 -> -warp 7)
                var mapNum = SelectedMapName.Substring(3);
                if (int.TryParse(mapNum, out var mapNumber))
                {
                    args.Add("-warp");
                    args.Add(mapNumber.ToString());
                }
            }
        }

        var dmFlags1 = DmFlags1List.Where(f => f.IsChecked).Sum(f => f.BitValue);
        var dmFlags2 = DmFlags2List.Where(f => f.IsChecked).Sum(f => f.BitValue);
        var dmFlags3 = DmFlags3List.Where(f => f.IsChecked).Sum(f => f.BitValue);

        if (dmFlags1 != 0)
        {
            args.Add("+DMFLAGS");
            args.Add(dmFlags1.ToString());
        }
        if (dmFlags2 != 0)
        {
            args.Add("+dmflags2");
            args.Add(dmFlags2.ToString());
        }
        if (dmFlags3 != 0)
        {
            args.Add("+dmflags3");
            args.Add(dmFlags3.ToString());
        }

        if (GameType != GameType.SinglePlayer)
        {
            if (GameType == GameType.Deathmatch)
            {
                args.Add("-deathmatch");
            }
            if (NetworkMode == Enums.NetworkMode.HostLAN || NetworkMode == Enums.NetworkMode.HostInternet)
            {
                args.Add("-host");
                args.Add(PlayerCount.ToString());
            }
        }

        if (ShowHexenClassSelection)
        {
            args.Add("+playerclass");
            args.Add(HexenClass.ToString().ToLower());
        }

        if (AvgSwitch) args.Add("-avg");
        if (FastSwitch) args.Add("-fast");
        if (NoMonstersSwitch) args.Add("-nomonsters");
        if (RespawnSwitch) args.Add("-respawn");
        if (TimerSwitch)
        {
            args.Add("-timer");
            args.Add(TimerMinutes.ToString());
        }
        if (TurboSwitch)
        {
            args.Add("-turbo");
            args.Add(TurboSpeed.ToString());
        }

        // Determine engine command
        string engineCmd = EngineExecutable;
        string[] engineArgs = args.ToArray();

        // Build summary text
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("🎮 LAUNCH CONFIGURATION SUMMARY");
        summary.AppendLine(new string('═', 60));
        summary.AppendLine();
        
        summary.AppendLine("📁 BASE GAME:");
        summary.AppendLine($"   {SelectedBaseGame.RelativePath}");
        summary.AppendLine();

        if (SelectedModFiles.Any())
        {
            summary.AppendLine("📦 MODS (Load Order):");
            foreach (var mod in SelectedModFiles)
            {
                summary.AppendLine($"   {mod.LoadOrder + 1}. {mod.RelativePath}");
            }
            summary.AppendLine();
        }

        summary.AppendLine("⚙️ GAME SETTINGS:");
        summary.AppendLine($"   Engine: {Path.GetFileName(EngineExecutable)}");
        summary.AppendLine($"   Difficulty: {SelectedDifficulty}");
        summary.AppendLine($"   Starting Map: {SelectedMapName}");
        summary.AppendLine($"   Game Type: {GameType}");
        if (GameType != GameType.SinglePlayer)
        {
            summary.AppendLine($"   Network Mode: {NetworkMode}");
            summary.AppendLine($"   Players: {PlayerCount}");
        }
        if (ShowHexenClassSelection)
        {
            summary.AppendLine($"   Hexen Class: {HexenClass}");
        }
        summary.AppendLine();

        var switches = new List<string>();
        if (AvgSwitch) switches.Add("AVG (20 min)");
        if (FastSwitch) switches.Add("Fast Monsters");
        if (NoMonstersSwitch) switches.Add("No Monsters");
        if (RespawnSwitch) switches.Add("Respawn");
        if (TimerSwitch) switches.Add($"Timer ({TimerMinutes} min)");
        if (TurboSwitch) switches.Add($"Turbo ({TurboSpeed}%)");

        if (switches.Any())
        {
            summary.AppendLine("🚀 RUN SWITCHES:");
            foreach (var sw in switches)
            {
                summary.AppendLine($"   • {sw}");
            }
            summary.AppendLine();
        }

        var activeDmFlags = new List<string>();
        if (dmFlags1 != 0) activeDmFlags.Add($"DMFLAGS: {dmFlags1}");
        if (dmFlags2 != 0) activeDmFlags.Add($"DMFLAGS2: {dmFlags2}");
        if (dmFlags3 != 0) activeDmFlags.Add($"DMFLAGS3: {dmFlags3}");

        if (activeDmFlags.Any())
        {
            summary.AppendLine("🏴 DMFLAGS:");
            foreach (var flag in activeDmFlags)
            {
                summary.AppendLine($"   • {flag}");
            }
            summary.AppendLine();
        }

        summary.AppendLine("💻 COMMAND LINE:");
        summary.AppendLine(new string('─', 60));
        var cmdLine = $"{engineCmd} {string.Join(" ", engineArgs)}";
        // Wrap long command line for readability
        if (cmdLine.Length > 80)
        {
            var words = cmdLine.Split(' ');
            var line = "";
            foreach (var word in words)
            {
                if (line.Length + word.Length + 1 > 80)
                {
                    summary.AppendLine($"   {line}");
                    line = word;
                }
                else
                {
                    line += (line.Length > 0 ? " " : "") + word;
                }
            }
            if (line.Length > 0)
                summary.AppendLine($"   {line}");
        }
        else
        {
            summary.AppendLine($"   {cmdLine}");
        }


        // Show dialog
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow : null;
            
            if (topLevel == null) return;

            var viewModel = new LaunchSummaryViewModel(summary.ToString());
            var dialog = new Views.LaunchSummaryWindow
            {
                DataContext = viewModel
            };

            // Wire up the close command in the parent ViewModel
            viewModel.CloseRequested += (_, _) => dialog.Close();

            await dialog.ShowDialog(topLevel);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error showing summary: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    private async Task AskToSavePresetConfig()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow : null;
            
            if (topLevel == null) return;

            // Create smart preset name
            var baseName = SelectedBaseGame?.RelativePath?.Replace(".wad", "") ?? "preset";
            var gameTypeStr = GameType == GameType.SinglePlayer ? "" : $"-{GameType.ToString().ToLower()}";
            var modCount = SelectedModFiles.Count;
            var modStr = modCount > 0 ? $"-{modCount}mods" : "";
            var defaultName = $"{baseName}{gameTypeStr}{modStr}.cfg";
            
            // Use modern StorageProvider API
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Configuration",
                SuggestedFileName = defaultName,
                DefaultExtension = "cfg",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Configuration File")
                    {
                        Patterns = new[] { "*.cfg" }
                    }
                },
                ShowOverwritePrompt = true
            });

            if (file == null) return;

            SaveConfigFile(file.Path.LocalPath);
            
            StatusMessage = $"✅ Configuration saved to: {file.Name}";
            StatusMessageColor = "LimeGreen";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Save error: {ex.Message}";
            StatusMessageColor = "Red";
            System.Diagnostics.Debug.WriteLine($"Save settings error: {ex.Message}");
        }
    }

    private async Task LoadSettings()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow : null;
            
            if (topLevel == null) return;

            // Use modern StorageProvider API
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Load Configuration",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Configuration File")
                    {
                        Patterns = new[] { "*.cfg" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count == 0) return;

            var file = files[0];
            LoadDefaultSettings(file.Path.LocalPath);
            
            StatusMessage = $"✅ Configuration loaded from: {file.Name}";
            StatusMessageColor = "LimeGreen";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Load error: {ex.Message}";
            StatusMessageColor = "Red";
            System.Diagnostics.Debug.WriteLine($"Load settings error: {ex.Message}");
        }
    }

    private void ApplyDmFlags(string key, int value)
    {
        var flagsList = key.ToLower() switch
        {
            "dmflags" => DmFlags1List,
            "dmflags2" => DmFlags2List,
            "dmflags3" => DmFlags3List,
            _ => null
        };

        if (flagsList == null) return;

        foreach (var flag in flagsList)
        {
            flag.IsChecked = (value & flag.BitValue) != 0;
        }
    }

    private async Task TestPortForwarding()
    {
        const int DoomPort = 5029;
        
        try
        {
            IsTestingPort = true;
            PortTestResult = "🔧 Checking UPnP support and UDP port 5029...";
            
            var sb = new System.Text.StringBuilder();
            NatDevice? natDevice = null;
            bool upnpSupported = false;
            Mapping? existingMapping = null;
            
            // Try to discover UPnP device
            try
            {
                PortTestResult = "🔍 Discovering UPnP-enabled router...";
                
                var discoverer = new NatDiscoverer();
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                natDevice = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
                
                if (natDevice != null)
                {
                    upnpSupported = true;
                    PortTestResult = "✅ UPnP router found! Checking port mappings...";
                    
                    // Get external IP from router
                    var externalIp = await natDevice.GetExternalIPAsync();
                    if (externalIp != null)
                    {
                        PublicIpAddress = externalIp.ToString();
                    }
                    
                    // Check if mapping already exists
                    try
                    {
                        existingMapping = await natDevice.GetSpecificMappingAsync(Protocol.Udp, DoomPort);
                    }
                    catch
                    {
                        // No existing mapping
                    }
                }
            }
            catch (NatDeviceNotFoundException)
            {
                upnpSupported = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UPnP discovery failed: {ex.Message}");
                upnpSupported = false;
            }
            
            // Get IPs if not already set
            if (string.IsNullOrEmpty(PublicIpAddress))
            {
                await GetPublicIpAddress();
            }
            
            // Build results
            sb.AppendLine($"🌐 Public IP: {(string.IsNullOrEmpty(PublicIpAddress) ? "Unknown" : PublicIpAddress)}");
            sb.AppendLine($"🏠 Local IP: {LocalIpAddress}");
            sb.AppendLine();
            
            if (upnpSupported && natDevice != null)
            {
                sb.AppendLine("📡 UPnP: ✅ Supported by your router!");
                sb.AppendLine();
                
                if (existingMapping != null)
                {
                    sb.AppendLine($"✅ UDP port {DoomPort} is already forwarded!");
                    sb.AppendLine($"   → {existingMapping.PrivateIP}:{existingMapping.PrivatePort}");
                    sb.AppendLine($"   Description: {existingMapping.Description}");
                    sb.AppendLine();
                    sb.AppendLine("Your router is configured for Doom multiplayer hosting.");
                }
                else
                {
                    sb.AppendLine($"ℹ️ UDP port {DoomPort} is NOT currently forwarded.");
                    sb.AppendLine();
                    sb.AppendLine("Would you like to automatically open the port?");
                    sb.AppendLine("Click 'Auto-Configure Port' below to set up UPnP forwarding.");
                }
            }
            else
            {
                sb.AppendLine("📡 UPnP: ❌ Not available or disabled");
                sb.AppendLine();
                sb.AppendLine("Your router either doesn't support UPnP or has it disabled.");
                sb.AppendLine();
                sb.AppendLine("To host internet games, manually configure port forwarding:");
                sb.AppendLine();
                sb.AppendLine("1. Log into your router (usually http://192.168.1.1)");
                sb.AppendLine("2. Find 'Port Forwarding' or 'NAT' settings");
                sb.AppendLine("3. Create a new rule:");
                sb.AppendLine($"   • External Port: {DoomPort}");
                sb.AppendLine($"   • Internal IP: {LocalIpAddress}");
                sb.AppendLine($"   • Internal Port: {DoomPort}");
                sb.AppendLine("   • Protocol: UDP");
                sb.AppendLine("4. Save and test again");
                sb.AppendLine();
                sb.AppendLine("Or enable UPnP in your router settings for automatic configuration.");
            }
            
            PortTestResult = sb.ToString();
        }
        catch (Exception ex)
        {
            PortTestResult = $"❌ Test error: {ex.Message}\n\n" +
                           "Port forwarding requirements for Doom:\n" +
                           "• UDP port 5029 forwarded to your local IP\n" +
                           "• Firewall allows incoming UDP on port 5029";
            System.Diagnostics.Debug.WriteLine($"Port test error: {ex}");
        }
        finally
        {
            IsTestingPort = false;
        }
    }
    
    private async Task AutoConfigurePort()
    {
        const int DoomPort = 5029;
        
        try
        {
            IsTestingPort = true;
            PortTestResult = "🔧 Configuring UPnP port forwarding...";
            
            var discoverer = new NatDiscoverer();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var natDevice = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            
            if (natDevice == null)
            {
                PortTestResult = "❌ Could not find UPnP-enabled router.\n\n" +
                               "Make sure UPnP is enabled in your router settings.";
                return;
            }
            
            PortTestResult = "📡 Creating UDP port mapping...";
            
            // Create the port mapping
            var mapping = new Mapping(
                Protocol.Udp,
                DoomPort,
                DoomPort,
                "Doom Multiplayer (DMINLauncher)"
            );
            
            await natDevice.CreatePortMapAsync(mapping);
            
            // Verify the mapping was created
            var verifyMapping = await natDevice.GetSpecificMappingAsync(Protocol.Udp, DoomPort);
            
            if (verifyMapping != null)
            {
                var externalIp = await natDevice.GetExternalIPAsync();
                
                PortTestResult = $"✅ SUCCESS! UDP port {DoomPort} is now forwarded!\n\n" +
                               $"🌐 External: {externalIp}:{DoomPort}\n" +
                               $"🏠 Internal: {verifyMapping.PrivateIP}:{verifyMapping.PrivatePort}\n\n" +
                               "Other players can now connect to your games.\n\n" +
                               "Note: This mapping may expire when your router restarts.\n" +
                               "Run this again if needed after a router reboot.";
            }
            else
            {
                PortTestResult = "⚠️ Port mapping was created but could not be verified.\n\n" +
                               "Try testing connectivity with another player.";
            }
        }
        catch (MappingException ex)
        {
            PortTestResult = $"❌ Could not create port mapping.\n\n" +
                           $"Error: {ex.Message}\n\n" +
                           "The port may already be in use or your router\n" +
                           "may not allow this operation.";
        }
        catch (Exception ex)
        {
            PortTestResult = $"❌ Auto-configure failed: {ex.Message}\n\n" +
                           "Try manual port forwarding instead.";
            System.Diagnostics.Debug.WriteLine($"Auto-configure error: {ex}");
        }
        finally
        {
            IsTestingPort = false;
        }
    }

    private void ResetSettings()
    {
        // Store current selected IWAD path to preserve it (not the object reference, since RefreshWadFiles will clear the collection)
        var currentSelectedIwadPath = SelectedBaseGame?.RelativePath;
        
        // Set Batocera-specific defaults if running on Batocera
        if (IsBatocera)
        {
            // Set default engine path for Batocera
            _engineFilePath = Path.Exists("/usr/bin/gzdoom") ? "/usr/bin/gzdoom" : "";
            UseFilePathEngine = true;
            this.RaisePropertyChanged(nameof(EngineExecutable));
            
            // Set default WADs directory for Batocera
            _wadDir = Path.Exists("/userdata/roms/gzdoom") ? "/userdata/roms/gzdoom" : "";
            DataDirectory = _wadDir;
        }
        // For non-Batocera systems, don't touch the paths at all

        if (!string.IsNullOrEmpty(_wadDir))
        {
            RefreshWadFiles();
            RefreshModFiles();
        }

        // Reset game settings to defaults
        SelectedDifficulty = Difficulty.Normal;
        GameType = GameType.SinglePlayer;
        PlayerCount = 1;
        HexenClass = HexenClass.Fighter;
        NetworkMode = Enums.NetworkMode.None;
        IpAddress = "";
        
        // Reset switches
        AvgSwitch = false;
        FastSwitch = false;
        NoMonstersSwitch = false;
        RespawnSwitch = false;
        TimerSwitch = false;
        TimerMinutes = 20;
        TurboSwitch = false;
        TurboSpeed = 100;
        
        // Reset mod selections
        foreach (var mod in ModFiles)
        {
            mod.IsSelected = false;
        }
        SelectedModFiles.Clear();
        
        // Reset DMFLAGS
        foreach (var flag in DmFlags1List) flag.IsChecked = false;
        foreach (var flag in DmFlags2List) flag.IsChecked = false;
        foreach (var flag in DmFlags3List) flag.IsChecked = false;
        
        // Restore the selected IWAD by finding it in the refreshed collection
        if (!string.IsNullOrEmpty(currentSelectedIwadPath))
        {
            SelectedBaseGame = WadFiles.FirstOrDefault(w => 
                w.RelativePath.Equals(currentSelectedIwadPath, StringComparison.OrdinalIgnoreCase));
        }
        
        // Set the starting map AFTER restoring the IWAD (so it doesn't get overwritten by the SelectedBaseGame setter)
        SelectedMapName = "MAP01";
        
        StatusMessage = IsBatocera 
            ? "✅ Settings reset to Batocera defaults (paths and IWAD preserved)" 
            : "✅ Settings reset to defaults (paths and IWAD preserved)";
        StatusMessageColor = "LimeGreen";
    }

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
        
        // Re-insert into ModFiles in alphabetical order
        var index = 0;
        while (index < ModFiles.Count && 
               string.Compare(ModFiles[index].RelativePath, mod.RelativePath, StringComparison.OrdinalIgnoreCase) < 0)
        {
            index++;
        }
        ModFiles.Insert(index, mod);
        
        // Update load order indices
        for (int i = 0; i < SelectedModFiles.Count; i++)
        {
            SelectedModFiles[i].LoadOrder = i;
        }
        
        SelectedLoadOrderMod = SelectedModFiles.FirstOrDefault();
    }

    private void MoveModUp()
    {
        if (SelectedLoadOrderMod == null) return;
        
        var index = SelectedModFiles.IndexOf(SelectedLoadOrderMod);
        if (index <= 0) return;
        
        var mod = SelectedLoadOrderMod;
        SelectedModFiles.RemoveAt(index);
        SelectedModFiles.Insert(index - 1, mod);
        
        // Update load order indices
        for (int i = 0; i < SelectedModFiles.Count; i++)
        {
            SelectedModFiles[i].LoadOrder = i;
        }
        
        SelectedLoadOrderMod = mod;
    }

    private void MoveModDown()
    {
        if (SelectedLoadOrderMod == null) return;
        
        var index = SelectedModFiles.IndexOf(SelectedLoadOrderMod);
        if (index < 0 || index >= SelectedModFiles.Count - 1) return;
        
        var mod = SelectedLoadOrderMod;
        SelectedModFiles.RemoveAt(index);
        SelectedModFiles.Insert(index + 1, mod);
        
        // Update load order indices
        for (int i = 0; i < SelectedModFiles.Count; i++)
        {
            SelectedModFiles[i].LoadOrder = i;
        }
        
        SelectedLoadOrderMod = mod;
    }

    /// <summary>
    /// Shows a text input dialog for entering a directory path manually.
    /// Used as fallback when native folder picker is unavailable.
    /// </summary>
    private async Task<string?> ShowTextInputDialog(Avalonia.Controls.Window parent, string title, string prompt, string currentValue, string watermark)
    {
        string? result = null;
        
        var dialog = new Avalonia.Controls.Window
        {
            Title = title,
            Width = 600,
            Height = 200,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var textBox = new Avalonia.Controls.TextBox
        {
            Text = currentValue,
            Watermark = watermark,
            Margin = new Avalonia.Thickness(10)
        };

        var okButton = new Avalonia.Controls.Button
        {
            Content = "OK",
            Width = 100,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(5)
        };

        var cancelButton = new Avalonia.Controls.Button
        {
            Content = "Cancel",
            Width = 100,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(5)
        };

        okButton.Click += (s, e) =>
        {
            result = textBox.Text;
            dialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            dialog.Close();
        };

        var buttonPanel = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Children = { cancelButton, okButton }
        };

        var mainPanel = new Avalonia.Controls.StackPanel
        {
            Margin = new Avalonia.Thickness(10),
            Spacing = 10,
            Children =
            {
                new Avalonia.Controls.TextBlock
                {
                    Text = prompt,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                },
                new Avalonia.Controls.TextBlock
                {
                    Text = "Current: " + currentValue,
                    FontSize = 11,
                    Foreground = Avalonia.Media.Brushes.Gray
                },
                textBox,
                buttonPanel
            }
        };

        dialog.Content = mainPanel;
        await dialog.ShowDialog(parent);
        
        return result;
    }

    /// <summary>
    /// Attempts to open the native folder picker. Returns null if cancelled or if picker fails.
    /// </summary>
    private async Task<string?> TryOpenFolderPicker(Avalonia.Controls.Window parent, string title, string? suggestedStartPath = null)
    {
        try
        {
            var folderDialog = new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            };
            
            // Set suggested start location if path exists
            if (!string.IsNullOrWhiteSpace(suggestedStartPath) && Directory.Exists(suggestedStartPath))
            {
                try
                {
                    var uri = new Uri(suggestedStartPath);
                    folderDialog.SuggestedStartLocation = await parent.StorageProvider.TryGetFolderFromPathAsync(uri);
                }
                catch
                {
                    // If we can't get the folder, just continue without a start location
                }
            }

            var folders = await parent.StorageProvider.OpenFolderPickerAsync(folderDialog);
            if (folders.Count > 0)
            {
                return folders[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            // Folder picker not supported on this platform
            System.Diagnostics.Debug.WriteLine($"Folder picker failed: {ex.Message}");
        }
        
        return null;
    }

    private async Task ChangeDataDirectory()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow : null;
            
            if (topLevel == null)
            {
                StatusMessage = "❌ Cannot access main window";
                StatusMessageColor = "Red";
                return;
            }

            var result = await TryOpenFolderPicker(topLevel, "Select WADs & Mods Directory", _wadDir);

            if (!string.IsNullOrWhiteSpace(result))
            {
                // Validate directory exists
                if (!Directory.Exists(result))
                {
                    StatusMessage = $"⚠️ Directory does not exist: {result}";
                    StatusMessageColor = "Orange";
                    return;
                }

                // Check if Flatpak is currently selected
                if (IsFlatpakPath(EngineExecutable, out _))
                {
                    // Warn about permission requirement with new directory
                    var oldWadDir = _wadDir;
                    _wadDir = result; // Temporarily set for warning message
                    
                    var confirmed = await ShowFlatpakPermissionWarning(topLevel);
                    
                    if (!confirmed)
                    {
                        _wadDir = oldWadDir; // Restore old directory
                        
                        // Clear the Flatpak selection
                        EngineExecutable = "";
                        UseFilePathEngine = true;
                        
                        StatusMessage = "⚠️ WADs directory change cancelled. Flatpak engine cleared.";
                        StatusMessageColor = "Orange";
                        return;
                    }
                }

                _wadDir = result;
                DataDirectory = result;
                RefreshWadFiles();
                RefreshModFiles();
                SaveDefaultConfig();
                
                StatusMessage = $"✅ WADs directory changed to: {result}";
                StatusMessageColor = "LimeGreen";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
            StatusMessageColor = "Red";
            System.Diagnostics.Debug.WriteLine($"Change directory error: {ex}");
        }
    }

    private async Task ChangeEngineExecutable()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow : null;
            
            if (topLevel == null)
            {
                StatusMessage = "❌ Cannot access main window";
                StatusMessageColor = "Red";
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Select Engine Executable",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Doom Engine")
                    {
                        Patterns = OperatingSystem.IsWindows() ? new[] { "*.exe" } : new[] { "*" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var selectedFile = files[0];
                var path = selectedFile.Path.LocalPath;

                // Validate file exists
                if (!File.Exists(path))
                {
                    StatusMessage = $"⚠️ File does not exist: {path}";
                    StatusMessageColor = "Orange";
                    return;
                }

                EngineExecutable = path;
                SaveDefaultConfig();
                
                StatusMessage = $"✅ Engine set to: {Path.GetFileName(path)}";
                StatusMessageColor = "LimeGreen";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
            StatusMessageColor = "Red";
            System.Diagnostics.Debug.WriteLine($"Change engine executable error: {ex}");
        }
    }

    private void UpdateDifficultyNames()
    {
        DifficultyOptions.Clear();

        if (SelectedBaseGame == null)
        {
            // Generic difficulty names
            DifficultyOptions.Add("Very Easy");
            DifficultyOptions.Add("Easy");
            DifficultyOptions.Add("Normal");
            DifficultyOptions.Add("Hard");
            DifficultyOptions.Add("Nightmare");
            return;
        }

        var gameName = SelectedBaseGame.RelativePath.ToLowerInvariant();

        if (gameName.Contains("doom2") || gameName.Contains("plutonia") || gameName.Contains("tnt") || 
            gameName.Contains("doom.wad") || gameName.Contains("doom1"))
        {
            // Doom / Doom 2 names
            DifficultyOptions.Add("I'm Too Young To Die");
            DifficultyOptions.Add("Hey, Not Too Rough");
            DifficultyOptions.Add("Hurt Me Plenty");
            DifficultyOptions.Add("Ultra-Violence");
            DifficultyOptions.Add("Nightmare!");
        }
        else if (gameName.Contains("heretic"))
        {
            // Heretic names
            DifficultyOptions.Add("Thou Needeth A Wet-Nurse");
            DifficultyOptions.Add("Yellowbellies-R-Us");
            DifficultyOptions.Add("Bringest Them Oneth");
            DifficultyOptions.Add("Thou Art A Smite-Meister");
            DifficultyOptions.Add("Black Plague Possesses Thee");
        }
        else if (gameName.Contains("hexen"))
        {
            // Hexen names
            DifficultyOptions.Add("Training");
            DifficultyOptions.Add("Squire");
            DifficultyOptions.Add("Knight");
            DifficultyOptions.Add("Warrior");
            DifficultyOptions.Add("Titan");
        }
        else
        {
            // Generic for unknown games
            DifficultyOptions.Add("Very Easy");
            DifficultyOptions.Add("Easy");
            DifficultyOptions.Add("Normal");
            DifficultyOptions.Add("Hard");
            DifficultyOptions.Add("Nightmare");
        }
    }

    private async Task AddCustomFlatpak()
    {
        if (!OperatingSystem.IsLinux())
        {
            StatusMessage = "⚠️ Flatpak is only available on Linux";
            StatusMessageColor = "Orange";
            return;
        }

        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow : null;
            
            if (topLevel == null) return;

            StatusMessage = "🔍 Scanning for Flatpak applications...";
            StatusMessageColor = "Yellow";

            // Get all installed Flatpak apps
            var psi = new ProcessStartInfo
            {
                FileName = "flatpak",
                Arguments = "list --app --columns=application,name",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            if (proc == null)
            {
                StatusMessage = "❌ Failed to run flatpak command";
                StatusMessageColor = "Red";
                return;
            }

            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (string.IsNullOrWhiteSpace(output))
            {
                StatusMessage = "ℹ️ No Flatpak applications found";
                StatusMessageColor = "Orange";
                return;
            }

            // Parse the output into app ID and name pairs
            var flatpakApps = new List<(string appId, string name)>();
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('\t', 2);
                if (parts.Length >= 2)
                {
                    flatpakApps.Add((parts[0].Trim(), parts[1].Trim()));
                }
                else if (parts.Length == 1 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    var appId = parts[0].Trim();
                    flatpakApps.Add((appId, appId));
                }
            }

            if (flatpakApps.Count == 0)
            {
                StatusMessage = "ℹ️ No Flatpak applications found";
                StatusMessageColor = "Orange";
                return;
            }

            // Create selection dialog
            string? selectedAppId = null;
            var dialog = new Avalonia.Controls.Window
            {
                Title = "Select Flatpak Application",
                Width = 450,
                Height = 350,
                CanResize = true
            };

            var listBox = new Avalonia.Controls.ListBox
            {
                Margin = new Avalonia.Thickness(10),
                ItemsSource = flatpakApps
            };

            listBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<(string appId, string name)>(
                (app, _) => new Avalonia.Controls.StackPanel
                {
                    Children =
                    {
                        new Avalonia.Controls.TextBlock
                        {
                            Text = app.name,
                            FontWeight = Avalonia.Media.FontWeight.SemiBold,
                            FontSize = 13
                        },
                        new Avalonia.Controls.TextBlock
                        {
                            Text = app.appId,
                            FontSize = 10,
                            Foreground = Avalonia.Media.Brushes.Gray
                        }
                    }
                });

            var okButton = new Avalonia.Controls.Button
            {
                Content = "Add Selected",
                Width = 120,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(5)
            };

            var cancelButton = new Avalonia.Controls.Button
            {
                Content = "Cancel",
                Width = 100,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(5),
                IsCancel = true  // Makes ESC key close the dialog
            };

            okButton.Click += (s, e) =>
            {
                if (listBox.SelectedItem is (string appId, string name))
                {
                    selectedAppId = appId;
                    dialog.Close();
                }
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.Close();
            };

            var buttonPanel = new Avalonia.Controls.StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(10),
                Spacing = 5,
                Children = { cancelButton, okButton }
            };

            var mainPanel = new Avalonia.Controls.DockPanel
            {
                Margin = new Avalonia.Thickness(10),
                Children =
                {
                    new Avalonia.Controls.TextBlock
                    {
                        [Avalonia.Controls.DockPanel.DockProperty] = Avalonia.Controls.Dock.Top,
                        Text = $"Select a Flatpak application to use as an engine ({flatpakApps.Count} found):",
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        Margin = new Avalonia.Thickness(0, 0, 0, 10)
                    },
                    buttonPanel,
                    listBox
                }
            };

            Avalonia.Controls.DockPanel.SetDock(buttonPanel, Avalonia.Controls.Dock.Bottom);

            dialog.Content = mainPanel;
            await dialog.ShowDialog(topLevel);

            // Add the selected Flatpak to engine list
            if (!string.IsNullOrEmpty(selectedAppId))
            {
                var selectedApp = flatpakApps.First(a => a.appId == selectedAppId);
                var displayName = $"{selectedApp.name} (flatpak)";
                var enginePath = $"flatpak:{selectedAppId}";

                // Show permission warning
                var confirmed = await ShowFlatpakPermissionWarning(topLevel);
                
                if (!confirmed)
                {
                    StatusMessage = "❌ Flatpak selection cancelled";
                    StatusMessageColor = "Orange";
                    return;
                }

                // Set the engine executable to the flatpak path
                EngineExecutable = enginePath;
                SaveDefaultConfig();
                
                StatusMessage = $"✅ Set engine to {displayName}";
                StatusMessageColor = "LimeGreen";
            }
            else
            {
                StatusMessage = "";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error scanning Flatpaks: {ex.Message}";
            StatusMessageColor = "Red";
            System.Diagnostics.Debug.WriteLine($"Add custom Flatpak error: {ex}");
        }
    }

    private async Task<bool> ShowFlatpakPermissionWarning(Avalonia.Controls.Window parent)
    {
        var warningDialog = new Avalonia.Controls.Window
        {
            Title = "Flatpak Permissions",
            Width = 550,
            Height = 280,
            CanResize = false
        };

        var result = false;
        
        var messageText = new Avalonia.Controls.TextBlock
        {
            Text = "ℹ️ Flatpak Permissions\n\n" +
                   $"The Flatpak application will be granted temporary access to:\n" +
                   $"  • Your WADs folder: {_wadDir}\n" +
                   $"  • Display (X11/Wayland)\n" +
                   $"  • GPU device (DRI)\n" +
                   $"  • Audio (PulseAudio)\n\n" +
                   "These permissions are temporary and only active while the game is running.\n" +
                   "They are automatically removed when you close the game.\n\n" +
                   "Do you want to proceed with this Flatpak selection?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(15),
            FontSize = 12
        };

        var buttonPanel = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(15, 0, 15, 15),
            Spacing = 10
        };

        var okButton = new Avalonia.Controls.Button
        {
            Content = "OK, Proceed",
            Width = 120,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#406020"))
        };

        var cancelButton = new Avalonia.Controls.Button
        {
            Content = "Cancel",
            Width = 100,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#604040")),
            IsCancel = true  // Makes ESC key close the dialog
        };

        okButton.Click += (s, e) =>
        {
            result = true;
            warningDialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            result = false;
            warningDialog.Close();
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);

        var mainPanel = new Avalonia.Controls.DockPanel
        {
            Children = { buttonPanel, messageText }
        };

        Avalonia.Controls.DockPanel.SetDock(buttonPanel, Avalonia.Controls.Dock.Bottom);

        warningDialog.Content = mainPanel;
        await warningDialog.ShowDialog(parent);

        return result;
    }

    private async void InitializeNetworkInfo()
    {
        // Fetch IP addresses based on the loaded NetworkMode
        switch (NetworkMode)
        {
            case Enums.NetworkMode.HostLAN:
                await GetLocalIpAddress();
                break;
            case Enums.NetworkMode.HostInternet:
                await GetLocalIpAddress();
                await GetPublicIpAddress();
                break;
            case Enums.NetworkMode.None:
            case Enums.NetworkMode.Connect:
                // No IP fetching needed for these modes on startup
                break;
        }
    }

    

    private void GrantFlatpakPermissions(string appId, string wadPath)
    {
        try
        {
            if (string.IsNullOrEmpty(wadPath) || !Directory.Exists(wadPath))
            {
                System.Diagnostics.Debug.WriteLine($"Cannot grant Flatpak permissions: Invalid WAD directory '{wadPath}'");
                return;
            }
            
            // Grant read-only filesystem access to WAD directory
            var psi = new ProcessStartInfo
            {
                FileName = "flatpak",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            psi.ArgumentList.Add("override");
            psi.ArgumentList.Add("--user");
            psi.ArgumentList.Add($"--filesystem={wadPath}:ro");
            psi.ArgumentList.Add(appId);
            
            var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Granted Flatpak {appId} access to {wadPath}");
                }
                else
                {
                    var stderr = process.StandardError.ReadToEnd();
                    System.Diagnostics.Debug.WriteLine($"Failed to grant Flatpak permissions: {stderr}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error granting Flatpak permissions: {ex.Message}");
        }
    }

    private void ZoomIn()
    {
        // Increase zoom by 5%, max 200%
        if (ZoomLevel < 200)
        {
            ZoomLevel = Math.Min(200, ZoomLevel + 5);
        }
    }

    private void ZoomOut()
    {
        // Decrease zoom by 5%, min 50%
        if (ZoomLevel > 50)
        {
            ZoomLevel = Math.Max(50, ZoomLevel - 5);
        }
    }

    private void ResetZoom()
    {
        ZoomLevel = 100;
    }

    private async Task ShowExitConfirmation()
    {
        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow : null;
        
        if (topLevel == null) return;

        var confirmDialog = new Avalonia.Controls.Window
        {
            Title = "Exit Application?",
            Width = 450,
            Height = 200,
            CanResize = false
        };

        string? result = null;
        
        var messageText = new Avalonia.Controls.TextBlock
        {
            Text = "Do you want to exit DMINLauncher?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(20),
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var buttonPanel = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(20, 10, 20, 20),
            Spacing = 10
        };

        var cancelButton = new Avalonia.Controls.Button
        {
            Content = "Cancel",
            Width = 120,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#404040")),
            IsCancel = true // Makes Escape key press this button
        };

        var exitButton = new Avalonia.Controls.Button
        {
            Content = "Exit",
            Width = 120,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#604040"))
        };

        var exitAndSaveButton = new Avalonia.Controls.Button
        {
            Content = "Exit and Save",
            Width = 120,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#406020")),
            IsDefault = true // Makes Enter key press this button
        };

        cancelButton.Click += (s, e) =>
        {
            result = "cancel";
            confirmDialog.Close();
        };

        exitButton.Click += (s, e) =>
        {
            result = "exit";
            confirmDialog.Close();
        };

        exitAndSaveButton.Click += (s, e) =>
        {
            result = "save";
            confirmDialog.Close();
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(exitButton);
        buttonPanel.Children.Add(exitAndSaveButton);

        var mainPanel = new Avalonia.Controls.StackPanel
        {
            Children = { messageText, buttonPanel }
        };

        confirmDialog.Content = mainPanel;
        
        // Set focus to Exit and Save button when dialog opens
        confirmDialog.Opened += (s, e) =>
        {
            exitAndSaveButton.Focus();
        };
        
        await confirmDialog.ShowDialog(topLevel);

        if (result == "save")
        {
            // Save settings before exiting
            SaveDefaultConfig();
            ExitApplication();
        }
        else if (result == "exit")
        {
            // Exit without saving
            ExitApplication();
        }
        // If cancel or null, do nothing (stay in app)
    }

    private void ExitApplication()
    {
        // Just trigger shutdown - OnClosed will handle cleanup automatically
        if (Avalonia.Application.Current?.ApplicationLifetime is 
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void LoadMapsFromIWAD(WadFile iwad)
    {
        try
        {
            AvailableMaps.Clear();
            
            var wadInfo = Services.WadParser.Parse(iwad.FullPath);
            
            if (wadInfo.IsValid && wadInfo.MapNames.Count > 0)
            {
                foreach (var mapName in wadInfo.MapNames)
                {
                    AvailableMaps.Add(mapName);
                }
                
                // Auto-select first map if nothing is selected
                if (string.IsNullOrEmpty(SelectedMapName) && AvailableMaps.Count > 0)
                {
                    SelectedMapName = AvailableMaps[0];
                }
                // If current selection doesn't exist in new IWAD, reset to first
                else if (!AvailableMaps.Contains(SelectedMapName) && AvailableMaps.Count > 0)
                {
                    SelectedMapName = AvailableMaps[0];
                }
            }
            
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load maps from IWAD: {ex.Message}");
            // Add default map as fallback
            AvailableMaps.Add("E1M1");
            SelectedMapName = "E1M1";
        }
    }

    private async Task SaveGZDoomConfig()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow : null;
            
            if (topLevel == null) return;

            // Try to set the start location to the gzdoom folder if we're on Batocera
            var startLocation = IsBatocera 
                ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri("/userdata/roms/gzdoom"))
                : null;

            // Use modern StorageProvider API to save to the gzdoom folder
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Batocera GZDoom Configuration",
                SuggestedFileName = "My Game.gzdoom",
                DefaultExtension = "gzdoom",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Batocera GZDoom Config")
                    {
                        Patterns = new[] { "*.gzdoom" }
                    }
                },
                ShowOverwritePrompt = true,
                SuggestedStartLocation = startLocation
            });

            if (file == null) return;

            var args = new System.Collections.Generic.List<string>();

            // Base IWAD
            if (SelectedBaseGame != null)
            {
                args.Add("-iwad");
                args.Add(SelectedBaseGame.RelativePath);
            }

            // Mods in load order
            if (SelectedModFiles.Any())
            {
                args.Add("-file");
                foreach (var mod in SelectedModFiles)
                {
                    args.Add(mod.RelativePath);
                }
            }

            // Difficulty
            args.Add("-skill");
            args.Add(((int)SelectedDifficulty + 1).ToString()); // GZDoom uses 1-5, not 0-4

            // Starting map
            if (!string.IsNullOrEmpty(SelectedMapName))
            {
                // Check if it's ExMy format (E1M1) or MAPxx format
                if (SelectedMapName.StartsWith("E") && SelectedMapName.Length == 4)
                {
                    // ExMy format: -warp episode map (e.g., E2M3 -> -warp 2 3)
                    if (char.IsDigit(SelectedMapName[1]) && char.IsDigit(SelectedMapName[3]))
                    {
                        args.Add("-warp");
                        args.Add(SelectedMapName[1].ToString()); // Episode number
                        args.Add(SelectedMapName[3].ToString()); // Map number
                    }
                }
                else if (SelectedMapName.StartsWith("MAP") && SelectedMapName.Length >= 5)
                {
                    // MAPxx format: -warp xx (e.g., MAP07 -> -warp 7)
                    var mapNum = SelectedMapName.Substring(3);
                    if (int.TryParse(mapNum, out var mapNumber))
                    {
                        args.Add("-warp");
                        args.Add(mapNumber.ToString());
                    }
                }
            }

            // DMFLAGS
            var dmFlags1 = DmFlags1List.Where(f => f.IsChecked).Sum(f => f.BitValue);
            var dmFlags2 = DmFlags2List.Where(f => f.IsChecked).Sum(f => f.BitValue);
            var dmFlags3 = DmFlags3List.Where(f => f.IsChecked).Sum(f => f.BitValue);

            if (dmFlags1 != 0)
            {
                args.Add("+set");
                args.Add("dmflags");
                args.Add(dmFlags1.ToString());
            }
            if (dmFlags2 != 0)
            {
                args.Add("+set");
                args.Add("dmflags2");
                args.Add(dmFlags2.ToString());
            }
            if (dmFlags3 != 0)
            {
                args.Add("+set");
                args.Add("dmflags3");
                args.Add(dmFlags3.ToString());
            }

            // Run switches
            if (AvgSwitch) args.Add("-avg");
            if (FastSwitch) args.Add("-fast");
            if (NoMonstersSwitch) args.Add("-nomonsters");
            if (RespawnSwitch) args.Add("-respawn");
            if (TimerSwitch)
            {
                args.Add("-timer");
                args.Add(TimerMinutes.ToString());
            }
            if (TurboSwitch)
            {
                args.Add("-turbo");
                args.Add(TurboSpeed.ToString());
            }

            // Hexen class
            if (ShowHexenClassSelection)
            {
                args.Add("+playerclass");
                args.Add(HexenClass.ToString().ToLower());
            }

            // Write to file (single line as per Batocera requirements)
            using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(string.Join(" ", args));
            
            StatusMessage = $"✅ Batocera config saved: {file.Name}";
            StatusMessageColor = "LimeGreen";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Save error: {ex.Message}";
            StatusMessageColor = "Red";
            System.Diagnostics.Debug.WriteLine($"Save Batocera config error: {ex.Message}");
        }
    }
}

