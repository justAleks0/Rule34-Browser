# Rule34 Gallery

**Current version:** 2.0.83 · [Releases](https://github.com/justAleks0/Rule34-Browser/releases)

A personal gallery browser for [rule34.xxx](https://rule34.xxx) (plus Danbooru and e621 sources), with a full **Windows** desktop app and a lighter **Android** companion. Sign in with Google to sync favorites, tag sets, credentials, and a trained **For You** feed across devices.

## What it is

Rule34 Gallery is not a website wrapper — it is a native client built around search, libraries, and recommendation. The PC app is the primary surface: rich filters, downloads, local folders, cloud sync control, and For You training. The Android app focuses on browse, save, sync, and optional LAN remote control of the desktop session.

| Client | Role |
|--------|------|
| **Windows (WPF)** | Full experience — browse, viewer, favorites/lists, local library, downloads, cloud sync hub, For You training, phone remote server |
| **Android (Kotlin)** | Lite companion — browse, For You feed, saved posts, cloud sync, QR remote to PC |
| **Shared core (.NET)** | API clients, settings, Firebase REST, downloads, cloud sync engine, For You logic |

Legacy **.NET MAUI** Android project remains in the solution but the native Kotlin app is the shipped Android client.

## Features

### Browse & search
- Tag search with autocomplete, presets, blacklist presets, and AI-assisted tag discovery (optional OpenAI key)
- Grid and feed layouts, rating/media/score filters, Danbooru and e621 account support
- Image and video viewer with favorites, lists, and passive For You learning from activity

### Library & storage
- **Cloud library** — favorites and custom lists via Firebase (Google sign-in)
- **Local library** — scan folders on disk, categories, thumbnails
- **Downloads** — queue manager with per-library targets (desktop)

### For You
- Learns from views, favorites, and watch-later signals; builds topics and scores
- Feed on desktop and Android; full topic tuning and training on Windows
- Optional cloud sync of the derived profile to Android

### Cloud sync
- Upload/download with **merge** or **replace**
- Per-item selection, cloud-only delete, device metadata, and activity log
- Syncs credentials, tag sets, favorites, lists, and For You profile data

### Cross-device
- **Phone remote (LAN)** — Android can drive search, paging, and the desktop viewer over Wi‑Fi (QR + short code)
- **Auto-update** — both apps check [GitHub Releases](https://github.com/justAleks0/Rule34-Browser/releases) and offer in-app download/install

## Configuration note

Firebase and API keys are **local only**. Copy `Rule34GalleryApp/firebase-config.example.json` to `firebase-config.json` for your own install — that file is gitignored and is not part of the public repo.

## Changelog

User-facing changes are recorded in [changelog.md](changelog.md) using the format in [changelog-template.md](changelog-template.md) (also bundled in both apps via the version badge).
