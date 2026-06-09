import { useState, useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { formatDistanceToNow } from 'date-fns';
import { notificationsApi } from '@/api';
import { useNotificationStore } from '@/store/uiStore';
import Avatar from '@/components/ui/Avatar';
import { clsx } from 'clsx';
import toast from 'react-hot-toast';

const TYPE_CONFIG = {
  TaskAssigned:     { icon: '📋', bg: 'bg-primary-500/20', color: 'text-primary-400' },
  TaskStatusChanged:{ icon: '🔄', bg: 'bg-amber-500/20',   color: 'text-amber-400'   },
  CommentAdded:     { icon: '💬', bg: 'bg-violet-500/20',  color: 'text-violet-400'  },
  Mentioned:        { icon: '@',  bg: 'bg-sky-500/20',      color: 'text-sky-400'     },
  TaskOverdue:      { icon: '⚠', bg: 'bg-red-500/20',      color: 'text-red-400'     },
  ProjectAdded:     { icon: '📁', bg: 'bg-emerald-500/20', color: 'text-emerald-400' },
};

export default function NotificationsPage() {
  const [filter, setFilter] = useState('all'); // 'all' | 'unread'
  const queryClient = useQueryClient();
  const { reset: resetBadge } = useNotificationStore();

  const { data, isLoading } = useQuery({
    queryKey: ['notifications', filter],
    queryFn: () => notificationsApi.getAll({
      isRead: filter === 'unread' ? false : undefined,
      page: 1, pageSize: 30,
    }),
  });

  const markReadMutation = useMutation({
    mutationFn: notificationsApi.markRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });
    },
  });

  const markAllReadMutation = useMutation({
    mutationFn: notificationsApi.markAllRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      resetBadge();
      toast.success('All notifications marked as read.');
    },
  });

  const notifications = data?.data?.items ?? [];
  const unreadCount = notifications.filter((n) => !n.isRead).length;

  return (
    <div className="max-w-3xl mx-auto space-y-5">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Notifications</h1>
          <p className="text-white/40 text-sm mt-1">
            {unreadCount > 0 ? `${unreadCount} unread notifications` : 'All caught up!'}
          </p>
        </div>
        {unreadCount > 0 && (
          <button
            onClick={() => markAllReadMutation.mutate()}
            disabled={markAllReadMutation.isPending}
            className="text-sm text-primary-400 hover:text-primary-300 font-medium transition-colors
                       flex items-center gap-2 px-3 py-1.5 rounded-lg hover:bg-primary-500/10"
          >
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
            Mark all read
          </button>
        )}
      </div>

      {/* Filter tabs */}
      <div className="flex gap-2">
        {[
          { key: 'all',    label: 'All'    },
          { key: 'unread', label: 'Unread' },
        ].map((tab) => (
          <button
            key={tab.key}
            onClick={() => setFilter(tab.key)}
            className={clsx(
              'px-4 py-2 rounded-xl text-sm font-medium transition-all',
              filter === tab.key
                ? 'bg-primary-500/20 text-primary-400 border border-primary-500/30'
                : 'text-white/40 hover:text-white/70 hover:bg-white/5'
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Notification list */}
      {isLoading ? (
        <div className="glass rounded-2xl divide-y divide-white/5">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex items-start gap-4 p-5">
              <div className="skeleton w-10 h-10 rounded-xl flex-shrink-0" />
              <div className="flex-1 space-y-2">
                <div className="skeleton h-3 rounded w-3/4" />
                <div className="skeleton h-3 rounded w-1/2" />
              </div>
            </div>
          ))}
        </div>
      ) : notifications.length === 0 ? (
        <div className="glass rounded-2xl p-16 text-center">
          <div className="text-5xl mb-4">🔔</div>
          <p className="text-white/50 font-medium">No notifications</p>
          <p className="text-white/30 text-sm mt-1">You&apos;re all caught up!</p>
        </div>
      ) : (
        <div className="glass rounded-2xl divide-y divide-white/5">
          {notifications.map((notif) => {
            const cfg = TYPE_CONFIG[notif.type] ?? TYPE_CONFIG.TaskAssigned;
            return (
              <div
                key={notif.notificationId}
                className={clsx(
                  'flex items-start gap-4 p-5 transition-all group cursor-pointer',
                  !notif.isRead ? 'bg-primary-500/5 hover:bg-primary-500/8' : 'hover:bg-white/3'
                )}
                onClick={() => {
                  if (!notif.isRead) markReadMutation.mutate(notif.notificationId);
                }}
              >
                {/* Icon */}
                <div className={clsx('w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 text-base', cfg.bg, cfg.color)}>
                  {cfg.icon}
                </div>

                {/* Content */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-white">{notif.title}</p>
                      <p className="text-sm text-white/60 mt-0.5">{notif.message}</p>
                    </div>
                    {/* Unread dot */}
                    {!notif.isRead && (
                      <span className="w-2 h-2 rounded-full bg-primary-500 flex-shrink-0 mt-1.5" />
                    )}
                  </div>
                  <div className="flex items-center gap-3 mt-2">
                    {notif.actor && (
                      <div className="flex items-center gap-1.5">
                        <Avatar user={notif.actor} size="xs" />
                        <span className="text-xs text-white/40">{notif.actor.fullName}</span>
                      </div>
                    )}
                    <span className="text-xs text-white/30">
                      {formatDistanceToNow(new Date(notif.createdAt), { addSuffix: true })}
                    </span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
