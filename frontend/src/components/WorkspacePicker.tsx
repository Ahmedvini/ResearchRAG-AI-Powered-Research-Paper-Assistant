import { useQuery } from '@tanstack/react-query';
import { api } from '../api';

type Props = {
  value: string;
  onChange: (value: string) => void;
};

export function WorkspacePicker({ value, onChange }: Props) {
  const { data = [] } = useQuery({ queryKey: ['workspaces'], queryFn: api.workspaces });

  return (
    <select className="field max-w-xs" value={value} onChange={(event) => onChange(event.target.value)}>
      <option value="">Select workspace</option>
      {data.map((workspace) => (
        <option key={workspace.id} value={workspace.id}>
          {workspace.name}
        </option>
      ))}
    </select>
  );
}

