import { useState } from 'react';
import type { FormEvent } from 'react';
import styles from './feedback.module.css';

type FeedbackEntry = { title: string; status: string; type: string };

export function FeedbackPage() {
  const [notice, setNotice] = useState('');
  const [entries, setEntries] = useState<FeedbackEntry[]>([]);
  const [titleLength, setTitleLength] = useState(0);
  const [bodyLength, setBodyLength] = useState(0);
  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const title = String(data.get('title') ?? '').trim();
    const body = String(data.get('body') ?? '').trim();
    if (title.length === 0 || body.length < 5) { setNotice('请填写标题和至少 5 个字的详细描述'); return; }
    setEntries((current) => [{ title, status: '新提交', type: String(data.get('type') ?? '问题') }, ...current]);
    event.currentTarget.reset(); setTitleLength(0); setBodyLength(0); setNotice('收到了，谢谢');
  };
  return <section className={styles.page} aria-labelledby="feedback-title"><div className={styles.heading}><span className="ui-kicker">LUMIO / FEEDBACK</span><h1 id="feedback-title">告诉我们你的想法</h1><p className="ui-muted">报告问题或分享建议，帮助我们把平台做得更好。</p></div><div className={styles.columns}><form className={`ui-card ui-form ${styles.form}`} onSubmit={submit} noValidate><div className={styles.typeRow} role="group" aria-label="反馈类型"><label className={styles.typeOption}><input type="radio" name="type" value="问题" defaultChecked /> <span>问题报告</span></label><label className={styles.typeOption}><input type="radio" name="type" value="建议" /> <span>产品建议</span></label></div><div className="ui-field"><label htmlFor="feedback-title-input">标题 <span className="ui-hint">{titleLength}/80</span></label><input className="ui-input" id="feedback-title-input" name="title" maxLength={80} required onChange={(event) => setTitleLength(event.target.value.length)} placeholder="用一句话概括你的反馈" /></div><div className="ui-field"><label htmlFor="feedback-body">详细描述 <span className="ui-hint">{bodyLength}/4000</span></label><textarea className="ui-textarea" id="feedback-body" name="body" maxLength={4000} required onChange={(event) => setBodyLength(event.target.value.length)} placeholder="请描述发生了什么，以及你期待的结果。" /></div><div className={styles.formGrid}><div className="ui-field"><label htmlFor="feedback-game">相关游戏</label><select className="ui-select" id="feedback-game" name="game"><option value="">请选择游戏</option><option>Voxel Bomber</option><option>Starfall</option></select></div><div className="ui-field"><label htmlFor="feedback-contact">联系邮箱 <span className="ui-hint">（可选）</span></label><input className="ui-input" id="feedback-contact" name="contact" type="email" autoComplete="email" /></div></div><label className={styles.anonymous}><input type="checkbox" name="anonymous" /> 匿名提交</label><div className="ui-actions"><button className="ui-btn ui-btn--primary" type="submit">提交反馈</button>{notice && <p className="ui-hint" role="status">{notice}</p>}</div></form><aside className={styles.aside}><div className={`ui-card ${styles.community}`}><span className="ui-kicker">COMMUNITY</span><h2>开发者交流群</h2><p className="ui-muted">社区入口将在平台设置接入后显示。</p><div className={styles.qrPair}><div aria-label="飞书群二维码占位" /><div aria-label="QQ 群二维码占位" /></div><div className={styles.communityActions}><button className="ui-btn ui-btn--primary" type="button" disabled>飞书群链接待配置</button><button className="ui-btn ui-btn--ghost" type="button" disabled>QQ 群链接待配置</button></div></div><div className={`ui-card ${styles.myFeedback}`}><div className={styles.asideHeading}><h2>我的反馈</h2><span className="ui-hint">{entries.length} 条</span></div>{entries.length === 0 ? <p className="ui-muted">还没有提交记录。</p> : <ul>{entries.map((entry, index) => <li key={`${entry.title}-${index}`}><strong>{entry.title}</strong><span className="ui-pill ui-pill--new">{entry.status}</span></li>)}</ul>}</div></aside></div></section>;
}
