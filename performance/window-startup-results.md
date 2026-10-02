# Startup window ordering

| Baseline | Fixed |
| --- | --- |
| Automatic video load — takes foreground | Other process keeps focus and covers VPlayer |

Removed the shared startup topmost toggle and automatic video focus. Owned video/control windows now preserve the foreground application's position above VPlayer. Explicit activation and intentional always-on-top remain supported.

Five unit checks; **223 tests passed**. Native checks use normal and topmost foreground windows in separate processes, with 100 layout updates and 10 hide/show cycles each. Library loading and media playback are outside this native check.

The full app also rendered an isolated 100k-entry playlist with correct order, stored metadata, final-row visibility and search fingerprints. Home and first/final playlist screenshots were reviewed. This is a compatibility check, not a startup speed comparison.

Source: `9cfd8889`. [Normal foreground](iterations/window-startup-normal-9cfd8889.json), [topmost foreground](iterations/window-startup-topmost-9cfd8889.json), [original overlay failure](iterations/window-startup-original-overlay-failure-6d860eba.txt). The [activation-only draft](iterations/window-startup-zorder-draft-failure.txt) still failed stacking order. A [draft runner handoff failure](iterations/window-startup-probe-handoff-draft-failure.json) is retained separately; atomic marker publication fixed it.
