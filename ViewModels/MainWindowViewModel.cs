using DMINLauncher.Data;
using DMINLauncher.Enums;
using DMINLauncher.Models;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace DMINLauncher.ViewModels;

public partial class MainWindowViewModel : ReactiveObject
{
    #region Fields

    private string _wadDir = "";

    #endregion

    #region Collections

    public ObservableCollection<string>      DifficultyOptions  { get; } = new();
    public ObservableCollection<GameType>    GameTypeOptions    { get; }
    public ObservableCollection<NetworkMode> NetworkModeOptions { get; }
    public ObservableCollection<HexenClass>  HexenClassOptions  { get; }

    public ObservableCollection<WadFile> WadFiles         { get; } = new();
    public ObservableCollection<WadFile> ModFiles         { get; } = new();
    public ObservableCollection<WadFile> SelectedModFiles { get; } = new();

    public ObservableCollection<DmFlag> DmFlags1List { get; } = new();
    public ObservableCollection<DmFlag> DmFlags1Col1 { get; } = new();
    public ObservableCollection<DmFlag> DmFlags1Col2 { get; } = new();
    public ObservableCollection<DmFlag> DmFlags1Col3 { get; } = new();
    public ObservableCollection<DmFlag> DmFlags2List { get; } = new();
    public ObservableCollection<DmFlag> DmFlags3List { get; } = new();

    public ObservableCollection<string> AvailableMaps { get; } = new();

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> LaunchGameCommand             { get; }
    public ReactiveCommand<Unit, Unit> ShowLaunchSummaryCommand      { get; }
    public ReactiveCommand<Unit, Unit> RefreshFilesCommand           { get; }
    public ReactiveCommand<Unit, Unit> SaveConfigCommand             { get; }
    public ReactiveCommand<Unit, Unit> LoadConfigCommand             { get; }
    public ReactiveCommand<Unit, Unit> TestPortForwardingCommand     { get; }
    public ReactiveCommand<Unit, Unit> AutoConfigurePortCommand      { get; }
    public ReactiveCommand<Unit, Unit> ResetSettingsCommand          { get; }
    public ReactiveCommand<Unit, Unit> ChangeDataDirectoryCommand    { get; }
    public ReactiveCommand<Unit, Unit> ChangeEngineExecutableCommand { get; }
    public ReactiveCommand<Unit, Unit> AddCustomFlatpakCommand       { get; }
    public ReactiveCommand<Unit, Unit> SaveCurrentSettingsCommand    { get; }
    public ReactiveCommand<Unit, Unit> SaveBatoceraConfigCommand     { get; }
    public ReactiveCommand<Unit, Unit> LoadBatoceraConfigCommand     { get; }
    public ReactiveCommand<Unit, Unit> ZoomInCommand                 { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand                { get; }
    public ReactiveCommand<Unit, Unit> ResetZoomCommand              { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand                   { get; }
    public ReactiveCommand<Unit, Unit> ShowExitConfirmationCommand   { get; }
    public ReactiveCommand<Unit, Unit> AddModToLoadOrderCommand      { get; }
    public ReactiveCommand<Unit, Unit> RemoveModFromLoadOrderCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveModUpCommand              { get; }
    public ReactiveCommand<Unit, Unit> MoveModDownCommand            { get; }

    #endregion

    #region Constructor

    public MainWindowViewModel()
    {
        GameTypeOptions    = new ObservableCollection<GameType>   (Enum.GetValues<GameType>());
        NetworkModeOptions = new ObservableCollection<NetworkMode>(Enum.GetValues<NetworkMode>());
        HexenClassOptions  = new ObservableCollection<HexenClass> (Enum.GetValues<HexenClass>());

        LoadSavedPaths();
        UpdateDifficultyNames();
        RefreshWadFiles();
        RefreshModFiles();
        UpdateDmFlagsLists();

        var configFile = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "dminlauncher.cfg");
        if (IsBatocera && !System.IO.File.Exists(configFile))
            ResetSettings();

        LoadDefaultSettings(configFile);
        InitializeNetworkInfo();

        var canLaunch = this.WhenAnyValue(
            x => x.SelectedBaseGame,
            x => x.EngineExecutable,
            x => x.IsLaunching,
            (iwad, engine, launching) => iwad != null && !string.IsNullOrEmpty(engine) && !launching);

        LaunchGameCommand             = ReactiveCommand.Create(LaunchGame, canLaunch);
        ShowLaunchSummaryCommand      = ReactiveCommand.CreateFromTask(ShowLaunchSummary);
        SaveConfigCommand             = ReactiveCommand.CreateFromTask(AskToSaveCfgConfig);
        LoadConfigCommand             = ReactiveCommand.CreateFromTask(AskToLoadCfgConfig);
        TestPortForwardingCommand     = ReactiveCommand.CreateFromTask(TestPortForwarding);
        AutoConfigurePortCommand      = ReactiveCommand.CreateFromTask(AutoConfigurePort);
        ResetSettingsCommand          = ReactiveCommand.Create(ResetSettings);
        ChangeDataDirectoryCommand    = ReactiveCommand.CreateFromTask(ChangeDataDirectory);
        ChangeEngineExecutableCommand = ReactiveCommand.CreateFromTask(ChangeEngineExecutable);
        AddCustomFlatpakCommand       = ReactiveCommand.CreateFromTask(AddCustomFlatpak);
        SaveCurrentSettingsCommand    = ReactiveCommand.Create(SaveDefaultConfig);
        SaveBatoceraConfigCommand     = ReactiveCommand.CreateFromTask(SaveGZDoomConfig);
        LoadBatoceraConfigCommand     = ReactiveCommand.CreateFromTask(LoadGZDoomConfig);
        ZoomInCommand                 = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand                = ReactiveCommand.Create(ZoomOut);
        ResetZoomCommand              = ReactiveCommand.Create(ResetZoom);
        ExitCommand                   = ReactiveCommand.Create(ExitApplication);
        ShowExitConfirmationCommand   = ReactiveCommand.CreateFromTask(ShowExitConfirmation);
        AddModToLoadOrderCommand      = ReactiveCommand.Create(AddModToLoadOrder);
        RemoveModFromLoadOrderCommand = ReactiveCommand.Create(RemoveModFromLoadOrder);
        MoveModUpCommand              = ReactiveCommand.Create(MoveModUp);
        MoveModDownCommand            = ReactiveCommand.Create(MoveModDown);

        RefreshFilesCommand = ReactiveCommand.Create(() =>
        {
            RefreshModFiles();

            if (StatusMessage.Contains("Configuration warnings", StringComparison.OrdinalIgnoreCase) ||
                StatusMessage.Contains("WAD not found",          StringComparison.OrdinalIgnoreCase) ||
                StatusMessage.Contains("IWAD not found",         StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage      = "✅ Files refreshed";
                StatusMessageColor = "LimeGreen";
            }
        });

        this.WhenAnyValue(x => x.NetworkMode)
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async mode =>
            {
                switch (mode)
                {
                    case NetworkMode.HostLAN:
                        await GetLocalIpAddress();
                        break;
                    case NetworkMode.HostInternet:
                        await GetLocalIpAddress();
                        await GetPublicIpAddress();
                        break;
                    case NetworkMode.None:
                        IpAddress = LocalIpAddress = PublicIpAddress = "";
                        break;
                }
            });
    }

    #endregion
}
