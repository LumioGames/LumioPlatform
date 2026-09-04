import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, expect, test } from 'vitest';
import { App } from './App';

vi.mock('../api/client', () => ({ api: { GET: vi.fn().mockResolvedValue({ response: { ok: true }, data: { status: 'ok', database: 'ok' } }) } }));

test('renders health panel', async () => {
  render(<QueryClientProvider client={new QueryClient()}><App /></QueryClientProvider>);
  expect(await screen.findByText('Operational')).toBeInTheDocument();
  expect(screen.getByText('Database: ok')).toBeInTheDocument();
});
