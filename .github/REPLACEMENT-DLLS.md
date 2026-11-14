# LLT 2.36.8.16 replacement DLLs

Built from official dev commit `d92cef891cbb40e770125cd72c2a3905009ea0ec`, with only the configurable DRR changes integrated into the application source. Both assembly and file versions are `2.36.8.16`.

## Install

1. Run the official updater, confirm the installed version is **2.36.8.16**, and fully exit LLT, including its tray process.
2. Back up `LenovoLegionToolkit.Lib.dll` and `Lenovo Legion Toolkit.dll` in the installation folder.
3. Replace those two files with the DLLs from this artifact, then restart LLT.

The executable, other DLLs, language folders and configuration stay in place. Restore the two backups to undo the replacement. `BUILDINFO.json` records the exact source commit, assembly identities and build URL; `SHA256SUMS.txt` records the two DLL hashes.

## Battery: dynamic 50/100 Hz; AC: fixed 120 Hz

1. In **Settings → Display → Custom dynamic refresh rate**, enter **50** for the low rate and select **100 Hz** for the high rate, then **Save**. The high rate must be an existing display mode at the current resolution; the low rate is virtual and does not require a static 50 Hz mode.
2. In **Actions**, create or edit an **AC adapter disconnected** action and add a **Refresh rate** step selecting the dynamic **50/100 Hz** pair.
3. Create or edit an **AC adapter connected** action and add a **Refresh rate** step selecting fixed **120 Hz**.
4. Enable both actions. Upstream also evaluates these power triggers when LLT starts. If other actions set the refresh rate, adjust them so they do not override these choices.

For a direct switch, select **100 Hz** on the dashboard and enable its dynamic refresh rate toggle. Select **120 Hz** and turn that toggle off for fixed 120 Hz. Saving a custom range changes the available choice; it does not immediately switch the display. **Reset** restores upstream's automatically detected ranges.

The dashboard and Actions use the same dynamic range list. Saved Actions retain their explicit pair even if the custom default is later changed. The latest upstream missing-mode, clone-group and fallback fixes are retained. DRR initialization also accepts an applied presentation mode when the panel keeps a different native transport timing; a mode that did not apply still uses the upstream fallback.

## Official ITS startup option

ITS behavior, its updated experimental driver, and the Settings -> Power control are identical to official dev. There is no fork-specific ITS patch. The official `RestoreLastStateOnStartup` setting is preserved from the prior 2.36.5.16 installation.

When disabled, official dev does not restore a different saved ITS mode, but still reapplies the current mode. This build preserves that behavior unchanged.

## Build and verification

The replacement workflow compiles the integrated source directly. It verifies that the official `v2.36.8.16` tag resolves to the pinned commit, runs regression checks against the production refresh-rate and Actions sources, and checks both assembly identities and file versions before publishing the artifact. It does not check out a moving upstream branch or apply patches during the build.

The earlier successful fork build, run `37579274823`, supplies the reference 50/100 switching behavior: initialize physical 100 Hz, request virtual 50 Hz with boost enabled, and expose that same range in Actions. The new implementation generalizes that behavior to the saved custom pair.

The optional `HardwareSmoke` test exercises the real feature and real Actions step on a connected laptop, restoring the original display paths in a `finally` block. On the tested Lenovo display, both direct switching and Actions reported a Windows presentation rate of 100 Hz with two syncs per presentation (50 Hz), with DRR enabled; fixed 120 Hz reported one sync per presentation with DRR disabled. The underlying transport signal continued reporting about 120 Hz, so it must not be used alone to infer the selected DRR pair.
