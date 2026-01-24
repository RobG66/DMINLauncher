# Changelog

All notable changes to DMINLauncher will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2024-01-15

### Added
- 🔧 Automatic launcher.cfg creation with default paths on first run
- ⚡ Improved first-time user experience - no manual configuration needed
- 💾 **Batocera: Save .gzdoom configuration files** - Create game entries for EmulationStation
- 🎮 Batocera-specific button appears when running on Batocera systems
- 🗺️ **Smart Map Selection** - ComboBox populated with actual maps from selected IWAD
- 📋 Proper ExMy (E1M1, E2M3) and MAPxx (MAP01, MAP15) format support for all games
- 🎯 Automatic map detection from Doom, Doom II, Heretic, Hexen, and Strife IWADs

### Changed
- 📝 Batocera users no longer need to manually copy launcher.cfg.batocera-template
- 🎯 Default configuration is now automatically generated with platform-appropriate paths
- 📁 .gzdoom files can be saved directly to `/userdata/roms/gzdoom/` on Batocera
- 🔢 Starting Map now shows actual map names instead of numeric input
- 🖼️ Mod management buttons now use icon images (add.png, remove.png, up.png, down.png)
- 🪟 Window title now shows clean version number without git hash

### Fixed
- ✅ Batocera button now appears on the same line as status message for better space utilization

## [1.0.0] - 2024-01-XX

### Added
- 🎮 Multi-engine support (GZDoom, Zandronum, Chocolate Doom, etc.)
- 📁 Smart file management for IWADs, PWADs, and PK3 mods
- 🗺️ Automatic map detection and counting for WAD/PK3 files
- 🔄 Drag-and-drop mod load order management
- ⚙️ Full DMFLAGS, DMFLAGS2, and DMFLAGS3 editor
- 🌐 Multiplayer support (LAN and Internet)
- 📡 UPnP automatic port forwarding
- 🔌 UDP port 5029 testing for Doom multiplayer
- 🌍 Automatic local and public IP detection
- 👥 Player count configuration (1-16 players)
- 🎯 Game modes: Cooperative, Deathmatch, Team Deathmatch
- 📋 Launch summary preview
- 🎲 Quick start options (fast monsters, no monsters, respawning items, turbo mode)
- ⏱️ Time limit configuration
- 🧙 Hexen class selection (Fighter, Cleric, Mage)
- 📦 Full PK3/PK7/ZIP mod archive support
- 🐧 Flatpak engine support (Linux only)
- 💾 Configuration persistence in launcher.cfg
- 🔍 UI zoom controls (Ctrl+Plus/Minus)
- 📖 Comprehensive README and Batocera setup guide
- 🔧 Linux diagnostic script

### Features
- Cross-platform support (Windows, Linux, Batocera)
- Self-contained executables with bundled .NET runtime
- Modern Avalonia UI with reactive MVVM architecture
- Automatic engine detection
- WAD file parser with format validation
- Network mode switching (None/LAN/Internet/Connect)
- Working directory management for engines
- Version display in title bar

### Technical
- Built with .NET 9 and Avalonia UI 11.0.10
- ReactiveUI for MVVM pattern
- Open.NAT for UPnP port forwarding
- Support for WAD, PWAD, PK3, PK7, IPK3, and ZIP formats

[1.0.2]: https://github.com/RobG66/DMINLauncher/releases/tag/v1.0.2
[1.0.0]: https://github.com/RobG66/DMINLauncher/releases/tag/v1.0.0
