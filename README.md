# Insait Audio Player

Windows-only MP3 player on Avalonia.

## What is implemented

- MP3 playback via Windows MCI / `winmm.dll`
- playlist UI with add/open/clear/save actions
- playlist persistence in `SQLite`
- AES encryption of text fields in the database without asking the user for a password
- MP3 cover art loading from embedded ID3 metadata (APIC/PIC frames)
- orange fallback music icon when a file has no cover
- quick button to register the app for `.mp3` and open the Windows default-app chooser
- opening `.mp3` files passed through command-line/file association

## Storage

The playlist database is stored in:

`%LocalAppData%\\Insait Audio Player\\playlist.sqlite`

Text values are saved encrypted, so the database does not keep track names and file paths in plain text.

## Run

```powershell
Set-Location "C:\Users\Oleg\RiderProjects\Insait Audio Player"
dotnet build "Insait Audio Player.sln"
dotnet run --project ".\Insait Audio Player\Insait Audio Player.csproj"
```

## Notes

- The player now targets `net10.0-windows` because playback uses Windows APIs.
- Covers are shown when the MP3 file contains embedded artwork in standard ID3 metadata.
- Windows 10/11 does not allow silent force-changing the user's default app for `.mp3`, so the app registers itself and opens the system UI where the user confirms the choice.

