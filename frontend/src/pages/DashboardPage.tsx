import { useQuery } from '@tanstack/react-query';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { api } from '../api';
import { PageHeader } from '../components/PageHeader';

export function DashboardPage() {
  const { data } = useQuery({ queryKey: ['dashboard'], queryFn: api.dashboard });

  return (
    <>
      <PageHeader title="Dashboard" description="A compact operational view of papers, chats, query activity, and uploaded research." />
      <div className="grid gap-3 md:grid-cols-3">
        <Metric label="Papers" value={data?.totalPapers ?? 0} />
        <Metric label="Chats" value={data?.totalChats ?? 0} />
        <Metric label="Queries" value={data?.totalQueries ?? 0} />
      </div>
      <div className="mt-4 grid gap-4 lg:grid-cols-[1.4fr_1fr]">
        <section className="panel p-4">
          <h2 className="mb-3 text-base font-bold">Papers per year</h2>
          <div className="h-72">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data?.papersPerYear ?? []}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="year" />
                <YAxis allowDecimals={false} />
                <Tooltip />
                <Bar dataKey="count" fill="#3d6b58" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </section>
        <section className="panel p-4">
          <h2 className="mb-3 text-base font-bold">Recent uploads</h2>
          <div className="space-y-2">
            {(data?.recentlyUploadedPapers ?? []).map((paper) => (
              <div key={paper.id} className="rounded-md border border-line p-3 text-sm">
                <div className="font-semibold text-ink">{paper.name}</div>
                <div className="mt-1 text-xs text-[#60706b]">{paper.status}</div>
              </div>
            ))}
          </div>
        </section>
      </div>
    </>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="panel p-4">
      <div className="text-xs font-semibold uppercase tracking-normal text-[#60706b]">{label}</div>
      <div className="mt-2 text-3xl font-bold text-ink">{value}</div>
    </div>
  );
}

