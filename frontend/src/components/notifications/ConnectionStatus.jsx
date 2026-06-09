import { useSignalRStore } from '@/store/signalRStore';
import { clsx } from 'clsx';

/**
 * ConnectionStatus — Small indicator in the Header showing SignalR state.
 *
 * ● Green  = Connected
 * ● Yellow = Reconnecting
 * ● Red    = Disconnected / Error
 *
 * Appears as a tiny colored dot next to the notification bell.
 * On hover it shows a tooltip with the status message.
 */
export default function ConnectionStatus() {
  const { status, error, lastConnected } = useSignalRStore();

  const configs = {
    connected:    { dot: 'bg-emerald-400', pulse: false, label: 'Live — real-time connected',   tooltip: null           },
    connecting:   { dot: 'bg-amber-400',   pulse: true,  label: 'Connecting...',                 tooltip: null           },
    reconnecting: { dot: 'bg-amber-400',   pulse: true,  label: 'Reconnecting...',               tooltip: error          },
    disconnected: { dot: 'bg-red-400',     pulse: false, label: 'Disconnected',                  tooltip: error ?? 'Connection lost. Data may not be live.' },
  };

  const cfg = configs[status] ?? configs.disconnected;

  if (status === 'connected') {
    // Don't render anything when healthy — clean UI
    return null;
  }

  return (
    <div className="relative group flex items-center gap-2 px-3 py-1.5 rounded-xl
                    bg-white/5 border border-white/10 text-xs text-white/60">
      {/* Status dot */}
      <span className={clsx(
        'w-2 h-2 rounded-full flex-shrink-0',
        cfg.dot,
        cfg.pulse && 'animate-pulse'
      )} />

      {/* Label */}
      <span className="hidden sm:inline">{cfg.label}</span>

      {/* Tooltip */}
      {cfg.tooltip && (
        <div className="absolute top-full left-1/2 -translate-x-1/2 mt-2 z-50
                        w-64 px-3 py-2 bg-surface-800 border border-white/10 rounded-xl
                        text-xs text-white/60 shadow-xl hidden group-hover:block">
          {cfg.tooltip}
        </div>
      )}
    </div>
  );
}

/**
 * NotificationBell — Enhanced bell button that opens the NotificationPanel.
 * Used in Header.jsx
 */
export function NotificationBell({ onClick }) {
  const { status } = useSignalRStore();
  const { unreadCount, hasNew } = require('@/store/signalRStore').useNotificationStore();

  return (
    <button
      onClick={onClick}
      className="relative p-2 rounded-xl hover:bg-white/10 text-white/60 hover:text-white transition-all"
      aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`}
    >
      {/* Bell icon */}
      <svg className={clsx(
        'w-5 h-5 transition-all',
        hasNew && 'animate-bounce-once'
      )} fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
      </svg>

      {/* Unread badge */}
      {unreadCount > 0 && (
        <span className={clsx(
          'absolute top-0.5 right-0.5 min-w-[18px] h-[18px]',
          'bg-primary-500 text-white text-[10px] font-bold rounded-full',
          'flex items-center justify-center px-1',
          'ring-2 ring-surface-900',
          hasNew && 'animate-scale-in'
        )}>
          {unreadCount > 99 ? '99+' : unreadCount}
        </span>
      )}

      {/* Disconnected indicator dot on the bell */}
      {status === 'disconnected' && (
        <span className="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 bg-red-400 rounded-full
                         ring-2 ring-surface-900" title="Real-time disconnected" />
      )}
    </button>
  );
}
