import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '@/store/authStore';
import { useNotificationStore, useSignalRStore } from '@/store/signalRStore';
import toast from 'react-hot-toast';

/*
 * ╔══════════════════════════════════════════════════════════════════╗
 * ║          useSignalR — Complete Reconnection System               ║
 * ╠══════════════════════════════════════════════════════════════════╣
 * ║                                                                  ║
 * ║  CONNECTION LIFECYCLE:                                           ║
 * ║  ─────────────────────                                           ║
 * ║  Disconnected → Connecting → Connected                           ║
 * ║       ↑                          │                               ║
 * ║       └──── Reconnecting ←───────┘ (on disconnect)              ║
 * ║                                                                  ║
 * ║  RECONNECTION STRATEGY (exponential backoff):                    ║
 * ║    Attempt 1: wait 0s                                            ║
 * ║    Attempt 2: wait 2s                                            ║
 * ║    Attempt 3: wait 5s                                            ║
 * ║    Attempt 4: wait 10s                                           ║
 * ║    Attempt 5: wait 20s                                           ║
 * ║    Attempt 6+: wait 30s (cap)                                    ║
 * ║                                                                  ║
 * ║  OFFLINE QUEUE:                                                  ║
 * ║    Events missed while offline are NOT queued client-side.       ║
 * ║    On reconnect, React Query invalidates affected caches         ║
 * ║    and refetches fresh data from the API.                        ║
 * ║    Personal notifications are loaded from DB via API.            ║
 * ║                                                                  ║
 * ╚══════════════════════════════════════════════════════════════════╝
 */

// Custom retry policy: delays in milliseconds
const RETRY_DELAYS = [0, 2000, 5000, 10000, 20000, 30000];

class ExponentialBackoffRetryPolicy {
  nextRetryDelayInMilliseconds(context) {
    const idx = Math.min(context.previousRetryCount, RETRY_DELAYS.length - 1);
    return RETRY_DELAYS[idx];
  }
}

export function useSignalR() {
  const { accessToken, isAuth }   = useAuthStore();
  const { increment, setCount }   = useNotificationStore();
  const { setStatus, setError,
          clearError, setLastConnected } = useSignalRStore();
  const queryClient               = useQueryClient();

  // Persistent ref — survives re-renders without triggering effects
  const connectionRef    = useRef(null);
  const reconnectTimeout = useRef(null);
  const isCleaningUp     = useRef(false);

  // ── Build and start the connection ────────────────────────────
  const startConnection = useCallback(async () => {
    if (!isAuth || !accessToken || connectionRef.current) return;

    isCleaningUp.current = false;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        // Provide fresh token on every connection attempt (handles token refresh)
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',

        // Try WebSocket first, fall back to Long Polling
        // (Long Polling works through restrictive corporate firewalls)
        transport: signalR.HttpTransportType.WebSockets |
                   signalR.HttpTransportType.LongPolling,

        // Skip HTTPS requirement in development
        skipNegotiation: false,
      })
      .withAutomaticReconnect(new ExponentialBackoffRetryPolicy())
      .configureLogging(
        import.meta.env.DEV ? signalR.LogLevel.Information : signalR.LogLevel.Warning
      )
      .build();

    // ── Register all server → client event handlers ────────────
    registerEventHandlers(connection, queryClient, increment, setCount);

    // ── Connection lifecycle hooks ─────────────────────────────
    connection.onreconnecting((error) => {
      setStatus('reconnecting');
      setError(error?.message ?? 'Connection lost. Reconnecting...');
      console.warn('[SignalR] Reconnecting...', error?.message);
    });

    connection.onreconnected(async (connectionId) => {
      setStatus('connected');
      clearError();
      setLastConnected(new Date());
      console.info('[SignalR] Reconnected. ConnectionId:', connectionId);

      // On reconnect: invalidate all caches so UI shows fresh data
      // (we may have missed events while offline)
      await queryClient.invalidateQueries({ queryKey: ['tasks'] });
      await queryClient.invalidateQueries({ queryKey: ['notifications'] });
      await queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });

      toast.success('Connection restored', {
        id: 'signalr-reconnected',
        duration: 3000,
        icon: '🔄',
      });
    });

    connection.onclose((error) => {
      if (isCleaningUp.current) return; // Expected close, don't show error

      setStatus('disconnected');
      if (error) {
        setError('Real-time connection lost. Some features may be delayed.');
        console.error('[SignalR] Connection closed with error:', error);
      }
    });

    // ── Start! ─────────────────────────────────────────────────
    try {
      setStatus('connecting');
      await connection.start();
      connectionRef.current = connection;
      setStatus('connected');
      clearError();
      setLastConnected(new Date());
      console.info('[SignalR] Connected. Transport:', connection.connection?.transport?.name);
    } catch (err) {
      setStatus('disconnected');
      setError('Failed to connect to real-time server.');
      console.error('[SignalR] Initial connection failed:', err);

      // Schedule a manual retry after 5 seconds
      reconnectTimeout.current = setTimeout(() => {
        if (!isCleaningUp.current) startConnection();
      }, 5000);
    }
  }, [isAuth, accessToken]); // eslint-disable-line

  // ── Tear down connection ──────────────────────────────────────
  const stopConnection = useCallback(async () => {
    isCleaningUp.current = true;
    clearTimeout(reconnectTimeout.current);

    if (connectionRef.current) {
      try { await connectionRef.current.stop(); } catch { /* ignore */ }
      connectionRef.current = null;
    }

    setStatus('disconnected');
  }, []); // eslint-disable-line

  // ── Start on login, stop on logout ───────────────────────────
  useEffect(() => {
    if (isAuth && accessToken) {
      startConnection();
    } else {
      stopConnection();
    }
    return () => { stopConnection(); };
  }, [isAuth, accessToken]); // eslint-disable-line

  // ── Public API ────────────────────────────────────────────────
  const joinProject = useCallback(async (projectId) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('JoinProject', projectId);
    }
  }, []);

  const leaveProject = useCallback(async (projectId) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('LeaveProject', projectId);
    }
  }, []);

  const joinTask = useCallback(async (taskId) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('JoinTask', taskId);
    }
  }, []);

  const leaveTask = useCallback(async (taskId) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('LeaveTask', taskId);
    }
  }, []);

  return { joinProject, leaveProject, joinTask, leaveTask };
}

// ── EVENT HANDLER REGISTRY ────────────────────────────────────
function registerEventHandlers(connection, queryClient, increment, setCount) {

  // ── Personal notifications ─────────────────────────────────
  connection.on('NewNotification', (notification) => {
    increment();
    queryClient.invalidateQueries({ queryKey: ['notifications'] });

    // Show a rich toast with action button
    toast(
      (t) => (
        <div className="flex items-start gap-3">
          <span className="text-xl flex-shrink-0">🔔</span>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold text-white">{notification.title}</p>
            <p className="text-xs text-white/60 mt-0.5">{notification.message}</p>
          </div>
          <button
            onClick={() => toast.dismiss(t.id)}
            className="text-white/30 hover:text-white text-lg leading-none flex-shrink-0"
          >
            ×
          </button>
        </div>
      ),
      { duration: 6000, className: 'toast-custom', id: `notif-${notification.notificationId}` }
    );
  });

  // ── Unread count update (after mark-read) ──────────────────
  connection.on('NotificationCountUpdated', ({ unreadCount }) => {
    setCount(unreadCount);
    queryClient.setQueryData(['notifications', 'unread-count'], { data: { unreadCount } });
  });

  // ── Task events ────────────────────────────────────────────
  connection.on('TaskCreated', (task) => {
    queryClient.invalidateQueries({ queryKey: ['tasks'] });
    queryClient.invalidateQueries({ queryKey: ['tasks', 'kanban'] });

    toast(`📋 New task: "${task.title}"`, {
      duration: 4000, className: 'toast-custom', id: `task-created-${task.taskId}`,
    });
  });

  connection.on('TaskUpdated', (task) => {
    // Update the specific task in cache without a full refetch
    queryClient.setQueryData(['tasks', task.taskId], (old) =>
      old ? { ...old, data: task } : old
    );
    queryClient.invalidateQueries({ queryKey: ['tasks'] });
  });

  connection.on('TaskStatusChanged', (payload) => {
    queryClient.invalidateQueries({ queryKey: ['tasks'] });
    queryClient.invalidateQueries({ queryKey: ['tasks', 'kanban', payload.projectId] });
  });

  connection.on('TaskAssigned', (task) => {
    queryClient.invalidateQueries({ queryKey: ['tasks'] });
    toast(`📌 You were assigned to: "${task.taskTitle}"`, {
      duration: 5000, className: 'toast-custom', icon: '📌',
    });
  });

  connection.on('TaskPositionChanged', (payload) => {
    queryClient.invalidateQueries({ queryKey: ['tasks', 'kanban', payload.projectId] });
  });

  // ── Comment events ─────────────────────────────────────────
  connection.on('CommentAdded', (comment) => {
    queryClient.invalidateQueries({ queryKey: ['comments', comment.taskId] });
  });

  connection.on('CommentUpdated', (comment) => {
    queryClient.setQueryData(['comments', comment.taskId], (old) => {
      if (!old?.data?.items) return old;
      return {
        ...old,
        data: {
          ...old.data,
          items: old.data.items.map((c) => c.commentId === comment.commentId ? comment : c),
        },
      };
    });
  });

  connection.on('CommentDeleted', ({ taskId, commentId }) => {
    queryClient.setQueryData(['comments', taskId], (old) => {
      if (!old?.data?.items) return old;
      return {
        ...old,
        data: {
          ...old.data,
          items: old.data.items.filter((c) => c.commentId !== commentId),
        },
      };
    });
  });

  // ── Project events ─────────────────────────────────────────
  connection.on('ProjectMemberAdded', (payload) => {
    queryClient.invalidateQueries({ queryKey: ['projects', payload.projectId] });
  });

  connection.on('ProjectMemberRemoved', (payload) => {
    queryClient.invalidateQueries({ queryKey: ['projects', payload.projectId] });
  });

  // ── Presence events ────────────────────────────────────────
  connection.on('UserPresenceChanged', ({ userId, isOnline }) => {
    // Update presence state in queries that include user data
    queryClient.setQueryData(['users', 'presence'], (old) => ({
      ...(old ?? {}),
      [userId]: isOnline,
    }));
  });
}
