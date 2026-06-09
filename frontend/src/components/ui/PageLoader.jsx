/**
 * SPINNER and PAGE LOADER — Loading state indicators.
 */

import { clsx } from 'clsx';

export function Spinner({ size = 'md', className }) {
  const sizes = { sm: 'w-4 h-4', md: 'w-6 h-6', lg: 'w-8 h-8', xl: 'w-12 h-12' };
  return (
    <svg
      className={clsx('animate-spin text-primary-500', sizes[size], className)}
      fill="none" viewBox="0 0 24 24"
    >
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
    </svg>
  );
}

export default function PageLoader({ message = 'Loading...' }) {
  return (
    <div className="fixed inset-0 flex flex-col items-center justify-center gap-4 bg-surface-900 z-50">
      {/* Logo */}
      <div className="w-12 h-12 rounded-2xl bg-primary-500 flex items-center justify-center shadow-glow mb-2">
        <svg className="w-7 h-7 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5}
            d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
        </svg>
      </div>
      <Spinner size="lg" />
      <p className="text-sm text-white/40 animate-pulse">{message}</p>
    </div>
  );
}

/** Skeleton block for content placeholders */
export function SkeletonBlock({ className }) {
  return <div className={clsx('skeleton rounded-xl', className)} />;
}

/** Card skeleton */
export function CardSkeleton() {
  return (
    <div className="glass rounded-2xl p-5 space-y-3">
      <SkeletonBlock className="h-4 w-3/4" />
      <SkeletonBlock className="h-3 w-full" />
      <SkeletonBlock className="h-3 w-1/2" />
      <div className="flex gap-2 pt-2">
        <SkeletonBlock className="h-6 w-16 rounded-full" />
        <SkeletonBlock className="h-6 w-16 rounded-full" />
      </div>
    </div>
  );
}
