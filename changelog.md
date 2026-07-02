# Changelog

All notable changes to this project will be documented here.

---

## 2.0.83 - 12-00 07-02-2026

### Changed
- **Cloud sync tree (PC):** For You learning rows now read `Learning data: Reopened post — ahe_gao` instead of raw timestamp/kind keys; section renamed **Learning data**.

---

## 2.0.82 - 12-00 07-02-2026

### Fixed
- **Cloud sync tree (PC):** Expanding **Learning log** (For You activity history) no longer crashes when scrolling — mouse wheel over count labels hit inline `Run` text and broke `PageScrollHelper`.

### Changed
- **Cloud sync tree (PC):** Activity rows show human-readable labels instead of raw `timestamp:Kind:tag` keys; capped at 100 visible events with overflow row.

---

## 2.0.81 - 12-00 07-02-2026

### Fixed
- **Startup crash (PC):** Update banner used missing WPF theme resources (`AccentButton`, card brushes), which crashed the app on launch before the main window appeared.

---

## 2.0.80 - 12-00 07-01-2026

### Added
- **Auto-update (PC + Android):** Update checker targets the live repo [justAleks0/Rule34-Browser](https://github.com/justAleks0/Rule34-Browser) on GitHub Releases.

### Notes
- **GitHub publish:** First public release ships `Rule34Gallery-win-x64.zip` and `R34Browser.apk` for in-app download/install.

---

<!-- Legacy format below: older entries use a single bullet list per version. -->

## 2.0.79 - 2026-07-01

- **Auto-update (PC + Android):** Checks GitHub Releases on startup for newer builds; banner offers download and install. Desktop downloads the zip and restarts via `apply-windows-update.ps1` (preserves `firebase-config.json`). Android downloads the APK and opens the system installer.
- **Settings:** **Check for updates on startup** toggle and **Check for updates now** on both platforms.
- **Publish:** `scripts/publish-github-release.ps1` uploads `Rule34Gallery-win-x64.zip` and `R34Browser.apk` to a GitHub release (requires `gh` CLI).

## 2.0.78 - 2026-07-01

- **Cloud sync stability (PC):** Large favorites/lists no longer render thousands of tree rows (caps at 200 visible posts with “… and N more”). Fixed checkbox cascade storms and removed expand binding that could freeze or crash WPF while browsing the explorer.

## 2.0.77 - 2026-07-01

- **Cloud sync tree (PC):** Checking or unchecking items no longer collapses the data explorer — expanded branches stay open while you select items.

## 2.0.76 - 2026-07-01

- **Cloud sync control center (PC):** Status banner with last sync/device, local vs cloud data explorer tree with checkboxes, Upload/Download with merge or replace, bulk selection helpers, cloud delete for favorites/presets, and activity log.
- **Cloud sync control center (Android):** Category preview with counts, View items bottom sheet, Upload/Download with merge or replace, last-sync metadata, and refreshed activity log.
- **Sync engine (shared):** Snapshot/diff/merge pipeline, Firestore `syncMeta` + per-device docs, Danbooru/e621 credential parity in cloud, and For You `removedTopics` parity on Android.

## 2.0.75 - 2026-07-01

- **Video playback fix (PC):** Stopped trying to play incomplete cache files (`.part`), which broke all videos. Streams from the URL immediately; uses the disk cache only when a full file is ready. Bad cache entries are dropped and playback retries from the network.

## 2.0.74 - 2026-07-01

- **Video cache setting easier to find (PC + Android):** New **Video playback** section in Settings with **Keep video cache between sessions** (default off = cleared on exit).

## 2.0.72 - 2026-07-01

- **Video cache privacy (PC + Android):** Settings toggle **Clear video cache on exit** (default **on**) removes buffered videos/GIFs when you close the app. Turn off to keep the disk cache for faster replays (up to 3 GB on PC, 512 MB on Android).

## 2.0.71 - 2026-07-01

- **Faster video playback (PC):** Remote videos/GIFs buffer to a local disk cache before opening in the viewer and feed player, so WPF MediaElement starts from a file instead of slow HTTPS streaming. Adjacent posts are pre-cached while you browse.
- **Faster video playback (Android):** ExoPlayer now uses a disk cache (512 MB LRU) with fast-start buffering for viewer and feed video.

## 2.0.70 - 2026-06-27

- **Android Account credentials:** Cloud sync no longer overwrites fields while you edit on the Account page. Parsing a blob or clearing credentials sticks after Save — phone credentials win over cloud once saved locally.
- **Credential clear/upload:** Saving empty Rule34 fields clears them on the phone and pushes that to cloud instead of restoring the old pair.
- **Desktop:** Cloud credential upload uses the same local Rule34 pair (including cleared) when syncing to Firestore.

## 2.0.69 - 2026-06-27

- **Android Rule34 API fix:** Cloud sync no longer mixes User ID from one device with an API key from another (which caused “Missing authentication” even when fields looked filled in). Credentials auto-save on the Account tab; manual Save verifies the pair against Rule34.
- **Desktop cloud sync:** Same atomic User ID + API key merge when syncing credentials across devices.

## 2.0.68 - 2026-06-07

- **For You viewing no longer retrains the feed:** Opening posts from the For You feed no longer records passive view signals (fixes feedback loops and feed thrashing). Browse/library viewing still learns. Favorites, downloads, and tag clicks still count.
- **For You page stops auto-rebuilding on every signal:** Learning updates refresh topics/timeline only; use Refresh to rebuild the feed.

## 2.0.67 - 2026-06-07

- **For You refresh fix:** Feed gathering no longer applies Browse media filters (which could drop every result on refresh). Franchise series tags (`marvel`, `baldurs_gate_3`, etc.) are recognized again. Sort & filter media choice falls back to All when it would empty the pool.

## 2.0.66 - 2026-06-07

- **Series-only fix:** Stop treating every underscore tag as a series — `big_ass`, `3d animation`, etc. were misclassified and kept with Series learning on. Purge now tombstones them and overwrites cloud sync. Post opens use API tag types (copyright = series).

## 2.0.65 - 2026-06-07

- **Learning gate enforcement:** With only Series learning on, artist/minor/character topics no longer learn, rank, or appear in feed match labels. Toggling categories purges disabled algorithm topics. OpenAI summaries respect enabled categories.
- **Exact tag matching:** Feed "Matches N topics" uses literal post tags only — fixes false matches like substring hallucinations (e.g. "more" inside "asking_for_more").

## 2.0.64 - 2026-06-07

- **For You training feel (PC + Android):** Feed ranking now prioritizes your learned topic weights and multi-query hits again. Each card shows which of your topics matched; status text names your top topics. **Highest training score** sort uses profile strength, not generic post score.

## 2.0.63 - 2026-06-07

- **For You sort &amp; filter (PC + Android):** New menu on the feed — sort by most tag matches, highest score, or random; filter by All, Images, Videos, GIFs, or Animated. Choices persist and re-apply instantly without refetching.

## 2.0.62 - 2026-06-07

- **Granular learning toggles (PC + Android):** Replace the single learning switch with category controls — master **Toggle learning** plus **Artist learning**, **Series learning**, and **Minor tags (1girl, …)**. Disabled categories are skipped when building your taste profile.

## 2.0.61 - 2026-06-07

- **Page loading animations (PC + Android):** Each tab shows its own themed loading overlay (glyph + motion) while fetching — Browse ⌕, For You ✦, Library ♥, Local ▤, Tag sets 🏷, Downloads ↓, Sync ↻, Help 📖, Account ◎, and more.
- Overlays only appear on the active page so background loads do not flash on other tabs.

## 2.0.60 - 2026-06-07

- **Promote to manual (PC):** In **Topics → From algorithm**, **Promote** moves a learned topic into **Manual** at its current score — no decay, no auto-removal, and it frees an algorithm slot (120-topic cap).
- Promoted topics sync to Android via cloud profile (`Promoted to manual` reason).

## 2.0.59 - 2026-06-07

- **Manual topic protection (PC + Android):** Typed, boosted, or promoted topics no longer decay, are not auto-trimmed, and browsing signals do not change their score.
- **Algorithm slot limit:** The 120-topic cap applies only to algorithm-learned topics; manual topics are extra.

## 2.0.58 - 2026-06-07

- **Topics manager (PC):** **Manual** and **From algorithm** are separate collapsible sections (typed / boosted vs learned automatically).
- Hint text explains manual rules and that **Remove** on algorithm topics frees a slot.

## 2.0.57 - 2026-06-07

- **For You strength fix (PC + Android):** Feed queries and post ranking now follow topic **strength scores** (pin → score → recency), not character-first bucketing.
- Feed posts sort by **weighted topic match**; top queries use your highest-scored topics.
- Manually set scores are protected from search decay.

## 2.0.56 - 2026-06-07

- **New topic highlight (PC):** Topics added or boosted while **Manage → Topics** is open show a green **●**, **new** badge, and left border; clears on hover.

## 2.0.55 - 2026-06-07

- **Remove topic fix (PC):** Removed topics are tombstoned so cloud merge / force rebuild no longer brings them back instantly.
- **Remove** uploads with overwrite (no merge-from-cloud-first); **Add topic** / **Boost** can restore a removed tag.

## 2.0.54 - 2026-06-07

- Republish / install refresh (includes v2.0.53 fixes).

## 2.0.53 - 2026-06-07

- **Force update algorithm (PC):** **Refresh ▾ → Force update algorithm** replays signals and refreshes OpenAI immediately (skips 5‑minute throttle).
- **Topics by category (PC):** Characters, series, positions, general, artists, other.
- **Boost (PC):** **Recent signals** → **Boost** pushes a tag into topics with strong score.
- **Remove vs Hide (PC):** **Remove** deletes and frees a slot; **Hide** blocks and zeros score but keeps the slot.

## 2.0.52 - 2026-06-07

- Republish / install refresh (includes v2.0.51 fixes).

## 2.0.51 - 2026-06-07

- **Collapsible profile (PC):** Summary, profile, and manage controls under **Profile & summary** expander (collapsed by default; feed-first).
- **Collapsible profile (Android):** Interest summary and chips under a collapsed row below the feed.

## 2.0.50 - 2026-06-07

- Republish / install refresh (includes v2.0.48–2.0.49 feed and summary fixes).

## 2.0.49 - 2026-06-07

- Republish / install refresh (includes v2.0.48 interest summary and decay).

## 2.0.48 - 2026-06-07

- **Interest summary (PC + Android):** Feed shows structured text — current interest, rising topic ⬆️, declining topic ⬇️ (from recent searches + decay).
- **Search decay (PC + Android):** After **100 searches** without a tag, lose **1 point per search** beyond that (101 = −1, 102 = −2, …); pinned topics exempt.
- Historical decay migration on profile schema v3.

## 2.0.47 - 2026-06-07

- **Feed vs profile fix (PC + Android):** Character-first feed queries so the feed matches trained interests (e.g. Himeko), not generic high-score art.
- **Query repair:** Broken lines like `honkai:_star_rail` / `honkai star rail` normalized for Rule34.
- Generic-only queries deprioritized; character tag overlap boosts ranking.
- **`{ }` data viewer:** Search lines show the **resolved** query the app sends.

## 2.0.46 - 2026-06-07

- **Android feed parity:** Uses synced PC search lines, same query order, `score:desc` per query, PC-style tag scoring, 48 posts, and profile summary when cloud-backed.

## 2.0.45 - 2026-06-07

- **Android profile preserve:** Phone browsing no longer rebuilds away a cloud-synced PC profile; search-line ordering aligned with PC.

## 2.0.44 - 2026-06-07

- **For You data viewer (Android):** Small **`{ }`** button opens scrollable sync detail — account, counts, topic scores, search lines (verify PC profile on phone).
- **PC data viewer:** Same `{ }` pattern on For You for local profile inspection.

## 2.0.43 - 2026-06-07

- **Lite vs full control:** Android framed as **Lite** companion; Windows as **full control** (train For You, tag sets, blocks).
- Cross-platform hints on Settings, Account, Cloud sync, For You, and Help (**Windows & Android** topic).

## 2.0.42 - 2026-06-07

- **Android Lite redesign:** Streamlined Settings (essentials / browse / from PC), collapsed Browse search, simplified Saved/Sync copy.
- **PC headers:** Full Control callouts on Settings, For You, Browse, Sync.

## 2.0.41 - 2026-06-07

- **For You Lite (Android):** Feed-first UI — topic chips and search suggestions only; full topic tuning stays on PC.

## 2.0.40 - 2026-06-07

- **Compact For You (Android):** Dense topic rows (score + Pin/Set/Hide), top 12 + **Show all**, Refresh/Manage at top.

## 2.0.39 - 2026-06-07

- **Google sign-in (Android):** Sign-out clears cached account; sign-in shows account picker (fix wrong-account sync vs PC).
- Account screen shows **Account ID** to match desktop Firebase user.

## 2.0.38 - 2026-06-07

- **For You cloud diagnostics:** Longer download timeouts for large profiles; sync errors show email, UID, and JSON size.
- PC upload verified; mobile no longer re-uploads immediately after download.

## 2.0.37 - 2026-06-07

- **For You JSON fix:** Mobile parser accepts camelCase and PascalCase (PC profile format); empty mobile profile no longer overwrites cloud training.
- Desktop uploads For You payload in camelCase for consistency.

## 2.0.36 - 2026-06-07

- **Android For You sync fix:** Stop wiping PC-trained topics after cloud download when `rebuildLocalProfile` ran on phone-only activity.

## 2.0.35 - 2026-06-07

- **Android sync hang fix:** Break credentials save recursion loop; HTTP/step timeouts; **Cancel** during sync.

## 2.0.34 - 2026-06-07

- **Android navigation:** Bottom bar reduced to **Browse · For You · Saved · Settings** (4 tabs).
- Account, Help, Cloud sync, tag sets, and PC remote live under Settings.

## 2.0.33 - 2026-06-07

- **Android:** Cloud sync moved out of bottom nav into **Settings → Open cloud sync**.

## 2.0.32 - 2026-06-07

- **Cloud sync page (PC + Android):** Dedicated **Cloud sync** tab/page (like Downloads) with per-step progress cards — no blocking modal.

## 2.0.31 - 2026-06-07

- **Desktop sync overlay fix:** `InvokeAsync` for progress UI; **Close** always works; clear stuck running state after failures.

## 2.0.30 - 2026-06-07

- **Sync crash fix:** Desktop marshals WPF progress updates on UI thread; Android runs sync on a background thread.

## 2.0.29 - 2026-06-07

- **Sync progress panel:** Step-by-step progress for credentials, library, saved tags, and For You (initial modal overlay version).

## 2.0.28 - 2026-06-07

- **Sync toast:** Completion message lists For You topics, search lines, and signals; clearer credentials/library wording.
## 2.0.27 - 2026-06-06

- **Android For You infinite loading fix:** deadlock when refreshing profile (save inside mutex); feed/search/cloud calls now time out instead of spinning forever.

## 2.0.26 - 2026-06-06

- **Android For You cloud sync:** pull desktop taste profile when opening For You; stop wiping synced topics on refresh; map desktop activity types correctly; skip redundant OpenAI rebuild when cloud profile is already present.
- **For You cloud payload:** sync topic reason text between desktop and phone.

## 2.0.25 - 2026-06-06

- **Gallery cards:** show tag-based titles (series · character · artist) instead of post IDs; title text is readable on dark cards.
- **For You viewer crash:** opening a feed post no longer crashes when preloading neighbor images (uses the feed list, not Browse results).
- **For You scoring fix:** OpenAI no longer overwrites topic strength with invented scores (e.g. 70 from one video); it only adds a small hint and writes the reason text.
- **For You scoring model:** each learning signal adds directly to the 0–100 score (0.12 signal → +0.12 score; max +1.0 per event).
- **Score migration:** existing topic scores are divided by 6 once on upgrade so old inflated values align with the new scale.
## 2.0.18 - 2026-06-06

- **For You reset fix:** Reset profile / Reset all now clear saved signals and overwrite cloud sync instead of rebuilding taste from history.
- Updated desktop and Android app versions.
## 2.0.17 - 2026-06-06

- **For You topic score 0–100:** manual topic score uses decimals on a 0–100 scale; browsing/search signals still apply as small 0–1 strength gains.
- Updated desktop and Android app versions.
## 2.0.16 - 2026-06-06

- **For You manage overlay:** Topics, suggested searches, and signals open in a large in-app panel (not a separate OS window); add/set strength is inline in the overlay.
- Updated desktop and Android app versions.
## 2.0.15 - 2026-06-06

- **For You manage menus:** Topics, suggested searches, and recent signals open in separate management windows instead of inline dropdowns on desktop.
- **Strength display fix:** topic strength now shows correctly on the 0.0–1.0 scale (legacy/OpenAI scores normalized on load).
- Updated desktop and Android app versions.
## 2.0.14 - 2026-06-06

- **Manual For You topics:** add tags yourself and set strength from 0.0–1.0 on desktop, Android, and MAUI; existing topics have a **Set** control instead of boost.
- Updated desktop and Android app versions.
## 2.0.13 - 2026-06-07

- **For You strength model:** topics now use a 0.0–1.0 strength scale with weighted signals (search, saves, views, skips, blocks, etc.) on desktop and Android.
- Updated desktop and Android app versions.

## 2.0.12 - 2026-06-06

- **Browse fix:** For You no longer replaces Browse search results (separate feed collection; only refreshes when For You tab is open).
- Updated desktop and Android app versions.

## 2.0.11 - 2026-06-06

- **For You ranking:** posts matching more of your topics now sort first (match count, then relevance); multi-tag searches run first; site score no longer drowns overlap.
- Updated desktop and Android app versions.

## 2.0.10 - 2026-06-06

- **For You learning fix:** filter/search settings (like Filter AI posts and `-ai*`) are no longer learned as interests; existing `ai_art` topics are cleaned up on load.
- Updated desktop and Android app versions.

## 2.0.9 - 2026-06-06

- **For You ranking:** posts that match more of your learned topics rank higher in the feed (desktop + Android).
- Updated desktop and Android app versions.

## 2.0.8 - 2026-06-06

- **Help / How To tab:** tutorials on desktop, Android, and MAUI (tagging, filters, sync, phone remote, etc.) with platform-specific steps.
- **Search presets fix:** enabling a preset now shows its tags as chips again (not just the preset counter).
- **Sync combine:** tag sets, API credentials, and For You profile merge local + cloud instead of overwriting — both devices keep unique saved data.
- Updated desktop and Android app versions.

## 2.0.7 - 2026-06-04

- **For You fix:** feed grid renders again (removed broken nested scroll layout); page reloads when you open the tab.
- **For You fix:** OpenAI profile refresh no longer blocks the feed from loading.
- **For You fix:** searches now save as suggested search lines; fallback uses your current Browse tags when the profile is still empty.

## 2.0.6 - 2026-06-04

- **Unified For You:** desktop and Android now share the same profile model — topics, suggested searches, recent signals, OpenAI summary, and cloud sync of search lines.
- **Desktop For You parity:** suggested searches (pin/hide/run), profile summary, OpenAI toggle, remove individual signals, Clear / Reset profile / Reset all.
- **Settings alignment:** shared JSON field names (`forYouEnabled`, `forYouCloudSync`, `useOpenAiForForYou`) across platforms.
- **Browse feed:** Favorite and Watch later actions on desktop feed view (matching Android).
- **Cross-device sync:** Firestore For You profile now includes search lines and summary.

## 2.0.5 - 2026-06-04

- **Desktop feature parity:** Browse **Grid/Feed** toggle works without a phone remote connected.
- **Settings → For You:** enable learning and cloud sync toggles (same as Android).
- **Settings → Browse:** default layout (Grid/Feed) and feed media quality options.
- **For You page:** recent signals panel, separate **Clear** (keeps learning on) vs **Reset** (turns learning off).

## 2.0.4 - 2026-06-04

- **For You cloud sync:** signed-in accounts now sync learned topics and activity across desktop and Android (Firestore `forYouProfile`).
- Desktop uploads profile changes automatically when cloud sync is enabled.
- Android adds **Sync with account** toggle under Settings → For You.
- **Version alignment:** desktop and Android both report **v2.0.4** (desktop was still on 1.0.25).

## 2.0.3 - 2026-06-04

- **Android For You:** fixed crash when opening the tab (lazy grid nested inside a scrollable column).
- Updated Android app version.

## 1.0.25 - 2026-05-27

- **Forgot the name? search fix:** DuckDuckGo now returns bot-block pages (HTTP 202, zero results) — replaced with **Reddit** (including franchise subreddits like r/HonkaiStarRail) and **Wikipedia** as primary sources.
- **HSR bow example:** queries like “honkai star rail female bow user” now pull threads mentioning March 7th / bow users instead of failing with “no results”.
- DuckDuckGo kept only as a last-resort fallback when Reddit/Wikipedia are thin.
- Updated desktop and Android app versions.
## 1.0.24 - 2026-05-27

- **Web search reliability:** Forgot the name? now uses a dedicated search client (not the Rule34 JSON HttpClient) with browser headers, retries, and pacing between queries.
- **Rate-limit fix:** one failed/throttled DuckDuckGo request no longer aborts the whole lookup; partial results are kept.
- **Smarter query fallbacks:** automatic extra searches (e.g. bow → archer/bow character/wiki) so queries like HSR bow users can surface March 7th / Yukong from Reddit and wiki snippets.
- Updated desktop and Android app versions.
## 1.0.23 - 2026-05-27

- **Forgot the name? now fact-checks:** runs real web searches (DuckDuckGo), then OpenAI may only answer from those snippets — no inventing weapons, franchises, or traits.
- **Verification pass:** a second AI step drops candidates that are not supported by the cited search results (fixes cases like wrong-game characters or made-up bow users).
- **Evidence links:** each match must cite web result indexes; code rejects answers whose evidence does not mention the name.
- Updated desktop and Android app versions.
## 1.0.22 - 2026-05-27

- **Forgot the name? mode (was Interpret search):** plain-language descriptions now return search-result style identity matches (character, series, etc.) — **no booru tags**.
- **Memory lookup flow:** e.g. "female viltrumite in white clothing" → Anissa (Invincible) with subtitle and snippet, not tag lines.
- **Discover UI:** dedicated result cards with copy-name action on desktop and Android; mode renamed to **Forgot the name?**
- Updated desktop and Android app versions.
## 1.0.21 - 2026-05-26

- **Interpret search quality pass:** strict canonicalization now keeps booru-validated tags only for semantic search mode.
- **Alias normalization:** plain-language tokens like `female` / `male` now map to canonical booru tags (`1girl` / `1boy`) during intent interpretation.
- **Reduced literal leakage:** invalid free-text tokens are dropped in Interpret search results instead of being emitted as fake tags.
- Updated desktop and Android app versions.
## 1.0.20 - 2026-05-26

- **New AI mode: Interpret search** (desktop + Android + MAUI) for search-engine-style natural-language queries.
- **Semantic intent mapping:** mode infers likely character/series/concept references plus traits and returns full booru-ready search lines.
- **Discover UI update:** added an explicit **Interpret search** mode next to **By theme** / **Find tag name**.
- Updated desktop and Android app versions.
## 1.0.19 - 2026-05-26

- **Windows video crash fix:** removed unsafe CoreAudio COM device-name interop from playback UI that was causing `AccessViolationException` during video startup on some systems.
- **Playback UI fallback:** audio output hint now uses a safe static fallback label to avoid native interop crashes while keeping viewer/feed controls functional.
- Updated desktop and Android app versions.
## 1.0.18 - 2026-05-26

- **Windows video playback stability:** audio output label lookup is now throttled and cached instead of re-querying COM on every playback tick.
- **COM resource safety:** properly releases MMDevice COM objects during audio-device name reads to prevent playback-session instability over time.
- Updated desktop and Android app versions.
## 1.0.17 - 2026-05-26

- **Android video crash hardening:** wrapped audio-route label lookup in safe fallback handling so OEM/framework audio query exceptions no longer crash playback screens.
- **Fullscreen playback stability:** guarded fullscreen window insets setup to avoid activity-cast crashes in wrapped Compose contexts.
- Updated desktop and Android app versions.
## 1.0.16 - 2026-05-26

- **Saved tag sets cloud sync:** saved tag bundles now persist locally and sync via Firestore across desktop and Android, with conflict merge by last update time.
- **Saved tag set management UI:** added a dedicated **Tag sets** page on Windows and **Manage tag sets** screen on Android for apply, edit, delete, save-current, and manual cloud sync.
- **Shared sync behavior:** settings saves now push saved tag sets to cloud automatically when signed in.
## 1.0.15 - 2026-05-26

- **Audio output label:** small “Audio: …” hint on video playback (viewer + feed on PC; viewer, feed, and browse video on Android) showing the active output device name.

## 1.0.14 - 2026-05-26

- **Phone remote — per-page controls:** Browse, Library, Local, and Downloads each have their own tab on the remote (no more one long page of every control).
- **Library remote:** switch Favorites / Watch later / Lists, pick a saved list, refresh metadata on the PC.
- **Local remote:** switch library, category group, and subcategory filters.
- **Downloads remote:** queue summary and clear finished jobs on the PC.
- **Viewer:** playback controls stay visible whenever the PC viewer is open, on any tab.

## 1.0.13 - 2026-05-26

- **Fix PC crash on remote tap:** remote connect/disconnect no longer fires UI work from the HTTP thread; feed media URLs are validated before load; remote commands and state capture are wrapped so failures show a message instead of closing the app.
## 1.0.12 - 2026-05-26

- **PC remote layout:** grouped controls into cards with aligned 2–3 column button grids (fixes uneven FlowRow wrapping).
- **PC remote navigation:** chips to switch the PC app to Browse, Library, Local, Downloads, Settings, and Account; status shows active screen.

## 1.0.11 - 2026-05-26

- **PC feed mode (phone remote):** when your phone is connected to the PC remote, Browse unlocks vertical **Feed** mode (TikTok-style) on Windows — one post per screen, mouse wheel or remote scroll.
- **Remote feed controls:** **↑ Prev post** / **↓ Next post**, **Feed mode** / **Grid mode** on Android remote; PC shows Grid/Feed toggle while the phone is connected.
- **Infinite scroll:** feed loads more search pages automatically near the end of results.

## 1.0.10 - 2026-05-26

- **Saved tags:** save current search tags (or a single tag) as named presets stored in settings — like saving posts to a list, but for tag combinations.
- **Search presets UI:** new **Saved tags** section at the top of the preset picker (desktop + Android); toggle to apply, delete to remove.
- **Browse:** **Save tags…** on Windows; **Save tags** / **Save current tags…** on Android.

## 1.0.9 - 2026-05-26

- **PC remote playback:** add full viewer video controls from Android remote (play/pause, seek +/- seconds, mute, set muted, set volume, set speed).
- **Remote API/state:** expose viewer playback state (playing, muted, position/duration, volume, speed) for live status in remote UI.

## 1.0.8 - 2026-05-26

- Release build refresh (includes 1.0.7 PC remote tag autocomplete).

## 1.0.7 - 2026-05-26

- **Android PC remote:** Rule34 tag autocomplete on **Add tag** and **Replace all tags** (same as Browse).

## 1.0.6 - 2026-05-26

- **Changelog:** fix dark-theme rendering on Windows and stop Android crash (custom markdown view instead of MdXaml / third-party Compose markdown).

## 1.0.5 - 2026-05-26

- **Changelog:** render `changelog.md` with real Markdown on Windows (MdXaml) and Android (Compose markdown renderer) instead of raw `#` / `**` text.

## 1.0.4 - 2026-05-26

- **PC remote (Android):** richer controls — go to page, show browse, clear/set/remove tags, tag chips, 3-column results, PC status card.
- **PC remote (PC):** new commands: remove tag, clear tags, go to page, focus browse.
- **Android:** changelog syncs from repo `changelog.md` into app assets on every version bump / post-change build.

## 1.0.3 - 2026-05-26

- **Phone remote (LAN):** fix PC server failing on Windows with "Access is denied" by using a TCP-based HTTP listener instead of `HttpListener` (no admin / URL ACL needed).
- **Phone remote (LAN):** hide QR code and pairing PIN on PC until the server is actually listening.
- **Android:** keep QR scanner in portrait (override ZXing `CaptureActivity` landscape default).

## 1.0.2 - 2026-05-26

- **Install scripts (PC):** Rule34 Gallery and PH Browser install paths are isolated so one app cannot overwrite the other’s shortcuts or folder.

## 1.0.1 - 2026-05-26

- **Version badge (PC + Android):** Tapping the version label opens the bundled changelog.

## 1.0.0 - 2026-05-26

- Improve "Find tag name" mode: resolve what you mean with OpenAI, then find the best Rule34 tag.
- If autocomplete can't find the resolved name, fall back to converting it into a plausible tag token.
- Add a clickable version label in the UI that opens this changelog.




















































































