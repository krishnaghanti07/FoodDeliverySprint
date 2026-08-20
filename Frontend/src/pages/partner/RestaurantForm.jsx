import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Store, ArrowLeft, Save, MapPin, Phone, Clock, DollarSign } from 'lucide-react';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import toast from 'react-hot-toast';
import ImageUpload from '../../components/common/ImageUpload';
import { validators } from '../../utils/validation';
import './RestaurantForm.css';

export default function RestaurantForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    cuisine: '',
    address: '',
    city: '',
    phone: '',
    logoUrl: '',
    deliveryFee: '',
    minOrderAmount: '',
    prepTimeMinutes: ''
  });
  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});

  useEffect(() => {
    if (id) loadRestaurant();
  }, [id]);

  const loadRestaurant = async () => {
    try {
      const res = await api.get(API_ENDPOINTS.catalog.restaurantById(id));
      const restaurant = res.data?.data;
      if (restaurant) {
        setFormData({
          name: restaurant.name || '',
          description: restaurant.description || '',
          cuisine: restaurant.cuisine || '',
          address: restaurant.address || '',
          city: restaurant.city || '',
          phone: restaurant.phone || '',
          logoUrl: restaurant.logoUrl || '',
          deliveryFee: restaurant.deliveryFee ?? 0,
          minOrderAmount: restaurant.minOrderAmount ?? 0,
          prepTimeMinutes: restaurant.prepTimeMinutes ?? 30
        });
      }
    } catch (error) {
      console.error('Failed to load restaurant:', error);
      toast.error('Failed to load restaurant details');
    }
  };

  // ── Validation rules ────────────────────────────────────────────────
  const validateField = (name, value) => {
    switch (name) {
      case 'name':
        return validators.restaurantName(value);
      case 'cuisine':
        return validators.cuisine(value);
      case 'address':
        if (!value || !value.trim()) return 'Address is required';
        if (value.trim().length < 5) return 'Please enter a complete address';
        return null;
      case 'city':
        if (!value || !value.trim()) return 'City is required';
        if (value.trim().length < 2) return 'City name must be at least 2 characters';
        return null;
      case 'phone':
        return validators.phone(value);
      case 'deliveryFee': {
        const num = parseFloat(value);
        if (value === '' || value === null) return null; // optional
        if (isNaN(num) || num < 0) return 'Delivery fee must be 0 or more';
        if (num > 500) return 'Delivery fee cannot exceed ₹500';
        return null;
      }
      case 'minOrderAmount': {
        const num = parseFloat(value);
        if (value === '' || value === null) return null; // optional
        if (isNaN(num) || num < 0) return 'Minimum order must be 0 or more';
        if (num > 10000) return 'Minimum order cannot exceed ₹10,000';
        return null;
      }
      case 'prepTimeMinutes': {
        const num = parseInt(value);
        if (value === '' || value === null) return null; // optional
        if (isNaN(num) || num < 1) return 'Prep time must be at least 1 minute';
        if (num > 180) return 'Prep time cannot exceed 180 minutes';
        return null;
      }
      default:
        return null;
    }
  };

  const validateAll = () => {
    const requiredFields = ['name', 'cuisine', 'address', 'city', 'phone'];
    const newErrors = {};
    requiredFields.forEach(field => {
      const err = validateField(field, formData[field]);
      if (err) newErrors[field] = err;
    });
    // Also validate optional numeric fields if they have values
    ['deliveryFee', 'minOrderAmount', 'prepTimeMinutes'].forEach(field => {
      const err = validateField(field, formData[field]);
      if (err) newErrors[field] = err;
    });
    setErrors(newErrors);
    setTouched(Object.fromEntries(Object.keys(newErrors).map(k => [k, true])));
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e) => {
    const { name, value, type } = e.target;
    const newValue = type === 'number' ? (value === '' ? '' : value) : value;
    setFormData(prev => ({ ...prev, [name]: newValue }));
    if (touched[name]) {
      const err = validateField(name, newValue);
      setErrors(prev => ({ ...prev, [name]: err || undefined }));
    }
  };

  const handleBlur = (e) => {
    const { name, value } = e.target;
    setTouched(prev => ({ ...prev, [name]: true }));
    const err = validateField(name, value);
    setErrors(prev => ({ ...prev, [name]: err || undefined }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateAll()) {
      toast.error('Please fix the errors before submitting');
      return;
    }

    try {
      setLoading(true);
      const payload = {
        ...formData,
        deliveryFee: formData.deliveryFee === '' ? 0 : parseFloat(formData.deliveryFee) || 0,
        minOrderAmount: formData.minOrderAmount === '' ? 0 : parseFloat(formData.minOrderAmount) || 0,
        prepTimeMinutes: formData.prepTimeMinutes === '' ? 30 : parseInt(formData.prepTimeMinutes) || 30,
      };

      if (id) {
        await api.put(API_ENDPOINTS.catalog.restaurantById(id), payload);
        toast.success('Restaurant updated successfully');
      } else {
        await api.post(API_ENDPOINTS.catalog.restaurants, payload);
        toast.success('Restaurant registered successfully! Awaiting admin approval.');
      }
      navigate('/partner');
    } catch (error) {
      console.error('Failed to save restaurant:', error);
      toast.error(error.response?.data?.message || 'Failed to save restaurant');
    } finally {
      setLoading(false);
    }
  };

  // Helper: show error only if field was touched
  const fieldError = (name) => touched[name] && errors[name] ? errors[name] : null;
  const inputClass = (name) => `form-input${fieldError(name) ? ' input-error' : ''}`;

  return (
    <div className="restaurant-form page-enter">
      <div className="container">
        <div className="form-header">
          <button className="btn btn-ghost" onClick={() => navigate('/partner')}>
            <ArrowLeft size={18} /> Back to Dashboard
          </button>
          <h1 className="headline-lg">
            <Store size={28} /> {id ? 'Edit Restaurant' : 'Register Restaurant'}
          </h1>
        </div>

        <form onSubmit={handleSubmit} className="restaurant-form-content" noValidate>

          {/* ── Basic Information ── */}
          <div className="card">
            <div className="card-body">
              <h2 className="headline-md">Basic Information</h2>

              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Restaurant Name *</label>
                  <input
                    type="text"
                    name="name"
                    className={inputClass('name')}
                    value={formData.name}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    placeholder="Enter restaurant name"
                    maxLength={100}
                  />
                  {fieldError('name') && <span className="field-error">{fieldError('name')}</span>}
                </div>

                <div className="form-group">
                  <label className="form-label">Cuisine Type *</label>
                  <input
                    type="text"
                    name="cuisine"
                    className={inputClass('cuisine')}
                    value={formData.cuisine}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    placeholder="e.g., Italian, Chinese, Indian"
                    maxLength={50}
                  />
                  {fieldError('cuisine') && <span className="field-error">{fieldError('cuisine')}</span>}
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Description <span style={{ color: 'var(--outline)', fontWeight: 400 }}>(optional)</span></label>
                <textarea
                  name="description"
                  className="form-input"
                  value={formData.description}
                  onChange={handleChange}
                  placeholder="Describe your restaurant, specialties, ambiance..."
                  rows="3"
                  maxLength={500}
                />
                <span style={{ fontSize: '11px', color: 'var(--outline)', textAlign: 'right', display: 'block', marginTop: '2px' }}>
                  {formData.description.length}/500
                </span>
              </div>

              <ImageUpload
                type="restaurant"
                currentImageUrl={formData.logoUrl}
                onImageUploaded={(url) => setFormData(prev => ({ ...prev, logoUrl: url }))}
                label="Restaurant Logo (optional)"
              />
            </div>
          </div>

          {/* ── Location Details ── */}
          <div className="card">
            <div className="card-body">
              <h2 className="headline-md"><MapPin size={20} /> Location Details</h2>

              <div className="form-group">
                <label className="form-label">Address *</label>
                <input
                  type="text"
                  name="address"
                  className={inputClass('address')}
                  value={formData.address}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  placeholder="Street address, building, landmark"
                  maxLength={200}
                />
                {fieldError('address') && <span className="field-error">{fieldError('address')}</span>}
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">City *</label>
                  <input
                    type="text"
                    name="city"
                    className={inputClass('city')}
                    value={formData.city}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    placeholder="City"
                    maxLength={50}
                  />
                  {fieldError('city') && <span className="field-error">{fieldError('city')}</span>}
                </div>
              </div>
            </div>
          </div>

          {/* ── Contact Information ── */}
          <div className="card">
            <div className="card-body">
              <h2 className="headline-md"><Phone size={20} /> Contact Information</h2>

              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Phone Number *</label>
                  <input
                    type="tel"
                    name="phone"
                    className={inputClass('phone')}
                    value={formData.phone}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    placeholder="+91 9876543210"
                    maxLength={15}
                  />
                  {fieldError('phone') && <span className="field-error">{fieldError('phone')}</span>}
                </div>
              </div>
            </div>
          </div>

          {/* ── Delivery Settings ── */}
          <div className="card">
            <div className="card-body">
              <h2 className="headline-md"><DollarSign size={20} /> Delivery Settings</h2>

              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Delivery Fee (₹) <span style={{ color: 'var(--outline)', fontWeight: 400 }}>(optional)</span></label>
                  <input
                    type="number"
                    name="deliveryFee"
                    className={inputClass('deliveryFee')}
                    value={formData.deliveryFee}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    min="0"
                    max="500"
                    step="0.01"
                    placeholder="0.00"
                  />
                  {fieldError('deliveryFee') && <span className="field-error">{fieldError('deliveryFee')}</span>}
                </div>

                <div className="form-group">
                  <label className="form-label">Minimum Order (₹) <span style={{ color: 'var(--outline)', fontWeight: 400 }}>(optional)</span></label>
                  <input
                    type="number"
                    name="minOrderAmount"
                    className={inputClass('minOrderAmount')}
                    value={formData.minOrderAmount}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    min="0"
                    max="10000"
                    step="0.01"
                    placeholder="0.00"
                  />
                  {fieldError('minOrderAmount') && <span className="field-error">{fieldError('minOrderAmount')}</span>}
                </div>

                <div className="form-group">
                  <label className="form-label"><Clock size={16} /> Prep Time (min) <span style={{ color: 'var(--outline)', fontWeight: 400 }}>(optional)</span></label>
                  <input
                    type="number"
                    name="prepTimeMinutes"
                    className={inputClass('prepTimeMinutes')}
                    value={formData.prepTimeMinutes}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    min="1"
                    max="180"
                    placeholder="30"
                  />
                  {fieldError('prepTimeMinutes') && <span className="field-error">{fieldError('prepTimeMinutes')}</span>}
                </div>
              </div>
            </div>
          </div>

          <div className="form-actions">
            <button type="button" className="btn btn-outline" onClick={() => navigate('/partner')}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              <Save size={18} /> {loading ? 'Saving...' : (id ? 'Update Restaurant' : 'Register Restaurant')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
