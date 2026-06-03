import type { DocumentStatus } from '../types';

const styles: Record<DocumentStatus, string> = {
  Queued: 'bg-[#ece7d8] text-[#6f5822]',
  Extracting: 'bg-[#e8f0f7] text-[#315f7b]',
  Chunking: 'bg-[#eee8f6] text-[#67508a]',
  Embedding: 'bg-[#e5f2ed] text-[#315f50]',
  Ready: 'bg-[#dceee4] text-[#27533f]',
  Failed: 'bg-[#f4dfd8] text-[#8a3b25]'
};

export function StatusBadge({ status }: { status: DocumentStatus }) {
  return <span className={`rounded px-2 py-1 text-xs font-semibold ${styles[status]}`}>{status}</span>;
}

