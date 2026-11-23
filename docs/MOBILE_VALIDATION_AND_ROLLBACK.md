# El Arca Wrapped — mobile-friendly validation package and rollback

This is a **test build only**, not a stable release. Use it only after confirming the target Jellyfin version is 12.4.x and the server is backed up.

## Get the validation ZIP from a phone

1. Open the repository in a browser and sign in to GitHub if prompted: https://github.com/Riscar5/el-arca-wrapped
2. Open **Actions**.
3. Choose the latest successful **Build** run on the `develop` branch (the green check; verify its commit is the latest).
4. Scroll to **Artifacts** and tap `el-arca-wrapped-validation-package` to download the ZIP.
5. Do not unzip or rename it yet. Keep it on the phone; transfer it to the NAS only when the install step is explicitly ready.

Workflow artifacts expire after **14 days**. If the artifact has expired, do not use an older package; ask for a fresh successful build.

## Pre-install safety checklist

- Confirm the Jellyfin server version is **12.4.x** and the plugin package is for Linux x64.
- In Synology Container Manager, note the current Jellyfin image/tag and container settings; do not update/recreate the container as part of this test.
- Back up Jellyfin's persistent configuration/data directory using the NAS's normal backup method. Ensure the backup is complete and accessible.
- Record the current plugin list (or screenshot it).
- Do not change media folders, library settings, metadata, or unrelated plugins.
- Keep tracking **disabled** initially.

## Test scope

Install only the validation plugin package. After restart, confirm Jellyfin starts normally, the plugin appears, its configuration page opens, and user list is visible. Check admin-only access before enabling tracking.

For the capture test, use one test account and one short item, following [REAL_SERVER_VALIDATION.md](REAL_SERVER_VALIDATION.md). Enable tracking only for the short test and disable it again immediately afterward.

## Rollback

If Jellyfin becomes unstable, the plugin page fails, or errors appear that affect the server:

1. Stop the validation and keep tracking disabled.
2. If Jellyfin remains responsive, remove **El Arca Wrapped only** using Jellyfin's plugin management page and restart Jellyfin.
3. If Jellyfin cannot start or the plugin cannot be removed normally, stop the Jellyfin container. Restore the backed-up persistent configuration/data directory (or remove the plugin's own folder from the plugins directory only if the exact plugin folder is identifiable and the backup is available). Do not delete the entire Jellyfin data directory.
4. Start the container using the same image/tag and settings recorded before the test.
5. Confirm Jellyfin and existing libraries/plugins are back to their pre-test state.

Removing the plugin does not necessarily erase its own database. Its SQLite file is kept in the plugin data folder; do not manually delete that folder during the first test unless removal/data cleanup is specifically planned.

## What to report after the test

Record the Jellyfin version, client/device, test item, sequence (play/pause/resume/seek/stop), approximate active time, report totals, whether the HTML opened offline, and any relevant log error. A few screenshots of the plugin page, preview, and export on mobile are enough for review.
