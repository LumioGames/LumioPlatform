import { create } from 'zustand';
import { accountApi } from '../api/client';

export type AccountRole = 'player' | 'admin';

export type SessionUser = {
  accountId: string;
  uid: number;
  loginName: string;
  role: AccountRole;
  avatarId: number;
};

type SessionStatus = 'anonymous' | 'authenticated' | 'loading';

type SessionState = {
  user: SessionUser | null;
  status: SessionStatus;
  me: () => Promise<SessionUser | null>;
  setUser: (user: SessionUser | null) => void;
  logout: () => Promise<void>;
};

export const useSession = create<SessionState>((set) => ({
  user: null,
  status: 'anonymous',
  me: async () => {
    set({ status: 'loading' });
    const user = await accountApi.me();
    set({ user, status: user ? 'authenticated' : 'anonymous' });
    return user;
  },
  setUser: (user) => set({ user, status: user ? 'authenticated' : 'anonymous' }),
  logout: async () => {
    await accountApi.logout();
    set({ user: null, status: 'anonymous' });
  },
}));

export type { SessionStatus };
