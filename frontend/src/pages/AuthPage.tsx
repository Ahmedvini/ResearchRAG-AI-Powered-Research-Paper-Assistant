import { useState } from 'react';
import { BookOpenCheck } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { api, setAuth } from '../api';

export function AuthPage() {
  const navigate = useNavigate();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('user@researchrag.local');
  const [password, setPassword] = useState('User123!');
  const [displayName, setDisplayName] = useState('Demo Researcher');
  const [error, setError] = useState('');

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setError('');
    try {
      const auth = mode === 'login' ? await api.login(email, password) : await api.register(email, password, displayName);
      setAuth(auth);
      navigate('/workspaces');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Authentication failed');
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
            <h2 className="text-xl font-bold text-ink">{mode === 'login' ? 'Sign in' : 'Create account'}</h2>
            <p className="mt-1 text-sm text-[#60706b]">Use the seeded account or register a new researcher.</p>
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
          <label className="block">
            <span className="label">Password</span>
            <input className="field mt-1" type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
          </label>
          {error && <div className="rounded-md border border-[#e4b7a9] bg-[#fff4f0] p-3 text-sm text-[#8a3b25]">{error}</div>}
          <button className="command-button w-full justify-center" type="submit">
            {mode === 'login' ? 'Sign in' : 'Register'}
          </button>
          <button className="secondary-button w-full justify-center" type="button" onClick={() => setMode(mode === 'login' ? 'register' : 'login')}>
            {mode === 'login' ? 'Need an account?' : 'Already registered?'}
          </button>
        </form>
      </section>
    </main>
  );
}

