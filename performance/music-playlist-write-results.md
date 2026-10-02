# 100k-track save and clear

Actual WPF operation through its painted result, with every persisted occurrence checked. Initial baseline versus optimized median; negative percentages mean less time.

| Baseline now | New optimized version |
| --- | --- |
| **Reorder, save and render 100k tracks** — Timed out | 27.6k ms |
| **Save, clear and render 100k tracks** — 66.9k ms | 15.3k ms (-77.1%) |

The save baseline reached reconciliation but exceeded the process budget; it has no percentage. Raw JSON retains all phases, failures and precision, including shutdown save scopes after the verified endpoint. Timings include queue waits and background activity on this machine.

Baseline: `3b70d7dcfde168480a432dc2a9b28b41ec67d652`; optimized: `d467b7ad7336f96b57024b2dbb9d600e86827000`.
