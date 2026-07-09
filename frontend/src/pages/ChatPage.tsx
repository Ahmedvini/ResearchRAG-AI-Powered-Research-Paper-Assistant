import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Send } from 'lucide-react';
import { useEffect, useState } from 'react';
import { api } from '../api';
import { CitationList } from '../components/CitationList';
import { PageHeader } from '../components/PageHeader';
import { WorkspacePicker } from '../components/WorkspacePicker';

export function ChatPage() {
  const queryClient = useQueryClient();
  const [workspaceId, setWorkspaceId] = useState('');
  const [chatId, setChatId] = useState('');
  const [question, setQuestion] = useState('');
  const { data: chats = [] } = useQuery({ queryKey: ['chats', workspaceId], queryFn: () => api.chats(workspaceId), enabled: Boolean(workspaceId) });
  const { data: messages = [] } = useQuery({ queryKey: ['messages', chatId], queryFn: () => api.messages(chatId), enabled: Boolean(chatId) });
  const createChat = useMutation({
    mutationFn: () => api.createChat(workspaceId, 'Research chat'),
    onSuccess: (chat) => {
      setChatId(chat.id);
      queryClient.invalidateQueries({ queryKey: ['chats', workspaceId] });
    }
  });
  const send = useMutation({
    mutationFn: () => api.sendMessage(chatId, question, []),
    onSuccess: () => {
      setQuestion('');
      queryClient.invalidateQueries({ queryKey: ['messages', chatId] });
    }
  });

  useEffect(() => {
    if (!chatId && chats[0]) setChatId(chats[0].id);
  }, [chatId, chats]);

  return (
    <>
      <PageHeader
        title="Citation Chat"
        description="Ask questions over ready papers in a workspace and inspect the source chunks used for each answer."
        action={
          <WorkspacePicker
            value={workspaceId}
            onChange={(id) => {
              // Deselect the previous workspace's chat so messages are not
              // read from or sent to a chat in another workspace.
              setWorkspaceId(id);
              setChatId('');
            }}
          />
        }
      />
      <div className="grid min-h-[680px] gap-4 lg:grid-cols-[280px_1fr]">
        <aside className="panel p-3">
          <button className="command-button mb-3 w-full justify-center" disabled={!workspaceId || createChat.isPending} onClick={() => createChat.mutate()}>
            New chat
          </button>
          <div className="space-y-2">
            {chats.map((chat) => (
              <button key={chat.id} className={`w-full rounded-md border px-3 py-2 text-left text-sm ${chat.id === chatId ? 'border-moss bg-[#edf5f0]' : 'border-line bg-white'}`} onClick={() => setChatId(chat.id)}>
                {chat.title}
              </button>
            ))}
          </div>
        </aside>
        <section className="panel flex flex-col overflow-hidden">
          <div className="flex-1 space-y-3 overflow-y-auto p-4">
            {messages.map((message) => (
              <article key={message.id} className={`max-w-4xl rounded-md border p-3 ${message.role === 'user' ? 'ml-auto border-[#d9c99c] bg-[#fffaf0]' : 'border-line bg-white'}`}>
                <div className="text-xs font-semibold uppercase tracking-normal text-[#60706b]">{message.role}</div>
                <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-ink">{message.content}</p>
                <CitationList citations={message.citations} />
              </article>
            ))}
          </div>
          <form
            className="flex gap-2 border-t border-line p-3"
            onSubmit={(event) => {
              event.preventDefault();
              if (question && chatId) send.mutate();
            }}
          >
            <input className="field" placeholder="What dataset was used?" value={question} onChange={(event) => setQuestion(event.target.value)} />
            <button className="command-button" disabled={!question || !chatId || send.isPending}>
              <Send className="h-4 w-4" />
              Ask
            </button>
          </form>
        </section>
      </div>
    </>
  );
}

