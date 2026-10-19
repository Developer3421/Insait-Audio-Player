# Privacy Policy — Insait Audio Player

**Effective date:** April 11, 2026
**Last updated:** April 11, 2026

---

## 1. Overview

Insait Audio Player is a lightweight, offline MP3 player for Windows 10 and Windows 11.
We are committed to protecting your privacy. This policy explains what data the application
stores, where it is stored, and how it is used.

**Short version:** Insait Audio Player does not collect, transmit, or share any personal
data. Everything stays on your device.

---

## 2. Data We Do NOT Collect

Insait Audio Player does **not**:

- Collect any personal information (name, email address, account credentials, etc.).
- Connect to the internet or any remote server.
- Send telemetry, analytics, crash reports, or usage statistics to any third party.
- Display advertisements or track your behaviour.
- Require account registration or sign-in of any kind.

---

## 3. Data Stored Locally on Your Device

All data created by the application is stored exclusively on your own device in the
following folder:

```
%LocalAppData%\Insait Audio Player\
```

### 3.1 Playlist Database (`playlist.sqlite`)

The application uses a SQLite database to remember your playlist between sessions.
It stores:

| Field | Description |
|---|---|
| File paths | Full local paths to the MP3 files you add to the playlist |
| Track title | Embedded ID3 tag or the file name |
| Artist | Embedded ID3 tag or a generic fallback label |
| Album / Folder | Embedded ID3 tag or the name of the parent folder |
| Track duration | Duration in ticks, derived from the audio file |
| Current track index | Which track was playing when you last closed the app |
| Playback position | The exact position (in milliseconds) within the current track |
| Volume level | Your last volume setting (0–100) |
| Playback speed | Your last speed setting (0.5×–2.0×) |
| Shuffle / Repeat | Whether shuffle and repeat modes were enabled |

**Encryption:** All text fields (file paths, titles, artist, album) are encrypted at rest
using AES before being written to the database. The encryption key is derived locally and
never leaves your device.

### 3.2 Application Settings (`app-settings.json`)

A small JSON file stores your UI preferences:

| Field | Description |
|---|---|
| Language code | The display language you selected (e.g., `en`, `uk`) |

### 3.3 User Agreement Record (`user-agreement.json`)

When you accept the End User Agreement on first launch, the following is stored locally:

| Field | Description |
|---|---|
| Accepted | Boolean flag indicating acceptance |
| Agreement version | The version of the agreement you accepted (e.g., `1.0`) |
| Accepted at (UTC) | Timestamp of when you accepted the agreement |

This record is used only to avoid showing the agreement dialog again on subsequent launches.
It is never transmitted anywhere.

---

## 4. File Access

The application reads MP3 files only when you explicitly open or add them through the
file picker, via command-line arguments, or through the Windows file association (double-click).
The application:

- Reads the audio stream and embedded ID3 metadata (title, artist, album, cover artwork) of
  the files you select.
- Does **not** scan, index, or access any files you have not explicitly chosen.
- Does **not** copy, upload, or modify your MP3 files in any way.

---

## 5. Windows Registry

When you click **"Set as Default MP3 Player"**, the application writes a single registry entry
to register itself as a handler for `.mp3` files. It then opens the Windows Default Apps
settings UI where you confirm the choice. No other registry changes are made.

---

## 6. Third-Party Libraries and Services

Insait Audio Player uses only open-source libraries (Avalonia UI, SQLite, etc.) that run
entirely on your local device. No third-party analytics SDKs, advertising networks, or
cloud services are integrated.

---

## 7. Children's Privacy

Insait Audio Player does not knowingly collect any information from anyone, including
children under the age of 13. Because no data is collected at all, the application is safe
for use by all ages.

---

## 8. Data Security

Because no data leaves your device, the risk of a remote data breach is zero. Your playlist
data is additionally protected by AES encryption on disk. Access to the data folder is
governed by your Windows user account permissions.

---

## 9. Your Rights and Data Deletion

All locally stored data can be deleted at any time by removing the application data folder:

```
%LocalAppData%\Insait Audio Player\
```

You may also clear the playlist directly within the app using the **Clear** button. Uninstalling
the application does not automatically remove the data folder; you may delete it manually
if desired.

---

## 10. Changes to This Policy

If this privacy policy is updated, the **"Last updated"** date at the top of this document
will change. We encourage you to review this policy periodically. Because no personal data
is collected, changes will generally only reflect updates to the application's local storage
behaviour.

---

## 11. Contact

If you have any questions about this privacy policy or the application's data practices,
please open an issue in the project repository or contact the developer directly through
the platform where you obtained the application.

---

*Insait Audio Player — no ads, no accounts, no data collection. Your music, your device, your privacy.*

