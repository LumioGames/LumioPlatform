import { useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { Link } from 'react-router';
import styles from './auth.module.css';

type LoginPageProps = { redirectTo?: string };

function AuthFrame({ title, kicker, children }: { title: string; kicker: string; children: ReactNode }) {
  return (
    <section className={`ui-card ${styles.frame}`} aria-labelledby="auth-title">
      <div className={styles.intro}>
        <span className="ui-kicker">{kicker}</span>
        <h1 id="auth-title">{title}</h1>
      </div>
      {children}
    </section>
  );
}

export function LoginPage({ redirectTo = '/' }: LoginPageProps) {
  const [notice, setNotice] = useState('');
  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setNotice('登录接口将在账号端口上线后启用。');
  };
  return (
    <AuthFrame title="欢迎回来" kicker="ACCOUNT / LOGIN">
      <form className="ui-form" onSubmit={submit}>
        <div className="ui-field">
          <label htmlFor="login-identity">邮箱或用户名</label>
          <input className="ui-input" id="login-identity" name="identity" autoComplete="username" required />
        </div>
        <div className="ui-field">
          <label htmlFor="login-password">密码</label>
          <input className="ui-input" id="login-password" name="password" type="password" autoComplete="current-password" required />
        </div>
        <button className="ui-btn ui-btn--primary" type="submit">登录</button>
        {notice && <p className="ui-hint" role="status">{notice}</p>}
      </form>
      <p className="ui-hint">还没有账号？ <Link to={`/register${redirectTo !== '/' ? `?next=${encodeURIComponent(redirectTo)}` : ''}`}>创建账号</Link></p>
    </AuthFrame>
  );
}

export function RegisterPage() {
  const [notice, setNotice] = useState('');
  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setNotice('注册接口将在账号端口上线后启用。');
  };
  return (
    <AuthFrame title="创建 Lumio 账号" kicker="ACCOUNT / REGISTER">
      <form className="ui-form" onSubmit={submit}>
        <div className="ui-field">
          <label htmlFor="register-name">用户名</label>
          <input className="ui-input" id="register-name" name="loginName" autoComplete="username" required minLength={3} />
          <span className="ui-hint">3-32 个字母、数字、下划线或短横线。</span>
        </div>
        <div className="ui-field">
          <label htmlFor="register-email">邮箱</label>
          <input className="ui-input" id="register-email" name="email" type="email" autoComplete="email" required />
        </div>
        <div className="ui-field">
          <label htmlFor="register-password">密码</label>
          <input className="ui-input" id="register-password" name="password" type="password" autoComplete="new-password" required minLength={8} />
        </div>
        <button className="ui-btn ui-btn--primary" type="submit">继续注册</button>
        {notice && <p className="ui-hint" role="status">{notice}</p>}
      </form>
      <p className="ui-hint">已有账号？ <Link to="/login">返回登录</Link></p>
    </AuthFrame>
  );
}
