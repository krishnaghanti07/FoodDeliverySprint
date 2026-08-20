import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Mail, Phone, Clock, MapPin, MessageCircle, Send,
  ChevronRight, Store, Truck, User, Shield
} from 'lucide-react';
import toast from 'react-hot-toast';
import './ContactUsPage.css';

const CONTACT_REASONS = [
  'Order Issue',
  'Payment / Refund',
  'Account Problem',
  'Restaurant Complaint',
  'Delivery Issue',
  'Partner Enquiry',
  'Delivery Agent Enquiry',
  'Technical Bug',
  'Feedback / Suggestion',
  'Other',
];

export default function ContactUsPage() {
  const [form, setForm] = useState({
    name: '',
    email: '',
    reason: '',
    orderId: '',
    message: '',
  });
  const [errors, setErrors] = useState({});
  const [submitted, setSubmitted] = useState(false);
  const [loading, setLoading] = useState(false);

  const validate = () => {
    const errs = {};
    if (!form.name.trim() || form.name.trim().length < 2) errs.name = 'Please enter your full name';
    if (!form.email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) errs.email = 'Enter a valid email address';
    if (!form.reason) errs.reason = 'Please select a reason';
    if (!form.message.trim() || form.message.trim().length < 20) errs.message = 'Message must be at least 20 characters';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleChange = (field, value) => {
    setForm(p => ({ ...p, [field]: value }));
    if (errors[field]) setErrors(p => { const e = { ...p }; delete e[field]; return e; });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;
    setLoading(true);
    // Simulate submission (no backend endpoint — this is a static contact form)
    await new Promise(r => setTimeout(r, 1200));
    setLoading(false);
    setSubmitted(true);
    toast.success('Message sent! We\'ll get back to you within 24 hours.');
  };

  return (
    <div className="cu-page page-enter">

      {/* ── Hero ── */}
      <section className="cu-hero">
        <div className="cu-hero-shapes">
          <div className="cu-shape cu-shape-1" />
          <div className="cu-shape cu-shape-2" />
        </div>
        <div className="container cu-hero-content">
          <span className="cu-hero-badge">💬 Contact Us</span>
          <h1 className="display-lg cu-hero-title">
            We're here to <span className="text-gradient">help you</span>
          </h1>
          <p className="body-lg cu-hero-sub">
            Have a question, issue, or feedback? Reach out and our team will respond within 24 hours.
          </p>
        </div>
      </section>

      <div className="container cu-body">
        <div className="cu-layout">

          {/* ── Left: Contact info ── */}
          <aside className="cu-info">

            {/* Info cards */}
            <div className="cu-info-card">
              <div className="cu-info-icon" style={{ background: 'var(--primary-fixed)', color: 'var(--primary)' }}>
                <Mail size={22} />
              </div>
              <div>
                <h4 className="cu-info-label">Email Support</h4>
                <a href="mailto:support@foodrush.com" className="cu-info-value">support@foodrush.com</a>
                <p className="cu-info-note">Response within 24 hours</p>
              </div>
            </div>

            <div className="cu-info-card">
              <div className="cu-info-icon" style={{ background: 'var(--secondary-fixed)', color: 'var(--secondary)' }}>
                <Phone size={22} />
              </div>
              <div>
                <h4 className="cu-info-label">Phone Support</h4>
                <a href="tel:+918001234567" className="cu-info-value">+91 800 123 4567</a>
                <p className="cu-info-note">Mon – Sun, 9 AM – 9 PM IST</p>
              </div>
            </div>

            <div className="cu-info-card">
              <div className="cu-info-icon" style={{ background: '#e0f7fa', color: 'var(--tertiary)' }}>
                <Clock size={22} />
              </div>
              <div>
                <h4 className="cu-info-label">Support Hours</h4>
                <p className="cu-info-value">9:00 AM – 9:00 PM</p>
                <p className="cu-info-note">7 days a week including holidays</p>
              </div>
            </div>

            <div className="cu-info-card">
              <div className="cu-info-icon" style={{ background: '#fff3e0', color: '#e65100' }}>
                <MapPin size={22} />
              </div>
              <div>
                <h4 className="cu-info-label">Headquarters</h4>
                <p className="cu-info-value">FoodRush Technologies</p>
                <p className="cu-info-note">Bengaluru, Karnataka, India</p>
              </div>
            </div>

            {/* Divider */}
            <div className="cu-info-divider" />

            {/* Role-specific contacts */}
            <h3 className="cu-info-section-title">Dedicated Support</h3>

            <div className="cu-role-cards">
              <div className="cu-role-card">
                <Store size={18} />
                <div>
                  <p className="cu-role-title">Restaurant Partners</p>
                  <a href="mailto:partners@foodrush.com" className="cu-role-email">partners@foodrush.com</a>
                </div>
              </div>
              <div className="cu-role-card">
                <Truck size={18} />
                <div>
                  <p className="cu-role-title">Delivery Agents</p>
                  <a href="mailto:agents@foodrush.com" className="cu-role-email">agents@foodrush.com</a>
                </div>
              </div>
              <div className="cu-role-card">
                <Shield size={18} />
                <div>
                  <p className="cu-role-title">Safety & Trust</p>
                  <a href="mailto:trust@foodrush.com" className="cu-role-email">trust@foodrush.com</a>
                </div>
              </div>
            </div>

            {/* Help Center link */}
            <Link to="/help" className="cu-help-link">
              <MessageCircle size={16} />
              Browse Help Center
              <ChevronRight size={14} />
            </Link>
          </aside>

          {/* ── Right: Contact form ── */}
          <main className="cu-form-wrap">
            {submitted ? (
              <div className="cu-success">
                <div className="cu-success-icon">✅</div>
                <h2 className="headline-lg">Message Sent!</h2>
                <p className="body-lg text-muted">
                  Thanks for reaching out, <strong>{form.name}</strong>. We've received your message and will reply to <strong>{form.email}</strong> within 24 hours.
                </p>
                <div className="cu-success-actions">
                  <button className="btn btn-primary" onClick={() => { setSubmitted(false); setForm({ name: '', email: '', reason: '', orderId: '', message: '' }); }}>
                    Send Another Message
                  </button>
                  <Link to="/" className="btn btn-outline">Back to Home</Link>
                </div>
              </div>
            ) : (
              <form className="cu-form" onSubmit={handleSubmit} noValidate>
                <div className="cu-form-header">
                  <h2 className="headline-lg">Send us a message</h2>
                  <p className="body-sm text-muted">Fill in the form below and we'll get back to you as soon as possible.</p>
                </div>

                {/* Name + Email */}
                <div className="cu-form-row">
                  <div className="form-group">
                    <label className="form-label">Full Name *</label>
                    <input
                      type="text"
                      className={`form-input${errors.name ? ' input-error' : ''}`}
                      placeholder="Your full name"
                      value={form.name}
                      onChange={e => handleChange('name', e.target.value)}
                      maxLength={100}
                    />
                    {errors.name && <span className="field-error">{errors.name}</span>}
                  </div>

                  <div className="form-group">
                    <label className="form-label">Email Address *</label>
                    <input
                      type="email"
                      className={`form-input${errors.email ? ' input-error' : ''}`}
                      placeholder="you@example.com"
                      value={form.email}
                      onChange={e => handleChange('email', e.target.value)}
                    />
                    {errors.email && <span className="field-error">{errors.email}</span>}
                  </div>
                </div>

                {/* Reason */}
                <div className="form-group">
                  <label className="form-label">Reason for Contact *</label>
                  <select
                    className={`form-input form-select${errors.reason ? ' input-error' : ''}`}
                    value={form.reason}
                    onChange={e => handleChange('reason', e.target.value)}
                  >
                    <option value="">Select a reason…</option>
                    {CONTACT_REASONS.map(r => (
                      <option key={r} value={r}>{r}</option>
                    ))}
                  </select>
                  {errors.reason && <span className="field-error">{errors.reason}</span>}
                </div>

                {/* Order ID (optional) */}
                <div className="form-group">
                  <label className="form-label">
                    Order ID <span style={{ color: 'var(--outline)', fontWeight: 400 }}>(optional — helps us resolve faster)</span>
                  </label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="e.g. 4e5d72d9-bb47-407b-8ac2-3bf7e2a55d8d"
                    value={form.orderId}
                    onChange={e => handleChange('orderId', e.target.value)}
                  />
                </div>

                {/* Message */}
                <div className="form-group">
                  <label className="form-label">Message *</label>
                  <textarea
                    className={`form-input cu-textarea${errors.message ? ' input-error' : ''}`}
                    placeholder="Describe your issue or question in detail (min 20 characters)…"
                    value={form.message}
                    onChange={e => handleChange('message', e.target.value)}
                    rows={6}
                    maxLength={2000}
                  />
                  <div className="cu-char-count">
                    <span className={form.message.length < 20 ? 'cu-char-warn' : ''}>
                      {form.message.length}/2000
                    </span>
                  </div>
                  {errors.message && <span className="field-error">{errors.message}</span>}
                </div>

                {/* Privacy note */}
                <p className="cu-privacy-note">
                  <Shield size={13} />
                  Your information is kept private and used only to resolve your query. See our{' '}
                  <a href="#">Privacy Policy</a>.
                </p>

                <button type="submit" className="btn btn-primary btn-lg cu-submit" disabled={loading}>
                  {loading ? (
                    <><div className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} /> Sending…</>
                  ) : (
                    <><Send size={18} /> Send Message</>
                  )}
                </button>
              </form>
            )}
          </main>
        </div>

        {/* ── FAQ teaser ── */}
        <div className="cu-faq-teaser">
          <div className="cu-faq-teaser-text">
            <h3 className="headline-md">Looking for quick answers?</h3>
            <p className="body-md text-muted">Browse our Help Center for instant answers to common questions.</p>
          </div>
          <Link to="/help" className="btn btn-outline">
            <MessageCircle size={18} /> Visit Help Center <ChevronRight size={16} />
          </Link>
        </div>
      </div>
    </div>
  );
}
