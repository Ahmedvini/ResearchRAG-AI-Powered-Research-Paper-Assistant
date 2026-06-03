import { BarChart3, FileText, LogOut, MessageSquareText, Search, Shield, SquareLibrary } from 'lucide-react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { clearAuth, currentUser } from '../api';

const navItems = [
  { to: '/workspaces', label: 'Workspaces', icon: SquareLibrary },
  { to: '/dashboard', label: 'Dashboard', icon: BarChart3 },
  { to: '/documents', label: 'Documents', icon: FileText },
  { to: '/chat', label: 'Chat', icon: MessageSquareText },
  { to: '/search', label: 'Search', icon: Search }
];

export function AppShell() {
  const navigate = useNavigate();
  const user = currentUser();
  const items = user?.role === 'Admin' ? [...navItems, { to: '/admin', label: 'Admin', icon: Shield }] : navItems;

  return (
    <div className="min-h-screen bg-[#eef1ec]">
      <aside className="fixed inset-y-0 left-0 hidden w-64 border-r border-line bg-[#17201f] text-white lg:block">
        <div className="flex h-16 items-center border-b border-white/10 px-5">
          <div>
            <div className="text-base font-bold">ResearchRAG</div>
            <div className="text-xs text-white/60">Academic assistant</div>
          </div>
        </div>
        <nav className="space-y-1 p-3">
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-md px-3 py-2 text-sm transition ${isActive ? 'bg-white text-ink' : 'text-white/78 hover:bg-white/10 hover:text-white'}`
              }
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="lg:pl-64">
        <header className="sticky top-0 z-20 flex min-h-16 items-center justify-between border-b border-line bg-white px-4 lg:px-6">
          <div>
            <div className="text-sm font-semibold text-ink">{user?.displayName ?? 'Researcher'}</div>
            <div className="text-xs text-[#60706b]">{user?.email}</div>
          </div>
          <button
            className="secondary-button"
            onClick={() => {
              clearAuth();
              navigate('/login');
            }}
          >
            <LogOut className="h-4 w-4" />
            Sign out
          </button>
        </header>
        <main className="mx-auto max-w-7xl px-4 py-5 lg:px-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

