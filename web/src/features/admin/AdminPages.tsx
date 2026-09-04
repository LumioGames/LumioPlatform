import { useState } from 'react';
import type { ReactNode } from 'react';
import { Link, NavLink } from 'react-router';
import styles from './admin.module.css';

const sections = [
  { to: '/admin', label: '看板', end: true },
  { to: '/admin/users', label: '用户管理' },
  { to: '/admin/games', label: '游戏目录' },
  { to: '/admin/feedback', label: '反馈队列' },
  { to: '/admin/analytics', label: '数据看板' },
  { to: '/admin/settings', label: '平台设置' },
];

const users = [
  { uid: 100001, name: 'lumio_player', email: 'player@example.com', role: 'player', status: 'active', joined: '今天' },
  { uid: 100002, name: 'voxel_builder', email: 'builder@example.com', role: 'player', status: 'active', joined: '昨天' },
  { uid: 100003, name: 'quiet_account', email: 'quiet@example.com', role: 'player', status: 'banned', joined: '8 月 30 日' },
];

const games = [
  { slug: 'voxel-bomber', name: 'Voxel Bomber', status: 'published', order: 1, release: 'v0.1' },
  { slug: 'starfall', name: 'Starfall', status: 'draft', order: 2, release: '未发布' },
  { slug: 'paper-escape', name: 'Paper Escape', status: 'draft', order: 3, release: '未发布' },
];

const feedback = [
  { type: 'Bug', title: '房间加载有些慢', from: 'lumio_player', game: 'Voxel Bomber', status: 'new', date: '今天 10:24' },
  { type: '建议', title: '希望看到更多合作玩法', from: 'voxel_builder', game: 'Voxel Bomber', status: 'triaged', date: '昨天 18:05' },
];

function AdminNav() {
  return <nav className={styles.sideNav} aria-label="后台导航"><span className="ui-kicker">OPERATIONS</span>{sections.map((section) => <NavLink key={section.to} to={section.to} end={section.end}>{section.label}</NavLink>)}</nav>;
}

function Frame({ title, description, children }: { title: string; description: string; children: ReactNode }) {
  return <div className={styles.page}><div className={styles.heading}><div><span className="ui-kicker">LUMIO / ADMIN</span><h1>{title}</h1><p className="ui-muted">{description}</p></div><span className="ui-pill ui-pill--active">管理员</span></div><div className={styles.layout}><AdminNav /><section className={styles.content}>{children}</section></div></div>;
}

export function AdminPage() {
  return <Frame title="运营后台" description="平台运行概览与管理入口。"><div className={styles.sectionTitle}><h2>今日概览</h2><span className="ui-hint">当前为演示数据，接入 API 后自动更新</span></div><div className="ui-stats"><div className="ui-stat"><strong className="ui-stat__n">--</strong><span className="ui-stat__l">活跃用户</span></div><div className="ui-stat"><strong className="ui-stat__n">--</strong><span className="ui-stat__l">今日注册</span></div><div className="ui-stat"><strong className="ui-stat__n">--</strong><span className="ui-stat__l">游戏启动</span></div><div className="ui-stat"><strong className="ui-stat__n">--</strong><span className="ui-stat__l">待处理反馈</span></div></div><div className={styles.overviewGrid}><div className="ui-card"><div className={styles.cardHeading}><h3>房间实时人数</h3><span className="ui-pill ui-pill--soon">暂无数据</span></div><div className={styles.emptyMetric}>接入 /api/admin/stats 后显示</div></div><div className="ui-card"><div className={styles.cardHeading}><h3>最近活动</h3><span className="ui-hint">最近 7 天</span></div><div className={styles.activityList}><span>平台基础设施已就绪</span><span>等待真实事件接入</span></div></div></div></Frame>;
}

export function AdminSectionPage({ title, description }: { title: string; description: string }) {
  if (title === '用户管理') return <UsersPage />;
  if (title === '游戏目录') return <GamesPage />;
  if (title === '反馈处理') return <FeedbackQueuePage />;
  if (title === '平台设置') return <SettingsPage />;
  return <AnalyticsPage title={title} description={description} />;
}

function UsersPage() {
  const [rows, setRows] = useState(users);
  const toggle = (uid: number) => setRows((current) => current.map((row) => row.uid === uid ? { ...row, status: row.status === 'active' ? 'banned' : 'active' } : row));
  return <Frame title="用户管理" description="查看用户资料、账号状态与登录记录。"><div className={styles.toolbar}><input className="ui-input" placeholder="搜索 UID、用户名或邮箱" aria-label="搜索用户" /><button className="ui-btn ui-btn--quiet" type="button">筛选</button></div><div className={`ui-card ${styles.tableCard}`}><div className="ui-table-wrap"><table className="ui-table"><thead><tr><th>UID</th><th>用户</th><th>邮箱</th><th>角色</th><th>状态</th><th>加入时间</th><th>操作</th></tr></thead><tbody>{rows.map((row) => <tr key={row.uid}><td className="ui-num">{row.uid}</td><td>{row.name}</td><td>{row.email}</td><td><span className="ui-pill">{row.role}</span></td><td><span className={`ui-pill ${row.status === 'active' ? 'ui-pill--active' : 'ui-pill--banned'}`}>{row.status === 'active' ? '正常' : '已停用'}</span></td><td>{row.joined}</td><td><button className="ui-btn ui-btn--quiet ui-btn--sm" type="button" onClick={() => toggle(row.uid)}>{row.status === 'active' ? '停用' : '恢复'}</button></td></tr>)}</tbody></table></div></div></Frame>;
}

function GamesPage() {
  const [rows, setRows] = useState(games);
  return <Frame title="游戏目录" description="管理已发布游戏与版本信息。"><div className={styles.toolbar}><span className="ui-hint">只管理 slug、名称、排序与发布状态</span><button className="ui-btn ui-btn--primary" type="button" onClick={() => setRows((current) => current.map((game) => game.slug === 'starfall' ? { ...game, status: 'published', release: 'v0.1' } : game))}>发布 Starfall</button></div><div className={`ui-card ${styles.tableCard}`}><div className="ui-table-wrap"><table className="ui-table"><thead><tr><th>Slug</th><th>名称</th><th>版本</th><th>排序</th><th>状态</th><th>操作</th></tr></thead><tbody>{rows.map((game) => <tr key={game.slug}><td><code>{game.slug}</code></td><td>{game.name}</td><td>{game.release}</td><td>{game.order}</td><td><span className={`ui-pill ${game.status === 'published' ? 'ui-pill--active' : 'ui-pill--soon'}`}>{game.status === 'published' ? '已发布' : '草稿'}</span></td><td><Link className="ui-btn ui-btn--quiet ui-btn--sm" to={`/games/${game.slug}/`}>查看</Link></td></tr>)}</tbody></table></div></div></Frame>;
}

function FeedbackQueuePage() {
  const [rows, setRows] = useState(feedback);
  return <Frame title="反馈处理" description="查看、分派并处理玩家反馈。"><div className={`ui-card ${styles.tableCard}`}><div className="ui-table-wrap"><table className="ui-table"><thead><tr><th>类型</th><th>标题</th><th>提交者</th><th>游戏</th><th>时间</th><th>状态</th></tr></thead><tbody>{rows.map((item) => <tr key={item.title}><td><span className="ui-pill">{item.type}</span></td><td><strong>{item.title}</strong></td><td>{item.from}</td><td>{item.game}</td><td>{item.date}</td><td><select className="ui-select" aria-label={`${item.title} 状态`} value={item.status} onChange={(event) => setRows((current) => current.map((row) => row.title === item.title ? { ...row, status: event.target.value } : row))}><option value="new">新提交</option><option value="triaged">处理中</option><option value="closed">已关闭</option></select></td></tr>)}</tbody></table></div></div></Frame>;
}

function SettingsPage() {
  const [saved, setSaved] = useState(false);
  return <Frame title="平台设置" description="配置平台公开信息与社区入口。"><form className={`ui-card ui-form ${styles.settings}`} onSubmit={(event) => { event.preventDefault(); setSaved(true); }}><div className="ui-field"><label htmlFor="feishu">飞书群链接</label><input className="ui-input" id="feishu" type="url" placeholder="https://" /></div><div className="ui-field"><label htmlFor="qq">QQ 群链接</label><input className="ui-input" id="qq" type="url" placeholder="https://" /></div><div className="ui-field"><label htmlFor="qq-number">QQ 群号</label><input className="ui-input" id="qq-number" inputMode="numeric" /></div><div className="ui-actions"><button className="ui-btn ui-btn--primary" type="submit">保存设置</button>{saved && <span className="ui-hint" role="status">设置已保存</span>}</div></form></Frame>;
}

function AnalyticsPage({ title, description }: { title: string; description: string }) {
  return <Frame title={title} description={description}><div className="ui-card ui-empty"><h2>等待数据接入</h2><p>实时数据与统计将在对应 API 上线后显示。</p><Link className="ui-btn ui-btn--ghost" to="/admin">返回看板</Link></div></Frame>;
}
