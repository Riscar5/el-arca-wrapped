# El Arca Wrapped — project plan

## Product goal

Build a Jellyfin plugin that generates a personalized annual viewing recap for each Jellyfin user, with a polished, story-driven visual presentation inspired by annual music recaps.

An administrator selects a year and one or more users, previews each recap and exports individual standalone HTML files. A ZIP batch export may be added later.

## Privacy and data integrity

- Calculate and export each user's data separately.
- Never expose one user's viewing activity in another user's recap.
- Keep processing local to the Jellyfin server.
- Do not invent narratives or statistics when data is missing.
- Label data coverage and distinguish plugin-captured, imported and estimated history.
- Historical playback detail may be unavailable before the plugin is installed.

## Planned architecture

- C# Jellyfin plugin, targeting the server's verified supported .NET/Jellyfin API versions.
- Playback event capture for start, progress, pause/resume, stop and completion.
- Use Jellyfin's existing primary artwork for films and series. For exported standalone HTML, fetch and embed thumbnail data (data URI) so images remain available offline; avoid depending on an authenticated Jellyfin URL in the final report.
- SQLite database stored in the plugin data directory.
- Statistics service for yearly aggregation.
- A user-scoped, serializable report data contract now packages display name, year, generated timestamp, and statistics for the future renderer.
- First self-contained HTML renderer implemented: mobile-responsive layout, monthly activity bars, top movie/series lists, records, coverage note, and HTML-escaped user/item text. Artwork embedding and richer visual story sections remain future improvements.
- Admin-only REST endpoints added for listing users, previewing a selected annual report, and downloading the standalone HTML export.
- Plugin configuration page now includes user/year selection and mobile-friendly preview/download actions. Endpoint/API integration validation is pending CI.
- Administrator interface for year/user selection, preview and export.
- Standalone responsive HTML with embedded CSS, JavaScript and required data; offline use is a core requirement.

## Planned statistics

Viewing hours and counts; films, episodes and series; active days; viewing patterns by month/day/hour; averages and streaks; top films and series; genres; decades; languages/countries when metadata is reliable; replays; completions; marathons and personal records.

## Milestones

### Implemented on `develop` (in progress)

- SQLite playback-session capture scaffold and database initialization tests.
- First pure annual-statistics calculator, separated from Jellyfin event APIs for easier testing.
- Playback rows now include metadata columns for genre, year, season/episode numbers, and a relative primary-image endpoint. The SQLite initializer adds these columns idempotently for existing installations. Image embedding into standalone HTML remains a future export-stage task.
- Initial metrics: play count, watched minutes (runtime-capped), movie/episode counts, unique movies/series, active days, longest consecutive-day streak, completions, monthly activity, top movies/series and genres.
- Automated tests cover user isolation, year filtering, repeated plays, runtime capping, empty years, monthly/streak calculations, completion rate, daily totals, and UTC hour/weekday distributions. CI validation is required before these metrics are considered verified.
- Expanded metrics: average minutes per play, repeated plays (repeat sessions beyond the first play of each item), longest single-day viewing time, longest individual play (with item name), completion rate, start-time patterns by hour and weekday, film activity by decade, and captured activity coverage dates/months.
- Hour and weekday breakdowns are explicitly UTC. Year selection, month/day/hour/weekday buckets, active-day grouping and streak calculations normalize timestamps to UTC. Reports must not label them as local time until the administrator's Jellyfin/server timezone is wired in.
- Regression tests now cover UTC year-boundary offsets and elapsed-duration fallback with runtime capping. Database migration tests verify legacy-schema upgrades are idempotent and preserve existing rows.



1. Verify Jellyfin server version and official plugin template/API compatibility.
2. Create the compilable plugin project and CI build.
3. Implement playback event capture and SQLite persistence.
4. Implement per-user, per-year statistics and coverage reporting.
5. Build the administrator interface and preview.
6. Create the visual recap and offline HTML export.
7. Test on the target Synology Docker deployment and publish a release.

## Initial constraints

- No additional Docker container or external service.
- No Telegram bot in the initial release; exports can be shared manually.
- No user comparisons or PDF export in the initial scope.
- Favor reversible tests and avoid modifying Jellyfin's existing data.
