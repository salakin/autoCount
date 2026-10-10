# Auto Clicker

Windows desktop app (C#, .NET 8, WinForms) that clicks at a saved screen position and types random paragraphs into a saved text field.

## Build and run

- Run from source: `dotnet run -c Release`
- Check it compiles: `dotnet build -c Release`
- Make the single-file exe: use the `/publish-exe` skill, or
  `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist`

There are no tests. Verify behaviour by running the app against a real window (Notepad is a good target).

## Files

- `Program.cs` - entry point.
- `MainForm.cs` - the whole UI (built in code, no designer file), global hotkeys, and the async run loop.
- `NativeMethods.cs` - Win32 P/Invoke: `RegisterHotKey`, `SetCursorPos`, `SendInput` for mouse clicks and Unicode typing.
- `ParagraphGenerator.cs` - random sentences from a fixed word list.
- `app.manifest` - requests administrator rights so input reaches elevated windows (UIPI).

## Hotkeys

| Action | Key | Alternate |
|---|---|---|
| Save click position | F6 | Ctrl+Alt+1 |
| Save text position | F7 | Ctrl+Alt+2 |
| Start / stop | F8 | Ctrl+Alt+3 |

Alternate hotkey ids are the main id + 10; `WndProc` maps them back with `% 10`. Keep that scheme when adding hotkeys, and unregister every id in `OnFormClosing`.

## Gotchas

- `dotnet publish` fails with "Access to the path ...\dist\AutoClicker.exe is denied" when the app is running. Ask the user to close it; don't kill the process.
- The exe needs admin (UAC prompt) because of `app.manifest`.
- Clicks use a 50 ms pause after moving and a 40 ms hold; many apps ignore zero-length clicks. Don't remove these delays.
- A position is only saved once the hotkey fires or the 3 s countdown ends. Laptop F-keys often need Fn, hence the Ctrl+Alt alternates.
- Some games read raw input and ignore `SendInput`; that is a Windows limit, not a bug here.
- `bin/`, `obj/` and `dist/` are git-ignored. The exe is ~154 MB, over GitHub's 100 MB file limit, so never commit it; ship it as a GitHub Release asset.

## Conventions

- Match the existing style: file-scoped namespaces, `_camelCase` private fields, short comments only where the reason isn't obvious.
- Keep all UI in code in `MainForm.cs`; don't add designer files.
- Run `dotnet build -c Release` with 0 warnings before committing.
- Remote: https://github.com/salakin/autoCount (branch `main`).
