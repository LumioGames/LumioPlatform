import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import type { CSSProperties } from 'react';
import styles from './platform.module.css';

const roadmap = [
  { quarter: '2026 Q3', title: '账号体系与大厅上线', detail: '统一账号、持久身份与游戏目录。', state: '已完成' },
  { quarter: '2026 Q3', title: '玩家首发体验', detail: '注册、登录、头像与反馈入口。', state: '已完成' },
  { quarter: '2026 Q4', title: '房间与观战', detail: '从大厅进入可信房间，接入准入票据。', state: '进行中' },
  { quarter: '2026 Q4', title: '起床战争俯视改编', detail: '为新的 Lumio 游戏准备可复用的房间体验。', state: '进行中' },
  { quarter: '2027 Q1', title: '逃离鸽科夫', detail: '面向更多玩家开放的首个大型版本。', state: '计划中' },
  { quarter: '2027 Q1', title: '引擎 SDK 开放', detail: '让创作者使用 Lumio GameTech 构建作品。', state: '计划中' },
];

export function RoadmapPage() {
  return (
    <section className={styles.narrowPage} aria-labelledby="roadmap-title">
      <div className={styles.pageIntro}>
        <span className="ui-kicker">LUMIO / ROADMAP</span>
        <h1 id="roadmap-title">Roadmap</h1>
        <p className="ui-muted">从账号基础到更多可玩的世界，记录平台正在前进的方向。</p>
      </div>
      <div className={styles.roadmapList}>
        {roadmap.map((item) => (
          <article className={styles.roadmapItem} key={`${item.quarter}-${item.title}`}>
            <span className={styles.quarter}>{item.quarter}</span>
            <span className={`${styles.state} ${item.state === '已完成' ? styles.complete : item.state === '进行中' ? styles.active : styles.planned}`}>{item.state}</span>
            <div>
              <h2>{item.title}</h2>
              <p className="ui-muted">{item.detail}</p>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

export function LaunchingPage() {
  const navigate = useNavigate();
  const { slug = 'voxel-bomber' } = useParams();
  const [progress, setProgress] = useState(12);
  useEffect(() => {
    const timer = window.setInterval(() => setProgress((value) => Math.min(100, value + 22)), 420);
    const redirect = window.setTimeout(() => navigate(`/games/${slug}/`), 1900);
    return () => { window.clearInterval(timer); window.clearTimeout(redirect); };
  }, [navigate, slug]);
  return (
    <section className={styles.launchPage} aria-labelledby="launch-title">
      <div className={styles.voxelSpinner} aria-hidden="true"><i /><i /><i /><i /></div>
      <span className="ui-kicker">LUMIO / LAUNCH</span>
      <h1 id="launch-title">正在进入 {slug}</h1>
      <p className="ui-muted">领取入场券，连接游戏服务器……</p>
      <div className={styles.progressTrack} role="progressbar" aria-valuenow={progress} aria-valuemin={0} aria-valuemax={100}><span style={{ width: `${progress}%` }} /></div>
      <Link className="ui-btn ui-btn--quiet" to="/">取消并返回大厅</Link>
    </section>
  );
}

export function LaunchFailPage() {
  const { slug = '游戏' } = useParams();
  return (
    <section className={`${styles.messagePage} ${styles.fail}`} aria-labelledby="launch-fail-title">
      <span className={styles.failIcon} aria-hidden="true">!</span>
      <span className="ui-kicker">LAUNCH / UNAVAILABLE</span>
      <h1 id="launch-fail-title">现在进不去，稍后再试</h1>
      <p className="ui-muted">{slug} 的房间暂时不可用。你的账号没有受到影响。</p>
      <div className="ui-actions"><Link className="ui-btn ui-btn--primary" to="/">回到大厅</Link><button className="ui-btn ui-btn--ghost" type="button" onClick={() => window.location.reload()}>再试一次</button></div>
    </section>
  );
}

export function GamePage() {
  const { slug = 'voxel-bomber' } = useParams();
  const [copied, setCopied] = useState(false);
  const copyInvite = async () => {
    try { await navigator.clipboard.writeText(window.location.href); setCopied(true); window.setTimeout(() => setCopied(false), 1800); } catch { setCopied(false); }
  };
  return (
    <section className={styles.gamePage} aria-labelledby="game-title">
      <div className={styles.gameBar}><Link className="ui-btn ui-btn--quiet ui-btn--sm" to="/">返回大厅</Link><span className="ui-chip"><i style={{ '--tone': 'var(--ui-mint)' } as CSSProperties} />{slug} · 平台托管页面</span><button className="ui-btn ui-btn--primary ui-btn--sm" type="button" onClick={() => void copyInvite()}>{copied ? '已复制' : '复制邀请链接'}</button></div>
      <div className={`ui-card ${styles.hostedNotice}`}><span className="ui-kicker">GAME HOST</span><h1 id="game-title">正在打开 {slug}</h1><p className="ui-muted">游戏正在准备房间连接，稍后将进入平台托管的游戏页面。</p><Link className="ui-btn ui-btn--primary" to="/">返回大厅</Link></div>
    </section>
  );
}
