import { createBrowserRouter, Navigate } from 'react-router-dom';
import { Suspense, lazy } from 'react';
import ProtectedRoute from './ProtectedRoute';
import RoleRoute from './RoleRoute';
import DashboardLayout from '@/layouts/DashboardLayout';
import AuthLayout from '@/layouts/AuthLayout';
import PageLoader from '@/components/ui/PageLoader';

/**
 * LAZY IMPORTS — Code Splitting.
 *
 * Beginner: Instead of loading ALL pages at startup, lazy() tells Vite to
 * create separate files (chunks) per page. The Dashboard code only loads
 * when the user actually visits /dashboard. This makes the initial load MUCH faster.
 */
const LoginPage         = lazy(() => import('@/pages/auth/LoginPage'));
const RegisterPage      = lazy(() => import('@/pages/auth/RegisterPage'));
const ForgotPasswordPage= lazy(() => import('@/pages/auth/ForgotPasswordPage'));
const ResetPasswordPage = lazy(() => import('@/pages/auth/ResetPasswordPage'));
const DashboardPage     = lazy(() => import('@/pages/DashboardPage'));
const ProjectsPage      = lazy(() => import('@/pages/ProjectsPage'));
const ProjectDetailPage = lazy(() => import('@/pages/ProjectDetailPage'));
const TasksPage         = lazy(() => import('@/pages/TasksPage'));
const UsersPage         = lazy(() => import('@/pages/UsersPage'));
const NotificationsPage = lazy(() => import('@/pages/NotificationsPage'));
const ProfilePage       = lazy(() => import('@/pages/ProfilePage'));
const NotFoundPage      = lazy(() => import('@/pages/NotFoundPage'));

/**
 * ROUTE STRUCTURE:
 *
 * /                         → Redirect to /dashboard
 * /login                    → LoginPage         (public)
 * /register                 → RegisterPage      (public)
 * /forgot-password          → ForgotPasswordPage(public)
 * /reset-password           → ResetPasswordPage (public)
 * /dashboard                → DashboardPage     (protected — any role)
 * /projects                 → ProjectsPage      (protected)
 * /projects/:id             → ProjectDetailPage (protected)
 * /tasks                    → TasksPage         (protected)
 * /users                    → UsersPage         (Admin only)
 * /notifications            → NotificationsPage (protected)
 * /profile                  → ProfilePage       (protected)
 * *                         → NotFoundPage
 */
export const router = createBrowserRouter([
  // ── Root redirect ─────────────────────────────────────────
  {
    path: '/',
    element: <Navigate to="/dashboard" replace />,
  },

  // ── Public pages (Auth Layout — centered card, no sidebar) ──
  {
    element: <AuthLayout />,
    children: [
      {
        path: 'login',
        element: (
          <Suspense fallback={<PageLoader />}>
            <LoginPage />
          </Suspense>
        ),
      },
      {
        path: 'register',
        element: (
          <Suspense fallback={<PageLoader />}>
            <RegisterPage />
          </Suspense>
        ),
      },
      {
        path: 'forgot-password',
        element: (
          <Suspense fallback={<PageLoader />}>
            <ForgotPasswordPage />
          </Suspense>
        ),
      },
      {
        path: 'reset-password',
        element: (
          <Suspense fallback={<PageLoader />}>
            <ResetPasswordPage />
          </Suspense>
        ),
      },
    ],
  },

  // ── Protected pages (Dashboard Layout — with sidebar) ────────
  {
    element: (
      <ProtectedRoute>
        <DashboardLayout />
      </ProtectedRoute>
    ),
    children: [
      {
        path: 'dashboard',
        element: (
          <Suspense fallback={<PageLoader />}>
            <DashboardPage />
          </Suspense>
        ),
      },
      {
        path: 'projects',
        element: (
          <Suspense fallback={<PageLoader />}>
            <ProjectsPage />
          </Suspense>
        ),
      },
      {
        path: 'projects/:id',
        element: (
          <Suspense fallback={<PageLoader />}>
            <ProjectDetailPage />
          </Suspense>
        ),
      },
      {
        path: 'tasks',
        element: (
          <Suspense fallback={<PageLoader />}>
            <TasksPage />
          </Suspense>
        ),
      },
      {
        path: 'notifications',
        element: (
          <Suspense fallback={<PageLoader />}>
            <NotificationsPage />
          </Suspense>
        ),
      },
      {
        path: 'profile',
        element: (
          <Suspense fallback={<PageLoader />}>
            <ProfilePage />
          </Suspense>
        ),
      },

      // ── Admin-only route ─────────────────────────────────
      {
        path: 'users',
        element: (
          <RoleRoute allowedRoles={['Admin']}>
            <Suspense fallback={<PageLoader />}>
              <UsersPage />
            </Suspense>
          </RoleRoute>
        ),
      },
    ],
  },

  // ── 404 page ──────────────────────────────────────────────
  {
    path: '*',
    element: (
      <Suspense fallback={<PageLoader />}>
        <NotFoundPage />
      </Suspense>
    ),
  },
]);
