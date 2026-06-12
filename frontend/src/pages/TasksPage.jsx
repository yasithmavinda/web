import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { tasksApi, projectsApi, usersApi } from '@/api';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Modal from '@/components/ui/Modal';
import { Textarea, Select } from '@/components/ui/Input';
import { StatusBadge, PriorityBadge } from '@/components/ui/Badge';
import Avatar, { AvatarGroup } from '@/components/ui/Avatar';
import { CardSkeleton } from '@/components/ui/PageLoader';
import { useAuthStore } from '@/store/authStore';
import { format } from 'date-fns';
import { clsx } from 'clsx';
import toast from 'react-hot-toast';

const STATUS_OPTS   = ['Backlog','Todo','InProgress','InReview','Done','Blocked'].map(v => ({ value: v, label: v }));
const PRIORITY_OPTS = ['Low','Medium','High','Critical'].map(v => ({ value: v, label: v }));

export default function TasksPage() {
  const [filters, setFilters]   = useState({ page: 1, pageSize: 12 });
  const [showCreate, setCreate] = useState(false);
  const isManager = useAuthStore((s) => s.isProjectManager);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['tasks', filters],
    queryFn:  () => tasksApi.getAll(filters),
    keepPreviousData: true,
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }) => tasksApi.updateStatus(id, { status }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tasks'] }),
  });

  const tasks      = data?.data?.items ?? [];
  const totalCount = data?.data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / filters.pageSize);

  const setFilter = (key, val) => setFilters((f) => ({ ...f, [key]: val, page: 1 }));

  return (
    <div className="space-y-5">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white">Tasks</h1>
          <p className="text-white/40 text-sm mt-1">{totalCount} total tasks</p>
        </div>
        {isManager && (
          <Button onClick={() => setCreate(true)}>
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            New Task
          </Button>
        )}
      </div>

      {/* Filters row */}
      <div className="flex flex-wrap gap-3">
        <div className="relative">
          <svg className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30"
            fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            type="search"
            placeholder="Search tasks..."
            onChange={(e) => setFilter('search', e.target.value || undefined)}
            className="bg-white/5 border border-white/10 rounded-xl pl-9 pr-4 py-2 text-sm
                       text-white placeholder-white/30 focus:outline-none focus:border-primary-500/50 w-52"
          />
        </div>

        {['Backlog','Todo','InProgress','InReview','Done','Blocked'].map((s) => (
          <button
            key={s}
            onClick={() => setFilter('status', filters.status === s ? undefined : s)}
            className={clsx(
              'px-3 py-1.5 rounded-lg text-xs font-medium transition-all border',
              filters.status === s
                ? 'bg-primary-500/20 text-primary-400 border-primary-500/30'
                : 'text-white/40 border-white/10 hover:text-white/70 hover:border-white/20'
            )}
          >
            {s}
          </button>
        ))}

        {(filters.status || filters.search) && (
          <button onClick={() => setFilters({ page: 1, pageSize: 12 })}
            className="text-xs text-white/30 hover:text-white/60 transition-colors px-2">
            Clear
          </button>
        )}
      </div>

      {/* Task grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => <CardSkeleton key={i} />)}
        </div>
      ) : tasks.length === 0 ? (
        <div className="glass rounded-2xl p-16 text-center">
          <p className="text-white/50">No tasks found</p>
          {isManager && (
            <button onClick={() => setCreate(true)}
              className="mt-4 text-primary-400 hover:text-primary-300 text-sm font-medium transition-colors">
              Create the first task →
            </button>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {tasks.map((task) => (
            <TaskCard key={task.taskId} task={task}
              onStatusChange={(status) => updateStatusMutation.mutate({ id: task.taskId, status })} />
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-end gap-2">
          <Button variant="secondary" size="sm" disabled={filters.page === 1}
            onClick={() => setFilters((f) => ({ ...f, page: f.page - 1 }))}>←</Button>
          <span className="flex items-center px-3 text-sm text-white/50">{filters.page}/{totalPages}</span>
          <Button variant="secondary" size="sm" disabled={filters.page === totalPages}
            onClick={() => setFilters((f) => ({ ...f, page: f.page + 1 }))}>→</Button>
        </div>
      )}

      <CreateTaskModal isOpen={showCreate} onClose={() => setCreate(false)}
        onSuccess={() => { setCreate(false); queryClient.invalidateQueries({ queryKey: ['tasks'] }); }} />
    </div>
  );
}

function TaskCard({ task, onStatusChange }) {
  const isOverdue = task.dueDate && new Date(task.dueDate) < new Date() && task.status !== 'Done';

  return (
    <div className="glass rounded-2xl p-5 card-hover flex flex-col gap-3">
      {/* Top badges */}
      <div className="flex items-center justify-between gap-2">
        <StatusBadge status={task.status} />
        <PriorityBadge priority={task.priority} />
      </div>

      {/* Title */}
      <div>
        <h3 className={clsx(
          'font-semibold text-sm leading-snug',
          task.status === 'Done' ? 'line-through text-white/30' : 'text-white'
        )}>
          {task.title}
        </h3>
        {task.projectName && (
          <p className="text-xs text-white/30 mt-1">{task.projectName}</p>
        )}
      </div>

      {/* Description preview */}
      {task.description && (
        <p className="text-xs text-white/40 line-clamp-2">{task.description}</p>
      )}

      {/* Due date + Assignees */}
      <div className="flex items-center justify-between border-t border-white/8 pt-3 mt-1">
        {task.dueDate ? (
          <div className={clsx('flex items-center gap-1 text-xs', isOverdue ? 'text-red-400' : 'text-white/30')}>
            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            {format(new Date(task.dueDate), 'MMM d')}
          </div>
        ) : <div />}
        <AvatarGroup users={task.assignees ?? []} max={3} size="xs" />
      </div>

      {/* Quick status change */}
      <select
        defaultValue={task.status}
        onChange={(e) => onStatusChange(e.target.value)}
        className="w-full bg-white/5 border border-white/10 rounded-lg px-2.5 py-1.5 text-xs text-white/70
                   focus:outline-none focus:border-primary-500/50 cursor-pointer"
      >
        {STATUS_OPTS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
    </div>
  );
}

function CreateTaskModal({ isOpen, onClose, onSuccess }) {
  const { register, handleSubmit, reset, formState: { errors } } = useForm();

  const { data: projects } = useQuery({
    queryKey: ['projects', 'all'],
    queryFn:  () => projectsApi.getAll({ page: 1, pageSize: 100 }),
    enabled:  isOpen,
  });

  const createMutation = useMutation({
    mutationFn: tasksApi.create,
    onSuccess: () => { reset(); onSuccess(); toast.success('Task created!'); },
    onError:   (error) => {
      console.log(error.response?.data);
      const msg = error.response?.data?.message || 'Failed to create task.';
      toast.error(msg);
    },
  });

  const projectOptions = (projects?.data?.items ?? []).map(p => ({ value: p.projectId, label: p.name }));

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Create New Task"
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button form="create-task-form" type="submit" loading={createMutation.isPending}>
            Create Task
          </Button>
        </>
      }
    >
      <form id="create-task-form"
        onSubmit={handleSubmit((d) => {
          const payload = {
            ...d,
            projectId: Number(d.projectId),
            dueDate: d.dueDate || null,
            status: d.status || 'Backlog',
            priority: d.priority || 'Medium',
          };
          createMutation.mutate(payload);
        })}
        className="space-y-4">
        <Input label="Task Title" required placeholder="What needs to be done?"
          error={errors.title?.message}
          {...register('title', { required: 'Title is required', maxLength: { value: 500, message: 'Too long' } })} />
        <Textarea label="Description" rows={3} placeholder="Add more details..."
          {...register('description')} />
        <div className="grid grid-cols-2 gap-4">
          <Select label="Project" required options={projectOptions} placeholder="Select project"
            error={errors.projectId?.message}
            {...register('projectId', { required: 'Project is required' })} />
          <Select label="Priority" options={PRIORITY_OPTS} {...register('priority')} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Due Date" type="date" {...register('dueDate')} />
          <Select label="Status" options={STATUS_OPTS} {...register('status')} />
        </div>
      </form>
    </Modal>
  );
}
