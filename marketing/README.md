# PrivacyDot launch kit

Public contact: **privacydot@farflash.com**. Prepared 2026-10-21 for the published v0.3.1 release. No editor pitches or outreach emails are included.

## Start here

- [Submission tracker](submission-tracker.csv): actual outcomes, requirements, and next actions. A prepared draft is not a submitted or approved listing.
- [Directory copy](directory-copy.md): consistent facts plus paste-ready descriptions for different forms.
- [Community launch copy](community-posts.md): original launch posts, technical explanation, and follow-up posts.
- [30-day launch plan](launch-plan.md): sequencing, measurement, and maintenance.
- [Localized blurbs](localized-copy.md): descriptions in all eight interface languages.
- [Website hosting](hosting.md): custom hostname, DNS record, and deployment checks.
- [Public activity guide](../docs/windows-microphone-camera-indicator.html): installation, tray colors, device grouping, and detection limits.
- [Product facts](product-facts.json): structured source for future listings.
- [Public website](../docs/index.html) and [press/resources page](../docs/press.html).

## Positioning

**See which apps are using your microphone and camera.**

PrivacyDot is a free, MIT-licensed Windows tray utility. Its color changes with detected microphone and camera activity; clicking it reveals detected apps grouped by device. Lead with visibility and everyday usefulness. Follow with source availability, local activity detection, no telemetry, eight languages, and installation without administrator privileges.

Useful audiences: Windows users who move between calls, streamers with multiple capture devices, people who want a persistent activity indicator, and developers who want to inspect or contribute to a small Windows utility. These audiences are positioning hypotheses, not measured customer segments.

## Claim guardrails

- Say **detected activity**, not guaranteed surveillance detection or protection from hackers.
- Gray means **no use detected**, not proof that every device is inactive.
- This is an indicator, not antivirus, a firewall, or a permission blocker.
- Activity detection reads local Windows metadata without recording audio/video. Do not describe it as completely offline: it contacts GitHub for startup and manual update checks.
- The app targets Windows 7 SP1 and newer with .NET Framework 4.8. Per-camera attribution needs Windows 10 1703+; older systems have limited detection. Do not imply equivalent capabilities across all Windows versions.
- Temporary per-app permission revocation is unavailable. Force quit closes matching processes and can lose unsaved work; it is not a device-only disconnect.
- v0.3.1 is unsigned. Do not claim Microsoft Store verification, independent security audit, guaranteed performance numbers, users, reviews, rankings, or awards.
- Screenshots contain illustrative app/device data and must retain that context.

## Publishing conventions

Use the exact product name **PrivacyDot** to distinguish the Windows app from similarly named Android projects. Disclose creator affiliation in community posts. Use the directory's product submission route, respect its rules, and avoid duplicate submissions, unsolicited comments, vote requests, paid upgrades, and mailing-list opt-ins. Never mark a platform as completed without a receipt or visible result.

## Updating this kit

On a new release, check version/date, installer size and SHA-256, compatibility, screenshots, and feature descriptions against the release and source. Keep the stable download link pointed at GitHub Releases; do not upload a different installer to a directory without verifying its origin and bytes. Record listing URLs and submission IDs in the tracker.
