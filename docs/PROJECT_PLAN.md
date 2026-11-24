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
## API compatibility review (2026-11-23)

- Cross-checked the playback consumer interfaces against Jellyfin 12.4 API documentation/source: playback start, progress and stop event args are available and expose the item, users, playback position, pause state, completion state and play-session identifier used by this implementation.
- The plugin consumer uses `IEventConsumer<T>.OnEvent` for all three event types and registers each interface through the plugin service registrator.
- This source/API check is not a substitute for runtime validation: event delivery order and timing, configuration-page behavior, native SQLite loading and real client pause/seek/stop handling still require the planned live-server test.
- Review focus for the first live test: normal start/progress/stop; pause and resume; forward seek; repeated playback; fallback ID grouping when `PlaySessionId` is missing; and report isolation by user.


## Validation package inspection (2026-11-23)

- Fresh CI artifact from run `45234150163` (commit `0174f8b5`): `el-arca-wrapped-validation-package`, artifact ID `14848053339`; expires 2026-12-07.
- Downloaded and inspected both ZIP layers. Outer artifact contains the JPRM package `el-arca-wrapped_0.1.0.0.zip`; inner package contains the plugin assembly, metadata, managed SQLite dependencies and native SQLite runtime.
- Verified `libe_sqlite3.so` is ELF 64-bit x86-64, matching the target Synology architecture.
- Package metadata identifies plugin version `0.1.0.0`, target ABI `12.4.0.0`, and explicitly states it is a development validation build pending live-server testing.
- SHA-256 outer artifact ZIP (downloaded inspection copy): `c1cd28718bb7fab56e16d172af3cee1554fe9bd33f6e2db3805acdbad814e4ac`. Inner plugin package SHA-256: `057d44c6e6bb7629a6355eded3ea35397b51a384b755c9e3293b49657cb0a2b8`.
- No installation performed. The next gate remains live Jellyfin validation with backup and rollback.
## Code review and fresh validation build (2026-11-24)

- Reviewed the current playback session resolver, playback time estimator, SQLite store/schema upgrades, event consumer/service registration, admin controller, configuration page, statistics calculator, HTML renderer and artwork embedder.
- Corrected late-arriving Jellyfin PlaySessionId handling in commit 1ecd2b4e, then added a regression test in 5d962bbd: once a playback has started with a fallback ID, later events carrying a PlaySessionId retain the original ID so one playback is not split into multiple records.
- CI run 45340754803 completed successfully on commit 5d962bbd956722b72c8a0f817c058c48becc3cee: build succeeded with 0 warnings and 0 errors; all 29 tests passed; JPRM package generation, Linux x64 native SQLite publish, package verification and artifact upload succeeded.
- Fresh artifact: el-arca-wrapped-validation-package, artifact ID 14895663866, expires 2026-12-08. Outer ZIP SHA-256: 00c74bdcc0f69c3b61dfb4d2a3475e641cafaf32536e9a6958b105a0e4820479. Inner plugin ZIP SHA-256: f1f59e443fe526eac91e9c521f751627622d0d8335a0d3ffd3e826a1838925b7. Native libe_sqlite3.so SHA-256: a5e8aed525023e6c209d37d3e762e2e76ed3a830464569a419abfc3cf12d7a6c.
- Inspected both ZIP layers. The package contains the plugin DLL, SQLite managed dependencies, meta.json, and libe_sqlite3.so; the native library is ELF 64-bit x86-64. Metadata declares plugin 0.1.0.0 and target ABI 12.4.0.0.
- Source review found no additional compile-time or obvious structural blocker. The fallback grouping can still be ambiguous if the same client starts overlapping playback of the same item while Jellyfin omits PlaySessionId; this remains a low-frequency live-test edge case.
- Automated tests currently cover the resolver, time calculations, schema/migrations, statistics and HTML escaping, but do not fully simulate Jellyfin's real event delivery into the store. The first server test must therefore verify actual event capture, pause/resume, seek, stop and report totals.
- Configuration UI and preview/download logic have been source-reviewed but not exercised in a live Jellyfin browser. No installation or tracking activation has been performed.
## Latest branch package refresh (2026-11-24)

- Documentation-only commits 2634a9d7 and f578c874 each passed GitHub Actions. The current branch head is f578c8744f2a4126dc315733de8839b52f6b39f1.
- Latest artifact is from run 45341580856, artifact ID 14896780243, named el-arca-wrapped-validation-package; it expires 2026-12-08.
- Downloaded and inspected the latest outer and inner ZIP layers. Outer SHA-256: 00f3c3df5e263bf7603c7fcfe3b2546df996fe2b8233ccdcfc5b1067e164d88e. Inner plugin ZIP SHA-256: 6655f07be0c875f208ccb97b1ad8a5598d5195875b4b823e2177f81a4fe953ae.
- Package contains Jellyfin.Plugin.ElArcaWrapped.dll, meta.json, Microsoft.Data.Sqlite.dll, SQLitePCLRaw managed dependencies and libe_sqlite3.so. Native SQLite binary hash remains a5e8aed525023e6c209d37d3e762e2e76ed3a830464569a419abfc3cf12d7a6c; ELF e_machine is x86-64.
- Metadata timestamp is 2026-11-24T06:35:51Z, version 0.1.0.0, target ABI 12.4.0.0. This is the freshest validation package; it remains a development build, not a stable release.