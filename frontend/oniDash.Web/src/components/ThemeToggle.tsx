import { MoonIcon, SunIcon } from './icons';
import { useTheme, type ResolvedTheme } from '../theme/ThemeProvider';

/** Quick dark↔light toggle; Dark/Light/System choice lives in Settings → Appearance. */
export function ThemeToggle() {
  const { resolvedTheme, setTheme } = useTheme();
  const next: ResolvedTheme = resolvedTheme === 'dark' ? 'light' : 'dark';

  return (
    <button
      type="button"
      className="icon-btn"
      aria-label={`Switch to ${next} theme`}
      title={`Switch to ${next} theme`}
      onClick={() => setTheme(next)}
    >
      {resolvedTheme === 'dark' ? <SunIcon /> : <MoonIcon />}
    </button>
  );
}
