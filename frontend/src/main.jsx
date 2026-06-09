import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import App from './App.jsx';
import './index.css';

/**
 * React Query Client
 *
 * Beginner: React Query manages ALL server data (fetching, caching, refetching).
 * It's like a smart cache that knows when data is "stale" and refetches it.
 *
 * staleTime: How long data is considered "fresh" before a background refetch.
 * retry: How many times to retry a failed request before showing an error.
 */
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime:            1000 * 60 * 5,  // 5 minutes — data is fresh for 5 min
      gcTime:               1000 * 60 * 10, // 10 minutes — remove from cache after
      retry:                1,              // Retry failed requests once
      refetchOnWindowFocus: true,           // Refetch when tab becomes active
    },
    mutations: {
      retry: 0, // Don't auto-retry mutations (POST, PUT, DELETE)
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    {/* React Query Provider — makes queryClient available everywhere */}
    <QueryClientProvider client={queryClient}>
      <App />

      {/* Global toast notifications — rendered at top level */}
      <Toaster
        position="top-right"
        gutter={8}
        containerStyle={{ top: 80 }}
        toastOptions={{
          duration: 4000,
          className: 'toast-custom',
          success: {
            iconTheme: { primary: '#10b981', secondary: '#fff' },
          },
          error: {
            iconTheme: { primary: '#ef4444', secondary: '#fff' },
            duration: 6000,
          },
        }}
      />
    </QueryClientProvider>
  </React.StrictMode>
);
