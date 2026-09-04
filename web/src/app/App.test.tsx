import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, expect, test } from 'vitest';
import { AppRoutes } from './App';
import { useSession } from '../stores/session';

const player = {
  accountId: 'acct_test',
  uid: 100001,
  loginName: 'player',
  role: 'player' as const,
  avatarId: 1,
};

const admin = { ...player, loginName: 'admin', role: 'admin' as const };

function renderAt(path: string) {
  return render(<MemoryRouter initialEntries={[path]}><AppRoutes /></MemoryRouter>);
}

afterEach(() => {
  cleanup();
  useSession.setState({ user: null, status: 'anonymous' });
});

test('renders the lobby route', () => {
  renderAt('/');
  expect(screen.getByRole('heading', { name: '发现下一场游戏' })).toBeInTheDocument();
  expect(screen.getByRole('link', { name: '反馈' })).toBeInTheDocument();
});

test('shows a 403 page when a player visits admin routes', () => {
  useSession.getState().setUser(player);
  renderAt('/admin');
  expect(screen.getByRole('heading', { name: '没有访问权限' })).toBeInTheDocument();
  expect(screen.getByText('403 / FORBIDDEN')).toBeInTheDocument();
});

test('allows an admin into the admin dashboard', () => {
  useSession.getState().setUser(admin);
  renderAt('/admin');
  expect(screen.getByRole('heading', { name: '运营后台' })).toBeInTheDocument();
  expect(screen.getByText('管理员')).toBeInTheDocument();
});

test('sends anonymous profile visits to the login shell', () => {
  renderAt('/me');
  expect(screen.getByRole('heading', { name: '欢迎回来' })).toBeInTheDocument();
  expect(screen.getByLabelText('邮箱或用户名')).toBeInTheDocument();
});

test('renders roadmap and game launch surfaces', () => {
  renderAt('/roadmap');
  expect(screen.getByRole('heading', { name: 'Roadmap' })).toBeInTheDocument();
  expect(screen.getByText('账号体系与大厅上线')).toBeInTheDocument();
  cleanup();
  renderAt('/launch-fail/voxel-bomber');
  expect(screen.getByRole('heading', { name: '现在进不去，稍后再试' })).toBeInTheDocument();
});

test('published lobby game exposes a launch action', () => {
  renderAt('/');
  expect(screen.getByRole('button', { name: '开始游戏' })).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Roadmap' })).toBeInTheDocument();
});

test('admin surfaces expose operational tables and settings', () => {
  useSession.getState().setUser(admin);
  renderAt('/admin/users');
  expect(screen.getByRole('heading', { name: '用户管理' })).toBeInTheDocument();
  expect(screen.getByText('lumio_player')).toBeInTheDocument();
  cleanup();
  renderAt('/admin/settings');
  expect(screen.getByRole('heading', { name: '平台设置' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: '保存设置' })).toBeInTheDocument();
});

test('feedback form validates content and records a new entry', () => {
  renderAt('/feedback');
  fireEvent.click(screen.getByRole('button', { name: '提交反馈' }));
  expect(screen.getByRole('status')).toHaveTextContent('请填写标题');
  fireEvent.change(screen.getByLabelText(/标题/), { target: { value: '房间反馈' } });
  fireEvent.change(screen.getByLabelText(/详细描述/), { target: { value: '加载需要更清晰的状态提示' } });
  fireEvent.click(screen.getByRole('button', { name: '提交反馈' }));
  expect(screen.getByRole('status')).toHaveTextContent('收到了，谢谢');
  expect(screen.getByText('房间反馈')).toBeInTheDocument();
});
