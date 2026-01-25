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
using DMINLauncher.Data;
using DMINLauncher.Enums;
using DMINLauncher.Models;
using Open.Nat;
using ReactiveUI;

namespace DMINLauncher.ViewModels;


public class MainWindowViewModel : ReactiveObject
{
    private string _wadDir = "";
    private string _engineExecutable = "";
    private LauncherSettings _settings = new();

    // Application version
    public string AppVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.2";
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
        
        // Load saved settings after files are populated
        LoadSavedSettings();
        
        // Fetch IP addresses based on loaded network mode
        InitializeNetworkInfo();

        // LaunchGameCommand can only execute when IWAD and engine are selected
        var canLaunch = this.WhenAnyValue(
            x => x.SelectedBaseGame,
            x => x.EngineExecutable,
            (iwad, engine) => iwad != null && !string.IsNullOrEmpty(engine));
        LaunchGameCommand = ReactiveCommand.Create(LaunchGame, canLaunch);
        ShowLaunchSummaryCommand = ReactiveCommand.CreateFromTask(ShowLaunchSummary);
        RefreshFilesCommand = ReactiveCommand.Create(() =>
        {
            RefreshWadFiles();
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
        SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettings);
        LoadSettingsCommand = ReactiveCommand.CreateFromTask(LoadSettings);
        TestPortForwardingCommand = ReactiveCommand.CreateFromTask(TestPortForwarding);
        AutoConfigurePortCommand = ReactiveCommand.CreateFromTask(AutoConfigurePort);
        ResetSettingsCommand = ReactiveCommand.Create(ResetSettings);
        ChangeDataDirectoryCommand = ReactiveCommand.CreateFromTask(ChangeDataDirectory);
        ChangeEngineExecutableCommand = ReactiveCommand.CreateFromTask(ChangeEngineExecutable);
        AddCustomFlatpakCommand = ReactiveCommand.CreateFromTask(AddCustomFlatpak);
        SaveCurrentSettingsCommand = ReactiveCommand.Create(SavePaths);
        
        // Batocera commands
        SaveBatoceraConfigCommand = ReactiveCommand.CreateFromTask(SaveBatoceraConfig);
        
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
    /// Called when the application is closing to save current settings
    /// </summary>
    public void OnClosing()
    {
        SavePaths();
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
    public ObservableCollection<WadFile> TotalConversionFiles { get; } = new();
    public ObservableCollection<DmFlag> DmFlags1List { get; } = new();
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
                _settings.BaseGame = value.RelativePath;
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

    // Property to check if launch is allowed
    public bool CanLaunchGame => SelectedBaseGame != null && !string.IsNullOrEmpty(EngineExecutable);

    private Difficulty _selectedDifficulty = Difficulty.Normal;
    public Difficulty SelectedDifficulty
    {
        get => _selectedDifficulty;
        set => this.RaiseAndSetIfChanged(ref _selectedDifficulty, value);
    }

    private int _startingMap = 1;
    public int StartingMap
    {
        get => _startingMap;
        set => this.RaiseAndSetIfChanged(ref _startingMap, value);
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

    private double _zoomLevel = 1.0;
    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            this.RaiseAndSetIfChanged(ref _zoomLevel, value);
            UpdateWindowSize();
        }
    }

    private const double BaseWindowWidth = 800;
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
        WindowWidth = BaseWindowWidth * ZoomLevel;
        WindowHeight = BaseWindowHeight * ZoomLevel;
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
        var currentDir = Directory.GetCurrentDirectory();
        var configFile = Path.Combine(currentDir, "launcher.cfg");
        
        // Detect if running on Batocera
        bool isBatocera = Directory.Exists("/userdata/roms/ports");
        
        // If no config file exists and we're on Batocera, create default config
        if (!File.Exists(configFile) && isBatocera)
        {
            CreateBatoceraDefaultConfig(configFile);
        }
        
        // Default paths
        if (isBatocera)
        {
            _wadDir = "/userdata/roms/gzdoom";
            _engineFilePath = "/usr/bin/gzdoom";
        }
        else
        {
            _wadDir = Environment.GetEnvironmentVariable("GZDOOM_DATA_DIR") ?? currentDir;
            _engineFilePath = "";
        }
        
        // Ensure absolute paths
        if (!Path.IsPathRooted(_wadDir))
            _wadDir = Path.GetFullPath(_wadDir);
        
        // Try to load saved paths from config file
        try
        {
            if (File.Exists(configFile))
            {
                foreach (var line in File.ReadAllLines(configFile))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                        continue;
                    
                    var parts = trimmed.Split('=', 2);
                    if (parts.Length != 2)
                        continue;
                    
                    var key = parts[0].Trim().ToLowerInvariant();
                    var value = parts[1].Trim();
                    
                    if (key == "wads" && !string.IsNullOrEmpty(value) && Directory.Exists(value))
                        _wadDir = value;
                    else if (key == "enginefilepath" && !string.IsNullOrEmpty(value))
                        _engineFilePath = value;
                    else if (key == "engineflatpak" && !string.IsNullOrEmpty(value))
                        _engineFlatpakPath = value;
                    else if (key == "usefilepathengine" && bool.TryParse(value, out var useFilePath))
                    {
                        _useFilePathEngine = useFilePath;
                        _useFlatpakEngine = !useFilePath;
                    }
                    // Legacy support for old config format
                    else if (key == "engineexe" && !string.IsNullOrEmpty(value))
                    {
                        if (value.StartsWith("flatpak:", StringComparison.OrdinalIgnoreCase))
                        {
                            _engineFlatpakPath = value;
                            _useFlatpakEngine = true;
                            _useFilePathEngine = false;
                        }
                        else
                        {
                            _engineFilePath = value;
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
    
    private void CreateBatoceraDefaultConfig(string configFile)
    {
        try
        {
            var defaultConfig = new[]
            {
                "# DMINLauncher Configuration for Batocera",
                "# Auto-generated on first run",
                "",
                "# Directory paths",
                "wads=/userdata/roms/gzdoom",
                "enginefilepath=/usr/bin/gzdoom",
                "engineflatpak=",
                "usefilepathengine=True",
                "",
                "# Game settings",
                "difficulty=2",
                "startmap=E1M1",
                "gametype=0",
                "playercount=1",
                "networkmode=0",
                "hexenclass=0",
                "",
                "# Run switches",
                "avg=False",
                "fast=False",
                "nomonsters=False",
                "respawn=False",
                "timer=False",
                "timerminutes=20",
                "turbo=False",
                "turbospeed=100",
                "",
                "# UI settings",
                "zoomlevel=1.0",
                "",
                "# Selected files will be saved here",
                "basegame=",
                "selectedengine=gzdoom (system)"
            };
            
            File.WriteAllLines(configFile, defaultConfig);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create Batocera config: {ex.Message}");
        }
    }

    private void SavePaths()
    {
        try
        {
            var configFile = Path.Combine(Directory.GetCurrentDirectory(), "launcher.cfg");
            
            var lines = new List<string>
            {
                "# DMINLauncher Configuration",
                "# This file stores your launcher settings",
                "",
                "# Directory paths",
                $"wads={_wadDir}",
                $"enginefilepath={_engineFilePath}",
                $"engineflatpak={_engineFlatpakPath}",
                $"usefilepathengine={UseFilePathEngine}",
                "",
                "# Game settings",
                $"difficulty={(int)SelectedDifficulty}",
                $"startmap={SelectedMapName}",
                $"gametype={(int)GameType}",
                $"playercount={PlayerCount}",
                $"networkmode={(int)NetworkMode}",
                $"hexenclass={(int)HexenClass}",
                "",
                "# Run switches",
                $"avg={AvgSwitch}",
                $"fast={FastSwitch}",
                $"nomonsters={NoMonstersSwitch}",
                $"respawn={RespawnSwitch}",
                $"timer={TimerSwitch}",
                $"timerminutes={TimerMinutes}",
                $"turbo={TurboSwitch}",
                $"turbospeed={TurboSpeed}",
                "",
                "# UI settings",
                $"zoomlevel={ZoomLevel}",
                "",
                "# Selected base game",
                $"basegame={SelectedBaseGame?.RelativePath ?? ""}",
                "",
                "# Selected WADs/Mods (in load order)"
            };
            
            // Add selected mods
            foreach (var mod in SelectedModFiles.OrderBy(m => m.LoadOrder))
            {
                lines.Add($"selectedmod={mod.RelativePath}");
            }
            
            File.WriteAllLines(configFile, lines);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private void LoadSavedSettings()
    {
        try
        {
            var configFile = Path.Combine(Directory.GetCurrentDirectory(), "launcher.cfg");
            if (!File.Exists(configFile)) return;
            
            string? savedBaseGame = null;
            var savedMods = new List<string>();
            var warnings = new List<string>();
            
            foreach (var line in File.ReadAllLines(configFile))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;
                
                var parts = trimmed.Split('=', 2);
                if (parts.Length != 2)
                    continue;
                
                var key = parts[0].Trim().ToLowerInvariant();
                var value = parts[1].Trim();
                
                switch (key)
                {
                    case "difficulty":
                        if (int.TryParse(value, out var diff))
                            SelectedDifficulty = (Enums.Difficulty)diff;
                        break;
                    case "startmap":
                        // Store map name for later application (after maps are loaded)
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
                    case "zoomlevel":
                        if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out var zoom))
                        {
                            // Clamp to valid range
                            ZoomLevel = Math.Clamp(zoom, 0.5, 2.0);
                        }
                        break;
                    case "basegame":
                        savedBaseGame = value;
                        break;
                    case "selectedmod":
                        if (!string.IsNullOrEmpty(value))
                            savedMods.Add(value);
                        break;
                }
            }
            
            // Apply saved selections after files are loaded
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
            
            // If no base game selected, auto-select doom2.wad or doom.wad if they exist
            if (SelectedBaseGame == null)
            {
                SelectedBaseGame = WadFiles.FirstOrDefault(w => 
                    w.RelativePath.Equals("doom2.wad", StringComparison.OrdinalIgnoreCase));
                
                if (SelectedBaseGame == null)
                {
                    SelectedBaseGame = WadFiles.FirstOrDefault(w => 
                        w.RelativePath.Equals("doom.wad", StringComparison.OrdinalIgnoreCase));
                }
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
            .OrderBy(w => w.RelativePath);

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

        // Scan current directory and up to 2 levels deep
        try
        {
            // Level 0: Current directory
            AddModsFromDirectory(_wadDir, "", modFiles);

            // Level 1: Subdirectories
            foreach (var subDir in Directory.GetDirectories(_wadDir))
            {
                var level1Name = Path.GetFileName(subDir);
                AddModsFromDirectory(subDir, level1Name, modFiles);

                // Level 2: Sub-subdirectories
                try
                {
                    foreach (var subSubDir in Directory.GetDirectories(subDir))
                    {
                        var level2Name = Path.Combine(level1Name, Path.GetFileName(subSubDir));
                        AddModsFromDirectory(subSubDir, level2Name, modFiles);
                    }
                }
                catch { } // Skip inaccessible subdirectories
            }
        }
        catch { } // Skip if directory is inaccessible

        // Sort by path
        foreach (var mod in modFiles.OrderBy(m => m.RelativePath))
        {
            ModFiles.Add(mod);
        }
    }

    private void AddModsFromDirectory(string directory, string relativePath, List<WadFile> modFiles)
    {
        try
        {
            var files = Directory.GetFiles(directory)
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
        DmFlags1List.Clear();
        foreach (var kvp in DmFlagsData.DmFlags1)
        {
            DmFlags1List.Add(new DmFlag { Label = kvp.Key, BitValue = kvp.Value });
        }

        DmFlags2List.Clear();
        foreach (var kvp in DmFlagsData.DmFlags2)
        {
            DmFlags2List.Add(new DmFlag { Label = kvp.Key, BitValue = kvp.Value });
        }

        DmFlags3List.Clear();
        foreach (var kvp in DmFlagsData.DmFlags3)
        {
            DmFlags3List.Add(new DmFlag { Label = kvp.Key, BitValue = kvp.Value });
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

        StatusMessage = "🎮 Preparing to launch Doom...";
        StatusMessageColor = "Yellow";

        var args = new System.Collections.Generic.List<string>();

        args.Add("-iwad");
        args.Add(SelectedBaseGame.FullPath);

        var selectedTCs = TotalConversionFiles.Where(m => m.IsSelected).ToList();
        
        if (SelectedModFiles.Any() || selectedTCs.Any())
        {
            args.Add("-file");
            
            // Add total conversions first (they should load before regular mods)
            foreach (var tc in selectedTCs)
            {
                args.Add(tc.FullPath);
            }
            
            // Then add mods in load order
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
            // Use flatpak run command with necessary permissions
            psi.FileName = "flatpak";
            psi.ArgumentList.Add("run");
            
            // Grant read-only access to the WADs directory
            if (!string.IsNullOrEmpty(_wadDir) && Directory.Exists(_wadDir))
            {
                psi.ArgumentList.Add($"--filesystem={_wadDir}:ro");
            }
            
            // Add basic display and device permissions (fallback if not set via Flatseal)
            psi.ArgumentList.Add("--socket=x11");
            psi.ArgumentList.Add("--socket=wayland");
            psi.ArgumentList.Add("--socket=pulseaudio");
            psi.ArgumentList.Add("--device=dri");
            
            psi.ArgumentList.Add(flatpakAppId);
            
            // Add game arguments
            foreach (var arg in engineArgs)
            {
                psi.ArgumentList.Add(arg);
            }
            
            System.Diagnostics.Debug.WriteLine($"Launching Flatpak: flatpak run --filesystem={_wadDir}:ro --socket=x11 --socket=wayland --socket=pulseaudio --device=dri {flatpakAppId} {string.Join(" ", engineArgs)}");
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

        var selectedTCs = TotalConversionFiles.Where(m => m.IsSelected).ToList();
        
        if (SelectedModFiles.Any() || selectedTCs.Any())
        {
            args.Add("-file");
            foreach (var tc in selectedTCs)
            {
                args.Add(tc.FullPath);
            }
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

        if (selectedTCs.Any())
        {
            summary.AppendLine("🔄 TOTAL CONVERSIONS:");
            foreach (var tc in selectedTCs)
            {
                summary.AppendLine($"   • {tc.RelativePath}");
            }
            summary.AppendLine();
        }

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
        summary.AppendLine($"   Starting Map: MAP{StartingMap:D2}");
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

    private async Task SaveSettings()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow : null;
            
            if (topLevel == null) return;

            var defaultName = $"preset-{DateTime.Now:yyyy-MM-dd-HHmm}.gzdoom";
            
            // Use modern StorageProvider API
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Doom Preset",
                SuggestedFileName = defaultName,
                DefaultExtension = "gzdoom",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Doom Preset")
                    {
                        Patterns = new[] { "*.gzdoom" }
                    }
                },
                ShowOverwritePrompt = true
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
            args.Add(((int)SelectedDifficulty).ToString());

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

            // Write to file
            using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(string.Join(" ", args));
            
            StatusMessage = $"✅ Settings saved to: {file.Name}";
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
                Title = "Load Doom Preset",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Doom Preset")
                    {
                        Patterns = new[] { "*.gzdoom" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count == 0) return;

            var file = files[0];
            using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            var args = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Reset current selections
            SelectedModFiles.Clear();
            foreach (var flag in DmFlags1List) flag.IsChecked = false;
            foreach (var flag in DmFlags2List) flag.IsChecked = false;
            foreach (var flag in DmFlags3List) flag.IsChecked = false;
            AvgSwitch = false;
            FastSwitch = false;
            NoMonstersSwitch = false;
            RespawnSwitch = false;
            TimerSwitch = false;
            TurboSwitch = false;

            // Track validation warnings
            var warnings = new List<string>();
            
            // Parse arguments
            bool parsingMods = false;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-iwad":
                        if (i + 1 < args.Length)
                        {
                            var iwad = args[++i];
                            SelectedBaseGame = WadFiles.FirstOrDefault(w => 
                                w.RelativePath.Equals(iwad, StringComparison.OrdinalIgnoreCase));
                            
                            // Validate IWAD exists
                            if (SelectedBaseGame != null)
                            {
                                if (!File.Exists(SelectedBaseGame.FullPath))
                                {
                                    warnings.Add($"IWAD not found: {iwad}");
                                    SelectedBaseGame = null;
                                }
                            }
                            else
                            {
                                warnings.Add($"IWAD not found: {iwad}");
                            }
                        }
                        parsingMods = false;
                        break;

                    case "-file":
                        parsingMods = true;
                        break;

                    case "-skill":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var skill))
                        {
                            SelectedDifficulty = (Difficulty)skill;
                        }
                        parsingMods = false;
                        break;

                    case "-warp":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var map))
                        {
                            StartingMap = map;
                        }
                        parsingMods = false;
                        break;

                    case "+set":
                        if (i + 2 < args.Length)
                        {
                            var key = args[++i];
                            var value = int.TryParse(args[++i], out var v) ? v : 0;
                            ApplyDmFlags(key, value);
                        }
                        parsingMods = false;
                        break;

                    case "-avg":
                        AvgSwitch = true;
                        parsingMods = false;
                        break;

                    case "-fast":
                        FastSwitch = true;
                        parsingMods = false;
                        break;

                    case "-nomonsters":
                        NoMonstersSwitch = true;
                        parsingMods = false;
                        break;

                    case "-respawn":
                        RespawnSwitch = true;
                        parsingMods = false;
                        break;

                    case "-timer":
                        TimerSwitch = true;
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var timer))
                        {
                            TimerMinutes = timer;
                        }
                        parsingMods = false;
                        break;

                    case "-turbo":
                        TurboSwitch = true;
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var turbo))
                        {
                            TurboSpeed = turbo;
                        }
                        parsingMods = false;
                        break;

                    case "+playerclass":
                        if (i + 1 < args.Length)
                        {
                            var className = args[++i];
                            HexenClass = className.ToLower() switch
                            {
                                "fighter" => Enums.HexenClass.Fighter,
                                "cleric" => Enums.HexenClass.Cleric,
                                "mage" => Enums.HexenClass.Mage,
                                _ => Enums.HexenClass.Fighter
                            };
                        }
                        parsingMods = false;
                        break;

                    default:
                        if (parsingMods && !args[i].StartsWith("-") && !args[i].StartsWith("+"))
                        {
                            // Find mod in available mods and add to load order
                            var mod = ModFiles.FirstOrDefault(m => 
                                m.RelativePath.Equals(args[i], StringComparison.OrdinalIgnoreCase));
                            if (mod != null)
                            {
                                // Validate mod file exists
                                if (File.Exists(mod.FullPath))
                                {
                                    ModFiles.Remove(mod);
                                    mod.LoadOrder = SelectedModFiles.Count;
                                    SelectedModFiles.Add(mod);
                                }
                                else
                                {
                                    warnings.Add($"WAD not found: {args[i]}");
                                }
                            }
                            else
                            {
                                warnings.Add($"WAD not found: {args[i]}");
                            }
                        }
                        break;
                }
            }
            
            // Show appropriate status message
            if (warnings.Any())
            {
                StatusMessage = $"⚠️ Settings loaded with warnings:\n{string.Join("\n", warnings)}";
                StatusMessageColor = "Orange";
            }
            else
            {
                StatusMessage = $"✅ Settings loaded from: {file.Name}";
                StatusMessageColor = "LimeGreen";
            }
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
        // Reset all settings to defaults
        SelectedBaseGame = null;
        EngineExecutable = "";
        SelectedDifficulty = Difficulty.Normal;
        StartingMap = 1;
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
        
        // Reset total conversion selections
        foreach (var tc in TotalConversionFiles)
        {
            tc.IsSelected = false;
        }
        
        // Reset DMFLAGS
        foreach (var flag in DmFlags1List) flag.IsChecked = false;
        foreach (var flag in DmFlags2List) flag.IsChecked = false;
        foreach (var flag in DmFlags3List) flag.IsChecked = false;
        
        StatusMessage = "✅ All settings reset to defaults";
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
                SavePaths();
                
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

                _engineExecutable = path;
                EngineExecutable = path;
                SavePaths();
                
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
                SavePaths();
                
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

    private void ZoomIn()
    {
        // Increase zoom by 5%, max 2.0x (200%)
        if (ZoomLevel < 2.0)
        {
            ZoomLevel = Math.Min(2.0, ZoomLevel + 0.05);
        }
    }

    private void ZoomOut()
    {
        // Decrease zoom by 5%, min 0.5x (50%)
        if (ZoomLevel > 0.5)
        {
            ZoomLevel = Math.Max(0.5, ZoomLevel - 0.05);
        }
    }

    private void ResetZoom()
    {
        ZoomLevel = 1.0;
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
            Width = 400,
            Height = 180,
            CanResize = false
        };

        var result = false;
        
        var messageText = new Avalonia.Controls.TextBlock
        {
            Text = "Are you sure you want to exit?\n\nYour settings will be saved.",
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
            Spacing = 15
        };

        var yesButton = new Avalonia.Controls.Button
        {
            Content = "Yes",
            Width = 100,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#604040")),
            IsDefault = true // Makes Enter key press this button
        };

        var noButton = new Avalonia.Controls.Button
        {
            Content = "No",
            Width = 100,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#404040")),
            IsCancel = true // Makes Escape key press this button
        };

        yesButton.Click += (s, e) =>
        {
            result = true;
            confirmDialog.Close();
        };

        noButton.Click += (s, e) =>
        {
            result = false;
            confirmDialog.Close();
        };

        buttonPanel.Children.Add(yesButton);
        buttonPanel.Children.Add(noButton);

        var mainPanel = new Avalonia.Controls.StackPanel
        {
            Children = { messageText, buttonPanel }
        };

        confirmDialog.Content = mainPanel;
        
        // Set focus to Yes button when dialog opens
        confirmDialog.Opened += (s, e) =>
        {
            yesButton.Focus();
        };
        
        await confirmDialog.ShowDialog(topLevel);

        if (result)
        {
            ExitApplication();
        }
    }

    private void ExitApplication()
    {
        // Save paths before exiting
        SavePaths();
        
        // Close the application
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
            else
            {
                // Fallback: add a default starting map based on game type
                if (iwad.RelativePath.Contains("doom2", StringComparison.OrdinalIgnoreCase) ||
                    iwad.RelativePath.Contains("plutonia", StringComparison.OrdinalIgnoreCase) ||
                    iwad.RelativePath.Contains("tnt", StringComparison.OrdinalIgnoreCase) ||
                    iwad.RelativePath.Contains("hexen", StringComparison.OrdinalIgnoreCase))
                {
                    AvailableMaps.Add("MAP01");
                    SelectedMapName = "MAP01";
                }
                else
                {
                    AvailableMaps.Add("E1M1");
                    SelectedMapName = "E1M1";
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

    private async Task SaveBatoceraConfig()
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
