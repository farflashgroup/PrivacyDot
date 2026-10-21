# Community launch copy

Prepared copy, not a claim of publication. Check each community's current rules before posting, disclose affiliation, and answer replies. Do not cross-post identical text in a burst or ask people to vote.

## Show HN

Title: **Show HN: PrivacyDot – a Windows tray indicator for microphone and camera activity**

URL: https://github.com/farflashgroup/PrivacyDot

First comment:

I built PrivacyDot to make detected microphone and camera activity easy to glance at in the Windows tray.

The icon is gray when no use is detected, green for microphone activity, orange for camera activity, and split when both are detected. Clicking it shows detected apps grouped by the device they use, which is useful with a headset, USB mic, and external webcam connected at the same time.

It reads local Windows activity metadata without recording audio/video. There is no telemetry; it does contact GitHub for update checks. Downloads of updates require confirmation.

The implementation is C# / WinForms on .NET Framework 4.8, under MIT. Microphone attribution uses Core Audio capture sessions; per-camera attribution uses the Windows sensor activity monitor on Windows 10 1703+. Older Windows versions have limited detection. It is best-effort visibility, not a permission blocker. The current installer is unsigned.

I'd especially appreciate reports about device attribution, unusual drivers, and useful capture activity that is missing from the popup.

## r/opensource

Title: **I built PrivacyDot: an MIT-licensed Windows tray indicator for mic and camera activity**

I'm the maintainer of PrivacyDot, a small Windows utility that shows detected microphone and camera activity in the notification area.

Click its colored dot to see detected apps grouped by device. It has dark/light themes, eight interface languages, and a per-user installer that does not need administrator privileges.

Source and installer: https://github.com/farflashgroup/PrivacyDot

The detection code reads Windows metadata without recording audio/video. No telemetry or device-usage uploads; GitHub is contacted for update checks. Detection is best-effort, older Windows versions have limited camera visibility, and the current installer is unsigned. It does not block permissions.

If you use several microphones or cameras, I'd love feedback on whether grouping by device makes the activity easier to understand. Bug reports and contributions are welcome.

## r/SideProject

Title: **A tiny Windows tray dot that shows detected mic and camera activity**

I built PrivacyDot around one small interaction: glance at the tray for activity, click to see the detected apps and devices.

Green means microphone activity, orange means camera activity, and split means both. With multiple devices connected, the popup groups apps under each microphone/camera rather than flattening them into a single list.

It's free and MIT-licensed, with eight languages and light/dark themes. No telemetry or usage uploads; update checks go to GitHub. Detection depends on Windows/driver reporting, and the installer is currently unsigned.

Screenshots, source, and download: https://github.com/farflashgroup/PrivacyDot

I'm looking for feedback on the experience and edge cases with real audio/video setups.

## Windows community / forum introduction

Subject: **PrivacyDot: free microphone and camera activity indicator for the Windows tray**

I'm sharing a utility I maintain called PrivacyDot. It keeps a small indicator in the Windows notification area and changes color when microphone or camera activity is detected. Click it to see detected apps grouped by the device they use.

It supports Windows light/dark themes and eight languages. The installer runs per user, without administrator privileges. Source code is MIT-licensed.

It reads local Windows metadata without recording audio/video, collects no telemetry, and contacts GitHub for update checks. Detection is best-effort and older Windows versions have limited camera detection. It is an indicator rather than a permission blocker; the current installer is unsigned.

Project and screenshots: https://github.com/farflashgroup/PrivacyDot

If project introductions are welcome here, I would appreciate feedback from people with USB microphones, headsets, and multiple webcams.

## X / Bluesky / Mastodon launch

I built PrivacyDot: a free, open-source Windows tray dot for detected mic/camera activity. Click to see apps grouped by device. No telemetry; GitHub update checks. MIT licensed.

https://github.com/farflashgroup/PrivacyDot

## LinkedIn personal post

I built PrivacyDot, a small open-source Windows utility for a simple question: which apps are using my microphone or camera?

Its tray dot changes color with detected activity. Clicking it shows detected apps grouped by microphone or camera, so multi-device setups are easier to read.

It's free, MIT-licensed, available in eight languages, and installs without administrator privileges. Device-usage data stays on the PC. There is no telemetry; the app contacts GitHub for update checks.

Detection depends on Windows and device drivers, so I'm interested in feedback from people with different headsets, microphones, and webcams. The current installer is unsigned.

Screenshots, source, and download: https://github.com/farflashgroup/PrivacyDot

## Follow-up: multiple devices

Headset mic + USB mic + external webcam?

PrivacyDot groups detected apps by the device they use, including a separate label when Windows cannot identify the device. A small detail that makes a busy capture setup easier to read.

Free Windows utility, MIT source: https://github.com/farflashgroup/PrivacyDot

## Follow-up: explain the dot

PrivacyDot's four states:

- Gray: no microphone or camera use detected
- Green: microphone activity detected
- Orange: camera activity detected
- Split: both detected

Click for detected apps and devices. Detection is best-effort; gray is not a guarantee that nothing can record.

https://github.com/farflashgroup/PrivacyDot

## Follow-up: privacy / technical detail

PrivacyDot observes Windows activity metadata without recording audio/video. Microphone attribution uses Core Audio sessions; camera attribution uses the Windows sensor activity monitor where available.

No telemetry or usage uploads. GitHub is contacted for update checks.

MIT source: https://github.com/farflashgroup/PrivacyDot

## Demo storyboard (20–30 seconds)

1. Show the tray at rest. Caption: “No microphone or camera activity detected.”
2. Start a deliberate test call or capture session. Show green, orange, or split state as actually observed.
3. Click the dot. Highlight detected app/device grouping.
4. Show dark/light mode or the language menu briefly.
5. End: “PrivacyDot · Free & open source · github.com/farflashgroup/PrivacyDot”.

Record a controlled demo with no personal notifications or private paths. Do not stage a simulated screenshot as live detection. Include “Detection depends on Windows and device drivers” in the video description.

## Article ready for a project blog or DEV Community

### Why I built a microphone and camera indicator for the Windows tray

The interesting moment is often after a call: you want a quick read on whether microphone or camera activity is still being reported. I wanted that information close to the clock, without having to keep a settings page open.

PrivacyDot is my small answer. It sits in the Windows notification area. Green represents detected microphone activity, orange represents detected camera activity, and a split dot represents both. Gray means no activity was detected.

Clicking the dot opens a more useful view: detected apps grouped by the device they use. A headset microphone and a USB microphone get separate headings. Multiple apps can appear under one device, and an app using several devices can appear under each. If Windows cannot identify the device, the interface says so instead of guessing the default.

The implementation reads activity metadata. It does not open a camera, capture frames, or record audio. Microphone attribution uses Core Audio capture sessions. On supported Windows versions, camera attribution uses the sensor activity monitor. Windows privacy usage records provide a fallback.

That distinction shapes the limits. PrivacyDot depends on Windows and drivers exposing activity; it cannot promise to identify every capture path. It is not antivirus or a permission firewall. Older Windows releases have less information available, especially for cameras.

The app is C# / WinForms on .NET Framework 4.8. It has light/dark themes and eight interface languages, and its installer runs per user without administrator privileges. It collects no telemetry and uploads no device usage. It contacts GitHub for update checks and only downloads an update after confirmation.

PrivacyDot is free under the MIT License. The source, screenshots, release notes, and installer are available at https://github.com/farflashgroup/PrivacyDot. The current installer is unsigned.

If you try it, the most useful feedback is specific: your Windows version, capture device and driver, the app using it, what PrivacyDot detected, and what you expected. Remove private paths and personal information before sharing diagnostics publicly.
