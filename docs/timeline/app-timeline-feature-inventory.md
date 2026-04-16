# App Timeline — Feature Inventory

> Companion to [app-timeline-audit.md](./app-timeline-audit.md). This file lists every observable feature of the WPF timeline with a file-and-line citation for each, so the web port has a concrete behavioral spec.

## Reading guide

- Every row has a **file:line** citation. Paths are relative to the repo root `I:\Dropbox\Projet VS\ResumeApp`.
- Features are grouped by concern. Tests that exercise a feature are cross-referenced where useful.
- Behavior priority (Core / Secondary / Desktop-only) is applied in the [app-to-web-parity-matrix](./app-to-web-parity-matrix.md), not here.

---

## 1. Structural

| # | Feature | Evidence |
|---|---|---|
| 1.1 | `TimelineControl` is a custom `Control` with immediate-mode rendering in `OnRender` | [Controls/TimelineControl.cs:889–921](../../Controls/TimelineControl.cs) |
| 1.2 | Default style sets `SnapsToDevicePixels`, `UseLayoutRounding`, `Padding=16,12,16,16`, transparent background, white foreground | [Resources/Controls.xaml:12–17](../../Resources/Controls.xaml) |
| 1.3 | `Focusable = true` | [Controls/TimelineControl.cs:351](../../Controls/TimelineControl.cs) |
| 1.4 | `ClipToBounds = true` | [Controls/TimelineControl.cs:352](../../Controls/TimelineControl.cs) |
| 1.5 | Six dependency properties registered as public static fields; test ensures they exist | [Controls/TimelineControl.cs:182–244](../../Controls/TimelineControl.cs); [ResumeApp.Tests/Controls/TimelineControlTests.cs](../../ResumeApp.Tests/Controls/TimelineControlTests.cs) |

## 2. Data model

| # | Feature | Evidence |
|---|---|---|
| 2.1 | `TimelineTimeFrameItem { StartDate, EndDate, Title, AccentColorKey }` with INPC | [Models/TimelineTimeFrameItem.cs](../../Models/TimelineTimeFrameItem.cs) |
| 2.2 | `StartDate`/`EndDate` stripped to `.Date` on set | [Models/TimelineTimeFrameItem.cs:14,43](../../Models/TimelineTimeFrameItem.cs) |
| 2.3 | Reversed dates auto-swap in constructor | [Models/TimelineTimeFrameItem.cs:43–45](../../Models/TimelineTimeFrameItem.cs) |
| 2.4 | Null title defaults to empty string | Tested in [ResumeApp.Tests/Models/TimelineTimeFrameItemTests.cs](../../ResumeApp.Tests/Models/TimelineTimeFrameItemTests.cs) |
| 2.5 | `ExperienceTimelineEntryViewModel.DateRangeText` formats `"yyyy-MM" + Separator + ("yyyy-MM" | LabelPresent)` | [ViewModels/Pages/ExperienceTimelineEntryViewModel.cs:150–159](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs) |
| 2.6 | `TechItems` splits `TechText` on `/`, `•`, `·`, `|`, `;`, dedupes case-insensitive | [ViewModels/Pages/ExperienceTimelineEntryViewModel.cs:107–122](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs) |
| 2.7 | `PaletteIndex` cycles through 8 brush keys (`ColorHelper.sAccentBrushKeys`) | `Helpers/ColorHelper.cs:12–22` |
| 2.8 | `MarkerGlyph` cycles through 4 resource-backed glyphs by `LaneIndex % 4` | [ViewModels/Pages/ExperienceTimelineEntryViewModel.cs:172–182](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs) |
| 2.9 | `LaneLeftMargin` = `LaneIndex × LargeThicknessValue` (via resource) | [ViewModels/Pages/ExperienceTimelineEntryViewModel.cs:162–169](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs) |
| 2.10 | `IsSelected` INPC-backed, defaults false | Tested in [ResumeApp.Tests/ViewModels/ExperienceTimelineEntryViewModelTests.cs](../../ResumeApp.Tests/ViewModels/ExperienceTimelineEntryViewModelTests.cs) |
| 2.11 | Entry list built from `.resx` keys (5 entries hardcoded) | [ViewModels/Pages/ExperiencePageViewModel.cs:310–423](../../ViewModels/Pages/ExperiencePageViewModel.cs) |
| 2.12 | Lane assignment sorts by `StartDate` then greedy-fits first lane with gap | [ViewModels/Pages/ExperiencePageViewModel.cs:124–164](../../ViewModels/Pages/ExperiencePageViewModel.cs) (entry lanes); [Controls/TimelineControl.cs:397–409](../../Controls/TimelineControl.cs) (bar lanes) |

## 3. Rendering

| # | Feature | Evidence |
|---|---|---|
| 3.1 | Background fill via `CommonBlackBrush` | [Controls/TimelineControl.cs:1693,918](../../Controls/TimelineControl.cs) |
| 3.2 | Alternating even-year era bands | [Controls/TimelineControl.cs:1903–1944](../../Controls/TimelineControl.cs) |
| 3.3 | Rounded-rect time-frame bar with `CornerRadius = min(10, height/2)` | [Controls/TimelineControl.cs:1758](../../Controls/TimelineControl.cs) |
| 3.4 | Bar height `min(22, max(2, laneHeight − 6))` | [Controls/TimelineControl.cs:1742](../../Controls/TimelineControl.cs) |
| 3.5 | Default bar opacity 65%, hover 85%, selected 100% | [Controls/TimelineControl.cs:1765–1799](../../Controls/TimelineControl.cs) |
| 3.6 | Selected bar: white stroke 1.5 px + 1.5–2 px glow | [Controls/TimelineControl.cs:1787–1799](../../Controls/TimelineControl.cs) |
| 3.7 | Bar title rendered clipped inside the bar with a leading dot | [Controls/TimelineControl.cs:1824–1863](../../Controls/TimelineControl.cs) |
| 3.8 | Title placement tries 6 shift iterations to avoid overlaps within a lane | [Controls/TimelineControl.cs:836–876, 1236–1297](../../Controls/TimelineControl.cs) |
| 3.9 | Baseline hairline uses `HairlineTwoToneBrush`, dashed | [Controls/TimelineControl.cs:1868, 1706](../../Controls/TimelineControl.cs) |
| 3.10 | Major ticks 8 px, minor ticks 4 px | [Controls/TimelineControl.cs:2103–2134](../../Controls/TimelineControl.cs) |
| 3.11 | Tick label formats: `yyyy` (year), `MMM yyyy` (month), `MMM d` (day) | [Controls/TimelineControl.cs:417–425](../../Controls/TimelineControl.cs) |
| 3.12 | Today marker: dashed vertical line + "Today" pill (hardcoded, **not localized**) | [Controls/TimelineControl.cs:1946–1988, 1979](../../Controls/TimelineControl.cs) |
| 3.13 | Selected-date pill format `MMM d, yyyy` under `CurrentCulture` | [Controls/TimelineControl.cs:2138–2196, 2166](../../Controls/TimelineControl.cs) |
| 3.14 | Focus outline: inner 4 px glow 12% opacity + outer 1.5 px stroke when `IsKeyboardFocusWithin` | [Controls/TimelineControl.cs:1990–2013](../../Controls/TimelineControl.cs) |

## 4. Time frame layout

| # | Feature | Evidence |
|---|---|---|
| 4.1 | Visible frames clipped to viewport before lane assignment | [Controls/TimelineControl.cs:2406–2463](../../Controls/TimelineControl.cs) |
| 4.2 | Sort order: `StartX` asc → `EndX` desc → `Title` asc | [Controls/TimelineControl.cs:2438–2440](../../Controls/TimelineControl.cs) |
| 4.3 | Lane gap constant `TimeFrameLaneGapPixels = 8` | [Controls/TimelineControl.cs:402](../../Controls/TimelineControl.cs) |
| 4.4 | Lane area fits between top padding and `baseline − 12 px` | [Controls/TimelineControl.cs:1723–1725](../../Controls/TimelineControl.cs) |
| 4.5 | Lane height auto-scales when too many lanes | [Controls/TimelineControl.cs:1736–1739](../../Controls/TimelineControl.cs) |
| 4.6 | Baseline Y at `bottom − 34 px` | [Controls/TimelineControl.cs:1720](../../Controls/TimelineControl.cs) |

## 5. Tick scheduling

| # | Feature | Evidence |
|---|---|---|
| 5.1 | 14 granularity/step combos tried (days 1/2/5/7/14, weeks 1/2/4, months 1/2/3/6, years 1/2/5/10) | [Controls/TimelineControl.cs:2015–2081](../../Controls/TimelineControl.cs) |
| 5.2 | First non-overlapping schedule wins (≥ `MajorTickLabelGapPixels = 20`) | [Controls/TimelineControl.cs:168,2047](../../Controls/TimelineControl.cs) |
| 5.3 | Natural alignment enforced (Mondays for weeks, 1st for months) | [Controls/TimelineControl.cs:654–823](../../Controls/TimelineControl.cs) |

## 6. Zoom, pan, viewport

| # | Feature | Evidence |
|---|---|---|
| 6.1 | Zoom range `[0.08, 48.0]` px/day | [Controls/TimelineControl.cs:615–627](../../Controls/TimelineControl.cs) |
| 6.2 | Wheel zoom factor `1.12` per notch, pointer-anchored | [Controls/TimelineControl.cs:154,923–961](../../Controls/TimelineControl.cs) |
| 6.3 | Fit animation 240 ms, `EaseOutCubic`, abortable on user interaction | [Controls/TimelineControl.cs:1597–1623, 172, 370–375, 514, 972](../../Controls/TimelineControl.cs) |
| 6.4 | Pan inertia: up to 6 samples in 120 ms ring buffer, friction `0.12^deltaSeconds`, stops below `0.02 days/sec` | [Controls/TimelineControl.cs:1625–1689](../../Controls/TimelineControl.cs) |
| 6.5 | Drag threshold 3 px, click window 320 ms | [Controls/TimelineControl.cs:170, 1035–1092](../../Controls/TimelineControl.cs) |
| 6.6 | `ViewportStartTicks` clamped to content bounds | [Controls/TimelineControl.cs:642](../../Controls/TimelineControl.cs) |
| 6.7 | `SelectedDate` clamped to `[EffectiveMinDate, Today]` | [Controls/TimelineControl.cs:501](../../Controls/TimelineControl.cs); tested in `ExperiencePageViewModelTests.cs` |

## 7. Interaction

### 7.1 Mouse

| # | Feature | Evidence |
|---|---|---|
| 7.1.1 | Wheel zoom around cursor | [Controls/TimelineControl.cs:923–961](../../Controls/TimelineControl.cs) |
| 7.1.2 | Left-drag pans viewport | [Controls/TimelineControl.cs:963–1033](../../Controls/TimelineControl.cs) |
| 7.1.3 | Click without drag selects a frame or a date | [Controls/TimelineControl.cs:1035–1092](../../Controls/TimelineControl.cs) |
| 7.1.4 | `Hand` cursor over hittable regions | [Controls/TimelineControl.cs:1355–1371](../../Controls/TimelineControl.cs) |
| 7.1.5 | Hover state tracked via `mHoveredTimeFrameItem`, clears on `MouseLeave` | [Controls/TimelineControl.cs:1094–1104](../../Controls/TimelineControl.cs) |

### 7.2 Keyboard

| # | Feature | Evidence |
|---|---|---|
| 7.2.1 | `Home` → `EffectiveMinDate` | [Controls/TimelineControl.cs:1120–1125](../../Controls/TimelineControl.cs) |
| 7.2.2 | `End` → `Today` | [Controls/TimelineControl.cs:1127–1131](../../Controls/TimelineControl.cs) |
| 7.2.3 | `Up`/`Down` → prev/next time frame | [Controls/TimelineControl.cs:1136–1140, 2284–2325](../../Controls/TimelineControl.cs) |
| 7.2.4 | `Left`/`Right` → pan one step; `Ctrl` promotes Day→Week→Month→Year→Decade | [Controls/TimelineControl.cs:1143–1155, 427–450](../../Controls/TimelineControl.cs) |
| 7.2.5 | Focus outline painted when keyboard-focused | [Controls/TimelineControl.cs:1990–2013](../../Controls/TimelineControl.cs) |

### 7.3 Hit testing

| # | Feature | Evidence |
|---|---|---|
| 7.3.1 | Smallest overlapping bar wins to keep nested frames reachable | [Controls/TimelineControl.cs:2378–2403, 2396–2398](../../Controls/TimelineControl.cs) |

## 8. Scroll sync

| # | Feature | Evidence |
|---|---|---|
| 8.1 | Attached behavior with `IsEnabled`, `ItemsControl`, `SelectedDate` (TwoWay), `SelectedItem` (TwoWay) | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs:73–150](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) |
| 8.2 | Scroll → selection uses debounced timer (90 ms) | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs:213–239, 269–282](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) |
| 8.3 | Picks item nearest viewport top (8 px padding) | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs:284–308, 361–407](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) |
| 8.4 | Selection → scroll animates vertical offset over 220 ms | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs:106–126, 424–439](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) |
| 8.5 | `FindTargetItemForDate`: nearest `StartDate ≤ target` or fallback to first | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs:324–359](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) |
| 8.6 | `StartDate` extracted via reflection (ConditionalWeakTable cache) | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs:480–511](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) |
| 8.7 | Re-entrancy guarded by `IsUpdatingFromScroll` / `IsApplyingSelection` | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs:108–134, 297–307](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) |
| 8.8 | All `TransformToAncestor` exceptions swallowed (layout safety) | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs:410–421, 466–475](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) |

## 9. Animations

| # | Feature | Evidence |
|---|---|---|
| 9.1 | Hover scale storyboard on detail cards | [Pages/ExperiencePage.xaml:66–110](../../Pages/ExperiencePage.xaml) |
| 9.2 | Fit animation 240 ms `EaseOutCubic` | [Controls/TimelineControl.cs:1597–1623](../../Controls/TimelineControl.cs) |
| 9.3 | Scroll sync animation 220 ms | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs:438](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) |
| 9.4 | No `prefers-reduced-motion` equivalent anywhere | Absence verified across the above files |

## 10. Accessibility

| # | Feature | Evidence |
|---|---|---|
| 10.1 | No `AutomationPeer` override | Absence in [Controls/TimelineControl.cs](../../Controls/TimelineControl.cs) |
| 10.2 | No `AutomationProperties.Name`/`HelpText` applied to the control | Absence in [Pages/ExperiencePage.xaml](../../Pages/ExperiencePage.xaml) |
| 10.3 | Keyboard focus indicator present (see 7.2.5) | [Controls/TimelineControl.cs:1990–2013](../../Controls/TimelineControl.cs) |
| 10.4 | Interactions help text is localized (`TimelineControlInteractionsHelpText`) | `Properties/Resources.resx` + `Resources.fr-CA.resx` |

## 11. Localization

| # | Feature | Evidence |
|---|---|---|
| 11.1 | All user strings live in `.resx` except `"Today"` label in `DrawTodayMarker` | [Controls/TimelineControl.cs:1979](../../Controls/TimelineControl.cs) |
| 11.2 | Dates formatted with `CultureInfo.CurrentCulture` | [Controls/TimelineControl.cs:417–425](../../Controls/TimelineControl.cs); [ViewModels/Pages/ExperienceTimelineEntryViewModel.cs:150–159](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs) |
| 11.3 | `Present` label keyed as `LabelPresent` | Used at [ViewModels/Pages/ExperienceTimelineEntryViewModel.cs:157](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs) |
| 11.4 | Date range separator keyed as `ExperienceDateRangeSeparator` | Used at [ViewModels/Pages/ExperienceTimelineEntryViewModel.cs:153](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs) |
| 11.5 | Glyph shapes keyed as `GlyphMarkerCircle/Square/Diamond/Triangle` | [ViewModels/Pages/ExperienceTimelineEntryViewModel.cs:176–182](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs) |

## 12. Performance

| # | Feature | Evidence |
|---|---|---|
| 12.1 | `CompositionTarget.Rendering` subscribed only while animating/dragging | [Controls/TimelineControl.cs:1506–1571](../../Controls/TimelineControl.cs) |
| 12.2 | FormattedText LRU cache, max 256 entries, keyed by `(text, fontSize)` | [Controls/TimelineControl.cs:180, 1394–1438](../../Controls/TimelineControl.cs) |
| 12.3 | Cache invalidated on culture/brush/dpi/font change | [Controls/TimelineControl.cs:1412–1438](../../Controls/TimelineControl.cs) |
| 12.4 | No virtualization (renders all visible frames each paint) | Absence of `IsVirtualizing` / item container generator in rendering code |

## 13. Persistence / deep-linking

| # | Feature | Evidence |
|---|---|---|
| 13.1 | No saved state, no URL routing, no settings file | Absence across all files |
| 13.2 | `SelectedDate` / `SelectedTimeFrame` bindings are TwoWay + `UpdateSourceTrigger=PropertyChanged` | [Pages/ExperiencePage.xaml:150–151](../../Pages/ExperiencePage.xaml) |

## 14. Test coverage cross-reference

| Test file | Behaviors exercised |
|---|---|
| [TimelineControlTests.cs](../../ResumeApp.Tests/Controls/TimelineControlTests.cs) | DP registration, defaults, round-trips, instantiation with sample data |
| [ExperienceTimelineEntryViewModelTests.cs](../../ResumeApp.Tests/ViewModels/ExperienceTimelineEntryViewModelTests.cs) | Tech split/dedupe, lane/palette/marker cycling, date range formatting, INPC |
| [TimelineTimeFrameItemTests.cs](../../ResumeApp.Tests/Models/TimelineTimeFrameItemTests.cs) | Constructor swap, .Date stripping, INPC, idempotent sets |
| [ExperiencePageViewModelTests.cs](../../ResumeApp.Tests/ViewModels/ExperiencePageViewModelTests.cs) | Entry rebuild, selection cascade, clamping, localization refresh, commands |
| [ExperienceTimelineScrollSyncBehaviorTests.cs](../../ResumeApp.Tests/Behaviors/ExperienceTimelineScrollSyncBehaviorTests.cs) | Attached DPs, attach/detach safety, re-entrancy guards |

## 15. Things that are absent (confirmed by search)

- No `TODO`, `FIXME`, `HACK`, `XXX`, or `BUG` comments in any timeline file.
- No `prefers-reduced-motion` / `SystemParameters.ClientAreaAnimation` check.
- No `AutomationPeer`/`AutomationProperties.Name` for the timeline.
- No screen-reader announcement on state change.
- No deep-link / persistence.
- No virtualization of time frames.
- No data file feeding the entries (built in code from `.resx`).
