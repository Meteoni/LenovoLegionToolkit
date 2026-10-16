# LLT 2.36.5.8 replacement DLLs

Built from official dev commit `c7e63484bafde60d3fbd843cd5d3d0b24aa3562c`, with the fork's ITS startup restore option and configurable DRR range integrated into the source. Both assembly and file versions are `2.36.5.8`.

## Install

1. Install the official **2.36.5.8** dev release and fully exit LLT, including its tray process.
2. Back up `LenovoLegionToolkit.Lib.dll` and `Lenovo Legion Toolkit.dll` in the installation folder.
3. Replace those two files with the DLLs from this artifact, then restart LLT.

The executable, other DLLs, language folders and configuration stay in place. Restore the two backups to undo the replacement. `BUILDINFO.json` records the exact source commit, assembly identities and build URL; `SHA256SUMS.txt` records the two DLL hashes.

## Battery: dynamic 50/100 Hz; AC: fixed 120 Hz

1. In **Settings → Display → Custom dynamic refresh rate**, enter **50** for the low rate and select **100 Hz** for the high rate, then **Save**. The high rate must be an existing display mode at the current resolution; the low rate is virtual and does not require a static 50 Hz mode.
2. In **Actions**, create or edit an **AC adapter disconnected** action and add a **Refresh rate** step selecting the dynamic **50/100 Hz** pair.
3. Create or edit an **AC adapter connected** action and add a **Refresh rate** step selecting fixed **120 Hz**.
4. Enable both actions. Upstream also evaluates these power triggers when LLT starts. If other actions set the refresh rate, adjust them so they do not override these choices.

For a direct switch, select **100 Hz** on the dashboard and enable its dynamic refresh rate toggle. Select **120 Hz** and turn that toggle off for fixed 120 Hz. Saving a custom range changes the available choice; it does not immediately switch the display. **Reset** restores upstream's automatically detected ranges.

The dashboard and Actions use the same dynamic range list. Saved Actions retain their explicit pair even if the custom default is later changed. Windows capability checks and the upstream native switching/fallback behavior are retained.

## ITS startup mode

Under **Settings → Power**, turn **Restore ITS mode on startup** off to keep the current hardware ITS mode when LLT starts. This initializes LLT's displayed mode without reapplying a mode or overwriting the saved last mode. The default remains enabled. Manual mode changes and Actions continue to work.

## Build and verification

The replacement workflow compiles the integrated source directly. It verifies that the official `v2.36.5.8` tag resolves to the pinned commit, runs regression checks against the production refresh-rate and Actions sources, and checks both assembly identities and file versions before publishing the artifact. It does not check out a moving upstream branch or apply patches during the build.

The earlier successful fork build, run `37579274823`, supplies the reference 50/100 switching behavior: initialize physical 100 Hz, request virtual 50 Hz with boost enabled, and expose that same range in Actions. The new implementation generalizes that behavior to the saved custom pair.

The optional `HardwareSmoke` test exercises the real feature and real Actions step on a connected laptop, restoring the original display paths in a `finally` block. On the tested Lenovo display, both direct switching and Actions reported a Windows presentation rate of 100 Hz with two syncs per presentation (50 Hz), with DRR enabled; fixed 120 Hz reported one sync per presentation with DRR disabled. The underlying transport signal continued reporting about 120 Hz, so it must not be used alone to infer the selected DRR pair.
