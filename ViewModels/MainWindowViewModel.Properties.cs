using DMINLauncher.Enums;
using DMINLauncher.Models;
using ReactiveUI;
using System;
using System.IO;
using System.Reflection;

namespace DMINLauncher.ViewModels;

public partial class MainWindowViewModel
{
    #region App Info

    public string AppVersion
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "";
        }
    }

    public bool IsWindows  => OperatingSystem.IsWindows();
    public bool IsLinux    => OperatingSystem.IsLinux();
    public bool IsBatocera => Directory.Exists("/userdata/roms/ports");

    #endregion

    #region Engine

    private string _engineFilePath    = string.Empty;
    private string _engineFlatpakPath = string.Empty;

    public string EngineExecutable
    {
        get => _useFilePathEngine ? _engineFilePath : _engineFlatpakPath;
        set
        {
            if (_useFilePathEngine)
                this.RaiseAndSetIfChanged(ref _engineFilePath, value);
            else
                this.RaiseAndSetIfChanged(ref _engineFlatpakPath, value);

            this.RaisePropertyChanged(nameof(EngineExecutable));
            this.RaisePropertyChanged(nameof(CanLaunchGame));
        }
    }

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

    #endregion

    #region Base Game

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
                UpdateDifficultyNames();
                SelectedDifficulty = Difficulty.Easy;

                if (AvailableMaps.Count > 0)
                    SelectedMapName = AvailableMaps[0];

                if (StatusMessage.Contains("IWAD not found", StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage      = "";
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

    #endregion

    #region Launch State

    private bool _isLaunching;
    public bool IsLaunching
    {
        get => _isLaunching;
        set => this.RaiseAndSetIfChanged(ref _isLaunching, value);
    }

    public bool CanLaunchGame => SelectedBaseGame != null && !string.IsNullOrEmpty(EngineExecutable);

    #endregion

    #region Game Settings

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
                if (_networkMode != NetworkMode.None)
                    NetworkMode = NetworkMode.None;
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

    #endregion

    #region Network

    private NetworkMode _networkMode = NetworkMode.None;
    public NetworkMode NetworkMode
    {
        get => _networkMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _networkMode, value);
            this.RaisePropertyChanged(nameof(ShowPortForwardingTest));

            if (value != NetworkMode.None && GameType == GameType.SinglePlayer)
                GameType = GameType.Cooperative;
            else if (value == NetworkMode.None)
            {
                GameType    = GameType.SinglePlayer;
                PlayerCount = 1;
            }
        }
    }

    public bool ShowPortForwardingTest =>
        NetworkMode == NetworkMode.HostInternet || NetworkMode == NetworkMode.Connect;

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

    #endregion

    #region Run Switches

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

    #endregion

    #region UI / Status

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

    private const double BaseWindowWidth  = 850;
    private const double BaseWindowHeight = 700;

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
        WindowWidth  = BaseWindowWidth  * (ZoomLevel / 100.0);
        WindowHeight = BaseWindowHeight * (ZoomLevel / 100.0);
    }

    #endregion

    #region Paths & Mod Selection

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

    #endregion
}
