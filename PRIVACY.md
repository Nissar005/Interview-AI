# Privacy and screen-share protections

This application intentionally excludes the overlay window from supported Windows capture APIs via `SetWindowDisplayAffinity` with `WDA_EXCLUDEFROMCAPTURE`.

The app does not claim universal invisibility. Screen-share protection is limited to supported Windows capture APIs and cannot guarantee coverage for all cameras, capture cards, or unsupported recording methods.

The app does not continuously capture the screen, microphone, or audio unless the user explicitly chooses to enable a feature. Memory is stored only when the user provides a memory command or a supported explicit preference.
