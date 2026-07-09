import { Navigate, Route, Routes } from 'react-router-dom';
import { currentUser, getToken } from './api';
import { AppShell } from './components/AppShell';
import { AuthPage } from './pages/AuthPage';
import { DashboardPage } from './pages/DashboardPage';
import { DocumentsPage } from './pages/DocumentsPage';
import { ChatPage } from './pages/ChatPage';
import { SearchPage } from './pages/SearchPage';
import { AdminPage } from './pages/AdminPage';
import { WorkspacesPage } from './pages/WorkspacesPage';
import { ResearchToolsPage } from './pages/ResearchToolsPage';

function Protected() {
  if (!getToken()) return <Navigate to="/login" replace />;
  return <AppShell />;
}

export default function App() {
  const user = currentUser();
  return (
    <Routes>
      <Route path="/login" element={<AuthPage />} />
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
  );
}
