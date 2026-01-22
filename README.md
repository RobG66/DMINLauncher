# DMINLauncher v1.0.0

**A modern, cross-platform launcher for classic DOOM source ports**

DMINLauncher is a feature-rich, user-friendly launcher for DOOM source ports like GZDoom, Zandronum, and more. Built with .NET 9 and Avalonia UI, it provides a beautiful interface for managing your DOOM games, mods, and multiplayer sessions.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)

---

## ✨ Features

### Core Features
- 🎮 **Multi-Engine Support** - Automatically detects GZDoom, Zandronum, Doom Retro, and other source ports
- 📁 **Smart File Management** - Organizes IWADs, mods (PWADs/PK3), and total conversions
- 🗺️ **Map Detection** - Shows map count and names for WAD/PK3 files
- 🔄 **Mod Load Order** - Drag and arrange mods in custom loading order
- ⚙️ **DMFLAGS Editor** - Full UI for DMFLAGS, DMFLAGS2, and DMFLAGS3
- 🌐 **Multiplayer** - LAN and Internet hosting with UPnP port forwarding
- 💾 **Configuration Persistence** - Saves all settings between sessions
- 🔍 **Zoom Controls** - Adjustable UI scaling (Ctrl+Plus/Minus)

### Multiplayer Features
- 📡 **UPnP Auto-Configuration** - One-click port forwarding for supported routers
- 🔌 **Port Testing** - Tests UDP port 5029 for Doom multiplayer
- 🌍 **IP Detection** - Automatically detects local and public IP addresses
- 👥 **Player Count** - Configure 1-16 players
- 🎯 **Game Modes** - Cooperative, Deathmatch, Team Deathmatch

### Advanced Features
- 📋 **Launch Summary** - Preview full command line before launching
- 🎲 **Quick Start Options** - Fast monsters, no monsters, respawning items, turbo mode
- ⏱️ **Time Limit** - Set match time limits
- 🧙 **Hexen Class Selection** - Choose Fighter, Cleric, or Mage for Hexen
- 📦 **PK3/ZIP Support** - Full support for modern PK3 mod archives
- 🐧 **Flatpak Support** - Add Flatpak applications as engines (Linux)

---

## 📥 Installation

### Windows

1. **Download** the latest `DMINLauncher-win-x64.exe` from releases
2. **Place** the executable in a dedicated folder (e.g., `C:\Games\DMINLauncher\`)
3. **Run** `DMINLauncher-win-x64.exe`
4. **Configure** directories:
   - Click "Change Data Directory" and select your DOOM WADs folder
   - Click "Change Engine Directory" and select folder containing your source port executables

### Linux

1. **Download** the latest `DMINLauncher-linux-x64` from releases
2. **Make executable:**
   ```bash
   chmod +x DMINLauncher-linux-x64
   ```
3. **Run:**
   ```bash
   ./DMINLauncher-linux-x64
   ```

### Batocera / RetroBat

See [BATOCERA-SETUP.md](BATOCERA-SETUP.md) for detailed instructions.

**Quick Setup:**
```bash
# Copy to ports folder
cp DMINLauncher-linux-x64 /userdata/roms/ports/DMINLauncher/

# Make executable
chmod +x /userdata/roms/ports/DMINLauncher/DMINLauncher-linux-x64

# Copy default config
cp launcher.cfg.batocera-template /userdata/roms/ports/DMINLauncher/launcher.cfg

# Create launcher script
cat > /userdata/roms/ports/DMINLauncher.sh << 'EOF'
#!/bin/bash
cd /userdata/roms/ports/DMINLauncher
export DISPLAY=:0
./DMINLauncher-linux-x64
EOF

chmod +x /userdata/roms/ports/DMINLauncher.sh
```

---

## 🚀 Quick Start

### First Launch

1. **Set Data Directory** - Point to your folder containing DOOM WAD files (DOOM.WAD, DOOM2.WAD, etc.)
2. **Set Engine Directory** - Point to folder containing source port executables (gzdoom.exe, zandronum.exe, etc.)
3. **Select Base Game** - Choose your IWAD (DOOM, DOOM II, Heretic, Hexen, etc.)
4. **Select Engine** - Choose which source port to use
5. **Add Mods** (Optional) - Select mod files from the right panel and click "Add to Load Order"
6. **Click Launch Game!**

### Organizing Your Files

**Recommended Folder Structure:**
```
C:\Games\Doom\
├── IWADs\
│   ├── doom.wad
│   ├── doom2.wad
│   ├── plutonia.wad
│   ├── tnt.wad
│   ├── heretic.wad
│   └── hexen.wad
├── Mods\
│   ├── maps\
│   │   ├── 1000LinesCommunityProject.wad
│   │   └── Eviternity.pk3
│   ├── weapons\
│   │   └── brutal_doom_v21.pk3
│   └── gameplay\
│       └── project_brutality.pk3
├── Engines\
│   ├── gzdoom.exe
│   └── zandronum.exe
```

---

## ⚙️ Configuration

### Configuration File

Settings are automatically saved to `launcher.cfg` in the same directory as the executable.

**Default launcher.cfg:**
```ini
# DMIN Launcher Configuration
# Auto-generated on first run

# Directory paths
wads=C:\Games\Doom\IWADs
engine=C:\Games\Doom\Engines

# Zoom level (0.5 to 2.0)
zoom=1.0

# Game settings
difficulty=2          # 0=ITYTD, 1=HNTR, 2=HMP, 3=UV, 4=NM
startmap=1           # Starting map number
gametype=0           # 0=Single, 1=Coop, 2=DM, 3=TDM
playercount=1        # 1-16 players
networkmode=0        # 0=None, 1=LAN, 2=Internet, 3=Connect
hexenclass=0         # 0=Fighter, 1=Cleric, 2=Mage

# Quick start switches
fast=False           # Fast monsters
nomonsters=False     # No monsters
respawn=False        # Respawning items/monsters
timer=False          # Time limit
timerminutes=20      # Minutes for time limit
turbo=False          # Turbo mode
turbospeed=100       # Turbo speed percentage

# Selected files
basegame=doom2.wad
selectedengine=gzdoom.exe

# Mod load order (one per line)
mods=

# DMFLAGS (decimal values)
dmflags=0
dmflags2=0
dmflags3=0
```

### Manual Configuration

You can edit `launcher.cfg` directly to:
- Set default game and engine
- Configure permanent DMFLAGS
- Set custom zoom level
- Pre-load mod order

---

## 🌐 Multiplayer Setup

### LAN Games (Easy)

1. Select **"Host LAN"** from Network Mode
2. Your **local IP** will be displayed automatically
3. Click **Launch Game**
4. Other players use your **local IP** to connect

### Internet Games (Requires Port Forwarding)

#### Method 1: Automatic (UPnP)

1. Select **"Host Internet"** from Network Mode
2. Click **"🔍 Test Port"** to check UPnP support
3. If supported, click **"⚡ Auto-Configure Port"**
4. Share your **Public IP** with other players
5. Click **Launch Game**

#### Method 2: Manual Port Forwarding

1. Log into your router (usually http://192.168.1.1)
2. Find "Port Forwarding" or "NAT" settings
3. Create new rule:
   - **Protocol:** UDP
   - **External Port:** 5029
   - **Internal IP:** Your computer's local IP (shown in app)
   - **Internal Port:** 5029
4. Save and click **"🔍 Test Port"** to verify
5. Share your **Public IP** with other players

### Joining Games

1. Select **"Connect"** from Network Mode
2. Enter host's IP address in the **IP Address** field
3. Click **Launch Game**

---

## 🎮 Supported Engines

DMINLauncher auto-detects the following source ports:

| Engine | Platform | Features |
|--------|----------|----------|
| **GZDoom** | Windows, Linux | Modern features, mods, multiplayer |
| **Zandronum** | Windows, Linux | Best multiplayer, CTF, skulltag |
| **Chocolate Doom** | Windows, Linux | Vanilla authentic experience |
| **Crispy Doom** | Windows, Linux | Enhanced vanilla |
| **Doom Retro** | Windows | Modern vanilla-plus |
| **DSDA-Doom** | Windows, Linux | Demo recording, speedrunning |
| **Eternity Engine** | Windows, Linux | Advanced features |
| **PrBoom+** | Windows, Linux | Demos, compatibility |
| **Woof!** | Windows, Linux | Limit-removing, QoL features |

**Flatpak Support (Linux):** Click "+ Flatpak" to add any Flatpak application as an engine.

---

## 🗂️ Supported File Formats

### Base Games (IWADs)
- `.wad` - Original IWAD files (DOOM.WAD, DOOM2.WAD, etc.)

### Mods (PWADs)
- `.wad` - Classic WAD format
- `.pk3` - PK3 archives (ZIP-based, used by modern mods)
- `.pk7` - PK7 archives (7z-based)
- `.ipk3` - Interactive PK3 files
- `.zip` - Generic ZIP files containing mods

The launcher automatically:
- Detects map count and names
- Shows file size
- Identifies IWAD vs PWAD types
- Parses PK3 archives for map information

---

## 🎯 DMFLAGS Reference

DMINLauncher provides a full UI editor for DMFLAGS. Here are some common settings:

### DMFLAGS
- **No Health** - Removes health pickups
- **No Items** - Removes item pickups
- **Weapons Stay** - Weapons remain after pickup
- **Fall Damage** - Players take fall damage
- **Same Level** - Stay on same level in co-op
- **Spawn Farthest** - Spawn away from other players
- **Force Respawn** - Automatic respawning
- **No Armor** - Removes armor pickups

### DMFLAGS2
- **Yes Freeaim** - Allow freelook aiming
- **No Fov** - Disable FOV changes
- **No Crouch** - Disable crouching
- **No Jump** - Disable jumping
- **No Freelook** - Disable mouselook
- **Infinite Ammo** - Unlimited ammunition
- **No Monsters** - Remove all monsters
- **Fast Monsters** - Monsters move/attack faster

### DMFLAGS3
- **No Coop Weapon/Ammo Spawn** - Co-op item spawning
- **Lose Frag/Lose Powerup** - Death penalties
- **Keep Frags Gained** - Keep frags on level change
- **No Team Select** - Auto-assign teams

---

## 🔧 Troubleshooting

### App Won't Start

**Windows:**
- Install [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
- Run as Administrator if file dialogs fail

**Linux:**
- Verify executable permissions: `chmod +x DMINLauncher-linux-x64`
- Install dependencies: `sudo apt install libx11-6 libice6 libsm6`

### No WAD Files Detected

1. Click **"Change Data Directory"**
2. Navigate to folder containing your `.wad` files
3. Click **"Refresh"** button (small button next to Engine dropdown)
4. Check that files are valid WAD/PK3 format

### No Engines Detected

1. Click **"Change Engine Directory"**
2. Navigate to folder containing source port executables
3. Click **"Refresh"** button
4. Ensure executables have correct names (gzdoom.exe, zandronum.exe, etc.)

### Multiplayer Connection Issues

**Cannot connect to host:**
- Verify port 5029 UDP is forwarded on host's router
- Check Windows Firewall allows the source port
- Confirm you're using the correct IP (Local IP for LAN, Public IP for Internet)
- Both players must use the same IWAD and mods

**Port forwarding test fails:**
- Check if UPnP is enabled in router settings
- Try manual port forwarding
- Some ISPs block common game ports (use VPN or contact ISP)
- Make sure no other application is using port 5029

### Mods Won't Load

1. Check mod compatibility with your source port
2. Verify mod requires the correct base IWAD
3. Check mod load order (some mods must load before/after others)
4. Look for error messages in source port console

### File Dialogs Don't Work (Linux/Batocera)

Edit `launcher.cfg` manually:
```bash
nano launcher.cfg
```

Set paths directly:
```ini
wads=/path/to/wads
engine=/path/to/engines
```

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl` + `+` | Zoom In |
| `Ctrl` + `-` | Zoom Out |
| `Ctrl` + `0` | Reset Zoom |

---

## 📚 Additional Resources

### Getting DOOM WAD Files

**Legal Options:**
- [Steam](https://store.steampowered.com/app/2280/DOOM/) - DOOM, DOOM II
- [GOG](https://www.gog.com/game/doom_ii) - DOOM II, Final DOOM
- [Bethesda](https://slayersclub.bethesda.net/) - Free DOOM Classic

**Free Alternatives:**
- [Freedoom](https://freedoom.github.io/) - Free IWAD replacement
- [Blasphemer](https://github.com/Blasphemer/blasphemer) - Free Heretic replacement

### Finding Mods

- [Doomworld](https://www.doomworld.com/idgames/) - Largest mod archive
- [ModDB](https://www.moddb.com/games/doom) - Popular mods
- [ZDoom Forums](https://forum.zdoom.org/) - Latest releases
- [Doom Wiki](https://doomwiki.org/) - Comprehensive information

### Source Ports

- [GZDoom](https://zdoom.org/) - Most popular modern port
- [Zandronum](https://zandronum.com/) - Best for multiplayer
- [Chocolate Doom](https://www.chocolate-doom.org/) - Vanilla accuracy

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git

### Clone and Build
```bash
git clone https://github.com/yourusername/DMINLauncher.git
cd DMINLauncher
dotnet restore
dotnet build
```

### Run
```bash
dotnet run
```

### Publish (Single Executable)

**Windows:**
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

**Linux:**
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

Output will be in `bin/Release/net9.0/[runtime]/publish/`

---

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🙏 Credits

### Built With
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform UI framework
- [ReactiveUI](https://www.reactiveui.net/) - MVVM framework
- [Open.NAT](https://github.com/lontivero/Open.NAT) - UPnP port forwarding

### Special Thanks
- id Software - For creating DOOM
- GZDoom Team - For the amazing source port
- DOOM Community - For decades of incredible mods
- Batocera Team - For the retro gaming platform

---

## 📧 Support

- **Issues:** [GitHub Issues](https://github.com/yourusername/DMINLauncher/issues)
- **Discussions:** [GitHub Discussions](https://github.com/yourusername/DMINLauncher/discussions)
- **Doomworld Thread:** [Link to thread]

---

## 🎮 Happy Dooming!

*"Rip and tear, until it is done."*

---

**Version:** 1.0.0  
**Last Updated:** 2024  
**Compatibility:** Windows 10/11, Linux (Ubuntu 20.04+, Debian 11+, Fedora 36+), Batocera v35+
