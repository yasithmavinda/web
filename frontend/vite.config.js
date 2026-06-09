import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react({
      // Include .js files so JSX inside .js hooks is processed correctly
      include: '**/*.{jsx,js,tsx,ts}',
    }),
  ],

  // Treat .js files as JSX (fixes "JSX syntax extension not enabled" error)
  esbuild: {
    loader: 'jsx',
    include: /src\/.*\.jsx?$/,
    exclude: [],
  },

  // Path aliases — import from '@/components/...' instead of '../../components/...'
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },

  // Development server settings
  server: {
    port: 3000,
    open: true,
    proxy: {
      // Forward /api/* and /hubs/* to the ASP.NET Core backend
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true,   // ← Enable WebSocket proxying for SignalR
        secure: false,
      },
    },
  },

  // Optimise dependencies
  optimizeDeps: {
    esbuildOptions: {
      loader: { '.js': 'jsx' },
    },
  },

  // Production build settings
  build: {
    outDir: 'dist',
    sourcemap: false,
    rollupOptions: {
      output: {
        manualChunks: {
          'react-vendor':  ['react', 'react-dom', 'react-router-dom'],
          'query-vendor':  ['@tanstack/react-query'],
          'chart-vendor':  ['recharts'],
          'signalr':       ['@microsoft/signalr'],
        },
      },
    },
  },
});
