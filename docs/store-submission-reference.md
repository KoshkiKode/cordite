# Store Submission Reference

Quick reference for submitting Cordite Wars to each major app store and platform.

---

## 🪟 Windows

### Steam

| Item | Details |
|------|---------|
| **Publisher Cost** | $100 per game |
| **Account Type** | Steamworks |
| **Binaries Required** | `.exe` (64-bit required) |
| **Build Version** | Auto-increments; update `version_info.json` |
| **Store Page Setup** | https://partner.steamgames.com/ → Dashboard → App Admin |
| **Metadata Fields** | Store description (3000 char), short desc, tags (up to 10), genre, screenshots (up to 8) |
| **Review Time** | Manual review: 2–5 business days |
| **Pricing** | Minimum $0.49 (USD), can set regional pricing |
| **DRM** | Steamworks DRM included (optional) |

**Timeline**: 1 week (account verification + build submission + review)

---

### Microsoft Store

| Item | Details |
|------|---------|
| **Publisher Cost** | $19 developer account |
| **Account Type** | Microsoft Partner Center |
| **Binaries Required** | `.msix` (signed, 64-bit) |
| **Signing Certificate** | Free via Partner Center or self-signed for testing |
| **Store Page Setup** | https://partner.microsoft.com/dashboard → App management |
| **Metadata** | Title (50 char), short desc (150 char), long desc (10,000 char), screenshots (5–9) |
| **Review Time** | 24–48 hours automated + manual review |
| **Content Rating** | IARC rating questionnaire (auto-rated after submission) |
| **Age Rating** | PEGI, ESRB, ClassInd (auto-assigned) |

**Timeline**: 1 week (account setup + build prep + review)

---

### Epic Games Store

| Item | Details |
|------|---------|
| **Publisher Cost** | Free (revenue split: 88/12 Epic/Dev) |
| **Account Type** | Epic Games publisher account |
| **Binaries Required** | `.exe` (64-bit) |
| **Store Page Setup** | https://publishing.unrealengine.com/ |
| **Metadata** | Title (100 char), description (250–500 char), genres, screenshots, hero image |
| **Review Time** | 1–2 weeks, feedback typically given |
| **Marketing** | Consider for featured placement if newsworthy |
| **Crossplay** | Integrated if game supports multiplayer |

**Timeline**: 2–3 weeks (account setup + submission + review feedback loop)

---

## 🐧 Linux

### Snap Store

| Item | Details |
|------|---------|
| **Publisher Cost** | Free |
| **Account Type** | snapcraft.io account |
| **Binaries Required** | `.snap` package |
| **Store Page Setup** | snapcraft.io → Dashboard → "New Snap" |
| **Metadata** | Title, summary (79 char), description, icon, screenshots (4–5) |
| **Channels** | `stable` (default), `candidate`, `beta`, `edge` |
| **Review Time** | ~1 day for stable, faster for other channels |
| **Auto-Update** | Users get updates automatically |
| **Confinement** | `strict` (sandboxed, most permissive) vs `devmode` (unrestricted, not recommended) |

**Timeline**: 2–3 days (account + build + store setup + review)

---

### Flathub

| Item | Details |
|------|---------|
| **Publisher Cost** | Free |
| **Account Type** | GitHub account |
| **Submission Method** | Pull Request to https://github.com/flathub/flathub |
| **Metadata** | Flatpak manifest (`.yml`), AppData XML, screenshots |
| **Review Time** | 1–2 weeks community review |
| **Sandbox** | Flatpak sandbox (permission-based) |
| **Auto-Update** | Central repository auto-updates |

**Timeline**: 1–2 weeks (PR creation + community feedback + merge)

---

### GOG

| Item | Details |
|------|---------|
| **Publisher Cost** | Free |
| **Account Type** | GOG Developer Program |
| **Binaries Required** | `.exe` with installer |
| **DRM** | DRM-free only (GOG policy) |
| **Store Page Setup** | GOG Galaxy dashboard |
| **Revenue** | 30/70 split (GOG/Dev), higher than Steam |
| **Review Time** | 3–5 business days |

**Timeline**: 2–3 weeks (account approval + review)

---

## 🤖 Android

### Google Play Store

| Item | Details |
|------|---------|
| **Publisher Cost** | $25 one-time |
| **Account Type** | Google Play Developer account |
| **Binaries Required** | `.apk` or `.aab` (Android App Bundle, recommended) |
| **Architecture** | ARM64 required (32-bit support deprecated) |
| **Signing** | Keystore signed with release key (CI handles this) |
| **API Level** | Min: 21 (Android 5.0), Target: 34+ (current) |
| **Metadata** | Title (50 char), short desc (80 char), full desc (4000 char), screenshots (2–8), icon, feature graphic |
| **Privacy Policy** | Link required if app collects data |
| **Content Rating** | IARC questionnaire (auto-rates PEGI, ESRB, etc.) |
| **Pricing** | Free or paid ($0.99–$200+ USD), regional pricing available |
| **Review Time** | Automated: 30 min, Manual: 24–48 hours |
| **Rollout** | Phase rollout recommended (5% → 10% → 50% → 100%) |

**Internal Testing Track**
- Upload APK/AAB to internal testing
- Share link with team for immediate testing (no review delay)

**Timeline**: 2–3 days (account + testing + metadata + review)

---

### Samsung Galaxy Store

| Item | Details |
|------|---------|
| **Publisher Cost** | Free |
| **Account Type** | Samsung Seller account |
| **Binaries Required** | `.apk` |
| **Metadata** | Similar to Google Play |
| **Review Time** | 24–48 hours |
| **Coverage** | Only Samsung devices (~15% of market, but premium audience) |

**Timeline**: 2–3 days (account + review)

---

### Amazon Appstore

| Item | Details |
|------|---------|
| **Publisher Cost** | Free |
| **Account Type** | Amazon Developer account |
| **Binaries Required** | `.apk` |
| **Review Time** | 24–48 hours |
| **Reach** | Fire tablets + select Android devices (~5% market) |

**Timeline**: 2–3 days (account + review)

---

## 🍎 macOS / iOS

### App Store Connect (macOS + iOS)

| Item | Details |
|------|---------|
| **Publisher Cost** | $99/year developer account |
| **Account Type** | Apple Developer Program |
| **Binaries Required** | `.app` (macOS), `.ipa` (iOS) |
| **Signing** | Apple Developer ID certificate required |
| **Notarization** | macOS only (prevents "unverified developer" warning) |
| **Metadata** | Name (30 char), subtitle (30 char), description (4000 char), keywords (100 char), screenshots (2–5), preview video |
| **Privacy Policy** | Required if app tracks user data |
| **Age Rating** | IARC questionnaire (auto-rates PEGI, ESRB, ClassInd) |
| **Pricing** | Free or tiered pricing ($0.99–$999.99 USD) |
| **Review Time** | 24–48 hours typically, can take 5+ days for rejections |
| **Rejection Reasons** | Common: metadata issues, bugs, privacy concerns, performance |
| **TestFlight** | Free beta testing via TestFlight (internal + external testers) |

**macOS Specific**
- Min OS: macOS 10.14+
- Codesign with "Developer ID Application" certificate
- Notarize build: ~5–10 min (done in CI)
- Can distribute outside App Store (web/direct download)

**iOS Specific**
- Min OS: iOS 13.0+
- Build via Xcode Cloud or manually
- Automatic app thinning optimizes for each device
- Bitcode required (auto-enabled)

**TestFlight Process**
1. Upload build to App Store Connect
2. Automated processing (~30 min)
3. Invite internal testers (team members)
4. Internal testing ~1 day
5. Invite external testers (up to 10,000)
6. External beta review ~24 hours
7. Collect feedback, iterate

**Timeline**: 1 week (account setup + build + TestFlight feedback + App Store review)

---

## 📋 Cross-Platform Checklist

### Before Any Submission

- [ ] App tested on minimum spec device for platform
- [ ] No crash on startup, app opens within 3 seconds
- [ ] Game is playable (reaches main menu at minimum)
- [ ] No sensitive data in logs or error messages
- [ ] Version bumped and consistent across platform
- [ ] Screenshots captured at native resolution (no upscaling)
- [ ] Minimum 3 screenshots per platform (preferably 5–8)
- [ ] Hero/feature image for each store (dimensions vary)
- [ ] App icon in correct format & size for platform
- [ ] Privacy policy linked (even if just "no data collected")
- [ ] Content rating completed (IARC/PEGI)

### Store Metadata Template

**Title**: "Cordite Wars: Six Fronts"

**Short Description** (80–150 char):
> "Strategy RTS—command units, capture territory, master the six fronts. Defeat rivals in single-player or multiplayer."

**Full Description** (300–500 char):
> "Cordite Wars: Six Fronts is a deep real-time strategy game where you command armies across six distinct battlefields. Manage resources, research technology, and outmaneuver opponents. Single-player campaign with 12 missions, skirmish mode with AI difficulty levels 1–10, local & online multiplayer for up to 4 players. Supports keyboard + mouse, gamepad. Cross-save between platforms."

**Key Features** (5–7 bullet points):
- Real-time strategy gameplay with deep mechanics
- Single-player campaign: 12 story missions
- Skirmish mode: 8 maps, 10 AI difficulty levels
- Multiplayer: local & online for 2–4 players
- Cross-platform: save on PC, continue on mobile
- Full gamepad support (mouse/keyboard also)
- Stunning visuals with dynamic lighting

**Tags/Categories** (platform-specific):
- Strategy
- Simulation
- Multiplayer
- Sci-Fi
- Real-Time

---

## ⏱️ Submission Timeline Summary

| Platform | Acct Setup | Build Prep | Review | Total |
|----------|-----------|----------|--------|-------|
| Steam | 3–5 days | 1 day | 2–5 days | 1 week |
| Microsoft Store | 1 day | 1 day | 1–2 days | 3–4 days |
| Epic | 2–3 days | 1 day | 1–2 weeks | 2–3 weeks |
| Google Play | 1 day | 1 day | 1–2 days | 3–4 days |
| Samsung Galaxy | 1 day | 1 day | 1–2 days | 3–4 days |
| Amazon | 1 day | 1 day | 1–2 days | 3–4 days |
| App Store (Mac/iOS) | 2–3 days | 2–3 days | 1–2 days | 1 week |
| Snap Store | 1 day | 1 day | ~1 day | 2–3 days |

**Fastest to Release**: Google Play / Galaxy Store (3–4 days)  
**Slowest to Release**: Epic / Full multi-platform launch (3 weeks)

---

## 🔗 Useful Links

- **Steam**: https://partner.steamgames.com/
- **Microsoft Store**: https://partner.microsoft.com/dashboard
- **Epic Games**: https://publishing.unrealengine.com/
- **Google Play**: https://play.google.com/console
- **Samsung Galaxy**: https://seller.samsungapps.com/
- **Amazon Appstore**: https://developer.amazon.com/apps-and-games
- **App Store Connect**: https://appstoreconnect.apple.com/
- **Snapcraft**: https://snapcraft.io/dashboard/
- **Flathub**: https://github.com/flathub
- **GOG**: https://www.gog.com/developer/
