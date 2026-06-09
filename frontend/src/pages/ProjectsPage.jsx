import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { projectsApi, usersApi } from '@/api';
import Avatar, { AvatarGroup } from '@/components/ui/Avatar';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Modal from '@/components/ui/Modal';
import { CardSkeleton } from '@/components/ui/PageLoader';
import { useAuthStore } from '@/store/authStore';
import { format } from 'date-fns';
import { clsx } from 'clsx';
import toast from 'react-hot-toast';

const STATUS_COLORS = {
  Planning:   'bg-slate-500/20  text-slate-400',
  Active:     'bg-emerald-500/20 text-emerald-400',
  OnHold:     'bg-amber-500/20  text-amber-400',
  Completed:  'bg-primary-500/20 text-primary-400',
  Cancelled:  'bg-red-500/20    text-red-400',
};

export default function ProjectsPage() {
  const [showModal, setShowModal] = useState(false);
  const isManager = useAuthStore((s) => s.isProjectManager);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['projects'],
    queryFn: () => projectsApi.getAll({ page: 1, pageSize: 20 }),
  });

  const projects = data?.data?.items ?? [];

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Projects</h1>
          <p className="text-white/40 text-sm mt-1">{projects.length} active projects</p>
        </div>
        {isManager && (
          <Button onClick={() => setShowModal(true)}>
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            New Project
          </Button>
        )}
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => <CardSkeleton key={i} />)}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {projects.map((project) => (
            <ProjectCard key={project.projectId} project={project} />
          ))}
        </div>
      )}

      <CreateProjectModal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        onSuccess={() => {
          setShowModal(false);
          queryClient.invalidateQueries({ queryKey: ['projects'] });
        }}
      />
    </div>
  );
}

function ProjectCard({ project }) {
  const pct = project.taskCount > 0
    ? Math.round((project.completedTaskCount / project.taskCount) * 100) : 0;

  return (
    <Link to={`/projects/${project.projectId}`} className="glass rounded-2xl p-5 card-hover flex flex-col gap-4 group block">
      {/* Top */}
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="w-3 h-3 rounded-full flex-shrink-0"
            style={{ background: project.colorTag, boxShadow: `0 0 8px ${project.colorTag}80` }} />
          <h3 className="font-semibold text-white group-hover:text-primary-300 transition-colors">{project.name}</h3>
        </div>
        <span className={clsx('status-badge text-xs', STATUS_COLORS[project.status] ?? STATUS_COLORS.Active)}>
          {project.status}
        </span>
      </div>

      {project.description && (
        <p className="text-sm text-white/40 line-clamp-2">{project.description}</p>
      )}

      {/* Progress */}
      <div>
        <div className="flex items-center justify-between text-xs mb-2">
          <span className="text-white/40">{project.completedTaskCount}/{project.taskCount} tasks</span>
          <span className="font-semibold text-white/70">{pct}%</span>
        </div>
        <div className="h-1.5 w-full bg-white/10 rounded-full overflow-hidden">
          <div className="h-full rounded-full transition-all duration-700"
            style={{ width: `${pct}%`, background: project.colorTag }} />
        </div>
      </div>

      {/* Footer */}
      <div className="flex items-center justify-between border-t border-white/8 pt-3">
        <AvatarGroup
          users={project.members?.slice(0, 5).map(m => m.user) ?? []}
          max={4} size="xs"
        />
        {project.endDate && (
          <span className="text-xs text-white/30 flex items-center gap-1">
            <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            {format(new Date(project.endDate), 'MMM d, yyyy')}
          </span>
        )}
      </div>
    </Link>
  );
}

function CreateProjectModal({ isOpen, onClose, onSuccess }) {
  const { register, handleSubmit, reset, formState: { errors } } = useForm();
  const createMutation = useMutation({
    mutationFn: projectsApi.create,
    onSuccess: () => { reset(); onSuccess(); toast.success('Project created!'); },
    onError:   () => toast.error('Failed to create project.'),
  });

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Create New Project"
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button form="create-project-form" type="submit" loading={createMutation.isPending}>
            Create Project
          </Button>
        </>
      }
    >
      <form id="create-project-form" onSubmit={handleSubmit((d) => createMutation.mutate(d))} className="space-y-4">
        <Input label="Project Name" required placeholder="e.g. Website Redesign"
          error={errors.name?.message}
          {...register('name', { required: 'Name is required', maxLength: { value: 200, message: 'Max 200 chars' } })} />
        <div className="flex flex-col gap-1.5">
          <label className="text-sm font-medium text-white/80">Description</label>
          <textarea rows={3} placeholder="What is this project about?"
            className="w-full px-3.5 py-2.5 rounded-xl text-sm bg-white/5 border border-white/10 text-white
                       placeholder-white/30 resize-none focus:outline-none focus:ring-2 focus:ring-primary-500/50 focus:border-primary-500/50"
            {...register('description')} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Start Date" type="date" {...register('startDate')} />
          <Input label="End Date" type="date" {...register('endDate')} />
        </div>
        <div className="flex flex-col gap-1.5">
          <label className="text-sm font-medium text-white/80">Color</label>
          <div className="flex gap-2 flex-wrap">
            {['#6366f1','#8b5cf6','#ec4899','#f43f5e','#f59e0b','#10b981','#06b6d4','#3b82f6'].map((c) => (
              <label key={c} className="cursor-pointer">
                <input type="radio" value={c} defaultChecked={c === '#6366f1'} {...register('colorTag')} className="sr-only peer" />
                <span className="w-7 h-7 rounded-full block ring-2 ring-transparent peer-checked:ring-white transition-all"
                  style={{ background: c }} />
              </label>
            ))}
          </div>
        </div>
      </form>
    </Modal>
  );
}
