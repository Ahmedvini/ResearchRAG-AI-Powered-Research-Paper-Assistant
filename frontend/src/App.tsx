import { Suspense, lazy } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { currentUser, getToken } from './api';
import { AppShell } from './components/AppShell';
import { AuthPage } from './pages/AuthPage';

// Route-level code splitting: each page (and its heavy dependencies, e.g.
// recharts on the dashboard) loads only when the route is visited. AuthPage
// stays eager because it is the first paint for signed-out users.
const DashboardPage = lazy(() => import('./pages/DashboardPage').then((m) => ({ default: m.DashboardPage })));
const DocumentsPage = lazy(() => import('./pages/DocumentsPage').then((m) => ({ default: m.DocumentsPage })));
const ChatPage = lazy(() => import('./pages/ChatPage').then((m) => ({ default: m.ChatPage })));
const SearchPage = lazy(() => import('./pages/SearchPage').then((m) => ({ default: m.SearchPage })));
const AdminPage = lazy(() => import('./pages/AdminPage').then((m) => ({ default: m.AdminPage })));
const WorkspacesPage = lazy(() => import('./pages/WorkspacesPage').then((m) => ({ default: m.WorkspacesPage })));
const ResearchToolsPage = lazy(() => import('./pages/ResearchToolsPage').then((m) => ({ default: m.ResearchToolsPage })));
const VerifyEmailPage = lazy(() => import('./pages/VerifyEmailPage').then((m) => ({ default: m.VerifyEmailPage })));
const ResetPasswordPage = lazy(() => import('./pages/ResetPasswordPage').then((m) => ({ default: m.ResetPasswordPage })));

function Protected() {
  if (!getToken()) return <Navigate to="/login" replace />;
  return <AppShell />;
}

function PageFallback() {
  return <div className="p-6 text-sm text-[#60706b]">Loading...</div>;
}

export default function App() {
  const user = currentUser();
  return (
    <Suspense fallback={<PageFallback />}>
      <Routes>
        <Route path="/login" element={<AuthPage />} />
        <Route path="/verify-email" element={<VerifyEmailPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/" element={<Protected />}>
          <Route index element={<Navigate to="/workspaces" replace />} />
          <Route path="workspaces" element={<WorkspacesPage />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="documents" element={<DocumentsPage />} />
          <Route path="chat" element={<ChatPage />} />
          <Route path="research" element={<ResearchToolsPage />} />
          <Route path="search" element={<SearchPage />} />
          {user?.role === 'Admin' && <Route path="admin" element={<AdminPage />} />}
        </Route>
        <Route path="*" element={<Navigate to={getToken() ? '/workspaces' : '/login'} replace />} />
      </Routes>
    </Suspense>
  );
}
