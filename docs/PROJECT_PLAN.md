# El Arca Wrapped — project plan

## Product goal

Create a personalized, story-driven annual viewing recap for each Jellyfin user. Administrators select a user and year, preview the report, and export a standalone HTML file. Batch ZIP export may be considered later.

## Privacy and data integrity

- Process and store playback data locally on the Jellyfin server.
- Keep each user's calculations and exports isolated.
- Clearly label data coverage; no plugin-captured history exists before installation unless a future import feature is implemented.
- Never fabricate statistics or insights when data is missing.
- Tracking is disabled by default and should remain off until the administrator has reviewed the settings.

## Implemented on `develop`

- Jellyfin plugin project, CI build and automated test project.
- SQLite persistence in the plugin data directory, including idempotent schema upgrades and explicit initialization of the bundled SQLite provider before opening connections.
- Playback start/progress/stop event consumer and configurable capture.
- Session ID handling: use Jellyfin's per-playback `PlaySessionId` where present; otherwise create a unique ID at playback start and reuse it for progress/stop events. The connected client `Session.Id` is deliberately not used as a playback key because it can span multiple playbacks.
- Playback persistence writes are serialized to reduce races between concurrent event updates; fallback client grouping ignores blank device/client identifiers.
- Watched-time estimation from media-position deltas, excluding paused intervals and rejecting large forward seek jumps; elapsed-time/runtime bounds help prevent inflated values.
- User/year-filtered statistics: total plays and minutes for films and episodes only (other item types excluded), unique titles, active days and streak, monthly activity, top rankings, genres, decades, UTC hour/weekday patterns, completion rate, repeat plays, longest play/day and coverage dates.
- Preview/export UI cleans up temporary Blob URLs to avoid leaking browser memory across repeated previews; download URL revocation is delayed to avoid interrupting slower mobile downloads.
- Regression tests for calculations, UTC/year boundaries, user isolation, repeated sessions, migrations and fallback session-ID grouping.
- Responsive, Spanish-language, standalone HTML renderer with embedded CSS, rankings, visual insight sections and escaping for user/item text.
- Export embeds bounded-size JPEG artwork when Jellyfin can process it; missing/invalid/oversized art safely falls back to poster placeholders.
- Series rankings prefer parent-series artwork and fall back to representative episode artwork.
- Admin-only API endpoints to list users, preview reports and export HTML. User-scoped data retrieval and admin authorization audited.
- Admin configuration UI for tracking control, user/year selection, preview and export.
- Linux x64 validation ZIP includes the plugin assembly, all required managed SQLite assemblies, native `libe_sqlite3.so`, and Jellyfin plugin metadata.
- Latest CI run passed: compilation with 0 warnings/errors, 28/28 automated tests, JPRM package generation, Linux x64 native publish, package dependency verification, and artifact upload.

## Current known limitations

- Real Jellyfin server behavior has not yet been validated. Event timing, pause/resume/seek behavior, configuration UI and SQLite capture must be tested against the target server/client.
- The current validation package targets Linux x64 (the target Synology architecture); other operating systems/architectures are not packaged yet.
- Reports use UTC for month/day/hour groupings. Local timezone support is not yet wired to server/user settings.
- Capture only starts after the plugin is installed and tracking is enabled; no retroactive library history.
- No public release or public plugin repository manifest yet.

## Next milestone: reversible live-server validation

The detailed checklist is in [REAL_SERVER_VALIDATION.md](REAL_SERVER_VALIDATION.md). The sequence is intentionally cautious:

1. Confirm the running Jellyfin version and container image/tag.
2. Back up Jellyfin configuration/data and note the current Docker image/tag.
3. Install the validation build only; leave tracking disabled initially.
4. Confirm the configuration UI loads, the user list populates, and admin/non-admin endpoint permissions behave correctly.
5. Enable tracking for a short test, exercise play/pause/resume/seek/stop, then disable it.
6. Inspect report counts/time, user isolation, logs, and standalone HTML offline behavior.
7. Fix any discrepancies and repeat the test before deciding on a release.

## Later improvements (after live validation)

- Resolve timezone display/grouping to local Jellyfin/server timezone, with explicit setting or reliable source.
- Add stronger capture integration tests and artwork tests where practical.
- Improve report storytelling and visual polish based on mobile review.
- Prepare the public plugin manifest and clear installation/update instructions.
- Only then consider a stable release and installation on the production NAS.

## Initial constraints

- No extra Docker container or external service.
- No Telegram bot, user comparisons, or PDF export in the initial scope.
- Prefer reversible tests; avoid changes to Jellyfin media and existing metadata.
