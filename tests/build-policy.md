# Debug build policy

The user requires Debug only for tests, performance measurements and local deployment. Always pass `-c Debug -p:Platform=x64` explicitly; use `Debug` output paths. Never mix Debug and Release measurements or present the discarded Release results as evidence of improvement.

Optimization is paused. New performance work requires fresh worst-case Debug baselines before comparison. Retain existing fixes and regression tests; do not revert implementation simply because its Release measurements were discarded.