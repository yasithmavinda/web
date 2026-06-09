/** @type {import('tailwindcss').Config} */
export default {
  // Tell Tailwind where to look for class names (so unused ones are removed in production)
  content: [
    './index.html',
    './src/**/*.{js,ts,jsx,tsx}',
  ],

  // Dark mode toggle via a 'dark' class on <html>
  darkMode: 'class',

  theme: {
    extend: {
      // ── Custom Color Palette ─────────────────────────────
      colors: {
        // Primary — deep indigo/violet
        primary: {
          50:  '#eef2ff',
          100: '#e0e7ff',
          200: '#c7d2fe',
          300: '#a5b4fc',
          400: '#818cf8',
          500: '#6366f1',   // main
          600: '#4f46e5',
          700: '#4338ca',
          800: '#3730a3',
          900: '#312e81',
          950: '#1e1b4b',
        },
        // Surface colors for the dark sidebar and cards
        surface: {
          50:  '#f8fafc',
          100: '#f1f5f9',
          700: '#1e2235',
          800: '#161929',
          900: '#0f1117',
          950: '#080a10',
        },
        // Task status colors
        status: {
          backlog:    '#64748b',
          todo:       '#6366f1',
          inprogress: '#f59e0b',
          inreview:   '#8b5cf6',
          done:       '#10b981',
          blocked:    '#ef4444',
        },
        // Task priority colors
        priority: {
          low:      '#10b981',
          medium:   '#f59e0b',
          high:     '#f97316',
          critical: '#ef4444',
        },
      },

      // ── Custom Opacity Scale (adds /2, /3, /7, /8 steps) ─
      // These allow classes like bg-white/8, border-white/3, etc.
      opacity: {
        '2':  '0.02',
        '3':  '0.03',
        '7':  '0.07',
        '8':  '0.08',
        '15': '0.15',
        '35': '0.35',
        '45': '0.45',
        '55': '0.55',
        '65': '0.65',
        '85': '0.85',
        '95': '0.95',
      },

      // ── Custom Font ───────────────────────────────────────
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
      },

      // ── Custom Border Radius ──────────────────────────────
      borderRadius: {
        xl: '0.875rem',
        '2xl': '1.125rem',
      },

      // ── Custom Animations ────────────────────────────────
      keyframes: {
        slideInRight: {
          from: { transform: 'translateX(100%)', opacity: 0 },
          to:   { transform: 'translateX(0)',    opacity: 1 },
        },
        slideInLeft: {
          from: { transform: 'translateX(-100%)', opacity: 0 },
          to:   { transform: 'translateX(0)',     opacity: 1 },
        },
        fadeIn: {
          from: { opacity: 0, transform: 'translateY(8px)' },
          to:   { opacity: 1, transform: 'translateY(0)' },
        },
        scaleIn: {
          from: { opacity: 0, transform: 'scale(0.95)' },
          to:   { opacity: 1, transform: 'scale(1)' },
        },
        shimmer: {
          '0%':   { backgroundPosition: '-200% 0' },
          '100%': { backgroundPosition: '200% 0' },
        },
        bounceOnce: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%':      { transform: 'translateY(-6px)' },
        },
      },
      animation: {
        'slide-in-right': 'slideInRight 0.25s ease-out',
        'slide-in-left':  'slideInLeft 0.25s ease-out',
        'fade-in':        'fadeIn 0.2s ease-out',
        'scale-in':       'scaleIn 0.15s ease-out',
        'shimmer':        'shimmer 1.5s infinite linear',
        'bounce-once':    'bounceOnce 0.4s ease-in-out',
      },

      // ── Custom Box Shadow ─────────────────────────────────
      boxShadow: {
        'card':     '0 1px 3px 0 rgba(0,0,0,.1), 0 1px 2px -1px rgba(0,0,0,.06)',
        'card-lg':  '0 4px 6px -1px rgba(0,0,0,.1), 0 2px 4px -2px rgba(0,0,0,.06)',
        'glow':     '0 0 20px rgba(99,102,241,.3)',
        'glow-sm':  '0 0 10px rgba(99,102,241,.2)',
      },
    },
  },

  // Safelist ensures these generated classes are never purged even if
  // Tailwind can't detect them via static analysis (e.g. dynamic class names)
  safelist: [
    { pattern: /bg-white\/(2|3|5|8|10)/ },
    { pattern: /border-white\/(2|3|5|8|10)/ },
    { pattern: /text-white\/(2|3|5|8|10|20|25|30|40|50|60|70)/ },
    { pattern: /divide-white\/(5|8|10)/ },
    { pattern: /bg-primary-500\/(5|8|10|20)/ },
    { pattern: /border-primary-500\/(20|30)/ },
  ],

  plugins: [],
};

