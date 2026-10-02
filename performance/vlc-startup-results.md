# Startup: prepared VLC plugins

Fresh processes, same native payload. Visible published app with the copied 207k-item library and 100k playlist. Initial first-launch delays are retained; no disk-cache purge or restart was performed. These are not cold-after-restart measurements.

| Baseline now | New optimized version |
| --- | --- |
| **App / first initial library view** — 42k ms | 20.9k ms (-50.3%) |
| **App / slowest initial library view** — 42k ms | 20.9k ms (-50.3%) |
| **App / slowest VLC construction total** — 21.6k ms | 73.3 ms (-99.7%) |
| **Component / first native initialization** — 30.8k ms | 49.9 ms (-99.8%) |
| **Component / slowest native initialization** — 30.8k ms | 56.3 ms (-99.8%) |
| **Component / slowest native library loading** — 335.1 ms | 17.1 ms (-94.9%) |

The publish profile now generates a fresh `plugins.dat` for its native VLC version. Native module lists and complete PCM decoding matched. Relocation, regeneration and invalid/unwritable payload checks passed. [VideoLAN guidance](https://docs.videolan.me/libvlcsharp/docs/best_practices.html).

Incomplete 100k playlist checks: baseline 1; optimized 0. The table uses completed initial-library-view boundaries even when a later playlist check timed out; incomplete runs do not establish playlist-load speed or correctness.

Raw samples: [native baseline](iterations/vlc-startup-baseline-6d1fb461.json), [native optimized](iterations/vlc-startup-prepared-17f25291.json), [app baseline](iterations/vlc-app-startup-baseline-3a69c4e8.json), [app optimized](iterations/vlc-app-startup-prepared-17f25291.json), [cache checks](iterations/vlc-cache-checks-17f25291.json), [publish and visual validation](iterations/vlc-startup-validation-17f25291.json). The initial native validation sample belongs to production source `6d1fb461`; its benchmark was subsequently frozen as `4f533a3c`.

Only the agent-owned published build was changed. The running user app and installed files were left untouched. This optimization has not been deployed.
