# Windows API Notes

The app relies on native Win32 interop where appropriate:

- `RegisterHotKey` for global shortcut support
- `SetWindowDisplayAffinity` for screen-share exclusion
- `SetForegroundWindow` and `AllowSetForegroundWindow` for single-instance activation

The overlay is also configured as a taskbar-suppressed, borderless top-level window to behave similarly to a native utility overlay.
