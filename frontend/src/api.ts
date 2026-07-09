import type {
  AuthResponse,
  Chat,
  ChatMessage,
  Dashboard,
  KnowledgeGraph,
  LiteratureReview,
  PaperComparison,
  ResearchDocument,
  ResearchGapReport,
  SearchResult,
  StudyTools,
  Workspace
} from './types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || `${window.location.protocol}//${window.location.hostname}:8080`;

export function getToken() {
  return localStorage.getItem('researchrag.accessToken');
}

function getRefreshToken() {
  return localStorage.getItem('researchrag.refreshToken');
}

export function setAuth(auth: AuthResponse) {
  localStorage.setItem('researchrag.accessToken', auth.accessToken);
  localStorage.setItem('researchrag.refreshToken', auth.refreshToken);
  localStorage.setItem('researchrag.user', JSON.stringify(auth.user));
}

export function clearAuth() {
  localStorage.removeItem('researchrag.accessToken');
  localStorage.removeItem('researchrag.refreshToken');
  localStorage.removeItem('researchrag.user');
}

export function currentUser() {
  const raw = localStorage.getItem('researchrag.user');
  return raw ? JSON.parse(raw) : null;
}

// Deduplicates concurrent refresh attempts: many queries can hit 401 at once
// when the access token expires, but the rotating refresh token is single-use.
let refreshInFlight: Promise<boolean> | null = null;

async function tryRefreshSession(): Promise<boolean> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) return false;
  refreshInFlight ??= (async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/Auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken })
      });
      if (!response.ok) return false;
      setAuth((await response.json()) as AuthResponse);
      return true;
    } catch {
      return false;
    }
  })();
  const refreshed = await refreshInFlight;
  refreshInFlight = null;
  return refreshed;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const send = async () => {
    const headers = new Headers(options.headers);
    const token = getToken();
    if (token) headers.set('Authorization', `Bearer ${token}`);
    if (!(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
    return fetch(`${API_BASE_URL}${path}`, { ...options, headers });
  };

  let response: Response;
  try {
    response = await send();
  } catch {
    throw new Error(`Cannot reach the ResearchRAG API at ${API_BASE_URL}. Start the backend and try again.`);
  }

  if (response.status === 401 && !path.startsWith('/api/Auth/')) {
    if (await tryRefreshSession()) {
      response = await send();
    } else {
      clearAuth();
      window.location.assign('/login');
      throw new Error('Your session expired. Sign in again.');
    }
  }

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || response.statusText);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const api = {
  login: (email: string, password: string) => request<AuthResponse>('/api/Auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  register: (email: string, password: string, displayName: string) => request<AuthResponse>('/api/Auth/register', { method: 'POST', body: JSON.stringify({ email, password, displayName }) }),
  logout: () => request<void>('/api/Auth/logout', { method: 'POST', body: JSON.stringify({ refreshToken: getRefreshToken() ?? '' }) }),
  workspaces: () => request<Workspace[]>('/api/Workspaces'),
  createWorkspace: (name: string, description: string) => request<Workspace>('/api/Workspaces', { method: 'POST', body: JSON.stringify({ name, description }) }),
  dashboard: () => request<Dashboard>('/api/Dashboard'),
  documents: (workspaceId: string) => request<ResearchDocument[]>(`/api/Documents/workspace/${workspaceId}`),
  uploadDocument: (workspaceId: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return request<ResearchDocument>(`/api/Documents/workspace/${workspaceId}/upload`, { method: 'POST', body: form });
  },
  chats: (workspaceId: string) => request<Chat[]>(`/api/Chats/workspace/${workspaceId}`),
  createChat: (workspaceId: string, title: string) => request<Chat>('/api/Chats', { method: 'POST', body: JSON.stringify({ workspaceId, title }) }),
  messages: (chatId: string) => request<ChatMessage[]>(`/api/Chats/${chatId}/messages`),
  sendMessage: (chatId: string, question: string, documentIds: string[]) => request<{ answer: string; citations: ChatMessage['citations'] }>(`/api/Chats/${chatId}/messages`, {
    method: 'POST',
    body: JSON.stringify({ question, documentIds })
  }),
  search: (query: string) => request<SearchResult[]>(`/api/Search?q=${encodeURIComponent(query)}`),
  literatureReview: (workspaceId: string, documentIds: string[]) => request<LiteratureReview>('/api/Research/literature-review', {
    method: 'POST',
    body: JSON.stringify({ workspaceId, documentIds })
  }),
  paperComparison: (workspaceId: string, documentIds: string[]) => request<PaperComparison>('/api/Research/paper-comparison', {
    method: 'POST',
    body: JSON.stringify({ workspaceId, documentIds })
  }),
  researchGaps: (workspaceId: string) => request<ResearchGapReport>('/api/Research/research-gaps', {
    method: 'POST',
    body: JSON.stringify({ workspaceId })
  }),
  knowledgeGraph: (workspaceId: string) => request<KnowledgeGraph>('/api/Research/knowledge-graph', {
    method: 'POST',
    body: JSON.stringify({ workspaceId })
  }),
  studyTools: (workspaceId: string, documentId: string | null, count: number) => request<StudyTools>('/api/Research/study-tools', {
    method: 'POST',
    body: JSON.stringify({ workspaceId, documentId, count })
  }),
  adminStats: () => request<Record<string, number>>('/api/Admin/stats'),
  processingJobs: () => request<Array<Record<string, unknown>>>('/api/Admin/processing-jobs')
};
