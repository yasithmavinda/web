import { create } from 'zustand';

/**
 * UI STORE — Controls sidebar, modals, theme.
 *
 * Beginner: This store holds UI state that many components need.
 * For example, the sidebar toggle button is in the Header, but
 * the Sidebar itself needs to know if it's open — so we use a global store.
 */
export const useUIStore = create((set) => ({
  // ── Sidebar ───────────────────────────────────────────────
  sidebarOpen:      true,
  sidebarCollapsed: false,
  toggleSidebar:    ()  => set((s) => ({ sidebarOpen: !s.sidebarOpen })),
  collapseSidebar:  ()  => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
  setSidebarOpen:   (v) => set({ sidebarOpen: v }),

  // ── Theme ─────────────────────────────────────────────────
  theme:            'dark',  // 'dark' | 'light'
  toggleTheme:      () => set((s) => {
    const next = s.theme === 'dark' ? 'light' : 'dark';
    document.documentElement.classList.toggle('dark', next === 'dark');
    return { theme: next };
  }),

  // ── Active project (persists between navigations) ─────────
  activeProjectId:  null,
  setActiveProject: (id) => set({ activeProjectId: id }),

  // ── Global modal control ───────────────────────────────────
  modals: {
    createTask:    false,
    createProject: false,
    taskDetail:    null,  // taskId or null
  },
  openModal:  (name, payload = true) => set((s) => ({
    modals: { ...s.modals, [name]: payload },
  })),
  closeModal: (name) => set((s) => ({
    modals: { ...s.modals, [name]: false },
  })),
  closeAllModals: () => set({
    modals: { createTask: false, createProject: false, taskDetail: null },
  }),
}));

/**
 * NOTIFICATION STORE — Manages real-time notification badge state.
 * Updated by SignalR when the server pushes new notifications.
 */
export const useNotificationStore = create((set) => ({
  unreadCount: 0,
  hasNew:      false,

  setUnreadCount: (count) => set({ unreadCount: count }),
  increment:      ()      => set((s) => ({ unreadCount: s.unreadCount + 1, hasNew: true })),
  clearNew:       ()      => set({ hasNew: false }),
  reset:          ()      => set({ unreadCount: 0, hasNew: false }),
}));
