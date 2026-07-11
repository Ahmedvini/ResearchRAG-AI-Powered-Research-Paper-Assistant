import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus } from 'lucide-react';
import { useState } from 'react';
import { api } from '../api';
import { PageHeader } from '../components/PageHeader';

export function WorkspacesPage() {
  const queryClient = useQueryClient();
  const { data = [], isLoading, error: listError } = useQuery({ queryKey: ['workspaces'], queryFn: api.workspaces });
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const create = useMutation({
    mutationFn: () => api.createWorkspace(name, description),
    onSuccess: () => {
      setName('');
      setDescription('');
      queryClient.invalidateQueries({ queryKey: ['workspaces'] });
    }
  });

  return (
    <>
      <PageHeader title="Workspaces" description="Organize papers, notes, chats, and retrieval context by research area." />
      <div className="grid gap-4 lg:grid-cols-[360px_1fr]">
        <form
          className="panel space-y-3 p-4"
          onSubmit={(event) => {
            event.preventDefault();
            create.mutate();
          }}
        >
          <h2 className="text-base font-bold">New workspace</h2>
          <input className="field" placeholder="BCI Research" value={name} onChange={(event) => setName(event.target.value)} />
          <textarea className="field min-h-24" placeholder="Research focus and collection notes" value={description} onChange={(event) => setDescription(event.target.value)} />
          {create.error instanceof Error && (
            <div className="rounded-md border border-[#e4b7a9] bg-[#fff4f0] p-3 text-sm text-[#8a3b25]">{create.error.message}</div>
          )}
          <button className="command-button" disabled={!name || create.isPending}>
            <Plus className="h-4 w-4" />
            {create.isPending ? 'Creating...' : 'Create'}
          </button>
        </form>
        <div className="grid gap-3 md:grid-cols-2">
          {listError instanceof Error && (
            <div className="panel p-4 text-sm text-[#8a3b25]">Could not load workspaces: {listError.message}</div>
          )}
          {isLoading && <div className="panel p-4 text-sm text-[#60706b]">Loading workspaces...</div>}
          {data.map((workspace) => (
            <article key={workspace.id} className="panel p-4">
              <h3 className="font-bold text-ink">{workspace.name}</h3>
              <p className="mt-2 min-h-10 text-sm text-[#60706b]">{workspace.description || 'No description yet.'}</p>
              <div className="mt-4 flex gap-2 text-xs font-semibold text-[#52635e]">
                <span className="rounded bg-panel px-2 py-1">{workspace.documentCount} papers</span>
                <span className="rounded bg-panel px-2 py-1">{workspace.chatCount} chats</span>
              </div>
            </article>
          ))}
        </div>
      </div>
    </>
  );
}

