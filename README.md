<p align="center">
  <img src="assets/privacydot-logo.png" alt="PrivacyDot logo" width="180">
</p>

<h1 align="center">PrivacyDot</h1>

<p align="center">
  A tiny Windows tray privacy indicator for microphone and camera activity.
</p>

<p align="center">
  <a href="https://github.com/farflashgroup/PrivacyDot/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/farflashgroup/PrivacyDot?sort=semver"></a>
  <a href="https://github.com/farflashgroup/PrivacyDot/actions"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/farflashgroup/PrivacyDot/build.yml?branch=main"></a>
  <a href="LICENSE"><img alt="License" src="https://img.shields.io/badge/license-MIT-green"></a>
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows%207%20SP1%2B-0078d4">
  <img alt=".NET Framework" src="https://img.shields.io/badge/.NET%20Framework-4.8-512bd4">
</p>

PrivacyDot sits in the Windows notification area and gives you a quick visual read on device use:

- Gray: no microphone or camera use detected
- Green: microphone use detected
- Orange: camera use detected
- Split green/orange: both detected

Left-click the dot to see the apps currently detected under **Microphone** and **Camera**. Right-click for refresh, Windows privacy settings, Settings, and Exit. Settings contains Start with Windows, Language, and update controls.

Apps are grouped under the device they use, such as **Headset Microphone**, **USB Microphone**, **Integrated Camera**, or **Logitech Brio**. Multiple apps can appear under one device, and an app using several devices appears under each one. Devices with identical names have separate numbered headings; hover over a heading to see its device ID. Activity that Windows cannot associate with a device appears under **Device not identified**, rather than being assigned to the default device.

Each app row has monochrome device and force-quit controls on the right. The crossed-out microphone or camera is unavailable: hover, focus, or click it for an explanation. PrivacyDot does not currently offer temporary per-app permission revocation; use the app's own controls or Windows privacy settings.

Click **×** to replace the controls with a warning, then click the warning again to force quit. The warning shows the number of matching processes and cautions about unsaved work. It expires after ten seconds; Esc, closing the popup, or refreshing the list cancels it. Only processes with the exact executable path or package identity, owned by you in your current Windows session, can be targeted. All matching processes captured at confirmation time may close, including multiple windows of the same app; newly started processes are not included. Protected or unidentifiable processes cannot be closed.

Force quitting closes the app's matching processes across all devices; it does not disconnect only the device whose row you clicked.

PrivacyDot follows your Windows app theme when showing its popup and tray menu, with dark mode support on Windows versions that expose the app-theme setting.

## Languages

PrivacyDot follows the Windows display language by default. You can choose a different language from the tray menu under **Language**; the selection is saved for future launches.

The app includes English, French, Spanish, Japanese, Mandarin Chinese (Simplified), Hindi, Modern Standard Arabic, and Russian. Unsupported system languages fall back to English, and Arabic uses a right-to-left layout.

## Updates

PrivacyDot quietly checks the latest stable GitHub release after startup. When a newer version is available, a small localized note appears at the bottom of Settings. Choose **Settings → Check for updates** to review the update and decide whether to download and launch `PrivacyDotSetup.exe`.

Update downloads require TLS 1.2 with certificate-revocation checking, are restricted to the exact release tag and installer path in this repository, and have strict metadata, download-size, and timeout limits. The installer’s size and SHA-256 digest must match GitHub’s release metadata, and Windows Internet-zone provenance is preserved for SmartScreen. PrivacyDot never downloads an update in the background.

## Screenshots

Illustrative app and device data.

| Dark mode | Light mode |
| --- | --- |
| <img src="docs/screenshots/popup-dark.png" alt="PrivacyDot popup in dark mode" width="390"> | <img src="docs/screenshots/popup-light.png" alt="PrivacyDot popup in light mode" width="390"> |

## Install

Download the latest `PrivacyDotSetup.exe` from the [releases page](https://github.com/farflashgroup/PrivacyDot/releases/latest).

The installer:

- Installs per-user to `%LOCALAPPDATA%\PrivacyDot`
- Creates Start Menu and Startup shortcuts
- Checks for Microsoft .NET Framework 4.8
- Launches PrivacyDot after installation

No administrator privileges are required.

## Compatibility

PrivacyDot targets .NET Framework 4.8 so it can run on Windows 7 SP1 and newer. Modern .NET no longer supports Windows 7, but .NET Framework 4.8 and the Core Audio APIs used for microphone session detection are compatible with Windows 7 SP1.

Microphone attribution reads each active Core Audio capture endpoint's ID, friendly name, and active sessions. Camera attribution uses Windows' [sensor activity monitor](https://learn.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imfsensoractivitymonitor), available from Windows 10 version 1703, to obtain camera names, device IDs, and streaming processes. This observes usage metadata without opening a camera or recording audio/video.

Windows privacy usage records remain a fallback for apps whose devices cannot be identified, including capture paths not reported by the camera activity monitor. Detection is best-effort and depends on Windows and the driver exposing activity. On older Windows releases or systems without Media Foundation, the app keeps working with the available sources; per-camera attribution is unavailable. Windows 7 also lacks the newer privacy usage records, so camera app detection there is limited.

## Build

```powershell
dotnet build .\PrivacyDot.slnx -c Release
.\PrivacyDot.Tests\bin\Release\net48\PrivacyDot.Tests.exe
```

The build uses `Microsoft.NETFramework.ReferenceAssemblies.net48` so the .NET Framework developer pack does not have to be installed globally.

## Build The Installer

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\installer\BuildInstaller.ps1
```

The installer builder uses the Windows IExpress tool included with Windows and writes `PrivacyDotSetup.exe` to `artifacts\` by default.

## Privacy

PrivacyDot does not collect telemetry or send device usage anywhere. It reads local Windows APIs and registry usage records on the current machine only. It contacts GitHub once after startup and when you choose **Check for updates**, and downloads an installer only after you confirm an available update.

## Security

PrivacyDot runs without administrator privileges, bounds registry traversal and all update responses, and uses least-privilege, commit-pinned GitHub Actions. GitHub secret scanning and push protection are enabled for the repository.

## License

PrivacyDot is open source under the [MIT License](LICENSE).
