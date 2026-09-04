import { useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { useSession } from '../../stores/session';
import styles from './auth.module.css';

type LoginPageProps = { redirectTo?: string };
const mockUser = { accountId: 'acct_demo_player', uid: 100001, loginName: 'lumio_player', role: 'player' as const, avatarId: 1 };

function AuthFrame({ title, kicker, children }: { title: string; kicker: string; children: ReactNode }) {
  return <section className={`ui-card ${styles.frame}`} aria-labelledby="auth-title"><div className={styles.intro}><span className="ui-kicker">{kicker}</span><h1 id="auth-title">{title}</h1></div>{children}</section>;
}

export function LoginPage({ redirectTo }: LoginPageProps) {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [error, setError] = useState('');
  const destination = redirectTo ?? params.get('next') ?? '/';
  const submit = (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const data = new FormData(event.currentTarget); const identity = String(data.get('identity') ?? '').trim(); const password = String(data.get('password') ?? ''); if (!identity || password.length < 6) { setError('邮箱或密码不对'); return; } useSession.getState().setUser({ ...mockUser, loginName: identity.includes('@') ? 'lumio_player' : identity }); navigate(destination); };
  return <AuthFrame title={params.get('next') ? '登录后继续' : '欢迎回来'} kicker="ACCOUNT / LOGIN"><form className="ui-form" onSubmit={submit} noValidate><div className="ui-field"><label htmlFor="login-identity">邮箱或用户名</label><input className="ui-input" id="login-identity" name="identity" autoComplete="username" required aria-invalid={Boolean(error)} /></div><div className="ui-field"><label htmlFor="login-password">密码</label><input className="ui-input" id="login-password" name="password" type="password" autoComplete="current-password" required aria-invalid={Boolean(error)} /></div>{error && <p className="ui-error" role="alert">{error}</p>}<button className="ui-btn ui-btn--primary" type="submit">登录并进入</button></form><p className="ui-hint">还没有账号？ <Link to={`/register${destination !== '/' ? `?next=${encodeURIComponent(destination)}` : ''}`}>创建账号</Link></p></AuthFrame>;
}

export function RegisterPage() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [step, setStep] = useState<1 | 2>(1);
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [avatarId, setAvatarId] = useState(1);
  const submitEmail = (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); if (!/^\S+@\S+\.\S+$/.test(email)) { setError('请输入有效邮箱'); return; } setError(''); setStep(2); };
  const submitAccount = (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const data = new FormData(event.currentTarget); const name = String(data.get('loginName') ?? ''); const password = String(data.get('password') ?? ''); const code = String(data.get('code') ?? ''); if (!/^[A-Za-z][A-Za-z0-9_-]{2,31}$/.test(name)) { setError('用户名格式不正确'); return; } if (password.length < 8) { setError('密码至少需要 8 位'); return; } if (!/^\d{6}$/.test(code)) { setError('验证码不正确'); return; } useSession.getState().setUser({ ...mockUser, loginName: name, avatarId }); navigate(params.get('next') ?? '/'); };
  if (step === 1) return <AuthFrame title="创建 Lumio 账号" kicker="ACCOUNT / REGISTER"><div className={styles.steps}><span className={styles.stepActive}>1 邮箱</span><span>2 资料</span></div><form className="ui-form" onSubmit={submitEmail} noValidate><div className="ui-field"><label htmlFor="register-email">邮箱</label><input className="ui-input" id="register-email" name="email" type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} required aria-invalid={Boolean(error)} /></div>{error && <p className="ui-error" role="alert">{error}</p>}<button className="ui-btn ui-btn--primary" type="submit">发送验证码</button></form><p className="ui-hint">已有账号？ <Link to="/login">返回登录</Link></p></AuthFrame>;
  return <AuthFrame title="完善资料" kicker="ACCOUNT / REGISTER"><div className={styles.steps}><button className="ui-btn ui-btn--quiet ui-btn--sm" type="button" onClick={() => setStep(1)}>返回</button><span className={styles.stepActive}>2 资料</span><span className="ui-hint">验证码已发送至 {email}</span></div><form className="ui-form" onSubmit={submitAccount} noValidate><div className="ui-field"><label htmlFor="register-name">用户名</label><input className="ui-input" id="register-name" name="loginName" autoComplete="username" required minLength={3} /></div><div className="ui-field"><label htmlFor="register-password">密码</label><input className="ui-input" id="register-password" name="password" type="password" autoComplete="new-password" required minLength={8} /></div><div className={styles.avatarOptions} aria-label="选择头像">{Array.from({ length: 12 }, (_, index) => index + 1).map((id) => <button className={styles.avatarOption} type="button" key={id} aria-label={`选择头像 ${id}`} aria-pressed={avatarId === id} onClick={() => setAvatarId(id)}><img src={`/avatars/avatar-${String(id).padStart(2, '0')}.svg`} alt="" /></button>)}</div><div className="ui-field"><label htmlFor="register-code">验证码</label><input className="ui-input" id="register-code" name="code" inputMode="numeric" maxLength={6} placeholder="6 位数字" required /></div>{error && <p className="ui-error" role="alert">{error}</p>}<button className="ui-btn ui-btn--primary" type="submit">完成注册</button></form></AuthFrame>;
}
