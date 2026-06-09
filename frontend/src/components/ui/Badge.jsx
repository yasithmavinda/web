import { clsx } from 'clsx';

/**
 * BADGE — Status and priority display chips.
 */

const STATUS_CONFIG = {
  Backlog:    { bg: 'bg-slate-500/20',   text: 'text-slate-400',   dot: 'bg-slate-400'   },
  Todo:       { bg: 'bg-indigo-500/20',  text: 'text-indigo-400',  dot: 'bg-indigo-400'  },
  InProgress: { bg: 'bg-amber-500/20',   text: 'text-amber-400',   dot: 'bg-amber-400'   },
  InReview:   { bg: 'bg-violet-500/20',  text: 'text-violet-400',  dot: 'bg-violet-400'  },
  Done:       { bg: 'bg-emerald-500/20', text: 'text-emerald-400', dot: 'bg-emerald-400' },
  Blocked:    { bg: 'bg-red-500/20',     text: 'text-red-400',     dot: 'bg-red-400'     },
};

const PRIORITY_CONFIG = {
  Low:      { bg: 'bg-emerald-500/15', text: 'text-emerald-400', icon: '↓' },
  Medium:   { bg: 'bg-amber-500/15',   text: 'text-amber-400',   icon: '→' },
  High:     { bg: 'bg-orange-500/15',  text: 'text-orange-400',  icon: '↑' },
  Critical: { bg: 'bg-red-500/15',     text: 'text-red-400',     icon: '⚠' },
};

const ROLE_CONFIG = {
  Admin:          { bg: 'bg-primary-500/20', text: 'text-primary-400' },
  ProjectManager: { bg: 'bg-violet-500/20',  text: 'text-violet-400'  },
  Collaborator:   { bg: 'bg-sky-500/20',     text: 'text-sky-400'     },
};

export function StatusBadge({ status }) {
  const config = STATUS_CONFIG[status] ?? STATUS_CONFIG.Backlog;
  return (
    <span className={clsx('status-badge', config.bg, config.text)}>
      <span className={clsx('priority-dot', config.dot)} />
      {status}
    </span>
  );
}

export function PriorityBadge({ priority }) {
  const config = PRIORITY_CONFIG[priority] ?? PRIORITY_CONFIG.Medium;
  return (
    <span className={clsx('status-badge', config.bg, config.text)}>
      <span className="text-xs leading-none">{config.icon}</span>
      {priority}
    </span>
  );
}

export function RoleBadge({ roleName }) {
  const config = ROLE_CONFIG[roleName] ?? ROLE_CONFIG.Collaborator;
  return (
    <span className={clsx('status-badge', config.bg, config.text)}>
      {roleName}
    </span>
  );
}

/** Generic colored badge */
export default function Badge({ children, color = 'primary', className }) {
  const colors = {
    primary: 'bg-primary-500/20 text-primary-400',
    success: 'bg-emerald-500/20 text-emerald-400',
    warning: 'bg-amber-500/20 text-amber-400',
    danger:  'bg-red-500/20 text-red-400',
    gray:    'bg-white/10 text-white/60',
  };
  return (
    <span className={clsx('status-badge', colors[color], className)}>
      {children}
    </span>
  );
}
