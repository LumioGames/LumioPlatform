import { useState } from 'react';
import styles from './me.module.css';
import { useSession } from '../../stores/session';

const avatars = Array.from({ length: 12 }, (_, index) => index + 1);

export function MePage() {
  const { user, setUser } = useSession();
  const [selectedAvatar, setSelectedAvatar] = useState(user?.avatarId ?? 1);
  if (!user) return null;
  const chooseAvatar = (avatarId: number) => {
    setSelectedAvatar(avatarId);
    setUser({ ...user, avatarId });
  };
  return (
    <section className={styles.page} aria-labelledby="me-title">
      <div className={styles.heading}>
        <span className="ui-kicker">ACCOUNT / PROFILE</span>
        <h1 id="me-title">我的资料</h1>
        <p className="ui-muted">管理公开资料与头像。</p>
      </div>
      <div className={`ui-card ${styles.profile}`}>
        <div className={styles.identity}>
          <img className={styles.avatar} src={`/avatars/avatar-${String(selectedAvatar).padStart(2, '0')}.svg`} alt="当前头像" />
          <div>
            <h2>{user.loginName}</h2>
            <p className="ui-muted">UID <span className="ui-num">{user.uid}</span></p>
          </div>
          <span className="ui-pill ui-pill--active">{user.role === 'admin' ? '管理员' : '玩家'}</span>
        </div>
        <div className={styles.details}>
          <div><span className="ui-hint">账号 ID</span><strong className="ui-num">{user.accountId}</strong></div>
          <div><span className="ui-hint">头像</span><strong>选择一个喜欢的头像</strong></div>
        </div>
        <div className="ui-avatar-grid" aria-label="头像选择">
          {avatars.map((avatarId) => (
            <button className="ui-avatar" type="button" key={avatarId} aria-label={`选择头像 ${avatarId}`} aria-pressed={selectedAvatar === avatarId} onClick={() => chooseAvatar(avatarId)}>
              <img src={`/avatars/avatar-${String(avatarId).padStart(2, '0')}.svg`} alt="" />
            </button>
          ))}
        </div>
      </div>
    </section>
  );
}
