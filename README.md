# Private AI Overlay

This project is a Windows 11 WPF overlay designed for private, keyboard-driven AI assistance. The current implementation provides a WPF foundation, single-instance behavior, system tray-style controls, capture exclusion via Win32, hotkeys, secure API key storage, and persistent memory backed by SQLite.

## Feature Highlights

- Semi-transparent borderless overlay window
- Single-instance enforcement
- Taskbar suppression and always-on-top support
- Global hotkeys for toggle and voice actions
- `SetWindowDisplayAffinity` with `WDA_EXCLUDEFROMCAPTURE`
- Settings stored in JSON under LocalAppData
- Secure credential storage with Windows DPAPI
- Persistent memory via SQLite
- Provider abstraction for AI and vision services

## Build

```powershell
dotnet build
```

## Run

```powershell
dotnet run
```

## Privacy notice

The capture exclusion is applied to the top-level overlay window using supported Windows APIs. It does not guarantee protection against cameras, unsupported capture paths, or external hardware capture.
