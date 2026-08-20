import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../../services/api';
import toast from 'react-hot-toast';
import './DeliveryDetailPage.css';

function DeliveryDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [delivery, setDelivery] = useState(null);
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [updatingStatus, setUpdatingStatus] = useState(false);

  useEffect(() => {
    fetchDeliveryDetails();
  }, [id]);

  const fetchDeliveryDetails = async () => {
    try {
      setLoading(true);
      
      // Fetch delivery details
      const deliveryResponse = await api.get(`/gateway/deliveries/${id}`);
      const deliveryData = deliveryResponse.data?.data;
      setDelivery(deliveryData);

      // Fetch order details
      if (deliveryData?.orderId) {
        try {
          const orderResponse = await api.get(`/gateway/orders/orders/${deliveryData.orderId}`);
          setOrder(orderResponse.data?.data);
        } catch (error) {
          console.error('Failed to fetch order details:', error);
        }
      }
    } catch (error) {
      console.error('Failed to fetch delivery details:', error);
      toast.error('Failed to load delivery details');
    } finally {
      setLoading(false);
    }
  };

  const getNextAction = (status) => {
    switch (status) {
      case 'Assigned': return { action: 'PickedUp', label: 'Mark as Picked Up', icon: '📦' };
      case 'PickedUp': return { action: 'OutForDelivery', label: 'Start Delivery', icon: '🚗' };
      case 'OutForDelivery': return { action: 'Delivered', label: 'Mark as Delivered', icon: '✅' };
      default: return null;
    }
  };

  const handleStatusUpdate = async (newStatus) => {
    try {
      setUpdatingStatus(true);
      
      let note = '';
      if (newStatus === 'Delivered') {
        note = prompt('Add delivery note (optional):') || 'Delivered successfully';
      }

      await api.put(`/gateway/deliveries/${id}/status`, {
        status: newStatus,
        note: note || undefined
      });

      toast.success(`Status updated to ${newStatus}`);
      await fetchDeliveryDetails();
    } catch (error) {
      console.error('Failed to update status:', error);
      toast.error(error.response?.data?.message || 'Failed to update status');
    } finally {
      setUpdatingStatus(false);
    }
  };

  const handleMarkAsFailed = async () => {
    const reason = prompt('Please provide a reason for failure:');
    if (!reason) {
      toast.error('Failure reason is required');
      return;
    }

    try {
      setUpdatingStatus(true);
      await api.put(`/gateway/deliveries/${id}/status`, {
        status: 'Failed',
        note: reason
      });

      toast.success('Delivery marked as failed');
      await fetchDeliveryDetails();
    } catch (error) {
      console.error('Failed to update status:', error);
      toast.error(error.response?.data?.message || 'Failed to update status');
    } finally {
      setUpdatingStatus(false);
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case 'Assigned': return 'status-pending';
      case 'PickedUp': return 'status-preparing';
      case 'OutForDelivery': return 'status-ready';
      case 'Delivered': return 'status-completed';
      case 'Failed': return 'status-cancelled';
      default: return '';
    }
  };

  if (loading) {
    return (
      <div className="container" style={{ padding: '2rem 1rem' }}>
        <div className="skeleton" style={{ height: '1.5rem', width: '9rem', marginBottom: 'var(--space-lg)' }} />
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 'var(--space-xl)' }}>
          <div>
            <div className="skeleton" style={{ height: '1.75rem', width: '12rem', marginBottom: '0.5rem' }} />
            <div className="skeleton" style={{ height: '1rem', width: '8rem' }} />
          </div>
          <div className="skeleton" style={{ height: '1.75rem', width: '7rem', borderRadius: 'var(--rounded-full)' }} />
        </div>
        {/* Timeline skeleton */}
        <div className="card" style={{ padding: 'var(--space-xl)', marginBottom: 'var(--space-xl)' }}>
          <div className="skeleton" style={{ height: '1.25rem', width: '8rem', marginBottom: 'var(--space-lg)' }} />
          <div style={{ display: 'flex', gap: '1rem' }}>
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.5rem' }}>
                <div className="skeleton skeleton-circle" style={{ height: '2.5rem', width: '2.5rem' }} />
                <div className="skeleton" style={{ height: '0.75rem', width: '4rem' }} />
              </div>
            ))}
          </div>
        </div>
        {/* Action buttons skeleton */}
        <div style={{ display: 'flex', gap: '1rem' }}>
          <div className="skeleton" style={{ height: '2.75rem', width: '10rem', borderRadius: 'var(--rounded-lg)' }} />
          <div className="skeleton" style={{ height: '2.75rem', width: '10rem', borderRadius: 'var(--rounded-lg)' }} />
        </div>
      </div>
    );
  }

  if (!delivery) {
    return (
      <div className="container" style={{ padding: '2rem', textAlign: 'center' }}>
        <h2>Delivery not found</h2>
        <button className="btn btn-primary" onClick={() => navigate('/agent/deliveries')}>
          Back to Deliveries
        </button>
      </div>
    );
  }

  const nextAction = getNextAction(delivery.status);

  return (
    <div className="delivery-detail-page">
      <div className="container" style={{ padding: '2rem 1rem' }}>
        {/* Header */}
        <div className="page-header">
          <button 
            className="btn btn-outline"
            onClick={() => navigate('/agent/deliveries')}
          >
            ← Back
          </button>
          <span className={`status-badge ${getStatusColor(delivery.status)}`}>
            {delivery.status}
          </span>
        </div>

        <h1 className="display-sm" style={{ marginBottom: '0.5rem' }}>
          Delivery Details
        </h1>
        <p className="body-md text-muted" style={{ marginBottom: '2rem' }}>
          Order #{delivery.orderId.substring(0, 8)}
        </p>

        <div className="detail-grid">
          {/* Delivery Information */}
          <div className="detail-card">
            <h2 className="headline-sm" style={{ marginBottom: '1rem' }}>
              Delivery Information
            </h2>

            <div className="info-grid">
              <div className="info-item">
                <span className="label">Delivery ID</span>
                <span className="value">{delivery.id.substring(0, 13)}...</span>
              </div>
              <div className="info-item">
                <span className="label">Order ID</span>
                <span className="value">{delivery.orderId.substring(0, 13)}...</span>
              </div>
              <div className="info-item">
                <span className="label">Agent Name</span>
                <span className="value">{delivery.agentName}</span>
              </div>
              <div className="info-item">
                <span className="label">Agent Mobile</span>
                <span className="value">{delivery.agentMobile}</span>
              </div>
              <div className="info-item">
                <span className="label">Assigned At</span>
                <span className="value">{new Date(delivery.assignedAt).toLocaleString()}</span>
              </div>
              {delivery.estimatedArrivalTime && (
                <div className="info-item">
                  <span className="label">Estimated Arrival</span>
                  <span className="value">{new Date(delivery.estimatedArrivalTime).toLocaleString()}</span>
                </div>
              )}
              {delivery.actualArrivalTime && (
                <div className="info-item">
                  <span className="label">Actual Arrival</span>
                  <span className="value">{new Date(delivery.actualArrivalTime).toLocaleString()}</span>
                </div>
              )}
              {delivery.deliveryNotes && (
                <div className="info-item" style={{ gridColumn: '1 / -1' }}>
                  <span className="label">Delivery Notes</span>
                  <span className="value">{delivery.deliveryNotes}</span>
                </div>
              )}
              {delivery.failureReason && (
                <div className="info-item" style={{ gridColumn: '1 / -1' }}>
                  <span className="label">Failure Reason</span>
                  <span className="value" style={{ color: 'var(--error)' }}>
                    {delivery.failureReason}
                  </span>
                </div>
              )}
            </div>
          </div>

          {/* Order Information */}
          {order && (
            <div className="detail-card">
              <h2 className="headline-sm" style={{ marginBottom: '1rem' }}>
                Order Information
              </h2>

              <div className="info-grid">
                <div className="info-item">
                  <span className="label">Customer Name</span>
                  <span className="value">{order.customerName}</span>
                </div>
                <div className="info-item">
                  <span className="label">Customer Mobile</span>
                  <span className="value">{order.customerMobile}</span>
                </div>
                <div className="info-item" style={{ gridColumn: '1 / -1' }}>
                  <span className="label">Delivery Address</span>
                  <span className="value">{order.deliveryAddress}</span>
                </div>
                <div className="info-item">
                  <span className="label">Total Amount</span>
                  <span className="value">₹{order.totalAmount?.toFixed(2)}</span>
                </div>
                <div className="info-item">
                  <span className="label">Payment Method</span>
                  <span className="value">{order.paymentMethod}</span>
                </div>
              </div>

              {/* Order Items */}
              {order.items && order.items.length > 0 && (
                <div style={{ marginTop: '1rem' }}>
                  <h3 className="title-md" style={{ marginBottom: '0.75rem' }}>
                    Order Items ({order.items.length})
                  </h3>
                  <div className="items-list">
                    {order.items.map((item, idx) => (
                      <div key={idx} className="item-row">
                        <div>
                          <span className="body-sm">{item.name}</span>
                          <span className="body-xs text-muted"> × {item.quantity}</span>
                        </div>
                        <span className="body-sm">₹{(item.unitPrice * item.quantity).toFixed(2)}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Status Timeline */}
          <div className="detail-card" style={{ gridColumn: '1 / -1' }}>
            <h2 className="headline-sm" style={{ marginBottom: '1.5rem' }}>
              Delivery Timeline
            </h2>

            <div className="timeline">
              <div className={`timeline-item ${delivery.assignedAt ? 'completed' : ''}`}>
                <div className="timeline-marker"></div>
                <div className="timeline-content">
                  <h4 className="title-sm">Assigned</h4>
                  {delivery.assignedAt && (
                    <p className="body-sm text-muted">
                      {new Date(delivery.assignedAt).toLocaleString()}
                    </p>
                  )}
                </div>
              </div>

              <div className={`timeline-item ${delivery.pickedUpAt ? 'completed' : ''}`}>
                <div className="timeline-marker"></div>
                <div className="timeline-content">
                  <h4 className="title-sm">Picked Up</h4>
                  {delivery.pickedUpAt && (
                    <p className="body-sm text-muted">
                      {new Date(delivery.pickedUpAt).toLocaleString()}
                    </p>
                  )}
                </div>
              </div>

              <div className={`timeline-item ${delivery.outForDeliveryAt ? 'completed' : ''}`}>
                <div className="timeline-marker"></div>
                <div className="timeline-content">
                  <h4 className="title-sm">Out for Delivery</h4>
                  {delivery.outForDeliveryAt && (
                    <p className="body-sm text-muted">
                      {new Date(delivery.outForDeliveryAt).toLocaleString()}
                    </p>
                  )}
                </div>
              </div>

              <div className={`timeline-item ${delivery.deliveredAt ? 'completed' : ''}`}>
                <div className="timeline-marker"></div>
                <div className="timeline-content">
                  <h4 className="title-sm">Delivered</h4>
                  {delivery.deliveredAt && (
                    <p className="body-sm text-muted">
                      {new Date(delivery.deliveredAt).toLocaleString()}
                    </p>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Status History */}
          {delivery.statusHistory && delivery.statusHistory.length > 0 && (
            <div className="detail-card" style={{ gridColumn: '1 / -1' }}>
              <h2 className="headline-sm" style={{ marginBottom: '1rem' }}>
                Status History
              </h2>
              <div className="history-list">
                {delivery.statusHistory.map((history, idx) => (
                  <div key={idx} className="history-item">
                    <div className="history-time">
                      <span className="body-sm">{new Date(history.changedAt).toLocaleString()}</span>
                    </div>
                    <div className="history-details">
                      <span className={`status-badge ${getStatusColor(history.status)}`}>
                        {history.status}
                      </span>
                      {history.note && (
                        <p className="body-sm text-muted" style={{ margin: '0.25rem 0 0 0' }}>
                          {history.note}
                        </p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Action Buttons */}
        {['Assigned', 'PickedUp', 'OutForDelivery'].includes(delivery.status) && (
          <div className="action-bar">
            {nextAction && (
              <button
                className="btn btn-primary"
                onClick={() => handleStatusUpdate(nextAction.action)}
                disabled={updatingStatus}
              >
                {updatingStatus ? 'Updating...' : `${nextAction.icon} ${nextAction.label}`}
              </button>
            )}
            <button
              className="btn btn-error"
              onClick={handleMarkAsFailed}
              disabled={updatingStatus}
            >
              Mark as Failed
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

export default DeliveryDetailPage;
