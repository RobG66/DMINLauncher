# Changelog

All notable changes to DMINLauncher will be documented in this file.

## [1.4.0] - 2026-01-29

### Added
- 🚪 **Window Close Confirmation** - Clicking the X button or pressing Alt+F4 now shows the Exit / Save and Exit / Cancel dialog instead of closing immediately
- 💾 **Load .gzdoom Config** - New button to load a saved `.gzdoom` config file and apply all its command-line arguments back to the UI
- 🎮 **GZDoom Command Line Interpreter** - Full parser for `.gzdoom` preset args: `-iwad`, `-file`, `-skill`, `-warp`, `-deathmatch`, `-host`, `-timer`, `-turbo`, `+set`, `+dmflags`, `+dmflags2`, `+dmflags3`, `+playerclass`, and boolean switches (`-avg`, `-fast`, `-nomonsters`, `-respawn`)
- 📂 **Smart File Dialog Start Locations** - Load/Save Config dialogs open in the program directory; Load/Save GZDoom dialogs open in the configured WAD folder

### Changed
- 🔗 **Cross-Platform File URI Handling** - All file picker dialogs now use a safe `file:///` URI builder that works correctly on both Windows and Linux (fixes silent failures on Linux paths)

### Fixed
- ✅ **Load GZDoom Config** - Previously missing; save existed but load did not

## [1.3.0] - 2026-01-28

### Fixed
- ✅ **Linux Path Separators** - Relative paths now always use forward slashes (`/`), config files are portable between Windows and Linux
- ✅ **Subdirectory Mod Paths** - Mods in subdirectories now display and restore with their full relative path instead of bare filename
- ✅ **WAD Search Case Sensitivity** - File extension matching (`.wad`, `.WAD`, `.pk3`, etc.) is case-insensitive on Linux; saved path lookups also use case-insensitive comparison
- ✅ **Show Maps Button** - Now displays all map names from the selected IWAD instead of truncating at 5
- ✅ **Show Maps Visibility** - Button correctly appears whenever a base game is selected (any valid IWAD has at least one map)
- ✅ **Version Display** - AssemblyVersion was out of sync with Version; title bar now correctly shows 1.3.0

## [1.2.0] - 2026-01-27


### Changed
- 🗂️ **Simplified Mod Management** - Removed Total Conversions tab, unified all WADs/mods in one location
- 🔄 **ResetSettings Behavior** - Now preserves engine paths, WAD paths, and selected IWAD
- 💾 **Save/Load Logic** - SaveSettings/LoadSettings now handle .cfg files, not .gzdoom presets
- 🎨 **Port Forwarding UI** - Compact two-column layout prevents UI jumping during tests

### Fixed
- ✅ **Layout Stability** - Port forwarding test no longer causes border size changes
- ✅ **Config Naming** - SavePaths renamed to SaveConfig for better clarity
- ✅ **IWAD Preservation** - Reset button properly keeps selected IWAD and sets MAP01
- ✅ **Mod Load Order** - Simplified single-list approach replaces complex TC management

### Removed
- ❌ **Total Conversions Tab** - Functionality merged into main Mods/WADs tab
- ❌ **LauncherSettings.cs** - Replaced with improved CfgFileService
- ❌ **Redundant Save Methods** - Consolidated SaveBatoceraConfig functionality

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
- 🔧 Automatic dminlauncher.cfg creation with default paths on first run
- ⚡ Improved first-time user experience - no manual configuration needed
- 💾 **Batocera: Save .gzdoom configuration files** - Create game entries for EmulationStation
- 🎮 Batocera-specific button appears when running on Batocera systems
- 🗺️ **Smart Map Selection** - ComboBox populated with actual maps from selected IWAD
- 📋 Proper ExMy (E1M1, E2M3) and MAPxx (MAP01, MAP15) format support for all games
- 🎯 Automatic map detection from Doom, Doom II, Heretic, Hexen, and Strife IWADs

### Changed
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
- 💾 Configuration persistence in dminlauncher.cfg
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
