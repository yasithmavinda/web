import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { usersApi } from '@/api';
import { authApi } from '@/api';
import Avatar from '@/components/ui/Avatar';
import Button from '@/components/ui/Button';
import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import { Select } from '@/components/ui/Input';
import { RoleBadge } from '@/components/ui/Badge';
import { CardSkeleton } from '@/components/ui/PageLoader';
import { format } from 'date-fns';
import { clsx } from 'clsx';
import toast from 'react-hot-toast';

const ROLES = [
  { value: 1, label: 'Admin'          },
  { value: 2, label: 'ProjectManager' },
  { value: 3, label: 'Collaborator'   },
];

const ROLE_OPTIONS = [
  { value: '1', label: 'Admin' },
  { value: '2', label: 'Project Manager' },
  { value: '3', label: 'Collaborator' },
];

export default function UsersPage() {
  const [search, setSearch]       = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [page, setPage]           = useState(1);
  const [showInvite, setShowInvite] = useState(false);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['users', page, search, roleFilter],
    queryFn: () => usersApi.getAll({ page, pageSize: 12, search: search || undefined, roleId: roleFilter || undefined }),
    keepPreviousData: true,
  });

  const toggleStatusMutation = useMutation({
    mutationFn: ({ id, isActive }) => usersApi.toggleStatus(id, { isActive }),
    onSuccess: (_, { isActive }) => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success(`User ${isActive ? 'activated' : 'deactivated'}.`);
    },
    onError: () => toast.error('Failed to update user status.'),
  });

  const assignRoleMutation = useMutation({
    mutationFn: ({ id, roleId }) => usersApi.assignRole(id, roleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success('Role updated.');
    },
    onError: () => toast.error('Failed to update role.'),
  });

  const users      = data?.data?.items ?? [];
  const total      = data?.data?.totalCount ?? 0;
  const totalPages = Math.ceil(total / 12);

  return (
    <div className="space-y-5">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white">User Management</h1>
          <p className="text-white/40 text-sm mt-1">{total} total users</p>
        </div>
        <Button onClick={() => setShowInvite(true)}>
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          Invite User
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1 max-w-sm">
          <svg className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30"
            fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            type="search"
            placeholder="Search by name or email..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            className="w-full bg-white/5 border border-white/10 rounded-xl pl-9 pr-4 py-2 text-sm
                       text-white placeholder-white/30 focus:outline-none focus:border-primary-500/50
                       focus:bg-white/8 transition-all"
          />
        </div>
        <select
          value={roleFilter}
          onChange={(e) => { setRoleFilter(e.target.value); setPage(1); }}
          className="bg-surface-800 border border-white/10 rounded-xl px-3 py-2 text-sm text-white
                     focus:outline-none focus:border-primary-500/50"
        >
          <option value="">All Roles</option>
          {ROLES.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
        </select>
      </div>

      {/* User grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => <CardSkeleton key={i} />)}
        </div>
      ) : users.length === 0 ? (
        <div className="glass rounded-2xl p-16 text-center">
          <p className="text-white/50">No users found</p>
          <button onClick={() => setShowInvite(true)}
            className="mt-4 text-primary-400 hover:text-primary-300 text-sm font-medium transition-colors">
            Invite your first user →
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {users.map((user) => (
            <UserCard
              key={user.userId}
              user={user}
              onToggleStatus={(isActive) => toggleStatusMutation.mutate({ id: user.userId, isActive })}
              onAssignRole={(roleId) => assignRoleMutation.mutate({ id: user.userId, roleId })}
              isLoading={toggleStatusMutation.isPending || assignRoleMutation.isPending}
            />
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-white/40">
            Showing {(page - 1) * 12 + 1}–{Math.min(page * 12, total)} of {total}
          </p>
          <div className="flex gap-2">
            <Button variant="secondary" size="sm" disabled={page === 1} onClick={() => setPage(p => p - 1)}>
              ←
            </Button>
            <span className="flex items-center px-4 text-sm text-white/60">
              {page} / {totalPages}
            </span>
            <Button variant="secondary" size="sm" disabled={page === totalPages} onClick={() => setPage(p => p + 1)}>
              →
            </Button>
          </div>
        </div>
      )}

      {/* Invite User Modal */}
      <InviteUserModal
        isOpen={showInvite}
        onClose={() => setShowInvite(false)}
        onSuccess={() => {
          setShowInvite(false);
          queryClient.invalidateQueries({ queryKey: ['users'] });
        }}
      />
    </div>
  );
}

function UserCard({ user, onToggleStatus, onAssignRole, isLoading }) {
  return (
    <div className={clsx(
      'glass rounded-2xl p-5 card-hover flex flex-col gap-4',
      !user.isActive && 'opacity-60'
    )}>
      {/* Top row */}
      <div className="flex items-start gap-3">
        <div className="relative flex-shrink-0">
          <Avatar user={user} size="md" />
          <span className={clsx(
            'absolute -bottom-0.5 -right-0.5 w-3 h-3 rounded-full ring-2 ring-surface-800',
            user.isActive ? 'bg-emerald-400' : 'bg-white/20'
          )} />
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-sm font-semibold text-white truncate">{user.fullName}</p>
          <p className="text-xs text-white/40 truncate">{user.email}</p>
          <div className="mt-1.5">
            <RoleBadge roleName={user.roleName} />
          </div>
        </div>
      </div>

      {/* Meta */}
      <div className="flex items-center justify-between text-xs text-white/30 border-t border-white/8 pt-3">
        <span>Joined {format(new Date(user.createdAt ?? Date.now()), 'MMM yyyy')}</span>
        <span className={user.isEmailVerified ? 'text-emerald-400' : 'text-amber-400'}>
          {user.isEmailVerified ? '✓ Verified' : '⚠ Unverified'}
        </span>
      </div>

      {/* Actions */}
      <div className="flex gap-2">
        <select
          defaultValue={user.roleId}
          onChange={(e) => onAssignRole(Number(e.target.value))}
          disabled={isLoading}
          className="flex-1 bg-white/5 border border-white/10 rounded-lg px-2 py-1.5 text-xs text-white
                     focus:outline-none focus:border-primary-500/50 disabled:opacity-50"
        >
          {ROLES.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
        </select>
        <Button
          variant={user.isActive ? 'danger' : 'outline'}
          size="sm"
          disabled={isLoading}
          onClick={() => onToggleStatus(!user.isActive)}
        >
          {user.isActive ? 'Deactivate' : 'Activate'}
        </Button>
      </div>
    </div>
  );
}

function InviteUserModal({ isOpen, onClose, onSuccess }) {
  const { register, handleSubmit, reset, formState: { errors } } = useForm({
    defaultValues: { roleId: '3' },
  });

  const inviteMutation = useMutation({
    mutationFn: (data) => authApi.register({
      fullName: data.fullName,
      email:    data.email,
      password: data.password,
      roleId:   Number(data.roleId),
    }),
    onSuccess: () => {
      reset();
      onSuccess();
      toast.success('User invited successfully! They can now log in.');
    },
    onError: (err) => {
      const msg = err?.response?.data?.message ?? 'Failed to invite user.';
      toast.error(msg);
    },
  });

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Invite New User"
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button
            form="invite-user-form"
            type="submit"
            loading={inviteMutation.isPending}
          >
            Create Account
          </Button>
        </>
      }
    >
      <form
        id="invite-user-form"
        onSubmit={handleSubmit((d) => inviteMutation.mutate(d))}
        className="space-y-4"
      >
        <Input
          label="Full Name"
          placeholder="e.g. Jane Smith"
          required
          error={errors.fullName?.message}
          {...register('fullName', { required: 'Full name is required' })}
        />
        <Input
          label="Email Address"
          type="email"
          placeholder="jane@company.com"
          required
          error={errors.email?.message}
          {...register('email', {
            required: 'Email is required',
            pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Enter a valid email' },
          })}
        />
        <Input
          label="Temporary Password"
          type="password"
          placeholder="Min. 8 characters"
          required
          error={errors.password?.message}
          {...register('password', {
            required: 'Password is required',
            minLength: { value: 8, message: 'At least 8 characters' },
          })}
        />
        <Select
          label="Role"
          options={ROLE_OPTIONS}
          error={errors.roleId?.message}
          {...register('roleId', { required: 'Role is required' })}
        />
        <p className="text-xs text-white/30 bg-white/5 rounded-xl p-3 border border-white/8">
          💡 The user will be able to log in immediately with the email and password you set here.
        </p>
      </form>
    </Modal>
  );
}
