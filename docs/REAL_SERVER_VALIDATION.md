# El Arca Wrapped — first real-server validation checklist

This checklist is for the first installation on the target Jellyfin server. It is intentionally a reversible, small-scale test. Do not enable tracking until the plugin has been installed and the administrator has reviewed the configuration page.

## Before installation

- [ ] Confirm the Jellyfin server version is compatible with the plugin build.
- [ ] Back up the Jellyfin configuration/data directory and note the current container image/tag.
- [ ] Keep the existing media library and metadata unchanged.
- [ ] Install only a build explicitly marked for this validation; do not replace the production image or update Jellyfin as part of the test.
- [ ] Confirm the plugin's tracking setting is **disabled** after installation.
- [ ] Verify that the configuration page loads, user list populates, and Preview/Export endpoints are available to an administrator.
- [ ] Confirm non-administrator accounts cannot access plugin endpoints.

## Minimal capture test

Choose a test user and one short film or episode. Keep the test playback device/client consistent.

1. [ ] Enable tracking; record the exact UTC time.
2. [ ] Start playback and let it run for approximately 2 minutes without seeking.
3. [ ] Pause for about 30 seconds; resume and play another minute.
4. [ ] Seek forward once by a noticeable amount, then play another minute.
5. [ ] Stop playback normally. Note the stop time and approximate media position.
6. [ ] Disable tracking after the test to prevent accidental capture during review.
7. [ ] Preview the selected test user's report for the current year.
8. [ ] Confirm there is one session for the test item, not multiple duplicate rows; watched time should be a reasonable approximation of active playback and should not include the pause or seek jump.
9. [ ] Export HTML and open it on a phone with network disconnected. Check layout, rankings, images, and text.

## Data-isolation and resilience checks

- [ ] Select a second user and confirm the first user's test playback is absent.
- [ ] Test an empty user/year: report should render with no-data wording and no fabricated statistics.
- [ ] Confirm missing artwork is handled as a text placeholder without export failure.
- [ ] Check the server log for plugin exceptions after start, progress, pause/resume, seek, and stop.
- [ ] Verify exported HTML is standalone: it should not request authenticated Jellyfin image URLs or other external assets.
- [ ] Confirm generated filenames are safe for user names containing punctuation/non-ASCII characters.

## Stop / rollback

- [ ] Disable tracking immediately if capture results look wrong.
- [ ] Remove the plugin and restore the Jellyfin configuration/data backup if the server becomes unstable.
- [ ] Do not publish or install broadly until the capture test, user isolation, and offline HTML test pass.

## Record results

Capture: Jellyfin version, plugin build/commit, client/device used, test item type, test sequence, approximate active watch time, report totals, log errors, and whether the exported HTML worked offline. Use this to decide which behavior needs adjustment before the first public release.
