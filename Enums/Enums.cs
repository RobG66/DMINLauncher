namespace DMINLauncher.Enums;

public enum Difficulty
{
    VeryEasy = 1,
    Easy = 2,
    Normal = 3,
    Hard = 4,
    Nightmare = 5
}

public enum GameType
{
    SinglePlayer,
    Cooperative,
    Deathmatch
}

public enum NetworkMode
{
    None,
    HostLAN,
    HostInternet,
    Connect
}

public enum HexenClass
{
    Fighter,
    Cleric,
    Mage,
    Random
}
