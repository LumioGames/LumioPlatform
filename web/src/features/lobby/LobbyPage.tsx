import { useState } from 'react';
import type { CSSProperties } from 'react';
import { Link, useNavigate } from 'react-router';
import styles from './lobby.module.css';

const games = [
  { slug: 'voxel-bomber', name: '体素炸弹人', summary: '方块地图，把对手炸出场。3–8 人同局。', tone: 'var(--ui-primary)', tag: '对战', status: 'published', room: 'vb-1', online: 5, capacity: 8, updated: '2026-09-04 更新' },
  { slug: 'bedwars-td', name: '起床战争俯视改编', summary: '守住自己的床，拆掉别人的。', tone: 'var(--ui-mint)', tag: '策略', status: 'soon', room: null, online: 0, capacity: 8, updated: '即将推出' },
  { slug: 'escape-duckov', name: '逃离鸭科夫', summary: '带上战利品，从地图里撤出去。', tone: 'var(--ui-rose)', tag: '撤离', status: 'soon', room: null, online: 0, capacity: 4, updated: '即将推出' },
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
        <div className={styles.heroInner}>
          <div className={styles.heroLogo} aria-hidden="true"><span>{Array.from({ length: 9 }, (_, index) => <i key={index} />)}</span></div>
          <div className={styles.heroCopy}>
            <h1 id="lobby-title" className="ui-hero">开源体素游戏平台</h1>
            <span className={styles.heroSub}>LUMIO GAMES</span>
            <div className="ui-actions"><a className="ui-btn ui-btn--primary" href="#games"><span aria-hidden="true">▶</span> 开始游戏</a><a className="ui-btn ui-btn--quiet" href="https://github.com/LumioGames" target="_blank" rel="noreferrer"><span aria-hidden="true">{ }</span> 开源引擎 ↗</a><Link className="ui-btn ui-btn--ghost" to="/roadmap"><span aria-hidden="true">◷</span> Roadmap</Link></div>
          </div>
        </div>
        <div className={styles.heroArt} aria-hidden="true">
          <span className={`${styles.heroShard} ${styles.heroShardOne}`} />
          <span className={`${styles.heroShard} ${styles.heroShardTwo}`} />
          <span className={`${styles.heroShard} ${styles.heroShardThree}`} />
          <span className={`${styles.heroShard} ${styles.heroShardFour}`} />
        </div>
      </section>
      <section className={styles.section} id="games" aria-labelledby="games-title">
        <div className={styles.sectionHeading}><div><h2 id="games-title">大厅</h2></div><div className={styles.statusRail}><span className={styles.onlineDot} aria-hidden="true" /><strong>12 人在线</strong><span className="ui-chip"><i style={{ '--tone': 'var(--ui-mint)' } as CSSProperties} />最近更新</span></div></div>
        <div className="ui-grid">
          {games.map((game, index) => game.status === 'published' ? (
            <article className={`ui-card ui-card--game ui-motion-enter ${index === 0 ? 'ui-card--pop' : ''}`} style={{ '--motion-index': index } as CSSProperties} key={game.slug}>
              <div className="ui-cover" style={{ '--tone': game.tone } as CSSProperties}><span className={styles.coverTag}>{game.tag}</span><div className={styles.coverScene} aria-hidden="true"><span className={styles.coverBlockMain} /><span className={styles.coverBlockShadow} /><span className={styles.coverBlockSmall} /><span className={styles.coverMarker} /></div><span className={styles.onlinePill} aria-label="体素炸弹人在线状态"><span className="ui-online-dot" />{game.online}/{game.capacity} 在线</span></div>
              <div className="ui-card__body"><div className={styles.cardTitle}><h3>{game.name}</h3><span className="ui-pill ui-pill--active">已发布</span></div><div className={styles.metaRow}><span className="ui-chip">▣ {game.updated}</span><span className="ui-chip">◷ 约 4 分钟一局</span></div><div className={styles.roomPanel}><div className={styles.avatarStack} aria-hidden="true"><i /><i /><i /><i /><b>+1</b></div><span className="ui-hint">{game.room} · <strong>{game.online}/{game.capacity}</strong></span></div><div className={styles.cardActions}><button className="ui-btn ui-btn--primary" type="button" onClick={() => navigate(`/launching/${game.slug}`)}>开始游戏</button><button className="ui-btn ui-btn--ghost" type="button" onClick={() => void share()}>{copied ? '已复制' : '分享'}</button></div></div>
            </article>
          ) : (
            <article className={`ui-card ui-card--game is-soon ui-motion-enter ${index === 1 ? styles.cardMint : styles.cardRose}`} style={{ '--motion-index': index } as CSSProperties} key={game.slug}>
              <div className="ui-cover" style={{ '--tone': game.tone } as CSSProperties}><span className={styles.coverTag}>{game.tag}</span><div className={styles.coverScene} aria-hidden="true"><span className={styles.coverBlockMain} /><span className={styles.coverBlockShadow} /><span className={styles.coverBlockSmall} /><span className={styles.coverMarker} /></div><span className="ui-pill ui-pill--soon">即将上线</span></div>
              <div className="ui-card__body"><div className={styles.cardTitle}><h3>{game.name}</h3><span className="ui-pill ui-pill--soon">即将推出</span></div><p className="ui-muted">{game.summary}</p><button className="ui-btn ui-btn--quiet ui-btn--sm" type="button" disabled>敬请期待</button></div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
