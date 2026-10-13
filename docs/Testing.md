# Testing and acceptance

Run commands from the repository root on Windows with .NET 10 SDK. Scripts work in Windows PowerShell 5.1 and PowerShell 7. Every .ps1 source file must contain English ASCII text only; Test.ps1 checks both this rule and script syntax.

## Unified entry points

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Test.ps1 -Suite Core
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Test.ps1 -Suite UI
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Test.ps1 -Suite Full
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Publish.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-PublishedApp.ps1
```

Core selects fast checks that need no external content. UI includes window checks; Full adds available real-content integration. Existing runners retain their assertions and may also run core checks when UI is selected. Native input and playback checks must run serially without other desktop interaction.

The summary reports Passed, Failed, and Skipped test groups. Existing runners print individual case counts. A failed native command or assertion makes the script exit nonzero. Skipped external-content checks are explicitly printed and are not counted as verified. An explicitly supplied missing/invalid data path is an error, not a skip.

## Published EXE acceptance

Test-PublishedApp.ps1 copies only the EXE into a temporary path with spaces, renames it, and launches it from another working directory. It checks the main window, configuration placement, clean closure, and the native missing-.NET installation instructions using an isolated runtime root. Installed runtimes and user configuration are untouched. MdictViewer also runs its existing nine application-specific published checks, covering settings, dictionary rendering, and missing WebView2.

Use -ExePath to verify an explicit binary. Tests do not publish automatically; an absent EXE is an error. Missing-runtime checks disable the graphical error popup and verify its text instructions rather than visually inspecting the popup. UI/audio checks do not replace listening and manual user acceptance.

## Workflow regression checks

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-Workflow.ps1
```

These checks replace external test processes and verify successful/failing exit handling and skip reporting. Test-Publish.ps1 separately exercises actual staged replacement against temporary files. Full application tests and actual EXE acceptance are required beyond these orchestration tests.

## Test data and ownership

External data paths are passed as arguments; scripts do not hard-code a developer's private directories. Source data is read and copied to unique temporary directories. Temporary files and environment overrides are cleaned up/restored; existing repository fixtures and original user files are not changed. Do not commit media, personal settings, or test output.

### ReciteWords

Core uses the existing service/storage runner. UI and Full add --ui-qa, including focus, navigation, rendering, and generated local WAV playback. No external word list is required; its tests own temporary word lists and progress files. WAV playback is muted and does not certify pronunciation quality.

## CI and results

The tag-triggered GitHub Release workflow runs Core, publishes the EXE, and then runs Test-PublishedApp.ps1 before uploading it. It cannot access private local recordings or dictionaries, so it does not certify Full real-content checks. Dated local results and remaining limitations are recorded in [Health Check](Health-Check-2026-10-13.md).