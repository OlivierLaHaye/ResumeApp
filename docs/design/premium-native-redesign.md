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

- The 160-DIP icon header and its collapse toggle are replaced by a single 56-DIP identity row inside the window chrome: R2 icon (28 DIP), `AppTitle` / `AppSubtitle`, then the EN | FR and Light | Dark segmented controls, then the caption buttons at the native right-hand position. The collapse toggle is removed because the space it saved no longer exists.
- Navigation is a left-aligned text tab strip: selected tab uses accent text and a 2-DIP accent underline; hover changes colour only; keyboard focus shows an immediate 2-DIP focus ring.
- Content sits on one quiet surface with hairline separators; redundant card-inside-card borders are removed. Meaningful groups (a project, an album, a selected role) keep a boundary.
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
| Accent graphics (underline, focus ring) | `#047AF7` on `#141414` ≈ 4.5:1 | `#047AF7` on `#F2F2F2` ≈ 3.7:1 |

Targets: ≥ 4.5:1 for normal text, ≥ 3:1 for meaningful control and focus graphics. This is a readability target, not a WCAG certification.

High contrast: when Windows high contrast is on, the theme service applies a dictionary built from `SystemColors` (window, window text, highlight, hot-track, gray text) instead of the app palette and re-applies it when the setting changes.

## 5. Motion

Motion only explains a state change. All remaining motion is code-driven through one `MotionPolicy` that reads `SystemParameters.ClientAreaAnimation` and listens for changes.

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

- Tab order: identity row controls → caption buttons → section tabs → page content.
- Section tabs: arrow keys / Ctrl+Tab switch sections; focus ring is visible immediately.
- Carousel: focusable; Left/Right/Home/End change image, Enter opens the viewer; previous, next and enlarge buttons are focusable with localized automation names; the position is announced as "Image n of m".
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
