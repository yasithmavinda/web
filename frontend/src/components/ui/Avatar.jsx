import { clsx } from 'clsx';

/**
 * AVATAR — User profile picture with fallback initials.
 */
export default function Avatar({ user, size = 'md', className, showStatus = false }) {
  const sizeClasses = {
    xs: 'w-6  h-6  text-xs',
    sm: 'w-8  h-8  text-sm',
    md: 'w-10 h-10 text-sm',
    lg: 'w-12 h-12 text-base',
    xl: 'w-16 h-16 text-lg',
  }[size];

  const initials = user?.fullName
    ?.split(' ').slice(0, 2).map((n) => n[0]).join('').toUpperCase()
    ?? '?';

  return (
    <div className={clsx('relative flex-shrink-0', className)}>
      {user?.avatarUrl ? (
        <img
          src={user.avatarUrl}
          alt={user.fullName}
          className={clsx(sizeClasses, 'rounded-full object-cover ring-2 ring-white/10')}
        />
      ) : (
        <div className={clsx(
          sizeClasses,
          'rounded-full flex items-center justify-center font-semibold',
          'bg-gradient-to-br from-primary-500 to-violet-600 text-white',
          'ring-2 ring-white/10'
        )}>
          {initials}
        </div>
      )}

      {/* Online status dot */}
      {showStatus && (
        <span className="absolute bottom-0 right-0 w-2.5 h-2.5 bg-emerald-400 rounded-full
                         ring-2 ring-surface-900" />
      )}
    </div>
  );
}

/** Stacked avatars (e.g., assignee list) */
export function AvatarGroup({ users = [], max = 3, size = 'sm' }) {
  const visible  = users.slice(0, max);
  const overflow = users.length - max;

  const sizeClasses = { xs: 'w-6 h-6 -ml-1', sm: 'w-8 h-8 -ml-2', md: 'w-10 h-10 -ml-2' }[size];

  return (
    <div className="flex items-center">
      {visible.map((user, i) => (
        <div key={user.userId ?? i} className={clsx(sizeClasses, 'first:ml-0')}
          title={user.fullName}>
          <Avatar user={user} size={size} className="ring-2 ring-surface-800 rounded-full" />
        </div>
      ))}
      {overflow > 0 && (
        <div className={clsx(
          sizeClasses, 'rounded-full bg-white/10 flex items-center justify-center',
          'text-xs font-medium text-white/60 ring-2 ring-surface-800'
        )}>
          +{overflow}
        </div>
      )}
    </div>
  );
}
