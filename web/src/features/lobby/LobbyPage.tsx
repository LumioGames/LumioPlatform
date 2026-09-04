import { useState } from 'react';
import type { CSSProperties } from 'react';
import { Link, useNavigate } from 'react-router';
import styles from './lobby.module.css';

const games = [
  { slug: 'voxel-bomber', name: 'Voxel Bomber', summary: '在方块世界里和朋友一起争夺最后的阵地。', tone: 'var(--ui-primary)', tag: '对战', status: 'published', room: 'vb-1' },
  { slug: 'starfall', name: 'Starfall', summary: '探索即将开放的新世界，关注平台的最新消息。', tone: 'var(--ui-mint)', tag: '探索', status: 'soon', room: null },
  { slug: 'paper-escape', name: 'Paper Escape', summary: '一场轻巧的逃离冒险，正在准备首发版本。', tone: 'var(--ui-rose)', tag: '冒险', status: 'soon', room: null },
];

export function LobbyPage() {
  const navigate = useNavigate();
  const [copied, setCopied] = useState(false);
  const share = async () => {
    try { await navigator.clipboard.writeText(window.location.origin); setCopied(true); window.setTimeout(() => setCopied(false), 1800); } catch { setCopied(false); }
  };
  return (
    <div className={styles.page}>
      <section className={`ui-grid-bg ${styles.hero}`} aria-labelledby="lobby-title">
        <div className={styles.heroCopy}>
          <span className="ui-kicker">LUMIO / GAME LOBBY</span>
          <h1 id="lobby-title" className="ui-hero">发现下一场游戏</h1>
          <p className="ui-muted">浏览 Lumio 制作的游戏作品，准备好就进入房间。</p>
          <div className="ui-actions"><a className="ui-btn ui-btn--primary" href="#games">浏览游戏</a><Link className="ui-btn ui-btn--ghost" to="/roadmap">查看 Roadmap</Link></div>
        </div>
        <div className={styles.heroArt} aria-hidden="true"><div className="ui-voxel"><i /><i /><i /></div></div>
      </section>
      <section className={styles.section} id="games" aria-labelledby="games-title">
        <div className={styles.sectionHeading}><div><span className="ui-kicker">CATALOG</span><h2 id="games-title">游戏目录</h2></div><span className="ui-chip"><i style={{ '--tone': 'var(--ui-mint)' } as CSSProperties} />持续更新</span></div>
        <div className="ui-grid">
          {games.map((game) => game.status === 'published' ? (
            <article className="ui-card ui-card--game" key={game.slug}>
              <div className="ui-cover" style={{ '--tone': game.tone } as CSSProperties}><span className={styles.coverTag}>{game.tag}</span><span className={styles.coverVoxel} aria-hidden="true" /></div>
              <div className="ui-card__body"><div className={styles.cardTitle}><h3>{game.name}</h3><span className="ui-pill ui-pill--active">已发布</span></div><p className="ui-muted">{game.summary}</p><div className={styles.roomRow}><span className="ui-hint">房间 {game.room} · 人数等待连接</span><button className="ui-btn ui-btn--primary ui-btn--sm" type="button" onClick={() => navigate(`/launching/${game.slug}`)}>开始游戏</button></div><button className={`ui-btn ui-btn--quiet ui-btn--sm ${styles.shareButton}`} type="button" onClick={() => void share()}>{copied ? '已复制' : '分享大厅'}</button></div>
            </article>
          ) : (
            <article className="ui-card ui-card--game is-soon" key={game.slug}>
              <div className="ui-cover" style={{ '--tone': game.tone } as CSSProperties}><span className={styles.coverTag}>{game.tag}</span></div>
              <div className="ui-card__body"><div className={styles.cardTitle}><h3>{game.name}</h3><span className="ui-pill ui-pill--soon">即将上线</span></div><p className="ui-muted">{game.summary}</p><span className="ui-hint">敬请期待</span></div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
