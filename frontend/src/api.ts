import type { AuthResponse, Chat, ChatMessage, Dashboard, ResearchDocument, SearchResult, Workspace } from './types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080';

export function getToken() {
  return localStorage.getItem('researchrag.accessToken');
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

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  const token = getToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  if (!(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');

  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers });
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
  adminStats: () => request<Record<string, number>>('/api/Admin/stats'),
  processingJobs: () => request<Array<Record<string, unknown>>>('/api/Admin/processing-jobs')
};

