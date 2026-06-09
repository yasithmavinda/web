import { clsx } from 'clsx';

/**
 * BUTTON — Reusable button component.
 *
 * Beginner: Instead of styling a <button> in every component,
 * we create ONE Button component with all our variants.
 * Then use it everywhere: <Button variant="primary">Save</Button>
 *
 * Props:
 *   variant  — 'primary' | 'secondary' | 'danger' | 'ghost' | 'outline'
 *   size     — 'sm' | 'md' | 'lg'
 *   loading  — Show spinner (e.g., while waiting for API)
 *   disabled — Disable interaction
 */
const VARIANTS = {
  primary:   'bg-primary-500 hover:bg-primary-600 text-white shadow-glow-sm active:scale-95',
  secondary: 'bg-white/10 hover:bg-white/15 text-white border border-white/10',
  danger:    'bg-red-500/20 hover:bg-red-500/30 text-red-400 border border-red-500/30',
  ghost:     'text-white/70 hover:text-white hover:bg-white/10',
  outline:   'border border-primary-500/50 text-primary-400 hover:bg-primary-500/10',
};

const SIZES = {
  sm: 'px-3 py-1.5 text-xs gap-1.5',
  md: 'px-4 py-2   text-sm gap-2',
  lg: 'px-6 py-3   text-base gap-2',
};

export default function Button({
  children, variant = 'primary', size = 'md',
  loading = false, disabled = false,
  className, type = 'button', onClick, ...rest
}) {
  return (
    <button
      type={type}
      disabled={disabled || loading}
      onClick={onClick}
      className={clsx(
        'inline-flex items-center justify-center font-medium rounded-xl',
        'transition-all duration-150 focus-visible:ring-2 focus-visible:ring-primary-500',
        'disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none',
        VARIANTS[variant],
        SIZES[size],
        className
      )}
      {...rest}
    >
      {/* Spinner */}
      {loading && (
        <svg className="w-4 h-4 animate-spin" viewBox="0 0 24 24" fill="none">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
      )}
      {children}
    </button>
  );
}
