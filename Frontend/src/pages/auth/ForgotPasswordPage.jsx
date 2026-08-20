import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Mail, ArrowLeft, CheckCircle } from 'lucide-react';
import toast from 'react-hot-toast';
import api from '../../services/api';
import { validators } from '../../utils/validation';
import './Auth.css';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [otpSent, setOtpSent] = useState(false);
  const [emailError, setEmailError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    const err = validators.email(email.trim());
    if (err) { setEmailError(err); return; }
    setEmailError('');
    setLoading(true);
    try {
      await api.post('/gateway/auth/forgot-password', { email: email.trim().toLowerCase() });
      toast.success('Password reset code sent to your email');
      setOtpSent(true);
      setTimeout(() => {
        window.location.href = `/reset-password?email=${encodeURIComponent(email.trim())}`;
      }, 2500);
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to send reset code';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page page-enter">
      <div className="auth-visual">
        <div className="auth-visual-blob auth-visual-blob-1" />
        <div className="auth-visual-blob auth-visual-blob-2" />
        <div className="auth-visual-content">
          <div className="auth-visual-icon" aria-hidden="true" />
          <h2 className="display-lg">Forgot your password?<br />No worries!</h2>
          <p className="body-lg">We'll send you a reset code to get you back on track.</p>
        </div>
        <div className="auth-visual-gradient" />
      </div>

      <div className="auth-form-section">
        <div className="auth-form-wrapper">
          <div className="auth-header">
            <Link to="/" className="auth-brand">
              <span className="brand-icon" />
              <span className="brand-text">FoodRush</span>
            </Link>
            <h1 className="headline-lg">Reset Password</h1>
            <p className="body-md text-muted">
              Enter your email address and we'll send you a code to reset your password.
            </p>
          </div>

          {otpSent ? (
            <div className="auth-success-state">
              <div className="auth-success-icon">
                <CheckCircle size={36} />
              </div>
              <h2 className="auth-success-title">Code Sent!</h2>
              <p className="auth-success-sub">
                We've sent a reset code to <strong>{email}</strong>.<br />
                Redirecting you to reset your password…
              </p>
            </div>
          ) : (
            <form className="auth-form" onSubmit={handleSubmit} noValidate>
              <div className="form-group">
                <label className="form-label" htmlFor="fp-email">Email Address</label>
                <div className="input-icon-wrapper">
                  <Mail size={18} className="input-icon" />
                  <input
                    id="fp-email"
                    type="email"
                    className={`form-input ${emailError ? 'input-error' : ''}`}
                    placeholder="you@example.com"
                    value={email}
                    onChange={(e) => { setEmail(e.target.value); setEmailError(''); }}
                    autoComplete="email"
                  />
                </div>
                {emailError && <span className="field-error">{emailError}</span>}
              </div>

              <button type="submit" className="btn btn-primary btn-lg auth-submit" disabled={loading}>
                {loading
                  ? <div className="spinner" style={{ width: 20, height: 20, borderWidth: 2 }} />
                  : 'Send Reset Code'}
              </button>
            </form>
          )}

          <div className="auth-footer">
            <Link to="/login" className="auth-link">
              <ArrowLeft size={15} /> Back to Login
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
