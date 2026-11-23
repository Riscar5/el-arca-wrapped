# El Arca Wrapped

A Jellyfin plugin for creating personalized annual viewing recaps for each server user.

The report is designed to feel like a yearly recap: viewing time, films and episodes, series rankings, genres, viewing patterns, personal milestones and records, with a self-contained HTML export that can be opened offline.

## Project status

**Validation package built — live Jellyfin testing is the remaining major milestone.**

The `develop` branch contains playback capture, local SQLite persistence, statistics, admin preview/export endpoints, and a responsive standalone HTML renderer. The latest CI run passed compilation, all 28 automated tests, package generation, SQLite dependency bundling, and package-content verification.

A validation ZIP is generated for Linux x64, including the managed SQLite assemblies and native `libe_sqlite3.so`. It is intended for the target Synology x86-64 server. It is not a stable release and has not yet been tested inside a live Jellyfin instance.

## Features implemented

- Local SQLite storage in the plugin data directory; no extra Docker container.
- Tracking toggle disabled by default; data capture begins only after enabling it.
- Per-user annual report; user activity is isolated.
- Films and TV episodes only (other item types are excluded from recap totals).
- Stats for playback time/counts, monthly activity, top films and series, genres, decades, UTC patterns, active days/streaks, completion, repeats and personal records.
- Offline HTML export with optional embedded artwork.
- Admin-only configuration, preview and export.
- Packaged SQLite managed dependencies and Linux x64 native runtime.

## Compatibility

Built against Jellyfin Server 12.4.0 API packages and .NET 10. The target server is Jellyfin 12.4 running in Docker on Synology. The current validation package targets Linux x64. Actual event capture, configuration-page behavior, and export still require a reversible live-server test.

## Known limitations

- No retroactive history; capture starts when the plugin is installed and tracking is enabled.
- Time/day/hour groupings use UTC.
- Playback event timing, pause/resume/seek behavior, report totals, and offline HTML have not yet been validated against a real Jellyfin server.
- No stable release or public plugin repository manifest yet.

## Development

See [docs/PROJECT_PLAN.md](docs/PROJECT_PLAN.md) for progress, [docs/REAL_SERVER_VALIDATION.md](docs/REAL_SERVER_VALIDATION.md) for the live-server test checklist, and [docs/MOBILE_VALIDATION_AND_ROLLBACK.md](docs/MOBILE_VALIDATION_AND_ROLLBACK.md) for phone-friendly package download and rollback steps.

**Do not enable tracking until the administrator has reviewed the configuration page and is ready to perform the short validation test.**

## License

License selection and distribution files are still pending.
