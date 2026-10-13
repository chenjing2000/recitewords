# Application overview

ReciteWords is a local Windows application built with C#, .NET 10, and WPF. It reads user-owned word lists and local pronunciation files, stores settings beside the executable, and saves each word list's study status in its adjacent userdata directory.

Storage validates files, Review maintains the fixed study queue and rollback snapshots, and Audio finds and plays local files. MainViewModel coordinates those services and exposes bindings; Views handle rendering, keyboard input and window events. App constructs the services directly. The six small service interfaces support module boundaries and regression tests.

The unified tooling wraps the existing test runners and portable publishing configuration. It does not replace the application algorithms or file formats. Test data is isolated from user data and UI/native-input checks run sequentially.

See [Module-Design.md](Module-Design.md) for the existing design, [Configuration](Configuration.md) for settings, [Testing](Testing.md) for verification, and [Publishing](Publishing.md) for distribution.

The [current code review](Code-Review-2026-10-13.md) records the relative-path persistence fix, progress parsing simplification, verification evidence and retained complexity.
