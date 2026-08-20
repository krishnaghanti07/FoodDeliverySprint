import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { Eye, EyeOff, Mail, Lock, User, Phone, CheckCircle, Circle } from 'lucide-react';
import toast from 'react-hot-toast';
import { validators } from '../../utils/validation';
import './Auth.css';

// ── Password strength calculator ────────────────────────────────────
function getPasswordStrength(pw) {
  if (!pw) return { score: 0, label: '', color: '' };
  let score = 0;
  const checks = {
    length:  pw.length >= 8,
    upper:   /[A-Z]/.test(pw),
    lower:   /[a-z]/.test(pw),
    number:  /\d/.test(pw),
    special: /[^A-Za-z0-9]/.test(pw),
  };
  score = Object.values(checks).filter(Boolean).length;
  const map = [
    { label: '', color: '' },
    { label: 'Weak', color: 'weak' },
    { label: 'Fair', color: 'fair' },
    { label: 'Good', color: 'good' },
    { label: 'Strong', color: 'strong' },
    { label: 'Strong', color: 'strong' },
  ];
  return { score, ...map[score], checks };
}

function PasswordStrengthMeter({ password }) {
  const { score, label, color, checks } = getPasswordStrength(password);
  if (!password) return null;

  const hints = [
    { key: 'length',  text: '8+ chars' },
    { key: 'upper',   text: 'Uppercase' },
    { key: 'number',  text: 'Number' },
    { key: 'special', text: 'Symbol' },
  ];

  return (
    <div className="pw-strength">
      <div className="pw-strength-bars">
        {[1,2,3,4].map(i => (
          <div
            key={i}
            className={`pw-strength-bar ${score >= i ? `filled-${color}` : ''}`}
          />
        ))}
      </div>
      {label && <span className={`pw-strength-label ${color}`}>{label}</span>}
      <div className="pw-strength-hints">
        {hints.map(h => (
          <span key={h.key} className={`pw-hint ${checks?.[h.key] ? 'met' : 'unmet'}`}>
            {checks?.[h.key]
              ? <CheckCircle size={10} />
              : <Circle size={10} />}
            {h.text}
          </span>
        ))}
      </div>
    </div>
  );
}

export default function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const initialRole = searchParams.get('role') || 'Customer';

  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    mobile: '',
    password: '',
    confirmPassword: '',
    role: initialRole,
    vehicleType: '',
    vehicleNumber: '',
  });
  const [showPw, setShowPw] = useState(false);
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors(prev => { const e = {...prev}; delete e[name]; return e; });
  };

  const validate = () => {
    const errs = {};
    const nameErr = validators.fullName(formData.fullName);
    if (nameErr) errs.fullName = nameErr;
    const emailErr = validators.email(formData.email);
    if (emailErr) errs.email = emailErr;
    const mobileErr = validators.mobile(formData.mobile);
    if (mobileErr) errs.mobile = mobileErr;
    const pwErr = validators.password(formData.password);
    if (pwErr) errs.password = pwErr;
    const confirmErr = validators.confirmPassword(formData.confirmPassword, formData.password);
    if (confirmErr) errs.confirmPassword = confirmErr;
    if (formData.role === 'DeliveryAgent') {
      if (!formData.vehicleType) errs.vehicleType = 'Vehicle type is required for Delivery Agent';
      if (formData.vehicleNumber && formData.vehicleNumber.trim().length > 15)
        errs.vehicleNumber = 'Vehicle number must be at most 15 characters';
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) {
      toast.error('Please fix the errors below');
      return;
    }

    setLoading(true);
    try {
      const mobileDigits = formData.mobile.replace(/\D/g, '');
      const registrationData = {
        fullName: formData.fullName.trim(),
        email: formData.email.trim().toLowerCase(),
        mobile: mobileDigits,
        password: formData.password,
        role: formData.role,
      };
      if (formData.role === 'DeliveryAgent') {
        registrationData.vehicleType = formData.vehicleType.trim();
        if (formData.vehicleNumber?.trim()) {
          registrationData.vehicleNumber = formData.vehicleNumber.trim();
        }
      }
      await register(registrationData);
      toast.success('Account created! Please login.');
      navigate('/login');
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.errors?.join(', ') || 'Registration failed';
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
          <h2 className="display-lg">Join the<br />revolution.</h2>
          <p className="body-lg">Start ordering from thousands of restaurants near you.</p>
        </div>
        <div className="auth-visual-badges">
          <span className="auth-visual-badge">🍕 500+ Restaurants</span>
          <span className="auth-visual-badge">⚡ 30 min delivery</span>
          <span className="auth-visual-badge">🔒 Secure payments</span>
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
            <h1 className="headline-lg">Create your account</h1>
            <p className="body-md text-muted">Join FoodRush today — it's free!</p>
          </div>

          <form className="auth-form" onSubmit={handleSubmit} id="register-form" noValidate>
            <div className="form-group">
              <label className="form-label">I want to</label>
              <div className="auth-role-select">
                {['Customer', 'Partner', 'DeliveryAgent'].map((r) => (
                  <button
                    key={r}
                    type="button"
                    className={`role-option ${formData.role === r ? 'active' : ''}`}
                    onClick={() => setFormData((prev) => ({ ...prev, role: r }))}
                  >
                    <span className="role-icon">
                      {r === 'Customer' ? '🍔' : r === 'Partner' ? '🏪' : '🛵'}
                    </span>
                    {r === 'DeliveryAgent' ? 'Deliver' : r === 'Partner' ? 'Partner' : 'Order'}
                  </button>
                ))}
              </div>
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="reg-fullname">Full Name *</label>
              <div className="input-icon-wrapper">
                <User size={18} className="input-icon" />
                <input id="reg-fullname" name="fullName" type="text" className={`form-input ${errors.fullName ? 'input-error' : ''}`} placeholder="John Doe" value={formData.fullName} onChange={handleChange} />
              </div>
              {errors.fullName && <span className="field-error">{errors.fullName}</span>}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="reg-email">Email Address *</label>
              <div className="input-icon-wrapper">
                <Mail size={18} className="input-icon" />
                <input id="reg-email" name="email" type="email" className={`form-input ${errors.email ? 'input-error' : ''}`} placeholder="you@example.com" value={formData.email} onChange={handleChange} autoComplete="email" />
              </div>
              {errors.email && <span className="field-error">{errors.email}</span>}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="reg-mobile">Mobile Number * (10 digits)</label>
              <div className="input-icon-wrapper">
                <Phone size={18} className="input-icon" />
                <input id="reg-mobile" name="mobile" type="tel" className={`form-input ${errors.mobile ? 'input-error' : ''}`} placeholder="9876543210" value={formData.mobile} onChange={handleChange} maxLength={10} />
              </div>
              {errors.mobile && <span className="field-error">{errors.mobile}</span>}
            </div>

            {/* Delivery Agent Specific Fields */}
            {formData.role === 'DeliveryAgent' && (
              <>
                <div className="form-group">
                  <label className="form-label" htmlFor="reg-vehicle-type">Vehicle Type *</label>
                  <div className="input-icon-wrapper">
                    <span className="input-icon">🛵</span>
                    <select id="reg-vehicle-type" name="vehicleType" className={`form-input ${errors.vehicleType ? 'input-error' : ''}`} value={formData.vehicleType} onChange={handleChange}>
                      <option value="">Select Vehicle Type</option>
                      <option value="Bike">Bike</option>
                      <option value="Scooter">Scooter</option>
                      <option value="Bicycle">Bicycle</option>
                      <option value="Car">Car</option>
                    </select>
                  </div>
                  {errors.vehicleType && <span className="field-error">{errors.vehicleType}</span>}
                </div>

                <div className="form-group">
                  <label className="form-label" htmlFor="reg-vehicle-number">Vehicle Number (Optional)</label>
                  <div className="input-icon-wrapper">
                    <span className="input-icon">🔢</span>
                    <input id="reg-vehicle-number" name="vehicleNumber" type="text" className={`form-input ${errors.vehicleNumber ? 'input-error' : ''}`} placeholder="e.g., MH12AB1234" value={formData.vehicleNumber} onChange={handleChange} maxLength={15} />
                  </div>
                  {errors.vehicleNumber && <span className="field-error">{errors.vehicleNumber}</span>}
                </div>
              </>
            )}

            <div className="form-group">
              <label className="form-label" htmlFor="reg-password">Password *</label>
              <div className="input-icon-wrapper">
                <Lock size={18} className="input-icon" />
                <input id="reg-password" name="password" type={showPw ? 'text' : 'password'} className={`form-input ${errors.password ? 'input-error' : ''}`} placeholder="Min. 8 chars, uppercase, number" value={formData.password} onChange={handleChange} autoComplete="new-password" />
                <button type="button" className="input-toggle" onClick={() => setShowPw(!showPw)}>
                  {showPw ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
              {errors.password && <span className="field-error">{errors.password}</span>}
              <PasswordStrengthMeter password={formData.password} />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="reg-confirm">Confirm Password *</label>
              <div className="input-icon-wrapper">
                <Lock size={18} className="input-icon" />
                <input id="reg-confirm" name="confirmPassword" type="password" className={`form-input ${errors.confirmPassword ? 'input-error' : ''}`} placeholder="Re-enter password" value={formData.confirmPassword} onChange={handleChange} autoComplete="new-password" />
              </div>
              {errors.confirmPassword && <span className="field-error">{errors.confirmPassword}</span>}
            </div>

            <button type="submit" className="btn btn-primary btn-lg auth-submit" disabled={loading} id="register-submit">
              {loading ? <div className="spinner" style={{ width: 20, height: 20, borderWidth: 2 }} /> : 'Create Account'}
            </button>
          </form>

          <p className="auth-switch">
            Already have an account? <Link to="/login" className="auth-link">Log In</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
