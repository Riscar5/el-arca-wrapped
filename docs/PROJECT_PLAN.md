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
- SQLite persistence in the plugin data directory, including idempotent schema upgrades.
- Playback start/progress/stop event consumer and configurable capture.
- Session ID handling: use Jellyfin's session ID where present; otherwise create a unique ID at playback start and reuse it for progress/stop events.
- Playback persistence writes are serialized to reduce races between concurrent event updates; fallback client grouping ignores blank device/client identifiers.
- Watched-time estimation from media-position deltas, excluding paused intervals and rejecting large forward seek jumps; elapsed-time/runtime bounds help prevent inflated values.
- User/year-filtered statistics: total plays and minutes, films/episodes/series, unique titles, active days and streak, monthly activity, top rankings, genres, decades, UTC hour/weekday patterns, completion rate, repeat plays, longest play/day and coverage dates.
- Regression tests for calculations, UTC/year boundaries, user isolation, repeated sessions, migrations and fallback session-ID grouping.
- Responsive, Spanish-language, standalone HTML renderer with embedded CSS, rankings, visual insight sections and escaping for user/item text.
- Export embeds bounded-size JPEG artwork when Jellyfin can process it; missing/invalid/oversized art safely falls back to poster placeholders.
- Series rankings prefer parent-series artwork and fall back to representative episode artwork.
- Admin-only API endpoints to list users, preview reports and export HTML. User-scoped data retrieval and admin authorization audited.
- Admin configuration UI for tracking control, user/year selection, preview and export.
- CI has passed on the latest `develop` commit at the time this plan was updated.

## Current known limitations

- Real Jellyfin server behavior has not yet been validated. Event timing, pause/resume/seek behavior and SQLite capture must be tested against the target server/client.
- Reports use UTC for month/day/hour groupings. Local timezone support is not yet wired to server/user settings.
- Capture only starts after the plugin is installed and tracking is enabled; no retroactive library history.
- No public release, package repository, or installation on the NAS yet.

## Next milestone: reversible live-server validation

The detailed checklist is in [REAL_SERVER_VALIDATION.md](REAL_SERVER_VALIDATION.md). The sequence is intentionally cautious:

1. Verify Jellyfin version and compatibility before installation.
2. Back up configuration/data and note the current Docker image/tag.
3. Install a validation build only; leave tracking disabled initially.
4. Confirm configuration UI and admin/non-admin endpoint permissions.
5. Enable tracking for a short test, exercise play/pause/resume/seek/stop, then disable it.
6. Inspect report counts/time, user isolation, logs, and standalone HTML offline behavior.
7. Fix any discrepancies and repeat the test before deciding on a release.

## Later improvements (after live validation)

- Resolve timezone display/grouping to local Jellyfin/server timezone, with explicit setting or reliable source.
- Add stronger capture integration tests and artwork tests where practical.
- Improve report storytelling and visual polish based on mobile review.
- Prepare plugin manifest/package and repository installation instructions.
- Only then consider a stable release and installation on the production NAS.

## Initial constraints

- No extra Docker container or external service.
- No Telegram bot, user comparisons, or PDF export in the initial scope.
- Prefer reversible tests; avoid changes to Jellyfin media and existing metadata.
