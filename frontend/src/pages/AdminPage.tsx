import { useQuery } from '@tanstack/react-query';
import { api } from '../api';
import { PageHeader } from '../components/PageHeader';

export function AdminPage() {
  const { data: stats = {} } = useQuery({ queryKey: ['admin-stats'], queryFn: api.adminStats });
  const { data: jobs = [] } = useQuery({ queryKey: ['processing-jobs'], queryFn: api.processingJobs, refetchInterval: 5000 });

  return (
    <>
      <PageHeader title="Admin" description="Monitor users, storage-facing counts, query load, vector ingestion jobs, and processing failures." />
      <div className="mb-4 grid gap-3 md:grid-cols-3 lg:grid-cols-6">
        {Object.entries(stats).map(([key, value]) => (
          <div key={key} className="panel p-3">
            <div className="text-xs font-semibold uppercase tracking-normal text-[#60706b]">{key}</div>
            <div className="mt-1 text-2xl font-bold text-ink">{value}</div>
          </div>
        ))}
      </div>
      <section className="panel overflow-hidden">
        <table className="w-full table-fixed text-left text-sm">
          <thead className="bg-panel text-xs uppercase tracking-normal text-[#60706b]">
            <tr>
              <th className="p-3">Job</th>
              <th className="p-3">Document</th>
              <th className="p-3">Status</th>
              <th className="p-3">Attempts</th>
              <th className="p-3">Error</th>
            </tr>
          </thead>
          <tbody>
            {jobs.map((job) => (
              <tr key={String(job.id)} className="border-t border-line">
                <td className="truncate p-3">{String(job.id)}</td>
                <td className="truncate p-3">{String(job.documentId)}</td>
                <td className="p-3">{String(job.status)}</td>
                <td className="p-3">{String(job.attempts)}</td>
                <td className="truncate p-3 text-[#8a3b25]">{String(job.lastError ?? '')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </>
  );
}

