# DMINLauncher v1.0.3

<img width="2045" height="1872" alt="image" src="https://github.com/user-attachments/assets/979168ae-bc2c-493c-bb29-076cdd8aded5" />


**A cross-platform launcher for DOOM source ports**

DMINLauncher is a simple launcher for DOOM engines.  It is built with .NET 9 and Avalonia UI. 

One of the main goals was to provide a consistent launcher experience that is easy to use with Batocera, Retrobat or standalone in either Windows or Linux.  Cross-platform compatibility was essential for this to happen.  

 If you wish, you can help support this and other projects I am working on.  If not, that is ok too.

- [PayPal](https://paypal.me/RGanshorn?country.x=CA&locale.x=en_US)
- [Buy Me a Coffee](https://buymeacoffee.com/silverballb)
---

## Features

- Launch DOOM source ports from Windows, Linux, Batocera or standalone.
- Load IWAD files (DOOM, DOOM II, Heretic, Hexen, etc.)
- Map count detection for WAD/PK3 files
- Set difficulty, starting map, and game options
- DMFLAGS editor (DMFLAGS, DMFLAGS2, DMFLAGS3)
- Basic multiplayer support (LAN/Internet hosting)
- UPnP port forwarding helper
- Saves settings to launcher.cfg
- UI zoom (Ctrl+Plus/Minus)
- **🆕 Batocera: Save .gzdoom configuration files for EmulationStation integration**

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

For Batocera, WAD files go in `/userdata/roms/gzdoom/`

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

## Credits

- [Avalonia UI](https://avaloniaui.net/) - UI framework
- [ReactiveUI](https://www.reactiveui.net/) - MVVM framework
- [Open.NAT](https://github.com/lontivero/Open.NAT) - UPnP library
- id Software - For DOOM
- DOOM Community - For decades of mods

---

**Version:** 1.0.2  
