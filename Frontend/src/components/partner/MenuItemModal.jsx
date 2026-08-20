import { useState, useEffect } from 'react';
import { X, Save } from 'lucide-react';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import toast from 'react-hot-toast';
import ImageUpload from '../common/ImageUpload';
import './MenuItemModal.css';

export default function MenuItemModal({ item, restaurantId, categories, onClose }) {
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    price: 0,
    categoryId: '',
    imageUrl: '',
    isVeg: false,
    isAvailable: true,
    restaurantId: restaurantId
  });

  useEffect(() => {
    if (item) {
      console.log('[MenuItemModal] Editing item - Full object:', JSON.stringify(item, null, 2));
      console.log('[MenuItemModal] categoryId:', item.categoryId);
      console.log('[MenuItemModal] category:', item.category);
      console.log('[MenuItemModal] isVeg:', item.isVeg);
      
      // Try to get categoryId from different possible field names
      const categoryIdValue = item.categoryId || item.category?.id || item.category || '';
      
      setFormData({
        name: item.name || '',
        description: item.description || '',
        price: item.price || 0,
        categoryId: categoryIdValue,
        imageUrl: item.imageUrl || '',
        isVeg: item.isVeg !== undefined ? item.isVeg : false,
        isAvailable: item.isAvailable !== undefined ? item.isAvailable : true,
        restaurantId: restaurantId
      });
      
      console.log('[MenuItemModal] Form data set - categoryId:', categoryIdValue);
    } else {
      // Reset form for new item
      setFormData({
        name: '',
        description: '',
        price: 0,
        categoryId: '',
        imageUrl: '',
        isVeg: false,
        isAvailable: true,
        restaurantId: restaurantId
      });
    }
  }, [item, restaurantId]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : (type === 'number' ? parseFloat(value) || 0 : value)
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!formData.name || !formData.price || !formData.categoryId) {
      toast.error('Please fill in all required fields');
      return;
    }

    try {
      setLoading(true);
      
      if (item) {
        await api.put(API_ENDPOINTS.catalog.menuItemById(item.id), formData);
        toast.success('Menu item updated successfully');
      } else {
        await api.post(API_ENDPOINTS.catalog.menuItems, formData);
        toast.success('Menu item added successfully');
      }
      
      onClose(true);
    } catch (error) {
      console.error('Failed to save menu item:', error);
      toast.error(error.response?.data?.message || 'Failed to save menu item');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={() => onClose(false)}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2 className="headline-md">{item ? 'Edit Menu Item' : 'Add Menu Item'}</h2>
          <button className="btn btn-ghost btn-sm" onClick={() => onClose(false)}>
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="modal-body">
          <div className="form-group">
            <label className="form-label">Item Name *</label>
            <input
              type="text"
              name="name"
              className="form-input"
              value={formData.name}
              onChange={handleChange}
              placeholder="Enter item name"
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label">Description</label>
            <textarea
              name="description"
              className="form-input"
              value={formData.description}
              onChange={handleChange}
              placeholder="Describe the item"
              rows="3"
            />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label className="form-label">Price (₹) *</label>
              <input
                type="number"
                name="price"
                className="form-input"
                value={formData.price}
                onChange={handleChange}
                min="0"
                step="0.01"
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label">Category *</label>
              <select
                name="categoryId"
                className="form-input form-select"
                value={formData.categoryId}
                onChange={handleChange}
                required
              >
                <option value="">Select category</option>
                {categories.map(cat => (
                  <option key={cat.id} value={cat.id}>{cat.name}</option>
                ))}
              </select>
            </div>
          </div>

          <ImageUpload
            type="menuItem"
            currentImageUrl={formData.imageUrl}
            onImageUploaded={(url) => setFormData(prev => ({...prev, imageUrl: url}))}
            label="Item Image"
          />

          <div className="form-checkboxes">
            <label className="checkbox-label">
              <input
                type="checkbox"
                name="isVeg"
                checked={formData.isVeg}
                onChange={handleChange}
              />
              <span>Vegetarian</span>
            </label>

            <label className="checkbox-label">
              <input
                type="checkbox"
                name="isAvailable"
                checked={formData.isAvailable}
                onChange={handleChange}
              />
              <span>Available</span>
            </label>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn btn-outline" onClick={() => onClose(false)}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              <Save size={18} /> {loading ? 'Saving...' : 'Save Item'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
