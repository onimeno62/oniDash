import type { SVGProps } from 'react';

/**
 * Hand-rolled inline icon set (stroke-based, 24×24 grid). Inline SVG keeps the app
 * offline-friendly (local-first) and avoids an icon-library dependency.
 */

type IconProps = SVGProps<SVGSVGElement>;

function withDefaults(props: IconProps): IconProps {
  return {
    width: 20,
    height: 20,
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 1.8,
    strokeLinecap: 'round',
    strokeLinejoin: 'round',
    'aria-hidden': true,
    focusable: false,
    ...props,
  };
}

export function DashboardIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <rect x="3.5" y="3.5" width="7" height="7" rx="2" />
      <rect x="13.5" y="3.5" width="7" height="7" rx="2" />
      <rect x="3.5" y="13.5" width="7" height="7" rx="2" />
      <rect x="13.5" y="13.5" width="7" height="7" rx="2" />
    </svg>
  );
}

export function LibraryIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M4 4.5h3.5A1.5 1.5 0 0 1 9 6v13.5H5.5A1.5 1.5 0 0 1 4 18V4.5Z" />
      <path d="M9 6a1.5 1.5 0 0 1 1.5-1.5H14A1.5 1.5 0 0 1 15.5 6v13.5H9" />
      <path d="m17.2 6.4 2.6 12.2" />
    </svg>
  );
}

export function SearchIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <circle cx="11" cy="11" r="6.5" />
      <path d="m20 20-4.4-4.4" />
    </svg>
  );
}

export function ActivityIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M3 12h4l2.5-6.5 4.5 13L16.5 12H21" />
    </svg>
  );
}

export function SlidersIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M5 4v6m0 4v6" />
      <path d="M12 4v2m0 4v10" />
      <path d="M19 4v10m0 4v2" />
      <circle cx="5" cy="12" r="2" />
      <circle cx="12" cy="8" r="2" />
      <circle cx="19" cy="16" r="2" />
    </svg>
  );
}

export function MenuIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M4 7h16" />
      <path d="M4 12h16" />
      <path d="M4 17h16" />
    </svg>
  );
}

export function CloseIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="m6 6 12 12" />
      <path d="m18 6-12 12" />
    </svg>
  );
}

export function SunIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2.5v2M12 19.5v2M2.5 12h2M19.5 12h2M5 5l1.4 1.4M17.6 17.6 19 19M19 5l-1.4 1.4M6.4 17.6 5 19" />
    </svg>
  );
}

export function MoonIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M20 14.5A8 8 0 0 1 9.5 4 8 8 0 1 0 20 14.5Z" />
    </svg>
  );
}

export function RefreshIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M20 12a8 8 0 1 1-2.34-5.66" />
      <path d="M20 4v4h-4" />
    </svg>
  );
}

export function DatabaseIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <ellipse cx="12" cy="5.5" rx="7.5" ry="2.5" />
      <path d="M4.5 5.5v13c0 1.38 3.36 2.5 7.5 2.5s7.5-1.12 7.5-2.5v-13" />
      <path d="M4.5 12c0 1.38 3.36 2.5 7.5 2.5s7.5-1.12 7.5-2.5" />
    </svg>
  );
}

export function ClockIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <circle cx="12" cy="12" r="8.5" />
      <path d="M12 7.5V12l3 2" />
    </svg>
  );
}

export function CheckCircleIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <circle cx="12" cy="12" r="8.5" />
      <path d="m8.5 12.2 2.4 2.4 4.6-5.2" />
    </svg>
  );
}

export function AlertIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M12 4 2.8 19.5h18.4L12 4Z" />
      <path d="M12 10v4" />
      <path d="M12 16.8v.2" />
    </svg>
  );
}

export function InboxIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M4 13.5 6.6 5.3A2 2 0 0 1 8.5 4h7a2 2 0 0 1 1.9 1.3L20 13.5V18a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-4.5Z" />
      <path d="M4 13.5h4.5l1 2h5l1-2H20" />
    </svg>
  );
}

export function ArrowRightIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M4.5 12h15" />
      <path d="m13.5 6 6 6-6 6" />
    </svg>
  );
}

export function PlusIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M12 5v14" />
      <path d="M5 12h14" />
    </svg>
  );
}

export function CheckIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="m5 12.5 4.5 4.5L19 7.5" />
    </svg>
  );
}

export function PencilIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M4 20h4.5L20 8.5a2.1 2.1 0 0 0-3-3L5.5 17 4 20Z" />
      <path d="m14.5 7 3 3" />
    </svg>
  );
}

export function TrashIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M4.5 6.5h15" />
      <path d="M8.5 6.5V5a1.5 1.5 0 0 1 1.5-1.5h4A1.5 1.5 0 0 1 15.5 5v1.5" />
      <path d="M6.5 6.5 7.5 19a2 2 0 0 0 2 1.8h5a2 2 0 0 0 2-1.8l1-12.5" />
      <path d="M10 10.5v6" />
      <path d="M14 10.5v6" />
    </svg>
  );
}

export function FolderIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M3.5 7A2.5 2.5 0 0 1 6 4.5h3.2a2 2 0 0 1 1.6.8l1 1.2h6.2A2.5 2.5 0 0 1 20.5 9v8A2.5 2.5 0 0 1 18 19.5H6A2.5 2.5 0 0 1 3.5 17V7Z" />
    </svg>
  );
}

export function MusicIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <circle cx="7" cy="17.5" r="2.5" />
      <circle cx="17" cy="15.5" r="2.5" />
      <path d="M9.5 17.5V6.5l10-2v11" />
    </svg>
  );
}

export function PlayIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <path d="M8 5.8a1 1 0 0 1 1.53-.85l9.3 6.2a1 1 0 0 1 0 1.7l-9.3 6.2A1 1 0 0 1 8 18.2V5.8Z" />
    </svg>
  );
}

export function FilmIcon(props: IconProps) {
  return (
    <svg {...withDefaults(props)}>
      <rect x="3.5" y="5.5" width="17" height="13" rx="2" />
      <path d="M8 5.5v13" />
      <path d="M16 5.5v13" />
      <path d="M3.5 9.5h4.5" />
      <path d="M3.5 14.5h4.5" />
      <path d="M16 9.5h4.5" />
      <path d="M16 14.5h4.5" />
    </svg>
  );
}
