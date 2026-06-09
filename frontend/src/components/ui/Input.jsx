import { forwardRef } from 'react';
import { clsx } from 'clsx';

/**
 * INPUT — Reusable form input with label and error display.
 *
 * Designed to work with react-hook-form:
 *   <Input label="Email" {...register('email')} error={errors.email?.message} />
 */
const Input = forwardRef(function Input(
  { label, error, hint, className, type = 'text', required, id, ...rest }, ref
) {
  const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-');

  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={inputId} className="text-sm font-medium text-white/80">
          {label}
          {required && <span className="text-red-400 ml-1">*</span>}
        </label>
      )}
      <input
        id={inputId}
        ref={ref}
        type={type}
        className={clsx(
          'w-full px-3.5 py-2.5 rounded-xl text-sm bg-white/5 border text-white',
          'placeholder-white/30 transition-all duration-150',
          'focus:outline-none focus:ring-2 focus:ring-primary-500/50 focus:border-primary-500/50',
          error
            ? 'border-red-500/50 bg-red-500/5 focus:ring-red-500/30'
            : 'border-white/10 hover:border-white/20',
          className
        )}
        aria-invalid={!!error}
        aria-describedby={error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined}
        {...rest}
      />
      {hint && !error && (
        <p id={`${inputId}-hint`} className="text-xs text-white/40">{hint}</p>
      )}
      {error && (
        <p id={`${inputId}-error`} role="alert" className="text-xs text-red-400 flex items-center gap-1">
          <svg className="w-3 h-3 flex-shrink-0" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
          </svg>
          {error}
        </p>
      )}
    </div>
  );
});

export default Input;

/** Textarea variant */
export const Textarea = forwardRef(function Textarea(
  { label, error, className, required, rows = 3, ...rest }, ref
) {
  const id = label?.toLowerCase().replace(/\s+/g, '-');
  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={id} className="text-sm font-medium text-white/80">
          {label}
          {required && <span className="text-red-400 ml-1">*</span>}
        </label>
      )}
      <textarea
        id={id} ref={ref} rows={rows}
        className={clsx(
          'w-full px-3.5 py-2.5 rounded-xl text-sm bg-white/5 border text-white',
          'placeholder-white/30 resize-y transition-all duration-150',
          'focus:outline-none focus:ring-2 focus:ring-primary-500/50 focus:border-primary-500/50',
          error ? 'border-red-500/50' : 'border-white/10 hover:border-white/20',
          className
        )}
        {...rest}
      />
      {error && <p className="text-xs text-red-400">{error}</p>}
    </div>
  );
});

/** Select dropdown */
export const Select = forwardRef(function Select(
  { label, error, options = [], className, required, placeholder, ...rest }, ref
) {
  const id = label?.toLowerCase().replace(/\s+/g, '-');
  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={id} className="text-sm font-medium text-white/80">
          {label}
          {required && <span className="text-red-400 ml-1">*</span>}
        </label>
      )}
      <select
        id={id} ref={ref}
        className={clsx(
          'w-full px-3.5 py-2.5 rounded-xl text-sm bg-surface-800 border text-white',
          'focus:outline-none focus:ring-2 focus:ring-primary-500/50 focus:border-primary-500/50',
          error ? 'border-red-500/50' : 'border-white/10 hover:border-white/20',
          className
        )}
        {...rest}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {options.map((o) => (
          <option key={o.value} value={o.value}>{o.label}</option>
        ))}
      </select>
      {error && <p className="text-xs text-red-400">{error}</p>}
    </div>
  );
});
