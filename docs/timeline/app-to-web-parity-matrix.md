# App → Web Timeline Parity Matrix

> Companion to [app-timeline-audit.md](./app-timeline-audit.md) and [app-timeline-feature-inventory.md](./app-timeline-feature-inventory.md). Target website: sibling repo `OlhPhotographieWebsite` under `I:\Dropbox\Projet VS\`.

## How to read this matrix

| Column | Meaning |
|---|---|
| **App behavior** | What the WPF control does today. |
| **Evidence** | File:line reference. |
| **Web recommendation** | Concrete proposal for the web port, informed by the 2025–2026 best-practice research summarized at the bottom of this file. |
| **Parity priority** | `P0` = ship-blocker, must match exactly. `P1` = core feel, same intent allowed to be re-expressed. `P2` = nice-to-have. `N/A` = desktop-only, do not copy. |
| **Risk** | Where the port is likely to break. |

---

## 1. Structure and data

| App behavior | Evidence | Web recommendation | Priority | Risk |
|---|---|---|---|---|
| 5 hardcoded experience entries built in code from `.resx` keys | `ExperiencePageViewModel.cs:310–423` | Move entries to a static JSON/TS file (`timeline.en-CA.json` / `timeline.fr-CA.json`) keyed the same way so content edits stay in the same place they are today. | P0 | Content drift between WPF and web if both read different sources. Pick a canonical source once ported. |
| `TimelineTimeFrameItem { StartDate, EndDate, Title, AccentColorKey }`, dates stripped to `.Date`, reversed swapped | `Models/TimelineTimeFrameItem.cs` | Mirror the type in TypeScript. Normalize at load time (strip time, swap if reversed). | P0 | ISO date parsing with TZ — use `YYYY-MM-DD` strings, treat as UTC-neutral. |
| `ExperienceTimelineEntryViewModel` fields (Company/Role/Location/Scope/Tech/Accomplishments) | `ExperienceTimelineEntryViewModel.cs` | Mirror as a plain TypeScript interface. | P0 | — |
| `TechItems` split on `/`, `•`, `·`, `|`, `;`, case-insensitive dedup | `ExperienceTimelineEntryViewModel.cs:107–122` | Re-implement as a pure function with the same separators and dedup rule. Add a unit test covering the same cases as the xUnit file. | P0 | Unicode normalization of `·` vs `•` — keep the literal characters. |
| `PaletteIndex` cycles 8 accent colors; `MarkerGlyph` cycles 4 shapes by `LaneIndex % 4` | `ExperienceTimelineEntryViewModel.cs:172–182` | Map to 8 CSS variables (`--accent-0` … `--accent-7`) in the site theme, and to 4 inline SVG glyphs. | P1 | The brush key names (`CommonBlueStrongBrush`, etc.) do not translate to CSS — keep the *ordering* deterministic, not the names. |

## 2. Rendering

| App behavior | Evidence | Web recommendation | Priority | Risk |
|---|---|---|---|---|
| Immediate-mode `DrawingContext` rendering of bars/ticks/labels | `TimelineControl.cs:889–921` | For the career timeline, use **DOM + CSS** (`<ol><li>` of `<article>`) rather than Canvas/SVG — a CV is content to be read, not a scrolling chart. Keep the visual vocabulary (bars, era bands, ticks, glyph markers) as CSS decoration. See "Non-negotiable parity requirements" below. | P1 | Desktop view may want a richer mini-map; treat that as a separate `<svg>` overlay, not as the primary UI. |
| Alternating even-year era bands | `TimelineControl.cs:1903–1944` | Reproduce as a CSS `repeating-linear-gradient` on the timeline rail background. | P2 | Contrast against text labels must meet 3:1; test in light and dark mode. |
| Rounded bar with `min(10, height/2)` corner radius, bar height `min(22, max(2, laneHeight−6))` | `TimelineControl.cs:1742,1758` | Directly translate to CSS custom properties (`--bar-h`, `--bar-radius`). | P1 | — |
| Opacity 65% default / 85% hover / 100% selected | `TimelineControl.cs:1765–1799` | Translate to CSS states: `:hover`, `[aria-current="true"]`, `:focus-visible`. | P0 | `:hover` on touch devices — use `@media (hover: hover)` to gate. |
| Selected bar: white stroke 1.5 px + 1.5–2 px glow | `TimelineControl.cs:1787–1799` | CSS `outline` + `box-shadow`. Keep glow subtle to respect dark/light mode. | P1 | `box-shadow` hurts GPU on long lists — measure before shipping. |
| Bar label clipped to bar, leading dot | `TimelineControl.cs:1824–1863` | CSS `overflow:hidden; text-overflow:ellipsis; white-space:nowrap;` plus a `::before` dot. | P1 | Truncation collides with bar width at small breakpoints — also render the full label inside the detail card. |
| Baseline hairline dashed + major/minor ticks (8 px / 4 px) | `TimelineControl.cs:1868, 2103–2134` | Render ticks as CSS pseudo-elements on a background axis, or as a single `<svg>` track for desktop only. | P2 | Axis may not exist at all on mobile (vertical layout) — that is acceptable. |
| Tick label formats `yyyy`, `MMM yyyy`, `MMM d` under `CurrentCulture` | `TimelineControl.cs:417–425` | Use `Intl.DateTimeFormat` with `en-CA`/`fr-CA`. Keep the same three formats. | P0 | `fr-CA` date order — verify visual parity with the WPF build. |
| "Today" marker pill — dashed line + pill label | `TimelineControl.cs:1946–1988` | Horizontal-axis variant only. On vertical layouts, show a "Présent" / "Present" badge on the top-most item instead. | P2 | The WPF `"Today"` string is hardcoded in English — web version must localize (`Aujourd'hui`). |
| Selected-date pill `MMM d, yyyy` | `TimelineControl.cs:2166` | Only needed if the web port keeps a scrubbable timeline axis. Otherwise omit. | P2 | — |

## 3. Layout, zoom, pan

| App behavior | Evidence | Web recommendation | Priority | Risk |
|---|---|---|---|---|
| Lane assignment: first lane with ≥ 8 px gap | `TimelineControl.cs:397–409` | Replicate server-side (at build time) or in a layout hook; deterministic and testable. | P1 | Different viewport widths yield different gaps — precompute at breakpoints. |
| Zoom range `[0.08, 48.0]` px/day, wheel factor `1.12` | `TimelineControl.cs:154,615–627` | Skip interactive zoom on the web timeline — the read flow does not need it. Offer instead a density toggle (compact / comfortable). | P2 | Zoom would require scroll-hijacking which hurts a11y; explicitly not recommended. |
| Pan inertia with friction `0.12^t`, threshold `0.02 days/sec` | `TimelineControl.cs:1625–1689` | Rely on native scroll momentum; do not reimplement inertia in JS. | N/A | JS-implemented inertia on web typically breaks reduced-motion and wheel semantics. |
| Fit animation 240 ms `EaseOutCubic` | `TimelineControl.cs:1597–1623` | Use `scroll-behavior: smooth` gated behind `prefers-reduced-motion: no-preference`. | P2 | Smooth scroll can skip rows on long pages — keep it short (200 ms feel). |
| `SelectedDate` clamped `[min, today]` | `TimelineControl.cs:501` | Enforce on any URL deep-link parameter (`?at=2024-08`); otherwise the UI does not expose selection. | P2 | — |

## 4. Interaction

| App behavior | Evidence | Web recommendation | Priority | Risk |
|---|---|---|---|---|
| Keyboard navigation `Home`/`End`/`Up`/`Down`/`Left`/`Right` / `Ctrl+arrow` | `TimelineControl.cs:1120–1155` | A CV list is *reading content, not a composite widget* — let native `Tab` traversal drive focus across links/buttons inside each entry. Do **not** hijack arrow keys. This follows the ARIA APG guidance on structural roles. | P0 | Copying the WPF keyboard model would break screen-reader browse mode on the web. |
| Mouse hover scale storyboards on detail cards | `ExperiencePage.xaml:66–110` | Keep the hover lift as a CSS `transform: translateY(-2px)` gated behind `@media (hover:hover) and (prefers-reduced-motion: no-preference)`. | P1 | Do not animate `top`/`margin` — forces layout. |
| Scroll sync between timeline axis and detail list (220 ms debounce + anim) | `ExperienceTimelineScrollSyncBehavior.cs` | If the web port keeps a visual axis rail, use an `IntersectionObserver` to update the active-year indicator from the currently-in-view `<article>`. Do **not** animate scroll programmatically in response to the axis. | P1 | Bidirectional sync plus programmatic smooth scroll is an accessibility trap. One-way (content → axis) is safer. |
| Drag threshold 3 px, click window 320 ms | `TimelineControl.cs:170, 1035–1092` | Native `click` semantics handle this on web — no custom tracking. | N/A | — |
| Hit-test picks smallest overlapping bar | `TimelineControl.cs:2378–2403` | DOM `z-index` + stacking context gives nested-frame clicks naturally when inner bars are above outer. | P2 | Requires event bubbling discipline on the wrapper. |

## 5. Accessibility

| App behavior | Evidence | Web recommendation | Priority | Risk |
|---|---|---|---|---|
| No `AutomationPeer`, no `AutomationProperties.Name` | absent in `TimelineControl.cs` / `ExperiencePage.xaml` | Replace with semantic HTML: `<section aria-labelledby="career-heading">` → `<ol>` → `<li><article aria-labelledby="role-N">`. Dates inside `<time datetime="…">`. | P0 | The web port must not replicate the app's missing accessibility; that is a gap, not a feature. |
| Focus outline when `IsKeyboardFocusWithin` | `TimelineControl.cs:1990–2013` | Use `:focus-visible` with a ≥ 2 CSS px perimeter ring meeting 3:1 contrast (WCAG 2.2 SC 2.4.13 guidance). | P0 | `outline: none` without replacement is disqualifying. |
| Hover scale on cards | `ExperiencePage.xaml` | Gate behind `prefers-reduced-motion: no-preference` (opt-in model). | P0 | Do not use the `animation-duration: 0.01ms` hack — set `animation: none` / remove the transition. |
| No live regions | absent | Not needed — the web version should avoid dynamic selection state that would require announcement. | N/A | — |
| Help text `TimelineControlInteractionsHelpText` (localized) | `.resx` | On the web, replace with visible copy near the timeline that explains "use arrow keys or Tab to navigate the list". Only show if the web version adopts any custom keyboard behavior (it should not). | P2 | — |

## 6. Responsiveness

| App behavior | Evidence | Web recommendation | Priority | Risk |
|---|---|---|---|---|
| Window resize recomputes `ContentRect`, triggers initial fit | `TimelineControl.cs:1158–1170` | Use CSS container queries (`container-type: inline-size`) on the timeline wrapper. Single-column below ~ 48 rem, dual-column rail-and-content above. | P0 | Media queries keyed to viewport break at 125% zoom — container queries do not. |
| Padding `16,12,16,16` default | `Resources/Controls.xaml:12–17` | Use the site's spacing tokens, matching visual density. | P1 | — |
| No virtualization | absent | Not needed for < 50 entries; do not premature-optimize. | P2 | — |

## 7. Localization

| App behavior | Evidence | Web recommendation | Priority | Risk |
|---|---|---|---|---|
| All user strings in `.resx` except `"Today"` | `TimelineControl.cs:1979` | Localize everything in the web port, including the "today/present" label. Keep `en-CA` / `fr-CA` keys consistent with the WPF resources. | P0 | Inconsistent capitalization between `Present`/`Présent` / `Today`/`Aujourd'hui`. Use the existing WPF strings as canonical. |
| `CultureInfo.CurrentCulture` for dates | `TimelineControl.cs:417–425` | `new Intl.DateTimeFormat(activeLocale, …)` — compute once per render, not per label. | P1 | SSR vs CSR locale mismatch — pass the resolved locale from the server. |

## 8. Performance

| App behavior | Evidence | Web recommendation | Priority | Risk |
|---|---|---|---|---|
| `FormattedText` LRU cache (256 entries) | `TimelineControl.cs:180, 1394–1438` | Not needed on the web — the browser text layout is already optimized. | N/A | — |
| `CompositionTarget.Rendering` subscribed only while animating | `TimelineControl.cs:1506–1571` | `requestAnimationFrame` only during active transitions; default to CSS transitions that do not require JS. | P2 | Over-subscribing RAF is the common web mistake. |

---

## Non-negotiable parity requirements

These are the aspects that make the WPF timeline *feel right* and must survive the port. The web may express them differently, but the intent is binding.

1. **Content order stays chronological and grouped by role.** Source of truth is the WPF entry list; the web must render the same 5 entries in the same order, bilingual.
2. **Date range formatting matches.** `YYYY-MM` start + `YYYY-MM`/`Present` end, with `Present` localized (unlike the current `"Today"` hardcode). Evidence: `ExperienceTimelineEntryViewModel.cs:150–159`.
3. **Tech list split rules match.** Separators: `/`, `•`, `·`, `|`, `;`. Case-insensitive dedup. Evidence: `ExperienceTimelineEntryViewModel.cs:107–122`.
4. **Palette cycle is deterministic.** Entries receive accent colors in the same order as WPF (by lane/palette index). Evidence: `ExperienceTimelineEntryViewModel.cs:172–182`.
5. **Visible selection state has three levels — default / hover / active.** Opacity or outline change at each level, matching the WPF 65% / 85% / 100% feel. Evidence: `TimelineControl.cs:1765–1799`.
6. **Keyboard users can reach every entry without a mouse**, and the focus ring is visible and meets WCAG 2.2. The WPF gap (no `AutomationPeer`) must not be carried forward.
7. **Reduced-motion users get an immediate end-state**. Every hover/entrance/scroll animation must be gated by `prefers-reduced-motion: no-preference`.
8. **Bilingual support is fully parallel.** Every string the WPF localizes, the web localizes.

## Desktop-only behaviors that need web adaptation

Do not try to port these verbatim. Replace with web-native equivalents.

1. Immediate-mode `OnRender` / `DrawingContext` drawing — replace with DOM/CSS (and a thin SVG overlay only if a mini-map is desired).
2. `CompositionTarget.Rendering` subscription loop — use CSS transitions for end-state animation; use RAF only when absolutely needed.
3. Mouse-wheel zoom and pan inertia on the timeline itself — remove. Browser scroll is the native interaction; scroll hijacking fails accessibility checks.
4. `TransformToAncestor`-based scroll sync — replace with `IntersectionObserver` one-way (content → axis indicator).
5. `ConditionalWeakTable` reflection caching of `StartDate` — unnecessary; the TS port uses typed models.
6. `DispatcherTimer` 90 ms debounce — standard `setTimeout`/`requestIdleCallback`.
7. `Typeface`/`FormattedText` measurement — browser text layout is already cached.
8. WPF `FrameworkPropertyMetadata` coercion — use setter validation or Zod parsing at load time.
9. WPF Storyboards — CSS transitions, Web Animations API, or Motion library.
10. `TryFindResource` brush lookup — CSS custom properties (theme tokens).
11. `ItemContainerGenerator` / `ItemsControl` realization — standard framework mapping (React/Svelte/Vue render each `<article>` directly).

## Open questions resolved by code evidence

| Question | Resolved answer | Evidence |
|---|---|---|
| Is the entry data stored in a separate file on disk? | No. Entries are built in code from resource strings. | `ExperiencePageViewModel.cs:310–423` |
| Does the timeline persist selection or zoom across sessions? | No. Bindings exist but no persistence layer is wired. | Absence across `App.xaml.cs`, `Infrastructure/` |
| Is "Today" localized in the WPF app? | No. The marker string is hardcoded English. | `TimelineControl.cs:1979` |
| Does the scroll-sync behavior animate on every scroll event? | No. It debounces at 90 ms and animates at 220 ms. | `ExperienceTimelineScrollSyncBehavior.cs:213–239, 438` |
| Is there any `TODO`/`FIXME` in the timeline code? | No. Grep over all timeline-related files found none. | Full-file grep of `Controls/TimelineControl.cs`, `Pages/ExperiencePage.*`, `ViewModels/Pages/Experience*`, `Models/TimelineTimeFrameItem.cs`, `Behaviors/ExperienceTimelineScrollSyncBehavior.cs` |
| Is there an `AutomationPeer` on the timeline? | No, and no `AutomationProperties.Name` is set on it. | Absence in `TimelineControl.cs` and `ExperiencePage.xaml` |
| Does the timeline virtualize time frames? | No. All visible frames render every paint. | Absence of virtualization code in `BuildVisibleTimeFrames` |
| Are reversed dates an error? | No. The model constructor auto-swaps. | `TimelineTimeFrameItem.cs:43–45` |
| How many experience entries feed the timeline today? | Five, assembled from `.resx` keys. | `ExperiencePageViewModel.cs:310–423` |
| Is there a backend contract? | None. | Absence of any service/serializer feeding the view model |

## Variant identification

The repo contains exactly **one** timeline implementation: `Controls/TimelineControl.cs`. The only other references to "timeline" are:

- `ViewModels/Pages/ExperienceTimelineEntryViewModel.cs` (detail-card model)
- `Behaviors/ExperienceTimelineScrollSyncBehavior.cs` (scroll sync)
- `Models/TimelineTimeFrameItem.cs` (data model)
- corresponding tests
- resource keys and README screenshots

There is no second variant, no experimental branch code in `main`, and no disabled/legacy timeline control. The canonical artifact to port is `Controls/TimelineControl.cs` + the detail-card layout in `Pages/ExperiencePage.xaml`.

> Note: the remote branch `claude/redesign-timeline-ux-cE5Zj` exists but is not merged into `main`. This audit reflects `main` at commit `fdf0ba2`. If that branch holds a better starting point, it must be evaluated before the web port starts.

---

## Research summary (2025–2026 best practices)

The recommendations above are informed by the following research. Full notes live with the implementation plan in whichever workspace does the port; this section captures the bullets that influenced this matrix.

### Accessible timeline pattern

- The ARIA APG has no "timeline" pattern. Use semantic HTML: `<section>` landmark → `<h2>` → `<ol>` → `<li><article>` with an `<h3>` inside each item and `<time datetime="YYYY-MM">` for dates.
- Avoid `role="feed"` (that is for infinite/lazy social streams) and avoid redundant `role="list"` on a native `<ol>`.
- Sources: W3C WAI-ARIA Authoring Practices Guide, ARIA in HTML.

### Responsive layout

- Default to vertical on all viewports. A horizontal-only desktop timeline traps keyboard users and breaks reading order for screen readers.
- Use CSS **container queries** on the timeline wrapper (`container-type: inline-size`), media queries only for the page shell.
- Respect WCAG 2.2 SC 1.4.10 Reflow — test at 200% zoom and 320 CSS px width; sticky year labels must not overlap content at that size.

### Keyboard and focus

- For *reading content*, do not apply roving tabindex. Use native tab order across links/buttons inside each item. Roving tabindex is for composite widgets (tabs, menus, grids) and breaks AT browse mode on an `<article>` list.
- WCAG 2.2 SC 2.4.11 Focus Not Obscured — compensate sticky headers with `scroll-margin-top`.
- WCAG 2.2 SC 2.4.13 Focus Appearance — ring ≥ 2 CSS px perimeter, ≥ 3:1 contrast, use `:focus-visible`, not `:focus`.

### Reduced motion

- Default posture: wrap non-essential animation in `@media (prefers-reduced-motion: no-preference) { ... }` (opt-in), not the reverse.
- Scroll-linked animations (`animation-timeline: scroll()/view()`) must be disabled under `reduce`. Cross-fades and color transitions remain acceptable.
- Drop under `reduce`: parallax, entrance slide-ins, scroll-progress bars, auto-scroll, any `transform: translate/scale` keyframes.

### Token-efficient durable prompting for the port workflow

- Keep a `CLAUDE.md` plus three task-scoped files: `<task>-plan.md` (stable), `<task>-context.md` (file paths + key decisions), `<task>-tasks.md` (mutable checklist). Reference by path; do not re-paste.
- Put long context at the top of prompts (inside XML tags with id/source attributes), instructions in the middle, specific question at the bottom.
- Insert checkpoint prompts between phases: emit a short state summary that the next session can resume from, rather than carrying full chat history.

Source shortlist:

- W3C ARIA Authoring Practices Guide — https://www.w3.org/WAI/ARIA/apg/
- ARIA in HTML — https://www.w3.org/TR/html-aria/
- WCAG 2.2 — https://www.w3.org/TR/WCAG22/
- prefers-reduced-motion (MDN) — https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/@media/prefers-reduced-motion
- web.dev: prefers-reduced-motion — https://web.dev/articles/prefers-reduced-motion
- Claude prompting best practices — https://docs.claude.com/en/docs/build-with-claude/prompt-engineering/claude-4-best-practices
- Claude Code best practices — https://code.claude.com/docs/en/best-practices

---

## Next recommendation for the website

When the port begins on the website, the single highest-leverage first step is:

> **Build a minimal, static, semantic vertical timeline**: `<section>` + `<h2>` + `<ol>` of `<article aria-labelledby="…">`, each with an `<h3>` role title, a `<time datetime="…">` range, scope/tech chips, and accomplishments list. Match content 1-to-1 with the WPF entries, bilingual, no JavaScript interactivity. Keyboard, focus ring, and reduced-motion support land on day one.

That baseline clears every P0 row in this matrix. Everything else (desktop rail visualization, density toggles, intersection-based year indicator) is a progressive enhancement that cannot regress the P0 layer.
