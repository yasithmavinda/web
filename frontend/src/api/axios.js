import axios from 'axios';
import { useAuthStore } from '@/store/authStore';

/**
 * AXIOS INSTANCE — The central HTTP client for ALL API calls.
 *
 * Beginner: Instead of calling axios.get() directly everywhere,
 * we create ONE configured instance with:
 *   1. A base URL — so we write '/auth/login' instead of 'http://localhost:5000/api/v1/auth/login'
 *   2. Default headers — Content-Type: application/json on every request
 *   3. Request interceptor  — auto-attach the JWT token to EVERY request
 *   4. Response interceptor — auto-refresh expired tokens (transparent to the rest of the app)
 */
const api = axios.create({
  baseURL: `${import.meta.env.VITE_API_URL ?? 'http://localhost:5000'}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000, // Fail if server doesn't respond in 15 seconds
});

// ── REQUEST INTERCEPTOR ─────────────────────────────────────────
// Runs BEFORE every request — attaches the JWT token
api.interceptors.request.use(
  (config) => {
    const token = useAuthStore.getState().accessToken;
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// ── RESPONSE INTERCEPTOR ────────────────────────────────────────
// Track ongoing refresh so we don't do it twice simultaneously
let isRefreshing = false;
let pendingRequests = []; // Requests that arrived while refreshing

api.interceptors.response.use(
  // Success — just return the response as-is
  (response) => response,

  // Error — handle 401 (Unauthorized) by refreshing the token
  async (error) => {
    const originalRequest = error.config;

    // If error is 401 AND we haven't already retried this request
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true; // Mark so we don't loop

      if (isRefreshing) {
        // Another request is already refreshing — queue this one
        return new Promise((resolve, reject) => {
          pendingRequests.push({ resolve, reject });
        }).then(() => api(originalRequest));
      }

      isRefreshing = true;

      try {
        const { refreshToken, setTokens, logout } = useAuthStore.getState();
        if (!refreshToken) throw new Error('No refresh token');

        // Call the refresh endpoint
        const response = await axios.post(
          `${import.meta.env.VITE_API_URL ?? 'http://localhost:5000'}/api/v1/auth/refresh-token`,
          { refreshToken }
        );

        const { accessToken: newAccess, refreshToken: newRefresh } = response.data.data;

        // Store the new tokens
        setTokens(newAccess, newRefresh);

        // Retry all queued requests with new token
        pendingRequests.forEach(({ resolve }) => resolve());
        pendingRequests = [];

        // Retry the original failed request
        return api(originalRequest);

      } catch (refreshError) {
        // Refresh failed — logout the user
        pendingRequests.forEach(({ reject }) => reject(refreshError));
        pendingRequests = [];
        useAuthStore.getState().logout();
        window.location.href = '/login';
        return Promise.reject(refreshError);

      } finally {
        isRefreshing = false;
      }
    }

    // For all other errors, just reject so the calling code can handle them
    return Promise.reject(error);
  }
);

export default api;
