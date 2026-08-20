import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import {
  ArrowLeft, Package, MapPin, Clock, CreditCard, Star,
  XCircle, CheckCircle, Truck, Ban, RefreshCw, Phone,
  User, RotateCcw, ChevronRight, AlertCircle, Receipt,
} from 'lucide-react';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import { useAuth } from '../../context/AuthContext';
import toast from 'react-hot-toast';
import CancelOrderModal from '../../components/customer/CancelOrderModal';
import './OrderDetailPage.css';

// ── Status pipeline ──────────────────────────────────────────────────
const STATUS_STEPS = [
  {
    keys: ['Pending', 'PaymentPending', 'Paid', 'AwaitingAcceptance'],
    label: 'Order Placed',
    sublabel: 'We received your order',
    icon: Receipt,
    color: 'primary',
  },
  {
    keys: ['Accepted'],
    label: 'Accepted',
    sublabel: 'Restaurant confirmed',
    icon: CheckCircle,
    color: 'secondary',
  },
  {
    keys: ['Preparing'],
    label: 'Preparing',
    sublabel: 'Chef is cooking',
    icon: Clock,
    color: 'warning',
  },
  {
    keys: ['ReadyForPickup'],
    label: 'Ready',
    sublabel: 'Waiting for pickup',
    icon: Package,
    color: 'tertiary',
  },
  {
    keys: ['PickedUp', 'OutForDelivery'],
    label: 'On the Way',
    sublabel: 'Agent is heading to you',
    icon: Truck,
    color: 'secondary',
  },
  {
    keys: ['Delivered'],
    label: 'Delivered',
    sublabel: 'Enjoy your meal!',
    icon: CheckCircle,
    color: 'success',
  },
];

// ── ETA Countdown ────────────────────────────────────────────────────
function ETACountdown({ estimatedTime }) {
  const [remaining, setRemaining] = useState('');

  useEffect(() => {
    if (!estimatedTime) return;
    const update = () => {
      const diff = new Date(estimatedTime) - new Date();
      if (diff <= 0) { setRemaining('Any moment now'); return; }
      const mins = Math.floor(diff / 60000);
      const secs = Math.floor((diff % 60000) / 1000);
      setRemaining(mins > 0 ? `${mins} min ${secs}s` : `${secs}s`);
    };
    update();
    const t = setInterval(update, 1000);
    return () => clearInterval(t);
  }, [estimatedTime]);

  if (!remaining) return null;
  return (
    <div className="eta-countdown">
      <Clock size={14} />
      <span>ETA: <strong>{remaining}</strong></span>
    </div>
  );
}

// ── Star Rating Input ────────────────────────────────────────────────
function StarRatingInput({ value, onChange, size = 32 }) {
  const [hovered, setHovered] = useState(0);
  return (
    <div className="star-rating-input" role="group" aria-label="Rating">
      {Array.from({ length: 5 }).map((_, i) => {
        const filled = i < (hovered || value);
        return (
          <button
            key={i}
            type="button"
            className={`star-btn ${filled ? 'filled' : ''}`}
            onClick={() => onChange(i + 1)}
            onMouseEnter={() => setHovered(i + 1)}
            onMouseLeave={() => setHovered(0)}
            aria-label={`${i + 1} star${i !== 0 ? 's' : ''}`}
          >
            <Star
              size={size}
              fill={filled ? 'var(--gold)' : 'none'}
              color={filled ? 'var(--gold)' : 'var(--outline-variant)'}
            />
          </button>
        );
      })}
    </div>
  );
}

// ── Main Component ───────────────────────────────────────────────────
export default function OrderDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();

  const [order, setOrder] = useState(null);
  const [delivery, setDelivery] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showRatingModal, setShowRatingModal] = useState(false);
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [canCancel, setCanCancel] = useState(false);
  const [rating, setRating] = useState(5);
  const [review, setReview] = useState('');
  const [submittingRating, setSubmittingRating] = useState(false);

  const isPartner = user?.role === 'Partner';
  const backPath = isPartner ? '/partner/orders' : '/orders';

  const fetchOrderDetails = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    else setRefreshing(true);
    try {
      const res = await api.get(API_ENDPOINTS.orders.orderById(id));
      const orderData = res.data?.data || res.data;
      setOrder(orderData);

      if (!isPartner) {
        const cancellableStatuses = ['Paid', 'AwaitingAcceptance', 'PaymentPending'];
        setCanCancel(cancellableStatuses.includes(orderData.status));
      } else {
        setCanCancel(false);
      }

      if (['PickedUp', 'OutForDelivery', 'Delivered'].includes(orderData.status)) {
        try {
          const deliveryRes = await api.get(API_ENDPOINTS.deliveries.track(id));
          setDelivery(deliveryRes.data?.data || deliveryRes.data);
        } catch {
          // delivery tracking optional
        }
      }
    } catch (err) {
      console.error('Failed to load order:', err);
      if (!silent) toast.error('Failed to load order details');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [id, isPartner]);

  useEffect(() => {
    if (!isAuthenticated) { navigate('/login'); return; }
    fetchOrderDetails();
  }, [id, isAuthenticated, navigate, fetchOrderDetails]);

  // Auto-refresh every 30s for active orders
  useEffect(() => {
    if (!order) return;
    const active = !['Delivered', 'Cancelled', 'Failed', 'RestaurantRejected'].includes(order.status);
    if (!active) return;
    const t = setInterval(() => fetchOrderDetails(true), 30000);
    return () => clearInterval(t);
  }, [order, fetchOrderDetails]);

  const handleSubmitRating = async () => {
    setSubmittingRating(true);
    try {
      await api.post(API_ENDPOINTS.orders.orderRating(id), {
        foodRating: rating,
        deliveryRating: rating,
        comment: review.trim() || null,
      });
      toast.success('Thanks for your feedback!');
      setShowRatingModal(false);
      fetchOrderDetails(true);
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to submit rating');
    } finally {
      setSubmittingRating(false);
    }
  };

  const getCurrentStepIndex = () => {
    if (!order) return -1;
    if (['Cancelled', 'Failed', 'RestaurantRejected'].includes(order.status)) return -1;
    return STATUS_STEPS.findIndex(s => s.keys.includes(order.status));
  };

  const currentStepIndex = getCurrentStepIndex();

  // ── Status badge helper ──────────────────────────────────────────
  const getStatusBadge = (status) => {
    const map = {
      Delivered: 'badge-success',
      Cancelled: 'badge-error',
      Failed: 'badge-error',
      RestaurantRejected: 'badge-error',
      PaymentFailed: 'badge-error',
      Preparing: 'badge-primary',
      OutForDelivery: 'badge-secondary',
      PickedUp: 'badge-secondary',
      Accepted: 'badge-info',
      AwaitingAcceptance: 'badge-warning',
      Paid: 'badge-info',
      PaymentPending: 'badge-warning',
    };
    return map[status] || 'badge-secondary';
  };

  const getStatusLabel = (status) => {
    const map = {
      PaymentPending: 'Payment Pending',
      AwaitingAcceptance: 'Awaiting Acceptance',
      ReadyForPickup: 'Ready for Pickup',
      OutForDelivery: 'Out for Delivery',
      RestaurantRejected: 'Rejected',
      PaymentFailed: 'Payment Failed',
    };
    return map[status] || status;
  };

  // ── Loading skeleton ─────────────────────────────────────────────
  if (loading) {
    return (
      <div className="odp-page page-enter">
        <div className="container">
          <div className="skeleton" style={{ height: '1.5rem', width: '9rem', marginBottom: 'var(--space-lg)' }} />
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 'var(--space-xl)' }}>
            <div>
              <div className="skeleton" style={{ height: '2rem', width: '14rem', marginBottom: '0.5rem' }} />
              <div className="skeleton" style={{ height: '1rem', width: '10rem' }} />
            </div>
            <div className="skeleton" style={{ height: '1.75rem', width: '7rem', borderRadius: 'var(--rounded-full)' }} />
          </div>
          {/* Timeline skeleton */}
          <div className="card" style={{ padding: 'var(--space-xl)', marginBottom: 'var(--space-xl)' }}>
            <div className="skeleton" style={{ height: '1.25rem', width: '8rem', marginBottom: 'var(--space-xl)' }} />
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: '0.5rem' }}>
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.5rem' }}>
                  <div className="skeleton skeleton-circle" style={{ height: '3rem', width: '3rem' }} />
                  <div className="skeleton" style={{ height: '0.75rem', width: '80%' }} />
                  <div className="skeleton" style={{ height: '0.625rem', width: '60%' }} />
                </div>
              ))}
            </div>
          </div>
          <div className="odp-layout">
            <div className="card" style={{ padding: 'var(--space-xl)' }}>
              <div className="skeleton" style={{ height: '1.25rem', width: '8rem', marginBottom: 'var(--space-lg)' }} />
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '0.75rem 0', borderBottom: '1px solid var(--outline-variant)' }}>
                  <div className="skeleton" style={{ height: '1rem', width: '55%' }} />
                  <div className="skeleton" style={{ height: '1rem', width: '4rem' }} />
                </div>
              ))}
            </div>
            <div className="card" style={{ padding: 'var(--space-xl)' }}>
              <div className="skeleton" style={{ height: '1.25rem', width: '8rem', marginBottom: 'var(--space-lg)' }} />
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                  <div className="skeleton" style={{ height: '0.875rem', width: '5rem' }} />
                  <div className="skeleton" style={{ height: '0.875rem', width: '4rem' }} />
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!order) {
    return (
      <div className="odp-page page-enter">
        <div className="container" style={{ textAlign: 'center', padding: 'var(--space-2xl) 0' }}>
          <Package size={48} style={{ color: 'var(--outline)', margin: '0 auto var(--space-md)' }} />
          <h2 className="headline-md">Order not found</h2>
          <button className="btn btn-outline" style={{ marginTop: 'var(--space-lg)' }} onClick={() => navigate(backPath)}>
            Back to Orders
          </button>
        </div>
      </div>
    );
  }

  const isCancelled = ['Cancelled', 'Failed', 'RestaurantRejected', 'PaymentFailed'].includes(order.status);
  const isDelivered = order.status === 'Delivered';
  const isActive = !isCancelled && !isDelivered;
  const canRate = isDelivered && !order.rating && !isPartner;

  return (
    <div className="odp-page page-enter">
      <div className="container">

        {/* ── Top bar ── */}
        <div className="odp-topbar">
          <button className="odp-back-btn" onClick={() => navigate(backPath)}>
            <ArrowLeft size={18} />
            <span>Back to Orders</span>
          </button>

          <div className="odp-topbar-right">
            {/* Refresh button for active orders */}
            {isActive && (
              <button
                className={`odp-refresh-btn ${refreshing ? 'spinning' : ''}`}
                onClick={() => fetchOrderDetails(true)}
                aria-label="Refresh order status"
                title="Refresh"
              >
                <RefreshCw size={16} />
              </button>
            )}

            {canCancel && !isPartner && (
              <button
                className="btn btn-danger btn-sm odp-cancel-btn"
                onClick={() => setShowCancelModal(true)}
              >
                <Ban size={16} /> Cancel Order
              </button>
            )}
          </div>
        </div>

        {/* ── Order header ── */}
        <div className="odp-header">
          <div className="odp-header-info">
            <h1 className="odp-restaurant-name">{order.restaurantName}</h1>
            <p className="odp-order-id">
              Order <span>#{order.id.substring(0, 8).toUpperCase()}</span>
              <span className="odp-order-date">
                · {new Date(order.createdAt || order.placedAt).toLocaleDateString('en-IN', {
                  day: '2-digit', month: 'short', year: 'numeric',
                  hour: '2-digit', minute: '2-digit',
                })}
              </span>
            </p>
          </div>
          <span className={`badge ${getStatusBadge(order.status)} odp-status-badge`}>
            {getStatusLabel(order.status)}
          </span>
        </div>

        {/* ── Alert banners ── */}
        {order.status === 'Cancelled' && order.cancellationReason && (
          <div className="odp-alert odp-alert-warning">
            <XCircle size={20} />
            <div>
              <strong>Order Cancelled</strong>
              <p>Reason: {order.cancellationReason}</p>
              {order.payment?.status === 'Success' && (
                <p className="odp-alert-note">A refund request has been created. You'll be notified once processed.</p>
              )}
            </div>
          </div>
        )}
        {order.status === 'RestaurantRejected' && order.rejectionReason && (
          <div className="odp-alert odp-alert-error">
            <XCircle size={20} />
            <div>
              <strong>Order Rejected by Restaurant</strong>
              <p>Reason: {order.rejectionReason}</p>
              {order.payment?.status === 'Success' && (
                <p className="odp-alert-note">A refund has been initiated and will be processed within 5–7 business days.</p>
              )}
            </div>
          </div>
        )}

        {/* ══════════════════════════════════════════════
            STATUS TIMELINE — horizontal stepper
        ══════════════════════════════════════════════ */}
        {!isCancelled && (
          <div className="odp-timeline-card">
            <div className="odp-timeline-header">
              <h2 className="odp-section-title">Order Status</h2>
              {isActive && order.estimatedDeliveryTime && (
                <ETACountdown estimatedTime={order.estimatedDeliveryTime} />
              )}
            </div>

            <div className="odp-stepper" role="list" aria-label="Order progress">
              {STATUS_STEPS.map((step, idx) => {
                const Icon = step.icon;
                const isCompleted = idx < currentStepIndex;
                const isCurrent = idx === currentStepIndex;
                const isPending = idx > currentStepIndex;

                return (
                  <div
                    key={step.keys[0]}
                    className={`odp-step ${isCompleted ? 'completed' : ''} ${isCurrent ? 'current' : ''} ${isPending ? 'pending' : ''}`}
                    role="listitem"
                    aria-current={isCurrent ? 'step' : undefined}
                  >
                    {/* Connector line before step */}
                    {idx > 0 && (
                      <div className={`odp-connector ${isCompleted || isCurrent ? 'filled' : ''}`} aria-hidden="true" />
                    )}

                    {/* Step circle */}
                    <div className={`odp-step-circle odp-step-circle-${step.color}`}>
                      {isCompleted ? (
                        <CheckCircle size={18} />
                      ) : (
                        <Icon size={18} />
                      )}
                      {isCurrent && <div className="odp-step-pulse" aria-hidden="true" />}
                    </div>

                    {/* Step label */}
                    <div className="odp-step-label">
                      <span className="odp-step-name">{step.label}</span>
                      <span className="odp-step-sub">{step.sublabel}</span>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Current step highlight bar */}
            {currentStepIndex >= 0 && (
              <div className="odp-current-step-bar">
                <div className={`odp-csb-dot odp-csb-dot-${STATUS_STEPS[currentStepIndex]?.color}`} />
                <span className="odp-csb-text">
                  {STATUS_STEPS[currentStepIndex]?.sublabel}
                </span>
                {isActive && (
                  <span className="odp-csb-live">
                    <span className="odp-live-dot" />
                    Live
                  </span>
                )}
              </div>
            )}
          </div>
        )}

        {/* ── Delivery Agent Card ── */}
        {delivery && (
          <div className="odp-agent-card">
            <div className="odp-agent-avatar">
              <Truck size={22} />
            </div>
            <div className="odp-agent-info">
              <span className="odp-agent-label">Delivery Agent</span>
              <span className="odp-agent-name">
                {delivery.agentName || `Agent #${delivery.deliveryAgentId?.substring(0, 6).toUpperCase()}`}
              </span>
              <span className="odp-agent-status">{delivery.status}</span>
            </div>
            {delivery.agentPhone && (
              <a href={`tel:${delivery.agentPhone}`} className="odp-agent-call">
                <Phone size={16} />
                Call
              </a>
            )}
          </div>
        )}

        {/* ── Main layout: items + summary ── */}
        <div className="odp-layout">

          {/* ── Order Items ── */}
          <div className="odp-items-card">
            <h2 className="odp-section-title">
              <Package size={18} />
              Order Items
              <span className="odp-items-count">{order.items?.length || 0} items</span>
            </h2>

            <div className="odp-items-list">
              {order.items?.map((item) => (
                <div key={item.id} className="odp-item">
                  <div className="odp-item-left">
                    {item.isVeg !== undefined && (
                      <div className={`odp-diet-dot ${item.isVeg ? 'veg' : 'nonveg'}`}>
                        <div className="odp-diet-inner" />
                      </div>
                    )}
                    <div className="odp-item-info">
                      <span className="odp-item-name">{item.name}</span>
                      <span className="odp-item-qty">× {item.quantity}</span>
                    </div>
                  </div>
                  <span className="odp-item-price">₹{item.lineTotal?.toFixed(2)}</span>
                </div>
              ))}
            </div>

            {/* Reorder button for delivered/cancelled */}
            {(isDelivered || isCancelled) && !isPartner && (
              <button
                className="btn btn-outline btn-sm odp-reorder-btn"
                onClick={async () => {
                  try {
                    await api.post(API_ENDPOINTS.orders.reorderOrder(id));
                    toast.success('Items added to cart!');
                    navigate('/cart');
                  } catch (err) {
                    toast.error(err.response?.data?.message || 'Failed to reorder');
                  }
                }}
              >
                <RotateCcw size={15} /> Reorder
              </button>
            )}
          </div>

          {/* ── Bill Summary ── */}
          <div className="odp-summary-card">
            <h2 className="odp-section-title">
              <Receipt size={18} />
              Bill Details
            </h2>

            <div className="odp-bill">
              <div className="odp-bill-row">
                <span>Item Total</span>
                <span>₹{order.subtotal?.toFixed(2)}</span>
              </div>
              <div className="odp-bill-row">
                <span>Delivery Fee</span>
                <span>₹{order.deliveryFee?.toFixed(2)}</span>
              </div>
              {order.gstAmount > 0 && (
                <div className="odp-bill-row">
                  <span>GST & Taxes</span>
                  <span>₹{order.gstAmount?.toFixed(2)}</span>
                </div>
              )}
              {order.platformFee > 0 && (
                <div className="odp-bill-row">
                  <span>Platform Fee</span>
                  <span>₹{order.platformFee?.toFixed(2)}</span>
                </div>
              )}
              {order.discount > 0 && (
                <div className="odp-bill-row odp-bill-discount">
                  <span>Discount</span>
                  <span>− ₹{order.discount?.toFixed(2)}</span>
                </div>
              )}
              <div className="odp-bill-total">
                <span>Total Paid</span>
                <span>₹{order.totalAmount?.toFixed(2)}</span>
              </div>
            </div>

            {/* Delivery address */}
            <div className="odp-meta-block">
              <div className="odp-meta-icon">
                <MapPin size={15} />
              </div>
              <div className="odp-meta-content">
                <span className="odp-meta-label">Delivery Address</span>
                <span className="odp-meta-value">{order.deliveryAddress}</span>
                {order.deliveryInstructions && (
                  <span className="odp-meta-note">Note: {order.deliveryInstructions}</span>
                )}
              </div>
            </div>

            {/* Payment method */}
            <div className="odp-meta-block">
              <div className="odp-meta-icon">
                <CreditCard size={15} />
              </div>
              <div className="odp-meta-content">
                <span className="odp-meta-label">Payment</span>
                <span className="odp-meta-value">{order.paymentMethod}</span>
              </div>
            </div>

            {/* Rate order CTA */}
            {canRate && (
              <button
                className="btn btn-primary odp-rate-btn"
                onClick={() => setShowRatingModal(true)}
              >
                <Star size={18} fill="currentColor" />
                Rate Your Order
              </button>
            )}

            {/* Existing rating display */}
            {order.rating && (
              <div className="odp-rating-display">
                <span className="odp-rating-label">
                  {isPartner ? 'Customer Rating' : 'Your Rating'}
                </span>
                <div className="odp-rating-stars">
                  {Array.from({ length: 5 }).map((_, i) => (
                    <Star
                      key={i}
                      size={18}
                      fill={i < (order.rating.foodRating || 0) ? 'var(--gold)' : 'none'}
                      color={i < (order.rating.foodRating || 0) ? 'var(--gold)' : 'var(--outline-variant)'}
                    />
                  ))}
                  <span className="odp-rating-value">{order.rating.foodRating?.toFixed(1)}</span>
                </div>
                {order.rating.comment && (
                  <p className="odp-rating-comment">"{order.rating.comment}"</p>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* ══════════════════════════════════════════════
          RATING MODAL
      ══════════════════════════════════════════════ */}
      {showRatingModal && (
        <div className="modal-overlay" onClick={() => setShowRatingModal(false)}>
          <div className="odp-rating-modal" onClick={e => e.stopPropagation()}>
            {/* Header */}
            <div className="odp-rm-header">
              <div className="odp-rm-icon">🍽️</div>
              <h2 className="odp-rm-title">How was your order?</h2>
              <p className="odp-rm-sub">from {order.restaurantName}</p>
            </div>

            {/* Stars */}
            <div className="odp-rm-stars-section">
              <StarRatingInput value={rating} onChange={setRating} size={36} />
              <p className="odp-rm-stars-label">
                {['', 'Poor', 'Fair', 'Good', 'Great', 'Excellent!'][rating]}
              </p>
            </div>

            {/* Review */}
            <div className="odp-rm-review">
              <label className="odp-rm-review-label">
                Tell us more <span>(optional)</span>
              </label>
              <textarea
                className="odp-rm-textarea"
                placeholder="What did you love? Any suggestions?"
                value={review}
                onChange={e => setReview(e.target.value)}
                rows={3}
                maxLength={500}
              />
              <span className="odp-rm-char-count">{review.length}/500</span>
            </div>

            {/* Actions */}
            <div className="odp-rm-actions">
              <button
                className="btn btn-ghost"
                onClick={() => setShowRatingModal(false)}
                disabled={submittingRating}
              >
                Skip
              </button>
              <button
                className="btn btn-primary odp-rm-submit"
                onClick={handleSubmitRating}
                disabled={submittingRating || rating === 0}
              >
                {submittingRating ? 'Submitting…' : 'Submit Rating'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Cancel Modal */}
      {showCancelModal && (
        <CancelOrderModal
          order={order}
          onClose={() => setShowCancelModal(false)}
          onSuccess={fetchOrderDetails}
        />
      )}
    </div>
  );
}
