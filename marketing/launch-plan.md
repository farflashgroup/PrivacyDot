# 30-day launch plan

Start: October 21, 2026. This is an execution plan, not a scheduled automation. No paid placements, accounts, or subscriptions are authorized by this document.

## Days 1–3: build discoverability

- Publish the product website, clear download/install information, privacy explanation, FAQ, and downloadable resources.
- Update GitHub description, website field, and relevant topics. Keep the README's explanation and installation path clear.
- Submit one factual entry to each relevant, maintained list that accepts the contribution. Record receipts; do not claim acceptance while review is pending.
- Complete free directory submissions where an existing authorized account and suitable category are available. Use privacydot@farflash.com as the public product contact.
- Prepare account-gated entries using the directory copy, then resume when the user provides an existing login or completes account creation.

## Days 4–7: explain the mechanism

- Share one creator-disclosed launch post in a community whose rules allow it. Tailor the post, answer replies, and avoid vote requests.
- Capture a real 20–30 second demo using the storyboard.
- Publish the technical article on the project owner's chosen blog/profile.
- Address installation friction or misleading copy before broadening reach.

## Week 2: show specific use cases

- Post a multiple-device example and the four-state explanation on the owner's product/personal profiles.
- Invite reproducible feedback about microphone/camera attribution and drivers.
- Update pending listings when editors request factual corrections. Do not submit duplicates while review is pending.
- Review whether app signing, a portable release, and a package-manager installer would materially reduce installation friction. These are engineering/distribution follow-ups, not existing capabilities.

## Weeks 3–4: follow the evidence

- Publish one useful follow-up about a real fix or a real contribution, with release notes.
- Revisit directories that had a concrete access barrier. Stop spending time on unsuitable SaaS-only, Linux-only, or archived lists.
- Ask for honest feedback after use, not star/vote exchanges or manufactured reviews.
- Maintain the listings' version, compatibility, images, and official links.

## Measurement without adding app telemetry

Capture these aggregate indicators weekly with dates and source links:

| Indicator | Source | Interpretation |
| --- | --- | --- |
| Installer download count per release asset | GitHub Releases API | Download events, not unique users or active installs |
| Stars/forks and substantive issues | GitHub repository | Interest / contribution signals, not usage |
| Submission → reviewed → accepted | Submission tracker | Separates outreach activity from actual distribution |
| Referral traffic where hosting exposes aggregate logs | Hosting platform | Only if available; do not invent unavailable analytics |
| Repeated installation/detection feedback | Public issues and voluntary feedback | Qualitative friction, not a representative sample |

Initial public snapshot: v0.3.1 installer download count 0; repository stars 0 and forks 0, observed October 21, 2026 before this launch work. Later downloads by maintainers/tests may inflate the count. There are no defensible conversion or reach forecasts yet.

Useful read-only checks:

```powershell
gh api repos/farflashgroup/PrivacyDot/releases --jq '.[] | {tag: .tag_name, assets: [.assets[] | {name, download_count}]}'
gh repo view farflashgroup/PrivacyDot --json stargazerCount,forkCount
```

## Operating rules

Use each site's real submission route and respect its community rules. No broad unsolicited messaging, automated comment spam, fabricated testimonials, implied endorsements, or payment. No editor pitches requested. A site's account creation, CAPTCHA, or financial step stays with the user where required. Record the precise barrier and the next action, then continue independent work.
