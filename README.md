# El Arca Wrapped

A Jellyfin plugin for creating personalized annual viewing recaps for each server user.

El Arca Wrapped aims to turn a user's viewing activity into a polished, standalone annual recap: films, series, episodes, genres, viewing time, milestones and personal records.

## Project status

**Early development — not yet functional or installable.**

The initial documentation scaffold is on `main`. The first .NET plugin project targeting Jellyfin Server 12.4 is being developed on the `develop` branch.

## Planned principles

- Individual statistics per Jellyfin user; never mix users' private viewing data.
- Administrator-controlled year and user selection.
- Local storage, with SQLite planned in the plugin data directory.
- Standalone responsive HTML exports designed to work offline.
- Clear distinction between captured, imported and estimated historical data.
- No additional Docker service required.

## Compatibility

The initial project targets Jellyfin Server 12.4 and .NET 10, using Jellyfin 12.4.0 reference packages. Compatibility will be confirmed by CI and testing before any installation instructions are published.

## Development

See [docs/PROJECT_PLAN.md](docs/PROJECT_PLAN.md) for the roadmap and [src/Jellyfin.Plugin.ElArcaWrapped/README.md](src/Jellyfin.Plugin.ElArcaWrapped/README.md) for source status.

**Do not install this development scaffold on a live server.**

## License

GPL-3.0-or-later (license text to be added before distribution).
