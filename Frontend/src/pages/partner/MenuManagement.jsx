import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Edit2, Trash2, Eye, EyeOff, Search, Filter, X } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import toast from 'react-hot-toast';
import MenuItemModal from '../../components/partner/MenuItemModal';
import { MenuItemSkeleton } from '../../components/common/Skeleton';
import './MenuManagement.css';

export default function MenuManagement() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [restaurant, setRestaurant] = useState(null);
  const [menuItems, setMenuItems] = useState([]);
  const [categories, setCategories] = useState([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState(null);
  const [showCategoryModal, setShowCategoryModal] = useState(false);
  const [newCategoryName, setNewCategoryName] = useState('');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      
      // Get partner's restaurants
      const restaurantsRes = await api.get(API_ENDPOINTS.catalog.restaurantsMyPartner);
      const restaurantData = restaurantsRes.data?.data || restaurantsRes.data;
      const restaurantsList = Array.isArray(restaurantData) ? restaurantData : [];
      const myRestaurant = restaurantsList[0];
      
      if (!myRestaurant) {
        toast.error('No restaurant found');
        navigate('/partner');
        return;
      }
      
      setRestaurant(myRestaurant);
      
      // Get menu items
      const menuRes = await api.get(`${API_ENDPOINTS.catalog.menuItems}?restaurantId=${myRestaurant.id}`);
      const menuData = menuRes.data?.data || menuRes.data;
      const items = Array.isArray(menuData) ? menuData : [];
      
      console.log('[MenuManagement] Menu items from API:', items);
      if (items.length > 0) {
        console.log('[MenuManagement] First item structure:', JSON.stringify(items[0], null, 2));
      }
      
      setMenuItems(items);
      
      // Get categories
      const categoriesRes = await api.get(`${API_ENDPOINTS.catalog.categories}?restaurantId=${myRestaurant.id}`);
      const categoriesData = categoriesRes.data?.data || categoriesRes.data;
      setCategories(Array.isArray(categoriesData) ? categoriesData : []);
      
    } catch (error) {
      console.error('Failed to load data:', error);
      toast.error('Failed to load menu data');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleAvailability = async (itemId) => {
    try {
      await api.patch(API_ENDPOINTS.catalog.menuItemToggle(itemId));
      toast.success('Item availability updated');
      loadData();
    } catch (error) {
      console.error('Failed to toggle availability:', error);
      toast.error('Failed to update availability');
    }
  };

  const handleDelete = async (itemId) => {
    if (!confirm('Are you sure you want to delete this item?')) return;
    
    try {
      await api.delete(API_ENDPOINTS.catalog.menuItemById(itemId));
      toast.success('Item deleted successfully');
      loadData();
    } catch (error) {
      console.error('Failed to delete item:', error);
      toast.error('Failed to delete item');
    }
  };

  const handleEdit = (item) => {
    setEditingItem(item);
    setShowModal(true);
  };

  const handleAdd = () => {
    setEditingItem(null);
    setShowModal(true);
  };

  const handleModalClose = (shouldReload) => {
    setShowModal(false);
    setEditingItem(null);
    if (shouldReload) {
      loadData();
    }
  };

  const handleCreateCategory = async () => {
    if (!newCategoryName.trim()) {
      toast.error('Please enter a category name');
      return;
    }

    try {
      await api.post(API_ENDPOINTS.catalog.categories, {
        name: newCategoryName.trim(),
        restaurantId: restaurant.id,
        displayOrder: categories.length + 1
      });
      toast.success('Category created successfully');
      setNewCategoryName('');
      setShowCategoryModal(false);
      loadData();
    } catch (error) {
      console.error('Failed to create category:', error);
      toast.error('Failed to create category');
    }
  };

  const filteredItems = menuItems.filter(item => {
    const matchesSearch = item.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         item.description?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCategory = selectedCategory === 'all' || item.categoryId === selectedCategory;
    return matchesSearch && matchesCategory;
  });

  if (loading) {
    return (
      <div className="menu-management page-enter">
        <div className="container">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-xl)' }}>
            <div className="skeleton" style={{ height: '1.75rem', width: '10rem' }} />
            <div className="skeleton" style={{ height: '2.5rem', width: '9rem', borderRadius: 'var(--rounded-lg)' }} />
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
            {Array.from({ length: 5 }).map((_, i) => <MenuItemSkeleton key={i} />)}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="menu-management page-enter">
      <div className="container">
        <div className="page-header">
          <div>
            <h1 className="headline-lg">Menu Management</h1>
            <p className="body-md text-muted">{restaurant?.name}</p>
          </div>
          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <button className="btn btn-outline" onClick={() => setShowCategoryModal(true)}>
              <Plus size={18} /> Add Category
            </button>
            <button className="btn btn-primary" onClick={handleAdd} disabled={categories.length === 0}>
              <Plus size={18} /> Add Menu Item
            </button>
          </div>
        </div>

        {categories.length === 0 ? (
          <div className="empty-state" style={{ padding: '3rem', textAlign: 'center', background: 'var(--surface-container-lowest)', borderRadius: 'var(--rounded-lg)', marginTop: '2rem' }}>
            <h3 style={{ marginBottom: '0.5rem' }}>No Categories Yet</h3>
            <p style={{ color: 'var(--on-surface-variant)', marginBottom: '1.5rem' }}>
              Create categories first before adding menu items. Categories help organize your menu (e.g., Appetizers, Main Course, Desserts).
            </p>
            <button className="btn btn-primary" onClick={() => setShowCategoryModal(true)}>
              <Plus size={18} /> Create Your First Category
            </button>
          </div>
        ) : (
          <>
            <div className="mm-filters-bar">
              {/* Search */}
              <div className="mm-search-wrap">
                <Search size={16} className="mm-search-icon" aria-hidden="true" />
                <input
                  type="search"
                  placeholder="Search menu items..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="mm-search-input"
                  aria-label="Search menu items"
                  autoComplete="off"
                />
                {searchTerm && (
                  <button className="mm-search-clear" onClick={() => setSearchTerm('')} aria-label="Clear">
                    <X size={13} />
                  </button>
                )}
              </div>

              {/* Category filter chips */}
              <div className="mm-cat-chips">
                <button
                  className={`rp-chip ${selectedCategory === 'all' ? 'active' : ''}`}
                  onClick={() => setSelectedCategory('all')}
                >
                  All Categories
                </button>
                {categories.map(cat => (
                  <button
                    key={cat.id}
                    className={`rp-chip ${selectedCategory === cat.id ? 'active' : ''}`}
                    onClick={() => setSelectedCategory(cat.id)}
                  >
                    {cat.name}
                  </button>
                ))}
              </div>
            </div>

        <div className="menu-grid">
          {filteredItems.length === 0 ? (
            <div className="mm-empty">
              <span style={{ fontSize: 48 }}>🍽️</span>
              <p className="body-lg text-muted">No menu items found</p>
              <button className="btn btn-primary" onClick={handleAdd}>
                <Plus size={18} /> Add Your First Item
              </button>
            </div>
          ) : (
            filteredItems.map(item => (
              <div key={item.id} className={`mm-card ${!item.isAvailable ? 'mm-card-unavailable' : ''}`}>

                {/* ── Image section ── */}
                <div className="mm-image-wrap">
                  {item.imageUrl && !item.imageUrl.startsWith('data:') ? (
                    <img src={item.imageUrl} alt={item.name} className="mm-image" />
                  ) : (
                    <div className="mm-image mm-image-placeholder">
                      <span>🍽️</span>
                    </div>
                  )}

                  {/* Gradient overlay */}
                  <div className="mm-image-overlay" />

                  {/* Availability badge — top left */}
                  <span className={`mm-avail-badge ${item.isAvailable ? 'available' : 'unavailable'}`}>
                    {item.isAvailable ? '● Available' : '● Unavailable'}
                  </span>

                  {/* Best seller ribbon — top right */}
                  {item.isBestSeller && (
                    <span className="mm-bestseller-badge">⭐ Best Seller</span>
                  )}
                </div>

                {/* ── Content section ── */}
                <div className="mm-content">

                  {/* Name row with veg/non-veg dot */}
                  <div className="mm-name-row">
                    {item.isVeg !== undefined && (
                      <div className={`mm-diet-dot ${item.isVeg ? 'veg' : 'nonveg'}`}>
                        <div className="mm-diet-inner" />
                      </div>
                    )}
                    <h3 className="mm-name">{item.name}</h3>
                  </div>

                  {/* Description */}
                  {item.description && (
                    <p className="mm-desc">{item.description}</p>
                  )}

                  {/* Category tag */}
                  {item.categoryName && (
                    <span className="mm-category-tag">{item.categoryName}</span>
                  )}

                  {/* Price + actions row */}
                  <div className="mm-footer">
                    <span className="mm-price">₹{item.price.toFixed(2)}</span>

                    <div className="mm-actions">
                      {/* Toggle availability */}
                      <button
                        className={`mm-action-btn ${item.isAvailable ? 'mm-btn-toggle-on' : 'mm-btn-toggle-off'}`}
                        onClick={() => handleToggleAvailability(item.id)}
                        title={item.isAvailable ? 'Mark Unavailable' : 'Mark Available'}
                        aria-label={item.isAvailable ? 'Mark as unavailable' : 'Mark as available'}
                      >
                        {item.isAvailable ? <Eye size={15} /> : <EyeOff size={15} />}
                      </button>

                      {/* Edit */}
                      <button
                        className="mm-action-btn mm-btn-edit"
                        onClick={() => handleEdit(item)}
                        title="Edit item"
                        aria-label="Edit menu item"
                      >
                        <Edit2 size={15} />
                      </button>

                      {/* Delete */}
                      <button
                        className="mm-action-btn mm-btn-delete"
                        onClick={() => handleDelete(item.id)}
                        title="Delete item"
                        aria-label="Delete menu item"
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
        </>
        )}
      </div>

      {showModal && (
        <MenuItemModal
          item={editingItem}
          restaurantId={restaurant?.id}
          categories={categories}
          onClose={handleModalClose}
        />
      )}

      {showCategoryModal && (
        <div className="modal-overlay" onClick={() => setShowCategoryModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: '400px' }}>
            <div className="modal-header">
              <h2 className="headline-md">Create Category</h2>
              <button className="btn btn-ghost btn-sm" onClick={() => setShowCategoryModal(false)}>
                <X size={20} />
              </button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label className="form-label">Category Name *</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="e.g., Appetizers, Main Course, Desserts"
                  value={newCategoryName}
                  onChange={(e) => setNewCategoryName(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && handleCreateCategory()}
                  autoFocus
                />
              </div>
            </div>
            <div className="modal-actions" style={{ padding: '1rem 1.5rem', borderTop: '1px solid var(--outline-variant)', display: 'flex', gap: '0.75rem', justifyContent: 'flex-end' }}>
              <button className="btn btn-outline" onClick={() => setShowCategoryModal(false)}>
                Cancel
              </button>
              <button className="btn btn-primary" onClick={handleCreateCategory}>
                <Plus size={18} /> Create Category
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
