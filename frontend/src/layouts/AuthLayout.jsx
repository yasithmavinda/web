import { Navigate, useLocation } from 'react-router-dom';
import { Outlet } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';

/**
 * AUTH LAYOUT — Full-screen centered design for Login/Register pages.
 *
 * If user is already logged in, redirect them to dashboard.
 * Layout: Left panel (brand/features) + Right panel (form)
 */
export default function AuthLayout() {
  const { isAuth } = useAuthStore();
  const location   = useLocation();
  const from       = location.state?.from?.pathname ?? '/dashboard';

  // Already logged in → go to where they came from (or dashboard)
  if (isAuth) return <Navigate to={from} replace />;

  return (
    <div className="min-h-screen flex">
      {/* ── Left: Brand Panel ───────────────────────────────── */}
      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden bg-gradient-to-br from-surface-950 via-primary-950 to-surface-900">
        {/* Glow effects */}
        <div className="absolute top-1/4 left-1/4 w-72 h-72 bg-primary-500/20 rounded-full blur-3xl" />
        <div className="absolute bottom-1/4 right-1/4 w-56 h-56 bg-violet-500/20 rounded-full blur-3xl" />

        <div className="relative z-10 flex flex-col justify-between p-12 w-full">
          {/* Logo */}
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-primary-500 flex items-center justify-center shadow-glow">
              <svg className="w-6 h-6 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5}
                  d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
              </svg>
            </div>
            <span className="text-xl font-bold gradient-text">TaskFlow</span>
          </div>

          {/* Hero text */}
          <div>
            <h1 className="text-4xl font-bold text-white leading-tight mb-4">
              Manage tasks.<br />
              Ship faster.<br />
              <span className="gradient-text">Together.</span>
            </h1>
            <p className="text-white/60 text-lg leading-relaxed">
              The modern task management system built for fast-moving teams. Real-time collaboration, Kanban boards, and powerful reporting.
            </p>

            {/* Feature list */}
            <div className="mt-8 flex flex-col gap-3">
              {[
                { icon: '⚡', text: 'Real-time updates via SignalR' },
                { icon: '🎯', text: 'Kanban boards with drag & drop' },
                { icon: '👥', text: 'Role-based team management' },
                { icon: '📊', text: 'Project analytics & reporting' },
              ].map((f) => (
                <div key={f.text} className="flex items-center gap-3 text-white/70">
                  <span className="text-xl">{f.icon}</span>
                  <span className="text-sm font-medium">{f.text}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Footer */}
          <p className="text-white/30 text-xs">© 2025 TaskFlow. All rights reserved.</p>
        </div>
      </div>

      {/* ── Right: Form Panel ────────────────────────────────── */}
      <div className="flex-1 flex items-center justify-center p-6 bg-surface-900">
        <div className="w-full max-w-md animate-fade-in">
          {/* Mobile logo */}
          <div className="flex items-center gap-2 mb-8 lg:hidden">
            <div className="w-8 h-8 rounded-lg bg-primary-500 flex items-center justify-center">
              <svg className="w-5 h-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5}
                  d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
              </svg>
            </div>
            <span className="text-lg font-bold gradient-text">TaskFlow</span>
          </div>

          {/* The actual form (Login/Register/etc.) renders here */}
          <Outlet />
        </div>
      </div>
    </div>
  );
}
