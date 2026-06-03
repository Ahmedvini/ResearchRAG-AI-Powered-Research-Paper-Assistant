import { useQuery } from '@tanstack/react-query';
import { Search } from 'lucide-react';
import { useState } from 'react';
import { api } from '../api';
import { PageHeader } from '../components/PageHeader';

export function SearchPage() {
  const [query, setQuery] = useState('');
  const { data = [] } = useQuery({ queryKey: ['search', query], queryFn: () => api.search(query), enabled: query.length > 1 });

  return (
    <>
      <PageHeader title="Global Search" description="Search across papers, metadata, authors, topics, and workspaces." />
      <div className="panel mb-4 flex items-center gap-2 p-3">
        <Search className="h-4 w-4 text-[#60706b]" />
        <input className="field border-0 shadow-none focus:ring-0" placeholder="Search papers, authors, topics..." value={query} onChange={(event) => setQuery(event.target.value)} />
      </div>
      <div className="space-y-2">
        {data.map((result) => (
          <article key={`${result.type}-${result.id}`} className="panel p-4">
            <div className="flex items-center justify-between gap-3">
              <h2 className="font-bold text-ink">{result.title}</h2>
              <span className="rounded bg-panel px-2 py-1 text-xs font-semibold text-[#60706b]">{result.type}</span>
            </div>
            <p className="mt-2 text-sm text-[#60706b]">{result.snippet}</p>
          </article>
        ))}
      </div>
    </>
  );
}

