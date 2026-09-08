import { Link } from 'react-router-dom';
import { useHealth } from '../hooks/useHealth';

const CHECKING = 'checking the API connection';
const UP = 'the oniDash API is online';
const DOWN = 'the oniDash API is unreachable';

/** Live API connection indicator shown in the TopBar; links to the health page. */
export function ApiStatusDot() {
  const { data, loading, error } = useHealth(60_000);
  const state = loading ? 'checking' : error ? 'down' : 'up';
  const label = state === 'up' ? UP : state === 'down' ? DOWN : CHECKING;
  const dotColor =
    state === 'up' ? 'bg-success' : state === 'down' ? 'bg-danger' : 'bg-muted animate-pulse';

  return (
    <Link
      to="/health"
      aria-label={`API status: ${label}`}
      title={`API status: ${label}${data ? ` · v${data.version}` : ''}`}
      className="icon-btn"
    >
      <span aria-hidden className={`size-2.5 rounded-full transition-colors ${dotColor}`} />
    </Link>
  );
}
