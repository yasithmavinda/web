import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { useEffect } from 'react';
import { notificationsApi } from '@/api';
import { useNotificationStore } from '@/store/signalRStore';
import { useAuthStore } from '@/store/authStore';

/**
 * useNotifications — Fetches and manages the notification list.
 *
 * Uses useInfiniteQuery for "load more" pagination (better UX than
 * page numbers for a notification feed).
 *
 * Beginner: useInfiniteQuery automatically manages multiple pages.
 * You call fetchNextPage() and it fetches the next page,
 * appending to the existing list.
 */
export function useNotifications(filter = {}) {
  return useInfiniteQuery({
    queryKey: ['notifications', filter],
    queryFn: ({ pageParam = 1 }) =>
      notificationsApi.getAll({ ...filter, page: pageParam, pageSize: 20 }),
    getNextPageParam: (lastPage) => {
      const { page, pageSize, totalCount } = lastPage.data;
      return page * pageSize < totalCount ? page + 1 : undefined;
    },
    staleTime: 30_000, // 30 seconds — notifications change often
  });
}

/**
 * useUnreadCount — Syncs the unread count badge.
 *
 * On first mount, fetches from the API.
 * After that, the Zustand store is updated via SignalR events.
 * This hook bridges the two.
 */
export function useUnreadCount() {
  const { isAuth }          = useAuthStore();
  const { setCount }        = useNotificationStore();

  const query = useQuery({
    queryKey: ['notifications', 'unread-count'],
    queryFn:  notificationsApi.getUnreadCount,
    enabled:  isAuth,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000, // Fallback poll every 5 min (in case SignalR missed an event)
  });

  // Sync API result into Zustand store
  useEffect(() => {
    if (query.data?.data?.unreadCount !== undefined) {
      setCount(query.data.data.unreadCount);
    }
  }, [query.data, setCount]);

  return query;
}

/**
 * useMarkAsRead — Mutation that marks a notification as read.
 */
export function useMarkAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (notificationId) => notificationsApi.markRead(notificationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });
    },
  });
}

/**
 * useMarkAllAsRead — Marks ALL notifications as read at once.
 */
export function useMarkAllAsRead() {
  const queryClient = useQueryClient();
  const { setCount } = useNotificationStore();

  return useMutation({
    mutationFn: notificationsApi.markAllRead,
    onSuccess: () => {
      setCount(0);
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });
}

/**
 * useNotificationSettings — Get and update user notification preferences.
 */
export function useNotificationSettings() {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ['notifications', 'settings'],
    queryFn:  notificationsApi.getSettings,
    staleTime: 5 * 60_000,
  });

  const updateMutation = useMutation({
    mutationFn: notificationsApi.updateSettings,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications', 'settings'] });
    },
  });

  return { ...query, updateMutation };
}
