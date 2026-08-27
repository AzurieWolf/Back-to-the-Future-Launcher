# Back to the Future Episode Launcher

A better launcher for the classic Back to the Future: The Game by Telltale Games.
with a small, fixed-size Windows launcher that reads its episode list and background image from `launcher.ini`.

## Folder layout

Place the published launcher, the INI file, your background, and episode folders together:

```text
Launch Back to the Future - The Game.exe
launcher.ini
background.jpg
Episode 1/
  BackToTheFuture101.exe
Episode 2/
  BackToTheFuture102.exe
Episode 3/
  BackToTheFuture103.exe
Episode 4/
  BackToTheFuture104.exe
Episode 5/
  BackToTheFuture105.exe
```

All relative paths in `launcher.ini` start from the folder containing `Launcher.exe`. Both `/` and `\` separators work. The game is started with its own episode folder as the working directory. While it runs, episode selection and Settings are locked and the Play button becomes **Force Stop**. A forced stop keeps the launcher open; if the game exits normally, the launcher closes automatically.

## Application icon

Set the window and taskbar icon in the `[launcher]` section. The path is relative to `Launcher.exe` and does not need to match the executable's filename:

```ini
[launcher]
icon=art/my-custom-icon.ico
```

Place the configured ICO in the published folder using the same relative path. If the configured file is missing or invalid, the launcher uses the icon embedded in `Launcher.exe` as a fallback. The included default is `launcher.ico`; if that source file is missing during a build, `build.bat` creates it from `launcher-icon-source.png` using `create-icon.ps1`.

## Configuration

The `[launcher]` section controls the window. `background` accepts a JPG, PNG, BMP, or GIF filename/path. `width` and `height` are optional; the window is always non-resizable.

Every other section with both a `name` and `executable` becomes a radio option:

```ini
[launcher]
title=Back to the Future: The Game
heading=SELECT AN EPISODE
background=background.png
icon=launcher.ico
width=960
height=600

[episode1]
name=Episode 1 - It's About Time!
executable=Episode 1/BackToTheFuture101.exe
preferences=%USERPROFILE%/Documents/Telltale Games/Episode 1/prefs.prop
```

The optional `preferences` path enables the Settings button. It may be relative to `Launcher.exe`, absolute, or contain environment variables such as `%USERPROFILE%`. For each episode, the launcher recognizes both `Episode N\prefs.prop` and `Back to the Future N\prefs.prop`; `Episode N` takes priority when both exist. Settings are loaded from the selected episode, but **Save All** applies the complete profile to every configured episode. The editor supports screen resolution, full-screen mode, graphics quality, shadow quality, anti-aliasing, effects, subtitles, and music/voice/effects volume. Shadow Quality and Effects are unavailable at Graphics Quality 6 or lower, matching the game. Effects volume updates both of the game's underlying Sound and Ambient channels. Every changed file gets a `prefs.prop.bak` backup.

## Build

### Batch files

- Run `install-requirements.bat` to check for a compatible .NET SDK (version 8 or newer) and install the .NET 8 SDK with Windows Package Manager when needed.
- Run `build.bat` to create the standalone `dist\Launcher.exe` release.
- Run `run.bat` to start the launcher. If it has not been built yet, the batch file runs the build first.

### Command line

Build normally (requires the .NET 8 SDK):

```powershell
dotnet build -c Release
```

Create a single-file 64-bit Windows executable that does not require .NET to be installed:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None -p:DebugSymbols=false -o dist
```

The publish output is under `dist/`. Keep `launcher.ini` and the configured background image beside `Launcher.exe`.
