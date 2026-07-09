export type User = {
  id: string;
  email: string;
  displayName: string;
  role: 'User' | 'Admin';
  emailVerified: boolean;
};

export type AuthResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
};

export type Workspace = {
  id: string;
  name: string;
  description: string;
  documentCount: number;
  chatCount: number;
  createdAt: string;
};

export type DocumentStatus = 'Queued' | 'Extracting' | 'Chunking' | 'Embedding' | 'Ready' | 'Failed';

export type ResearchDocument = {
  id: string;
  workspaceId: string;
  originalFileName: string;
  status: DocumentStatus;
  title?: string;
  authors?: string;
  publicationYear?: number;
  abstract?: string;
  keywords?: string;
  createdAt: string;
};

export type Chat = {
  id: string;
  workspaceId: string;
  title: string;
  createdAt: string;
};

export type Citation = {
  chunkId: string;
  documentName: string;
  section: string;
  pageNumber: number;
  relevanceScore: number;
};

export type ChatMessage = {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  citations: Citation[];
  createdAt: string;
};

export type Dashboard = {
  totalPapers: number;
  totalChats: number;
  totalQueries: number;
  mostStudiedTopics: Array<{ topic: string; count: number }>;
  recentlyUploadedPapers: Array<{ id: string; name: string; status: string; uploadedAt: string }>;
  papersPerYear: Array<{ year: number; count: number }>;
};

export type SearchResult = {
  type: string;
  id: string;
  title: string;
  snippet: string;
  score: number;
};

export type LiteratureReview = {
  background: string;
  existingMethods: string;
  trends: string;
  researchGaps: string;
  futureWork: string;
  markdown: string;
};

export type PaperComparisonRow = {
  documentId: string;
  paper: string;
  dataset: string;
  model: string;
  methodology: string;
  metrics: string;
  results: string;
  strengths: string;
  weaknesses: string;
};

export type PaperComparison = {
  rows: PaperComparisonRow[];
};

export type ResearchGapReport = {
  commonLimitations: string[];
  underexploredAreas: string[];
  missingDatasets: string[];
  missingEvaluations: string[];
  markdown: string;
};

export type KnowledgeGraph = {
  nodes: Array<{ id: string; label: string; type: string }>;
  edges: Array<{ source: string; target: string; relation: string }>;
};

export type StudyTools = {
  flashcards: Array<{ front: string; back: string }>;
  quiz: Array<{ type: string; question: string; options: string[]; answer: string }>;
};
