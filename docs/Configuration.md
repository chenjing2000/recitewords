# Configuration

The current configuration file is ReciteWords.settings.json beside the actual executable. It follows the EXE directory rather than the working directory. For AudioPlayer, ReciteWords, and ReadArticles, renaming the EXE changes the .settings.json stem; MdictViewer always uses dictionaries.json.

Invalid settings are reported and retained. Word-list progress is stored in userdata beside each word list; failed saves preserve the previous data.

## Ownership and updates

The application owns its configuration format. Publishing updates only the EXE and preserves configuration and other user files. Git and Release assets exclude personal settings and media. Do not delete dist to update a program. Use a writable application folder.

Acceptance tests launch a renamed EXE in a temporary directory, check configuration placement, and use an isolated runtime root to verify the missing-.NET instructions. Application-specific configuration recovery remains covered by the existing service/UI checks.

## Application-specific details

See [Module-Design.md](Module-Design.md) and [Testing](Testing.md).
## Detailed settings behavior

Startup creates the settings file if it is absent. It preserves the last folder, word list, window position and dimensions. A renamed EXE uses its own settings stem without falling back to another name. If settings cannot be read, the application reports the error and stops writing that configuration to protect its existing contents. An unwritable directory reports a save failure without redirecting settings elsewhere.
