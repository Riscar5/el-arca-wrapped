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
- SQLite database stored in the plugin data directory.
- Statistics service for yearly aggregation.
- Administrator interface for year/user selection, preview and export.
- Standalone responsive HTML with embedded CSS, JavaScript and required data; offline use is a core requirement.

## Planned statistics

Viewing hours and counts; films, episodes and series; active days; viewing patterns by month/day/hour; averages and streaks; top films and series; genres; decades; languages/countries when metadata is reliable; replays; completions; marathons and personal records.

## Milestones

### Implemented on `develop` (in progress)

- SQLite playback-session capture scaffold and database initialization tests.
- First pure annual-statistics calculator, separated from Jellyfin event APIs for easier testing.
- Initial metrics: play count, watched minutes (runtime-capped), movie/episode counts, unique movies/series, active days, longest consecutive-day streak, completions, monthly activity, top movies/series and genres.
- Automated tests cover user isolation, year filtering, repeated plays, runtime capping, empty years and monthly/streak calculations. CI validation is running/required before these metrics are considered verified.


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
