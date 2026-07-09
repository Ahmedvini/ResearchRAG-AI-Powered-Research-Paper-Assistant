import { useMutation, useQuery } from '@tanstack/react-query';
import { BrainCircuit, FileStack, GitFork, HelpCircle, Layers3 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { api } from '../api';
import { PageHeader } from '../components/PageHeader';
import { WorkspacePicker } from '../components/WorkspacePicker';
import type { KnowledgeGraph, LiteratureReview, PaperComparison, ResearchGapReport, StudyTools } from '../types';

type ToolKey = 'review' | 'comparison' | 'gaps' | 'graph' | 'study';

const tools: Array<{ key: ToolKey; label: string; icon: typeof FileStack }> = [
  { key: 'review', label: 'Literature review', icon: FileStack },
  { key: 'comparison', label: 'Paper comparison', icon: Layers3 },
  { key: 'gaps', label: 'Research gaps', icon: BrainCircuit },
  { key: 'graph', label: 'Knowledge graph', icon: GitFork },
  { key: 'study', label: 'Flashcards & quiz', icon: HelpCircle }
];

export function ResearchToolsPage() {
  const [workspaceId, setWorkspaceId] = useState('');
  const [active, setActive] = useState<ToolKey>('review');
  const [selected, setSelected] = useState<string[]>([]);
  const { data: documents = [] } = useQuery({ queryKey: ['documents', workspaceId], queryFn: () => api.documents(workspaceId), enabled: Boolean(workspaceId) });

  const review = useMutation({ mutationFn: () => api.literatureReview(workspaceId, selected) });
  const comparison = useMutation({ mutationFn: () => api.paperComparison(workspaceId, selected.length ? selected : documents.map((x) => x.id)) });
  const gaps = useMutation({ mutationFn: () => api.researchGaps(workspaceId) });
  const graph = useMutation({ mutationFn: () => api.knowledgeGraph(workspaceId) });
  const study = useMutation({ mutationFn: () => api.studyTools(workspaceId, selected[0] ?? null, 8) });

  const busy = review.isPending || comparison.isPending || gaps.isPending || graph.isPending || study.isPending;
  const hasWorkspace = Boolean(workspaceId);
  const readyDocuments = useMemo(() => documents.filter((document) => document.status === 'Ready'), [documents]);

  function runActiveTool() {
    if (active === 'review') review.mutate();
    if (active === 'comparison') comparison.mutate();
    if (active === 'gaps') gaps.mutate();
    if (active === 'graph') graph.mutate();
    if (active === 'study') study.mutate();
  }

  return (
    <>
      <PageHeader title="Research Tools" description="Generate literature reviews, comparisons, gap reports, knowledge graphs, flashcards, and quizzes from workspace papers." action={<WorkspacePicker value={workspaceId} onChange={setWorkspaceId} />} />
      <div className="grid gap-4 lg:grid-cols-[300px_1fr]">
        <aside className="space-y-3">
          <section className="panel p-3">
            <div className="mb-2 text-sm font-bold">Tool</div>
            <div className="space-y-2">
              {tools.map((tool) => (
                <button key={tool.key} className={`flex w-full items-center gap-2 rounded-md border px-3 py-2 text-left text-sm ${active === tool.key ? 'border-moss bg-[#edf5f0] text-moss' : 'border-line bg-white text-ink'}`} onClick={() => setActive(tool.key)}>
                  <tool.icon className="h-4 w-4" />
                  {tool.label}
                </button>
              ))}
            </div>
          </section>
          <section className="panel p-3">
            <div className="mb-2 text-sm font-bold">Papers</div>
            <div className="max-h-72 space-y-2 overflow-y-auto">
              {documents.map((document) => (
                <label key={document.id} className="flex gap-2 rounded-md border border-line bg-white p-2 text-sm">
                  <input
                    type="checkbox"
                    checked={selected.includes(document.id)}
                    onChange={(event) => setSelected((current) => event.target.checked ? [...current, document.id] : current.filter((id) => id !== document.id))}
                  />
                  <span>
                    <span className="block font-semibold text-ink">{document.title || document.originalFileName}</span>
                    <span className="text-xs text-[#60706b]">{document.status}</span>
                  </span>
                </label>
              ))}
              {!documents.length && <div className="rounded-md bg-panel p-3 text-sm text-[#60706b]">Select a workspace with uploaded papers.</div>}
            </div>
          </section>
          <button className="command-button w-full justify-center" disabled={!hasWorkspace || busy} onClick={runActiveTool}>
            Run tool
          </button>
          {readyDocuments.length === 0 && hasWorkspace && <div className="rounded-md border border-[#d9c99c] bg-[#fffaf0] p-3 text-xs text-[#6f5822]">Tools can run from available metadata, but richer output needs processed ready papers.</div>}
        </aside>
        <section className="panel min-h-[620px] overflow-hidden p-4">
          {active === 'review' && <LiteratureReviewResult data={review.data} error={review.error} />}
          {active === 'comparison' && <ComparisonResult data={comparison.data} error={comparison.error} />}
          {active === 'gaps' && <GapResult data={gaps.data} error={gaps.error} />}
          {active === 'graph' && <GraphResult data={graph.data} error={graph.error} />}
          {active === 'study' && <StudyResult data={study.data} error={study.error} />}
        </section>
      </div>
    </>
  );
}

function ErrorBox({ error }: { error: unknown }) {
  if (!error) return null;
  return <div className="mb-3 rounded-md border border-[#e4b7a9] bg-[#fff4f0] p-3 text-sm text-[#8a3b25]">{error instanceof Error ? error.message : 'Tool failed'}</div>;
}

function EmptyState() {
  return <div className="rounded-md bg-panel p-4 text-sm text-[#60706b]">Run the selected tool to generate output.</div>;
}

function LiteratureReviewResult({ data, error }: { data?: LiteratureReview; error: unknown }) {
  return (
    <div>
      <ErrorBox error={error} />
      {!data ? <EmptyState /> : (
        <div className="space-y-4">
          <Section title="Research Background" text={data.background} />
          <Section title="Existing Methods" text={data.existingMethods} />
          <Section title="Current Trends" text={data.trends} />
          <Section title="Research Gaps" text={data.researchGaps} />
          <Section title="Future Work" text={data.futureWork} />
          <pre className="max-h-72 overflow-auto rounded-md bg-[#17201f] p-3 text-xs text-white">{data.markdown}</pre>
        </div>
      )}
    </div>
  );
}

function ComparisonResult({ data, error }: { data?: PaperComparison; error: unknown }) {
  return (
    <div>
      <ErrorBox error={error} />
      {!data ? <EmptyState /> : (
        <div className="overflow-auto">
          <table className="w-full min-w-[900px] table-fixed text-left text-sm">
            <thead className="bg-panel text-xs uppercase tracking-normal text-[#60706b]">
              <tr>{['Paper', 'Dataset', 'Model', 'Methodology', 'Metrics', 'Results', 'Strengths', 'Weaknesses'].map((header) => <th key={header} className="p-2">{header}</th>)}</tr>
            </thead>
            <tbody>
              {data.rows.map((row) => (
                <tr key={row.documentId} className="border-t border-line align-top">
                  <td className="p-2 font-semibold">{row.paper}</td>
                  <td className="p-2">{row.dataset}</td>
                  <td className="p-2">{row.model}</td>
                  <td className="p-2">{row.methodology}</td>
                  <td className="p-2">{row.metrics}</td>
                  <td className="p-2">{row.results}</td>
                  <td className="p-2">{row.strengths}</td>
                  <td className="p-2">{row.weaknesses}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function GapResult({ data, error }: { data?: ResearchGapReport; error: unknown }) {
  return (
    <div>
      <ErrorBox error={error} />
      {!data ? <EmptyState /> : (
        <div className="grid gap-3 md:grid-cols-2">
          <List title="Common limitations" items={data.commonLimitations} />
          <List title="Underexplored areas" items={data.underexploredAreas} />
          <List title="Missing datasets" items={data.missingDatasets} />
          <List title="Missing evaluations" items={data.missingEvaluations} />
        </div>
      )}
    </div>
  );
}

function GraphResult({ data, error }: { data?: KnowledgeGraph; error: unknown }) {
  return (
    <div>
      <ErrorBox error={error} />
      {!data ? <EmptyState /> : (
        <div className="grid gap-4 lg:grid-cols-2">
          <List title="Nodes" items={data.nodes.map((node) => `${node.label} (${node.type})`)} />
          <List title="Relationships" items={data.edges.map((edge) => `${edge.source} - ${edge.relation} -> ${edge.target}`)} />
        </div>
      )}
    </div>
  );
}

function StudyResult({ data, error }: { data?: StudyTools; error: unknown }) {
  return (
    <div>
      <ErrorBox error={error} />
      {!data ? <EmptyState /> : (
        <div className="grid gap-4 lg:grid-cols-2">
          <List title="Flashcards" items={data.flashcards.map((card) => `${card.front} ${card.back}`)} />
          <List title="Quiz" items={data.quiz.map((question) => `${question.question} Answer: ${question.answer}`)} />
        </div>
      )}
    </div>
  );
}

function Section({ title, text }: { title: string; text: string }) {
  return (
    <section>
      <h2 className="text-base font-bold text-ink">{title}</h2>
      <p className="mt-1 text-sm leading-6 text-[#52635e]">{text}</p>
    </section>
  );
}

function List({ title, items }: { title: string; items: string[] }) {
  return (
    <section className="rounded-md border border-line bg-white p-3">
      <h2 className="mb-2 text-base font-bold text-ink">{title}</h2>
      <ul className="space-y-2 text-sm text-[#52635e]">
        {items.map((item, index) => <li key={`${item}-${index}`}>{item}</li>)}
      </ul>
    </section>
  );
}

