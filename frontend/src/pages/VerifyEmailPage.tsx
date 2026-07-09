import { useEffect, useRef, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api } from '../api';

export function VerifyEmailPage() {
  const [params] = useSearchParams();
  const token = params.get('token') ?? '';
  const [status, setStatus] = useState<'verifying' | 'done' | 'failed'>('verifying');
  const [message, setMessage] = useState('');
  const requested = useRef(false);

  useEffect(() => {
    if (!token) {
      setStatus('failed');
      setMessage('The verification link is missing its token.');
      return;
    }
    // Tokens are single-use; guard against StrictMode double-invocation.
    if (requested.current) return;
    requested.current = true;
    api
      .verifyEmail(token)
      .then(() => setStatus('done'))
      .catch((error: unknown) => {
        setStatus('failed');
        setMessage(error instanceof Error ? error.message : 'Verification failed.');
      });
  }, [token]);

  return (
    <main className="grid min-h-screen place-items-center bg-[#eef1ec] px-6">
      <div className="panel w-full max-w-md p-6 text-center">
        <h1 className="text-xl font-bold text-ink">Email verification</h1>
        {status === 'verifying' && <p className="mt-3 text-sm text-[#60706b]">Verifying your email...</p>}
        {status === 'done' && <p className="mt-3 text-sm text-[#27533f]">Your email is verified. You can sign in now.</p>}
        {status === 'failed' && <p className="mt-3 text-sm text-[#8a3b25]">{message}</p>}
        <Link className="command-button mt-5 inline-flex justify-center" to="/login">
          Go to sign in
        </Link>
      </div>
    </main>
  );
}
