import { NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import { authApi } from '@/api';
import { clsx } from 'clsx';

/**
 * SIDEBAR — The left navigation panel.
 *
 * Features:
 * - Logo + app name at top
 * - Role-based navigation items (Admin sees Users page, others don't)
 * - Collapse to icon-only mode
 * - User avatar + logout at the bottom
 */

// Navigation items definition
// 'roles: null' = visible to everyone
const NAV_ITEMS = [
  {
    label: 'Dashboard',
    to: '/dashboard',
    icon: (
      <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
      </svg>
    ),
    roles: null,
  },
  {
    label: 'Projects',
    to: '/projects',
    icon: (
      <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
      </svg>
    ),
    roles: null,
  },
  {
    label: 'Tasks',
    to: '/tasks',
    icon: (
      <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
      </svg>
    ),
    roles: null,
  },
  {
    label: 'Users',
    to: '/users',
    icon: (
      <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
      </svg>
    ),
    roles: ['Admin'],
  },
  {
    label: 'Notifications',
    to: '/notifications',
    icon: (
      <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
      </svg>
    ),
    roles: null,
  },
];

export default function Sidebar() {
  const { sidebarOpen, sidebarCollapsed, collapseSidebar } = useUIStore();
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await authApi.logout(useAuthStore.getState().refreshToken);
    } finally {
      logout();
      navigate('/login');
    }
  };

  const width = sidebarCollapsed ? 72 : 260;

  return (
    <aside
      style={{ width, minWidth: width }}
      className={clsx(
        'fixed left-0 top-0 h-full z-30 flex flex-col',
        'bg-surface-800/95 backdrop-blur-xl border-r border-white/8',
        'transition-all duration-300',
        !sidebarOpen && '-translate-x-full lg:translate-x-0'
      )}
    >
      {/* ── Logo ──────────────────────────────────────────── */}
      <div className="h-16 flex items-center justify-between px-4 border-b border-white/8 flex-shrink-0">
        <div className="flex items-center gap-3 overflow-hidden">
          <div className="w-8 h-8 rounded-lg bg-primary-500 flex items-center justify-center flex-shrink-0 shadow-glow-sm">
            <svg className="w-5 h-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5}
                d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
            </svg>
          </div>
          {!sidebarCollapsed && (
            <span className="text-lg font-bold gradient-text whitespace-nowrap">TaskFlow</span>
          )}
        </div>

        {/* Collapse button */}
        <button
          onClick={collapseSidebar}
          className="p-1.5 rounded-lg hover:bg-white/10 text-white/40 hover:text-white transition-all"
          title={sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          <svg className={clsx('w-4 h-4 transition-transform', sidebarCollapsed && 'rotate-180')}
            fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 19l-7-7 7-7m8 14l-7-7 7-7" />
          </svg>
        </button>
      </div>

      {/* ── Navigation ────────────────────────────────────── */}
      <nav className="flex-1 overflow-y-auto py-4 px-3 space-y-1">
        {NAV_ITEMS.filter(item =>
          !item.roles || item.roles.includes(user?.roleName)
        ).map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            title={sidebarCollapsed ? item.label : undefined}
            className={({ isActive }) =>
              clsx('nav-item', isActive && 'active', sidebarCollapsed && 'justify-center')
            }
          >
            <span className="flex-shrink-0">{item.icon}</span>
            {!sidebarCollapsed && (
              <span className="transition-all duration-200">{item.label}</span>
            )}
          </NavLink>
        ))}
      </nav>

      {/* ── User Profile + Logout ─────────────────────────── */}
      <div className="border-t border-white/8 p-3 flex-shrink-0">
        {/* Profile link */}
        <NavLink
          to="/profile"
          className={({ isActive }) =>
            clsx('nav-item mb-1', isActive && 'active', sidebarCollapsed && 'justify-center')
          }
        >
          {user?.avatarUrl ? (
            <img src={user.avatarUrl} alt={user.fullName}
              className="w-7 h-7 rounded-full object-cover flex-shrink-0 ring-2 ring-primary-500/30" />
          ) : (
            <div className="w-7 h-7 rounded-full bg-primary-500/30 flex items-center justify-center flex-shrink-0">
              <span className="text-xs font-semibold text-primary-300">
                {user?.fullName?.charAt(0) ?? '?'}
              </span>
            </div>
          )}
          {!sidebarCollapsed && (
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-white truncate">{user?.fullName}</p>
              <p className="text-xs text-white/40 truncate">{user?.roleName}</p>
            </div>
          )}
        </NavLink>

        {/* Logout button */}
        <button
          onClick={handleLogout}
          className={clsx('nav-item w-full text-red-400/70 hover:text-red-400 hover:bg-red-500/10',
            sidebarCollapsed && 'justify-center')}
          title="Logout"
        >
          <svg className="w-5 h-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
              d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
          </svg>
          {!sidebarCollapsed && <span>Logout</span>}
        </button>
      </div>
    </aside>
  );
}
