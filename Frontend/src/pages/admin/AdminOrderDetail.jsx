import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft, Package, User, MapPin, DollarSign, Clock,
  Store, Truck, CreditCard, AlertCircle
} from 'lucide-react';
import { apiService } from '../../services/api';
import toast from 'react-hot-toast';
import { Skeleton } from '../../components/common/Skeleton';

export default function AdminOrderDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [statusModal, setStatusModal] = useState(false);
  const [statusForm, setStatusForm] = useState({ newStatus: '', reason: '' });
  const [updating, setUpdating] = useState(false);

  useEffect(() => {
    fetchOrder();
  }, [id]);

  const fetchOrder = async () => {
    try {
      setLoading(true);
      const res = await apiService.admin.getOrderById(id);
      setOrder(res.data?.data || res.data);
    } catch (error) {
      console.error('Failed to fetch order:', error);
      toast.error('Failed to load order details');
    } finally {
      setLoading(false);
    }
  };

  const handleStatusUpdate = async (e) => {
    e.preventDefault();
    if (!statusForm.newStatus) return toast.error('Select a new status');

    const requiresReason = ['Cancelled', 'RefundInitiated', 'Refunded', 'CancelRequested'].includes(statusForm.newStatus);
    if (requiresReason && !statusForm.reason.trim()) {
      return toast.error('Reason is required for this status change');
    }

    try {
      setUpdating(true);
      await apiService.admin.updateOrderStatus(id, {
        newStatus: statusForm.newStatus,
        reason: statusForm.reason || ''   // always send reason, empty string if not needed
      });
      toast.success('Order status updated');
      setStatusModal(false);
      setStatusForm({ newStatus: '', reason: '' });
      fetchOrder();
    } catch (error) {
      const msg = error.response?.data?.message
        || error.response?.data?.errors?.[0]
        || 'Failed to update status';
      toast.error(msg);
    } finally {
      setUpdating(false);
    }
  };

  const getStatusColor = (status) => {
    const map = {
      PaymentPending: '#f59e0b', Paid: '#3b82f6', Accepted: '#8b5cf6',
      Preparing: '#f97316', ReadyForPickup: '#06b6d4', PickedUp: '#6366f1',
      OutForDelivery: '#0ea5e9', Delivered: '#22c55e', Cancelled: '#ef4444',
      Refunded: '#ec4899', PaymentFailed: '#dc2626', CancelRequested: '#f59e0b',
    };
    return map[status] || '#6b7280';
  };

  const formatDate = (d) => {
    if (!d) return 'N/A';
    const date = new Date(d);
    if (isNaN(date)) return 'N/A';
    return date.toLocaleString('en-IN', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  };

  const allStatuses = [
    'Paid', 'Accepted', 'Preparing', 'ReadyForPickup', 'PickedUp',
    'OutForDelivery', 'Delivered', 'CancelRequested', 'Cancelled',
    'RefundInitiated', 'Refunded', 'PaymentFailed'
  ];

  if (loading) {
    return (
      <div className="container" style={{ padding: '2rem 1rem' }}>
        <div className="skeleton" style={{ height: '1.5rem', width: '9rem', marginBottom: 'var(--space-lg)' }} />
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 'var(--space-xl)' }}>
          <div>
            <div className="skeleton" style={{ height: '2rem', width: '14rem', marginBottom: '0.5rem' }} />
            <div className="skeleton" style={{ height: '1rem', width: '10rem' }} />
          </div>
          <div className="skeleton" style={{ height: '1.75rem', width: '7rem', borderRadius: 'var(--rounded-full)' }} />
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 'var(--space-lg)' }}>
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="card" style={{ padding: 'var(--space-xl)' }}>
              <div className="skeleton" style={{ height: '1.25rem', width: '8rem', marginBottom: 'var(--space-lg)' }} />
              {Array.from({ length: 3 }).map((_, j) => (
                <div key={j} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                  <div className="skeleton" style={{ height: '0.875rem', width: '5rem' }} />
                  <div className="skeleton" style={{ height: '0.875rem', width: '7rem' }} />
                </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (!order) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem 1rem' }}>
        <AlertCircle size={64} style={{ color: 'var(--error)', margin: '0 auto 1rem' }} />
        <h2>Order Not Found</h2>
        <p style={{ color: 'var(--on-surface-variant)', marginBottom: '1.5rem' }}>
          The order you're looking for doesn't exist or couldn't be loaded.
        </p>
        <button className="btn btn-primary" onClick={() => navigate('/admin/orders')}>
          Back to Orders
        </button>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '900px', margin: '0 auto', padding: '1.5rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1.5rem' }}>
        <button
          onClick={() => navigate('/admin/orders')}
          style={{
            display: 'flex', alignItems: 'center', gap: '0.5rem',
            background: 'none', border: '1px solid var(--outline)', borderRadius: '8px',
            padding: '0.5rem 1rem', cursor: 'pointer', color: 'var(--on-surface)'
          }}
        >
          <ArrowLeft size={18} /> Back
        </button>
        <div>
          <h1 style={{ fontSize: '1.5rem', fontWeight: 700, margin: 0 }}>
            Order #{order.id?.substring(0, 8)}
          </h1>
          <p style={{ color: 'var(--on-surface-variant)', margin: 0, fontSize: '0.875rem' }}>
            {formatDate(order.placedAt || order.createdAt)}
          </p>
        </div>
        <span style={{
          marginLeft: 'auto', padding: '0.375rem 1rem', borderRadius: '999px',
          background: getStatusColor(order.status) + '20',
          color: getStatusColor(order.status), fontWeight: 600, fontSize: '0.875rem'
        }}>
          {order.status}
        </span>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        {/* Customer Info */}
        <div style={{ background: 'var(--surface-container-lowest)', borderRadius: '12px', padding: '1.25rem', border: '1px solid var(--outline-variant)' }}>
          <h3 style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem', fontSize: '1rem' }}>
            <User size={18} style={{ color: 'var(--primary)' }} /> Customer
          </h3>
          <p style={{ fontWeight: 600, marginBottom: '0.25rem' }}>{order.customerName || 'N/A'}</p>
          <p style={{ color: 'var(--on-surface-variant)', fontSize: '0.875rem' }}>{order.customerEmail || 'N/A'}</p>
          <p style={{ color: 'var(--on-surface-variant)', fontSize: '0.75rem', marginTop: '0.25rem' }}>
            ID: {order.customerId?.substring(0, 8)}
          </p>
        </div>

        {/* Restaurant Info */}
        <div style={{ background: 'var(--surface-container-lowest)', borderRadius: '12px', padding: '1.25rem', border: '1px solid var(--outline-variant)' }}>
          <h3 style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem', fontSize: '1rem' }}>
            <Store size={18} style={{ color: 'var(--secondary)' }} /> Restaurant
          </h3>
          <p style={{ fontWeight: 600, marginBottom: '0.25rem' }}>{order.restaurantName || 'N/A'}</p>
          <p style={{ color: 'var(--on-surface-variant)', fontSize: '0.75rem' }}>
            ID: {order.restaurantId?.substring(0, 8)}
          </p>
        </div>

        {/* Payment Info */}
        <div style={{ background: 'var(--surface-container-lowest)', borderRadius: '12px', padding: '1.25rem', border: '1px solid var(--outline-variant)' }}>
          <h3 style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem', fontSize: '1rem' }}>
            <CreditCard size={18} style={{ color: 'var(--tertiary)' }} /> Payment
          </h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--on-surface-variant)' }}>Method</span>
              <span style={{ fontWeight: 600 }}>{order.paymentMethod || 'N/A'}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--on-surface-variant)' }}>Total Amount</span>
              <span style={{ fontWeight: 700, fontSize: '1.1rem', color: 'var(--primary)' }}>
                ₹{order.totalAmount?.toFixed(2)}
              </span>
            </div>
          </div>
        </div>

        {/* Timeline */}
        <div style={{ background: 'var(--surface-container-lowest)', borderRadius: '12px', padding: '1.25rem', border: '1px solid var(--outline-variant)' }}>
          <h3 style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem', fontSize: '1rem' }}>
            <Clock size={18} style={{ color: 'var(--warning)' }} /> Timeline
          </h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--on-surface-variant)' }}>Placed At</span>
              <span style={{ fontSize: '0.875rem' }}>{formatDate(order.placedAt || order.createdAt)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--on-surface-variant)' }}>Last Updated</span>
              <span style={{ fontSize: '0.875rem' }}>{formatDate(order.updatedAt)}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Cancellation Reason */}
      {order.cancellationReason && (
        <div style={{
          marginTop: '1rem', background: '#fef2f2', border: '1px solid #fecaca',
          borderRadius: '12px', padding: '1rem'
        }}>
          <p style={{ fontWeight: 600, color: '#dc2626', marginBottom: '0.25rem' }}>
            Cancellation Reason
          </p>
          <p style={{ color: '#7f1d1d', margin: 0 }}>{order.cancellationReason}</p>
        </div>
      )}

      {/* Admin Actions */}
      <div style={{
        marginTop: '1.5rem', background: 'var(--surface-container-lowest)',
        borderRadius: '12px', padding: '1.25rem', border: '1px solid var(--outline-variant)'
      }}>
        <h3 style={{ marginBottom: '1rem', fontSize: '1rem' }}>Admin Actions</h3>
        <button
          onClick={() => setStatusModal(true)}
          style={{
            background: 'var(--primary)', color: 'var(--on-primary)',
            border: 'none', borderRadius: '8px', padding: '0.625rem 1.25rem',
            cursor: 'pointer', fontWeight: 600
          }}
        >
          Update Order Status
        </button>
      </div>

      {/* Status Update Modal */}
      {statusModal && (
        <div style={{
          position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)',
          display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
        }} onClick={() => setStatusModal(false)}>
          <div style={{
            background: 'var(--surface)', borderRadius: '16px', padding: '1.5rem',
            width: '100%', maxWidth: '480px', margin: '1rem'
          }} onClick={e => e.stopPropagation()}>
            <h3 style={{ marginBottom: '1.25rem' }}>Update Order Status</h3>
            <form onSubmit={handleStatusUpdate}>
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}>
                  Current Status
                </label>
                <input
                  value={order.status}
                  disabled
                  style={{
                    width: '100%', padding: '0.625rem', borderRadius: '8px',
                    border: '1px solid var(--outline)', background: 'var(--surface-variant)',
                    color: 'var(--on-surface-variant)', boxSizing: 'border-box'
                  }}
                />
              </div>
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}>
                  New Status *
                </label>
                <select
                  value={statusForm.newStatus}
                  onChange={e => setStatusForm({ ...statusForm, newStatus: e.target.value })}
                  required
                  style={{
                    width: '100%', padding: '0.625rem', borderRadius: '8px',
                    border: '1px solid var(--outline)', background: 'var(--surface)',
                    color: 'var(--on-surface)', boxSizing: 'border-box'
                  }}
                >
                  <option value="">Select new status</option>
                  {allStatuses.filter(s => s !== order.status).map(s => (
                    <option key={s} value={s}>{s}</option>
                  ))}
                </select>
              </div>
              {['Cancelled', 'RefundInitiated', 'Refunded', 'CancelRequested'].includes(statusForm.newStatus) && (
                <div style={{ marginBottom: '1rem' }}>
                  <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}>
                    Reason * (min 5 characters)
                  </label>
                  <textarea
                    value={statusForm.reason}
                    onChange={e => setStatusForm({ ...statusForm, reason: e.target.value })}
                    placeholder="Enter reason for this status change..."
                    rows={3}
                    required
                    style={{
                      width: '100%', padding: '0.625rem', borderRadius: '8px',
                      border: '1px solid var(--outline)', background: 'var(--surface)',
                      color: 'var(--on-surface)', resize: 'vertical', boxSizing: 'border-box'
                    }}
                  />
                </div>
              )}
              <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end' }}>
                <button
                  type="button"
                  onClick={() => setStatusModal(false)}
                  style={{
                    padding: '0.625rem 1.25rem', borderRadius: '8px',
                    border: '1px solid var(--outline)', background: 'none',
                    cursor: 'pointer', color: 'var(--on-surface)'
                  }}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={updating}
                  style={{
                    padding: '0.625rem 1.25rem', borderRadius: '8px',
                    background: 'var(--primary)', color: 'var(--on-primary)',
                    border: 'none', cursor: 'pointer', fontWeight: 600,
                    opacity: updating ? 0.7 : 1
                  }}
                >
                  {updating ? 'Updating...' : 'Update Status'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
