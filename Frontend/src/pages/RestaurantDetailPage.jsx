import { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Star, Clock, MapPin, Plus, Minus, Search, X, ChevronDown, ShoppingCart, Filter } from 'lucide-react';
import api from '../services/api';
import { API_ENDPOINTS } from '../config/api';
import { useCart } from '../context/CartContext';
import { useAuth } from '../context/AuthContext';
import { isRestaurantOpen } from '../utils/timeUtils';
import toast from 'react-hot-toast';
import { MenuItemSkeleton } from '../components/common/Skeleton';
import { getCuisineImage, isValidImageUrl } from '../utils/cuisineImages';
import './RestaurantDetailPage.css';

// Format TimeSpan "HH:mm:ss" → "9:00 AM"
function formatTimeSpan(timeSpan) {
  if (!timeSpan) return '';
  const parts = timeSpan.split(':');
  if (parts.length < 2) return timeSpan;
  const hours = parseInt(parts[0]);
  const minutes = parts[1];
  const ampm = hours >= 12 ? 'PM' : 'AM';
  const displayHours = hours % 12 || 12;
  return `${displayHours}:${minutes} ${ampm}`;
}

export default function RestaurantDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { cart, addToCart, updateCartItem, removeCartItem, fetchCart } = useCart();
  const { isAuthenticated } = useAuth();

  const [restaurant, setRestaurant] = useState(null);
  const [menuItems, setMenuItems] = useState([]);
  const [categories, setCategories] = useState([]);
  const [operatingHours, setOperatingHours] = useState([]);
  const [loading, setLoading] = useState(true);
  const [hoursOpen, setHoursOpen] = useState(false);

  // ── Filter state ──────────────────────────────────────────────────
  const [activeCategory, setActiveCategory] = useState('all');
  const [filterVeg, setFilterVeg] = useState('all');
  const [menuSearch, setMenuSearch] = useState('');
  const [priceMin, setPriceMin] = useState('');
  const [priceMax, setPriceMax] = useState('');
  const [filterDrawerOpen, setFilterDrawerOpen] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [restRes, menuRes, catsRes] = await Promise.all([
          api.get(API_ENDPOINTS.catalog.restaurantById(id)),
          api.get(API_ENDPOINTS.catalog.menuItems, { params: { restaurantId: id } }),
          api.get(`${API_ENDPOINTS.catalog.categories}?restaurantId=${id}`).catch(() => ({ data: { data: [] } })),
        ]);

        const restaurantData = restRes.data?.data || restRes.data;
        setRestaurant(restaurantData);

        const menuData = menuRes.data?.data || menuRes.data;
        const items = Array.isArray(menuData) ? menuData : menuData?.items || [];
        setMenuItems(items);

        // Real categories from the categories endpoint
        const catsData = catsRes.data?.data || catsRes.data || [];
        const catsList = Array.isArray(catsData) ? catsData : [];
        setCategories(catsList.sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0)));

        // Fetch operating hours
        try {
          const hoursRes = await api.get(`${API_ENDPOINTS.catalog.operatingHours}?restaurantId=${id}`);
          const hoursData = hoursRes.data?.data || hoursRes.data || [];
          setOperatingHours(Array.isArray(hoursData) ? hoursData : []);
        } catch {
          // Operating hours optional
        }
      } catch (err) {
        console.error('Failed to load restaurant:', err);
        toast.error('Failed to load restaurant');
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [id]);

  useEffect(() => {
    if (isAuthenticated) fetchCart();
  }, [isAuthenticated, fetchCart]);

  // ── Cart helpers ──────────────────────────────────────────────────
  const getCartItem = (menuItemId) => cart?.items?.find(i => i.menuItemId === menuItemId) || null;

  const handleAddToCart = async (item) => {
    if (!isAuthenticated) { toast.error('Please login to add items'); return; }
    try {
      await addToCart({ MenuItemId: item.id, RestaurantId: id, ItemName: item.name, UnitPrice: item.price, Quantity: 1, IsVeg: item.isVeg || false });
    } catch {}
  };

  const handleUpdateQuantity = async (cartItemId, currentQty, delta) => {
    const newQty = currentQty + delta;
    if (newQty < 1) return;
    try { await updateCartItem(cartItemId, { quantity: newQty }); } catch {}
  };

  const handleRemoveFromCart = async (cartItemId) => {
    try { await removeCartItem(cartItemId); } catch {}
  };

  // ── Filter logic ──────────────────────────────────────────────────
  const hasMenuFilters = filterVeg !== 'all' || priceMin !== '' || priceMax !== '' || menuSearch.trim() !== '';
  const activeFilterCount = (filterVeg !== 'all' ? 1 : 0) + (priceMin !== '' || priceMax !== '' ? 1 : 0);

  const filteredItems = useMemo(() => {
    return menuItems.filter(item => {
      if (activeCategory !== 'all' && item.categoryId !== activeCategory) return false;
      if (filterVeg === 'veg' && !item.isVeg) return false;
      if (filterVeg === 'nonveg' && item.isVeg) return false;
      if (priceMin !== '' && item.price < parseFloat(priceMin)) return false;
      if (priceMax !== '' && item.price > parseFloat(priceMax)) return false;
      if (menuSearch.trim()) {
        const q = menuSearch.toLowerCase();
        if (!item.name?.toLowerCase().includes(q) && !item.description?.toLowerCase().includes(q)) return false;
      }
      return true;
    });
  }, [menuItems, activeCategory, filterVeg, priceMin, priceMax, menuSearch]);

  const clearMenuFilters = () => {
    setFilterVeg('all');
    setPriceMin('');
    setPriceMax('');
    setMenuSearch('');
  };

  // ── Price range bounds from actual items ──────────────────────────
  const priceRange = useMemo(() => {
    if (!menuItems.length) return { min: 0, max: 1000 };
    const prices = menuItems.map(i => i.price);
    return { min: Math.floor(Math.min(...prices)), max: Math.ceil(Math.max(...prices)) };
  }, [menuItems]);

  // ── Category name lookup ──────────────────────────────────────────
  const getCategoryName = (categoryId) => {
    const cat = categories.find(c => c.id === categoryId);
    return cat?.name || 'Other';
  };

  if (loading) {
    return (
      <div className="rd-page page-enter">
        <div className="container">
          {/* Hero banner skeleton */}
          <div className="skeleton" style={{
            height: 240,
            borderRadius: 'var(--rounded-xl)',
            marginBottom: 'var(--space-lg)',
            background: 'linear-gradient(135deg, var(--surface-container-high) 0%, var(--surface-container-highest) 100%)',
          }} />
          {/* Restaurant info skeleton */}
          <div style={{ marginBottom: 'var(--space-xl)' }}>
            <div className="skeleton" style={{ height: '2rem', width: '40%', marginBottom: '0.75rem' }} />
            <div className="skeleton" style={{ height: '1rem', width: '30%', marginBottom: '0.75rem' }} />
            <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
              {[80, 70, 90, 120].map((w, i) => (
                <div key={i} className="skeleton" style={{ height: '1.5rem', width: w, borderRadius: 'var(--rounded-full)' }} />
              ))}
            </div>
          </div>
          {/* Category tabs skeleton */}
          <div style={{ display: 'flex', gap: '0.5rem', marginBottom: 'var(--space-lg)', overflowX: 'auto' }}>
            {[80, 100, 90, 110, 85].map((w, i) => (
              <div key={i} className="skeleton" style={{ height: '2.25rem', width: w, borderRadius: 'var(--rounded-full)', flexShrink: 0 }} />
            ))}
          </div>
          {/* Menu item skeletons */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
            {Array.from({ length: 5 }).map((_, i) => (
              <MenuItemSkeleton key={i} />
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (!restaurant) {
    return (
      <div className="rd-page page-enter">
        <div className="container" style={{ textAlign: 'center', padding: 'var(--space-2xl) 0' }}>
          <h2 className="headline-md">Restaurant not found</h2>
        </div>
      </div>
    );
  }

  const { isOpen, nextOpenTime } = isRestaurantOpen(operatingHours, restaurant.isOpen);

  return (
    <div className="rd-page page-enter">
      {/* ── Hero Banner ── */}
      <div className="rd-hero" style={{
        backgroundImage: (() => {
          const uploaded = restaurant.logoUrl || restaurant.imageUrl;
          const img = isValidImageUrl(uploaded)
            ? uploaded
            : getCuisineImage(restaurant.cuisineType || restaurant.cuisine, restaurant.id, true);
          return `linear-gradient(to bottom, rgba(0,0,0,0.08), rgba(38,24,20,0.88)), url(${img})`;
        })(),
      }}>
        <div className="container rd-hero-content">
          {/* Status pill */}
          <div className={`rd-status-pill ${isOpen ? 'open' : 'closed'}`}>
            <span className="rd-status-dot" />
            {isOpen ? 'Open Now' : 'Closed'}
          </div>

          <h1 className="rd-hero-title">{restaurant.name}</h1>
          <p className="rd-hero-cuisine">{restaurant.cuisineType || restaurant.cuisine || 'Multi Cuisine'}</p>

          <div className="rd-meta-pills">
            <span className="rd-meta-pill">
              <Star size={14} fill="var(--gold)" color="var(--gold)" />
              {restaurant.rating > 0 ? restaurant.rating.toFixed(1) : 'New'}
            </span>
            {restaurant.deliveryTime && (
              <span className="rd-meta-pill">
                <Clock size={14} /> {restaurant.deliveryTime} min
              </span>
            )}
            {restaurant.address && (
              <span className="rd-meta-pill rd-meta-pill-addr">
                <MapPin size={14} />
                <span className="rd-addr-text">{restaurant.address}</span>
              </span>
            )}
          </div>

          {!isOpen && nextOpenTime && (
            <p className="rd-next-open">
              {nextOpenTime === 'when partner reopens'
                ? 'Temporarily closed by restaurant'
                : `Opens ${nextOpenTime}`}
            </p>
          )}
        </div>
      </div>

      {/* ── Operating Hours toggle strip ── */}
      {operatingHours.length > 0 && (
        <>
          <div className="rd-info-strip">
            <div className="container">
              <button
                className="rd-hours-toggle"
                onClick={() => setHoursOpen(v => !v)}
                aria-expanded={hoursOpen}
              >
                <Clock size={15} />
                Operating Hours
                <ChevronDown size={14} className={`rd-hours-chevron ${hoursOpen ? 'open' : ''}`} />
              </button>
            </div>
          </div>
          {hoursOpen && (
            <div className="rd-hours-panel">
              <div className="container">
                <div className="hours-grid">
                  {operatingHours.map((hour) => (
                    <div key={hour.id} className="hour-row">
                      <span className="hour-day">{hour.dayName}</span>
                      <span className="hour-time">
                        {hour.isClosed
                          ? <span className="hour-closed">Closed</span>
                          : `${formatTimeSpan(hour.openTime)} – ${formatTimeSpan(hour.closeTime)}`}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </>
      )}

      {/* ── Closed warning ── */}
      {!isOpen && (
        <div className="container">
          <div className="rd-closed-banner">
            <Clock size={20} />
            <div>
              <strong>Restaurant is currently closed</strong>
              <p>
                {nextOpenTime === 'when partner reopens'
                  ? 'The restaurant is temporarily closed. Please check back later.'
                  : `Opens ${nextOpenTime}`}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* ── Sticky Category Tab Bar ── */}
      {categories.length > 0 && (
        <div className="rd-cat-bar-wrap">
          <div className="container">
            <div className="rd-cat-bar" role="tablist" aria-label="Menu categories">
              <button
                className={`rd-cat-tab ${activeCategory === 'all' ? 'active' : ''}`}
                onClick={() => setActiveCategory('all')}
                role="tab"
                aria-selected={activeCategory === 'all'}
              >
                All
                <span className="rd-cat-count">{menuItems.length}</span>
              </button>
              {categories.map((cat) => {
                const count = menuItems.filter(i => i.categoryId === cat.id).length;
                if (count === 0) return null;
                return (
                  <button
                    key={cat.id}
                    className={`rd-cat-tab ${activeCategory === cat.id ? 'active' : ''}`}
                    onClick={() => setActiveCategory(cat.id)}
                    role="tab"
                    aria-selected={activeCategory === cat.id}
                  >
                    {cat.name}
                    <span className="rd-cat-count">{count}</span>
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      )}

      {/* ── Menu Section ── */}
      <div className="container rd-menu-section">

        {/* ── Menu Toolbar — matches RestaurantsPage filter bar ── */}
        <div className="rd-menu-toolbar">

          {/* Search */}
          <div className="rd-menu-search-wrap">
            <Search size={16} className="rd-menu-search-icon" aria-hidden="true" />
            <input
              type="search"
              className="rd-menu-search"
              placeholder="Search menu items..."
              value={menuSearch}
              onChange={e => setMenuSearch(e.target.value)}
              aria-label="Search menu"
              autoComplete="off"
            />
            {menuSearch && (
              <button
                className="rd-menu-search-clear"
                onClick={() => setMenuSearch('')}
                aria-label="Clear search"
              >
                <X size={13} />
              </button>
            )}
          </div>

          {/* Filter button — same style as RestaurantsPage */}
          <button
            className={`rp-filter-btn ${filterDrawerOpen ? 'active' : ''} ${activeFilterCount > 0 ? 'has-filters' : ''}`}
            onClick={() => setFilterDrawerOpen(v => !v)}
            aria-expanded={filterDrawerOpen}
          >
            <Filter size={15} />
            Filters
            {activeFilterCount > 0 && (
              <span className="rp-filter-count">{activeFilterCount}</span>
            )}
          </button>

          {/* Diet quick chips — same style as RestaurantsPage */}
          <div className="rp-chips-row rd-diet-chips">
            <button
              className={`rp-chip ${filterVeg === 'veg' ? 'active' : ''}`}
              onClick={() => setFilterVeg(v => v === 'veg' ? 'all' : 'veg')}
            >
              <span className="rp-chip-dot open" />
              Veg Only
            </button>
            <button
              className={`rp-chip ${filterVeg === 'nonveg' ? 'active' : ''}`}
              onClick={() => setFilterVeg(v => v === 'nonveg' ? 'all' : 'nonveg')}
            >
              <span className="rp-chip-dot closed" />
              Non-Veg
            </button>
            {hasMenuFilters && (
              <button className="rp-chip rp-chip-clear" onClick={clearMenuFilters}>
                <X size={12} /> Clear
              </button>
            )}
          </div>

          {/* Results count */}
          {hasMenuFilters && (
            <div className="rd-filter-meta">
              <span className="rd-filter-count">
                {filteredItems.length} of {menuItems.length} items
              </span>
            </div>
          )}
        </div>

        {/* ── Expanded Filter Panel — same style as RestaurantsPage ── */}
        {filterDrawerOpen && (
          <div className="rp-filter-panel" style={{ marginBottom: 'var(--space-lg)' }}>
            <div className="rp-filter-panel-content">
              {/* Diet */}
              <div className="rp-filter-group">
                <span className="rp-filter-label">Diet</span>
                <div className="rp-filter-options">
                  {[
                    { label: 'All', value: 'all' },
                    { label: '🟢 Veg Only', value: 'veg' },
                    { label: '🔴 Non-Veg', value: 'nonveg' },
                  ].map(opt => (
                    <button
                      key={opt.value}
                      className={`rp-filter-opt ${filterVeg === opt.value ? 'active' : ''}`}
                      onClick={() => setFilterVeg(opt.value)}
                    >
                      {opt.label}
                    </button>
                  ))}
                </div>
              </div>

              {/* Price Range */}
              <div className="rp-filter-group">
                <span className="rp-filter-label">Price Range</span>
                <div className="rd-price-range">
                  <div className="rd-price-input-wrap">
                    <span className="rd-price-prefix">₹</span>
                    <input
                      type="number"
                      className="rd-price-input"
                      placeholder={`Min (${priceRange.min})`}
                      value={priceMin}
                      onChange={e => setPriceMin(e.target.value)}
                      min={0}
                    />
                  </div>
                  <span className="rd-price-sep">—</span>
                  <div className="rd-price-input-wrap">
                    <span className="rd-price-prefix">₹</span>
                    <input
                      type="number"
                      className="rd-price-input"
                      placeholder={`Max (${priceRange.max})`}
                      value={priceMax}
                      onChange={e => setPriceMax(e.target.value)}
                      min={0}
                    />
                  </div>
                </div>
              </div>

              {hasMenuFilters && (
                <button className="btn btn-ghost btn-sm rp-clear-all" onClick={clearMenuFilters}>
                  <X size={14} /> Clear All Filters
                </button>
              )}
            </div>
          </div>
        )}

        {/* ── Menu Items ── */}
        <div className="rd-menu-list">
          {filteredItems.length === 0 ? (
            <div className="rd-menu-empty">
              <span className="rd-menu-empty-icon">🍽️</span>
              <p className="body-lg">No items match your filters</p>
              {hasMenuFilters && (
                <button className="btn btn-outline btn-sm" onClick={clearMenuFilters}>
                  Clear Filters
                </button>
              )}
            </div>
          ) : (
            filteredItems.map((item) => {
              const cartItem = getCartItem(item.id);
              const isInCart = !!cartItem;
              const categoryName = getCategoryName(item.categoryId);
              const unavailable = !item.isAvailable && item.isAvailable !== undefined;

              return (
                <div
                  key={item.id}
                  className={`menu-item-card ${unavailable ? 'mic-unavailable' : ''}`}
                  id={`menu-item-${item.id}`}
                >
                  <div className="mi-info">
                    {/* Badges row */}
                    <div className="mi-badges">
                      {item.isVeg !== undefined && (
                        <div className={`mi-diet-dot ${item.isVeg ? 'veg' : 'nonveg'}`}>
                          <div className="mi-diet-inner" />
                        </div>
                      )}
                      {item.isBestSeller && (
                        <span className="mi-tag mi-tag-bestseller">⭐ Best Seller</span>
                      )}
                      {categoryName && categoryName !== 'Other' && (
                        <span className="mi-tag mi-tag-category">{categoryName}</span>
                      )}
                      {unavailable && (
                        <span className="mi-tag" style={{ background: 'var(--error-container)', color: 'var(--error)', border: '1px solid var(--error)' }}>
                          Unavailable
                        </span>
                      )}
                    </div>

                    <h3 className="mi-name">{item.name}</h3>
                    <p className="mi-price">₹{item.price}</p>
                    {item.description && (
                      <p className="mi-desc">{item.description}</p>
                    )}
                  </div>

                  <div className="mi-action">
                    {item.imageUrl && isValidImageUrl(item.imageUrl) && (
                      <div className="mi-image" style={{ backgroundImage: `url(${item.imageUrl})` }} />
                    )}

                    <div className="mi-action-ctrl">
                      {isInCart ? (
                        /* ── Quantity control ── */
                        <div className="quantity-control">
                          <button
                            className="qty-btn"
                            onClick={() => handleRemoveFromCart(cartItem.id)}
                            disabled={!isOpen}
                            aria-label="Remove item"
                          >
                            <Minus size={15} />
                          </button>
                          <span className="qty-display">{cartItem.quantity}</span>
                          <button
                            className="qty-btn"
                            onClick={() => handleUpdateQuantity(cartItem.id, cartItem.quantity, 1)}
                            disabled={!isOpen}
                            aria-label="Add one more"
                          >
                            <Plus size={15} />
                          </button>
                        </div>
                      ) : (
                        /* ── ADD button ── */
                        <button
                          className={`mi-add-btn ${(!isOpen || unavailable) ? 'mi-add-btn-disabled' : ''}`}
                          onClick={() => handleAddToCart(item)}
                          disabled={!isOpen || unavailable}
                          id={`add-${item.id}`}
                          aria-label={`Add ${item.name} to cart`}
                        >
                          <Plus size={15} />
                          {!isOpen ? 'Closed' : 'ADD'}
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>

      {/* ── Floating Cart Bar ── */}
      {cart?.items?.length > 0 && (
        <div className="rd-cart-bar">
          <div className="rd-cart-bar-inner container">
            <div className="rd-cart-bar-left">
              <ShoppingCart size={20} />
              <span className="rd-cart-bar-count">{cart.items.length} item{cart.items.length !== 1 ? 's' : ''}</span>
              <span className="rd-cart-bar-sep">·</span>
              <span className="rd-cart-bar-total">₹{cart.total?.toFixed(2) || '0.00'}</span>
            </div>
            <button
              className="btn rd-cart-bar-btn"
              onClick={() => navigate('/cart')}
            >
              View Cart →
            </button>
          </div>
        </div>
      )}

      {/* ── Mobile Filter Drawer ── */}
      <div
        className={`rp-drawer-overlay ${filterDrawerOpen ? 'open' : ''}`}
        onClick={() => setFilterDrawerOpen(false)}
        aria-hidden="true"
      />
      <div className={`rp-filter-drawer ${filterDrawerOpen ? 'open' : ''}`}>
        <div className="rp-drawer-header">
          <h3 className="headline-sm">Filter Menu</h3>
          <button className="rp-drawer-close" onClick={() => setFilterDrawerOpen(false)} aria-label="Close">
            <X size={20} />
          </button>
        </div>
        <div className="rp-drawer-body">
          <div className="rp-filter-panel-content">
            <div className="rp-filter-group">
              <span className="rp-filter-label">Diet</span>
              <div className="rp-filter-options">
                {[
                  { label: 'All', value: 'all' },
                  { label: '🟢 Veg Only', value: 'veg' },
                  { label: '🔴 Non-Veg', value: 'nonveg' },
                ].map(opt => (
                  <button
                    key={opt.value}
                    className={`rp-filter-opt ${filterVeg === opt.value ? 'active' : ''}`}
                    onClick={() => setFilterVeg(opt.value)}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>
            <div className="rp-filter-group">
              <span className="rp-filter-label">Price Range</span>
              <div className="rd-price-range">
                <div className="rd-price-input-wrap">
                  <span className="rd-price-prefix">₹</span>
                  <input type="number" className="rd-price-input" placeholder={`Min (${priceRange.min})`} value={priceMin} onChange={e => setPriceMin(e.target.value)} min={0} />
                </div>
                <span className="rd-price-sep">—</span>
                <div className="rd-price-input-wrap">
                  <span className="rd-price-prefix">₹</span>
                  <input type="number" className="rd-price-input" placeholder={`Max (${priceRange.max})`} value={priceMax} onChange={e => setPriceMax(e.target.value)} min={0} />
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="rp-drawer-footer">
          <button className="btn btn-primary" onClick={() => setFilterDrawerOpen(false)}>
            Show {filteredItems.length} Items
          </button>
        </div>
      </div>
    </div>
  );
}
