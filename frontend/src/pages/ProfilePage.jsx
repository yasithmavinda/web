import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '@/store/authStore';
import { usersApi } from '@/api';
import { useAuth } from '@/hooks/useAuth';
import Avatar from '@/components/ui/Avatar';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import { Textarea } from '@/components/ui/Input';
import { RoleBadge } from '@/components/ui/Badge';
import { clsx } from 'clsx';
import toast from 'react-hot-toast';
import { format } from 'date-fns';

const TABS = ['Profile', 'Security', 'Notifications', 'Sessions'];

export default function ProfilePage() {
  const [activeTab, setActiveTab] = useState('Profile');
  const { user, setUser } = useAuthStore();

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* ── Profile Header ─────────────────────────────────── */}
      <div className="glass rounded-2xl overflow-hidden">
        {/* Cover */}
        <div className="h-32 bg-gradient-to-r from-primary-900 via-primary-800 to-violet-900 relative">
          <div className="absolute inset-0 opacity-30"
            style={{
              backgroundImage: 'radial-gradient(circle at 20% 50%, #6366f1 0%, transparent 50%), radial-gradient(circle at 80% 20%, #8b5cf6 0%, transparent 40%)',
            }}
          />
        </div>

        {/* User info */}
        <div className="px-6 pb-6">
          <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 -mt-10 mb-4">
            {/* Avatar with upload button */}
            <div className="relative w-20 h-20">
              <Avatar user={user} size="xl"
                className="ring-4 ring-surface-800 shadow-xl rounded-full" />
              <button
                className="absolute -bottom-1 -right-1 w-7 h-7 rounded-full bg-primary-500
                           hover:bg-primary-600 flex items-center justify-center shadow-lg
                           transition-all text-white"
                title="Change avatar"
              >
                <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                    d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </button>
            </div>

            <div className="sm:flex-1">
              <h1 className="text-xl font-bold text-white">{user?.fullName}</h1>
              <div className="flex items-center gap-3 mt-1">
                <p className="text-white/50 text-sm">{user?.email}</p>
                <RoleBadge roleName={user?.roleName ?? 'Collaborator'} />
              </div>
              {user?.jobTitle && (
                <p className="text-white/40 text-xs mt-1">{user.jobTitle} · {user.department}</p>
              )}
            </div>

            {/* Stats row */}
            <div className="flex items-center gap-5 text-center">
              {[
                { label: 'Projects',   value: 6  },
                { label: 'Tasks',      value: 23 },
                { label: 'Completed',  value: 18 },
              ].map((s) => (
                <div key={s.label}>
                  <p className="text-xl font-bold text-white">{s.value}</p>
                  <p className="text-xs text-white/40">{s.label}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* ── Tabs ───────────────────────────────────────────── */}
      <div className="flex gap-1 bg-surface-800/60 p-1 rounded-xl border border-white/8 w-fit">
        {TABS.map((tab) => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={clsx(
              'px-4 py-2 rounded-lg text-sm font-medium transition-all',
              activeTab === tab
                ? 'bg-primary-500/20 text-primary-400 border border-primary-500/30'
                : 'text-white/40 hover:text-white/70'
            )}
          >
            {tab}
          </button>
        ))}
      </div>

      {/* ── Tab Content ─────────────────────────────────────── */}
      <div className="animate-fade-in">
        {activeTab === 'Profile'       && <ProfileTab user={user} setUser={setUser} />}
        {activeTab === 'Security'      && <SecurityTab />}
        {activeTab === 'Notifications' && <NotifSettingsTab />}
        {activeTab === 'Sessions'      && <SessionsTab />}
      </div>
    </div>
  );
}

// ── PROFILE TAB ──────────────────────────────────────────────
function ProfileTab({ user, setUser }) {
  const queryClient = useQueryClient();
  const { register, handleSubmit, formState: { errors, isDirty } } = useForm({
    defaultValues: {
      fullName:   user?.fullName   ?? '',
      jobTitle:   user?.jobTitle   ?? '',
      department: user?.department ?? '',
      bio:        user?.bio        ?? '',
    },
  });

  const updateMutation = useMutation({
    mutationFn: usersApi.updateProfile,
    onSuccess: (data) => {
      setUser(data.data);
      queryClient.invalidateQueries({ queryKey: ['me'] });
      toast.success('Profile updated successfully!');
    },
    onError: () => toast.error('Failed to update profile.'),
  });

  return (
    <form onSubmit={handleSubmit((d) => updateMutation.mutate(d))} className="glass rounded-2xl p-6 space-y-5">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
        <Input label="Full Name" required error={errors.fullName?.message}
          {...register('fullName', { required: 'Name is required', minLength: { value: 3, message: 'At least 3 chars' } })} />
        <Input label="Job Title" placeholder="e.g. Senior Developer"
          {...register('jobTitle')} />
        <Input label="Department" placeholder="e.g. Engineering"
          {...register('department')} />
        <div>
          <label className="text-sm font-medium text-white/80 block mb-1.5">Email</label>
          <div className="px-3.5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-sm text-white/50">
            {user?.email}
            <span className="ml-2 text-xs bg-emerald-500/20 text-emerald-400 px-2 py-0.5 rounded-full">
              Verified
            </span>
          </div>
        </div>
      </div>
      <Textarea label="Bio" rows={3} placeholder="Tell your team a bit about yourself..."
        {...register('bio')} />
      <div className="flex justify-end">
        <Button type="submit" loading={updateMutation.isPending} disabled={!isDirty}>
          Save Changes
        </Button>
      </div>
    </form>
  );
}

// ── SECURITY TAB ─────────────────────────────────────────────
function SecurityTab() {
  const { changePasswordMutation } = useAuth();
  const { register, handleSubmit, watch, reset, formState: { errors } } = useForm();
  const pw = watch('newPassword');

  const onSubmit = (data) => {
    changePasswordMutation.mutate(data, { onSuccess: () => reset() });
  };

  return (
    <div className="glass rounded-2xl p-6 space-y-6">
      <div>
        <h3 className="text-base font-semibold text-white mb-1">Change Password</h3>
        <p className="text-sm text-white/40">After changing your password, you&apos;ll be logged out on all devices.</p>
      </div>
      <form onSubmit={handleSubmit(onSubmit)} className="max-w-md space-y-4">
        <Input label="Current Password" type="password" required error={errors.currentPassword?.message}
          {...register('currentPassword', { required: 'Required' })} />
        <Input label="New Password" type="password" required error={errors.newPassword?.message}
          hint="Min. 8 chars with uppercase, lowercase, number & special char"
          {...register('newPassword', {
            required: 'Required', minLength: { value: 8, message: 'At least 8 characters' },
            pattern: { value: /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])/, message: 'Must include uppercase, lowercase, number & special char' },
          })} />
        <Input label="Confirm New Password" type="password" required error={errors.confirm?.message}
          {...register('confirm', {
            required: 'Required',
            validate: (v) => v === pw || 'Passwords do not match',
          })} />
        <Button type="submit" loading={changePasswordMutation.isPending} variant="danger">
          Change Password
        </Button>
      </form>

      <div className="border-t border-white/10 pt-6">
        <h3 className="text-base font-semibold text-white mb-1">Two-Factor Authentication</h3>
        <p className="text-sm text-white/40 mb-4">Add an extra layer of security to your account.</p>
        <Button variant="outline">Enable 2FA</Button>
      </div>
    </div>
  );
}

// ── NOTIFICATION SETTINGS TAB ────────────────────────────────
function NotifSettingsTab() {
  const settings = [
    { key: 'emailOnTaskAssigned', label: 'Task Assigned',     desc: 'When a task is assigned to you'   },
    { key: 'emailOnCommentAdded', label: 'Comment Added',     desc: 'When someone comments on your task'},
    { key: 'emailOnMentioned',    label: 'Mentioned',         desc: 'When you are @mentioned'           },
    { key: 'emailOnTaskOverdue',  label: 'Task Overdue',      desc: 'When a task passes its due date'   },
    { key: 'inAppOnAll',          label: 'In-App (All)',      desc: 'All in-app notifications'          },
  ];

  return (
    <div className="glass rounded-2xl p-6 space-y-5">
      <div>
        <h3 className="text-base font-semibold text-white mb-1">Notification Preferences</h3>
        <p className="text-sm text-white/40">Choose which events send you notifications.</p>
      </div>
      <div className="divide-y divide-white/8">
        {settings.map((s) => (
          <div key={s.key} className="flex items-center justify-between py-4 first:pt-0 last:pb-0">
            <div>
              <p className="text-sm font-medium text-white">{s.label}</p>
              <p className="text-xs text-white/40 mt-0.5">{s.desc}</p>
            </div>
            <label className="relative cursor-pointer">
              <input type="checkbox" defaultChecked className="sr-only peer" />
              <div className="w-10 h-5 bg-white/10 rounded-full peer-checked:bg-primary-500 transition-colors duration-200" />
              <div className="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow-sm
                              transition-transform duration-200 peer-checked:translate-x-5" />
            </label>
          </div>
        ))}
      </div>
      <div className="flex justify-end">
        <Button>Save Preferences</Button>
      </div>
    </div>
  );
}

// ── SESSIONS TAB ─────────────────────────────────────────────
function SessionsTab() {
  const MOCK_SESSIONS = [
    { sessionId: 1, deviceInfo: 'Chrome on Windows 11',  ipAddress: '192.168.1.10', createdAt: new Date(Date.now() - 86400000), current: true },
    { sessionId: 2, deviceInfo: 'Firefox on macOS',      ipAddress: '10.0.0.5',     createdAt: new Date(Date.now() - 3 * 86400000) },
    { sessionId: 3, deviceInfo: 'Safari on iPhone 15',   ipAddress: '172.20.10.3',  createdAt: new Date(Date.now() - 7 * 86400000) },
  ];

  return (
    <div className="glass rounded-2xl p-6 space-y-5">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-base font-semibold text-white">Active Sessions</h3>
          <p className="text-sm text-white/40 mt-0.5">Devices where you&apos;re currently logged in.</p>
        </div>
        <Button variant="danger" size="sm">Revoke All</Button>
      </div>
      <div className="space-y-3">
        {MOCK_SESSIONS.map((session) => (
          <div key={session.sessionId}
            className="flex items-center justify-between p-4 rounded-xl bg-white/3 border border-white/8 hover:border-white/15 transition-all">
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-xl bg-white/10 flex items-center justify-center flex-shrink-0">
                <svg className="w-5 h-5 text-white/50" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
                    d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <p className="text-sm font-medium text-white">{session.deviceInfo}</p>
                  {session.current && (
                    <span className="text-xs bg-emerald-500/20 text-emerald-400 px-1.5 py-0.5 rounded-full">
                      Current
                    </span>
                  )}
                </div>
                <p className="text-xs text-white/40 mt-0.5">
                  {session.ipAddress} · {format(session.createdAt, 'MMM d, yyyy')}
                </p>
              </div>
            </div>
            {!session.current && (
              <Button variant="danger" size="sm">Revoke</Button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
