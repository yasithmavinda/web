import { create } from 'zustand';

/**
 * SIGNALR STORE — Connection status and notification badge.
 *
 * Beginner: Separate from authStore so the connection status
 * can be read anywhere (e.g., a status indicator in the header)
 * without coupling it to authentication logic.
 *
 * Status values:
 *   'disconnected' → No active connection
 *   'connecting'   → Attempting first connection
 *   'connected'    → Healthy, receiving events
 *   'reconnecting' → Lost connection, retrying automatically
 */
export const useSignalRStore = create((set) => ({
  status:        'disconnected',
  error:         null,
  lastConnected: null,

  setStatus:        (status)        => set({ status }),
  setError:         (error)         => set({ error }),
  clearError:       ()              => set({ error: null }),
  setLastConnected: (date)          => set({ lastConnected: date }),
}));

/**
 * NOTIFICATION STORE — Global unread badge and new notification flag.
 *
 * Deliberately kept separate from signalRStore so components
 * only subscribed to the badge number don't re-render on
 * every connection status change.
 */
export const useNotificationStore = create((set, get) => ({
  unreadCount: 0,
  hasNew:      false,

  // Called when a NewNotification event arrives via SignalR
  increment: () => set((s) => ({
    unreadCount: s.unreadCount + 1,
    hasNew: true,
  })),

  // Called when NotificationCountUpdated event arrives
  setCount: (count) => set({ unreadCount: count }),

  // Called when user opens the notification panel
  clearNew: () => set({ hasNew: false }),

  // Called on logout
  reset: () => set({ unreadCount: 0, hasNew: false }),
}));

/**
 * PRESENCE STORE — Tracks which users are currently online.
 * Updated by "UserPresenceChanged" SignalR event.
 */
export const usePresenceStore = create((set) => ({
  onlineUsers: {}, // { [userId]: boolean }

  setUserOnline:  (userId, isOnline) => set((s) => ({
    onlineUsers: { ...s.onlineUsers, [userId]: isOnline },
  })),

  isOnline: (userId) => {
    const state = usePresenceStore.getState();
    return state.onlineUsers[userId] === true;
  },

  reset: () => set({ onlineUsers: {} }),
}));
