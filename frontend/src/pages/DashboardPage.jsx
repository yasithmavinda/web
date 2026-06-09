import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  AreaChart, Area, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts';
import { useAuthStore } from '@/store/authStore';
import { tasksApi, projectsApi, notificationsApi } from '@/api';
import { StatusBadge, PriorityBadge } from '@/components/ui/Badge';
import Avatar, { AvatarGroup } from '@/components/ui/Avatar';
import { CardSkeleton } from '@/components/ui/PageLoader';
import { formatDistanceToNow, format, subDays } from 'date-fns';
import { clsx } from 'clsx';

// ── Mock data for charts (replace with API data in production) ─
const ACTIVITY_DATA = Array.from({ length: 7 }, (_, i) => ({
  day: format(subDays(new Date(), 6 - i), 'EEE'),
  completed: Math.floor(Math.random() * 8) + 2,
  created:   Math.floor(Math.random() * 6) + 1,
}));

const TASK_STATUS_DATA = [
  { name: 'Done',       value: 34, color: '#10b981' },
  { name: 'In Progress',value: 18, color: '#f59e0b' },
  { name: 'In Review',  value:  9, color: '#8b5cf6' },
  { name: 'Todo',       value: 12, color: '#6366f1' },
  { name: 'Backlog',    value: 11, color: '#64748b' },
  { name: 'Blocked',    value:  4, color: '#ef4444' },
];

const PRIORITY_DATA = [
  { name: 'Critical', tasks: 8  },
  { name: 'High',     tasks: 19 },
  { name: 'Medium',   tasks: 41 },
  { name: 'Low',      tasks: 20 },
];

const RECENT_TASKS = [
  { taskId: 1, title: 'Implement JWT refresh token rotation',  status: 'InProgress', priority: 'High',     projectName: 'TaskFlow API',    assignees: [{ fullName: 'Alice M.' }, { fullName: 'Bob K.' }],   dueDate: '2025-06-10' },
  { taskId: 2, title: 'Design Kanban board drag-and-drop UX', status: 'InReview',   priority: 'Medium',   projectName: 'Frontend App',    assignees: [{ fullName: 'Carol D.' }],                            dueDate: '2025-06-08' },
  { taskId: 3, title: 'Write API integration tests',           status: 'Todo',       priority: 'High',     projectName: 'TaskFlow API',    assignees: [{ fullName: 'Dave S.' }, { fullName: 'Eve R.' }],    dueDate: '2025-06-12' },
  { taskId: 4, title: 'Configure Docker Compose for staging',  status: 'Blocked',    priority: 'Critical', projectName: 'DevOps',          assignees: [{ fullName: 'Frank L.' }],                            dueDate: '2025-06-07' },
  { taskId: 5, title: 'Deploy notification email templates',   status: 'Done',       priority: 'Medium',   projectName: 'TaskFlow API',    assignees: [{ fullName: 'Grace H.' }],                            dueDate: '2025-06-05' },
];

const RECENT_ACTIVITY = [
  { id: 1, type: 'status_changed',  actor: 'Alice M.',  message: 'moved "Setup CI/CD Pipeline" to Done',                     time: new Date(Date.now() - 5  * 60_000) },
  { id: 2, type: 'task_created',    actor: 'Bob K.',    message: 'created "Implement file upload API"',                       time: new Date(Date.now() - 22 * 60_000) },
  { id: 3, type: 'comment_added',   actor: 'Carol D.',  message: 'commented on "Design system tokens"',                       time: new Date(Date.now() - 55 * 60_000) },
  { id: 4, type: 'task_assigned',   actor: 'Dave S.',   message: 'assigned "Write unit tests" to Eve R.',                     time: new Date(Date.now() - 2  * 3600_000) },
  { id: 5, type: 'status_changed',  actor: 'Eve R.',    message: 'moved "Database schema review" to In Review',               time: new Date(Date.now() - 4  * 3600_000) },
  { id: 6, type: 'project_created', actor: 'Frank L.',  message: 'created project "Mobile App v2"',                           time: new Date(Date.now() - 6  * 3600_000) },
];

const STAT_CARDS = [
  {
    label:   'Total Tasks',
    value:   '88',
    change:  '+12%',
    positive: true,
    icon: (
      <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
      </svg>
    ),
    gradient: 'from-primary-500/20 to-primary-600/5',
    iconBg:   'bg-primary-500/20 text-primary-400',
    sparkData: [4, 7, 5, 9, 6, 12, 10, 14, 11, 16],
  },
  {
    label:   'Completed',
    value:   '34',
    change:  '+8%',
    positive: true,
    icon: (
      <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
    gradient: 'from-emerald-500/20 to-emerald-600/5',
    iconBg:   'bg-emerald-500/20 text-emerald-400',
    sparkData: [2, 4, 3, 7, 5, 9, 8, 11, 10, 13],
  },
  {
    label:   'In Progress',
    value:   '18',
    change:  '+3',
    positive: true,
    icon: (
      <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M13 10V3L4 14h7v7l9-11h-7z" />
      </svg>
    ),
    gradient: 'from-amber-500/20 to-amber-600/5',
    iconBg:   'bg-amber-500/20 text-amber-400',
    sparkData: [3, 5, 4, 6, 7, 5, 8, 6, 9, 7],
  },
  {
    label:   'Overdue',
    value:   '5',
    change:  '-2',
    positive: false,
    icon: (
      <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.8}
          d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
    gradient: 'from-red-500/20 to-red-600/5',
    iconBg:   'bg-red-500/20 text-red-400',
    sparkData: [8, 7, 9, 6, 8, 7, 6, 5, 6, 5],
  },
];

// ── Custom Tooltip for charts ─────────────────────────────────
const ChartTooltip = ({ active, payload, label }) => {
  if (!active || !payload?.length) return null;
  return (
    <div className="bg-surface-800 border border-white/10 rounded-xl px-3 py-2 text-xs shadow-xl">
      <p className="text-white/50 mb-1">{label}</p>
      {payload.map((p) => (
        <div key={p.dataKey} className="flex items-center gap-2">
          <span className="w-2 h-2 rounded-full" style={{ background: p.color }} />
          <span className="text-white font-medium">{p.value}</span>
          <span className="text-white/40 capitalize">{p.dataKey}</span>
        </div>
      ))}
    </div>
  );
};

// ── Sparkline (mini chart in stat card) ──────────────────────
const Sparkline = ({ data, color }) => (
  <ResponsiveContainer width="100%" height={48}>
    <AreaChart data={data.map((v, i) => ({ v, i }))} margin={{ top: 4, right: 0, bottom: 0, left: 0 }}>
      <defs>
        <linearGradient id={`grad-${color}`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity={0.4} />
          <stop offset="100%" stopColor={color} stopOpacity={0} />
        </linearGradient>
      </defs>
      <Area type="monotone" dataKey="v" stroke={color} strokeWidth={2}
        fill={`url(#grad-${color})`} dot={false} />
    </AreaChart>
  </ResponsiveContainer>
);

const SPARK_COLORS = ['#6366f1', '#10b981', '#f59e0b', '#ef4444'];

// ── Activity Icon ─────────────────────────────────────────────
const ACTIVITY_ICONS = {
  status_changed:  { bg: 'bg-emerald-500/20', icon: '✓',  color: 'text-emerald-400' },
  task_created:    { bg: 'bg-primary-500/20', icon: '+',  color: 'text-primary-400' },
  comment_added:   { bg: 'bg-violet-500/20',  icon: '💬', color: 'text-violet-400'  },
  task_assigned:   { bg: 'bg-amber-500/20',   icon: '→',  color: 'text-amber-400'   },
  project_created: { bg: 'bg-sky-500/20',     icon: '📁', color: 'text-sky-400'     },
};

// ────────────────────────────────────────────────────────────────
// DASHBOARD PAGE
// ────────────────────────────────────────────────────────────────
export default function DashboardPage() {
  const user = useAuthStore((s) => s.user);
  const [activeTab, setActiveTab] = useState('all');

  const now = new Date();
  const hour = now.getHours();
  const greeting = hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening';

  const filteredTasks = activeTab === 'all'
    ? RECENT_TASKS
    : RECENT_TASKS.filter((t) => t.status.toLowerCase().replace(' ', '') === activeTab);

  const completionRate = Math.round((34 / 88) * 100);

  return (
    <div className="space-y-6 max-w-[1600px]">

      {/* ── Page Header ─────────────────────────────────────── */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white">
            {greeting}, <span className="gradient-text">{user?.fullName?.split(' ')[0] ?? 'there'}</span> 👋
          </h1>
          <p className="text-white/40 text-sm mt-1">
            {format(now, 'EEEE, MMMM d, yyyy')} · Here&apos;s your workspace overview
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Link to="/tasks?status=overdue"
            className="flex items-center gap-2 px-4 py-2 rounded-xl bg-red-500/15 border border-red-500/20
                       text-red-400 text-sm font-medium hover:bg-red-500/20 transition-all">
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            5 Overdue
          </Link>

          <button
            onClick={() => {/* open create task modal */}}
            className="flex items-center gap-2 px-4 py-2 rounded-xl bg-primary-500 hover:bg-primary-600
                       text-white text-sm font-medium shadow-glow-sm transition-all active:scale-95">
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            New Task
          </button>
        </div>
      </div>

      {/* ── Stat Cards ──────────────────────────────────────── */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        {STAT_CARDS.map((card, i) => (
          <StatCard key={card.label} card={card} color={SPARK_COLORS[i]} index={i} />
        ))}
      </div>

      {/* ── Middle Row: Activity Chart + Donut ──────────────── */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Area Chart */}
        <div className="lg:col-span-2 glass rounded-2xl p-5">
          <div className="flex items-center justify-between mb-5">
            <div>
              <h2 className="text-base font-semibold text-white">Task Activity</h2>
              <p className="text-xs text-white/40 mt-0.5">Last 7 days overview</p>
            </div>
            <div className="flex items-center gap-4 text-xs">
              <div className="flex items-center gap-1.5 text-white/50">
                <span className="w-3 h-0.5 rounded bg-primary-400 inline-block" />
                Completed
              </div>
              <div className="flex items-center gap-1.5 text-white/50">
                <span className="w-3 h-0.5 rounded bg-amber-400 inline-block" />
                Created
              </div>
            </div>
          </div>
          <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={ACTIVITY_DATA} margin={{ top: 4, right: 4, bottom: 0, left: -20 }}>
              <defs>
                <linearGradient id="gradCompleted" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%"   stopColor="#6366f1" stopOpacity={0.35} />
                  <stop offset="100%" stopColor="#6366f1" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="gradCreated" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%"   stopColor="#f59e0b" stopOpacity={0.25} />
                  <stop offset="100%" stopColor="#f59e0b" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
              <XAxis dataKey="day" tick={{ fill: 'rgba(255,255,255,0.35)', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: 'rgba(255,255,255,0.35)', fontSize: 11 }} axisLine={false} tickLine={false} />
              <Tooltip content={<ChartTooltip />} cursor={{ stroke: 'rgba(255,255,255,0.08)', strokeWidth: 1 }} />
              <Area type="monotone" dataKey="completed" stroke="#6366f1" strokeWidth={2.5}
                fill="url(#gradCompleted)" dot={{ fill: '#6366f1', strokeWidth: 0, r: 3 }}
                activeDot={{ r: 5, fill: '#6366f1', strokeWidth: 2, stroke: '#fff' }} />
              <Area type="monotone" dataKey="created" stroke="#f59e0b" strokeWidth={2}
                fill="url(#gradCreated)" dot={{ fill: '#f59e0b', strokeWidth: 0, r: 3 }}
                activeDot={{ r: 5, fill: '#f59e0b', strokeWidth: 2, stroke: '#fff' }} />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* Donut / Task Status */}
        <div className="glass rounded-2xl p-5">
          <div className="mb-4">
            <h2 className="text-base font-semibold text-white">Task Status</h2>
            <p className="text-xs text-white/40 mt-0.5">Distribution across all projects</p>
          </div>

          {/* Completion circle */}
          <div className="relative flex justify-center mb-2">
            <ResponsiveContainer width={180} height={180}>
              <PieChart>
                <Pie
                  data={TASK_STATUS_DATA}
                  cx="50%" cy="50%"
                  innerRadius={58} outerRadius={80}
                  paddingAngle={3}
                  dataKey="value"
                  strokeWidth={0}
                >
                  {TASK_STATUS_DATA.map((entry) => (
                    <Cell key={entry.name} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip content={<ChartTooltip />} />
              </PieChart>
            </ResponsiveContainer>
            {/* Center text */}
            <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
              <span className="text-3xl font-bold text-white">{completionRate}%</span>
              <span className="text-xs text-white/40">Complete</span>
            </div>
          </div>

          {/* Legend */}
          <div className="space-y-1.5">
            {TASK_STATUS_DATA.map((item) => (
              <div key={item.name} className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ background: item.color }} />
                  <span className="text-xs text-white/60">{item.name}</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-16 h-1 rounded-full bg-white/10">
                    <div className="h-full rounded-full transition-all duration-700"
                      style={{ width: `${(item.value / 88) * 100}%`, background: item.color }} />
                  </div>
                  <span className="text-xs font-medium text-white/70 w-4 text-right">{item.value}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* ── Bottom Row: Tasks Table + Priority Bar + Activity ── */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">

        {/* Recent Tasks Table */}
        <div className="xl:col-span-2 glass rounded-2xl overflow-hidden">
          <div className="p-5 border-b border-white/8">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-base font-semibold text-white">Recent Tasks</h2>
                <p className="text-xs text-white/40 mt-0.5">Your latest assigned work</p>
              </div>
              <Link to="/tasks"
                className="text-xs text-primary-400 hover:text-primary-300 font-medium transition-colors
                           flex items-center gap-1">
                View all
                <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </Link>
            </div>

            {/* Filter tabs */}
            <div className="flex gap-1 mt-4">
              {[
                { key: 'all',        label: 'All',         count: 88 },
                { key: 'inprogress', label: 'In Progress', count: 18 },
                { key: 'inreview',   label: 'In Review',   count: 9  },
                { key: 'blocked',    label: 'Blocked',     count: 4  },
              ].map((tab) => (
                <button
                  key={tab.key}
                  onClick={() => setActiveTab(tab.key)}
                  className={clsx(
                    'px-3 py-1.5 rounded-lg text-xs font-medium transition-all',
                    activeTab === tab.key
                      ? 'bg-primary-500/20 text-primary-400 border border-primary-500/30'
                      : 'text-white/40 hover:text-white/70 hover:bg-white/5'
                  )}
                >
                  {tab.label}
                  <span className={clsx(
                    'ml-1.5 px-1.5 py-0.5 rounded-full text-[10px]',
                    activeTab === tab.key ? 'bg-primary-500/30' : 'bg-white/10'
                  )}>
                    {tab.count}
                  </span>
                </button>
              ))}
            </div>
          </div>

          {/* Task rows */}
          <div className="divide-y divide-white/5">
            {RECENT_TASKS.map((task) => (
              <TaskRow key={task.taskId} task={task} />
            ))}
          </div>
        </div>

        {/* Right column */}
        <div className="flex flex-col gap-4">

          {/* Priority Distribution Bar Chart */}
          <div className="glass rounded-2xl p-5">
            <div className="mb-4">
              <h2 className="text-base font-semibold text-white">By Priority</h2>
              <p className="text-xs text-white/40 mt-0.5">Tasks per priority level</p>
            </div>
            <ResponsiveContainer width="100%" height={140}>
              <BarChart data={PRIORITY_DATA} margin={{ top: 0, right: 0, bottom: 0, left: -28 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" horizontal={true} vertical={false} />
                <XAxis dataKey="name" tick={{ fill: 'rgba(255,255,255,0.4)', fontSize: 10 }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fill: 'rgba(255,255,255,0.4)', fontSize: 10 }} axisLine={false} tickLine={false} />
                <Tooltip content={<ChartTooltip />} cursor={{ fill: 'rgba(255,255,255,0.04)' }} />
                <Bar dataKey="tasks" radius={[4, 4, 0, 0]}>
                  {PRIORITY_DATA.map((entry) => (
                    <Cell key={entry.name}
                      fill={
                        entry.name === 'Critical' ? '#ef4444' :
                        entry.name === 'High'     ? '#f97316' :
                        entry.name === 'Medium'   ? '#f59e0b' : '#10b981'
                      }
                    />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>

          {/* Recent Activity Feed */}
          <div className="glass rounded-2xl p-5 flex-1">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h2 className="text-base font-semibold text-white">Activity</h2>
                <p className="text-xs text-white/40 mt-0.5">Team actions</p>
              </div>
              <Link to="/notifications"
                className="text-xs text-primary-400 hover:text-primary-300 transition-colors">
                See all
              </Link>
            </div>
            <div className="space-y-3">
              {RECENT_ACTIVITY.map((item) => {
                const cfg = ACTIVITY_ICONS[item.type] ?? ACTIVITY_ICONS.task_created;
                return (
                  <div key={item.id} className="flex items-start gap-3 group">
                    {/* Icon */}
                    <div className={clsx(
                      'w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0 text-xs',
                      cfg.bg, cfg.color
                    )}>
                      {cfg.icon}
                    </div>
                    {/* Text */}
                    <div className="min-w-0 flex-1">
                      <p className="text-xs text-white/80 leading-relaxed">
                        <span className="font-semibold text-white">{item.actor}</span>{' '}
                        {item.message}
                      </p>
                      <p className="text-[10px] text-white/30 mt-0.5">
                        {formatDistanceToNow(item.time, { addSuffix: true })}
                      </p>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </div>

      {/* ── Project Progress Row ─────────────────────────────── */}
      <ProjectProgressSection />
    </div>
  );
}

// ── STAT CARD COMPONENT ──────────────────────────────────────
function StatCard({ card, color, index }) {
  return (
    <div
      className={clsx(
        'relative overflow-hidden glass rounded-2xl p-5 card-hover group',
        'animate-fade-in'
      )}
      style={{ animationDelay: `${index * 80}ms` }}
    >
      {/* Background gradient */}
      <div className={clsx('absolute inset-0 bg-gradient-to-br opacity-60', card.gradient)} />

      <div className="relative z-10">
        {/* Top row */}
        <div className="flex items-start justify-between mb-4">
          <div className={clsx('w-10 h-10 rounded-xl flex items-center justify-center', card.iconBg)}>
            {card.icon}
          </div>
          <span className={clsx(
            'flex items-center gap-1 text-xs font-semibold px-2 py-1 rounded-full',
            card.positive
              ? 'text-emerald-400 bg-emerald-500/15'
              : 'text-red-400 bg-red-500/15'
          )}>
            {card.positive ? '↑' : '↓'} {card.change}
          </span>
        </div>

        {/* Value */}
        <div className="mb-1">
          <span className="text-3xl font-bold text-white">{card.value}</span>
        </div>
        <p className="text-sm text-white/50">{card.label}</p>

        {/* Sparkline */}
        <div className="mt-3 -mx-1 opacity-70 group-hover:opacity-100 transition-opacity">
          <Sparkline data={card.sparkData} color={color} />
        </div>
      </div>
    </div>
  );
}

// ── TASK ROW COMPONENT ───────────────────────────────────────
function TaskRow({ task }) {
  const isOverdue = task.dueDate && new Date(task.dueDate) < new Date() && task.status !== 'Done';

  return (
    <Link
      to={`/tasks/${task.taskId}`}
      className="flex items-center gap-4 px-5 py-3.5 hover:bg-white/3 transition-all group"
    >
      {/* Title + Project */}
      <div className="flex-1 min-w-0">
        <p className={clsx(
          'text-sm font-medium truncate transition-colors',
          task.status === 'Done' ? 'line-through text-white/30' : 'text-white group-hover:text-primary-300'
        )}>
          {task.title}
        </p>
        <p className="text-xs text-white/30 mt-0.5">{task.projectName}</p>
      </div>

      {/* Status */}
      <div className="hidden sm:block flex-shrink-0">
        <StatusBadge status={task.status} />
      </div>

      {/* Priority */}
      <div className="hidden md:block flex-shrink-0">
        <PriorityBadge priority={task.priority} />
      </div>

      {/* Due Date */}
      <div className="hidden lg:flex items-center gap-1.5 flex-shrink-0 text-xs">
        <svg className={clsx('w-3.5 h-3.5', isOverdue ? 'text-red-400' : 'text-white/30')}
          fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
        <span className={isOverdue ? 'text-red-400' : 'text-white/40'}>
          {format(new Date(task.dueDate), 'MMM d')}
        </span>
      </div>

      {/* Assignees */}
      <div className="flex-shrink-0">
        <AvatarGroup users={task.assignees} max={2} size="xs" />
      </div>

      {/* Arrow */}
      <svg className="w-4 h-4 text-white/20 group-hover:text-white/50 transition-colors flex-shrink-0"
        fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
      </svg>
    </Link>
  );
}

// ── PROJECT PROGRESS SECTION ─────────────────────────────────
const PROJECTS = [
  { name: 'TaskFlow API',  total: 32, done: 22, color: '#6366f1', members: [{ fullName: 'Alice M.' }, { fullName: 'Bob K.' }],   status: 'InProgress', due: '2025-06-20' },
  { name: 'Frontend App', total: 28, done: 10, color: '#8b5cf6', members: [{ fullName: 'Carol D.' }, { fullName: 'Dave S.' }],  status: 'InProgress', due: '2025-06-25' },
  { name: 'DevOps Setup', total: 15, done: 2,  color: '#f59e0b', members: [{ fullName: 'Frank L.' }],                            status: 'Blocked',    due: '2025-06-15' },
  { name: 'Mobile App v2',total: 13, done: 0,  color: '#10b981', members: [{ fullName: 'Eve R.' }, { fullName: 'Grace H.' }],   status: 'Todo',       due: '2025-07-01' },
];

function ProjectProgressSection() {
  return (
    <div className="glass rounded-2xl overflow-hidden">
      <div className="flex items-center justify-between p-5 border-b border-white/8">
        <div>
          <h2 className="text-base font-semibold text-white">Project Progress</h2>
          <p className="text-xs text-white/40 mt-0.5">Task completion across active projects</p>
        </div>
        <Link to="/projects"
          className="text-xs text-primary-400 hover:text-primary-300 transition-colors
                     flex items-center gap-1 font-medium">
          All projects
          <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
        </Link>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 divide-y sm:divide-y-0 sm:divide-x divide-white/8">
        {PROJECTS.map((project) => {
          const pct = Math.round((project.done / project.total) * 100);
          return (
            <div key={project.name} className="p-5 hover:bg-white/3 transition-colors group">
              {/* Header */}
              <div className="flex items-start justify-between mb-3">
                <div className="flex items-center gap-2.5">
                  <div className="w-3 h-3 rounded-full flex-shrink-0"
                    style={{ background: project.color, boxShadow: `0 0 8px ${project.color}60` }} />
                  <span className="text-sm font-medium text-white">{project.name}</span>
                </div>
              </div>

              {/* Progress bar */}
              <div className="mb-3">
                <div className="flex items-center justify-between text-xs mb-1.5">
                  <span className="text-white/40">{project.done}/{project.total} tasks</span>
                  <span className="font-semibold" style={{ color: project.color }}>{pct}%</span>
                </div>
                <div className="h-1.5 w-full bg-white/10 rounded-full overflow-hidden">
                  <div
                    className="h-full rounded-full transition-all duration-1000 ease-out"
                    style={{ width: `${pct}%`, background: project.color }}
                  />
                </div>
              </div>

              {/* Footer */}
              <div className="flex items-center justify-between">
                <AvatarGroup users={project.members} max={3} size="xs" />
                <div className="flex items-center gap-1 text-xs text-white/30">
                  <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                      d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                  {format(new Date(project.due), 'MMM d')}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
