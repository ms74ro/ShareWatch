# ShareWatch

[Română](README.ro.md)

ShareWatch is a small Windows tray app that tells you when another computer on the network opens your shared folders. Each notification shows the IP address and the computer name, and every access is written to a log.

It is a single executable with no installer and no dependencies beyond what ships with Windows (.NET Framework 4).

![Monitoring tab](docs/screenshots/en/monitor.png)

More screenshots (English and Romanian, light theme, made-up data) are in [docs/screenshots](docs/screenshots).

## Features

- **Live notifications** when a computer connects to a shared folder, with IP and computer name, optional subtle sound and configurable duration.
- **Monitoring tab**: the computers connected right now. Right-click one to disconnect or block it.
- **Shared folders**: choose which shares are monitored. Share a new folder or stop sharing one directly from the app.
- **Hide administrative shares** (C$, ADMIN$ etc.) from the lists and tray menu. On by default.
- **Block list**: listed computers are disconnected as soon as they connect. Stored in `blocklist.txt`, editable in the app or in the file.
- **Optional Windows Firewall blocking**: one inbound rule (`ShareWatch - blocare SMB`) blocks TCP 139/445 from blocked IPs.
- **History**: access log in `acces_foldere.log`.
- **Start with Windows** straight to the tray, with administrator rights and no UAC prompt.
- Optional: open the window with a single click on the tray icon.
- Light / dark / automatic theme; Romanian and English built in, other languages from `language\xx.json` files (see [Languages](#languages)).

## Requirements

- Windows 10 or 11 (.NET Framework 4.x, included in Windows).
- Administrator rights. Without them Windows does not let the app see SMB sessions, so monitoring does not work.

## Usage

1. Download `ShareWatch-x.y.z.zip` from [Releases](https://github.com/ms74ro/ShareWatch/releases/latest) and extract it to a folder of your choice. The zip holds `ShareWatch.exe`, the `language` folder, this README, the changelog, the license and the source code (`source` folder). The bare `.exe` is not attached because some browsers' web protection (Bitdefender, for example) blocks downloading it from GitHub.
2. Run `ShareWatch.exe` and accept the administrator prompt.
3. The icon appears in the tray. Double-click it to open the window.

Settings are saved in `sharewatch.ini` next to the executable (or in `%LOCALAPPDATA%\ShareWatch` if that folder is not writable).

## Languages

Romanian and English are built into the executable. Other languages are loaded from JSON files in a `language` folder next to `ShareWatch.exe`:

```
ShareWatch.exe
language\
    de.json
    fr.json
```

Ready-made language files are in the [`language`](language) folder: **Deutsch** (`de.json`), **Français** (`fr.json`), **Italiano** (`it.json`) and **Español** (`es.json`). Copy the ones you want into a `language` folder next to the executable.

To add another language:

1. Copy [`language/en.json`](language/en.json) as `xx.json`, where `xx` is the two-letter language code (`de.json`, `fr.json`, ...).
2. Set `_name` to the language name as it should appear in Options (e.g. `Deutsch`).
3. Translate only the values. Keep the keys (the Romanian text) and placeholders such as `{0}` unchanged.
4. Restart ShareWatch and pick the language in Options → Language. With **Automatic**, the language matching Windows is used if a file for it exists.

Texts missing from a file are shown in English, and an invalid JSON file is ignored. `ro.json` and `en.json` are never loaded (both languages are built in), so `en.json` can stay in the folder as a template.

## Antivirus warnings

Some antivirus programs may flag `ShareWatch.exe` as suspicious. An older version was flagged by at least one antivirus. This is a false positive: heuristic scanners react to what the app needs to do, together with the fact that the executable is small, new and not digitally signed:

- it asks for administrator rights and starts with Windows through a scheduled task;
- it runs hidden in the tray;
- it reads SMB sessions and can disconnect computers;
- it can add a Windows Firewall rule.

On [VirusTotal](https://www.virustotal.com/gui/file/08e83c2792a22b23a92722e4d9c73ac6886c87afc4611485e621411f9b47aa4b) (version 0.7.4, November 2026), 5 of 68 engines flag it. All 5 results come from machine-learning or heuristic analysis, not from a known malware signature:

| Engine | Result |
|---|---|
| Bkav Pro | W32.Malware.E414854D |
| Elastic | Malicious (high Confidence) |
| Microsoft | Trojan:Win32/Wacatac.C!ml |
| SecureAge | Malicious |
| Symantec | ML.Attribute.HighConfidence |

The other 63 engines report it as clean, including BitDefender, Kaspersky, ESET-NOD32, Avast and AVG.

The Microsoft result comes and goes between builds: 0.7.2 and 0.7.4 were flagged, 0.7.0, 0.7.1 and 0.7.3 were not, with almost the same code. The `!ml` suffix means a machine-learning model, not a signature, so it scores each new build afresh. In practice **Microsoft Defender** may block or quarantine the file. If that happens, you can restore it from Windows Security → Protection history and report it as a false positive at [Microsoft's file submission page](https://www.microsoft.com/en-us/wdsi/filesubmission).

On the author's PC, **Bitdefender Antivirus Plus** does not flag the file, but its web protection blocked downloading the bare `ShareWatch.exe` from GitHub ("Dangerous page blocked"), which is why releases ship as a `.zip`.

If your antivirus flags it:

1. Read the source code. It is all in this repository, with no external libraries.
2. Build the executable yourself with `.\source\build.ps1` (see below) instead of downloading it.
3. Or check that the downloaded file matches the one in the release. The release notes give its SHA-256. Compare it with:

   ```powershell
   Get-FileHash .\ShareWatch.exe -Algorithm SHA256
   ```

4. You can scan the file on [VirusTotal](https://www.virustotal.com) and report the false positive to your antivirus vendor.

## Building

No Visual Studio needed. The script uses `csc.exe` from .NET Framework 4, which is part of Windows. The source code is in the `source` folder, both in this repository and in the release zip:

```powershell
.\source\build.ps1
```

The exe is created in the `source` folder.

`.\source\build.ps1 -Test` builds `source\ShareWatch-test.exe` without the administrator manifest, for testing the interface only.

The README screenshots come from the test build with made-up shares, computers and log entries (code in `source/Demo.cs`, compiled only into the test build). Put the test exe in a folder with a `sharewatch.ini` containing `theme=1` and `lang=2` (or `lang=1` for Romanian), then run:

```powershell
.\ShareWatch-test.exe --screenshots .\shots
```

It saves one PNG per tab plus two notifications (a normal one and one for a blocked computer), then exits.

`language/en.json` is generated from the built-in English texts. After adding or changing texts, regenerate it with the test build:

```powershell
.\source\ShareWatch-test.exe --export-lang language\en.json
```

## Files

| File | Contents |
|---|---|
| `source/ShareWatch.cs` | Entry point, settings, NetApi (shares and sessions) |
| `source/MainForm.cs` | Main window and tabs |
| `source/Tray.cs` | Tray icon, menu, monitoring loop |
| `source/Toast.cs` | Notification window |
| `source/ShareDialog.cs` | Share / stop sharing a folder |
| `source/BlockList.cs` | Block list |
| `source/Firewall.cs` | Windows Firewall rule |
| `source/Support.cs` | Translations and helpers |
| `source/Demo.cs` | Made-up data and screenshots (test build only) |
| `source/build.ps1` | Build script |
| `source/app.manifest` | Administrator manifest |
| `language/` | Language files (`en.json` is the template) |

## Changes

See [CHANGELOG.md](CHANGELOG.md).

## Contributors

- [ms74ro](https://github.com/ms74ro) — author
- [Claude](https://claude.com/claude-code) (Anthropic) — code and documentation, working with ms74ro

## License

[MIT](LICENSE) © 2026 ms74ro. A Romanian translation, for information only, is in [LICENTA.md](LICENTA.md).
