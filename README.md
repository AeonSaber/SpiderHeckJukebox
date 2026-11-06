# SpiderHeck Jukebox

Choose which of the game's battle music tracks play during matches, and add your own songs from a folder to the battle music shuffle.

A mod for SpiderHeck by AeonS. Sister mod: [SpiderHeck Palette](https://github.com/AeonSaber/SpiderHeckPalette).

## Features

- **Choose the battle music.** Pick which of the game's battle music tracks play during matches (survival waves and versus rounds).
- **Your own songs.** Adds your own songs from a folder to the battle music shuffle.
- **Supported files.** .mp3, .ogg, and .wav (8, 16, or 24-bit PCM, or 32-bit float). 32-bit integer WAV is not supported.
- **Sensible shuffle.** Picks a random enabled track; never repeats the track that just finished unless it is the only one enabled. A single enabled track loops.
- **Off switch.** If every track (game and custom) is switched off, battle music is silent, and the menu shows a warning saying so.
- **Untouched when idle.** If nothing is changed (no custom tracks on, no game tracks off), the game's own music code runs untouched.
- **Battle music only.** Only battle music is changed; the lobby theme and the victory theme are not.
- **Client-side only.** It sends nothing over the network. Other players do not need the mod, and each player hears the music chosen on their own PC.

## Requirements

- SpiderHeck with the [Silk](https://github.com/SilkModding/Silk) mod loader
- Silk 0.7.0 or later

No other mods required.

## Installation

1. Install Silk 0.7.0 or later for SpiderHeck.
2. Download `SpiderHeckJukebox.dll` from the [releases page](https://github.com/AeonSaber/SpiderHeckJukebox/releases).
3. Put it in `SpiderHeck\Silk\Mods\` (inside the game folder, usually `C:\Program Files (x86)\Steam\steamapps\common\SpiderHeck`).
4. Start the game once, then put music files in the music folder and press "Reload Music Folder" (or restart the game).

## Usage

### In-game menu

Open Silk's mod list in the game and choose **SpiderHeckJukebox**:

- **Enable Jukebox / Disable Jukebox.**
- The music folder path, with **Open Music Folder** and **Reload Music Folder** buttons.
- **Game tracks:** one [on]/[off] button per game battle track.
- **Your tracks:** one [on]/[off] button per custom file, with its length.
- Changes apply straight away, including during a match.

### Music folder

- Default: `%USERPROFILE%\Music\SpiderHeck Jukebox` (usually `C:\Users\<you>\Music\SpiderHeck Jukebox`). Created automatically on first launch.
- Change it with the `MusicFolder` setting (see [Configuration](#configuration)).
- Files are loaded when the game starts. After adding, removing or replacing files, press **Reload Music Folder** in the mod menu; files that did not change are not loaded again.
- Each loaded track is written to the Silk log, `Silk\Logs\mod_manager.log`, with its length, for example: `[Jukebox] Track 'My Song.mp3': 3:58, 44100 Hz, 2 channel(s)`. A file that cannot be decoded is logged with a warning and skipped.

## Configuration

Settings live in `Silk\Config\Mods\SpiderHeckJukebox.yaml` (created on first launch). The menu writes it; editing by hand is optional. Keys:

- `Enabled`: true or false.
- `MusicFolder`: a folder path, or '' (empty) for the default folder.
- `DisabledNativeTracks`: game tracks that are off, separated by `|`.
- `DisabledCustomTracks`: custom tracks that are off, by file name with extension, separated by `|` (so "song.mp3" and "song.ogg" are separate).

Example:

```yaml
Enabled: true
MusicFolder: ''
DisabledNativeTracks: Starthropod|Temple Toss
DisabledCustomTracks: ''
```

## Limitations

- Music is decoded into memory when it loads, so a very large folder uses more RAM and adds to the loading time.
- Only battle music is changed; the lobby and victory themes are not.

## How it works

- A Harmony prefix on `AudioController.PlayBattleTrack`, which the game calls every frame while battle music is on. Vanilla restarts the music whenever the playing clip is not in its own `battleTrackList`, so the mod replaces that check with its own pool of enabled game and custom clips.
- A postfix on `Announcer.ModsPopup` builds the settings page in Silk's mod menu.
- Custom files load on a `DontDestroyOnLoad` object: .mp3 and .ogg through `UnityWebRequestMultimedia`, .wav through a small built-in parser.
- No ILRepack: the DLL binds against the assemblies Silk already loads.

## Building from source

```
dotnet build -c Release
```

Requires the .NET SDK. Target framework: `net472`. Output: `bin\Release\net472\SpiderHeckJukebox.dll`.

The project references DLLs in a `lib\` folder, which is not in the repository (the game's and Silk's files are not redistributed). Copy them from your own install:

- From `SpiderHeck\Silk\Library\`: Silk.dll, 0Harmony.dll, YamlDotNet.dll, NewAssembly.dll, MyScriptAssembly.dll, UnityEngine.CoreModule.dll, UnityEngine.UI.dll, Unity.TextMeshPro.dll, Unity.Netcode.Runtime.dll, Unity.Collections.dll, Unity.Mathematics.dll, DoozyEngine.dll, I2.dll
- From `SpiderHeck\SpiderHeckApp_Data\Managed\`: UnityEngine.AudioModule.dll, UnityEngine.UnityWebRequestModule.dll, UnityEngine.UnityWebRequestAudioModule.dll

## Credits

Built on [Silk](https://github.com/SilkModding/Silk) and [Harmony](https://github.com/pardeike/Harmony).

## License

[MIT](LICENSE)
