# Directory submission copy

All descriptions are original. Facts checked against the repository and published v0.3.1 release on 2026-10-21. Refresh release-specific fields before later use.

## Common form fields

| Field | Value |
| --- | --- |
| Name | PrivacyDot |
| Developer / publisher | FarFlash Group Ltd. (farflashgroup on GitHub) |
| Contact | privacydot@farflash.com |
| Website | https://privacydot.farflash.com/ |
| Source | https://github.com/farflashgroup/PrivacyDot |
| Download page | https://github.com/farflashgroup/PrivacyDot/releases/latest |
| Direct installer | https://github.com/farflashgroup/PrivacyDot/releases/latest/download/PrivacyDotSetup.exe |
| License / price | MIT; free; open source |
| Category | Security & Privacy → Privacy / System Utilities |
| Platform | Windows desktop |
| Runtime | .NET Framework 4.8 |
| Version | 0.3.1 |
| Release date | October 21, 2026 |
| Installer size | 246,272 bytes (about 241 KiB) |
| Installer type | EXE; per-user install; no administrator privileges required |
| Languages | English, French, Spanish, Japanese, Simplified Chinese, Hindi, Arabic, Russian |
| Tags | privacy, microphone, camera, webcam, windows, system-tray, open-source, device-activity |
| Icon | https://raw.githubusercontent.com/farflashgroup/PrivacyDot/main/assets/privacydot-logo.png |
| Dark screenshot | https://raw.githubusercontent.com/farflashgroup/PrivacyDot/main/docs/screenshots/popup-dark.png |
| Light screenshot | https://raw.githubusercontent.com/farflashgroup/PrivacyDot/main/docs/screenshots/popup-light.png |

Do not claim a native macOS/Linux/Android edition. Do not select a portable, Microsoft Store, signed, certified, or enterprise-managed distribution option.

## Tagline

See which apps are using your microphone and camera.

## Very short

Free Windows tray indicator for detected microphone and camera activity.

## Short

PrivacyDot shows detected microphone and camera activity in the Windows tray. Click the dot to see apps grouped by device. Free, MIT-licensed, and telemetry-free.

## Standard

PrivacyDot is a free, open-source Windows tray utility for microphone and camera activity. The dot turns green for detected microphone use, orange for camera use, and split green/orange when both are detected. Click it to see detected apps grouped by device. It supports dark and light themes, eight languages, and per-user installation without administrator privileges. Device activity stays on your PC; GitHub is contacted for update checks. Detection is best-effort and varies with Windows and drivers.

## Long

PrivacyDot gives microphone and camera activity a small, persistent home in the Windows notification area. Its tray icon has four states: gray when no activity is detected, green for microphone use, orange for camera use, and split green/orange for both.

Click the icon to see which apps are currently detected under Microphone and Camera. Entries are grouped by device, so a headset microphone, USB microphone, integrated camera, and external webcam can each have their own heading. Activity without a known device is labeled rather than assigned to the default device.

PrivacyDot reads Windows activity metadata without opening a camera or recording audio/video. It collects no telemetry and uploads no device-usage data. It contacts GitHub for startup and manual update checks; downloading an available update requires the user's confirmation.

The interface follows Windows light or dark mode and includes English, French, Spanish, Japanese, Simplified Chinese, Hindi, Arabic, and Russian. The installer runs per user without administrator privileges. Source code is available under the MIT License.

Detection depends on information exposed by Windows and device drivers. The application targets Windows 7 SP1 and newer with .NET Framework 4.8; per-camera attribution requires Windows 10 version 1703 or newer, and older systems have limited detection. PrivacyDot is an activity indicator, not a permission blocker or a guarantee against hidden recording. The current v0.3.1 installer is unsigned.

## AlternativeTo

Choose Free + Open Source + MIT; platform Windows. Use the standard or long description above **without URLs in its body**. Use official-site/source fields for links and clean URLs without tracking parameters. Upload the logo and both screenshots with the caption: “Illustrative app and device data.” Select only features that actually match the app. Do not add the project as an alternative to unrelated privacy suites, network firewalls, or camera blockers.

## LibHunt

Find a project whose primary function is displaying microphone/camera activity, then use its “Suggest alternative” route with the source repository URL. Explain the Windows platform difference if the existing project is macOS-only. Do not imply identical functionality or submit against an unrelated project simply to obtain a listing.

## SaaSHub

Submitted on October 21, 2026 through the free queue. Website ownership is verified and the listing is linked to the owner's account. The logo, two captioned screenshots, and the publisher Q&A below are saved. The listing is still pending moderation approval.

- Tagline: Free Windows tray indicator for detected microphone and camera activity, with apps grouped by device.
- Categories: Privacy; Security & Privacy; Utilities.
- Platform: Windows desktop. Free option: Yes. Paid option: No. Free trial: No.
- Related alternatives: OverSight and Micro Snitch. Both are macOS products with a related monitoring purpose; PrivacyDot is for Windows. Do not imply they run on the same platform or have identical functionality.
- Source: https://github.com/farflashgroup/PrivacyDot

Publisher Q&A posted on October 21, 2026:

**Which are the primary technologies used for building PrivacyDot?**

Publisher answer: PrivacyDot is built with C# and Windows Forms on .NET Framework 4.8. It reads Core Audio capture sessions for microphone attribution and the Windows sensor activity monitor for camera attribution on supported Windows versions, with Windows privacy usage records as a fallback. Detection observes metadata without recording audio/video. The MIT-licensed source is public on GitHub.

**How would you describe the primary audience of PrivacyDot?**

Publisher answer: PrivacyDot is intended for Windows users who want a persistent, glanceable view of detected microphone and camera activity. Device grouping can help people using a headset, USB microphone, and webcam together. It is also intended for developers who want to inspect or contribute to a small open-source Windows utility. These are intended use cases, not measured customer demographics or adoption claims.

## Awesome-list entry

```markdown
- [PrivacyDot](https://github.com/farflashgroup/PrivacyDot) - Windows tray indicator for detected microphone and camera activity, with apps grouped by device.
```

Proposed PR body:

> Adds PrivacyDot, a free MIT-licensed Windows tray utility, to the privacy section. Its icon reflects detected microphone/camera activity and its popup groups detected apps by device. Source, installer, screenshots, compatibility limits, and privacy behavior are documented in the linked repository. Detection is best-effort; this is not a permission-blocking tool. I maintain the submitted project. This contribution was prepared with AI assistance.

## SourceForge / FossHub / Softpedia / Uptodown

Use the common fields and long description. Preserve the official GitHub source and download links. Hosting integrations, developer accounts, verification, and redistribution terms must be reviewed in their actual submission flows. No mirror or developer account has been created merely because this copy exists.

## Screenshot captions / image alt text

- PrivacyDot dark mode popup showing illustrative detected microphone and camera apps grouped by device.
- PrivacyDot light mode popup showing illustrative detected microphone and camera apps grouped by device.
- PrivacyDot icon: a gray ring around a green and orange split dot.
