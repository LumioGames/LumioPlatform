import { useQuery } from '@tanstack/react-query';
import { api } from '../api/client';
import './app.css';

export function App() {
  const health = useQuery({
    queryKey: ['health'],
    queryFn: async () => {
      const response = await api.GET('/healthz');
      return { status: response.data?.status ?? 'unknown', database: response.data?.database ?? 'unknown', ok: response.response.ok };
    },
    retry: false,
  });
  return <main className="shell"><header><span className="eyebrow">LUMIO PLATFORM</span><h1>System health</h1></header><section className="health-panel" aria-label="Health status"><div className={`status-dot ${health.data?.ok ? 'ok' : 'warn'}`} /><div><strong>{health.isPending ? 'Checking services' : health.data?.status === 'ok' ? 'Operational' : 'Unavailable'}</strong><p>Database: {health.data?.database ?? (health.error ? 'unreachable' : 'checking')}</p></div><button type="button" onClick={() => void health.refetch()}>Refresh</button></section></main>;
}
