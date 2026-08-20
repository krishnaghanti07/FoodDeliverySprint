import { useState, useEffect } from 'react';
import { Plus, Edit2, Trash2, Tag } from 'lucide-react';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import toast from 'react-hot-toast';
import { validators } from '../../utils/validation';
import './CouponsManagement.css';

const EMPTY_FORM = {
  code: '',
  discountType: 'Percentage',
  discountValue: '',
  minimumOrderAmount: '',
  maxDiscountAmount: '',
  validFrom: '',
  validTo: '',
  usageLimit: '',
  isActive: true
};

export default function CouponsManagement() {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [restaurant, setRestaurant] = useState(null);
  const [coupons, setCoupons] = useState([]);
  const [showModal, setShowModal] = useState(false);
  const [editingCoupon, setEditingCoupon] = useState(null);
  const [formData, setFormData] = useState(EMPTY_FORM);
  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});

  useEffect(() => { loadData(); }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const restaurantsRes = await api.get(API_ENDPOINTS.catalog.restaurantsMyPartner);
      const restaurantData = restaurantsRes.data?.data || restaurantsRes.data;
      const myRestaurant = (Array.isArray(restaurantData) ? restaurantData : [])[0];
      if (myRestaurant) {
        setRestaurant(myRestaurant);
        const couponsRes = await api.get(API_ENDPOINTS.orders.myCoupons, { params: { restaurantId: myRestaurant.id } });
        const couponsData = couponsRes.data?.data || couponsRes.data;
        setCoupons(Array.isArray(couponsData) ? couponsData : []);
      }
    } catch (error) {
      console.error('Failed to load coupons:', error);
      toast.error('Failed to load coupons');
    } finally {
      setLoading(false);
    }
  };

  // ── Validation ──────────────────────────────────────────────────────
  const validateField = (name, value, data = formData) => {
    switch (name) {
      case 'code':
        return validators.couponCode(value);
      case 'discountValue': {
        if (value === '' || value === null || value === undefined) return 'Discount value is required';
        const num = parseFloat(value);
        if (isNaN(num) || num <= 0) return 'Discount value must be greater than 0';
        if (data.discountType === 'Percentage' && num > 100) return 'Percentage discount cannot exceed 100%';
        if (data.discountType === 'Fixed' && num > 10000) return 'Fixed discount cannot exceed ₹10,000';
        return null;
      }
      case 'minimumOrderAmount': {
        if (value === '' || value === null || value === undefined) return null; // optional
        const num = parseFloat(value);
        if (isNaN(num) || num < 0) return 'Minimum order must be 0 or more';
        if (num > 100000) return 'Minimum order cannot exceed ₹1,00,000';
        return null;
      }
      case 'maxDiscountAmount': {
        if (value === '' || value === null || value === undefined) return null; // optional
        const num = parseFloat(value);
        if (isNaN(num) || num < 0) return 'Max discount must be 0 or more';
        // Max discount should be less than discount value for percentage type
        if (data.discountType === 'Percentage' && parseFloat(data.discountValue) > 0 && num > 0) {
          // This is a cap, so it's fine
        }
        return null;
      }
      case 'validFrom': {
        if (!value) return 'Start date is required';
        // Allow editing existing coupons without past-date restriction
        if (!editingCoupon && new Date(value) < new Date(new Date().toDateString()))
          return 'Start date cannot be in the past';
        return null;
      }
      case 'validTo': {
        if (!value) return 'End date is required';
        if (data.validFrom && new Date(value) <= new Date(data.validFrom))
          return 'End date must be after start date';
        return null;
      }
      case 'usageLimit': {
        if (value === '' || value === null || value === undefined) return null; // optional
        const num = parseInt(value);
        if (isNaN(num) || num < 1) return 'Usage limit must be at least 1';
        if (num > 100000) return 'Usage limit cannot exceed 1,00,000';
        return null;
      }
      default:
        return null;
    }
  };

  const validateAll = () => {
    const fields = ['code', 'discountValue', 'minimumOrderAmount', 'maxDiscountAmount', 'validFrom', 'validTo', 'usageLimit'];
    const errs = {};
    fields.forEach(f => {
      const err = validateField(f, formData[f], formData);
      if (err) errs[f] = err;
    });
    setErrors(errs);
    setTouched(Object.fromEntries(fields.map(f => [f, true])));
    return Object.keys(errs).length === 0;
  };

  const handleChange = (name, value) => {
    const newData = { ...formData, [name]: value };
    setFormData(newData);
    if (touched[name]) {
      const err = validateField(name, value, newData);
      setErrors(prev => ({ ...prev, [name]: err || undefined }));
    }
    // Re-validate discountValue when type changes
    if (name === 'discountType' && touched.discountValue) {
      const err = validateField('discountValue', formData.discountValue, newData);
      setErrors(prev => ({ ...prev, discountValue: err || undefined }));
    }
    // Re-validate validTo when validFrom changes
    if (name === 'validFrom' && touched.validTo) {
      const err = validateField('validTo', formData.validTo, newData);
      setErrors(prev => ({ ...prev, validTo: err || undefined }));
    }
  };

  const handleBlur = (name) => {
    setTouched(prev => ({ ...prev, [name]: true }));
    const err = validateField(name, formData[name], formData);
    setErrors(prev => ({ ...prev, [name]: err || undefined }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateAll()) {
      toast.error('Please fix the errors before submitting');
      return;
    }

    setSaving(true);
    try {
      const discountValue = parseFloat(formData.discountValue);
      const minOrder = parseFloat(formData.minimumOrderAmount) || 0;
      const maxDiscount = formData.maxDiscountAmount !== '' ? parseFloat(formData.maxDiscountAmount) || null : null;
      const usageLimit = formData.usageLimit !== '' ? parseInt(formData.usageLimit) || 100 : 100;

      const payload = {
        code: formData.code.trim().toUpperCase(),
        description: `Get ${formData.discountType === 'Percentage' ? discountValue + '%' : '₹' + discountValue} off on orders above ₹${minOrder}`,
        type: formData.discountType === 'Fixed' ? 'FixedAmount' : 'Percentage',
        value: discountValue,
        minOrderAmount: minOrder,
        maxDiscountAmount: maxDiscount,
        usageLimit,
        validFrom: new Date(formData.validFrom).toISOString(),
        validUntil: new Date(formData.validTo).toISOString(),
        restaurantId: restaurant.id
      };

      if (editingCoupon) {
        await api.put(API_ENDPOINTS.orders.couponById(editingCoupon.id), {
          description: payload.description,
          value: payload.value,
          minOrderAmount: payload.minOrderAmount,
          maxDiscountAmount: payload.maxDiscountAmount,
          usageLimit: payload.usageLimit,
          validUntil: payload.validUntil,
          isActive: formData.isActive
        });
        toast.success('Coupon updated successfully');
      } else {
        await api.post(API_ENDPOINTS.orders.coupons, payload);
        toast.success('Coupon created successfully');
      }

      setShowModal(false);
      resetForm();
      loadData();
    } catch (error) {
      console.error('Failed to save coupon:', error);
      toast.error(error.response?.data?.message || 'Failed to save coupon');
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (coupon) => {
    setEditingCoupon(coupon);
    setFormData({
      code: coupon.code,
      discountType: coupon.type === 'FixedAmount' ? 'Fixed' : 'Percentage',
      discountValue: coupon.value,
      minimumOrderAmount: coupon.minOrderAmount ?? '',
      maxDiscountAmount: coupon.maxDiscountAmount ?? '',
      validFrom: coupon.validFrom?.split('T')[0] || '',
      validTo: coupon.validUntil?.split('T')[0] || '',
      usageLimit: coupon.usageLimit ?? '',
      isActive: coupon.isActive
    });
    setErrors({});
    setTouched({});
    setShowModal(true);
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Are you sure you want to delete this coupon?')) return;
    try {
      await api.delete(API_ENDPOINTS.orders.couponById(id));
      toast.success('Coupon deleted successfully');
      loadData();
    } catch (error) {
      toast.error('Failed to delete coupon');
    }
  };

  const resetForm = () => {
    setEditingCoupon(null);
    setFormData(EMPTY_FORM);
    setErrors({});
    setTouched({});
  };

  const openCreate = () => { resetForm(); setShowModal(true); };

  // Helper
  const fieldErr = (name) => touched[name] && errors[name] ? errors[name] : null;
  const inputClass = (name) => `form-input${fieldErr(name) ? ' input-error' : ''}`;

  if (loading) {
    return <div className="coupons-management page-enter"><div className="container"><div className="loading-spinner">Loading coupons...</div></div></div>;
  }

  return (
    <div className="coupons-management page-enter">
      <div className="container">
        <div className="page-header">
          <div>
            <h1 className="headline-lg">Coupons Management</h1>
            <p className="body-md text-muted">{restaurant?.name}</p>
          </div>
          <button className="btn btn-primary" onClick={openCreate}>
            <Plus size={18} /> Create Coupon
          </button>
        </div>

        <div className="coupons-grid">
          {coupons.length === 0 ? (
            <div className="empty-state">
              <Tag size={64} className="text-muted" />
              <h2 className="headline-lg">No Coupons Yet</h2>
              <p className="body-lg text-muted">Create your first coupon to attract customers</p>
              <button className="btn btn-primary" onClick={openCreate}>
                <Plus size={18} /> Create Coupon
              </button>
            </div>
          ) : (
            coupons.map(coupon => (
              <div key={coupon.id} className="coupon-card">
                <div className="coupon-header">
                  <div className="coupon-code"><Tag size={20} /><span>{coupon.code}</span></div>
                  <span className={`badge ${coupon.isActive ? 'badge-success' : 'badge-error'}`}>
                    {coupon.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
                <div className="coupon-details">
                  <div className="coupon-discount">
                    <span className="discount-value">
                      {coupon.type === 'Percentage' ? `${coupon.value}% OFF` : `₹${coupon.value} OFF`}
                    </span>
                  </div>
                  <div className="coupon-info">
                    <div className="info-row"><span className="label">Min Order:</span><span className="value">₹{coupon.minOrderAmount}</span></div>
                    {coupon.maxDiscountAmount > 0 && (
                      <div className="info-row"><span className="label">Max Discount:</span><span className="value">₹{coupon.maxDiscountAmount}</span></div>
                    )}
                    <div className="info-row"><span className="label">Usage:</span><span className="value">{coupon.usedCount || 0} / {coupon.usageLimit}</span></div>
                    <div className="info-row"><span className="label">Valid Till:</span><span className="value">{new Date(coupon.validUntil).toLocaleDateString('en-IN')}</span></div>
                  </div>
                </div>
                <div className="coupon-actions">
                  <button className="btn btn-ghost btn-sm" onClick={() => handleEdit(coupon)}><Edit2 size={16} /> Edit</button>
                  <button className="btn btn-ghost btn-sm" onClick={() => handleDelete(coupon.id)} style={{ color: 'var(--error)' }}><Trash2 size={16} /> Delete</button>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* ── Create / Edit Modal ── */}
      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal-content" onClick={e => e.stopPropagation()} style={{ maxHeight: '90vh', overflowY: 'auto' }}>
            <div className="modal-header">
              <h2 className="headline-md">{editingCoupon ? 'Edit Coupon' : 'Create Coupon'}</h2>
              <button className="btn btn-ghost btn-sm" onClick={() => setShowModal(false)}>×</button>
            </div>

            <form onSubmit={handleSubmit} className="modal-body" noValidate>

              {/* Coupon Code */}
              <div className="form-group">
                <label className="form-label">Coupon Code * <span style={{ color: 'var(--outline)', fontWeight: 400, fontSize: '12px' }}>(3-20 chars, letters/numbers/_/-)</span></label>
                <input
                  type="text"
                  className={inputClass('code')}
                  value={formData.code}
                  onChange={e => handleChange('code', e.target.value.toUpperCase().replace(/[^A-Z0-9_-]/g, ''))}
                  onBlur={() => handleBlur('code')}
                  placeholder="SAVE20"
                  maxLength={20}
                  disabled={!!editingCoupon}
                />
                {fieldErr('code') && <span className="field-error">{fieldErr('code')}</span>}
                {editingCoupon && <span style={{ fontSize: '11px', color: 'var(--outline)' }}>Coupon code cannot be changed after creation</span>}
              </div>

              {/* Discount Type + Value */}
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Discount Type *</label>
                  <select
                    className="form-input form-select"
                    value={formData.discountType}
                    onChange={e => handleChange('discountType', e.target.value)}
                  >
                    <option value="Percentage">Percentage (%)</option>
                    <option value="Fixed">Fixed Amount (₹)</option>
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">
                    Discount Value * {formData.discountType === 'Percentage' ? '(%)' : '(₹)'}
                  </label>
                  <input
                    type="number"
                    className={inputClass('discountValue')}
                    value={formData.discountValue}
                    onChange={e => handleChange('discountValue', e.target.value)}
                    onBlur={() => handleBlur('discountValue')}
                    min="0.01"
                    max={formData.discountType === 'Percentage' ? 100 : 10000}
                    step="0.01"
                    placeholder={formData.discountType === 'Percentage' ? 'e.g., 20' : 'e.g., 50'}
                  />
                  {fieldErr('discountValue') && <span className="field-error">{fieldErr('discountValue')}</span>}
                </div>
              </div>

              {/* Min Order + Max Discount */}
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Min Order Amount (₹) <span style={{ color: 'var(--outline)', fontWeight: 400 }}>(optional)</span></label>
                  <input
                    type="number"
                    className={inputClass('minimumOrderAmount')}
                    value={formData.minimumOrderAmount}
                    onChange={e => handleChange('minimumOrderAmount', e.target.value)}
                    onBlur={() => handleBlur('minimumOrderAmount')}
                    min="0"
                    step="0.01"
                    placeholder="0"
                  />
                  {fieldErr('minimumOrderAmount') && <span className="field-error">{fieldErr('minimumOrderAmount')}</span>}
                </div>

                <div className="form-group">
                  <label className="form-label">Max Discount (₹) <span style={{ color: 'var(--outline)', fontWeight: 400 }}>(optional)</span></label>
                  <input
                    type="number"
                    className={inputClass('maxDiscountAmount')}
                    value={formData.maxDiscountAmount}
                    onChange={e => handleChange('maxDiscountAmount', e.target.value)}
                    onBlur={() => handleBlur('maxDiscountAmount')}
                    min="0"
                    step="0.01"
                    placeholder="No cap"
                  />
                  {fieldErr('maxDiscountAmount') && <span className="field-error">{fieldErr('maxDiscountAmount')}</span>}
                </div>
              </div>

              {/* Valid From + Valid To */}
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Valid From *</label>
                  <input
                    type="date"
                    className={inputClass('validFrom')}
                    value={formData.validFrom}
                    onChange={e => handleChange('validFrom', e.target.value)}
                    onBlur={() => handleBlur('validFrom')}
                    min={!editingCoupon ? new Date().toISOString().split('T')[0] : undefined}
                  />
                  {fieldErr('validFrom') && <span className="field-error">{fieldErr('validFrom')}</span>}
                </div>

                <div className="form-group">
                  <label className="form-label">Valid To *</label>
                  <input
                    type="date"
                    className={inputClass('validTo')}
                    value={formData.validTo}
                    onChange={e => handleChange('validTo', e.target.value)}
                    onBlur={() => handleBlur('validTo')}
                    min={formData.validFrom || new Date().toISOString().split('T')[0]}
                  />
                  {fieldErr('validTo') && <span className="field-error">{fieldErr('validTo')}</span>}
                </div>
              </div>

              {/* Usage Limit */}
              <div className="form-group">
                <label className="form-label">Usage Limit <span style={{ color: 'var(--outline)', fontWeight: 400 }}>(optional, default: 100)</span></label>
                <input
                  type="number"
                  className={inputClass('usageLimit')}
                  value={formData.usageLimit}
                  onChange={e => handleChange('usageLimit', e.target.value)}
                  onBlur={() => handleBlur('usageLimit')}
                  min="1"
                  max="100000"
                  placeholder="100"
                />
                {fieldErr('usageLimit') && <span className="field-error">{fieldErr('usageLimit')}</span>}
              </div>

              {/* Active toggle */}
              <div className="form-group">
                <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={e => handleChange('isActive', e.target.checked)}
                    style={{ width: '18px', height: '18px' }}
                  />
                  <span className="form-label" style={{ margin: 0 }}>Active (visible to customers)</span>
                </label>
              </div>

              <div className="modal-actions">
                <button type="button" className="btn btn-outline" onClick={() => setShowModal(false)}>
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary" disabled={saving}>
                  {saving ? 'Saving...' : (editingCoupon ? 'Update Coupon' : 'Create Coupon')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
