# El Arca Wrapped

A Jellyfin plugin for creating personalized annual viewing recaps for each server user.

The report is designed to feel like a yearly recap: viewing time, films and episodes, series rankings, genres, viewing patterns, personal milestones and records, with a self-contained HTML export that can be opened offline.

## Project status

**Development build — not yet validated or installed on the live server.**

The `develop` branch currently contains the plugin, local SQLite playback capture, statistics, admin preview/export endpoints, and a responsive standalone HTML renderer. CI builds and unit tests are passing. The next milestone is packaging a validation build and a small, reversible test on the target Jellyfin server.

## Features implemented

- Local SQLite storage in the plugin data directory; no extra Docker container.
- Tracking toggle disabled by default; data capture begins only after enabling it.
- Per-user annual report; user activity is isolated.
- Films and TV episodes only (other item types are excluded from recap totals).
- Stats for playback time/counts, monthly activity, top films and series, genres, decades, UTC patterns, active days/streaks, completion, repeats and personal records.
- Offline HTML export with optional embedded artwork.
- Admin-only configuration, preview and export.

## Compatibility

Built against Jellyfin Server 12.4.0 API packages and .NET 10. The target server is Jellyfin 12.4 running in Docker on Synology. Compatibility and event capture still require live-server validation before installation is considered safe.

## Known limitations

- No retroactive history; capture starts when the plugin is installed and tracking is enabled.
- Time/day/hour groupings use UTC.
- Event behavior, report totals, and HTML export have not yet been validated against a real Jellyfin server.
- No stable release or public plugin repository manifest yet.

## Development

See [docs/PROJECT_PLAN.md](docs/PROJECT_PLAN.md) for progress and [docs/REAL_SERVER_VALIDATION.md](docs/REAL_SERVER_VALIDATION.md) for the cautious live-server test checklist.

**Do not install on the production server until the validation milestone is ready and the user is explicitly guided through it.**

## License

License selection and distribution files are still pending.
