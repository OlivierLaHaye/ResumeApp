# Premium native redesign — design note

Status: chosen direction for branch `resumeApp/premium-native-redesign` (base `main` 8c07625). Implementation and validation results are recorded in section 10.
Scope: ResumeApp only. Résumé facts, dates, employer labels (FARO CREAFORM), product names, contact data, real photographs and the approved R2 icon are unchanged.

## 1. Directions considered

| Direction | Idea | Why not / why |
|---|---|---|
| A. Dashboard | Left nav rail, metric tiles, cards everywhere | Generic SaaS look; invites invented metrics; card-in-card noise. Rejected. |
| B. Immersive gallery | Full-bleed photography, overlay text | Photography would dominate an engineering résumé; text over images hurts contrast. Rejected. |
| C. Compact editorial desktop portfolio | Quiet chrome, typographic hierarchy, hairlines, generous but purposeful space, photography framed as evidence | Fits an engineering résumé that also shows visual craft. **Chosen.** |

Direction C keeps the six-section navigation, Inter / Inconsolata, the blue accent (`#047AF7`) and the existing light/dark themes. Polish comes from composition, readable type, precise spacing and restrained motion — not from new decoration.

## 2. Shell

- The 160-DIP icon header and its collapse toggle are replaced by a single identity row inside the window chrome. The collapse toggle is removed because the space it saved no longer exists.
- Hybrid with the original identity (decided after the first review, at Olivier's request): the window keeps its 40-DIP rounded corners (custom `WindowChrome`, square when maximized), and the signature centred elements return.
  - Left: R2 icon (32 DIP), `AppTitle` / `AppSubtitle`, trimmed with an ellipsis when space is short.
  - Centre: the minimize / maximize / close buttons sit in a small capsule attached to the top edge.
  - Right: the EN | FR and Light | Dark segmented pills.
  - The image viewer uses the same frame and centred caption capsule.
- Navigation is a centred strip of six equal-width tabs inside a rounded 14-DIP band. The selected tab is a neutral pill with accent text; hover changes the pill surface only; keyboard focus shows an immediate 2-DIP focus ring. The earlier left-aligned underline strip was replaced because it lost the original identity.
- Content sits on one quiet surface with 16-DIP rounded groups; redundant card-inside-card borders are removed. Meaningful groups (a project, an album, a selected role) keep a boundary.
- Segmented controls are command toggle buttons: `IsChecked` is bound one-way to the view model and activation executes the command, so mouse, keyboard (Space) and UI Automation (Toggle) produce exactly one state transition and the visual, logical and UIA states cannot diverge.

## 3. Adaptive layout (effective DIP)

| Size class | Window width | Behaviour |
|---|---|---|
| Compact | < 1100 | Single column; project facts above the carousel; skills in one column. |
| Regular | 1100–1599 | Two columns where useful (Overview identity / summary, Projects facts / images). |
| Wide | ≥ 1600 | Same as Regular with a bounded content width; reading columns capped near 760 DIP. |

- Minimum window size: 960 × 640 DIP, clamped to the monitor work area in `WM_GETMINMAXINFO` (the old 1 400-DIP minimum exceeded the 1 280 DIP available on a 1920-px screen at 150 %). The image viewer uses the same rule.
- Initial size stays 95 % of the work area of the monitor under the cursor; maximise keeps the work-area bounds.
- Size class is an inherited attached property computed from the window width, so pages react without code-behind.

## 4. Type, colour and contrast

| Role | Size (DIP) | Weight | Brush |
|---|---|---|---|
| Display (name) | 32 | SemiBold | TextPrimary |
| Page title | 26 | SemiBold | TextPrimary |
| Section title | 18 | SemiBold | TextPrimary |
| Body | 15 | Regular | TextPrimary |
| Secondary / metadata | 13 | Regular | TextSecondary |
| Label (caps) | 12 | SemiBold | TextSecondary |
| Mono chip | 13 | Regular (Inconsolata) | TextPrimary |

Essential text no longer uses opacity. Nominal solid-surface contrast (computed, not measured on screen):

| Pair | Dark | Light |
|---|---|---|
| TextSecondary on raised surface | `#A6A6A6` on `#1C1C1C` ≈ 7.0:1 | `#4D4D4D` on `#E6E6E6` ≈ 6.8:1 |
| AccentText on page surface | `#4DA3FF` on `#141414` ≈ 7.0:1 | `#0057D6` on `#F2F2F2` ≈ 5.6:1 |
| AccentText on selected tab / segment pill | `#4DA3FF` on `#333333` ≈ 4.8:1 | `#0057D6` on `#DCDCDC` ≈ 4.6:1 |
| Accent graphics (focus ring, selected role border) | `#047AF7` on `#141414` ≈ 4.5:1 | `#047AF7` on `#F2F2F2` ≈ 3.7:1 |

Targets: ≥ 4.5:1 for normal text, ≥ 3:1 for meaningful control and focus graphics. This is a readability target, not a WCAG certification.

High contrast: when Windows high contrast is on, the theme service applies a dictionary built from `SystemColors` (window, window text, highlight, highlight text, hot-track) instead of the app palette and re-applies it when the setting changes. Selected pills use the window surface with a highlight outline and hot-track text (the `SurfacePillSelectedBorder` token matches the pill fill in the normal themes, so the outline only appears in high contrast); the frame border uses window text; timeline lanes use highlight and are drawn opaque.

## 5. Motion

Motion only explains a state change. The code-driven motion below goes through one `MotionPolicy` that reads `SystemParameters.ClientAreaAnimation` and listens for changes; a running section fade stops when animation is turned off.

Limit: a few implicit storyboards that remain in `Resources/Controls.xaml` templates (some button, tooltip and scroll-viewer states) are not gated by `MotionPolicy` yet.

| Interaction | Before | After | Reduced motion |
|---|---|---|---|
| Section switch | 300 ms content fade | 160 ms opacity fade | Instant |
| Carousel image change | 240 ms scale/translate/opacity | 220 ms (direction cue) | Instant final state |
| Card ↔ timeline scroll sync | 220 ms | 220 ms | Instant |
| Timeline drag release | Inertia | Inertia | No inertia |
| Hover (tabs, chips, buttons, cards) | Scale 1.02–1.12, glow fades | Colour change, no animation | Same |
| Press | Scale 0.90–0.98 | Colour change | Same |
| Keyboard focus | Glow / fade | Immediate 2-DIP ring | Same |
| Header collapse | Rotate + fade | Removed | — |

No ambient loops, autoplay, cursor followers, parallax or glow.

## 6. Keyboard and accessibility

- Tab order follows the visual order left to right: caption buttons (centre) → EN | FR → Light | Dark → section tabs → page content.
- Section tabs: arrow keys / Ctrl+Tab switch sections; focus ring is visible immediately.
- Carousel: focusable; Left/Right/Home/End change image; previous and next buttons are focusable with localized automation names; Enter or a double-click opens the viewer (no separate enlarge button); the position is announced as "Image n of m".
- Viewer: opens focused; Left/Right navigate; Escape closes; focus returns to the opening carousel with the same image selected.
- Timeline: existing Home/End/arrow/Ctrl contract is preserved; a single focus outline is drawn and refreshed on focus change.
- Contact and project link buttons are focusable.

## 7. Loading, empty and error states

- Startup: a localized "Loading…" line is shown until the shell has its data; if initialization fails, a localized failure message replaces the blank window.
- Galleries: "Loading images…", "No images yet." and "Images could not be loaded." are shown in the carousel placeholder instead of a blank frame.

## 8. Images

- Carousel images are decoded at preview size: the long edge is capped from the primary screen size × DPI × 0.75 (bounded 1 280–3 840 px) using `DecodePixelWidth`/`DecodePixelHeight`; aspect ratio is preserved; bitmaps are frozen and decoded off the UI thread; collection updates stay on the dispatcher.
- The first image of each gallery is published before the others; decoding concurrency is bounded.
- The viewer decodes the full-resolution current image and its neighbours on demand and releases them when it closes.
- Photography stays `HighQuality` scaled; no global quality downgrade.

## 9. Out of scope

- Font-file trimming, `Controls.xaml` cleanup beyond touched styles, timeline roadmap features, installer/release work.

## 10. Results (branch `resumeApp/premium-native-redesign`)

Validation recorded on 2026-10-02. Final release-build evidence is kept with the hand-off report, not in this note.

- Tests: the xUnit suite grew from 576 (baseline `8c07625`) to 701 passing tests (Debug, `dotnet test`, 0 failed). New coverage includes the command toggle, motion policy, high-contrast role mapping, adaptive size classes, window bounds clamping, carousel/viewer keyboard handling, preview-size decoding, the shared decode limiter, timeline label collision and culture formatting.
- UI Automation (real app, commit `601a607`): segmented toggles expose the Toggle pattern with localized names; re-selecting the active language keeps it on; tabs and carousel controls have localized names ("Image 1 of 10" / "Image 1 sur 6").
- Performance: Release builds of `8c07625` (baseline) and `601a607` (changed) were measured interleaved, three runs each, on the same machine. Before the hybrid shell commits.

| Metric | Baseline runs | Changed runs |
|---|---|---|
| Main window visible (ms) | 1598 / 1337 / 1433 | 1363 / 1269 / 1294 |
| Named tabs ready (ms) | 2569 / 2244 / 2429 | 2560 / 2391 / 2643 |
| Private memory after 15 s idle (MB) | 945.8 / 931.9 / 941.1 | 654.1 / 659.6 / 666.7 |
| Private memory on Photography (MB) | 1095.5 / 1085.6 / 1039.1 | 709.5 / 707.3 / 714.4 |

The memory drop comes from preview-size decoding. Start-up time to named tabs did not change measurably.

Known remaining items:
- Some French résumé strings still contain English text (for example "Lead UI/UX…", "(Hybrid)", "Tech"). They need an author-written translation and were not machine translated.
- The minimum window size is clamped to the work area at start-up but is not re-evaluated when the window moves to another monitor.
- Caption button automation names are empty for a moment during start-up, before resources load.
- Implicit template storyboards in `Controls.xaml` are not gated by `MotionPolicy` (see section 5).
