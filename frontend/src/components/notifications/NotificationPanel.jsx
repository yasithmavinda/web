import { useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { createPortal } from 'react-dom';
import { formatDistanceToNow } from 'date-fns';
import {
  useNotifications,
  useMarkAsRead,
  useMarkAllAsRead,
} from '@/hooks/useNotifications';
import { useNotificationStore } from '@/store/signalRStore';
import { Spinner } from '@/components/ui/PageLoader';
import { clsx } from 'clsx';

/*
 * NOTIFICATION PANEL — Slide-in drawer from the right.
 *
 * Features:
 *   - Infinite scroll: loads 20 at a time, "Load more" button
 *   - Click to mark read (with optimistic unread dot removal)
 *   - Mark all read button
 *   - Grouped by: Today, Yesterday, Older
 *   - Live badge updates from SignalR
 *   - Closes on Escape or outside click
 */

const TYPE_ICONS = {
  TaskAssigned:     { emoji: '📋', bg: 'bg-primary-500/20', color: 'text-primary-400' },
  TaskStatusChanged:{ emoji: '🔄', bg: 'bg-amber-500/20',   color: 'text-amber-400'   },
  CommentAdded:     { emoji: '💬', bg: 'bg-violet-500/20',  color: 'text-violet-400'  },
  Mentioned:        { emoji: '@',  bg: 'bg-sky-500/20',      color: 'text-sky-400'     },
  TaskOverdue:      { emoji: '⚠', bg: 'bg-red-500/20',      color: 'text-red-400'     },
  ProjectAdded:     { emoji: '📁', bg: 'bg-emerald-500/20', color: 'text-emerald-400' },
  TaskCreated:      { emoji: '✨', bg: 'bg-primary-500/20', color: 'text-primary-400' },
};

function groupByDate(notifications) {
  const today     = new Date(); today.setHours(0,0,0,0);
  const yesterday = new Date(today); yesterday.setDate(yesterday.getDate() - 1);

  const groups = { Today: [], Yesterday: [], Older: [] };
  for (const n of notifications) {
    const d = new Date(n.createdAt);
    if (d >= today)     groups.Today.push(n);
    else if (d >= yesterday) groups.Yesterday.push(n);
    else                groups.Older.push(n);
  }
  return groups;
}

export default function NotificationPanel({ isOpen, onClose }) {
  const panelRef           = useRef(null);
  const { clearNew }       = useNotificationStore();
  const markReadMut        = useMarkAsRead();
  const markAllReadMut     = useMarkAllAsRead();

  const {
    data, fetchNextPage, hasNextPage, isFetchingNextPage, isLoading,
  } = useNotifications();

  // Flatten pages
  const allNotifications = data?.pages?.flatMap((p) => p.data?.items ?? []) ?? [];
  const unreadCount      = allNotifications.filter((n) => !n.isRead).length;
  const groups           = groupByDate(allNotifications);

  // Clear "new" flag when panel opens
  useEffect(() => {
    if (isOpen) clearNew();
  }, [isOpen, clearNew]);

  // Close on Escape
  useEffect(() => {
    if (!isOpen) return;
    const handler = (e) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [isOpen, onClose]);

  // Close on outside click
  useEffect(() => {
    if (!isOpen) return;
    const handler = (e) => {
      if (panelRef.current && !panelRef.current.contains(e.target)) onClose();
    };
    setTimeout(() => document.addEventListener('mousedown', handler), 100);
    return () => document.removeEventListener('mousedown', handler);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return createPortal(
    <div className="fixed inset-0 z-50 flex justify-end">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm animate-fade-in" />

      {/* Panel */}
      <div
        ref={panelRef}
        className={clsx(
          'relative z-10 w-full max-w-md h-full flex flex-col',
          'bg-surface-800/95 backdrop-blur-xl border-l border-white/10',
          'shadow-2xl animate-slide-in-right'
        )}
      >
        {/* ── Header ──────────────────────────────────────────── */}
        <div className="flex items-center justify-between p-5 border-b border-white/8 flex-shrink-0">
          <div className="flex items-center gap-3">
            <h2 className="text-lg font-semibold text-white">Notifications</h2>
            {unreadCount > 0 && (
              <span className="min-w-[22px] h-[22px] flex items-center justify-center
                               bg-primary-500 text-white text-xs font-bold rounded-full px-1.5
                               animate-scale-in">
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </div>

          <div className="flex items-center gap-2">
            {unreadCount > 0 && (
              <button
                onClick={() => markAllReadMut.mutate()}
                disabled={markAllReadMut.isPending}
                className="text-xs text-primary-400 hover:text-primary-300 font-medium
                           px-3 py-1.5 rounded-lg hover:bg-primary-500/10 transition-all
                           disabled:opacity-50"
              >
                {markAllReadMut.isPending ? 'Clearing...' : 'Mark all read'}
              </button>
            )}
            <button
              onClick={onClose}
              className="p-2 rounded-xl text-white/40 hover:text-white hover:bg-white/10 transition-all"
              aria-label="Close"
            >
              <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        {/* ── Body ─────────────────────────────────────────────── */}
        <div className="flex-1 overflow-y-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-16">
              <Spinner size="lg" />
            </div>
          ) : allNotifications.length === 0 ? (
            <EmptyState />
          ) : (
            <div>
              {/* Grouped sections */}
              {Object.entries(groups).map(([group, items]) =>
                items.length === 0 ? null : (
                  <div key={group}>
                    <div className="px-5 py-2 bg-white/2 border-y border-white/5">
                      <span className="text-xs font-semibold text-white/30 uppercase tracking-wider">
                        {group}
                      </span>
                    </div>
                    {items.map((notif) => (
                      <NotificationItem
                        key={notif.notificationId}
                        notification={notif}
                        onRead={() => {
                          if (!notif.isRead) markReadMut.mutate(notif.notificationId);
                        }}
                        onClose={onClose}
                      />
                    ))}
                  </div>
                )
              )}

              {/* Load more */}
              <div className="p-4 flex justify-center">
                {hasNextPage ? (
                  <button
                    onClick={() => fetchNextPage()}
                    disabled={isFetchingNextPage}
                    className="text-sm text-primary-400 hover:text-primary-300 font-medium
                               px-4 py-2 rounded-lg hover:bg-primary-500/10 transition-all
                               flex items-center gap-2 disabled:opacity-50"
                  >
                    {isFetchingNextPage ? (
                      <><Spinner size="sm" /> Loading...</>
                    ) : (
                      'Load more'
                    )}
                  </button>
                ) : (
                  <p className="text-xs text-white/20">You&apos;ve seen all notifications</p>
                )}
              </div>
            </div>
          )}
        </div>

        {/* ── Footer ────────────────────────────────────────────── */}
        <div className="border-t border-white/8 p-4 flex-shrink-0">
          <Link
            to="/notifications"
            onClick={onClose}
            className="flex items-center justify-center gap-2 w-full py-2.5 rounded-xl
                       text-sm font-medium text-white/60 hover:text-white
                       hover:bg-white/8 transition-all"
          >
            View all notifications
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
          </Link>
        </div>
      </div>
    </div>,
    document.body
  );
}

// ── NOTIFICATION ITEM ─────────────────────────────────────────
function NotificationItem({ notification, onRead, onClose }) {
  const cfg = TYPE_ICONS[notification.type] ?? TYPE_ICONS.TaskCreated;
  const timeAgo = formatDistanceToNow(new Date(notification.createdAt), { addSuffix: true });

  // Determine navigation target
  const getLink = () => {
    if (notification.entityType === 'Task' && notification.entityId) {
      return `/tasks/${notification.entityId}`;
    }
    if (notification.entityType === 'Project' && notification.entityId) {
      return `/projects/${notification.entityId}`;
    }
    return null;
  };
  const link = getLink();

  const content = (
    <div
      className={clsx(
        'flex items-start gap-4 px-5 py-4 transition-all cursor-pointer group',
        !notification.isRead
          ? 'bg-primary-500/5 hover:bg-primary-500/10'
          : 'hover:bg-white/3'
      )}
      onClick={onRead}
    >
      {/* Icon */}
      <div className={clsx(
        'w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 text-base',
        cfg.bg, cfg.color
      )}>
        {cfg.emoji}
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-start gap-2">
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-white leading-snug">{notification.title}</p>
            <p className="text-xs text-white/50 mt-0.5 leading-relaxed">{notification.message}</p>
          </div>
          {/* Unread indicator */}
          {!notification.isRead && (
            <span className="w-2 h-2 rounded-full bg-primary-500 flex-shrink-0 mt-1.5 animate-pulse" />
          )}
        </div>
        <p className="text-[11px] text-white/25 mt-1.5">{timeAgo}</p>
      </div>
    </div>
  );

  if (link) {
    return (
      <Link to={link} onClick={onClose} className="block">
        {content}
      </Link>
    );
  }

  return content;
}

// ── EMPTY STATE ───────────────────────────────────────────────
function EmptyState() {
  return (
    <div className="flex flex-col items-center justify-center py-20 px-8 text-center">
      <div className="w-20 h-20 rounded-2xl bg-white/5 flex items-center justify-center mb-4">
        <svg className="w-10 h-10 text-white/20" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
            d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
        </svg>
      </div>
      <h3 className="text-base font-semibold text-white/60 mb-1">All caught up!</h3>
      <p className="text-sm text-white/30">
        No notifications yet. They&apos;ll appear here when someone assigns a task or leaves a comment.
      </p>
    </div>
  );
}
