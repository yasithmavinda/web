/**
 * useProjectSignalR — Auto-join/leave SignalR project groups on route changes.
 *
 * Beginner: When a user opens a project page, they need to "subscribe"
 * to real-time updates for that project. When they navigate away,
 * they should "unsubscribe" to avoid receiving updates for a project
 * they're no longer viewing.
 *
 * Usage in ProjectDetailPage:
 *   const { isOnline } = useProjectSignalR(projectId);
 */

import { useEffect, useState, useCallback } from 'react';
import { useSignalR } from './useSignalR';
import { usePresenceStore } from '@/store/signalRStore';

export function useProjectSignalR(projectId) {
  const { joinProject, leaveProject } = useSignalR();

  useEffect(() => {
    if (!projectId) return;

    joinProject(projectId);

    return () => {
      leaveProject(projectId);
    };
  }, [projectId, joinProject, leaveProject]);
}

/**
 * useTaskSignalR — Auto-join/leave task comment feed.
 *
 * Usage in TaskDetailPage:
 *   useTaskSignalR(taskId);
 */
export function useTaskSignalR(taskId) {
  const { joinTask, leaveTask } = useSignalR();

  useEffect(() => {
    if (!taskId) return;

    joinTask(taskId);

    return () => {
      leaveTask(taskId);
    };
  }, [taskId, joinTask, leaveTask]);
}

/**
 * useUserPresence — Returns whether a specific user is currently online.
 *
 * Usage:
 *   const isOnline = useUserPresence(userId);
 *   // Returns true if the user has an active WebSocket connection
 */
export function useUserPresence(userId) {
  const { onlineUsers } = usePresenceStore();
  return userId ? onlineUsers[userId.toString()] === true : false;
}

/**
 * useRealtimeTaskList — Combines task queries with real-time updates.
 *
 * Wraps useQuery + subscribes to Kanban board updates for a project.
 */
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { tasksApi } from '@/api';

export function useRealtimeTaskList(projectId, filters = {}) {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ['tasks', 'kanban', projectId, filters],
    queryFn:  () => tasksApi.getAll({ projectId, ...filters, pageSize: 200 }),
    enabled:  !!projectId,
    staleTime: 30_000,
  });

  // Subscribe to project updates
  useProjectSignalR(projectId);

  return query;
}
