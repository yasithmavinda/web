import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useAuth } from '@/hooks/useAuth';
import Input from '@/components/ui/Input';
import { Select } from '@/components/ui/Input';
import Button from '@/components/ui/Button';

const ROLE_OPTIONS = [
  { value: 1, label: 'Admin' },
  { value: 2, label: 'Project Manager' },
  { value: 3, label: 'Collaborator' },
];

export default function RegisterPage() {
  const { registerMutation } = useAuth();
  const { register, handleSubmit, watch, formState: { errors } } = useForm({
    defaultValues: { roleId: 3 },
  });

  const password = watch('password');
  const onSubmit = (data) => registerMutation.mutate({ ...data, roleId: Number(data.roleId) });

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">Create account</h1>
        <p className="text-white/50">Start managing tasks with your team today</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
        <Input
          label="Full name"
          placeholder="John Doe"
          required
          error={errors.fullName?.message}
          {...register('fullName', {
            required: 'Full name is required',
            minLength: { value: 3, message: 'At least 3 characters' },
          })}
        />

        <Input
          label="Email address"
          type="email"
          placeholder="you@example.com"
          required
          error={errors.email?.message}
          {...register('email', {
            required: 'Email is required',
            pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Invalid email' },
          })}
        />

        <Select
          label="Role"
          options={ROLE_OPTIONS}
          required
          error={errors.roleId?.message}
          {...register('roleId', { required: 'Please select a role' })}
        />

        <Input
          label="Password"
          type="password"
          placeholder="Min. 8 characters"
          required
          hint="Must contain uppercase, lowercase, number, and special character"
          error={errors.password?.message}
          {...register('password', {
            required: 'Password is required',
            minLength: { value: 8, message: 'At least 8 characters' },
            pattern: {
              value: /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])/,
              message: 'Must include uppercase, lowercase, number, and special char',
            },
          })}
        />

        <Input
          label="Confirm password"
          type="password"
          placeholder="Repeat your password"
          required
          error={errors.confirmPassword?.message}
          {...register('confirmPassword', {
            required: 'Please confirm your password',
            validate: (v) => v === password || 'Passwords do not match',
          })}
        />

        <Button type="submit" size="lg" className="w-full mt-2" loading={registerMutation.isPending}>
          Create account
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-white/40">
        Already have an account?{' '}
        <Link to="/login" className="text-primary-400 hover:text-primary-300 font-medium transition-colors">
          Sign in →
        </Link>
      </p>
    </div>
  );
}
