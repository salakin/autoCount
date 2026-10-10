# Auto Clicker

A Windows desktop utility that clicks at a saved screen position and types randomized text into a saved field, driven by global hotkeys for hands-free automation.

Built with C# / .NET 8 / WinForms.

## Features

- Click a saved screen position on a timer, with optional left/right button selection.
- Type randomly generated paragraphs into a saved text field on a separate timer.
- Global hotkeys work from any window, even while the app isn't focused:

  | Action | Key | Alternate |
  |---|---|---|
  | Save click position | F6 | Ctrl+Alt+1 |
  | Save text position | F7 | Ctrl+Alt+2 |
  | Start / stop | F8 | Ctrl+Alt+3 |

- Configurable click interval, type interval, sentences per paragraph, per-character typing delay, optional Enter key after typing, and a repeat count (0 = run forever).
- Live preview of the last paragraph typed.

## Usage

1. Run `AutoClicker.exe` (requires administrator rights so input reaches elevated windows).
2. Hover over the target spot and press **F6** (or Ctrl+Alt+1) to save the click position, or use the **Set** button and wait out the 3-second countdown.
3. Hover over the text field and press **F7** (or Ctrl+Alt+2) to save the typing position.
4. Enable "Click at position" and/or "Type random paragraph at position", adjust the intervals and other options as needed.
5. Press **F8** (or Ctrl+Alt+3) to start. Press it again to stop.

Laptop keyboards often need Fn for F-keys — use the Ctrl+Alt alternates if F6/F7/F8 don't register.

## Download

Grab the latest `AutoClicker.exe` from the [Releases](https://github.com/salakin/autoCount/releases) page — no install required.

## Building from source

```
dotnet build -c Release
```

Run from source:

```
dotnet run -c Release
```

Publish a single-file exe:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

## Notes

- Some games and anti-cheat software read raw input and ignore synthetic input (`SendInput`); that's a Windows limitation, not a bug in this app.
- There are no automated tests — verify behavior by running against a real window (Notepad is a good target).

## License

No license specified.
