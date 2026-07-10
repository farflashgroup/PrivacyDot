# PrivacyDot

PrivacyDot is a tiny Windows tray utility inspired by the iOS privacy dots. It shows a dot in the notification area:

- Gray when no microphone or camera use is detected
- Green when microphone use is detected
- Orange when camera use is detected
- Split green/orange when both are detected

Left-click the dot to see the apps currently detected under Microphone and Camera. Right-click for refresh, Windows privacy settings, Start with Windows, and Exit.

## Compatibility

PrivacyDot targets .NET Framework 4.8 so it can run on Windows 7 SP1 and newer. Microsoft no longer supports modern .NET on Windows 7, but .NET Framework 4.8 and the Core Audio APIs used for microphone session detection are compatible with Windows 7 SP1.

Camera app detection uses the Windows privacy usage registry when it exists, which is available on newer Windows versions. On Windows 7, the app still runs and microphone detection uses Core Audio, but camera app attribution is best-effort because Windows 7 does not expose the Windows 10+ privacy usage records.

## Build

```powershell
dotnet build .\PrivacyDot.Tests\PrivacyDot.Tests.csproj -c Release
.\PrivacyDot.Tests\bin\Release\net48\PrivacyDot.Tests.exe
```

The build uses `Microsoft.NETFramework.ReferenceAssemblies.net48` so the .NET Framework developer pack does not have to be installed globally.

## Install

Use `PrivacyDotSetup.exe` from a release or build the installer with the scripts in `installer/`. The installer:

- Checks for .NET Framework 4.8
- Installs per-user to `%LOCALAPPDATA%\PrivacyDot`
- Adds Start Menu and Startup shortcuts
- Launches PrivacyDot

No administrator privileges are required.
