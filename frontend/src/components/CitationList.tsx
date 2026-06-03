import type { Citation } from '../types';

export function CitationList({ citations }: { citations: Citation[] }) {
  if (!citations.length) return null;
  return (
    <div className="mt-3 grid gap-2 sm:grid-cols-2">
      {citations.map((citation) => (
        <div key={citation.chunkId} className="rounded-md border border-line bg-panel p-2 text-xs">
          <div className="font-semibold text-ink">{citation.documentName}</div>
          <div className="mt-1 text-[#60706b]">
            {citation.section} · page {citation.pageNumber} · score {citation.relevanceScore.toFixed(3)}
          </div>
        </div>
      ))}
    </div>
  );
}

