import { fetchMusicPlaybackState, recordMusicPlay, saveMusicPlaybackState, type TrackSummary } from '../api/music';

export type RepeatMode = 'off' | 'track' | 'queue';
export interface PlayerSnapshot { current: TrackSummary | null; playing: boolean; position: number; duration: number; queue: TrackSummary[]; queueIndex: number; repeatMode: RepeatMode; shuffle: boolean; gapless: boolean; volume: number; muted: boolean; error: string | null; }
type Listener = () => void;
const KEY = 'onidash.player.v3';

class PlayerService {
  private audio = new Audio();
  private listeners = new Set<Listener>();
  private state: PlayerSnapshot = { current: null, playing: false, position: 0, duration: 0, queue: [], queueIndex: 0, repeatMode: 'off', shuffle: false, gapless: false, volume: 1, muted: false, error: null };
  private lastPersist = 0;

  constructor() {
    this.audio.preload = 'auto';
    this.audio.addEventListener('play', () => this.patch({ playing: true, error: null }));
    this.audio.addEventListener('pause', () => this.patch({ playing: false }));
    this.audio.addEventListener('timeupdate', () => { this.patch({ position: this.audio.currentTime }); this.persistPlayback(false); });
    this.audio.addEventListener('loadedmetadata', () => this.patch({ duration: Number.isFinite(this.audio.duration) ? this.audio.duration : this.state.current?.durationSeconds ?? 0 }));
    this.audio.addEventListener('ended', () => void this.advance(true));
    this.audio.addEventListener('error', () => this.patch({ playing: false, error: 'The audio stream could not be loaded or decoded.' }));
    this.restore();
    this.installMediaSession();
  }

  private installMediaSession() {
    if (!('mediaSession' in navigator)) return;
    const session = navigator.mediaSession;
    const bind = (action: MediaSessionAction, handler: () => void) => { try { session.setActionHandler(action, handler); } catch { /* browser does not support this action */ } };
    bind('play', () => { void this.audio.play().catch(() => this.patch({ error: 'Playback could not start.' })); });
    bind('pause', () => this.audio.pause());
    bind('previoustrack', () => this.previous());
    bind('nexttrack', () => void this.advance());
    bind('seekbackward', () => this.seek(this.state.position - 10));
    bind('seekforward', () => this.seek(this.state.position + 10));
    bind('seekto', () => undefined);
  }

  private updateMediaSession() {
    if (!('mediaSession' in navigator)) return;
    if (!this.state.current) { navigator.mediaSession.metadata = null; navigator.mediaSession.playbackState = 'none'; return; }
    const track = this.state.current;
    try {
      navigator.mediaSession.metadata = new MediaMetadata({ title: track.title, artist: track.artistName ?? 'Unknown artist', album: track.albumTitle ?? 'Unknown album', artwork: track.albumId && track.hasCover ? [{ src: `/api/music/albums/${track.albumId}/cover` }] : [] });
      navigator.mediaSession.playbackState = this.state.playing ? 'playing' : 'paused';
    } catch { /* media session metadata is progressive enhancement */ }
  }

  private restore() {
    try {
      const value = JSON.parse(localStorage.getItem(KEY) ?? 'null') as Partial<PlayerSnapshot> | null;
      if (value?.queue?.length) this.state = { ...this.state, ...value, queue: value.queue, queueIndex: Math.min(value.queueIndex ?? 0, value.queue.length - 1), current: value.queue[value.queueIndex ?? 0] ?? null };
      this.audio.volume = this.state.volume; this.audio.muted = this.state.muted;
    } catch { /* corrupt local player state is disposable */ }
  }
  private patch(partial: Partial<PlayerSnapshot>) { this.state = { ...this.state, ...partial }; this.updateMediaSession(); for (const listener of this.listeners) listener(); }
  private persist() { localStorage.setItem(KEY, JSON.stringify({ queue: this.state.queue, queueIndex: this.state.queueIndex, position: this.state.position, repeatMode: this.state.repeatMode, shuffle: this.state.shuffle, gapless: this.state.gapless, volume: this.state.volume, muted: this.state.muted })); }
  private persistPlayback(force: boolean) {
    if (!this.state.current) return;
    const now = Date.now(); if (!force && now - this.lastPersist < 1500) return;
    this.lastPersist = now; this.persist(); void saveMusicPlaybackState(this.state.current.id, this.state.position, false).catch(() => undefined);
  }
  subscribe(listener: Listener) { this.listeners.add(listener); return () => this.listeners.delete(listener); }
  snapshot() { return this.state; }
  getAudioElement() { return this.audio; }
  playQueue(tracks: TrackSummary[], index = 0) { if (!tracks.length) return; const safe = Math.max(0, Math.min(index, tracks.length - 1)); this.state = { ...this.state, queue: tracks, queueIndex: safe, current: tracks[safe], position: 0, error: null }; this.loadCurrent(true); }
  toggle(track: TrackSummary) { if (this.state.current?.id === track.id) { if (this.audio.paused) void this.audio.play().catch(() => this.patch({ error: 'Playback was blocked by the browser.' })); else this.audio.pause(); return; } const index = this.state.queue.findIndex(item => item.id === track.id); this.playQueue(index >= 0 ? this.state.queue : [track], index >= 0 ? index : 0); }
  private loadCurrent(autoplay: boolean) {
    const track = this.state.current; if (!track) return;
    this.audio.src = `/api/music/tracks/${encodeURIComponent(track.id)}/stream`;
    this.audio.currentTime = Math.max(0, this.state.position);
    this.patch({ duration: track.durationSeconds ?? 0, error: null }); this.persist();
    void fetchMusicPlaybackState(track.id).then(saved => {
      if (!saved || this.state.current?.id !== track.id || this.audio.currentTime > 0.5) return;
      const position = saved.completed ? 0 : Math.max(0, saved.positionSeconds); this.audio.currentTime = position; this.patch({ position });
    }).catch(() => undefined);
    if (autoplay) void this.audio.play().catch(() => this.patch({ playing: false, error: 'Playback could not start. Interact with the player and try again.' }));
  }
  enqueue(track: TrackSummary) { if (this.state.queue.some(item => item.id === track.id)) return; this.patch({ queue: [...this.state.queue, track] }); this.persist(); }
  playNext(track: TrackSummary) { const queue = this.state.queue.filter(item => item.id !== track.id); const at = Math.min(this.state.queueIndex + 1, queue.length); this.patch({ queue: [...queue.slice(0, at), track, ...queue.slice(at)] }); this.persist(); }
  async advance(fromEnded = false) {
    if (!this.state.queue.length) return;
    if (fromEnded && this.state.current) { const played = Math.max(0, this.state.position); void recordMusicPlay(this.state.current.id, played, this.state.duration > 0 ? Math.min(1, played / this.state.duration) : 1).catch(() => undefined); void saveMusicPlaybackState(this.state.current.id, this.state.duration || played, true).catch(() => undefined); }
    if (this.state.repeatMode === 'track' && this.state.current) { this.patch({ position: 0 }); this.loadCurrent(true); return; }
    let next = this.state.shuffle && this.state.queue.length > 1 ? Math.floor(Math.random() * this.state.queue.length) : this.state.queueIndex + 1;
    if (next >= this.state.queue.length) { if (this.state.repeatMode !== 'queue') { this.patch({ playing: false }); return; } next = 0; }
    this.patch({ queueIndex: next, current: this.state.queue[next], position: 0 }); this.loadCurrent(true);
  }
  previous() { if (this.audio.currentTime > 5) { this.seek(0); return; } const next = Math.max(0, this.state.queueIndex - 1); if (this.state.queue[next]) { this.patch({ queueIndex: next, current: this.state.queue[next], position: 0 }); this.loadCurrent(true); } }
  seek(seconds: number) { if (!Number.isFinite(seconds)) return; const value = Math.max(0, Math.min(seconds, this.state.duration || Number.MAX_SAFE_INTEGER)); this.audio.currentTime = value; this.patch({ position: value }); this.persistPlayback(true); }
  setShuffle(enabled: boolean) { this.patch({ shuffle: enabled }); this.persist(); }
  cycleRepeat() { const repeatMode = this.state.repeatMode === 'off' ? 'track' : this.state.repeatMode === 'track' ? 'queue' : 'off'; this.patch({ repeatMode }); this.persist(); }
  setGapless(enabled: boolean) { this.patch({ gapless: enabled }); this.persist(); }
  setVolume(value: number) { const volume = Math.max(0, Math.min(1, value)); this.audio.volume = volume; this.patch({ volume, muted: volume === 0 ? true : this.state.muted }); this.persist(); }
  setMuted(muted: boolean) { this.audio.muted = muted; this.patch({ muted }); this.persist(); }
  clearQueue() { const keep = this.state.current ? [this.state.current] : []; this.patch({ queue: keep, queueIndex: 0 }); this.persist(); }
  removeFromQueue(trackId: string) { const index = this.state.queue.findIndex(item => item.id === trackId); if (index < 0) return; const queue = this.state.queue.filter(item => item.id !== trackId); const queueIndex = index < this.state.queueIndex ? this.state.queueIndex - 1 : Math.min(this.state.queueIndex, Math.max(0, queue.length - 1)); this.patch({ queue, queueIndex }); this.persist(); }
  stop() { this.audio.pause(); this.audio.removeAttribute('src'); this.audio.load(); if (this.state.current) void saveMusicPlaybackState(this.state.current.id, this.state.position, false).catch(() => undefined); this.patch({ current: null, playing: false, position: 0, duration: 0, queue: [], queueIndex: 0, error: null }); this.persist(); }
}

export const playerService = new PlayerService();
