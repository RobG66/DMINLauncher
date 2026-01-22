# DMINLauncher - Batocera Setup Guide

## Installation

1. **Copy DMINLauncher to Ports folder:**
   ```
   /userdata/roms/ports/DMINLauncher/DMINLauncher-linux-x64
   ```

2. **Make executable:**
   ```bash
   chmod +x /userdata/roms/ports/DMINLauncher/DMINLauncher-linux-x64
   ```

3. **Copy the configuration template:**
   ```bash
   cp launcher.cfg.batocera-template launcher.cfg
   ```

## Batocera Default Locations

### WAD Files
Place your DOOM WAD files in:
```
/userdata/roms/gzdoom/
```

Supported files:
- `doom.wad` - DOOM
- `doom2.wad` - DOOM II
- `plutonia.wad` - The Plutonia Experiment
- `tnt.wad` - TNT: Evilution
- `heretic.wad` - Heretic
- `hexen.wad` - Hexen
- `strife1.wad` - Strife

### Mods
Place mod files (.wad, .pk3) in:
```
/userdata/roms/gzdoom/mods/
```

Or organize in subfolders:
```
/userdata/roms/gzdoom/mods/maps/
/userdata/roms/gzdoom/mods/weapons/
/userdata/roms/gzdoom/mods/gameplay/
```

## Engine Detection

**Batocera includes GZDoom at `/usr/bin/gzdoom`**

The launcher will automatically detect it as **"gzdoom (system)"** - you don't need to set the engine directory to `/usr/bin`.

Set the engine directory to a custom folder (like `/userdata/roms/ports/engines`) if you want to add other engines. The system `gzdoom` will still be auto-detected.

## Creating a Batocera Port Entry

Create a file: `/userdata/roms/ports/DMINLauncher.sh`

```bash
#!/bin/bash
# DMINLauncher Port Entry

cd /userdata/roms/ports/DMINLauncher
export DISPLAY=:0
./DMINLauncher-linux-x64
```

Then make it executable:
```bash
chmod +x /userdata/roms/ports/DMINLauncher.sh
```

## Troubleshooting

### App doesn't start
1. Check execute permissions: `chmod +x DMINLauncher-linux-x64`
2. Verify DISPLAY is set: `echo $DISPLAY` (should show `:0`)
3. Run from terminal to see errors: `./DMINLauncher-linux-x64`

### No WADs detected
1. Check WAD folder: `ls /userdata/roms/gzdoom/*.wad`
2. Verify paths in `launcher.cfg`:
   ```
   wads=/userdata/roms/gzdoom
   engine=/userdata/roms/ports/engines
   ```
3. Click "Refresh" button in the app

### No engines detected
- The launcher should auto-detect `gzdoom (system)` from `/usr/bin/gzdoom`
- If it doesn't appear, run `which gzdoom` to verify it's in PATH
- Don't set engine directory to `/usr/bin` - it will scan hundreds of files
- Use a dedicated folder or leave it pointing to a non-existent path

### File dialogs don't work
This is a known issue with Batocera's minimal desktop environment. Use the configuration file method instead:

1. Edit `launcher.cfg` manually
2. Set paths:
   ```
   wads=/userdata/roms/doom
   engine=/usr/bin
   ```

## Configuration File

The `launcher.cfg` file stores all settings. You can edit it manually if needed.

Example for DOOM II with Brutal Doom:
```
wads=/userdata/roms/doom
engine=/usr/bin
basegame=doom2.wad
difficulty=2
selectedengine=gzdoom (system)
```

Then add mods through the UI or launch directly from command line.

## Command Line Launch

You can also launch directly with GZDoom:
```bash
gzdoom -iwad /userdata/roms/doom/doom2.wad \
       -file /userdata/roms/doom/mods/brutalv21.pk3
```

But the launcher provides a nice GUI for managing configurations! 🎮
