# Desktop Horizontal Timeline — Technical Spec

> Planning doc. No code in this change. Target: premium horizontal lane-based career timeline for the WPF resume app. Source-of-truth reference: the horizontal lane mockup previously shared by the user (bottom month/year axis, horizontal role bars, overlapping lanes, Today/Present marker, labels inside bars, polished dark surface).

## 1. Scope and intent

### 1.1 What this spec is

A durable blueprint for the desktop target timeline. Replaces the visual intent established in the earlier redesign passes (`claude/redesign-timeline-ux-cE5Zj`, `Phase 1..5`) but **reuses** every part of the existing implementation that already matches the target.

### 1.2 What this spec is not

- Not a port spec. Web adaptation is tracked separately in `app-to-web-parity-matrix.md`.
- Not an implementation. No code changes in this branch.
- Not a data-source change. Entries stay resx-driven.

### 1.3 Target summary (one paragraph)

Horizontal time axis at bottom (month/year ticks, adaptive granularity). Above axis, multiple horizontal lanes each hosting one or more role/company bars spanning a date range. Overlapping roles packed greedily by lane. Each bar carries a short label inside it (role · company) plus accent color. A vertical dashed **Today / Présent** marker crosses all lanes. Dense, polished, dark-surface presentation with predictable palette cycle. Hover surfaces richer detail (date range, location, short highlight); click synchronizes the detail card list beneath.

## 2. Reuse vs. replace

Current `Controls/TimelineControl.cs` already renders:

- horizontal lanes with greedy packing (`BuildVisibleTimeFrames`, `FindLaneIndex`),
- rounded bars with hover/selected states (lines 1746–1819),
- clipped in-bar title with leading dot (1824–1863),
- dashed baseline hairline (1868),
- adaptive major/minor ticks with 14 candidate schedules (2015–2081),
- Today marker pill + dashed vertical (1946–1988),
- selected-date pill (2138–2196),
- pan, wheel zoom, fit animation, inertia, keyboard nav,
- focus outline + FormattedText cache.

**Verdict:** keep the control. The target is not a rewrite; it is an **uplift** of label density, localization, axis polish, surface hierarchy, and layout presence (larger canvas, better integration with detail cards). Throwing away the rendering engine would regress pan/zoom/ticks/inertia that already work.

### Reuse table

| Area | Current code | Target decision |
|---|---|---|
| Lane packing | `FindLaneIndex` ([Controls/TimelineControl.cs:397](Controls/TimelineControl.cs)) | Reuse. Tune `TimeFrameLaneGapPixels` to 10 at current densities. |
| Bar draw | lines 1746–1819 | Reuse. Add soft drop-shadow on selected; keep opacity ladder 60/85/100. |
| Bar label | lines 1824–1863 | Extend: two-line label when bar ≥ 64 px wide and lane ≥ 34 px high (`Role`, then `Company` smaller/dimmer). Fallback to current one-line at tight widths. |
| Axis ticks | `DrawTicksAndLabels` + `ChooseTickLabelSchedule` | Reuse. Add minor-tick softening (opacity 0.35). |
| Era bands | `DrawYearEraBands` | Reuse. Reduce opacity from 0.03 → 0.025 to avoid clash with bar highlights. |
| Today marker | `DrawTodayMarker` at 1946–1988 | Reuse line + pill. **Localize the label** (see §6). |
| Selected-date pill | 2138–2196 | Reuse. Add chevron/tick anchor pointing down to axis. |
| Pan/zoom/inertia | 615–1689 | Reuse unchanged. |
| Data model | [Models/TimelineTimeFrameItem.cs](Models/TimelineTimeFrameItem.cs) | Reuse `StartDate/EndDate/Title/AccentColorKey`. Add optional `SubtitleText` (role) through a new bound DP on the control, sourced from the entry VM — model stays minimal. |
| Scroll sync | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs](Behaviors/ExperienceTimelineScrollSyncBehavior.cs) | Reuse. No change. |
| ExperiencePage layout | [Pages/ExperiencePage.xaml:141–154](Pages/ExperiencePage.xaml) | Change: timeline row height grows, gets dedicated `MinHeight=220`, `MaxHeight=360`, and a real top/bottom rhythm. Detail list keeps `*` row. |

## 3. Feature decision table

Priority: **P0** = must ship. **P1** = target parity. **P2** = polish. **Defer** = not in first pass.

| # | Feature | Priority | Notes |
|---|---|---|---|
| F1 | Bottom month/year axis with adaptive granularity | P0 | Already works; ensure `fr-CA` format parity (`MMM yyyy` → `janv. 2024`). |
| F2 | Horizontal lanes with greedy overlap packing | P0 | Reuse. |
| F3 | Rounded bars with accent color cycle | P0 | Reuse; verify 8-palette determinism against entry order. |
| F4 | In-bar label — role on top, company on bottom | P0 | New two-line layout when space allows; single-line fallback. |
| F5 | Vertical Today/Present marker + localized pill | P0 | Replace hardcoded `"Today"` string. |
| F6 | Hover state: 85 % opacity + subtle hairline stroke | P0 | Reuse. |
| F7 | Selected state: 100 % opacity + accent glow + outline | P0 | Reuse; add 1 px inner highlight for depth. |
| F8 | Hover tooltip with date range, location, scope excerpt | P1 | New `Popup`; see §5.5. |
| F9 | Detail-card sync (bar click → scroll; scroll → bar highlight) | P0 | Already implemented. |
| F10 | Keyboard nav (Home/End/arrows/Ctrl+arrows) | P0 | Reuse. |
| F11 | Focus ring meets WCAG 2.2 SC 2.4.13 on the focused bar (not only the whole control) | P1 | Extend focus outline to decorate the selected bar only when `IsKeyboardFocusWithin` and a `SelectedTimeFrame` exists. |
| F12 | DPI/resize/viewport adaptation | P0 | Reuse. |
| F13 | Density toggle (compact / comfortable) | P2 | Switches `TimeFrameLaneHeight` 24 ↔ 36. |
| F14 | Flag-gated legacy path | P1 | Temporary `AppSettings.UseHorizontalTimelineV2 = true` (default). See §7. |
| F15 | AutomationPeer (name, role, children per bar) | P1 | Add `TimelineControlAutomationPeer` exposing each bar as `ListItem`. |
| F16 | Reduced-motion respect | P1 | Gate fit animation and hover storyboards behind `SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast`. |
| F17 | Mini time-cursor line following mouse X over lanes | P2 | Hairline `OnSurfaceDividerOnDarkBrush @ 0.18` under cursor during drag. |
| F18 | Bar secondary badge for "Present" roles | P1 | Tiny dot at bar right edge when `EndDate == Today`. |
| F19 | Persist selection/zoom in session (not cross-session) | Defer | Not in scope. |
| F20 | Virtualization of bars | Defer | Entry count stays < 50. |

## 4. Architecture and file ownership

### 4.1 Likely file touch list (planning, not edits)

- [Controls/TimelineControl.cs](Controls/TimelineControl.cs) — label uplift, localized Today, AutomationPeer, reduced-motion gating, optional two-line label.
- [Models/TimelineTimeFrameItem.cs](Models/TimelineTimeFrameItem.cs) — add optional `SubtitleText` (role text) with INPC.
- [ViewModels/Pages/ExperiencePageViewModel.cs](ViewModels/Pages/ExperiencePageViewModel.cs) — pass `pEntry.RoleText` into new `SubtitleText` when creating timeframes.
- [Pages/ExperiencePage.xaml](Pages/ExperiencePage.xaml) — dedicated row height for timeline; border/padding; attach new DP bindings.
- [Resources/Controls.xaml](Resources/Controls.xaml) — default style padding unchanged; add `FocusVisualStyle` token if needed.
- `Properties/Resources.resx` + `Resources.fr-CA.resx` — new key `TimelineLabelTodayMarker` (`Today` / `Aujourd'hui`).
- `Controls/Automation/TimelineControlAutomationPeer.cs` (new file) — AutomationPeer implementation.
- Tests in `ResumeApp.Tests/Controls/TimelineControlTests.cs` and `ResumeApp.Tests/ViewModels/ExperienceTimelineEntryViewModelTests.cs` — extend coverage for new subtitle, localized Today, focus-on-bar.

### 4.2 Boundaries

- **Rendering only inside `TimelineControl`.** XAML still hosts layout, never draws bars.
- **Data stays in VM.** Control reads `TimeFrames : ObservableCollection<TimelineTimeFrameItem>`. No extra data contracts.
- **Styling via DynamicResource** only. No hardcoded colors in `OnRender`.
- **Localization via `ResourcesService` at the VM layer**, never inside the control.

### 4.3 Rendering responsibilities (desktop)

1. Compute content rect, viewport bounds, zoom.
2. Draw background (`CommonBlackBrush`) + era bands.
3. Build visible frames, assign lanes (greedy, gap `10 px`).
4. Draw bars with opacity ladder + optional highlight gradient top.
5. Draw bar labels (two-line if fits; one-line otherwise).
6. Draw baseline hairline.
7. Draw ticks + tick labels using `ChooseTickLabelSchedule`.
8. Draw Today marker (dashed vertical + localized pill).
9. Draw selected-date pill when `SelectedDate` in viewport.
10. Draw focus outline on the selected bar when `IsKeyboardFocusWithin`.
11. Push hover tooltip as `Popup` (non-rendered-in-OnRender concern).

### 4.4 Data responsibilities

- `ExperiencePageViewModel.RebuildEntries` continues to build entries + parallel timeframes.
- New: pass `pEntry.RoleText` → `TimelineTimeFrameItem.SubtitleText` (optional, may be `null`).
- `TimelineMinDate` = min `StartDate` of entries (unchanged).
- Selection stays three-way: `SelectedDate` ↔ `SelectedTimeFrame` ↔ `SelectedTimelineEntry`.

## 5. Layout, lane, bar, axis, marker rules

### 5.1 Content rect

- Padding `16,12,16,16` (default style unchanged).
- Baseline Y = `content.Bottom - 34`.
- Tick label Y = `baseline + 10`.
- Lane area: top = `content.Top`, bottom = `baseline - 12`.

### 5.2 Lane-packing rules

- Sort visible frames by `StartX` asc, then `EndX` desc, then `Title` asc (deterministic).
- Greedy fit: first lane with `prevEndX + TimeFrameLaneGapPixels ≤ StartX`; allocate new lane otherwise.
- `TimeFrameLaneGapPixels = 10` (was 8). Keeps breathing room between sequential bars at current entry count.
- Lane height auto-compresses: `effective = min(TimeFrameLaneHeight, available / laneCount)`; clamp `≥ 12`.
- Density toggle switches `TimeFrameLaneHeight` between 28 (compact) and 36 (comfortable). Default **comfortable**.

### 5.3 Bar rendering rules

- Height = `min(28, max(2, laneHeight - 6))`. Bumped from 22 so the two-line label fits comfortably at comfortable density.
- Corner radius = `min(10, height/2)`.
- Accent brush from `AccentColorKey` via `TryFindResource`. Fallback: `CommonBrush`.
- Opacity: default **0.60**, hover **0.85**, selected **1.00**.
- Top-half inner highlight (`foreground @ 0.08`) only when bar ≥ 10 px tall and ≥ 8 px wide. Kept.
- Selected: 1.5 px `CommonWhiteBrush @ 0.6` stroke + `accent @ 0.25` glow halo 2 px outside.
- Hover (non-selected): 1.0 px `foreground @ 0.2` stroke.
- "Present" badge: 4-px circle at right edge, same accent, when `EndDate >= Today.AddDays(-1)`.

### 5.4 Bar label rules

- Typeface = `CommonFontFamily`. Sizes: role 12 px, company 10 px.
- Two-line when `barWidth ≥ 64` AND `barHeight ≥ 24`:
  - Line 1: `Role` (semi-bold, 100 %).
  - Line 2: `Company` (regular, 70 %).
- One-line fallback shows `Company` (current behavior) for narrow/short bars.
- Leading dot unchanged (accent color, radius ~`height*0.22`).
- Hard clip to bar rounded rect (`PushClip`). No overflow.
- Title placement uses existing 6-iteration shift to avoid in-lane overlap.

### 5.5 Hover tooltip (Popup)

- Trigger: `mHoveredTimeFrameItem` non-null for > 200 ms.
- Anchor: above bar, left-aligned to bar `StartX`.
- Content: role, company, `DateRangeText`, location, short scope excerpt (first 140 chars, ellipsis).
- Not inside `OnRender`; a `Popup` element maintained in the control code-behind.
- Disappears on `MouseLeave` or `SelectedTimeFrame` change.

### 5.6 Axis rules

- Ticks by `ChooseTickLabelSchedule` — unchanged, 14 candidate steps.
- Format: `yyyy` (year), `MMM yyyy` (month), `MMM d` (day), under `CultureInfo.CurrentCulture`.
- Major tick 8 px, minor 4 px. Minor brush opacity 0.35.
- Label minimum spacing 20 px (unchanged).
- Natural alignment preserved (Mondays, month 1sts).

### 5.7 Today / Présent marker

- Dashed vertical (`accent @ 0.7`, dash `4,3`) from `laneTop` to `baseline`.
- Pill above line at `laneTop`: rounded, padding 6×3, `CommonBlackBrush @ 0.85` bg, 1 px accent stroke.
- Pill text: `ResourcesService["TimelineLabelTodayMarker"]` — new resx key. English `Today`, French `Aujourd'hui`.
- Fallback if resource missing: `"Today"`.
- Shown only when `Today` is within viewport.

### 5.8 Selected-date pill

- Format `MMM d, yyyy` under current culture (unchanged).
- Anchor: just above axis, centered at selected X.
- Add a 3×6 chevron pointing down to the tick.

### 5.9 Empty / fallback

- `TimeFrames` null or empty → render background + axis + era bands only, with `EffectiveMinDate = Today.AddYears(-1)` (unchanged).
- No "empty" illustration in desktop for this scope (entries are guaranteed from resx).

### 5.10 DPI, monitor, resize

- Rendering already uses `VisualTreeHelper.GetDpi` per paint + `SnapsToDevicePixels` + `UseLayoutRounding`.
- Monitor change triggers `OnDpiChanged` → invalidate FormattedText cache (existing).
- Window resize → `InvalidateVisual` + re-fit if the current view would show zero days.
- Supported DPI range: 96–240 (100 %–250 % scaling).
- Minimum usable width: 560 CSS px. Below this, two-line labels collapse to one-line; tooltips disabled.

### 5.11 Keyboard and accessibility

- `Home` → `EffectiveMinDate`; `End` → `Today` (unchanged).
- `Up`/`Down` → previous/next bar by chronological start (unchanged).
- `Left`/`Right` → pan by step (Day default, `Ctrl` promotes) — unchanged.
- `Enter`/`Space` → confirm selection (commits `SelectedTimeFrame` through the existing TwoWay binding).
- `Esc` → clear hover tooltip, collapse selection visually.
- `AutomationPeer` exposes each visible bar as a `ListItem` with name = `$"{Role}, {Company}, {DateRangeText}"` from the backing entry VM.
- Focus ring meets WCAG 2.2 SC 2.4.13 (≥ 2 CSS px perimeter, ≥ 3:1 contrast). Uses inner glow + outer stroke already present, extended to decorate the selected bar when keyboard-focused.

### 5.12 Reduced motion

- Guard animations by `SystemParameters.ClientAreaAnimation`.
- If off: no fit ease (snap to target), no hover storyboards (see `ExperiencePage.xaml:66–110`), no inertia (zero out velocity at release).
- Dashed Today line does **not** animate regardless.

### 5.13 Color strategy (desktop)

- Base surface: `CommonBlackBrush`.
- Accents: existing 8-brush cycle from `ColorHelper.sAccentBrushKeys`.
- Hairlines/divider: `OnSurfaceDividerOnDarkBrush`, `HairlineTwoToneBrush`.
- Foreground text: `CommonWhiteBrush`.
- Palette cycle determinism verified by `AssignLanes` sort key (existing).
- High-contrast fallback: if `SystemParameters.HighContrast`, bars drop opacity ladder (all render at 1.0) and strokes bump to 2 px `SystemColors.WindowTextBrush`.

## 6. Localization deltas

| Key (new) | en-CA | fr-CA |
|---|---|---|
| `TimelineLabelTodayMarker` | `Today` | `Aujourd'hui` |

`LabelPresent` (`Present` / `Présent`) already exists and is reused by `DateRangeText`. No change to existing keys.

## 7. Rollout and flag strategy

- Add `AppSettings.UseHorizontalTimelineV2` (boolean, default `true`).
- Implementation gates the new label layout, tooltip popup, localized Today, focus-on-bar, AutomationPeer, and reduced-motion guard.
- Set `false` to revert to pre-uplift behavior for a single release cycle. Flag is ephemeral: removed once the v2 path ships with tests green for two consecutive builds.

## 8. Testing and validation strategy

Full validation plan lives in [desktop-horizontal-timeline-validation-plan.md](desktop-horizontal-timeline-validation-plan.md). Summary here:

- Unit tests (xUnit) for `TimelineTimeFrameItem.SubtitleText` INPC, for `TimelineControlAutomationPeer`, and for the resource-key fallback when `TimelineLabelTodayMarker` is missing.
- Visual parity: capture screenshots at `en-CA` / `fr-CA` × `compact` / `comfortable` × `100 %` / `150 %` DPI and diff against reference PNGs under `docs/screenshots/app-timeline/horizontal-v2/`.
- Regression: existing timeline tests must pass (DP registration, round-trip, scroll-sync behavior).

## 9. Regression risks and must-not-break

### Regression risks

1. `TimelineTimeFrameItem` shape change (added `SubtitleText`) breaks test fixtures — migration is additive, default empty.
2. Localized Today resource lookup fails in tests lacking the resource provider → mitigated by fallback to hardcoded `"Today"`.
3. Two-line label + corner-radius clip at very small bar heights produces text clipping — gated by `barHeight ≥ 24` guard.
4. New tooltip `Popup` steals hover focus from detail-card list — mitigated by `StaysOpen=False`, `AllowsTransparency=True`, and release on `MouseLeave`.
5. Selection glow growing bar bounds by 4 px may overlap neighboring lanes — mitigated by lane-top offset centering and clamp.
6. AutomationPeer enumeration during paint can race with `BuildVisibleTimeFrames` — peer reads the **last painted** snapshot, never touches live layout.

### Must-not-break list

- `TimelineControl` dependency property registrations (all 6) stay identical.
- `OnRender` order of operations unchanged (background → era → bars → labels → baseline → ticks → today → selected → focus outline).
- Existing pan/zoom/inertia/fit/keyboard behavior unchanged in coordinates and easing.
- `ExperienceTimelineScrollSyncBehavior` interface (attached DPs + reflection fallback) unchanged.
- `AssignLanes` sort-key determinism unchanged.
- 8-accent palette order unchanged.
- `DateRangeText` format unchanged.
- All existing xUnit tests continue to pass.

## 10. Phased implementation plan

| Phase | Goal | Exit criteria |
|---|---|---|
| P0 | Localize Today marker, add resx key, wire `SubtitleText` DP on item model, pass RoleText from VM | Existing tests pass; new resx key round-trips `en-CA`/`fr-CA`; `SubtitleText` INPC covered. |
| P1 | Two-line label rendering with the existing LayoutTimeFrameTitles path | Screenshots at 100 % DPI show role on top + company on bottom for wide bars; narrow bars fall back to one-line. |
| P2 | AutomationPeer + focus-on-bar outline | UIA tree (inspect.exe) lists each bar as ListItem with expected `Name`; focus ring moves with `Up`/`Down`. |
| P3 | Hover tooltip Popup | Tooltip appears after 200 ms hover; disappears on leave; content matches entry VM. |
| P4 | Reduced-motion guard + density toggle | With animations off, no fit/hover anim observed; density toggle swaps lane height. |
| P5 | Visual parity sweep + regression screenshots + flag sunset | All reference screenshots match; `UseHorizontalTimelineV2` can be removed. |

Each phase ships behind `UseHorizontalTimelineV2` until P5.

## 11. Research conclusions that shaped this spec

### Lane packing for time-based visualizations

- Greedy first-fit with a minimum X-gap is the standard in charting libs (vis.js Timeline, d3-timeline) and is already correct here. Interval-tree approaches add complexity but no visible benefit under ~50 frames.

### Dense label placement on horizontal bars

- Two-line in-bar labels (role + company) win at typical densities over external labels when bar widths average ≥ 80 px. Truncate with `…` and always provide a tooltip or detail peek for the full text.
- Leading-dot accent + label is a common convention in Linear, GitHub Projects, and Notion timelines.

### Dark-theme hierarchy

- Opacity ladder 0.60 / 0.85 / 1.00 on accent fills provides clear three-state hierarchy on near-black surfaces without needing separate hue shifts.
- Maintain ≥ 4.5:1 contrast for in-bar text against the 0.60-opacity fill. The current 8-accent palette combined with white text meets this; the audit already validated the resource keys.

### Today marker

- Dashed vertical + labeled pill is the established "now" convention across Jira, GitHub Projects, Asana timeline. Localize the label — do not ship English-only.

### Accessibility for a desktop composite timeline

- WPF AutomationPeer must expose each bar as a named item so screen readers can enumerate. The audit correctly flagged absence as a gap — this spec fixes it.
- WCAG 2.2 SC 2.4.13 applies even inside a custom `Control`; the focus ring must decorate the *focused element* (here: the selected bar) not just the canvas.

### Performance

- `OnRender`-driven custom control stays appropriate at this entry count. Virtualization not needed; `FormattedText` LRU cache already handles label churn.

### Token-efficient prompting for multi-phase work

- Three-doc harness (spec / parity / validation) per feature beats a single long prompt. Each phase of §10 references §5 and §8 by anchor; no re-paste.
- Caveman-compressed status updates during implementation cut turn cost without losing technical content.

## 12. Deliverables

- This document: `docs/timeline/desktop-horizontal-timeline-spec.md`.
- Parity matrix: `docs/timeline/desktop-horizontal-timeline-parity-matrix.md`.
- Validation plan: `docs/timeline/desktop-horizontal-timeline-validation-plan.md`.
- Commit and push on branch `docs/timeline-horizontal-spec`.

## 13. Change log

- 2026-04-16 — Initial spec on branch `docs/timeline-horizontal-spec`.
