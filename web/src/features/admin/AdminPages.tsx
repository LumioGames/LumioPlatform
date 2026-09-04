import { Link, NavLink } from 'react-router';
import styles from './admin.module.css';

const sections = [
  { to: '/admin/users', label: '用户管理' },
  { to: '/admin/games', label: '游戏目录' },
  { to: '/admin/feedback', label: '反馈处理' },
  { to: '/admin/analytics', label: '数据看板' },
  { to: '/admin/settings', label: '平台设置' },
];

function AdminNav() {
  return (
    <nav className={styles.sideNav} aria-label="后台导航">
      <span className="ui-kicker">OPERATIONS</span>
      {sections.map((section) => <NavLink key={section.to} to={section.to}>{section.label}</NavLink>)}
    </nav>
  );
}

export function AdminPage() {
  return (
    <div className={styles.page}>
      <div className={styles.heading}>
        <div>
          <span className="ui-kicker">LUMIO / ADMIN</span>
          <h1>运营后台</h1>
          <p className="ui-muted">平台运行概览与管理入口。</p>
        </div>
        <span className="ui-pill ui-pill--active">管理员</span>
      </div>
      <div className={styles.layout}>
        <AdminNav />
        <section className={styles.content} aria-labelledby="overview-title">
          <div className={styles.sectionTitle}>
            <h2 id="overview-title">今日概览</h2>
            <span className="ui-hint">数据端点将在后续版本接入</span>
          </div>
          <div className="ui-stats">
            <div className="ui-stat"><strong className="ui-stat__n">--</strong><span className="ui-stat__l">活跃用户</span></div>
            <div className="ui-stat"><strong className="ui-stat__n">--</strong><span className="ui-stat__l">今日注册</span></div>
            <div className="ui-stat"><strong className="ui-stat__n">--</strong><span className="ui-stat__l">游戏启动</span></div>
            <div className="ui-stat"><strong className="ui-stat__n">--</strong><span className="ui-stat__l">待处理反馈</span></div>
          </div>
          <div className="ui-card ui-empty">
            <h3>等待数据接入</h3>
            <p>后台数据将在管理员 API 上线后显示。</p>
            <Link className="ui-btn ui-btn--ghost" to="/admin/feedback">查看反馈入口</Link>
          </div>
        </section>
      </div>
    </div>
  );
}

export function AdminSectionPage({ title, description }: { title: string; description: string }) {
  return (
    <div className={styles.page}>
      <div className={styles.heading}>
        <div>
          <span className="ui-kicker">LUMIO / ADMIN</span>
          <h1>{title}</h1>
          <p className="ui-muted">{description}</p>
        </div>
        <span className="ui-pill ui-pill--soon">占位页面</span>
      </div>
      <div className={styles.layout}>
        <AdminNav />
        <section className={`ui-card ${styles.placeholder}`} aria-label={title}>
          <span className={styles.placeholderIcon} aria-hidden="true">+</span>
          <h2>功能准备中</h2>
          <p className="ui-muted">当前版本先提供导航与权限边界，数据操作将在对应端点完成后启用。</p>
        </section>
      </div>
    </div>
  );
}
