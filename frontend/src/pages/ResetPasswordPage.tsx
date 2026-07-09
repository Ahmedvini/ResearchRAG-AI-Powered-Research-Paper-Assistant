import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api } from '../api';

export function ResetPasswordPage() {
  const [params] = useSearchParams();
  const token = params.get('token') ?? '';
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);
  const [pending, setPending] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setError('');
    if (password.length < 8) {
      setError('Password must be at least 8 characters.');
      return;
    }
    if (password !== confirm) {
      setError('Passwords do not match.');
      return;
    }
    setPending(true);
    try {
      await api.resetPassword(token, password);
      setDone(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Password reset failed.');
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="grid min-h-screen place-items-center bg-[#eef1ec] px-6">
      <div className="panel w-full max-w-md p-6">
        <h1 className="text-xl font-bold text-ink">Reset password</h1>
        {!token && <p className="mt-3 text-sm text-[#8a3b25]">The reset link is missing its token. Request a new one from the sign-in page.</p>}
        {done ? (
          <>
            <p className="mt-3 text-sm text-[#27533f]">Your password was updated. Sign in with the new password.</p>
            <Link className="command-button mt-5 inline-flex justify-center" to="/login">
              Go to sign in
            </Link>
          </>
        ) : (
          <form className="mt-4 space-y-3" onSubmit={submit}>
            <label className="block">
              <span className="label">New password</span>
              <input className="field mt-1" type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
            </label>
            <label className="block">
              <span className="label">Confirm password</span>
              <input className="field mt-1" type="password" value={confirm} onChange={(event) => setConfirm(event.target.value)} />
            </label>
            {error && <div className="rounded-md border border-[#e4b7a9] bg-[#fff4f0] p-3 text-sm text-[#8a3b25]">{error}</div>}
            <button className="command-button w-full justify-center" type="submit" disabled={!token || pending}>
              {pending ? 'Saving...' : 'Set new password'}
            </button>
          </form>
        )}
      </div>
    </main>
  );
}
