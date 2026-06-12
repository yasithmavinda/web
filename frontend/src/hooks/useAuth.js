import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { authApi } from '@/api';
import toast from 'react-hot-toast';

// ── DEMO USERS (work without any backend) ────────────────────────
const DEMO_USERS = {
  'admin@taskflow.com': {
    user: {
      userId: 1, fullName: 'Alex Johnson',
      email: 'admin@taskflow.com',
      roleId: 1, roleName: 'Admin',
      avatarUrl: null, isActive: true,
      createdAt: '2024-01-01T00:00:00Z',
    },
    accessToken:  'demo-token-admin',
    refreshToken: 'demo-refresh-admin',
  },
  'pm@taskflow.com': {
    user: {
      userId: 2, fullName: 'Sarah Miller',
      email: 'pm@taskflow.com',
      roleId: 2, roleName: 'Project Manager',
      avatarUrl: null, isActive: true,
      createdAt: '2024-01-01T00:00:00Z',
    },
    accessToken:  'demo-token-pm',
    refreshToken: 'demo-refresh-pm',
  },
  'dev@taskflow.com': {
    user: {
      userId: 3, fullName: 'Chris Park',
      email: 'dev@taskflow.com',
      roleId: 3, roleName: 'Collaborator',
      avatarUrl: null, isActive: true,
      createdAt: '2024-01-01T00:00:00Z',
    },
    accessToken:  'demo-token-dev',
    refreshToken: 'demo-refresh-dev',
  },
};

const DEMO_PASSWORD = 'Admin@123';

function tryDemoLogin(email, password) {
  const u = DEMO_USERS[email?.trim().toLowerCase()];
  if (u && password === DEMO_PASSWORD) return u;
  return null;
}

// ── useAuth HOOK ─────────────────────────────────────────────────
export function useAuth() {
  const { setAuth, logout } = useAuthStore();
  const navigate            = useNavigate();
  const location            = useLocation();
  const queryClient         = useQueryClient();
  const from = location.state?.from?.pathname ?? '/dashboard';

  const loginMutation = useMutation({
    mutationFn: async ({ email, password }) => {
      return authApi.login({ email, password });
    },
    onSuccess: (res) => {
      setAuth(res.data);
      const first = res.data.user.fullName.split(' ')[0];
      toast.success(`Welcome back, ${first}!`, { duration: 3000 });
      navigate(from, { replace: true });
    },
    onError: (err) => {
      const msg = err.response?.data?.message;
      if (msg) {
        toast.error(msg);
      } else {
        toast.error('Network Error: Could not connect to the backend server.');
      }
    },
  });

  // REGISTER
  const registerMutation = useMutation({
    mutationFn: async (data) => {
      try { return await authApi.register(data); }
      catch {
        await new Promise((r) => setTimeout(r, 700));
        return {
          data: {
            user: { userId: 99, fullName: data.fullName, email: data.email,
                    roleId: 3, roleName: 'Collaborator', avatarUrl: null,
                    isActive: true, createdAt: new Date().toISOString() },
            accessToken: 'demo-token-new', refreshToken: 'demo-refresh-new',
          },
        };
      }
    },
    onSuccess: (res) => {
      setAuth(res.data);
      toast.success('Account created! Welcome to TaskFlow');
      navigate('/dashboard', { replace: true });
    },
    onError: (err) => toast.error(err.response?.data?.message ?? 'Registration failed.'),
  });

  // LOGOUT
  const logoutMutation = useMutation({
    mutationFn: async () => {
      const token = useAuthStore.getState().refreshToken;
      if (token && !token.startsWith('demo-')) {
        await authApi.logout(token).catch(() => {});
      }
    },
    onSettled: () => {
      logout();
      queryClient.clear();
      navigate('/login', { replace: true });
    },
  });

  // FORGOT PASSWORD
  const forgotPasswordMutation = useMutation({
    mutationFn: async (data) => {
      await new Promise((r) => setTimeout(r, 800));
      return authApi.forgotPassword(data).catch(() => {});
    },
    onSuccess: () => toast.success('If this email exists, a reset link was sent.'),
    onError:   () => toast.success('If this email exists, a reset link was sent.'),
  });

  // RESET PASSWORD
  const resetPasswordMutation = useMutation({
    mutationFn: authApi.resetPassword,
    onSuccess: () => { toast.success('Password reset! Please login.'); navigate('/login'); },
    onError: (err) => toast.error(err.response?.data?.message ?? 'Link invalid or expired.'),
  });

  // CHANGE PASSWORD
  const changePasswordMutation = useMutation({
    mutationFn: async (data) => {
      const token = useAuthStore.getState().accessToken;
      if (token?.startsWith('demo-')) {
        await new Promise((r) => setTimeout(r, 600));
        return {};
      }
      return authApi.changePassword(data);
    },
    onSuccess: () => { toast.success('Password changed.'); logout(); navigate('/login'); },
    onError: (err) => toast.error(err.response?.data?.message ?? 'Failed to change password.'),
  });

  return {
    loginMutation, registerMutation, logoutMutation,
    forgotPasswordMutation, resetPasswordMutation, changePasswordMutation,
  };
}
