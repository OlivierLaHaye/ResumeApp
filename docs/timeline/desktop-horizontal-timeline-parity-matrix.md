# Desktop Horizontal Timeline — Parity Matrix

> Companion to [desktop-horizontal-timeline-spec.md](./desktop-horizontal-timeline-spec.md). Rows compare the **target** horizontal lane timeline (user-provided reference) against the **current** WPF `TimelineControl` and state the parity decision for the desktop build.

## Reading guide

| Column | Meaning |
|---|---|
| **Target feature** | What the intended horizontal timeline must do. |
| **Current state** | What `Controls/TimelineControl.cs` / `Pages/ExperiencePage.xaml` / related VMs do today, with file:line evidence. |
| **Parity decision** | `Keep` / `Extend` / `Replace` / `Add` / `Defer`. |
| **Priority** | `P0` (ship-blocker), `P1` (core feel), `P2` (polish). |
| **Risk** | Where parity is likely to drift. |

---

## 1. Surface and structure

| # | Target feature | Current state | Parity decision | Priority | Risk |
|---|---|---|---|---|---|
| 1.1 | Premium dark surface with alternating year bands | `CommonBlackBrush` background + even-year era bands @ 0.03 ([Controls/TimelineControl.cs:918, 1903–1944](../../Controls/TimelineControl.cs)) | Keep; drop era band opacity 0.03 → 0.025 | P1 | Too-faint bands defeat the purpose. Validate on 100 % and 150 % DPI. |
| 1.2 | Dedicated timeline region with room for ≥ 3 lanes | Timeline hosted in `Grid.Row="1"` at `Auto` height ([Pages/ExperiencePage.xaml:141–154](../../Pages/ExperiencePage.xaml)) | Extend: set `MinHeight=220, MaxHeight=360` on the container grid row | P0 | Small windows may clip; lane height auto-compressor already handles this. |
| 1.3 | Bottom month/year axis across full width | `DrawTicksAndLabels` at `lBaselineY = bottom - 34` ([Controls/TimelineControl.cs:1720, 1871](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 1.4 | Padding / breathing room inside timeline | Default style `Padding="16,12,16,16"` ([Resources/Controls.xaml:12–17](../../Resources/Controls.xaml)) | Keep | P0 | — |

## 2. Data model and bindings

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 2.1 | Bars carry role (primary) and company (secondary) labels | `TimelineTimeFrameItem { Title, AccentColorKey }` only ([Models/TimelineTimeFrameItem.cs](../../Models/TimelineTimeFrameItem.cs)) | Extend: add optional `SubtitleText` with INPC (additive) | P0 | Tests on `TimelineTimeFrameItem` must still pass; default empty string. |
| 2.2 | Bars know the accent color via palette cycle | `PaletteIndex` → `sAccentBrushKeys` cycle ([Helpers/ColorHelper.cs](../../Helpers/ColorHelper.cs); [ExperienceTimelineEntryViewModel.cs:172–182](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs)) | Keep | P0 | — |
| 2.3 | Viewmodel passes role into timeframe | `CreateTimeFrameForEntry` uses `CompanyText` as `Title` ([ExperiencePageViewModel.cs:101–111](../../ViewModels/Pages/ExperiencePageViewModel.cs)) | Extend: set `SubtitleText = RoleText` (keep `Title = CompanyText`) | P0 | Swap risk — spec §5.4 fixes role on line 1, company on line 2. Do not swap. |
| 2.4 | Three-way selection (date ↔ timeframe ↔ entry) | `SynchronizeSelectionFromSelectedDate` ([ExperiencePageViewModel.cs:247–275](../../ViewModels/Pages/ExperiencePageViewModel.cs)) | Keep | P0 | — |

## 3. Lane layout

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 3.1 | Multiple lanes stack vertically above axis | `lLaneCount = max(LaneIndex)+1` + compressor ([Controls/TimelineControl.cs:1732–1740](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 3.2 | Overlapping ranges get side-by-side lanes | `FindLaneIndex` greedy first-fit ([Controls/TimelineControl.cs:397–409](../../Controls/TimelineControl.cs)) | Keep; widen gap 8 → 10 px | P0 | Regression on very dense windows — validate at comfortable density. |
| 3.3 | Lanes auto-compress when many roles overlap | `lCompressedLaneHeight` clamp ([Controls/TimelineControl.cs:1736](../../Controls/TimelineControl.cs)) | Keep | P1 | — |
| 3.4 | Deterministic lane assignment across renders | Sort `StartX → EndX desc → Title` ([Controls/TimelineControl.cs:2438–2440](../../Controls/TimelineControl.cs)) | Keep | P0 | — |

## 4. Bars

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 4.1 | Rounded bars spanning date range | `DrawRoundedRect` + `min(10, h/2)` radius ([Controls/TimelineControl.cs:1758, 1774](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 4.2 | Three-state opacity (default / hover / selected) | 65 / 85 / 100 ([Controls/TimelineControl.cs:1765–1799](../../Controls/TimelineControl.cs)) | Extend: 60 / 85 / 100 (target is slightly more subtle at rest) | P1 | Contrast against dark bg — validate with white text. |
| 4.3 | Inner top-half highlight for depth | `lHighlightRect` white @ 0.08 ([Controls/TimelineControl.cs:1776–1784](../../Controls/TimelineControl.cs)) | Keep | P1 | — |
| 4.4 | Selected: outline + glow | 1.5 px white @ 0.6 stroke + accent @ 0.25 glow ([Controls/TimelineControl.cs:1788–1808](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 4.5 | Hover: subtle stroke | 1.0 px white @ 0.2 ([Controls/TimelineControl.cs:1810–1818](../../Controls/TimelineControl.cs)) | Keep | P1 | — |
| 4.6 | "Present" badge on ongoing roles | Absent | Add: 4-px accent dot at bar right when `EndDate >= Today.AddDays(-1)` | P1 | Confusion with selection dot — place at inner-right, not center. |
| 4.7 | Bar height scales with lane density | `min(22, max(2, laneHeight-6))` ([Controls/TimelineControl.cs:1742](../../Controls/TimelineControl.cs)) | Extend: cap `28` instead of `22` for 2-line labels | P0 | Labels clip if cap not raised at comfortable density. |

## 5. Labels

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 5.1 | Label inside the bar, clipped to its rounded rect | `PushClip` + `DrawText` ([Controls/TimelineControl.cs:1850–1858](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 5.2 | Two-line label when space allows — role primary, company secondary | Single-line `Title` only ([Controls/TimelineControl.cs:1824–1863](../../Controls/TimelineControl.cs)) | Extend: branch on `barWidth ≥ 64 && barHeight ≥ 24`; one-line fallback uses `Title` unchanged | P0 | Two-line at narrow widths becomes unreadable; guard widths carefully. |
| 5.3 | Leading accent dot before text | `DrawEllipse` at title rect left ([Controls/TimelineControl.cs:1843–1853](../../Controls/TimelineControl.cs)) | Keep | P1 | — |
| 5.4 | In-lane overlap avoidance shifts label | 6-iteration shift ([Controls/TimelineControl.cs:836–876, 1236–1297](../../Controls/TimelineControl.cs)) | Keep | P1 | — |

## 6. Axis

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 6.1 | Adaptive major/minor ticks | 14 candidate schedules ([Controls/TimelineControl.cs:2015–2081](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 6.2 | Month/year tick labels | Formats `yyyy`, `MMM yyyy`, `MMM d` ([Controls/TimelineControl.cs:417–425](../../Controls/TimelineControl.cs)) | Keep | P0 | `fr-CA` `MMM yyyy` → `janv. 2024`; visual-check against reference. |
| 6.3 | Minor ticks visually subdued | Not styled separately ([Controls/TimelineControl.cs:2103–2134](../../Controls/TimelineControl.cs)) | Extend: minor tick brush opacity 0.35 | P2 | — |
| 6.4 | Dashed baseline hairline across width | `HairlineTwoToneBrush` ([Controls/TimelineControl.cs:1865–1868](../../Controls/TimelineControl.cs)) | Keep | P1 | — |

## 7. Today / Présent marker

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 7.1 | Vertical dashed line at today across lanes | `DrawTodayMarker` dashed vertical ([Controls/TimelineControl.cs:1946–1988](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 7.2 | Labeled pill above line ("Today" / "Aujourd'hui") | Hardcoded `"Today"` ([Controls/TimelineControl.cs:1979](../../Controls/TimelineControl.cs)) | Replace: new resx key `TimelineLabelTodayMarker` with `en-CA`/`fr-CA` values | P0 | Missing resource — ship a `"Today"` fallback. |
| 7.3 | Hidden when Today is outside viewport | Bounds check before draw ([Controls/TimelineControl.cs:1946–1988](../../Controls/TimelineControl.cs)) | Keep | P0 | — |

## 8. Selected-date indicator

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 8.1 | Pill showing `MMM d, yyyy` | `DrawSelectedIndicator` ([Controls/TimelineControl.cs:2138–2196](../../Controls/TimelineControl.cs)) | Keep | P1 | — |
| 8.2 | Downward chevron to axis | Absent | Add: 3×6 triangle under pill | P2 | — |

## 9. Interaction

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 9.1 | Wheel zoom around cursor | `OnMouseWheel` + zoom anchor ([Controls/TimelineControl.cs:923–961](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 9.2 | Left-drag pan | Drag threshold 3 px ([Controls/TimelineControl.cs:963–1092](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 9.3 | Inertia after pan release | Ring buffer + friction ([Controls/TimelineControl.cs:1625–1689](../../Controls/TimelineControl.cs)) | Keep; gate on `ClientAreaAnimation` | P1 | Reduced-motion violations if not gated. |
| 9.4 | Hover tooltip with richer detail | Absent | Add: `Popup` (see spec §5.5) | P1 | Focus capture; set `StaysOpen=False`. |
| 9.5 | Bar click selects frame / date | `OnMouseLeftButtonUp` click branch ([Controls/TimelineControl.cs:1035–1092](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 9.6 | Keyboard nav Home/End/arrows | Existing ([Controls/TimelineControl.cs:1120–1155](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 9.7 | `Enter`/`Space` commits selection | Absent | Add: key handler that sets `SelectedTimeFrame` to focused candidate | P1 | Existing `Up`/`Down` already moves focus; Enter ratifies. |
| 9.8 | `Esc` clears transient state | Absent | Add: clear `mHoveredTimeFrameItem` + close tooltip | P2 | — |

## 10. Accessibility

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 10.1 | Each bar reachable by keyboard | `Up`/`Down` traversal exists | Keep | P0 | — |
| 10.2 | Each bar announced as a list item | No `AutomationPeer` ([Controls/TimelineControl.cs](../../Controls/TimelineControl.cs) — absent) | Add: `TimelineControlAutomationPeer` with `ListItem` children | P1 | Snapshot races — peer reads last-painted snapshot only. |
| 10.3 | Focus ring meets WCAG 2.2 SC 2.4.13 | Outline currently decorates the whole control ([Controls/TimelineControl.cs:1990–2013](../../Controls/TimelineControl.cs)) | Extend: decorate the selected bar when focused | P1 | — |
| 10.4 | Reduced-motion respected | Animations always on | Extend: guard by `SystemParameters.ClientAreaAnimation` | P1 | — |
| 10.5 | High-contrast mode fallback | No branch | Add: at 1.0 opacity + 2 px `SystemColors.WindowText` stroke | P2 | — |

## 11. Localization

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 11.1 | All visible strings localized | All except `"Today"` ([Controls/TimelineControl.cs:1979](../../Controls/TimelineControl.cs)) | Replace with resx key `TimelineLabelTodayMarker` | P0 | — |
| 11.2 | Dates under `CurrentCulture` | Already done ([Controls/TimelineControl.cs:417–425](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 11.3 | `Present` label in entry `DateRangeText` | Keyed `LabelPresent` ([ExperienceTimelineEntryViewModel.cs:157](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs)) | Keep | P0 | — |

## 12. Scroll sync with detail cards

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 12.1 | Bar click → detail card scrolls smoothly | `AnimateVerticalOffset` 220 ms ([ExperienceTimelineScrollSyncBehavior.cs:106–126, 438](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs)) | Keep | P0 | — |
| 12.2 | Scroll → bar highlights | Debounced 90 ms ([ExperienceTimelineScrollSyncBehavior.cs:213–239](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs)) | Keep | P0 | — |
| 12.3 | No runaway loops on programmatic scroll | Re-entrancy flags ([ExperienceTimelineScrollSyncBehavior.cs:108–134](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs)) | Keep | P0 | — |

## 13. Performance

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 13.1 | Smooth 60 fps pan/zoom | `CompositionTarget.Rendering` subscribed only while animating ([Controls/TimelineControl.cs:1506–1571](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 13.2 | Text layout cached | FormattedText LRU (max 256) ([Controls/TimelineControl.cs:180, 1394–1438](../../Controls/TimelineControl.cs)) | Keep | P1 | Two-line labels double the key space; confirm capacity. |
| 13.3 | DPI/culture/brush invalidation | Already handled ([Controls/TimelineControl.cs:1412–1438](../../Controls/TimelineControl.cs)) | Keep | P0 | — |

## 14. Responsiveness

| # | Target | Current | Decision | Priority | Risk |
|---|---|---|---|---|---|
| 14.1 | Resizes without jitter | `InvalidateVisual` via `AffectsRender` DPs | Keep | P0 | — |
| 14.2 | Supports 100–250 % DPI | `VisualTreeHelper.GetDpi` per paint ([Controls/TimelineControl.cs:1701](../../Controls/TimelineControl.cs)) | Keep | P0 | — |
| 14.3 | Density toggle compact/comfortable | Absent | Add: `TimelineControl.Density` DP (`Compact`/`Comfortable`), default `Comfortable` | P2 | — |

## 15. Desktop-only choices (intentional non-parity with web)

| # | Choice | Rationale |
|---|---|---|
| 15.1 | Immediate-mode `DrawingContext` rendering | Native WPF path; performant, no virtualization needed at current scale. |
| 15.2 | Wheel zoom + keyboard pan | Desktop convention. Web port drops these per web matrix. |
| 15.3 | `CompositionTarget.Rendering` loop | Necessary for inertia/fit animation frames. |
| 15.4 | FormattedText cache | Built-in WPF cost; web has native text metrics. |
| 15.5 | `Popup`-based tooltip | Desktop-native hover pattern; not mirrored on web. |

## 16. Open items resolved by evidence

| Question | Answer | Evidence |
|---|---|---|
| Does the current control render horizontal lanes today? | Yes. | [Controls/TimelineControl.cs:1723–1819](../../Controls/TimelineControl.cs) |
| Is "Today" localized? | No, hardcoded English. | [Controls/TimelineControl.cs:1979](../../Controls/TimelineControl.cs) |
| Does each bar have an AutomationPeer? | No. | Absence in [Controls/TimelineControl.cs](../../Controls/TimelineControl.cs) |
| Is there a reduced-motion guard? | No. | Absence across rendering + storyboards |
| Is there a hover tooltip for richer detail? | No, only hover opacity. | [Controls/TimelineControl.cs:1810–1818](../../Controls/TimelineControl.cs) |
| Does the bar label show role AND company? | No, only `CompanyText` via `Title`. | [ExperiencePageViewModel.cs:109](../../ViewModels/Pages/ExperiencePageViewModel.cs) |
| Are overlapping ranges packed into lanes? | Yes. | [Controls/TimelineControl.cs:397–409, 2406–2463](../../Controls/TimelineControl.cs) |

## 17. Summary

The current control is ~80 % of the target. The uplift is:

- role-over-company two-line label,
- hover tooltip for richer detail,
- localized Today marker,
- AutomationPeer,
- focus-on-bar outline,
- reduced-motion guard,
- density toggle,
- minor axis/era-band polish.

No wholesale rewrite, no new data source, no change to the scroll-sync contract.
