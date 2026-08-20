import { useState, useEffect, useRef } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Mail, Lock, Eye, EyeOff, CheckCircle, ArrowLeft } from 'lucide-react';
import toast from 'react-hot-toast';
import api from '../../services/api';
import { validators } from '../../utils/validation';
import './Auth.css';

// ── 6-box OTP Input ──────────────────────────────────────────────────
function OtpInput({ value, onChange, hasError }) {
  const inputs = useRef([]);
  const digits = value.split('').concat(Array(6).fill('')).slice(0, 6);

  const handleKey = (e, idx) => {
    if (e.key === 'Backspace') {
      e.preventDefault();
      const next = value.slice(0, idx) + value.slice(idx + 1);
      onChange(next);
      if (idx > 0) inputs.current[idx - 1]?.focus();
      return;
    }
    if (e.key === 'ArrowLeft' && idx > 0) { inputs.current[idx - 1]?.focus(); return; }
    if (e.key === 'ArrowRight' && idx < 5) { inputs.current[idx + 1]?.focus(); return; }
  };

  const handleChange = (e, idx) => {
    const char = e.target.value.replace(/\D/g, '').slice(-1);
    if (!char) return;
    const next = value.slice(0, idx) + char + value.slice(idx + 1);
    onChange(next.slice(0, 6));
    if (idx < 5) inputs.current[idx + 1]?.focus();
  };

  const handlePaste = (e) => {
    e.preventDefault();
    const pasted = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
    onChange(pasted);
    const focusIdx = Math.min(pasted.length, 5);
    inputs.current[focusIdx]?.focus();
  };

  return (
    <div className="otp-input-group" role="group" aria-label="One-time password">
      {digits.map((d, i) => (
        <input
          key={i}
          ref={el => inputs.current[i] = el}
          type="text"
          inputMode="numeric"
          maxLength={1}
          value={d}
          className={`otp-box ${d ? 'filled' : ''} ${hasError ? 'error' : ''}`}
          onChange={e => handleChange(e, i)}
          onKeyDown={e => handleKey(e, i)}
          onPaste={handlePaste}
          aria-label={`Digit ${i + 1}`}
          autoComplete="one-time-code"
        />
      ))}
    </div>
  );
}

export default function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [email, setEmail] = useState('');
  const [otpCode, setOtpCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPw, setShowPw] = useState(false);
  const [showConfirmPw, setShowConfirmPw] = useState(false);
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [errors, setErrors] = useState({});

  useEffect(() => {
    const emailParam = searchParams.get('email');
    if (emailParam) setEmail(decodeURIComponent(emailParam));
  }, [searchParams]);

  const clearError = (field) => setErrors(prev => { const e = {...prev}; delete e[field]; return e; });

  const validate = () => {
    const errs = {};
    const emailErr = validators.email(email);
    if (emailErr) errs.email = emailErr;
    if (otpCode.length !== 6) errs.otpCode = 'Please enter the complete 6-digit code';
    const pwErr = validators.password(newPassword);
    if (pwErr) errs.newPassword = pwErr;
    const confirmErr = validators.confirmPassword(confirmPassword, newPassword);
    if (confirmErr) errs.confirmPassword = confirmErr;
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;
    setLoading(true);
    try {
      await api.post('/gateway/auth/reset-password', {
        email: email.trim(),
        otpCode,
        newPassword,
      });
      setSuccess(true);
      toast.success('Password reset successfully!');
      setTimeout(() => navigate('/login'), 2500);
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.error || 'Failed to reset password. Please check your code.';
      toast.error(msg);
      if (msg.toLowerCase().includes('code') || msg.toLowerCase().includes('otp')) {
        setErrors(prev => ({ ...prev, otpCode: 'Invalid or expired code' }));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page page-enter">
      {/* ── Visual Panel ── */}
      <div className="auth-visual">
        <div className="auth-visual-blob auth-visual-blob-1" />
        <div className="auth-visual-blob auth-visual-blob-2" />
        <div className="auth-visual-content">
          <div className="auth-visual-icon" aria-hidden="true" />
          <h2 className="display-lg">Create a new<br />password</h2>
          <p className="body-lg">Enter the code we sent to your email and choose a strong new password.</p>
        </div>
        <div className="auth-visual-gradient" />
      </div>

      {/* ── Form Panel ── */}
      <div className="auth-form-section">
        <div className="auth-form-wrapper">
          <div className="auth-header">
            <Link to="/" className="auth-brand">
              <span className="brand-icon" />
              <span className="brand-text">FoodRush</span>
            </Link>
            <h1 className="headline-lg">Reset Password</h1>
            <p className="body-md text-muted">
              Enter the 6-digit code sent to your email and create a new password.
            </p>
          </div>

          {success ? (
            /* ── Success state ── */
            <div className="auth-success-state">
              <div className="auth-success-icon">
                <CheckCircle size={36} />
              </div>
              <h2 className="auth-success-title">Password Reset!</h2>
              <p className="auth-success-sub">
                Your password has been updated successfully.<br />
                Redirecting you to login…
              </p>
            </div>
          ) : (
            <form className="auth-form" onSubmit={handleSubmit} noValidate>

              {/* Email */}
              <div className="form-group">
                <label className="form-label" htmlFor="rp-email">Email Address</label>
                <div className="input-icon-wrapper">
                  <Mail size={18} className="input-icon" />
                  <input
                    id="rp-email"
                    type="email"
                    className={`form-input ${errors.email ? 'input-error' : ''}`}
                    placeholder="you@example.com"
                    value={email}
                    onChange={e => { setEmail(e.target.value); clearError('email'); }}
                    autoComplete="email"
                  />
                </div>
                {errors.email && <span className="field-error">{errors.email}</span>}
              </div>

              {/* OTP — 6 boxes */}
              <div className="form-group">
                <label className="form-label">Reset Code</label>
                <OtpInput
                  value={otpCode}
                  onChange={v => { setOtpCode(v); clearError('otpCode'); }}
                  hasError={!!errors.otpCode}
                />
                {errors.otpCode && <span className="field-error" style={{ textAlign: 'center', display: 'block', marginTop: '0.5rem' }}>{errors.otpCode}</span>}
              </div>

              {/* New password */}
              <div className="form-group">
                <label className="form-label" htmlFor="rp-newpw">New Password</label>
                <div className="input-icon-wrapper">
                  <Lock size={18} className="input-icon" />
                  <input
                    id="rp-newpw"
                    type={showPw ? 'text' : 'password'}
                    className={`form-input ${errors.newPassword ? 'input-error' : ''}`}
                    placeholder="Min. 8 chars, uppercase, number"
                    value={newPassword}
                    onChange={e => { setNewPassword(e.target.value); clearError('newPassword'); }}
                    autoComplete="new-password"
                  />
                  <button type="button" className="input-toggle" onClick={() => setShowPw(v => !v)} aria-label="Toggle">
                    {showPw ? <EyeOff size={18} /> : <Eye size={18} />}
                  </button>
                </div>
                {errors.newPassword && <span className="field-error">{errors.newPassword}</span>}
              </div>

              {/* Confirm password */}
              <div className="form-group">
                <label className="form-label" htmlFor="rp-confirmpw">Confirm Password</label>
                <div className="input-icon-wrapper">
                  <Lock size={18} className="input-icon" />
                  <input
                    id="rp-confirmpw"
                    type={showConfirmPw ? 'text' : 'password'}
                    className={`form-input ${errors.confirmPassword ? 'input-error' : ''}`}
                    placeholder="Re-enter new password"
                    value={confirmPassword}
                    onChange={e => { setConfirmPassword(e.target.value); clearError('confirmPassword'); }}
                    autoComplete="new-password"
                  />
                  <button type="button" className="input-toggle" onClick={() => setShowConfirmPw(v => !v)} aria-label="Toggle">
                    {showConfirmPw ? <EyeOff size={18} /> : <Eye size={18} />}
                  </button>
                </div>
                {errors.confirmPassword && <span className="field-error">{errors.confirmPassword}</span>}
              </div>

              <button type="submit" className="btn btn-primary btn-lg auth-submit" disabled={loading || otpCode.length !== 6}>
                {loading ? <div className="spinner" style={{ width: 20, height: 20, borderWidth: 2 }} /> : 'Reset Password'}
              </button>
            </form>
          )}

          <div className="auth-footer">
            <Link to="/login" className="auth-link">
              <ArrowLeft size={15} /> Back to Login
            </Link>
            <span className="auth-divider">·</span>
            <Link to="/forgot-password" className="auth-link">Resend Code</Link>
          </div>
        </div>
      </div>
    </div>
  );
}
