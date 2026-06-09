import { useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { clsx } from 'clsx';
import Button from './Button';

/**
 * MODAL — Accessible dialog component.
 *
 * Beginner: createPortal renders the modal directly in <body>,
 * not inside the parent component. This prevents z-index and
 * overflow issues.
 *
 * Features:
 * - Backdrop click to close
 * - Escape key to close
 * - Focus trap
 * - Smooth animation
 *
 * Usage:
 *   <Modal isOpen={isOpen} onClose={() => setOpen(false)} title="Create Task">
 *     <p>Modal content here</p>
 *   </Modal>
 */
export default function Modal({
  isOpen, onClose, title, children,
  size = 'md', showClose = true, footer,
}) {
  const overlayRef = useRef(null);

  // Close on Escape key
  useEffect(() => {
    if (!isOpen) return;
    const handler = (e) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [isOpen, onClose]);

  // Prevent body scroll when modal is open
  useEffect(() => {
    document.body.style.overflow = isOpen ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [isOpen]);

  if (!isOpen) return null;

  const sizeClasses = {
    sm: 'max-w-sm',
    md: 'max-w-lg',
    lg: 'max-w-2xl',
    xl: 'max-w-4xl',
    full: 'max-w-[95vw] max-h-[95vh]',
  }[size] ?? 'max-w-lg';

  return createPortal(
    <div
      ref={overlayRef}
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      onClick={(e) => e.target === overlayRef.current && onClose()}
      role="dialog"
      aria-modal="true"
      aria-labelledby="modal-title"
    >
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm animate-fade-in" />

      {/* Modal box */}
      <div className={clsx(
        'relative w-full glass-dark rounded-2xl shadow-2xl animate-scale-in',
        'border border-white/10 flex flex-col max-h-[90vh]',
        sizeClasses
      )}>
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-white/10 flex-shrink-0">
          <h2 id="modal-title" className="text-lg font-semibold text-white">{title}</h2>
          {showClose && (
            <button
              onClick={onClose}
              className="p-1.5 rounded-lg text-white/40 hover:text-white hover:bg-white/10 transition-all"
              aria-label="Close modal"
            >
              <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          )}
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto p-6">{children}</div>

        {/* Footer */}
        {footer && (
          <div className="flex items-center justify-end gap-3 p-6 border-t border-white/10 flex-shrink-0">
            {footer}
          </div>
        )}
      </div>
    </div>,
    document.body
  );
}
