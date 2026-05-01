# App Timeline Audit

> Evidence-based audit of the WPF `TimelineControl` and the `ExperiencePage` it belongs to. Written to support a later port to the OlhPhotographieWebsite (sibling repo at `I:\Dropbox\Projet VS\OlhPhotographieWebsite`).

## Repository under audit

- **Path:** `I:\Dropbox\Projet VS\ResumeApp`
- **Branch at audit time:** `docs/timeline-parity-audit` (created from `main` at commit `fdf0ba2`)
- **Stack:** .NET 10, C# 14, WPF, xUnit
- **Entry point for the timeline:** `Pages/ExperiencePage.xaml` + `Pages/ExperiencePage.xaml.cs`

## File map

### Core timeline files

| Role | File | Size |
|------|------|------|
| Custom rendered control | [Controls/TimelineControl.cs](../../Controls/TimelineControl.cs) | 2538 lines |
| Model for one time frame | [Models/TimelineTimeFrameItem.cs](../../Models/TimelineTimeFrameItem.cs) | 54 lines |
| View model for one entry | [ViewModels/Pages/ExperienceTimelineEntryViewModel.cs](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs) | 185 lines |
| Page view model | [ViewModels/Pages/ExperiencePageViewModel.cs](../../ViewModels/Pages/ExperiencePageViewModel.cs) | 461 lines |
| XAML layout | [Pages/ExperiencePage.xaml](../../Pages/ExperiencePage.xaml) | 399 lines |
| Scroll-sync behavior | [Behaviors/ExperienceTimelineScrollSyncBehavior.cs](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) | 512 lines |
| Default style | [Resources/Controls.xaml](../../Resources/Controls.xaml) (timeline block at lines 12–17) | — |

### Tests (behavioral spec)

- [ResumeApp.Tests/Controls/TimelineControlTests.cs](../../ResumeApp.Tests/Controls/TimelineControlTests.cs)
- [ResumeApp.Tests/ViewModels/ExperiencePageViewModelTests.cs](../../ResumeApp.Tests/ViewModels/ExperiencePageViewModelTests.cs)
- [ResumeApp.Tests/ViewModels/ExperienceTimelineEntryViewModelTests.cs](../../ResumeApp.Tests/ViewModels/ExperienceTimelineEntryViewModelTests.cs)
- [ResumeApp.Tests/Models/TimelineTimeFrameItemTests.cs](../../ResumeApp.Tests/Models/TimelineTimeFrameItemTests.cs)
- [ResumeApp.Tests/Behaviors/ExperienceTimelineScrollSyncBehaviorTests.cs](../../ResumeApp.Tests/Behaviors/ExperienceTimelineScrollSyncBehaviorTests.cs)

### Related infrastructure

- Palette: `Helpers/ColorHelper.cs` — 8 accent brush keys cycled by `PaletteIndex`
- Converters: `Converters/ColorConverters.cs` — `PaletteIndexToBrushConverter` (lines 56–95)
- Localization: `Properties/Resources.resx` + `Properties/Resources.fr-CA.resx` — timeline-visible strings such as `LabelPresent`, `ExperienceDateRangeSeparator`, `GlyphMarkerCircle/Square/Diamond/Triangle`, `ExperienceCreaform*`, `TimelineControlInteractionsHelpText`

## Architecture in one screen

```
ExperiencePage.xaml
├─ TimelineControl (custom, all rendering via OnRender)
│    ├─ DPs: MinDate, SelectedDate (TwoWay), SelectedTimeFrame (TwoWay),
│    │       TimeFrames, ZoomLevel (TwoWay), ViewportStartTicks (TwoWay)
│    └─ Bound to ExperiencePageViewModel.ExperienceTimeFrames
│
└─ ScrollViewer + ItemsControl (detail cards)
     └─ ExperienceTimelineScrollSyncBehavior attached
          ├─ IsEnabled, ItemsControl, SelectedDate (TwoWay), SelectedItem (TwoWay)
          └─ Debounced (90 ms) ScrollChanged -> SelectedDate;
             SelectedDate -> AnimateVerticalOffset (220 ms)

Data flow:
ExperiencePageViewModel.RebuildEntries()
  -> 5 hardcoded entries built from .resx keys
  -> AssignLanes() sorts by StartDate, assigns LaneIndex/PaletteIndex/MarkerGlyph
  -> parallel ObservableCollection<TimelineTimeFrameItem> feeds the control
```

There is **no** backend, no serialization shape, no persistence. The entry list is constructed in code from resource strings (`ExperiencePageViewModel.cs:310–423`).

## What the control actually renders

All rendering happens in `OnRender(DrawingContext)` at `Controls/TimelineControl.cs:889–921`. The layered draw order is:

1. Background fill (`CommonBlackBrush`, line 918).
2. Alternating even-year "era bands" (`DrawYearEraBands`, line 1903–1944).
3. Time-frame bars per lane, with hover/selected/default states (lines 1746–1819).
4. Title dot + clipped label inside each bar (lines 1824–1863).
5. Baseline hairline (line 1868, uses `HairlineTwoToneBrush`).
6. Major ticks (8 px) + minor ticks (4 px) + formatted tick labels (lines 1871–1880, 2103–2134).
7. Today marker: dashed vertical line + "Today" pill if today is in viewport (lines 1946–1988).
8. Selected-date indicator pill showing `MMM d, yyyy` (lines 2138–2196).
9. Focus outline (inner glow + outer stroke) when `IsKeyboardFocusWithin` (lines 1990–2013).

Content rectangle is the control bounds minus padding; padding defaults to `16,12,16,16` via the default style (`Resources/Controls.xaml:12–17`).

## Dependency properties

| Property | Type | Default | Flags | Coerce | file:line |
|---|---|---|---|---|---|
| `MinDate` | `DateTime` | `DateTime.MinValue` | AffectsRender | `CoerceMinDate` (forces `.Date`) | 182–191, 472 |
| `SelectedDate` | `DateTime` | `DateTime.Today` | TwoWay + AffectsRender | Clamped to `[EffectiveMinDate, Today]` | 193–202, 501 |
| `SelectedTimeFrame` | `TimelineTimeFrameItem` | `null` | TwoWay + AffectsRender | — | 204–212 |
| `TimeFrames` | `ObservableCollection<TimelineTimeFrameItem>` | `null` | AffectsRender | — | 214–222 |
| `ZoomLevel` | `double` (pixels per day) | `2.0` | TwoWay + AffectsRender | Clamped to `[0.08, 48.0]` | 224–233, 615–627 |
| `ViewportStartTicks` | `double` (DateTime ticks as double) | `DateTime.Today.AddYears(-1).Ticks` | TwoWay + AffectsRender | Clamped to content bounds | 235–244, 642 |

## Data contract

### `TimelineTimeFrameItem` ([Models/TimelineTimeFrameItem.cs:8–54](../../Models/TimelineTimeFrameItem.cs))

```csharp
public class TimelineTimeFrameItem : INotifyPropertyChanged
{
    public DateTime StartDate { get; set; } // stripped to .Date
    public DateTime EndDate   { get; set; } // swapped with StartDate if reversed
    public string   Title     { get; set; } // drawn on the bar
    public string?  AccentColorKey { get; set; } // resource key, resolved via TryFindResource
}
```

### `ExperienceTimelineEntryViewModel` ([ViewModels/Pages/ExperienceTimelineEntryViewModel.cs:12–184](../../ViewModels/Pages/ExperienceTimelineEntryViewModel.cs))

Key fields used by the detail card and the timeline:

- `CompanyText`, `RoleText`, `LocationText`, `ScopeText`, `TechText` — localized strings
- `TechItems` — derived from `TechText`, split on `/`, `•`, `·`, `|`, `;` (lines 107–122), case-insensitive dedup
- `StartDate`, `EndDate?` — raw dates (end-null means ongoing)
- `Accomplishments` — `ObservableCollection<string>`
- `DateRangeText` — `"yyyy-MM" + Separator + ("yyyy-MM" | LabelPresent)` under `CultureInfo.CurrentCulture` (lines 150–159)
- `PaletteIndex` — cycles through 8 accent brushes (`ColorHelper.sAccentBrushKeys`)
- `LaneIndex`, `LaneLeftMargin` — used for the detail-card lane layout
- `MarkerGlyph` — one of 4 resources keyed `GlyphMarkerCircle/Square/Diamond/Triangle`, cycled by `LaneIndex % 4` (lines 172–182)
- `IsSelected` — INPC-backed selection flag

### `ExperiencePageViewModel` ([ViewModels/Pages/ExperiencePageViewModel.cs:15–461](../../ViewModels/Pages/ExperiencePageViewModel.cs))

- `TimelineEntries` — `ObservableCollection<ExperienceTimelineEntryViewModel>` (RebuildEntries at line 310)
- `ExperienceTimeFrames` — parallel `ObservableCollection<TimelineTimeFrameItem>`
- `TimelineMinDate` — min of all `StartDate` values (line 405)
- `SelectedDate`, `SelectedTimeFrame`, `SelectedTimelineEntry` — three-way synchronized (see `SynchronizeSelectionFromSelectedDate`, line 247–275)
- Commands: `SelectExperienceCommand`, `SelectDateCommand` (string or `DateTime`)

## Time-frame layout algorithm

### Lane assignment

`TimelineControl.FindLaneIndex` (lines 397–409) iterates an array of per-lane `EndX` and places the new frame in the first lane where there is at least `TimeFrameLaneGapPixels = 8` px of gap from the previous frame's end. New lane if none fit.

`BuildVisibleTimeFrames` (lines 2406–2463) first clips each frame to the viewport, then sorts:

1. `StartX` ascending
2. `EndX` descending (longer ranges first at same start)
3. `Title` ascending (deterministic tiebreak)

…then assigns lanes.

### Tick scheduling

`ChooseTickLabelSchedule` (lines 2015–2081) tries 14 combinations of granularity × step (days 1/2/5/7/14, weeks 1/2/4, months 1/2/3/6, years 1/2/5/10). The first schedule whose labels do not overlap within `MajorTickLabelGapPixels = 20` wins.

Tick dates are enumerated respecting natural alignment (Mondays for weeks, month boundaries for months) by `EnumerateMajorTicks`/`EnumerateMinorTicks` (lines 654–823).

### Date formatting

`FormatTickLabel` (lines 417–425) under `CultureInfo.CurrentCulture`:

- Year scale: `"yyyy"`
- Month scale: `"MMM yyyy"`
- Day scale: `"MMM d"`

Selected-date pill uses `"MMM d, yyyy"` (line 2166). "Today" pill text is hardcoded `"Today"` at line 1979 — this is **not** currently localized even though the `fr-CA` resources exist.

### Zoom and pan

- `PixelsPerDay = ZoomLevel`, clamped `[0.08, 48.0]` (lines 615–627).
- `DateToPixel((date - viewportStart).TotalDays * ZoomLevel)` (lines 2355–2360).
- `PixelToDate` is the inverse (lines 2346–2353).
- Wheel zoom keeps the date under the cursor fixed (`WheelZoomFactorPerNotch = 1.12`, line 154 / 923–961).
- Fit animation eases zoom + pan over 240 ms via `EaseOutCubic` (`Lerp` at line 368, main path 1597–1623). Aborted on user interaction (lines 514, 972).
- Pan inertia: ring buffer of up to 6 pan samples over 120 ms, velocity decays by `0.12^deltaSeconds`, stops below `0.02 days/sec` (lines 1625–1689).

## States

| State | Trigger | Visual effect | file:line |
|---|---|---|---|
| Default | render | 65% opacity bar, no stroke | 1771 |
| Hover | mouse over bar | 85% opacity + hover stroke when small | 1765–1818 |
| Selected | `SelectedTimeFrame` set | 100% opacity + 1.5 px white stroke + 1.5–2 px glow | 1761–1799 |
| Keyboard focused | `IsKeyboardFocusWithin` | inner 4 px glow + outer 1.5 px stroke | 1990–2013 |
| Today | date == `DateTime.Today` in viewport | dashed vertical line + "Today" pill | 1946–1988 |
| Fit animating | initial layout or user-triggered fit | eased zoom + pan, aborted on input | 1597–1623 |
| Inertia | after pan drag release | RAF-style velocity decay until threshold | 1625–1644 |
| Empty | `TimeFrames` null/empty | axis and era bands still render; `EffectiveMinDate = Today.AddYears(-1)` | 1538–1540 |

## Interaction surface

### Mouse

- `OnMouseWheel` (923–961) — zoom around pointer.
- `OnMouseLeftButtonDown`/`Move`/`Up` (963–1092) — click vs drag-pan decided by 3 px drag threshold and 320 ms click window.
- `OnMouseLeave` (1094–1104) — clears hover, invalidates.
- `UpdateCursorAtPosition` (1355–1371) — `Hand` cursor over clickable regions.

### Keyboard

- `Home` — jump to `EffectiveMinDate` (1120–1125)
- `End` — jump to `Today` (1127–1131)
- `Up`/`Down` — previous/next time frame (1136–1140 / 2284–2325)
- `Left`/`Right` — pan by keyboard step; `Ctrl` promotes step from Day → Week → Month → Year → Decade (1143–1155 / 427–450)
- `Focusable = true` (line 351)

### Hit-testing

`GetTimeFrameHitInfoAtPosition` (2378–2403) returns the *smallest* rect among overlapping hits so nested frames are reachable.

## Scroll-sync behavior

[Behaviors/ExperienceTimelineScrollSyncBehavior.cs](../../Behaviors/ExperienceTimelineScrollSyncBehavior.cs) is attached to the ScrollViewer that contains the detail ItemsControl.

- Debounced ScrollChanged (90 ms `DispatcherTimer`) finds the item closest to the scroll viewport top (8 px top padding) and sets both `SelectedItem` and `SelectedDate` — those propagate into `ExperiencePageViewModel` through TwoWay bindings.
- Changes to `SelectedDate` run `FindTargetItemForDate` (nearest start date ≤ target, fallback to first) and animate vertical offset over 220 ms through `ScrollViewerAnimatedOffsetBehavior.AnimateVerticalOffset`.
- Re-entrancy is guarded by `IsUpdatingFromScroll` / `IsApplyingSelection` flags on a per-state object.
- `StartDate` on arbitrary bound item types is read via a `ConditionalWeakTable` reflection cache (lines 480–511). If absent, the behavior silently skips sync (no crash).

## Styling, theming, localization

- All colors come from resource keys (`TryFindResource`): `CommonBlackBrush`, `CommonWhiteBrush`, `OnSurfaceDividerOnDarkBrush`, `HairlineTwoToneBrush`, `CommonBrush`, `CommonBlueStrongBrush`, `CommonGreenStrongBrush`, `CommonYellowStrongBrush`, `CommonRedStrongBrush`, `CommonPurpleStrongBrush`, `CommonOrangeStrongBrush`, `CommonCyanStrongBrush`, `CommonPinkStrongBrush`.
- Typography: `CommonFontFamily` (body), `MonoFontFamily` (chips/dates), plus `CommonFontSize/SmallFontSize/LargeFontSize`.
- Text is measured with `FormattedText` under `CultureInfo.CurrentCulture`, with an LRU cache (`FormattedTextCache`, max 256, line 180). Cache is invalidated on culture/brush/dpi/font changes (lines 1412–1438).
- Localization: every visible string other than `"Today"` (line 1979) is sourced from `.resx` resources. The English/French files define `LabelPresent`, `ExperienceDateRangeSeparator`, the `GlyphMarker*` glyphs, entry metadata, and `TimelineControlInteractionsHelpText` used as help overlay text.

## Performance notes

- No virtualization. All frames in the viewport are rendered every paint.
- Rendering is subscribed to `CompositionTarget.Rendering` only when needed (fit animation, inertia, drag, pending pan) and unsubscribes otherwise (lines 1506–1571).
- Heavy text measurement is cached per `(text, fontSize)` key.
- `SnapsToDevicePixels = True` and `UseLayoutRounding = True` on the default style.

## Bugs / TODOs / edge cases

### Explicit `TODO`/`FIXME`/`HACK` markers

None found in `TimelineControl.cs`, `ExperiencePage*`, `ExperienceTimelineEntryViewModel`, `ExperiencePageViewModel`, `TimelineTimeFrameItem`, or `ExperienceTimelineScrollSyncBehavior`.

### Observed rough edges

- **`"Today"` is not localized** (`TimelineControl.cs:1979`). French users still see "Today".
- **No `AutomationPeer`** on `TimelineControl`; the control surfaces no accessible name or role other than the WPF default for `Control`. Keyboard focus is supported, but assistive tech will not announce time-frame semantics.
- **No `prefers-reduced-motion` equivalent.** Fit animation, scroll sync animation, and hover scale storyboards (`ExperiencePage.xaml:66–110`) always run regardless of `SystemParameters.ClientAreaAnimation`.
- **Scroll-sync fallback uses reflection** for `StartDate` (lines 480–511); if the bound item lacks it, sync silently no-ops.
- **Hardcoded 5 experience entries** built from `.resx` keys in `ExperiencePageViewModel.RebuildEntries` (lines 310–423); there is no JSON/data pipeline.
- **No hover state for focus indicator on individual frames** — the focus outline decorates the whole control, not the focused frame. Up/Down do select a time frame, but the selected visual is the same as mouse-selected, not a separate focus ring.
- **`TryGetStartDate` catches all exceptions on `TransformToAncestor`** (lines 410–421, 466–475). Safe, but hides measurement regressions during layout storms.

## Edge cases already handled

1. Empty `TimeFrames` — EffectiveMinDate falls back to `Today.AddYears(-1)` (1538–1540).
2. Single entry — clipped and rendered without special-casing.
3. Future `EndDate` — clamped to today (2240–2245).
4. Overlapping ranges — side-by-side via lane assignment (2406–2463).
5. Very long titles — font size capped to bar height − 8 px; overflow clipped (1310–1311 / 1832–1836).
6. Missing `EndDate` — displayed as "Present"; the frame model treats null as today.
7. Reversed constructor dates — auto-swapped (`TimelineTimeFrameItem:43–45`).
8. Negative `LaneIndex`/`PaletteIndex` — clamped to 0 (`ExperienceTimelineEntryViewModel:133, 140`).
9. Viewport scrolled past content — coerced back into range (2248–2264).
10. Hit-test on overlapping bars — smallest bar wins so nested frames stay reachable (2396–2398).

## Desktop-only behaviors (need web adaptation, not copy)

1. `DrawingContext.DrawRoundedRectangle` / `DrawText` immediate-mode rendering (889–921) — replace with SVG or DOM nodes on the web.
2. `CompositionTarget.Rendering` loop (1520) — replace with `requestAnimationFrame`.
3. `Mouse.GetPosition`, `Keyboard.Modifiers` — replace with DOM `PointerEvent` / `KeyboardEvent`.
4. `VisualTreeHelper.GetDpi` (1701) — replace with `window.devicePixelRatio`.
5. `Typeface` + `FormattedText` text measurement — replace with `CanvasRenderingContext2D.measureText` or natural SVG `<text>`.
6. `FormattedText` cache (1394–1438) — unnecessary when using DOM/SVG text nodes.
7. `RenderTransform` scale on hover (`ExperiencePage.xaml:61–110`) — replace with CSS `transform` gated behind `prefers-reduced-motion: no-preference`.
8. `ItemContainerGenerator` / `TransformToAncestor` scroll-sync — replace with `getBoundingClientRect` + scroll offsets.
9. `DependencyProperty` coercion — replace with setter validation.
10. `ObservableCollection` — replace with the framework's reactive primitive (Signals / stores / `useState`).
11. `DispatcherTimer` 90 ms debounce — replace with `setTimeout` or `requestIdleCallback`.
12. `WeakReference`/`ConditionalWeakTable` — replace with `WeakMap` where applicable.
13. `Brush` resource keys + `TryFindResource` — replace with CSS custom properties (tokens) or a theme provider.
14. `FrameworkPropertyMetadata` / `AffectsRender` — replace with declarative reactivity.
15. WPF Storyboards — replace with CSS transitions or Web Animations API.
16. `ClipToBounds` on the content rect — replace with CSS `overflow` / SVG `clipPath`.

## Validation snapshot (this audit)

- Build: `dotnet build ResumeApp.sln -c Release` — see `docs/timeline/app-timeline-feature-inventory.md` for command transcript.
- Tests exercised here: all 5 timeline-related test files in `ResumeApp.Tests/`.
- Screenshots: see [../screenshots/app-timeline/](../screenshots/app-timeline/). Provenance for each capture is documented there.

## Change log for this document

- 2026-04-16 — Initial audit created on branch `docs/timeline-parity-audit`.
