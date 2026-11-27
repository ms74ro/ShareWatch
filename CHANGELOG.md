# ShareWatch – Changelog

All notable changes to ShareWatch are listed here, newest first.

## 0.7.0
- Other languages: put `language\xx.json` next to the executable and pick the language in Options (or leave Automatic to follow Windows). Romanian and English stay built in. `language\en.json` is the template for translators; texts missing from a file are shown in English.
- New option, off by default: open the window with a single click on the tray icon.

## 0.6.2
- "Hide administrative shares" is now on by default. An existing `sharewatch.ini` keeps the saved choice.

## 0.6.1
- New option: hide administrative shares (C$, ADMIN$ etc.) from the Monitored folders list and the tray menu. Hidden shares keep their monitoring setting.

## 0.6.0
- Optional Windows Firewall blocking for the block list: one inbound rule (`ShareWatch - blocare SMB`) blocks TCP 139/445 from the listed IPs; computer names are resolved to IPs and refreshed every 10 minutes.
- The firewall rule can be removed at any time from Options (button), and is removed automatically when firewall blocking or the block list is turned off.
- New option, off by default: notification when a blocked computer is disconnected. These notifications never play a sound.

## 0.5.0
- Block list: computers on it are disconnected as soon as they connect (every SMB session, on any share).
- The list is stored in `blocklist.txt` next to the executable; it can be edited in Options or directly in the file (changes apply within seconds).
- Right-click a connected computer in Monitoring to block it (adds its IP and name).
- Dedicated red notification and a BLOCKED event in History.

## 0.4.1
- Fixed: the tray icon could be missing after a Windows restart even though the app was running. The app now waits for the taskbar, re-adds the icon, and restores it if Explorer restarts.
- Automatic start now begins 10 seconds after sign-in. If it was already enabled, turn it off and on again to apply this.
- The changelog moved out of the app into this file (`CHANGELOG.md`), shown as a link in About.

## 0.4.0
- Icons on the tabs.
- Right-click a connected computer to disconnect it.
- Share a new folder or stop sharing one directly from the app.

## 0.3.2
- Tray menu check marks are visible in both themes.
- Product information added to the executable.

## 0.3.1
- Selected rows are readable in every theme.
- Author credit in About.

## 0.3.0
- Theme: automatic (follows Windows), light or dark.
- Languages: Romanian and English (automatic based on Windows).
- Optional subtle notification sound.
- The window keeps its position and size after a restart.
- Narrower notification.

## 0.2.0
- Tabbed window: Monitoring, Monitored folders, History, Options, About.
- Choose which shared folders are monitored.
- Larger, DPI-aware notification with configurable duration.
- Automatic start with Windows launches straight to the tray (`--tray`).
- Launching the app a second time opens the window of the running instance.

## 0.1.0
- First version: tray icon, notifications with the IP and computer name, access log.
