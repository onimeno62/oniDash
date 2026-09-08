import type { ComponentType, SVGProps } from 'react';
import { ActivityIcon, DashboardIcon, FilmIcon, LibraryIcon, MusicIcon, SearchIcon, SlidersIcon } from './icons';
export interface NavItem { to: string; label: string; description: string; icon: ComponentType<SVGProps<SVGSVGElement>>; end?: boolean; }
export const NAV_ITEMS: NavItem[] = [
  { to: '/', label: 'Dashboard', description: 'Overview of your oniDash library.', icon: DashboardIcon, end: true },
  { to: '/library', label: 'Library', description: 'Browse and manage your media collections.', icon: LibraryIcon },
  { to: '/music', label: 'Music', description: 'Artists, albums, and playback from your indexed audio.', icon: MusicIcon },
  { to: '/music/playlists', label: 'Playlists', description: 'Manual and smart Music playlists.', icon: MusicIcon },
  { to: '/movies', label: 'Movies', description: 'Your film collection with posters and resume playback.', icon: FilmIcon },
  { to: '/search', label: 'Search', description: 'Global search across your entire library.', icon: SearchIcon },
  { to: '/health', label: 'API Health', description: 'Connection status of the local oniDash API.', icon: ActivityIcon },
  { to: '/settings', label: 'Settings', description: 'Appearance, libraries, plugins, and playback.', icon: SlidersIcon },
];
const TITLES = new Map(NAV_ITEMS.map((item) => [item.to, item.label]));
export function pageTitleFor(pathname: string): string { return TITLES.get(pathname) ?? 'oniDash'; }
