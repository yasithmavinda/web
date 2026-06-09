import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useAuth } from '@/hooks/useAuth';
import Input from '@/components/ui/Input';
import Button from '@/components/ui/Button';

export default function LoginPage() {
  const { loginMutation } = useAuth();
  const { register, handleSubmit, setValue, formState: { errors } } = useForm();

  const onSubmit = (data) => loginMutation.mutate(data);

  // One-click demo login
  const fillDemo = (email) => {
    setValue('email', email);
    setValue('password', 'Admin@123');
  };

  const demoAccounts = [
    { label: 'Admin', email: 'admin@taskflow.com', color: 'from-violet-500 to-indigo-500' },
    { label: 'Manager', email: 'pm@taskflow.com', color: 'from-sky-500 to-cyan-500' },
    { label: 'Dev', email: 'dev@taskflow.com', color: 'from-emerald-500 to-teal-500' },
  ];

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">Welcome back</h1>
        <p className="text-white/50">Sign in to your TaskFlow account</p>
      </div>

      {/* One-click demo buttons */}
      <div className="mb-6">
        <p className="text-xs text-white/30 uppercase tracking-wider font-semibold mb-3">
          Quick Demo Login
        </p>
        <div className="grid grid-cols-3 gap-2">
          {demoAccounts.map((a) => (
            <button
              key={a.email}
              type="button"
              onClick={() => {
                fillDemo(a.email);
                // Auto-submit after filling
                setTimeout(() => loginMutation.mutate({ email: a.email, password: 'Admin@123' }), 100);
              }}
              className={`
                bg-gradient-to-r ${a.color} text-white text-xs font-semibold
                py-2.5 px-3 rounded-xl transition-all duration-200
                hover:scale-105 hover:shadow-lg hover:shadow-black/30
                active:scale-95 flex items-center justify-center gap-1.5
              `}
            >
              <span className="text-sm">
                {a.label === 'Admin' ? '👑' : a.label === 'Manager' ? '📋' : '💻'}
              </span>
              {a.label}
            </button>
          ))}
        </div>
      </div>

      <div className="flex items-center gap-3 mb-6">
        <div className="flex-1 h-px bg-white/10" />
        <span className="text-xs text-white/20">or sign in manually</span>
        <div className="flex-1 h-px bg-white/10" />
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5" noValidate>
        <Input
          label="Email address"
          type="email"
          placeholder="you@example.com"
          required
          error={errors.email?.message}
          {...register('email', {
            required: 'Email is required',
            pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Enter a valid email' },
          })}
        />

        <div>
          <Input
            label="Password"
            type="password"
            placeholder="••••••••"
            required
            error={errors.password?.message}
            {...register('password', { required: 'Password is required' })}
          />
          <div className="mt-2 text-right">
            <Link to="/forgot-password"
              className="text-xs text-primary-400 hover:text-primary-300 transition-colors">
              Forgot password?
            </Link>
          </div>
        </div>

        <Button
          type="submit"
          size="lg"
          className="w-full mt-2"
          loading={loginMutation.isPending}
        >
          Sign in
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-white/40">
        Don&apos;t have an account?{' '}
        <Link to="/register" className="text-primary-400 hover:text-primary-300 font-medium transition-colors">
          Create one →
        </Link>
      </p>

      {/* Credentials hint */}
      <div className="mt-5 p-3 rounded-xl bg-white/3 border border-white/8 text-center">
        <p className="text-xs text-white/25">
          All demo accounts use password: <span className="text-white/40 font-mono">Admin@123</span>
        </p>
      </div>
    </div>
  );
}
