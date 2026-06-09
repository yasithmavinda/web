import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useAuth } from '@/hooks/useAuth';
import Input from '@/components/ui/Input';
import Button from '@/components/ui/Button';

export default function ForgotPasswordPage() {
  const { forgotPasswordMutation } = useAuth();
  const { register, handleSubmit, formState: { errors, isSubmitSuccessful } } = useForm();

  if (isSubmitSuccessful || forgotPasswordMutation.isSuccess) {
    return (
      <div className="text-center py-8">
        <div className="w-16 h-16 rounded-2xl bg-emerald-500/20 flex items-center justify-center mx-auto mb-4">
          <svg className="w-8 h-8 text-emerald-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
          </svg>
        </div>
        <h2 className="text-xl font-bold text-white mb-2">Check your email</h2>
        <p className="text-white/50 text-sm mb-6">
          If this email is registered, we&apos;ve sent a password reset link. Check your inbox and spam folder.
        </p>
        <Link to="/login" className="text-primary-400 hover:text-primary-300 text-sm font-medium transition-colors">
          ← Back to Login
        </Link>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">Reset password</h1>
        <p className="text-white/50">Enter your email and we&apos;ll send you a reset link.</p>
      </div>
      <form onSubmit={handleSubmit((d) => forgotPasswordMutation.mutate(d.email))} className="space-y-5" noValidate>
        <Input label="Email address" type="email" placeholder="you@example.com" required
          error={errors.email?.message}
          {...register('email', {
            required: 'Email is required',
            pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Invalid email' },
          })} />
        <Button type="submit" size="lg" className="w-full" loading={forgotPasswordMutation.isPending}>
          Send Reset Link
        </Button>
      </form>
      <p className="mt-6 text-center text-sm text-white/40">
        Remember your password?{' '}
        <Link to="/login" className="text-primary-400 hover:text-primary-300 font-medium transition-colors">
          Sign in →
        </Link>
      </p>
    </div>
  );
}
