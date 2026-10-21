# PrivacyDot website hosting

Public address: **https://privacydot.farflash.com/**

The static website is published from `main:/docs` in `farflashgroup/PrivacyDot` using GitHub Pages. The requested public hostname is configured in GitHub Pages and persisted in `docs/CNAME`.

At the authoritative DNS provider, GoDaddy, the product subdomain has this record:

| Type | Name | Value |
| --- | --- | --- |
| CNAME | privacydot | farflashgroup.github.io |

Do not include `/PrivacyDot` in the DNS target. The site uses relative asset links so the custom hostname serves the product from `/`. Main-domain website and email DNS records are separate.

Canonical URLs, Open Graph URLs, structured data, sitemap, README, directory copy, product facts, and the downloadable listing kit use the custom hostname. When changing hosting, update these together. The open Awesome Free Apps PR also uses the custom hostname; LibHunt's homepage change was submitted for moderation.

At 02:34 EDT on October 21, 2026, GitHub approved the certificate and HTTPS enforcement was enabled. Normal certificate validation returned HTTP 200 for the custom hostname. Both the old GitHub Pages address and the HTTP custom-domain address returned permanent redirects to `https://privacydot.farflash.com/`.

All four HTML pages, the stylesheet/script, sitemap, robots file, screenshots, social graphic, and downloadable listing ZIP were verified over HTTPS. Desktop and mobile browser checks found no horizontal overflow; the landing page controls and guide images loaded correctly. Both the application Build and Pages deployment workflows passed for commit `cb0b6ce`.

## Read-only deployment checks

```powershell
gh api repos/farflashgroup/PrivacyDot/pages
Resolve-DnsName privacydot.farflash.com -Type CNAME
curl.exe -I https://privacydot.farflash.com/
```

DNS being saved does not establish that HTTPS is ready. Verify the custom hostname with normal certificate validation and enable GitHub Pages HTTPS enforcement after the certificate is issued. Never bypass a certificate warning to declare a deployment successful.

App installers remain on the official GitHub Releases page. The marketing website does not run an updater or request device access.
