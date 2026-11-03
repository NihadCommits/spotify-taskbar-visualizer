# Spotify Taskbar Visualizer

[English](#english) · [Deutsch](#deutsch)

```
[Cover] Title / Artist   ⤮  ⏮  ⏯  ⏭  ↻   0:42 ━━━━━━━━──── 2:44
```

## English

Spotify controls for Windows that look like part of the taskbar: a small transparent overlay that sits in a free spot of the taskbar.

### Features

- Cover, title and artist of the current song
- Play/pause, next, previous, shuffle and repeat; click the progress bar to seek
- Clicks never take the focus away from the window you are working in
- Avoids taskbar icons and the tray: switches between full, compact (title + controls) and hidden depending on the free space
- Hides while a fullscreen app (video, game) covers the taskbar
- Click the cover or title to bring Spotify to the front
- Stays invisible in the background while Spotify is not running

### Requirements

- Windows 11 (Windows 10 should work too)
- Spotify desktop app
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build

### Build and install

Try it straight from the repo:

```powershell
dotnet run
```

Install it to a fixed folder and run it from there:

```powershell
dotnet publish -c Release -o "$env:LOCALAPPDATA\SpotifyTaskbarVisualizer"
& "$env:LOCALAPPDATA\SpotifyTaskbarVisualizer\SpotifyTaskbarVisualizer.exe"
```

Then enable **Start with Windows** in the tray menu. The autostart entry always points to the exe that is running, so enable it from the installed copy.

> **Smart App Control:** If Smart App Control is on in Windows, it may block self-built, unsigned builds. Windows then reports that parts of the app were blocked, and the app closes right away. On the test machine only the Release build was blocked; the Debug build (`dotnet run`, or `bin\Debug\net8.0-windows10.0.19041.0\SpotifyTaskbarVisualizer.exe` after `dotnet build`) ran fine and can be used for autostart as well. Alternatively, turn Smart App Control off under Windows Security → App & browser control. Note that it can only be turned back on by resetting Windows.

### Usage

- The widget shows up as soon as Spotify has played something.
- Tray icon (on Windows 11 possibly behind the **^** arrow): *Start with Windows*, *Debug overlay* (outlines the detected icons and the widget position), *Exit*.
- Right-click the widget: *Exit*.

### How it works

- Song info and controls come from the Windows media controls (SMTC), no Spotify API or login.
- UI Automation reads the position of all taskbar buttons once per second to find free space.
- A borderless, transparent, non-activating window is kept on top of the taskbar. No changes to Explorer, no admin rights.

### Limitations

- Spotify only, other players are ignored
- Primary taskbar only, docked at the bottom; hidden while taskbar autohide is on
- No "like" button (not available through SMTC)

### License

[MIT](LICENSE)

## Deutsch

Spotify-Steuerung für Windows, die aussieht wie ein Teil der Taskleiste: ein kleines transparentes Overlay, das auf einem freien Platz der Taskleiste sitzt.

### Features

- Cover, Titel und Artist des aktuellen Songs
- Play/Pause, Vor, Zurück, Shuffle und Repeat; Klick in den Fortschrittsbalken zum Spulen
- Klicks nehmen dem Fenster, in dem du gerade arbeitest, nie den Fokus
- Weicht Taskleisten-Icons und Tray aus: wechselt je nach freiem Platz zwischen voll, kompakt (Titel + Steuerung) und ausgeblendet
- Verschwindet, solange eine Vollbild-App (Video, Spiel) die Taskleiste verdeckt
- Klick auf Cover oder Titel holt Spotify in den Vordergrund
- Bleibt unsichtbar im Hintergrund, solange Spotify nicht läuft

### Voraussetzungen

- Windows 11 (Windows 10 sollte auch gehen)
- Spotify Desktop-App
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) zum Bauen

### Bauen und installieren

Direkt aus dem Repo ausprobieren:

```powershell
dotnet run
```

In einen festen Ordner installieren und von dort starten:

```powershell
dotnet publish -c Release -o "$env:LOCALAPPDATA\SpotifyTaskbarVisualizer"
& "$env:LOCALAPPDATA\SpotifyTaskbarVisualizer\SpotifyTaskbarVisualizer.exe"
```

Danach im Tray-Menü **Start with Windows** einschalten. Der Autostart zeigt immer auf die Exe, die gerade läuft, also in der installierten Kopie einschalten.

> **Smart App Control:** Ist in Windows Smart App Control eingeschaltet, kann es selbst gebaute, unsignierte Builds blockieren. Windows meldet dann, dass Teile der App blockiert wurden, und die App beendet sich sofort. Auf dem Testrechner war nur der Release-Build betroffen; der Debug-Build (`dotnet run`, oder nach `dotnet build` die `bin\Debug\net8.0-windows10.0.19041.0\SpotifyTaskbarVisualizer.exe`) lief problemlos und kann auch für den Autostart genutzt werden. Alternativ Smart App Control unter Windows-Sicherheit → App- und Browsersteuerung ausschalten. Achtung: Wieder einschalten lässt es sich nur durch Zurücksetzen von Windows.

### Bedienung

- Das Widget erscheint, sobald Spotify einmal etwas abgespielt hat.
- Tray-Icon (bei Windows 11 eventuell hinter dem Pfeil **^**): *Start with Windows*, *Debug overlay* (zeigt die erkannten Icons und die Widget-Position als Rahmen), *Exit*.
- Rechtsklick aufs Widget: *Exit*.

### So funktioniert es

- Song-Infos und Steuerung kommen aus der Windows-Mediensteuerung (SMTC), ohne Spotify-API und ohne Login.
- UI Automation liest einmal pro Sekunde die Position aller Taskleisten-Buttons und findet so den freien Platz.
- Ein randloses, transparentes, nicht aktivierbares Fenster wird über der Taskleiste gehalten. Keine Eingriffe in den Explorer, keine Admin-Rechte.

### Grenzen

- Nur Spotify, andere Player werden ignoriert
- Nur die primäre Taskleiste, unten angedockt; ausgeblendet, solange Autohide aktiv ist
- Kein "Gefällt mir"-Button (über SMTC nicht verfügbar)

### Lizenz

[MIT](LICENSE)
