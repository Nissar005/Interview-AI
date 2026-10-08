# Architecture Notes

The application follows a modular design with clear boundaries for windowing, settings, AI, memory, security, and diagnostics. The focus is on a native Windows overlay that can be extended with additional providers or device handlers without tightly coupling the UI to cloud APIs.

## Layers

- App: startup and single-instance
- UI: overlay shell and window behavior
- Windows: hotkeys, capture exclusion, interop helpers
- Settings: JSON-backed configuration
- Security: DPAPI-backed credential protection
- AI: provider abstractions and OpenAI-compatible implementation
- Memory: SQLite memory store with relevance queries
