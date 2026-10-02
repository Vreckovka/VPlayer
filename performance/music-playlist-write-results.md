# 100k-track save and clear

Actual WPF operation through its painted result, with every persisted occurrence checked. Initial baseline versus optimized median; negative percentages mean less time.

| Baseline now | New optimized version |
| --- | --- |
| **Reorder, save and render 100k tracks** — Timed out | 28.6k ms |
| **Save, clear and render 100k tracks** — 66.9k ms | 14.3k ms (-78.6%) |

The save baseline did not complete; it has no percentage.
Raw JSON retains all phases, failures and precision, including shutdown save scopes after the verified endpoint. Timings include queue waits and background activity on this machine.

Earlier rerun failures remain in [retained evidence](iterations/music-playlist-metadata-native-attempts-074136ae.json): initial-render timeout and playback-save exception. Completed timings do not establish failure rates.

Baseline: `3b70d7dcfde168480a432dc2a9b28b41ec67d652`; optimized: `09ea4d79154a53eb4c949db0154f797a68d8b9da`.
