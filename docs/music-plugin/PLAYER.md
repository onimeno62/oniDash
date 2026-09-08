# Music Player Specification

The player is global and persists across navigation. States: idle, loading, playing, paused, buffering, ended, error.

State: currentTrackId, queue, queueIndex, position, duration, volume, muted, shuffle, repeatMode, playbackRate, crossfadeEnabled.

Controls: play, pause, previous, next, seek, volume, mute, shuffle, repeat, queue management, and clear queue. Starting an album creates an ordered queue; shuffle preserves the canonical queue. Repeat modes are off, track, and queue. Previous restarts the current track after a position threshold, otherwise goes to the previous queue item.

Record history after a meaningful threshold with startedAt, completedAt, playedSeconds, completionRatio, and source. Architect for gapless playback and degrade gracefully if unsupported. ReplayGain supports off, track, and album modes. Crossfade belongs in the audio engine, not the UI.

Persist queue, current track, position, volume, shuffle, and repeat mode with debounced persistence. Keyboard shortcuts: Space play/pause, Left/Right seek, Shift+Left/Right previous/next, M mute, S shuffle, R repeat. Respect focused inputs.

A failed track must not crash the player. Offer retry, skip, remove from queue, and a file error view.
