import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';

/**
 * ROLE ROUTE — Role-Based Access Control (RBAC) at the route level.
 *
 * Beginner: Some pages should only be visible to certain roles.
 * For example, the /users page is Admin-only.
 *
 * This component checks if the logged-in user's role is in the allowedRoles list.
 * If not, they get a "403 Forbidden" redirect instead of seeing the page.
 *
 * HOW TO USE:
 *   <RoleRoute allowedRoles={['Admin']}>
 *     <UsersPage />
 *   </RoleRoute>
 *
 *   <RoleRoute allowedRoles={['Admin', 'ProjectManager']}>
 *     <ReportsPage />
 *   </RoleRoute>
 */
export default function RoleRoute({ children, allowedRoles }) {
  const user = useAuthStore((state) => state.user);

  // If user's role is not in the allowed list, show forbidden page
  if (!user || !allowedRoles.includes(user.roleName)) {
    return <Navigate to="/dashboard" replace state={{ forbidden: true }} />;
  }

  return children;
}
