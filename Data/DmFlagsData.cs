using System.Collections.Generic;

namespace DMINLauncher.Data;

public static class DmFlagsData
{
    public static readonly Dictionary<string, int> DmFlags1 = new()
    {
        ["No monsters"] = 1,
        ["Monsters respawn"] = 2,
        ["Item respawn"] = 4,
        ["No falling damage"] = 8,
        ["Old random number generator"] = 16,
        ["Infinite ammo"] = 32,
        ["No cheating"] = 64,
        ["Allow jumping"] = 128,
        ["Allow crouching"] = 256,
        ["Disable autoaim"] = 512,
        ["Disable weapon auto-switching"] = 1024,
        ["Enable mouse freelook"] = 2048,
        ["Disable friendly fire"] = 4096,
        ["Monsters cannot target invisible players"] = 8192,
        ["Respawn items (not in DM)"] = 16384,
        ["Disable monster drops"] = 32768,
        ["Disable pain chance"] = 65536,
        ["Disable monster sounds"] = 131072,
        ["Fast monsters"] = 262144
    };

    public static readonly Dictionary<string, int> DmFlags2 = new()
    {
        ["Drop weapon"] = 2,
        ["No team changing (DM)"] = 16,
        ["Double ammo"] = 64,
        ["Degeneration"] = 128,
        ["Allow BFG aiming"] = 256,
        ["Barrels respawn (DM)"] = 512,
        ["Respawn protection (DM)"] = 1024,
        ["Spawn where died (Coop)"] = 4096,
        ["Keep frags gained (DM)"] = 8192,
        ["No respawn"] = 16384,
        ["Lose frag on death (DM)"] = 32768,
        ["Infinite inventory"] = 65536,
        ["No monsters to exit"] = 131072,
        ["Allow automap"] = 262144,
        ["Automap allies"] = 524288,
        ["Allow spying"] = 1048576,
        ["Chasecam cheat"] = 2097152,
        ["Disallow Suicide"] = 4194304,
        ["Allow Autoaim"] = 8388608,
        ["Check ammo for weapon switch"] = 16777216,
        ["Icon of Sins death kills its spawns"] = 33554432,
        ["End sector counts for kill %"] = 67108864,
        ["Big powerups respawn"] = 134217728,
        ["Allow vertical bullet spread"] = 1073741824
    };

    public static readonly Dictionary<string, int> DmFlags3 = new()
    {
        ["Allow team damage (Coop)"] = 1,
        ["Show team scores (DM)"] = 2,
        ["Disable item pickup (DM)"] = 4,
        ["Kill without gibbing"] = 8,
        ["Teleport to start on death"] = 16,
        ["Short respawn delay"] = 32,
        ["Long respawn delay"] = 64,
        ["Convert splash to direct damage"] = 128,
        ["Disable medikit pickup"] = 256,
        ["Disable armor pickup"] = 512,
        ["Slow weapon switching"] = 1024,
        ["Custom gravity"] = 2048,
        ["Limited air control"] = 4096,
        ["Disable health regeneration"] = 8192,
        ["Disable armor regeneration"] = 16384,
        ["Enable team item sharing"] = 32768,
        ["Team auto-balance"] = 65536
    };
}
