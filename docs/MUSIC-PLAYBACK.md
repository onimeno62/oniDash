# Music playback boundary

The web player uses two HTMLAudioElement instances. One is active and audible; the other preloads the next queued track. On track completion, the preloaded element becomes active and the old element is stopped, avoiding the source replacement gap of a single-element player.

Gapless mode is opt-in and persisted locally with the queue settings. It falls back to normal sequential loading when the next track cannot be preloaded, the queue ends, or repeat mode is disabled. This remains browser-limited: codec support, buffering, and network/file-server behavior can still introduce a gap.
