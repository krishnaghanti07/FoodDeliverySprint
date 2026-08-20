import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Trash2, Plus, Minus, ShoppingBag, ArrowRight, Tag } from 'lucide-react';
import { useCart } from '../context/CartContext';
import toast from 'react-hot-toast';
import api from '../services/api';
import { API_ENDPOINTS } from '../config/api';
import './CartPage.css';

export default function CartPage() {
  const { cart, cartLoading, fetchCart, updateCartItem, removeCartItem, clearCart, applyCoupon } = useCart();
  const navigate = useNavigate();
  const [couponCode, setCouponCode] = useState('');
  const [checkingOut, setCheckingOut] = useState(false);

  useEffect(() => {
    fetchCart();
  }, [fetchCart]);

  const handleQuantityChange = async (item, delta) => {
    const newQty = (item.quantity || 1) + delta;
    if (newQty <= 0) {
      await removeCartItem(item.id || item.cartItemId);
    } else {
      await updateCartItem(item.id || item.cartItemId, { quantity: newQty });
    }
  };

  const handleApplyCoupon = async () => {
    if (!couponCode.trim()) return;
    try {
      await applyCoupon(couponCode.trim());
      setCouponCode('');
    } catch {}
  };

  const handleCheckout = async () => {
    setCheckingOut(true);
    try {
      const { data } = await api.post(API_ENDPOINTS.orders.orders, {
        addressId: null,
        paymentMethod: 'Online',
      });
      const orderId = data.id || data.orderId;
      toast.success('Order placed successfully!');
      navigate(`/orders/${orderId}`);
    } catch (err) {
      const msg = err.response?.data?.message || 'Checkout failed. Please select an address.';
      toast.error(msg);
    } finally {
      setCheckingOut(false);
    }
  };

  if (cartLoading) {
    return (
      <div className="cart-page page-enter container">
        <div style={{ display: 'flex', justifyContent: 'center', padding: 'var(--space-2xl) 0' }}>
          <div className="spinner" />
        </div>
      </div>
    );
  }

  if (!cart || !cart.items || cart.items.length === 0) {
    return (
      <div className="cart-page page-enter container">
        <div className="cart-empty">
          <span className="cart-empty-icon">🛒</span>
          <h2 className="headline-md">Your cart is empty</h2>
          <p className="body-md text-muted">Add items from your favourite restaurants</p>
          <Link to="/restaurants" className="btn btn-primary">
            <ShoppingBag size={18} /> Browse Restaurants
          </Link>
        </div>
      </div>
    );
  }

  const subtotal = cart.subtotal || cart.items.reduce((sum, i) => sum + (i.price * (i.quantity || 1)), 0);
  const discount = cart.discount || 0;
  const deliveryFee = cart.deliveryFee || 0;
  const tax = cart.tax || 0;
  const total = cart.total || (subtotal - discount + deliveryFee + tax);

  return (
    <div className="cart-page page-enter">
      <div className="container">
        <h1 className="headline-lg" style={{ marginBottom: 'var(--space-lg)' }}>Your Cart</h1>

        <div className="cart-layout">
          <div className="cart-items-section">
            {cart.restaurantName && (
              <p className="body-sm text-muted" style={{ marginBottom: 'var(--space-md)' }}>
                From <strong>{cart.restaurantName}</strong>
              </p>
            )}

            {cart.items.map((item) => (
              <div key={item.id || item.cartItemId} className="cart-item" id={`cart-item-${item.id || item.cartItemId}`}>
                <div className="ci-info">
                  {item.isVeg !== undefined && (
                    <span className={`badge badge-sm ${item.isVeg ? 'badge-veg' : 'badge-nonveg'}`} style={{ fontSize: 10 }}>
                      {item.isVeg ? 'VEG' : 'NON-VEG'}
                    </span>
                  )}
                  <h4 className="headline-sm">{item.name || item.menuItemName}</h4>
                  <p className="body-md">₹{item.price}</p>
                </div>
                <div className="ci-controls">
                  <div className="qty-control">
                    <button className="qty-btn" onClick={() => handleQuantityChange(item, -1)} aria-label="Decrease">
                      <Minus size={14} />
                    </button>
                    <span className="qty-value">{item.quantity || 1}</span>
                    <button className="qty-btn" onClick={() => handleQuantityChange(item, 1)} aria-label="Increase">
                      <Plus size={14} />
                    </button>
                  </div>
                  <span className="ci-total">₹{(item.price * (item.quantity || 1)).toFixed(2)}</span>
                  <button className="ci-remove" onClick={() => removeCartItem(item.id || item.cartItemId)} aria-label="Remove">
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>
            ))}

            <div className="cart-coupon">
              <Tag size={18} className="text-muted" />
              <input
                type="text"
                placeholder="Enter coupon code"
                value={couponCode}
                onChange={(e) => setCouponCode(e.target.value)}
                className="form-input"
                id="coupon-input"
              />
              <button className="btn btn-outline btn-sm" onClick={handleApplyCoupon}>Apply</button>
            </div>

            <button className="btn btn-ghost btn-sm" onClick={clearCart} style={{ color: 'var(--error)' }}>
              <Trash2 size={14} /> Clear Cart
            </button>
          </div>

          <div className="cart-summary">
            <div className="summary-card card">
              <div className="card-body">
                <h3 className="headline-sm" style={{ marginBottom: 'var(--space-md)' }}>Order Summary</h3>
                <div className="summary-row">
                  <span>Subtotal</span>
                  <span>₹{subtotal.toFixed(2)}</span>
                </div>
                {discount > 0 && (
                  <div className="summary-row discount">
                    <span>Discount</span>
                    <span>-₹{discount.toFixed(2)}</span>
                  </div>
                )}
                <div className="summary-row">
                  <span>Delivery Fee</span>
                  <span>{deliveryFee === 0 ? 'FREE' : `₹${deliveryFee.toFixed(2)}`}</span>
                </div>
                {tax > 0 && (
                  <div className="summary-row">
                    <span>Tax</span>
                    <span>₹{tax.toFixed(2)}</span>
                  </div>
                )}
                <div className="summary-divider" />
                <div className="summary-row total">
                  <span>Total</span>
                  <span>₹{total.toFixed(2)}</span>
                </div>

                <button
                  className="btn btn-primary btn-lg"
                  style={{ width: '100%', marginTop: 'var(--space-md)' }}
                  onClick={handleCheckout}
                  disabled={checkingOut}
                  id="checkout-btn"
                >
                  {checkingOut ? (
                    <div className="spinner" style={{ width: 20, height: 20, borderWidth: 2 }} />
                  ) : (
                    <>Place Order <ArrowRight size={18} /></>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
