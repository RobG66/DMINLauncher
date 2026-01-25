# Changelog

All notable changes to DMINLauncher will be documented in this file.

## [1.1.0] - 2026-01-25

### Added
- 🐧 **Flatpak Engine Support** - Linux/Batocera users can now select Flatpak engines (e.g., org.zdoom.GZDoom)
- 🔄 **Dual Engine Storage** - Separate storage for file path and Flatpak engines, switch between them without losing selections
- 📂 **Subdirectory WAD Scanning** - IWADs directory now scans subdirectories for WAD files
- 🎨 **Compact Radio Buttons** - Smaller, cleaner radio button style (30% smaller circles)
- ⚠️ **Flatpak Permission Management** - Automatic filesystem permission configuration using `flatpak override --user`
- ⌨️ **ESC Key Exit Confirmation** - Press ESC to show exit dialog, ESC again to cancel, Enter to confirm
- 🚪 **Exit Confirmation Dialog** - Exit button and ESC key both show "Are you sure?" confirmation

### Changed
- 🎮 **Engine Selection UI** - Radio buttons for File Path vs Flatpak (Linux only), cleaner layout
- 📊 **IWAD Info Display** - Shows full file path and complete map list instead of redundant stats
- 🎨 **UI Refinements** - Aligned spacing between WADs and Engine borders, colored bottom action buttons
- ⚙️ **Batocera Defaults** - Default engine path changed from `/userdata/roms/ports/engines/gzdoom` to `/usr/bin/gzdoom`
- 📝 **Config File Format** - Now stores separate `enginefilepath` and `engineflatpak` values

### Fixed
- ✅ **Flatpak Dialog Positioning** - Dialogs now properly sized on Batocera (not fullscreen)
- ✅ **ESC Key Handling** - All dialogs can be closed with ESC key
- ✅ **Radio Button Visibility** - Properly hidden on Windows, shown on Linux/Batocera

## [1.0.3]

### Changed
- 🎨 Switched to fluent compact theme
- 🔍 Updated search logic

## [1.0.2] 

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

## [1.0.0] - First Release

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

[1.0.2]: https://github.com/RobG66/DMINLauncher/releases/tag/v1.0.2
[1.0.0]: https://github.com/RobG66/DMINLauncher/releases/tag/v1.0.0
