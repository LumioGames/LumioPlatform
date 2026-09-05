import { useState } from 'react';
import type { CSSProperties } from 'react';
import { Link, useNavigate } from 'react-router';
import styles from './lobby.module.css';

const games = [
  { slug: 'voxel-bomber', name: 'Voxel Bomber', summary: '在方块世界里和朋友一起争夺最后的阵地。', tone: 'var(--ui-primary)', tag: '对战', status: 'published', room: 'vb-1', online: 5, capacity: 8, updated: '今天更新' },
  { slug: 'starfall', name: 'Starfall', summary: '探索即将开放的新世界，关注平台的最新消息。', tone: 'var(--ui-mint)', tag: '探索', status: 'soon', room: null, online: 0, capacity: 8, updated: '准备中' },
  { slug: 'paper-escape', name: 'Paper Escape', summary: '一场轻巧的逃离冒险，正在准备首发版本。', tone: 'var(--ui-rose)', tag: '冒险', status: 'soon', room: null, online: 0, capacity: 4, updated: '准备中' },
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
          <h1 id="lobby-title" className="ui-hero">开源体素游戏平台</h1>
          <p className={styles.heroLead}>进入一场即时联机游戏，和朋友一起把想象搭成可以玩的世界。</p>
          <div className="ui-actions"><a className="ui-btn ui-btn--primary" href="#games"><span aria-hidden="true">▶</span> 开始游戏</a><a className="ui-btn ui-btn--quiet" href="https://github.com/LumioGames" target="_blank" rel="noreferrer"><span aria-hidden="true">{ }</span> 开源引擎 ↗</a><Link className="ui-btn ui-btn--ghost" to="/roadmap"><span aria-hidden="true">◷</span> Roadmap</Link></div>
        </div>
        <div className={styles.heroArt} aria-hidden="true">
          <span className={`${styles.heroShard} ${styles.heroShardOne}`} />
          <span className={`${styles.heroShard} ${styles.heroShardTwo}`} />
          <span className={`${styles.heroShard} ${styles.heroShardThree}`} />
          <span className={`${styles.heroShard} ${styles.heroShardFour}`} />
          <div className={`ui-voxel ${styles.heroCore}`}><i /><i /><i /></div>
          <span className={styles.heroOrbit} />
        </div>
      </section>
      <section className={styles.section} id="games" aria-labelledby="games-title">
        <div className={styles.sectionHeading}><div><span className="ui-kicker">CATALOG</span><h2 id="games-title">游戏目录</h2></div><div className={styles.statusRail}><span className={styles.onlineDot} aria-hidden="true" /><strong>12 人在线</strong><span className="ui-chip"><i style={{ '--tone': 'var(--ui-mint)' } as CSSProperties} />最近更新</span></div></div>
        <div className="ui-grid">
          {games.map((game, index) => game.status === 'published' ? (
            <article className={`ui-card ui-card--game ui-motion-enter ${index === 0 ? 'ui-card--pop' : ''}`} style={{ '--motion-index': index } as CSSProperties} key={game.slug}>
              <div className="ui-cover" style={{ '--tone': game.tone } as CSSProperties}><span className={styles.coverTag}>{game.tag}</span><div className={styles.coverScene} aria-hidden="true"><span className={styles.coverBlockMain} /><span className={styles.coverBlockShadow} /><span className={styles.coverBlockSmall} /><span className={styles.coverMarker} /></div><span className={styles.onlinePill} aria-label="体素炸弹人在线状态"><span className="ui-online-dot" />{game.online}/{game.capacity} 在线</span></div>
              <div className="ui-card__body"><div className={styles.cardTitle}><h3>{game.name}</h3><span className="ui-pill ui-pill--active">已发布</span></div><p className="ui-muted">{game.summary}</p><div className={styles.metaRow}><span className="ui-hint">◷ {game.updated}</span><span className="ui-hint">⌁ 8 分钟</span></div><div className={styles.roomRow}><span className="ui-hint">房间 {game.room} · {game.online}/{game.capacity} 人</span><button className="ui-btn ui-btn--primary ui-btn--sm" type="button" onClick={() => navigate(`/launching/${game.slug}`)}>开始游戏</button></div><button className={`ui-btn ui-btn--quiet ui-btn--sm ${styles.shareButton}`} type="button" onClick={() => void share()}>{copied ? '已复制' : '分享大厅'}</button></div>
            </article>
          ) : (
            <article className={`ui-card ui-card--game is-soon ui-motion-enter ${index === 1 ? styles.cardMint : styles.cardRose}`} style={{ '--motion-index': index } as CSSProperties} key={game.slug}>
              <div className="ui-cover" style={{ '--tone': game.tone } as CSSProperties}><span className={styles.coverTag}>{game.tag}</span><div className={styles.coverScene} aria-hidden="true"><span className={styles.coverBlockMain} /><span className={styles.coverBlockShadow} /><span className={styles.coverBlockSmall} /><span className={styles.coverMarker} /></div><span className="ui-pill ui-pill--soon">即将上线</span></div>
              <div className="ui-card__body"><div className={styles.cardTitle}><h3>{game.name}</h3><span className="ui-pill ui-pill--soon">准备中</span></div><p className="ui-muted">{game.summary}</p><span className="ui-hint">{game.updated}</span></div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
