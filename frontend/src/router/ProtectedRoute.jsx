import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import PageLoader from '@/components/ui/PageLoader';

/**
 * PROTECTED ROUTE
 *
 * Beginner: This is a "guard" component.
 * Before showing any protected page, it checks:
 *   1. Are we still loading? → Show a spinner
 *   2. Is the user logged in? → Show the page
 *   3. Not logged in?         → Redirect to /login (and remember where they were)
 *
 * The 'state={{ from: location }}' saves the page they tried to visit,
 * so after login we can send them back there automatically.
 *
 * HOW TO USE:
 *   <ProtectedRoute>
 *     <MyPage />
 *   </ProtectedRoute>
 */
export default function ProtectedRoute({ children }) {
  const { isAuth, isLoading } = useAuthStore();
  const location = useLocation();

  if (isLoading) {
    return <PageLoader message="Verifying session..." />;
  }

  if (!isAuth) {
    // Redirect to login, but remember the page they were trying to visit
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children;
}
