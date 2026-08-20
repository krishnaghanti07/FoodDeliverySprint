import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ShoppingCart, Trash2, Plus, Minus, Tag, ArrowRight, AlertCircle } from 'lucide-react';
import { useCart } from '../../context/CartContext';
import { useAuth } from '../../context/AuthContext';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import { isRestaurantOpen } from '../../utils/timeUtils';
import toast from 'react-hot-toast';
import { CartItemSkeleton } from '../../components/common/Skeleton';
import './CartPage.css';

export default function CartPage() {
  const navigate = useNavigate();
  const { cart, cartLoading, fetchCart, updateCartItem, removeCartItem, clearCart, applyCoupon, removeCoupon } = useCart();
  const { isAuthenticated } = useAuth();
  const [couponCode, setCouponCode] = useState('');
  const [applyingCoupon, setApplyingCoupon] = useState(false);
  const [restaurantStatus, setRestaurantStatus] = useState({ isOpen: true, loading: true });

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    fetchCart();
  }, [isAuthenticated, navigate, fetchCart]);

  // Check restaurant open/closed status when cart loads
  useEffect(() => {
    const checkRestaurantStatus = async () => {
      if (!cart || !cart.restaurantId) {
        setRestaurantStatus({ isOpen: true, loading: false });
        return;
      }

      try {
        // Fetch restaurant details and operating hours
        const [restaurantRes, hoursRes] = await Promise.all([
          api.get(API_ENDPOINTS.catalog.restaurantById(cart.restaurantId)),
          api.get(`${API_ENDPOINTS.catalog.operatingHours}?restaurantId=${cart.restaurantId}`)
        ]);

        const restaurant = restaurantRes.data?.data || restaurantRes.data;
        const hours = hoursRes.data?.data || hoursRes.data || [];

        // Check if restaurant is open
        const { isOpen, nextOpenTime } = isRestaurantOpen(hours, restaurant.isOpen);
        
        setRestaurantStatus({ 
          isOpen, 
          nextOpenTime, 
          restaurantName: restaurant.name,
          loading: false 
        });
      } catch (error) {
        console.error('Failed to check restaurant status:', error);
        // Fail open - allow checkout if we can't verify
        setRestaurantStatus({ isOpen: true, loading: false });
      }
    };

    if (!cartLoading && cart) {
      checkRestaurantStatus();
    }
  }, [cart, cartLoading]);

  const handleUpdateQuantity = async (itemId, currentQty, delta) => {
    const newQty = currentQty + delta;
    if (newQty < 1) return;
    try {
      await updateCartItem(itemId, { quantity: newQty });
    } catch (err) {
      console.error('Failed to update quantity:', err);
    }
  };

  const handleRemoveItem = async (itemId) => {
    try {
      await removeCartItem(itemId);
    } catch (err) {
      console.error('Failed to remove item:', err);
    }
  };

  const handleClearCart = async () => {
    if (!window.confirm('Are you sure you want to clear your cart?')) return;
    await clearCart();
  };

  const handleApplyCoupon = async () => {
    if (!couponCode.trim()) {
      toast.error('Please enter a coupon code');
      return;
    }
    setApplyingCoupon(true);
    try {
      await applyCoupon(couponCode.trim());
      setCouponCode('');
    } catch (err) {
      // Error already handled by context
    } finally {
      setApplyingCoupon(false);
    }
  };

  const handleRemoveCoupon = async () => {
    setApplyingCoupon(true);
    try {
      await removeCoupon();
    } catch (err) {
      // Error already handled by context
    } finally {
      setApplyingCoupon(false);
    }
  };

  const handleCheckout = () => {
    if (!restaurantStatus.isOpen) {
      toast.error('Restaurant is currently closed. Cannot proceed to checkout.');
      return;
    }
    navigate('/checkout');
  };

  if (cartLoading) {
    return (
      <div className="cart-page page-enter">
        <div className="container">
          {/* Header skeleton */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-xl)' }}>
            <div className="skeleton" style={{ height: '2rem', width: '10rem' }} />
            <div className="skeleton" style={{ height: '1.5rem', width: '5rem' }} />
          </div>
          <div className="cart-layout">
            {/* Cart items skeleton */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
              <div className="skeleton" style={{ height: '1.25rem', width: '8rem', marginBottom: '0.5rem' }} />
              {Array.from({ length: 3 }).map((_, i) => (
                <CartItemSkeleton key={i} />
              ))}
            </div>
            {/* Summary skeleton */}
            <div className="card" style={{ padding: 'var(--space-lg)', alignSelf: 'flex-start' }}>
              <div className="skeleton" style={{ height: '1.25rem', width: '7rem', marginBottom: 'var(--space-lg)' }} />
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                  <div className="skeleton" style={{ height: '0.875rem', width: '5rem' }} />
                  <div className="skeleton" style={{ height: '0.875rem', width: '4rem' }} />
                </div>
              ))}
              <div style={{ borderTop: '1px solid var(--outline-variant)', paddingTop: 'var(--space-md)', marginTop: 'var(--space-sm)' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 'var(--space-lg)' }}>
                  <div className="skeleton" style={{ height: '1.25rem', width: '4rem' }} />
                  <div className="skeleton" style={{ height: '1.25rem', width: '5rem' }} />
                </div>
              </div>
              <div className="skeleton" style={{ height: '3rem', width: '100%', borderRadius: 'var(--rounded-lg)' }} />
            </div>
          </div>
        </div>
      </div>
    );
  }

  const isEmpty = !cart || !cart.items || cart.items.length === 0;

  if (isEmpty) {
    return (
      <div className="cart-page page-enter">
        <div className="container">
          <div className="empty-state">
            <ShoppingCart size={64} className="empty-icon" />
            <h2 className="headline-lg">Your cart is empty</h2>
            <p className="body-lg text-muted">Add items from restaurants to get started</p>
            <button className="btn btn-primary" onClick={() => navigate('/restaurants')}>
              Browse Restaurants
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Group items by restaurant
  const itemsByRestaurant = {};
  cart.items.forEach((item) => {
    const restaurantId = item.restaurantId || cart.restaurantId;
    if (!itemsByRestaurant[restaurantId]) {
      itemsByRestaurant[restaurantId] = [];
    }
    itemsByRestaurant[restaurantId].push(item);
  });

  return (
    <div className="cart-page page-enter">
      <div className="container">
        <div className="cart-header">
          <h1 className="headline-xl">
            <ShoppingCart size={32} /> Your Cart
          </h1>
          <button className="btn btn-text" onClick={handleClearCart}>
            Clear Cart
          </button>
        </div>

        <div className="cart-layout">
          {/* Cart Items */}
          <div className="cart-items">
            {Object.entries(itemsByRestaurant).map(([restaurantId, items]) => (
              <div key={restaurantId} className="restaurant-section">
                <div className="cart-restaurant">
                  <h3 className="headline-md">Restaurant Items</h3>
                </div>

                {items.map((item) => (
                  <div key={item.id} className="cart-item">
                    <div className="cart-item-info">
                      {item.isVeg !== undefined && (
                        <span className={`badge ${item.isVeg ? 'badge-veg' : 'badge-nonveg'}`}>
                          {item.isVeg ? '🟢' : '🔴'}
                        </span>
                      )}
                      <h4 className="headline-sm">{item.name}</h4>
                      <p className="body-lg">₹{item.unitPrice}</p>
                    </div>

                    <div className="cart-item-actions">
                      <div className="quantity-control">
                        <button
                          className="btn btn-icon"
                          onClick={() => handleUpdateQuantity(item.id, item.quantity, -1)}
                          disabled={item.quantity <= 1}
                        >
                          <Minus size={16} />
                        </button>
                        <span className="quantity">{item.quantity}</span>
                        <button
                          className="btn btn-icon"
                          onClick={() => handleUpdateQuantity(item.id, item.quantity, 1)}
                        >
                          <Plus size={16} />
                        </button>
                      </div>

                      <p className="body-lg cart-item-total">₹{item.lineTotal}</p>

                      <button
                        className="btn btn-icon btn-danger"
                        onClick={() => handleRemoveItem(item.id)}
                      >
                        <Trash2 size={18} />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            ))}
          </div>

          {/* Cart Summary */}
          <div className="cart-summary">
            <h3 className="headline-md">Bill Details</h3>

            {/* Restaurant Closed Warning */}
            {!restaurantStatus.loading && !restaurantStatus.isOpen && (
              <div className="alert alert-error" style={{ 
                padding: 'var(--space-sm)', 
                marginBottom: 'var(--space-md)', 
                borderRadius: 'var(--rounded-lg)',
                backgroundColor: 'var(--error-container)',
                color: 'var(--on-error-container)',
                display: 'flex',
                alignItems: 'flex-start',
                gap: 'var(--space-xs)',
                fontSize: '0.875rem'
              }}>
                <AlertCircle size={18} style={{ flexShrink: 0, marginTop: '2px' }} />
                <div>
                  <strong>Restaurant Closed</strong>
                  <p style={{ margin: 0, marginTop: '4px' }}>
                    {restaurantStatus.restaurantName} is currently closed. 
                    {restaurantStatus.nextOpenTime && restaurantStatus.nextOpenTime !== 'when partner reopens' && (
                      <span> Opens {restaurantStatus.nextOpenTime}.</span>
                    )}
                  </p>
                </div>
              </div>
            )}

            <div className="summary-row">
              <span>Subtotal</span>
              <span>₹{cart.subtotal?.toFixed(2) || '0.00'}</span>
            </div>

            {cart.discount > 0 && (
              <div className="summary-row discount">
                <span>Discount</span>
                <span>-₹{cart.discount?.toFixed(2)}</span>
              </div>
            )}

            <div className="summary-row total">
              <span className="headline-sm">Total</span>
              <span className="headline-sm">₹{cart.total?.toFixed(2) || '0.00'}</span>
            </div>

            {/* Coupon Section */}
            <div className="coupon-section">
              <h4 className="headline-sm">
                <Tag size={18} /> Apply Coupon
              </h4>
              {cart.couponCode ? (
                <div className="applied-coupon">
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
                    <span className="badge badge-success">{cart.couponCode}</span>
                    <span className="body-sm text-success">Coupon applied!</span>
                  </div>
                  <button
                    className="btn btn-text btn-sm"
                    onClick={handleRemoveCoupon}
                    disabled={applyingCoupon}
                    style={{ padding: '4px 8px', fontSize: '14px' }}
                  >
                    Remove Coupon
                  </button>
                </div>
              ) : (
                <div className="coupon-input">
                  <input
                    type="text"
                    placeholder="Enter coupon code"
                    value={couponCode}
                    onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
                    onKeyPress={(e) => e.key === 'Enter' && handleApplyCoupon()}
                  />
                  <button
                    className="btn btn-secondary btn-sm"
                    onClick={handleApplyCoupon}
                    disabled={applyingCoupon || !couponCode.trim()}
                  >
                    Apply
                  </button>
                </div>
              )}
              <p className="body-sm text-muted">Try: WELCOME10, FLAT50, SAVE20</p>
            </div>

            <button 
              className="btn btn-primary btn-lg checkout-btn" 
              onClick={handleCheckout}
              disabled={!restaurantStatus.isOpen || restaurantStatus.loading}
              title={!restaurantStatus.isOpen ? 'Restaurant is currently closed' : 'Proceed to checkout'}
            >
              {restaurantStatus.loading ? 'Checking...' : (
                !restaurantStatus.isOpen ? 'Restaurant Closed' : (
                  <>Proceed to Checkout <ArrowRight size={20} /></>
                )
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
