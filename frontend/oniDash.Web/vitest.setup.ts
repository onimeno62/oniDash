import '@testing-library/jest-dom/vitest';

// jsdom does not implement matchMedia; provide a deterministic stub that reports
// a light system preference unless a test overrides it.
if (typeof window !== 'undefined' && typeof window.matchMedia !== 'function') {
  window.matchMedia = ((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  })) as unknown as typeof window.matchMedia;
}

// jsdom implements no media playback: HTMLMediaElement.play() returns undefined, so the
// player's `play().catch(...)` chain throws an unhandled TypeError. Provide the playback
// surface every test needs, regardless of which file mounts the player.
if (typeof window !== 'undefined' && typeof window.HTMLMediaElement !== 'undefined') {
  window.HTMLMediaElement.prototype.play = function play(this: HTMLMediaElement) {
    return Promise.resolve();
  };
  window.HTMLMediaElement.prototype.pause = function pause(this: HTMLMediaElement) {};
  window.HTMLMediaElement.prototype.load = function load(this: HTMLMediaElement) {};
}
