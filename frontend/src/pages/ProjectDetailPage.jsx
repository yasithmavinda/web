// ProjectDetailPage — Kanban board for a specific project
// Full Kanban implementation will be in Phase 10

import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { projectsApi } from '@/api';
import { CardSkeleton } from '@/components/ui/PageLoader';
import { StatusBadge, PriorityBadge } from '@/components/ui/Badge';
import Avatar, { AvatarGroup } from '@/components/ui/Avatar';
import Button from '@/components/ui/Button';
import { useAuthStore } from '@/store/authStore';
import { format } from 'date-fns';
import { clsx } from 'clsx';

const COLUMNS = [
  { key: 'Backlog',    label: 'Backlog',      color: '#64748b' },
  { key: 'Todo',       label: 'To Do',        color: '#6366f1' },
  { key: 'InProgress', label: 'In Progress',  color: '#f59e0b' },
  { key: 'InReview',   label: 'In Review',    color: '#8b5cf6' },
  { key: 'Done',       label: 'Done',         color: '#10b981' },
];

// Mock tasks for demonstration
const MOCK_TASKS = [
  { taskId: 1, title: 'Set up project repo',           status: 'Done',       priority: 'High',   assignees: [{ fullName: 'Alice M.' }] },
  { taskId: 2, title: 'Design database schema',        status: 'Done',       priority: 'High',   assignees: [{ fullName: 'Bob K.' }] },
  { taskId: 3, title: 'Implement authentication API',  status: 'InProgress', priority: 'Critical',assignees: [{ fullName: 'Alice M.' }, { fullName: 'Dave S.' }] },
  { taskId: 4, title: 'Build login/register UI',       status: 'InProgress', priority: 'High',   assignees: [{ fullName: 'Carol D.' }] },
  { taskId: 5, title: 'Write unit tests for AuthService',status:'InReview',  priority: 'Medium', assignees: [{ fullName: 'Dave S.' }] },
  { taskId: 6, title: 'Configure CI/CD pipeline',      status: 'Todo',       priority: 'Medium', assignees: [] },
  { taskId: 7, title: 'Create API documentation',      status: 'Todo',       priority: 'Low',    assignees: [{ fullName: 'Eve R.' }] },
  { taskId: 8, title: 'Performance testing',           status: 'Backlog',    priority: 'Low',    assignees: [] },
];

export default function ProjectDetailPage() {
  const { id } = useParams();
  const isManager = useAuthStore((s) => s.isProjectManager);

  const { data, isLoading } = useQuery({
    queryKey: ['projects', id],
    queryFn:  () => projectsApi.getById(id),
    enabled:  !!id,
  });

  const project = data?.data;

  return (
    <div className="space-y-5">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-sm text-white/40">
        <Link to="/projects" className="hover:text-white transition-colors">Projects</Link>
        <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
        </svg>
        <span className="text-white">{project?.name ?? 'Loading...'}</span>
      </nav>

      {/* Project header */}
      {isLoading ? <CardSkeleton /> : (
        <div className="glass rounded-2xl p-5 flex flex-col sm:flex-row gap-4 sm:items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-4 h-4 rounded-full flex-shrink-0"
              style={{ background: project?.colorTag ?? '#6366f1', boxShadow: `0 0 10px ${project?.colorTag ?? '#6366f1'}60` }} />
            <div>
              <h1 className="text-xl font-bold text-white">{project?.name ?? 'Project'}</h1>
              {project?.description && <p className="text-sm text-white/40 mt-0.5">{project.description}</p>}
            </div>
          </div>
          <div className="flex items-center gap-4">
            <AvatarGroup users={project?.members?.slice(0,6).map(m => m.user) ?? []} max={5} size="sm" />
            {isManager && <Button size="sm">+ Add Task</Button>}
          </div>
        </div>
      )}

      {/* Kanban Board */}
      <div className="overflow-x-auto pb-4">
        <div className="flex gap-4 min-w-max">
          {COLUMNS.map((col) => {
            const tasks = MOCK_TASKS.filter(t => t.status === col.key);
            return (
              <div key={col.key} className="w-72 flex flex-col gap-3">
                {/* Column header */}
                <div className="flex items-center justify-between px-1">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ background: col.color }} />
                    <span className="text-sm font-semibold text-white">{col.label}</span>
                    <span className="text-xs bg-white/10 text-white/50 px-2 py-0.5 rounded-full">
                      {tasks.length}
                    </span>
                  </div>
                  <button className="w-6 h-6 rounded-lg hover:bg-white/10 flex items-center justify-center
                                     text-white/30 hover:text-white transition-all text-lg leading-none">
                    +
                  </button>
                </div>

                {/* Task cards */}
                <div className="kanban-column">
                  {tasks.length === 0 ? (
                    <div className="flex flex-col items-center justify-center py-8 text-center">
                      <div className="w-8 h-8 rounded-xl border-2 border-dashed border-white/10 mb-2" />
                      <p className="text-xs text-white/20">Drop tasks here</p>
                    </div>
                  ) : (
                    tasks.map((task) => (
                      <div key={task.taskId} className="kanban-card animate-fade-in">
                        <div className="flex items-start justify-between gap-2 mb-3">
                          <p className={clsx(
                            'text-sm font-medium leading-snug',
                            task.status === 'Done' ? 'line-through text-white/30' : 'text-white'
                          )}>
                            {task.title}
                          </p>
                        </div>
                        <div className="flex items-center justify-between">
                          <PriorityBadge priority={task.priority} />
                          <AvatarGroup users={task.assignees} max={2} size="xs" />
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
