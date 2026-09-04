import { Link } from 'react-router';
import type { CSSProperties } from 'react';
import styles from './lobby.module.css';

const games = [
  { name: '即将上线的游戏', summary: '新的世界正在准备中。', tone: 'var(--ui-primary)' },
  { name: '更多作品', summary: '关注平台动态，第一时间发现新游戏。', tone: 'var(--ui-mint)' },
];

export function LobbyPage() {
  return (
    <div className={styles.page}>
      <section className={`ui-grid-bg ${styles.hero}`} aria-labelledby="lobby-title">
        <div className={styles.heroCopy}>
          <span className="ui-kicker">LUMIO / GAME LOBBY</span>
          <h1 id="lobby-title" className="ui-hero">发现下一场游戏</h1>
          <p className="ui-muted">浏览 Lumio 制作的游戏作品，准备好就进入房间。</p>
          <div className="ui-actions">
            <a className="ui-btn ui-btn--primary" href="#games">浏览游戏</a>
            <Link className="ui-btn ui-btn--ghost" to="/feedback">提交反馈</Link>
          </div>
        </div>
        <div className={styles.heroArt} aria-hidden="true">
          <div className="ui-voxel"><i /><i /><i /></div>
        </div>
      </section>
      <section className={styles.section} id="games" aria-labelledby="games-title">
        <div className={styles.sectionHeading}>
          <div>
            <span className="ui-kicker">CATALOG</span>
            <h2 id="games-title">游戏目录</h2>
          </div>
          <span className="ui-chip"><i style={{ '--tone': 'var(--ui-mint)' } as CSSProperties} />持续更新</span>
        </div>
        <div className="ui-grid">
          {games.map((game) => (
            <article className="ui-card ui-card--game" key={game.name}>
              <div className="ui-cover" style={{ '--tone': game.tone } as CSSProperties} />
              <div className="ui-card__body">
                <span className="ui-pill ui-pill--soon">即将上线</span>
                <h3>{game.name}</h3>
                <p className="ui-muted">{game.summary}</p>
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
