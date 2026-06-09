import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { useUIStore, useNotificationStore } from '@/store/uiStore';
import { clsx } from 'clsx';

/**
 * HEADER — Top bar with menu toggle, search, notifications, user menu.
 */
export default function Header() {
  const { toggleSidebar } = useUIStore();
  const { user } = useAuthStore();
  const { unreadCount } = useNotificationStore();

  return (
    <header
      className={clsx(
        'fixed top-0 right-0 left-0 z-20 h-16',
        'bg-surface-900/80 backdrop-blur-xl border-b border-white/8',
        'flex items-center justify-between px-4 gap-4'
      )}
      style={{ paddingLeft: '16px' }}
    >
      {/* Left: Sidebar toggle */}
      <button
        onClick={toggleSidebar}
        className="p-2 rounded-lg hover:bg-white/10 text-white/60 hover:text-white transition-all"
        aria-label="Toggle sidebar"
      >
        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
        </svg>
      </button>

      {/* Center: Global search */}
      <div className="flex-1 max-w-sm hidden sm:block">
        <div className="relative">
          <svg className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30"
            fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            type="search"
            placeholder="Search tasks, projects..."
            className="w-full bg-white/5 border border-white/10 rounded-xl pl-9 pr-4 py-2 text-sm
                       text-white placeholder-white/30 focus:outline-none focus:border-primary-500/50
                       focus:bg-white/8 transition-all"
          />
        </div>
      </div>

      {/* Right: Actions */}
      <div className="flex items-center gap-2">
        {/* Notification bell */}
        <Link
          to="/notifications"
          className="relative p-2 rounded-lg hover:bg-white/10 text-white/60 hover:text-white transition-all"
          aria-label="Notifications"
        >
          <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
              d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
          </svg>
          {/* Unread badge */}
          {unreadCount > 0 && (
            <span className="absolute top-1 right-1 min-w-[18px] h-[18px] bg-primary-500 rounded-full
                             text-[10px] font-bold text-white flex items-center justify-center px-1
                             ring-2 ring-surface-900 animate-scale-in">
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          )}
        </Link>

        {/* Avatar */}
        <Link to="/profile" className="flex items-center gap-2 p-1.5 rounded-xl hover:bg-white/10 transition-all">
          {user?.avatarUrl ? (
            <img src={user.avatarUrl} alt={user.fullName}
              className="w-8 h-8 rounded-full object-cover ring-2 ring-primary-500/30" />
          ) : (
            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-primary-500 to-violet-600
                            flex items-center justify-center text-sm font-semibold text-white">
              {user?.fullName?.charAt(0) ?? '?'}
            </div>
          )}
          <div className="hidden sm:block text-left">
            <p className="text-sm font-medium text-white leading-none">{user?.fullName}</p>
            <p className="text-xs text-white/40 mt-0.5">{user?.roleName}</p>
          </div>
        </Link>
      </div>
    </header>
  );
}
