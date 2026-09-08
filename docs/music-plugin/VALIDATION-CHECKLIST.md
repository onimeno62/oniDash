# Music plugin validation checklist

## Build and tests

- [ ] `dotnet test oniDash.sln --configuration Release`
- [ ] `npm ci` in `frontend/oniDash.Web`
- [ ] `npm run typecheck`
- [ ] `npm run test`
- [ ] `npm run build`

## Windows file safety

- [ ] Read-only MP3 returns a recoverable error and leaves bytes unchanged.
- [ ] Locked MP3 returns a recoverable error and leaves bytes unchanged.
- [ ] Failed TagLib parse leaves the original bytes unchanged.
- [ ] Successful write replaces the original only after the staged copy saves.
- [ ] Interrupt/restart behavior is tested on NTFS.

## Large-library performance

- [ ] 50,000-track fixture scans without UI blocking.
- [ ] Artist, album, track, playlist, and search responses stay bounded.
- [ ] Track list rendering uses virtualization before production-scale release.
- [ ] Search and smart-playlist queries are measured with representative indexes.

## Accessibility

- [ ] All Music and playlist actions are keyboard reachable.
- [ ] Dialogs trap focus, label controls, and close predictably.
- [ ] Player controls expose state through accessible names and pressed values.
- [ ] Reduced-motion preference is respected.
- [ ] Contrast and screen-reader announcements pass review.
