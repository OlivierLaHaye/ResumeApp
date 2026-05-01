# App Timeline Screenshots

## Provenance and scope

This folder contains screenshots that illustrate the WPF timeline's visible states for the parity audit in [../../timeline/app-timeline-audit.md](../../timeline/app-timeline-audit.md).

**Important honesty note:** The capture below is reused from the existing README portfolio assets at `docs/readme-assets/screenshots/`, which were taken previously from the real WPF application window. No fresh hardware capture was performed during this audit session — running the WPF app from a bash session without a user-controlled display would not produce trustworthy screenshots, and I do not want to claim hardware validation that did not happen.

If the repo owner wants to refresh this folder with freshly captured screenshots at additional sizes, DPIs, and states (hover, selected, keyboard-focused, today marker visible, empty list), the reproduction steps are in the "How to reproduce" section below.

## Captures present

| File | Language | Theme | Orientation | State captured |
|---|---|---|---|---|
| `experience-en-light-vertical.png` | English (`en-CA`) | Light | Vertical 4K | Experience page with timeline bars, role cards, lane-offset details |

## States *not* captured (gap list)

The following states are referenced in the audit but do not have dedicated screenshots yet. They would materially improve the parity documentation:

1. Hover on a time-frame bar (85% opacity + stroke) — [Controls/TimelineControl.cs:1765–1818](../../../Controls/TimelineControl.cs).
2. Selected time frame (100% + white stroke + glow) — [Controls/TimelineControl.cs:1787–1799](../../../Controls/TimelineControl.cs).
3. Keyboard-focused timeline control (inner glow + outer stroke) — [Controls/TimelineControl.cs:1990–2013](../../../Controls/TimelineControl.cs).
4. Today marker visible in viewport (dashed vertical + pill) — [Controls/TimelineControl.cs:1946–1988](../../../Controls/TimelineControl.cs).
5. Selected-date pill `MMM d, yyyy` visible — [Controls/TimelineControl.cs:2138–2196](../../../Controls/TimelineControl.cs).
6. `fr-CA` localization of the experience page — complements the existing English capture.
7. Dark theme + experience page together.
8. Scroll-sync in action (timeline axis highlights the year of the visible detail card).
9. Keyboard-navigation state after pressing `Up` / `Down` to step between time frames.
10. Ctrl+arrow pan promotion behavior — hard to show in a still, optional.

## How to reproduce

On a Windows host with .NET 10 SDK installed:

```powershell
dotnet build ResumeApp.sln -c Release
dotnet run --project ResumeApp.csproj -c Release
```

Then navigate to the Experience tab and capture the window at the states listed above. The repository ships `ResxCleaner.exe` for the pre-build step.

Recommended capture matrix:

- 2 languages × 2 themes = 4 baseline captures
- At least 3 window widths (e.g. 1920 × 1080, 3840 × 2160 horizontal, vertical 4K)
- At least 2 DPI scales (100%, 150%) — noted with DPI in the filename

File-naming convention used by the existing README assets: `<section>-<lang>-<theme>-<orientation>.png`. Follow that convention for new captures so both sets remain consistent.

## Limitations honestly stated

- No fresh hardware screenshots were captured for this audit session.
- No emulator was used; no "simulated" captures exist in this folder.
- The single captured file is a copy of the README hero capture that happens to show the experience timeline as it exists in `main`.
