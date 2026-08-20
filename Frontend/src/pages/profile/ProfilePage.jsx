import { useState, useEffect } from 'react';
import { User, Mail, Phone, MapPin, Edit2, Save, Plus, Trash2, Truck, Shield } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import toast from 'react-hot-toast';
import ImageUpload from '../../components/common/ImageUpload';
import { validators } from '../../utils/validation';
import './ProfilePage.css';

export default function ProfilePage() {
  const { user, fetchProfile, updateProfile } = useAuth();
  const [editing, setEditing] = useState(false);
  const [profileData, setProfileData] = useState(null);
  const [formData, setFormData] = useState({
    fullName: '',
    mobile: '',
    profileImageUrl: '',
    vehicleType: '',
    vehicleNumber: '',
    isAvailableForDelivery: false
  });
  const [profileErrors, setProfileErrors] = useState({});

  const [addresses, setAddresses] = useState([]);
  const [showAddressForm, setShowAddressForm] = useState(false);
  const [addressForm, setAddressForm] = useState({ fullAddress: '', city: '', state: '', pincode: '', label: 'Home' });
  const [addressErrors, setAddressErrors] = useState({});
  const [addressTouched, setAddressTouched] = useState({});

  const [loading, setLoading] = useState(false);

  // Email verification
  const [showVerificationSection, setShowVerificationSection] = useState(false);
  const [otpSent, setOtpSent] = useState(false);
  const [otpCode, setOtpCode] = useState('');
  const [otpError, setOtpError] = useState('');
  const [sendingOtp, setSendingOtp] = useState(false);
  const [verifying, setVerifying] = useState(false);

  const isDeliveryAgent = profileData?.role?.toLowerCase() === 'deliveryagent';
  const isCustomer = profileData?.role?.toLowerCase() === 'customer';

  useEffect(() => { loadData(); }, []);

  const loadData = async () => {
    try {
      const profile = await fetchProfile();
      setProfileData(profile);
      setFormData({
        fullName: profile.fullName || '',
        mobile: profile.mobile || '',
        profileImageUrl: profile.profileImageUrl || '',
        vehicleType: profile.vehicleType || '',
        vehicleNumber: profile.vehicleNumber || '',
        isAvailableForDelivery: profile.isAvailableForDelivery || false
      });

      // Only load addresses for Customers — other roles don't need delivery addresses
      if (profile.role?.toLowerCase() === 'customer') {
        try {
          const res = await api.get(API_ENDPOINTS.auth.addresses);
          const addrData = res.data?.data || res.data;
          setAddresses(Array.isArray(addrData) ? addrData : addrData?.addresses || []);
        } catch (error) {
          console.error('Failed to load addresses:', error);
        }
      }
    } catch (error) {
      console.error('Failed to load profile:', error);
    }
  };

  // ── Profile form validation ─────────────────────────────────────────
  const validateProfile = () => {
    const errs = {};
    const nameErr = validators.fullName(formData.fullName);
    if (nameErr) errs.fullName = nameErr;
    const mobileErr = validators.mobile(formData.mobile);
    if (mobileErr) errs.mobile = mobileErr;
    if (isDeliveryAgent) {
      if (!formData.vehicleType) errs.vehicleType = 'Vehicle type is required';
      if (formData.vehicleNumber && formData.vehicleNumber.trim().length > 15)
        errs.vehicleNumber = 'Vehicle number must be at most 15 characters';
    }
    setProfileErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSave = async () => {
    if (!validateProfile()) {
      toast.error('Please fix the errors before saving');
      return;
    }
    setLoading(true);
    try {
      const mobileDigits = formData.mobile.replace(/\D/g, '');
      const updateData = {
        fullName: formData.fullName.trim(),
        mobile: mobileDigits,
        profileImageUrl: formData.profileImageUrl || null
      };
      if (isDeliveryAgent) {
        updateData.vehicleType = formData.vehicleType || null;
        updateData.vehicleNumber = formData.vehicleNumber?.trim() || null;
        updateData.isAvailableForDelivery = formData.isAvailableForDelivery;
      }
      await updateProfile(updateData);
      toast.success('Profile updated successfully');
      setEditing(false);
      setProfileErrors({});
      await loadData();
    } catch (err) {
      toast.error(err.response?.data?.message || err.message || 'Update failed');
    } finally {
      setLoading(false);
    }
  };

  const handleProfileFieldChange = (field, value) => {
    setFormData(p => ({ ...p, [field]: value }));
    // Clear error on change
    if (profileErrors[field]) setProfileErrors(p => { const e = {...p}; delete e[field]; return e; });
  };

  // ── Address form validation ─────────────────────────────────────────
  const validateAddressField = (name, value) => {
    switch (name) {
      case 'fullAddress':
        if (!value || !value.trim()) return 'Full address is required';
        if (value.trim().length < 10) return 'Please enter a complete address (min 10 characters)';
        return null;
      case 'city':
        return validators.city(value);
      case 'state':
        return validators.state(value);
      case 'pincode':
        return validators.pincode(value);
      default:
        return null;
    }
  };

  const validateAddress = () => {
    const fields = ['fullAddress', 'city', 'state', 'pincode'];
    const errs = {};
    fields.forEach(f => {
      const err = validateAddressField(f, addressForm[f]);
      if (err) errs[f] = err;
    });
    setAddressErrors(errs);
    setAddressTouched(Object.fromEntries(fields.map(f => [f, true])));
    return Object.keys(errs).length === 0;
  };

  const handleAddressChange = (field, value) => {
    setAddressForm(p => ({ ...p, [field]: value }));
    if (addressTouched[field]) {
      const err = validateAddressField(field, value);
      setAddressErrors(p => ({ ...p, [field]: err || undefined }));
    }
  };

  const handleAddressBlur = (field) => {
    setAddressTouched(p => ({ ...p, [field]: true }));
    const err = validateAddressField(field, addressForm[field]);
    setAddressErrors(p => ({ ...p, [field]: err || undefined }));
  };

  const handleAddAddress = async () => {
    if (!validateAddress()) {
      toast.error('Please fix the address errors');
      return;
    }
    try {
      await api.post(API_ENDPOINTS.auth.addresses, addressForm);
      toast.success('Address added successfully');
      setShowAddressForm(false);
      setAddressForm({ fullAddress: '', city: '', state: '', pincode: '', label: 'Home' });
      setAddressErrors({});
      setAddressTouched({});
      const res = await api.get(API_ENDPOINTS.auth.addresses);
      const addrData = res.data?.data || res.data;
      setAddresses(Array.isArray(addrData) ? addrData : addrData?.addresses || []);
    } catch (err) {
      toast.error(err.response?.data?.message || err.message || 'Failed to add address');
    }
  };

  const handleDeleteAddress = async (id) => {
    try {
      await api.delete(API_ENDPOINTS.auth.addressById(id));
      setAddresses(prev => prev.filter(a => a.id !== id));
      toast.success('Address removed');
    } catch { toast.error('Failed to delete address'); }
  };

  const handleSetDefault = async (id) => {
    try {
      await api.patch(API_ENDPOINTS.auth.setDefaultAddress(id));
      toast.success('Default address updated');
      const res = await api.get(API_ENDPOINTS.auth.addresses);
      const addrData = res.data?.data || res.data;
      setAddresses(Array.isArray(addrData) ? addrData : addrData?.addresses || []);
    } catch { toast.error('Failed to set default'); }
  };

  // ── Email OTP validation ────────────────────────────────────────────
  const handleSendVerificationOtp = async () => {
    setSendingOtp(true);
    try {
      await api.post('/gateway/auth/send-otp', {
        email: profileData?.email || user?.email,
        purpose: 'EmailVerification'
      });
      toast.success('Verification code sent to your email');
      setOtpSent(true);
      setOtpError('');
    } catch (error) {
      toast.error('Failed to send verification code');
    } finally {
      setSendingOtp(false);
    }
  };

  const handleVerifyEmail = async () => {
    const err = validators.otpCode(otpCode);
    if (err) { setOtpError(err); return; }
    setOtpError('');
    setVerifying(true);
    try {
      await api.post('/gateway/auth/verify-email', {
        email: profileData?.email || user?.email,
        otpCode
      });
      toast.success('Email verified successfully!');
      setShowVerificationSection(false);
      setOtpSent(false);
      setOtpCode('');
      await loadData();
    } catch (error) {
      setOtpError(error.response?.data?.message || 'Invalid or expired code');
    } finally {
      setVerifying(false);
    }
  };

  // ── Address field error helper ──────────────────────────────────────
  const addrFieldErr = (f) => addressTouched[f] && addressErrors[f] ? addressErrors[f] : null;
  const addrInputClass = (f) => `form-input${addrFieldErr(f) ? ' input-error' : ''}`;

  return (
    <div className="profile-page page-enter">
      <div className="container">
        <h1 className="headline-lg" style={{ marginBottom: 'var(--space-xl)' }}>My Profile</h1>

        <div className="profile-grid">
          {/* ── Profile Card ── */}
          <div className="card profile-card">
            <div className="card-body">
              {/* Avatar with hover overlay */}
              <div className="profile-avatar-wrap" onClick={() => editing && document.querySelector('[data-avatar-upload]')?.click()}>
                <div className="profile-avatar">
                  {formData.profileImageUrl && !formData.profileImageUrl.startsWith('data:') ? (
                    <img src={formData.profileImageUrl} alt="Profile" style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: '50%' }} />
                  ) : (
                    <User size={36} />
                  )}
                </div>
                {editing && (
                  <div className="profile-avatar-overlay" aria-hidden="true">
                    <User size={18} />
                    <span>Change</span>
                  </div>
                )}
              </div>
              <h2 className="headline-md" style={{ textAlign: 'center', marginBottom: 2 }}>
                {profileData?.fullName || user?.fullName}
              </h2>
              <p className="body-sm text-muted" style={{ textAlign: 'center', marginBottom: 'var(--space-lg)' }}>
                <span className="badge badge-primary">{profileData?.role || user?.role}</span>
                {profileData?.isEmailVerified && (
                  <span className="badge badge-success" style={{ marginLeft: 4 }}>
                    <Shield size={12} /> Verified
                  </span>
                )}
              </p>

              {editing ? (
                <div className="profile-form" noValidate>
                  {/* Full Name */}
                  <div className="form-group">
                    <label className="form-label">Full Name *</label>
                    <input
                      className={`form-input${profileErrors.fullName ? ' input-error' : ''}`}
                      value={formData.fullName}
                      onChange={e => handleProfileFieldChange('fullName', e.target.value)}
                      placeholder="Enter your full name"
                      maxLength={100}
                    />
                    {profileErrors.fullName && <span className="field-error">{profileErrors.fullName}</span>}
                  </div>

                  {/* Mobile */}
                  <div className="form-group">
                    <label className="form-label">Mobile * (10 digits)</label>
                    <input
                      className={`form-input${profileErrors.mobile ? ' input-error' : ''}`}
                      value={formData.mobile}
                      onChange={e => handleProfileFieldChange('mobile', e.target.value.replace(/\D/g, '').slice(0, 10))}
                      placeholder="9876543210"
                      maxLength={10}
                      type="tel"
                    />
                    {profileErrors.mobile && <span className="field-error">{profileErrors.mobile}</span>}
                  </div>

                  {/* Profile Image */}
                  <ImageUpload
                    type="profile"
                    currentImageUrl={formData.profileImageUrl}
                    onImageUploaded={(url) => handleProfileFieldChange('profileImageUrl', url)}
                    label="Profile Image (optional)"
                  />

                  {/* Delivery Agent Fields */}
                  {isDeliveryAgent && (
                    <>
                      <div style={{ borderTop: '1px solid var(--outline-variant)', paddingTop: 'var(--space-md)', marginTop: 'var(--space-md)' }}>
                        <h3 className="headline-sm" style={{ marginBottom: 'var(--space-md)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <Truck size={18} /> Delivery Agent Details
                        </h3>
                      </div>

                      <div className="form-group">
                        <label className="form-label">Vehicle Type *</label>
                        <select
                          className={`form-input form-select${profileErrors.vehicleType ? ' input-error' : ''}`}
                          value={formData.vehicleType}
                          onChange={e => handleProfileFieldChange('vehicleType', e.target.value)}
                        >
                          <option value="">Select vehicle type</option>
                          <option value="Bike">Bike</option>
                          <option value="Scooter">Scooter</option>
                          <option value="Bicycle">Bicycle</option>
                          <option value="Car">Car</option>
                        </select>
                        {profileErrors.vehicleType && <span className="field-error">{profileErrors.vehicleType}</span>}
                      </div>

                      <div className="form-group">
                        <label className="form-label">Vehicle Number <span style={{ color: 'var(--outline)', fontWeight: 400 }}>(optional)</span></label>
                        <input
                          className={`form-input${profileErrors.vehicleNumber ? ' input-error' : ''}`}
                          value={formData.vehicleNumber}
                          onChange={e => handleProfileFieldChange('vehicleNumber', e.target.value.toUpperCase())}
                          placeholder="e.g., MH12AB1234"
                          maxLength={15}
                        />
                        {profileErrors.vehicleNumber && <span className="field-error">{profileErrors.vehicleNumber}</span>}
                      </div>

                      <div className="form-group">
                        <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
                          <input
                            type="checkbox"
                            checked={formData.isAvailableForDelivery}
                            onChange={e => handleProfileFieldChange('isAvailableForDelivery', e.target.checked)}
                            style={{ width: '18px', height: '18px' }}
                          />
                          <span className="form-label" style={{ margin: 0 }}>Available for Delivery</span>
                        </label>
                      </div>
                    </>
                  )}

                  <div style={{ display: 'flex', gap: '0.5rem', marginTop: 'var(--space-md)' }}>
                    <button className="btn btn-primary" onClick={handleSave} disabled={loading} style={{ flex: 1 }}>
                      <Save size={16} /> {loading ? 'Saving...' : 'Save Changes'}
                    </button>
                    <button className="btn btn-ghost" onClick={() => { setEditing(false); setProfileErrors({}); loadData(); }}>
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <div className="profile-info">
                  <div className="pi-row"><Mail size={16} /><span>{profileData?.email || user?.email}</span></div>
                  <div className="pi-row"><Phone size={16} /><span>{profileData?.mobile || 'Not provided'}</span></div>

                  {/* Email Verification */}
                  {profileData?.role?.toLowerCase() !== 'admin' && (
                    <div style={{
                      backgroundColor: profileData?.isEmailVerified ? 'var(--success-container)' : 'var(--warning-container)',
                      padding: 'var(--space-md)',
                      borderRadius: 'var(--radius-md)',
                      marginTop: 'var(--space-md)',
                      border: `1px solid ${profileData?.isEmailVerified ? 'var(--success)' : 'var(--warning)'}`
                    }}>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 'var(--space-sm)' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <Shield size={18} />
                          <span style={{ fontWeight: 500 }}>Email Verification</span>
                        </div>
                        <span className={`badge ${profileData?.isEmailVerified ? 'badge-success' : 'badge-warning'}`}>
                          {profileData?.isEmailVerified ? 'Verified ✓' : 'Not Verified'}
                        </span>
                      </div>

                      {!profileData?.isEmailVerified && (
                        <>
                          {!showVerificationSection ? (
                            <button className="btn btn-secondary btn-sm" onClick={() => setShowVerificationSection(true)} style={{ width: '100%', marginTop: 'var(--space-sm)' }}>
                              Verify Email Now
                            </button>
                          ) : !otpSent ? (
                            <div style={{ marginTop: 'var(--space-sm)' }}>
                              <p style={{ fontSize: '0.875rem', marginBottom: 'var(--space-sm)', color: 'var(--on-surface-variant)' }}>
                                We'll send a 6-digit code to <strong>{profileData?.email || user?.email}</strong>
                              </p>
                              <div style={{ display: 'flex', gap: '0.5rem' }}>
                                <button className="btn btn-primary btn-sm" onClick={handleSendVerificationOtp} disabled={sendingOtp} style={{ flex: 1 }}>
                                  {sendingOtp ? 'Sending...' : 'Send Code'}
                                </button>
                                <button className="btn btn-ghost btn-sm" onClick={() => setShowVerificationSection(false)}>Cancel</button>
                              </div>
                            </div>
                          ) : (
                            <div style={{ marginTop: 'var(--space-sm)' }}>
                              <p style={{ fontSize: '0.875rem', marginBottom: 'var(--space-sm)', color: 'var(--on-surface-variant)' }}>
                                Enter the 6-digit code sent to your email:
                              </p>
                              <input
                                type="text"
                                className={`form-input${otpError ? ' input-error' : ''}`}
                                placeholder="0 0 0 0 0 0"
                                value={otpCode}
                                onChange={(e) => { setOtpCode(e.target.value.replace(/\D/g, '').slice(0, 6)); setOtpError(''); }}
                                maxLength={6}
                                style={{ textAlign: 'center', fontSize: '1.25rem', letterSpacing: '0.5rem', marginBottom: '4px' }}
                              />
                              {otpError && <span className="field-error" style={{ marginBottom: '8px', display: 'block' }}>{otpError}</span>}
                              <div style={{ display: 'flex', gap: '0.5rem', marginTop: '8px' }}>
                                <button className="btn btn-primary btn-sm" onClick={handleVerifyEmail} disabled={verifying || otpCode.length !== 6} style={{ flex: 1 }}>
                                  {verifying ? 'Verifying...' : 'Verify Email'}
                                </button>
                                <button className="btn btn-ghost btn-sm" onClick={handleSendVerificationOtp} disabled={sendingOtp}>
                                  {sendingOtp ? 'Sending...' : 'Resend'}
                                </button>
                              </div>
                            </div>
                          )}
                        </>
                      )}

                      {profileData?.isEmailVerified && (
                        <p style={{ fontSize: '0.875rem', marginTop: 'var(--space-sm)', color: 'var(--on-surface-variant)' }}>
                          Your email is verified. You have full access to all features.
                        </p>
                      )}
                    </div>
                  )}

                  {/* Wallet Balance */}
                  {profileData?.role?.toLowerCase() === 'customer' && profileData?.walletBalance !== undefined && (
                    <div className="pi-row" style={{ backgroundColor: 'var(--surface-variant)', padding: 'var(--space-md)', borderRadius: 'var(--radius-md)', marginTop: 'var(--space-md)' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flex: 1 }}>
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                          <rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect>
                          <line x1="1" y1="10" x2="23" y2="10"></line>
                        </svg>
                        <span style={{ fontWeight: 500 }}>Wallet Balance</span>
                      </div>
                      <span style={{ fontSize: '1.25rem', fontWeight: 'bold', color: 'var(--primary)' }}>
                        ₹{profileData.walletBalance.toFixed(2)}
                      </span>
                    </div>
                  )}

                  {/* Delivery Agent Details */}
                  {isDeliveryAgent && (
                    <>
                      <div style={{ borderTop: '1px solid var(--outline-variant)', paddingTop: 'var(--space-md)', marginTop: 'var(--space-md)' }}>
                        <h3 className="headline-sm" style={{ marginBottom: 'var(--space-md)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <Truck size={18} /> Delivery Agent Details
                        </h3>
                      </div>
                      <div className="pi-row"><span style={{ fontWeight: 500 }}>Vehicle Type:</span><span>{profileData?.vehicleType || 'Not set'}</span></div>
                      <div className="pi-row"><span style={{ fontWeight: 500 }}>Vehicle Number:</span><span>{profileData?.vehicleNumber || 'Not set'}</span></div>
                      <div className="pi-row">
                        <span style={{ fontWeight: 500 }}>Availability:</span>
                        <span className={`badge ${profileData?.isAvailableForDelivery ? 'badge-success' : 'badge-error'}`}>
                          {profileData?.isAvailableForDelivery ? 'Available' : 'Unavailable'}
                        </span>
                      </div>
                    </>
                  )}

                  <button className="btn btn-primary" onClick={() => setEditing(true)} style={{ marginTop: 'var(--space-md)', width: '100%' }}>
                    <Edit2 size={16} /> Edit Profile
                  </button>

                  {profileData?.role?.toLowerCase() === 'customer' && (
                    <button className="btn btn-secondary" onClick={() => window.location.href = '/wallet'} style={{ marginTop: 'var(--space-sm)', width: '100%' }}>
                      View Wallet Transactions
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>

          {/* ── Addresses — Customers only ── */}
          {isCustomer && (
            <div className="profile-addresses">
              <div className="section-header">
                <h2 className="headline-lg">Saved Addresses</h2>
                <button className="btn btn-primary" onClick={() => { setShowAddressForm(true); setAddressErrors({}); setAddressTouched({}); }}>
                  <Plus size={16} /> Add Address
                </button>
              </div>

              {addresses.length === 0 ? (
                <div className="empty-state">
                  <MapPin size={48} className="empty-icon" />
                  <p className="body-lg text-muted">No saved addresses yet</p>
                </div>
              ) : (
                <div className="addresses-grid">
                  {addresses.map(addr => (
                    <div key={addr.id} className={`address-card ${addr.isDefault ? 'is-default' : ''}`}>
                      <div className="address-header">
                        <span className="badge badge-primary">{addr.label}</span>
                        {addr.isDefault && <span className="badge badge-success">Default</span>}
                      </div>
                      <p className="body-md">{addr.fullAddress}</p>
                      <p className="body-sm text-muted">{addr.city}, {addr.state} — {addr.pincode}</p>
                      <div className="address-actions">
                        {!addr.isDefault && (
                          <button className="btn btn-text" onClick={() => handleSetDefault(addr.id)}>Set as Default</button>
                        )}
                        <button className="btn btn-text text-error" onClick={() => handleDeleteAddress(addr.id)}>
                          <Trash2 size={14} /> Delete
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>

        {/* ── Add Address Modal — Customers only ── */}
        {isCustomer && showAddressForm && (
          <div className="modal-overlay" onClick={() => setShowAddressForm(false)}>
            <div className="modal-content" onClick={e => e.stopPropagation()}>
              <h2 className="headline-lg">Add New Address</h2>

              <div className="form-group">
                <label className="form-label">Label</label>
                <select className="form-input form-select" value={addressForm.label} onChange={e => setAddressForm(p => ({ ...p, label: e.target.value }))}>
                  <option value="Home">🏠 Home</option>
                  <option value="Work">💼 Work</option>
                  <option value="Other">📍 Other</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Full Address *</label>
                <textarea
                  className={addrInputClass('fullAddress')}
                  rows={3}
                  value={addressForm.fullAddress}
                  onChange={e => handleAddressChange('fullAddress', e.target.value)}
                  onBlur={() => handleAddressBlur('fullAddress')}
                  placeholder="Street, Building, Landmark (min 10 characters)"
                  maxLength={300}
                />
                {addrFieldErr('fullAddress') && <span className="field-error">{addrFieldErr('fullAddress')}</span>}
              </div>

              <div className="form-group">
                <label className="form-label">City *</label>
                <input
                  className={addrInputClass('city')}
                  value={addressForm.city}
                  onChange={e => handleAddressChange('city', e.target.value)}
                  onBlur={() => handleAddressBlur('city')}
                  placeholder="City"
                  maxLength={50}
                />
                {addrFieldErr('city') && <span className="field-error">{addrFieldErr('city')}</span>}
              </div>

              <div className="form-group">
                <label className="form-label">State *</label>
                <input
                  className={addrInputClass('state')}
                  value={addressForm.state}
                  onChange={e => handleAddressChange('state', e.target.value)}
                  onBlur={() => handleAddressBlur('state')}
                  placeholder="State"
                  maxLength={50}
                />
                {addrFieldErr('state') && <span className="field-error">{addrFieldErr('state')}</span>}
              </div>

              <div className="form-group">
                <label className="form-label">Pincode * (6 digits)</label>
                <input
                  className={addrInputClass('pincode')}
                  value={addressForm.pincode}
                  onChange={e => handleAddressChange('pincode', e.target.value.replace(/\D/g, '').slice(0, 6))}
                  onBlur={() => handleAddressBlur('pincode')}
                  placeholder="123456"
                  maxLength={6}
                  type="tel"
                />
                {addrFieldErr('pincode') && <span className="field-error">{addrFieldErr('pincode')}</span>}
              </div>

              <div className="modal-actions">
                <button className="btn btn-secondary" onClick={() => { setShowAddressForm(false); setAddressErrors({}); setAddressTouched({}); }}>
                  Cancel
                </button>
                <button className="btn btn-primary" onClick={handleAddAddress} disabled={loading}>
                  {loading ? 'Adding...' : 'Add Address'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
