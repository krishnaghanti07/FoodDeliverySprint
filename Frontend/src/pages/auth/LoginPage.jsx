import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { Eye, EyeOff, Mail, Lock } from 'lucide-react';
import toast from 'react-hot-toast';
import { validators } from '../../utils/validation';
import './Auth.css';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPw, setShowPw] = useState(false);
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});

  const validate = () => {
    const errs = {};
    const emailErr = validators.email(email);
    if (emailErr) errs.email = emailErr;
    if (!password) {
      errs.password = 'Password is required';
    } else if (password.length < 8) {
      errs.password = 'Password must be at least 8 characters';
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    try {
      const data = await login(email.trim(), password);
      toast.success('Welcome back!');
      const role = (data.user?.role || data.role || '').toLowerCase();
      if (role === 'admin') navigate('/admin');
      else if (role === 'partner') navigate('/partner');
      else if (role === 'deliveryagent') navigate('/agent/dashboard');
      else navigate('/');
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.title || 'Login failed';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  const clearError = (field) => setErrors(prev => { const e = {...prev}; delete e[field]; return e; });

  return (
    <div className="auth-page page-enter">
      <div className="auth-visual">
        <div className="auth-visual-blob auth-visual-blob-1" />
        <div className="auth-visual-blob auth-visual-blob-2" />
        <div className="auth-visual-content">
          <div className="auth-visual-icon" aria-hidden="true" />
          <h2 className="display-lg">Surgical precision<br />in every delivery.</h2>
          <p className="body-lg">Experience the future of food logistics with FoodRush.</p>
        </div>
        <div className="auth-visual-badges">
          <span className="auth-visual-badge">⭐ 4.8 avg rating</span>
          <span className="auth-visual-badge">🚀 30 min delivery</span>
        </div>
        <div className="auth-visual-gradient" />
      </div>

      <div className="auth-form-section">
        <div className="auth-form-wrapper">
          <div className="auth-header">
            <Link to="/" className="auth-brand">
              <span>🍕</span>
              <span className="brand-text">FoodRush</span>
            </Link>
            <h1 className="headline-lg">Login to FoodRush</h1>
            <p className="body-md text-muted">Welcome back! Please enter your details.</p>
          </div>

          <form className="auth-form" onSubmit={handleSubmit} id="login-form" noValidate>
            <div className="form-group">
              <label className="form-label" htmlFor="login-email">Email Address</label>
              <div className="input-icon-wrapper">
                <Mail size={18} className="input-icon" />
                <input
                  id="login-email"
                  type="email"
                  className={`form-input ${errors.email ? 'input-error' : ''}`}
                  placeholder="you@example.com"
                  value={email}
                  onChange={(e) => { setEmail(e.target.value); clearError('email'); }}
                  autoComplete="email"
                />
              </div>
              {errors.email && <span className="field-error">{errors.email}</span>}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="login-password">Password</label>
              <div className="input-icon-wrapper">
                <Lock size={18} className="input-icon" />
                <input
                  id="login-password"
                  type={showPw ? 'text' : 'password'}
                  className={`form-input ${errors.password ? 'input-error' : ''}`}
                  placeholder="Enter your password"
                  value={password}
                  onChange={(e) => { setPassword(e.target.value); clearError('password'); }}
                  autoComplete="current-password"
                />
                <button
                  type="button"
                  className="input-toggle"
                  onClick={() => setShowPw(!showPw)}
                  aria-label="Toggle password visibility"
                >
                  {showPw ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
              {errors.password && <span className="field-error">{errors.password}</span>}
            </div>

            <div className="auth-options">
              <Link to="/forgot-password" className="auth-link">Forgot Password?</Link>
            </div>

            <button type="submit" className="btn btn-primary btn-lg auth-submit" disabled={loading} id="login-submit">
              {loading ? <div className="spinner" style={{ width: 20, height: 20, borderWidth: 2 }} /> : 'Log In'}
            </button>
          </form>

          <p className="auth-switch">
            Don't have an account? <Link to="/register" className="auth-link">Create Account</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
