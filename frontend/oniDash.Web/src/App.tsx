import { Route, Routes } from 'react-router-dom';
import { AppShell } from './components/AppShell';
import { DashboardPage } from './pages/DashboardPage';
import { LibraryPage } from './pages/LibraryPage';
import { MoviesDashboardPage } from './pages/MoviesDashboardPage';
import { BooksDashboardPage } from './pages/BooksDashboardPage';
import { MusicPage } from './pages/MusicPage';
import { MusicInsightsPage } from './pages/MusicInsightsPage';
import { PlaylistsPage } from './pages/PlaylistsPage';
import { SearchPage } from './pages/SearchPage';
import { HealthPage } from './pages/HealthPage';
import { SettingsPage } from './pages/SettingsPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { PlayerProvider } from './hooks/usePlayer';
export default function App() { return <PlayerProvider><Routes><Route element={<AppShell />}><Route index element={<DashboardPage />} /><Route path="library" element={<LibraryPage />} /><Route path="music" element={<MusicPage />} /><Route path="music/playlists" element={<PlaylistsPage />} /><Route path="music/insights" element={<MusicInsightsPage />} /><Route path="movies" element={<MoviesDashboardPage />} /><Route path="books" element={<BooksDashboardPage />} /><Route path="search" element={<SearchPage />} /><Route path="health" element={<HealthPage />} /><Route path="settings" element={<SettingsPage />} /><Route path="*" element={<NotFoundPage />} /></Route></Routes></PlayerProvider>; }
