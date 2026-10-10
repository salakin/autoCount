---
name: publish-exe
description: Build the single-file self-contained AutoClicker.exe into dist/. Use when the user asks for the exe, a release build, or to rebuild after changes.
---

1. Check the app isn't running: `Get-Process AutoClicker -ErrorAction SilentlyContinue`. If it is, ask the user to close it (the exe file is locked otherwise). Don't kill it.
2. Run:
   ```
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
   ```
3. Confirm `dist/AutoClicker.exe` exists and report its size.
4. Remind the user: it asks for admin (UAC), SmartScreen may warn because it is unsigned, and the exe must not be committed (over GitHub's 100 MB limit). Offer to attach it to a GitHub Release with `gh release create` instead.
