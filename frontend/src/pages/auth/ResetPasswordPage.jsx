import { useSearchParams, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useAuth } from '@/hooks/useAuth';
import Input from '@/components/ui/Input';
import Button from '@/components/ui/Button';

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const { resetPasswordMutation } = useAuth();
  const { register, handleSubmit, watch, formState: { errors } } = useForm();
  const pw = watch('newPassword');

  if (!token) {
    return (
      <div className="text-center py-8">
        <div className="text-5xl mb-4">⚠️</div>
        <h2 className="text-xl font-bold text-white mb-2">Invalid reset link</h2>
        <p className="text-white/50 text-sm mb-6">This link is invalid or has expired.</p>
        <Link to="/forgot-password" className="text-primary-400 hover:text-primary-300 text-sm font-medium">
          Request a new link →
        </Link>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">New password</h1>
        <p className="text-white/50">Choose a strong password for your account.</p>
      </div>
      <form
        onSubmit={handleSubmit((d) => resetPasswordMutation.mutate({ token, newPassword: d.newPassword, confirmPassword: d.confirm }))}
        className="space-y-5" noValidate
      >
        <Input label="New Password" type="password" required error={errors.newPassword?.message}
          hint="Min. 8 chars with uppercase, lowercase, number & special char"
          {...register('newPassword', {
            required: 'Required', minLength: { value: 8, message: 'At least 8 characters' },
            pattern: { value: /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])/, message: 'Must include uppercase, lowercase, number & special char' },
          })} />
        <Input label="Confirm Password" type="password" required error={errors.confirm?.message}
          {...register('confirm', {
            required: 'Required',
            validate: (v) => v === pw || 'Passwords do not match',
          })} />
        <Button type="submit" size="lg" className="w-full" loading={resetPasswordMutation.isPending}>
          Reset Password
        </Button>
      </form>
    </div>
  );
}
