import { BrowserRouter, Link, NavLink, Outlet, Route, Routes, useLocation } from 'react-router';
import { useState } from 'react';
import { useSession } from '../stores/session';
import { AdminPage, AdminSectionPage } from '../features/admin/AdminPages';
import { FeedbackPage } from '../features/feedback/FeedbackPage';
import { LobbyPage } from '../features/lobby/LobbyPage';
import { LoginPage, RegisterPage } from '../features/auth/AuthPages';
import { MePage } from '../features/me/MePage';
import { GamePage, LaunchFailPage, LaunchingPage, RoadmapPage } from '../features/platform/PlatformPages';
import styles from './app.module.css';

function Brand() {
  return (
    <Link className="ui-brand" to="/" aria-label="Lumio 平台首页">
      <span className="ui-brand__mark" aria-hidden="true">
        <span className="ui-brand__pix">{Array.from({ length: 9 }, (_, index) => <i key={index} />)}</span>
      </span>
      <span className="ui-brand__txt">
        <span className="ui-brand__name">LUMIO</span>
        <span className="ui-brand__sub">PLATFORM</span>
      </span>
    </Link>
  );
}

function Header() {
  const { user, logout } = useSession();
  const [communityOpen, setCommunityOpen] = useState(false);
  return (
    <header className={`ui-nav ${styles.nav}`}>
      <Brand />
      <nav className={`ui-nav__links ${styles.navLinks}`} aria-label="主导航">
        <NavLink to="/" end>大厅</NavLink>
        <NavLink to="/feedback">反馈</NavLink>
        <NavLink to="/roadmap">Roadmap</NavLink>
        <button className={`ui-btn ui-btn--quiet ui-btn--sm ${styles.communityButton}`} type="button" onClick={() => setCommunityOpen(true)}>开发者交流群</button>
        <a href="https://github.com/LumioGames" target="_blank" rel="noreferrer">开源引擎 ↗</a>
        {user?.role === 'admin' && <NavLink to="/admin">管理后台</NavLink>}
      </nav>
      <div className={styles.accountActions}>
        {user ? (
          <>
            <NavLink className={styles.accountLink} to="/me">{user.loginName}</NavLink>
            <button className="ui-btn ui-btn--quiet ui-btn--sm" type="button" onClick={() => void logout()}>退出</button>
          </>
        ) : (
          <>
            <Link className="ui-btn ui-btn--quiet ui-btn--sm" to="/login">登录</Link>
            <Link className="ui-btn ui-btn--primary ui-btn--sm" to="/register">注册</Link>
          </>
        )}
      </div>
      {communityOpen && <CommunityDialog onClose={() => setCommunityOpen(false)} />}
    </header>
  );
}

function CommunityDialog({ onClose }: { onClose: () => void }) {
  return <div className={styles.modalBackdrop} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}><section className={styles.modal} role="dialog" aria-modal="true" aria-labelledby="community-title"><div className={styles.modalHeading}><div><span className="ui-kicker">COMMUNITY</span><h2 id="community-title">开发者交流群</h2></div><button className="ui-btn ui-btn--quiet ui-btn--sm" type="button" aria-label="关闭" onClick={onClose}>关闭</button></div><p className="ui-muted">加入社区，和其他 Lumio 玩家交流。</p><div className={styles.qrGrid}><div aria-label="飞书群二维码占位" /><div aria-label="QQ 群二维码占位" /></div><div className="ui-actions"><a className="ui-btn ui-btn--primary" href="https://www.feishu.cn" target="_blank" rel="noreferrer">飞书群 ↗</a><a className="ui-btn ui-btn--ghost" href="https://qm.qq.com" target="_blank" rel="noreferrer">QQ 群 ↗</a></div></section></div>;
}

function Shell() {
  const { user } = useSession();
  return (
    <div className="ui-shell" data-surface={user?.role === 'admin' ? 'admin' : 'player'}>
      <Header />
      <main className={`ui-main ${styles.main}`}>
        <Outlet />
      </main>
      <footer className={styles.footer}>Lumio Platform <span aria-hidden="true">·</span> GameTech</footer>
    </div>
  );
}

export function ForbiddenPage() {
  return (
    <section className={`ui-card ${styles.messagePage}`} aria-labelledby="forbidden-title">
      <span className="ui-kicker">403 / FORBIDDEN</span>
      <h1 id="forbidden-title">没有访问权限</h1>
      <p className="ui-muted">这个页面仅对管理员开放。请联系平台管理员获取帮助。</p>
      <Link className="ui-btn ui-btn--primary" to="/">返回大厅</Link>
    </section>
  );
}

function AdminGuard() {
  const user = useSession((state) => state.user);
  return user?.role === 'admin' ? <Outlet /> : <ForbiddenPage />;
}

function MeGuard() {
  const user = useSession((state) => state.user);
  const location = useLocation();
  if (!user) {
    return <LoginPage redirectTo={`${location.pathname}${location.search}`} />;
  }
  return <MePage />;
}

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<Shell />}>
        <Route index element={<LobbyPage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route path="feedback" element={<FeedbackPage />} />
        <Route path="roadmap" element={<RoadmapPage />} />
        <Route path="launching" element={<LaunchingPage />} />
        <Route path="launching/:slug" element={<LaunchingPage />} />
        <Route path="launch-fail" element={<LaunchFailPage />} />
        <Route path="launch-fail/:slug" element={<LaunchFailPage />} />
        <Route path="games/:slug/*" element={<GamePage />} />
        <Route path="me" element={<MeGuard />} />
        <Route path="admin" element={<AdminGuard />}>
          <Route index element={<AdminPage />} />
          <Route path="users" element={<AdminSectionPage title="用户管理" description="查看用户资料、账号状态与登录记录。" />} />
          <Route path="games" element={<AdminSectionPage title="游戏目录" description="管理已发布游戏与版本信息。" />} />
          <Route path="feedback" element={<AdminSectionPage title="反馈处理" description="查看、分派并处理玩家反馈。" />} />
          <Route path="analytics" element={<AdminSectionPage title="数据看板" description="平台活动与事件数据将在此展示。" />} />
          <Route path="settings" element={<AdminSectionPage title="平台设置" description="配置平台公开信息与社区入口。" />} />
          <Route path="*" element={<AdminSectionPage title="后台页面" description="当前后台页面尚未配置。" />} />
        </Route>
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}

function NotFoundPage() {
  return (
    <section className={`ui-card ${styles.messagePage}`} aria-labelledby="not-found-title">
      <span className="ui-kicker">404 / NOT FOUND</span>
      <h1 id="not-found-title">页面不存在</h1>
      <p className="ui-muted">请检查地址，或回到大厅继续浏览。</p>
      <Link className="ui-btn ui-btn--primary" to="/">返回大厅</Link>
    </section>
  );
}

export function App() {
  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  );
}
