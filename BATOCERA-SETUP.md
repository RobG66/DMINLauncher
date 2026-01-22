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
/userdata/roms/doom/
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
/userdata/roms/doom/mods/
```

Or organize in subfolders:
```
/userdata/roms/doom/mods/maps/
/userdata/roms/doom/mods/weapons/
/userdata/roms/doom/mods/gameplay/
```

### Total Conversions
Place total conversion mods in:
```
/userdata/roms/doom/total-conversions/
/userdata/roms/doom/tc/
/userdata/roms/doom/conversions/
```

## Default Engine

Batocera includes GZDoom by default at `/usr/bin/gzdoom`

The launcher will automatically detect it as "gzdoom (system)"

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
1. Check WAD folder: `ls /userdata/roms/doom/*.wad`
2. Verify paths in `launcher.cfg`
3. Click "Refresh" button in the app

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
