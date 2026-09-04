import { useState } from 'react';
import type { FormEvent } from 'react';
import styles from './feedback.module.css';

export function FeedbackPage() {
  const [notice, setNotice] = useState('');
  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setNotice('反馈已准备提交，端点将在后续版本启用。');
  };
  return (
    <section className={styles.page} aria-labelledby="feedback-title">
      <div className={styles.heading}>
        <span className="ui-kicker">LUMIO / FEEDBACK</span>
        <h1 id="feedback-title">告诉我们你的想法</h1>
        <p className="ui-muted">报告问题或分享建议，帮助我们把平台做得更好。</p>
      </div>
      <form className={`ui-card ui-form ${styles.form}`} onSubmit={submit}>
        <div className={styles.typeRow} role="group" aria-label="反馈类型">
          <label className={styles.typeOption}><input type="radio" name="type" value="bug" defaultChecked /> <span>问题报告</span></label>
          <label className={styles.typeOption}><input type="radio" name="type" value="suggestion" /> <span>产品建议</span></label>
        </div>
        <div className="ui-field">
          <label htmlFor="feedback-title-input">标题</label>
          <input className="ui-input" id="feedback-title-input" name="title" maxLength={80} required placeholder="用一句话概括你的反馈" />
        </div>
        <div className="ui-field">
          <label htmlFor="feedback-body">详细描述</label>
          <textarea className="ui-textarea" id="feedback-body" name="body" maxLength={4000} required placeholder="请描述发生了什么，以及你期待的结果。" />
        </div>
        <div className="ui-field">
          <label htmlFor="feedback-contact">联系邮箱 <span className="ui-faint">（可选）</span></label>
          <input className="ui-input" id="feedback-contact" name="contact" type="email" autoComplete="email" />
        </div>
        <div className="ui-actions">
          <button className="ui-btn ui-btn--primary" type="submit">提交反馈</button>
          {notice && <p className="ui-hint" role="status">{notice}</p>}
        </div>
      </form>
    </section>
  );
}
