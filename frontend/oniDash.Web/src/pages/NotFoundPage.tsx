import { Link } from 'react-router-dom';
import { EmptyState } from '../components/states/EmptyState';
import { ArrowRightIcon, SearchIcon } from '../components/icons';

export function NotFoundPage() {
  return (
    <EmptyState
      icon={<SearchIcon className="size-6" />}
      title="Page not found"
      description="The page you were looking for doesn’t exist or has moved."
      action={
        <Link
          to="/"
          className="inline-flex items-center gap-2 rounded-lg bg-accent px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-accent-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        >
          Back to dashboard
          <ArrowRightIcon className="size-4" />
        </Link>
      }
    />
  );
}
