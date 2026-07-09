import { useState } from 'react';
import { BookOpenCheck } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { api, setAuth } from '../api';

type Mode = 'login' | 'register' | 'forgot';

const headings: Record<Mode, string> = {
  login: 'Sign in',
  register: 'Create account',
  forgot: 'Reset password'
};

export function AuthPage() {
  const navigate = useNavigate();
  const [mode, setMode] = useState<Mode>('login');
  const [email, setEmail] = useState('user@researchrag.local');
  const [password, setPassword] = useState('User123!');
  const [displayName, setDisplayName] = useState('Demo Researcher');
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');
  const [pending, setPending] = useState(false);

  function switchMode(next: Mode) {
    setError('');
    setInfo('');
    setMode(next);
    if (next === 'login') {
      setEmail('user@researchrag.local');
      setPassword('User123!');
      setDisplayName('Demo Researcher');
    } else {
      setEmail('');
      setPassword('');
      setDisplayName('');
    }
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setError('');
    setInfo('');
    setPending(true);
    try {
      if (mode === 'forgot') {
        await api.forgotPassword(email);
        setInfo('If that email is registered, a reset link is on its way. Check the backend log when no SMTP server is configured.');
        return;
      }
      const auth = mode === 'login' ? await api.login(email, password) : await api.register(email, password, displayName);
      setAuth(auth);
      navigate('/workspaces');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Authentication failed');
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="grid min-h-screen bg-[#eef1ec] lg:grid-cols-[1fr_440px]">
      <section className="flex items-center px-6 py-12 lg:px-16">
        <div className="max-w-3xl">
          <div className="mb-8 inline-flex h-12 w-12 items-center justify-center rounded-md bg-ink text-white">
            <BookOpenCheck className="h-6 w-6" />
          </div>
          <h1 className="text-4xl font-bold tracking-normal text-ink">ResearchRAG</h1>
          <p className="mt-4 max-w-2xl text-lg text-[#52635e]">
            Upload papers, build workspace memory, and ask citation-aware questions across research collections.
          </p>
          <div className="mt-8 grid max-w-2xl gap-3 sm:grid-cols-3">
            {['Hybrid retrieval', 'Paper workspaces', 'Cited answers'].map((item) => (
              <div key={item} className="rounded-md border border-line bg-white p-3 text-sm font-semibold text-ink">
                {item}
              </div>
            ))}
          </div>
        </div>
      </section>
      <section className="flex items-center border-l border-line bg-white px-6 py-10">
        <form onSubmit={submit} className="w-full space-y-4">
          <div>
            <h2 className="text-xl font-bold text-ink">{headings[mode]}</h2>
            <p className="mt-1 text-sm text-[#60706b]">
              {mode === 'forgot' ? 'Enter your account email to receive a reset link.' : 'Use the seeded account or register a new researcher.'}
            </p>
          </div>
          {mode === 'register' && (
            <label className="block">
              <span className="label">Display name</span>
              <input className="field mt-1" value={displayName} onChange={(event) => setDisplayName(event.target.value)} />
            </label>
          )}
          <label className="block">
            <span className="label">Email</span>
            <input className="field mt-1" type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
          </label>
          {mode !== 'forgot' && (
            <label className="block">
              <span className="label">Password</span>
              <input className="field mt-1" type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
            </label>
          )}
          {error && <div className="rounded-md border border-[#e4b7a9] bg-[#fff4f0] p-3 text-sm text-[#8a3b25]">{error}</div>}
          {info && <div className="rounded-md border border-[#b7d2c2] bg-[#f0f7f2] p-3 text-sm text-[#27533f]">{info}</div>}
          <button className="command-button w-full justify-center" type="submit" disabled={pending}>
            {mode === 'login' ? 'Sign in' : mode === 'register' ? 'Register' : 'Send reset link'}
          </button>
          <div className="flex flex-col gap-2">
            <button className="secondary-button w-full justify-center" type="button" onClick={() => switchMode(mode === 'login' ? 'register' : 'login')}>
              {mode === 'login' ? 'Need an account?' : 'Back to sign in'}
            </button>
            {mode === 'login' && (
              <button className="w-full text-center text-sm text-[#3d6b58] underline" type="button" onClick={() => switchMode('forgot')}>
                Forgot password?
              </button>
            )}
          </div>
        </form>
      </section>
    </main>
  );
}
