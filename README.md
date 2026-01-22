# DMINLauncher v1.0.1

<img width="2045" height="1872" alt="image" src="https://github.com/user-attachments/assets/a3865d61-7c92-48aa-9be3-da386e688345" />

**A cross-platform launcher for DOOM source ports**

DMINLauncher is a simple launcher for DOOM engines built with .NET 9 and Avalonia UI. It helps you organize your WAD files, load mods, and launch games.

## Support the Project

[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-Support-FFDD00?logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/silverballb)
[![PayPal](https://img.shields.io/badge/PayPal-Donate-00457C?logo=paypal&logoColor=white)](https://paypal.me/RGanshorn?country.x=CA&locale.x=en_US)

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)

---

## Features

- Launch DOOM source ports (UZDoom, GZDoom, Zandronum, etc.)
- Load IWAD files (DOOM, DOOM II, Heretic, Hexen, etc.)
- Map count detection for WAD/PK3 files
- Set difficulty, starting map, and game options
- DMFLAGS editor (DMFLAGS, DMFLAGS2, DMFLAGS3)
- Basic multiplayer support (LAN/Internet hosting)
- UPnP port forwarding helper
- Saves settings to launcher.cfg
- UI zoom (Ctrl+Plus/Minus)

---

## Installation

**Note:** Binaries include .NET 9 runtime - no separate installation needed.

### Windows

1. Download `DMINLauncher-win-x64.exe` from [releases](https://github.com/RobG66/DMINLauncher/releases)
2. Run the executable
3. Set Data Directory (where your WAD files are)
4. Set Engine Directory (where your source port executables are)

### Linux

```bash
chmod +x DMINLauncher-linux-x64
./DMINLauncher-linux-x64
```

### Batocera

For Batocera, WAD files go in `/userdata/roms/gzdoom/` and the default config uses:

See [BATOCERA-SETUP.md](BATOCERA-SETUP.md) for full setup instructions.

---

## Quick Start

1. Set Data Directory (folder with WAD files)
2. Set Engine Directory (folder with engine executables)
3. Select your IWAD (DOOM.WAD, DOOM2.WAD, etc.)
4. Select your engine (uzdoom, gzdoom, etc.)
5. (Optional) Add mods from the right panel
6. Click "Launch Game!"

---

## Multiplayer

### LAN
1. Select "Host LAN"
2. Share your local IP with other players
3. Click "Launch Game"

### Internet
1. Select "Host Internet"
2. Forward port 5029 UDP on your router (or use UPnP auto-config)
3. Share your public IP with other players
4. Click "Launch Game"

### Join
1. Select "Connect"
2. Enter host's IP address
3. Click "Launch Game"

---

## Troubleshooting

**No WAD files showing:**
- Click "Change Data Directory"
- Select folder containing .wad files

**No engines showing:**
- Click "Change Engine Directory"
- Place engine executables in that folder
- On Linux, ensure files have execute permission

---

## License

MIT License - see LICENSE file

---

## Credits

- [Avalonia UI](https://avaloniaui.net/) - UI framework
- [ReactiveUI](https://www.reactiveui.net/) - MVVM framework
- [Open.NAT](https://github.com/lontivero/Open.NAT) - UPnP library
- id Software - For DOOM
- DOOM Community - For decades of mods

---

**Version:** 1.0.1  
