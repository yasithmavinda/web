import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

/**
 * AUTH STORE — Global authentication state using Zustand.
 *
 * Beginner: Zustand is like a "global variable" that React knows how to watch.
 * When authStore changes (e.g., user logs in), ALL components that read from
 * it automatically re-render with the new data.
 *
 * 'persist' middleware saves state to localStorage so the user stays
 * logged in even after refreshing the browser.
 *
 * STATE:
 *   user         — The logged-in user's profile data
 *   accessToken  — Short-lived JWT (15 minutes)
 *   refreshToken — Long-lived token used to get new access tokens (7 days)
 *   isAuth       — Boolean: is the user logged in?
 *   isLoading    — True while checking auth on app startup
 *
 * ACTIONS:
 *   setAuth()    — Called after login/register
 *   setTokens()  — Called after token refresh
 *   setUser()    — Called after profile update
 *   logout()     — Clears everything
 */
export const useAuthStore = create(
  persist(
    (set, get) => ({
      // ── State ─────────────────────────────────────────────
      user:         null,
      accessToken:  null,
      refreshToken: null,
      isAuth:       false,
      isLoading:    true,  // True until persist rehydrates from localStorage

      // ── Actions ───────────────────────────────────────────
      /** Called after a successful login or register */
      setAuth: (authResult) => set({
        user:         authResult.user,
        accessToken:  authResult.accessToken,
        refreshToken: authResult.refreshToken,
        isAuth:       true,
        isLoading:    false,
      }),

      /** Called after token refresh — update tokens only */
      setTokens: (accessToken, refreshToken) => set({ accessToken, refreshToken }),

      /** Called after profile update */
      setUser: (user) => set({ user }),

      /** Partial user update (e.g., just avatar) */
      updateUser: (patch) => set((state) => ({
        user: state.user ? { ...state.user, ...patch } : null,
      })),

      /** Clear everything — user is logged out */
      logout: () => set({
        user:         null,
        accessToken:  null,
        refreshToken: null,
        isAuth:       false,
        isLoading:    false,
      }),

      /** Mark loading as done (after startup check) */
      setLoaded: () => set({ isLoading: false }),

      // ── Computed getters ──────────────────────────────────
      get isAdmin()         { return get().user?.roleId === 1; },
      get isProjectManager(){ return get().user?.roleId <= 2; },
      get roleName()        { return get().user?.roleName ?? 'Guest'; },
    }),

    {
      name:    'taskflow-auth',      // localStorage key
      storage: createJSONStorage(() => localStorage),

      // Only persist these fields — DON'T persist isLoading
      partialize: (state) => ({
        user:         state.user,
        accessToken:  state.accessToken,
        refreshToken: state.refreshToken,
        isAuth:       state.isAuth,
      }),

      // ✅ THIS IS THE KEY FIX:
      // Called automatically by Zustand after it loads saved state from
      // localStorage. We set isLoading:false so the spinner disappears
      // and the app either shows the dashboard (if logged in) or
      // redirects to /login (if not logged in).
      onRehydrateStorage: () => (state, error) => {
        if (state) {
          state.setLoaded();
        }
      },
    }
  )
);

