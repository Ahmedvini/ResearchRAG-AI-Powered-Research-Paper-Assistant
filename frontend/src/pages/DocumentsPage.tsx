import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Upload } from 'lucide-react';
import { useState } from 'react';
import { api } from '../api';
import { PageHeader } from '../components/PageHeader';
import { StatusBadge } from '../components/StatusBadge';
import { WorkspacePicker } from '../components/WorkspacePicker';

export function DocumentsPage() {
  const queryClient = useQueryClient();
  const [workspaceId, setWorkspaceId] = useState('');
  const { data = [] } = useQuery({ queryKey: ['documents', workspaceId], queryFn: () => api.documents(workspaceId), enabled: Boolean(workspaceId), refetchInterval: 5000 });
  const upload = useMutation({
    mutationFn: (file: File) => api.uploadDocument(workspaceId, file),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['documents', workspaceId] })
  });

  return (
    <>
      <PageHeader title="Documents" description="Upload PDFs, track processing, and inspect metadata extracted by the worker." action={<WorkspacePicker value={workspaceId} onChange={setWorkspaceId} />} />
      <section className="panel mb-4 flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="font-bold">PDF ingestion</h2>
          <p className="text-sm text-[#60706b]">Queued files move through extraction, chunking, embedding, then become ready for cited chat.</p>
        </div>
        <label className={`command-button ${!workspaceId ? 'pointer-events-none opacity-50' : ''}`}>
          <Upload className="h-4 w-4" />
          Upload PDF
          <input
            className="hidden"
            type="file"
            accept="application/pdf"
            disabled={!workspaceId}
            onChange={(event) => {
              const file = event.target.files?.[0];
              if (file) upload.mutate(file);
            }}
          />
        </label>
      </section>
      <div className="panel overflow-hidden">
        <table className="w-full table-fixed border-collapse text-left text-sm">
          <thead className="bg-panel text-xs uppercase tracking-normal text-[#60706b]">
            <tr>
              <th className="p-3">Paper</th>
              <th className="w-32 p-3">Status</th>
              <th className="w-28 p-3">Year</th>
              <th className="p-3">Keywords</th>
            </tr>
          </thead>
          <tbody>
            {data.map((document) => (
              <tr key={document.id} className="border-t border-line align-top">
                <td className="p-3">
                  <div className="font-semibold text-ink">{document.title || document.originalFileName}</div>
                  <div className="mt-1 text-xs text-[#60706b]">{document.authors || document.originalFileName}</div>
                </td>
                <td className="p-3"><StatusBadge status={document.status} /></td>
                <td className="p-3 text-[#60706b]">{document.publicationYear ?? '-'}</td>
                <td className="p-3 text-[#60706b]">{document.keywords ?? '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}

