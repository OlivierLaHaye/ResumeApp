# Desktop Horizontal Timeline — Validation Plan

> Companion to [desktop-horizontal-timeline-spec.md](./desktop-horizontal-timeline-spec.md) and [desktop-horizontal-timeline-parity-matrix.md](./desktop-horizontal-timeline-parity-matrix.md). Defines the exact checks the implementation must pass per phase of §10 of the spec, plus the final visual-fidelity gate against the user-provided reference.

## 1. Validation principles

1. **No regression of existing tests.** All files under `ResumeApp.Tests/` must pass green at every phase.
2. **Additive model changes only.** `TimelineTimeFrameItem` grows; it does not reshape.
3. **Visual fidelity by reference images.** Each density × culture × DPI combination has an approved reference PNG; diffs outside tolerance fail the phase.
4. **Feature flag gate.** Until phase P5, the new code path is opt-in via `AppSettings.UseHorizontalTimelineV2`.

## 2. Test pyramid

### 2.1 Unit tests (xUnit, headless where possible)

| Area | File | Cases |
|---|---|---|
| Model | `ResumeApp.Tests/Models/TimelineTimeFrameItemTests.cs` | `SubtitleText` default empty; INPC raised; set/get round-trip; null coalesces to empty. |
| Entry VM | `ResumeApp.Tests/ViewModels/ExperienceTimelineEntryViewModelTests.cs` | `RoleText` flows to timeframe `SubtitleText` via page VM builder. |
| Page VM | `ResumeApp.Tests/ViewModels/ExperiencePageViewModelTests.cs` | `CreateTimeFrameForEntry` sets `SubtitleText = RoleText`, `Title = CompanyText`. |
| Automation | `ResumeApp.Tests/Controls/TimelineControlAutomationPeerTests.cs` (new) | Peer creates children only when bars are painted; `Name` = `"{Role}, {Company}, {DateRange}"`; role = `ListItem`. |
| Localization fallback | `ResumeApp.Tests/Controls/TimelineControlTests.cs` | Missing `TimelineLabelTodayMarker` resource falls back to `"Today"` without throwing. |
| Density DP | `ResumeApp.Tests/Controls/TimelineControlTests.cs` | `Density` DP defaults to `Comfortable`; setter + AffectsRender invalidation. |

### 2.2 Integration / control-host tests

- `ResumeApp.Tests/Controls/TimelineControlTests.cs` instantiates the control with 3 timeframes and asserts no exception on `Measure`/`Arrange`/`Render` at 100 %/150 %/200 % DPI.
- Scroll-sync tests in `ResumeApp.Tests/Behaviors/ExperienceTimelineScrollSyncBehaviorTests.cs` must still pass unchanged.

### 2.3 Manual visual checks

Capture screenshots from a dev build and save under `docs/screenshots/app-timeline/horizontal-v2/<culture>-<density>-<dpi>.png`. Compare to reference by eye, and when available, by pixel diff script (documented in `docs/screenshots/app-timeline/README.md`).

Combinations to capture:

- `en-CA` × `Comfortable` × 100 %
- `en-CA` × `Comfortable` × 150 %
- `en-CA` × `Compact` × 100 %
- `fr-CA` × `Comfortable` × 100 %
- `fr-CA` × `Comfortable` × 150 %
- `fr-CA` × `Compact` × 100 %
- Window width 560 CSS px (minimum)
- Window width 1920 CSS px (wide)
- High-contrast mode (Windows) at 100 %

## 3. Per-phase acceptance criteria

### Phase P0 — localization + SubtitleText wiring

- [ ] New resx key `TimelineLabelTodayMarker` present in both `Resources.resx` and `Resources.fr-CA.resx`.
- [ ] Today pill reads the resource; fallback returns `"Today"` when resource is absent (covered by a unit test that removes the key from a mock `ResourcesService`).
- [ ] `TimelineTimeFrameItem.SubtitleText` present, INPC-enabled, default empty.
- [ ] `ExperiencePageViewModel.CreateTimeFrameForEntry` sets `SubtitleText = pEntry.RoleText`.
- [ ] All existing tests pass.
- [ ] Screenshot: `en-CA-comfortable-100.png` shows "Today" pill unchanged; `fr-CA-comfortable-100.png` shows "Aujourd'hui".

### Phase P1 — two-line label

- [ ] Wide bars (width ≥ 64 and bar height ≥ 24) render role on line 1 (semibold 12 px) + company on line 2 (regular 10 px @ 70 %).
- [ ] Narrow/short bars fall back to existing single-line.
- [ ] `FormattedText` cache invalidation verified via unit test.
- [ ] No regression in label overlap-avoidance (existing 6-iteration shift).
- [ ] Screenshot parity vs. reference mockup: role text dominance matches.

### Phase P2 — AutomationPeer + focus-on-bar

- [ ] `inspect.exe` shows each painted bar as `ListItem` child of `TimelineControl`.
- [ ] `Name` matches `"{Role}, {Company}, {DateRangeText}"` under active culture.
- [ ] Focus ring decorates the focused (or selected + keyboard-focused) bar, not just the whole control.
- [ ] Focus ring meets WCAG 2.2 SC 2.4.13 (≥ 2 px, ≥ 3:1 contrast) measured against dark surface.

### Phase P3 — hover tooltip

- [ ] Tooltip appears after 200 ms hover, content = role, company, date range, location, scope excerpt (≤ 140 chars).
- [ ] Tooltip positioned above bar; does not clip outside window.
- [ ] Closes on `MouseLeave`, on bar click, and on `Esc`.
- [ ] `StaysOpen=False`; does not steal keyboard focus.

### Phase P4 — reduced motion + density

- [ ] With `SystemParameters.ClientAreaAnimation = false`, no fit animation, no hover storyboard, no inertia.
- [ ] Inertia velocity zeroed on pan release when reduced motion is on.
- [ ] `Density` DP toggles `TimeFrameLaneHeight` between 28 (Compact) and 36 (Comfortable).
- [ ] Lane auto-compressor continues to work under both densities.

### Phase P5 — visual parity sweep + flag sunset

- [ ] All reference PNGs updated and committed.
- [ ] Pixel diff against reference under agreed tolerance (≤ 2 % at per-channel level; structural similarity ≥ 0.98 where measurable).
- [ ] `UseHorizontalTimelineV2` removed; code path is the sole implementation.
- [ ] `docs/screenshots/app-timeline/horizontal-v2/` set is the new baseline for future regression.

## 4. Regression checklist (runs every phase)

- [ ] `dotnet build ResumeApp.sln -c Release` green.
- [ ] `dotnet test ResumeApp.sln -c Release` green.
- [ ] Timeline renders with 5 seeded entries at `100 %`, `125 %`, `150 %`, `200 %` DPI.
- [ ] Pan/zoom/fit/inertia still behave per pre-uplift feel.
- [ ] Scroll-sync two-way still converges without runaway.
- [ ] Language switch (`en-CA` ↔ `fr-CA`) rebuilds entries, timeline updates, Today pill localized.
- [ ] Window resize from 560 CSS px to 1920 CSS px does not clip the axis or the baseline.
- [ ] `SelectedDate` clamp still `[EffectiveMinDate, Today]`.
- [ ] `SelectedTimeFrame` still round-trips through the TwoWay binding.

## 5. Must-not-break (hard fail)

If any item below regresses, the phase is **not** mergeable:

- `TimelineControl` dependency property registrations or defaults.
- `OnRender` call order (background → era → bars → labels → baseline → ticks → today → selected → focus).
- `ExperienceTimelineScrollSyncBehavior` attached DP shape.
- `ExperiencePageViewModel.RebuildEntries` entry count and order.
- 8-accent palette cycle determinism (order tied to entry sort key).
- `AssignLanes` sort-key order.
- `DateRangeText` format string.
- Any existing xUnit test file's pass/fail status (green stays green, no new skips).

## 6. Tooling and reproducibility

- Screenshot capture: run app in release config; window size scripted via a small helper (to be added under `tools/screenshots/` during phase P5).
- DPI simulation: Windows Display settings + `SystemParameters.RenderHighContrast/ClientAreaAnimation` for reduced-motion / HC checks.
- Culture switch: `App.xaml.cs` already supports runtime culture reload via `ResourcesService`.
- Visual diff: `ImageMagick` `compare -metric SSIM` suggested; see `docs/screenshots/app-timeline/README.md` for prior guidance.

## 7. Exit criteria for the overall uplift

- All P0 and P1 items from the parity matrix (§§1–14) verified green in at least one reference environment.
- All screenshots listed in §2.3 committed under `docs/screenshots/app-timeline/horizontal-v2/`.
- `CHANGELOG` entry describing the uplift.
- `AppSettings.UseHorizontalTimelineV2` removed.
- No new `TODO` / `FIXME` / `HACK` comments in the timeline code.

## 8. Post-ship observation window

- For one release cycle, retain the ability to roll back to the prior `TimelineControl` render path via a dev-time `#define` (not a user-facing setting). Remove at cycle end.

## 9. Change log

- 2026-04-16 — Initial validation plan on branch `docs/timeline-horizontal-spec`.
