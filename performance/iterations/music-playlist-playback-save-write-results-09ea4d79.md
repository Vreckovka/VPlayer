# 100k-track save and clear

Actual WPF operation through its painted result, with every persisted occurrence checked. Initial baseline versus optimized median; negative percentages mean less time.

| Baseline now | New optimized version |
| --- | --- |
| **Reorder, save and render 100k tracks** — 27.6k ms | 28.6k ms (+3.7%) |
| **Save, clear and render 100k tracks** — 15.3k ms | 14.3k ms (-6.7%) |

Raw JSON retains all phases, failures and precision, including shutdown save scopes after the verified endpoint. Timings include queue waits and background activity on this machine.

Earlier rerun failures remain in [retained evidence](iterations/music-playlist-metadata-native-attempts-074136ae.json): initial-render timeout and playback-save exception. Completed timings do not establish failure rates.

Baseline: `d467b7ad7336f96b57024b2dbb9d600e86827000`; optimized: `09ea4d79154a53eb4c949db0154f797a68d8b9da`.
