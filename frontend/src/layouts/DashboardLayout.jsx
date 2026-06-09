import { Outlet } from 'react-router-dom';
import { useEffect } from 'react';
import Sidebar from './Sidebar';
import Header from './Header';
import { useUIStore } from '@/store/uiStore';
import { useSignalR } from '@/hooks/useSignalR';
import { useUnreadCount } from '@/hooks/useNotifications';

/**
 * DASHBOARD LAYOUT — The main app shell with sidebar + header + content.
 *
 * Beginner: Every protected page (Dashboard, Tasks, Projects...) is rendered
 * INSIDE this layout. The Outlet is the "hole" where the current page appears.
 *
 * Layout:
 *   ┌─────────────┬────────────────────────────────┐
 *   │             │  Header (top bar)               │
 *   │   Sidebar   ├────────────────────────────────┤
 *   │             │  <Outlet /> (current page)     │
 *   └─────────────┴────────────────────────────────┘
 */
export default function DashboardLayout() {
  const { sidebarOpen, sidebarCollapsed } = useUIStore();

  // Start SignalR connection when the app shell mounts
  useSignalR();

  // Fetch initial unread notification count
  useUnreadCount();

  // Apply sidebar CSS variable for smooth transitions
  const sidebarWidth = sidebarCollapsed ? 72 : 260;

  return (
    <div className="flex min-h-screen bg-surface-900">
      {/* ── Sidebar ────────────────────────────────────────── */}
      <Sidebar />

      {/* ── Main Content Area ────────────────────────────────── */}
      <div
        className="flex-1 flex flex-col min-w-0 transition-all duration-300"
        style={{ marginLeft: sidebarOpen ? sidebarWidth : 0 }}
      >
        {/* Fixed top header */}
        <Header />

        {/* Page content */}
        <main className="flex-1 overflow-auto p-6" style={{ marginTop: 64 }}>
          <div className="animate-fade-in">
            <Outlet />
          </div>
        </main>
      </div>

      {/* ── Mobile overlay (closes sidebar on small screens) ─── */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-20 lg:hidden"
          onClick={() => useUIStore.getState().setSidebarOpen(false)}
        />
      )}
    </div>
  );
}
