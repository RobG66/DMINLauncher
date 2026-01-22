using System.Collections.Generic;
using DMINLauncher.Enums;

namespace DMINLauncher.Models;

public class LauncherSettings
{
    public string BaseGame { get; set; } = "None Selected";
    public string SelectedEngine { get; set; } = "batocera";
    public List<string> SelectedMods { get; set; } = new();
    public List<string> RunSwitches { get; set; } = new();
    public int TurboSpeed { get; set; } = 100;
    public int TimeValue { get; set; } = 20;
    public Difficulty SelectedDifficulty { get; set; } = Difficulty.Normal;
    public int StartingMap { get; set; } = 1;
    public GameType GameType { get; set; } = GameType.SinglePlayer;
    public int PlayerCount { get; set; } = 1;
    public HexenClass PlayerType { get; set; } = HexenClass.Fighter;
    public NetworkMode NetworkMode { get; set; } = NetworkMode.None;
    public string IpAddress { get; set; } = "";
    public int DmFlags { get; set; } = 0;
    public int DmFlags2 { get; set; } = 0;
    public int DmFlags3 { get; set; } = 0;
}
