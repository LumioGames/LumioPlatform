import { useState } from 'react';
import { useSession } from '../../stores/session';
import styles from './me.module.css';

const avatars = Array.from({ length: 12 }, (_, index) => index + 1);

export function MePage() {
  const { user, setUser } = useSession();
  const [selectedAvatar, setSelectedAvatar] = useState(user?.avatarId ?? 1);
  const [copied, setCopied] = useState(false);
  if (!user) return null;
  const chooseAvatar = (avatarId: number) => { setSelectedAvatar(avatarId); setUser({ ...user, avatarId }); };
  const copyId = async () => { try { await navigator.clipboard.writeText(user.accountId); setCopied(true); window.setTimeout(() => setCopied(false), 1800); } catch { setCopied(false); } };
  return <section className={styles.page} aria-labelledby="me-title"><div className={styles.heading}><span className="ui-kicker">ACCOUNT / PROFILE</span><h1 id="me-title">我的资料</h1><p className="ui-muted">管理公开资料与头像。</p></div><div className={styles.profileGrid}><div className={`ui-card ${styles.profile}`}><div className={styles.identity}><img className={styles.avatar} src={`/avatars/avatar-${String(selectedAvatar).padStart(2, '0')}.svg`} alt="当前头像" /><div><h2>{user.loginName}</h2><p className="ui-muted">UID <span className="ui-num">{user.uid}</span> <button className="ui-btn ui-btn--quiet ui-btn--sm" type="button" onClick={() => void copyId()}>{copied ? '已复制' : '复制账号 ID'}</button></p></div><span className="ui-pill ui-pill--active">{user.role === 'admin' ? '管理员' : '玩家'}</span></div><div className="ui-avatar-grid" aria-label="头像选择">{avatars.map((avatarId) => <button className="ui-avatar" type="button" key={avatarId} aria-label={`选择头像 ${avatarId}`} aria-pressed={selectedAvatar === avatarId} onClick={() => chooseAvatar(avatarId)}><img src={`/avatars/avatar-${String(avatarId).padStart(2, '0')}.svg`} alt="" /></button>)}</div></div><div className={`ui-card ${styles.detailsCard}`}><h2>账号信息</h2><dl className={styles.details}><div><dt>账号 ID</dt><dd className="ui-num">{user.accountId}</dd></div><div><dt>用户名</dt><dd>{user.loginName}<span className="ui-hint">不可修改</span></dd></div><div><dt>邮箱</dt><dd>尚未接入 API</dd></div><div><dt>加入时间</dt><dd>2026 年 9 月</dd></div></dl><button className="ui-btn ui-btn--quiet" type="button">退出登录</button></div></div><div className={`ui-card ${styles.history}`}><div className={styles.historyHeading}><h2>我玩过的</h2><span className="ui-hint">暂无历史记录</span></div><p className="ui-muted">开始一场游戏后，这里会显示你的最近活动。</p></div></section>;
}
