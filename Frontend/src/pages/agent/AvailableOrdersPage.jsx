import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import toast from 'react-hot-toast';
import './AvailableOrdersPage.css';

function AvailableOrdersPage() {
  const navigate = useNavigate();
  const [availableOrders, setAvailableOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [accepting, setAccepting] = useState(null);
  const [autoRefresh, setAutoRefresh] = useState(true);

  useEffect(() => {
    fetchAvailableOrders();
    
    // Auto-refresh every 15 seconds
    let interval;
    if (autoRefresh) {
      interval = setInterval(() => {
        fetchAvailableOrders(true); // Silent refresh
      }, 15000);
    }
    
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [autoRefresh]);

  const fetchAvailableOrders = async (silent = false) => {
    try {
      if (!silent) setLoading(true);
      const response = await api.get('/gateway/deliveries/available');
      setAvailableOrders(response.data?.data || []);
    } catch (error) {
      console.error('Failed to fetch available orders:', error);
      if (!silent) toast.error('Failed to load available orders');
    } finally {
      if (!silent) setLoading(false);
    }
  };

  const handleAcceptOrder = async (orderId) => {
    try {
      setAccepting(orderId);
      await api.post(`/gateway/deliveries/${orderId}/accept`);
      toast.success('Order accepted successfully!');
      
      // Refresh the list
      await fetchAvailableOrders();
      
      // Navigate to my deliveries
      setTimeout(() => {
        navigate('/agent/deliveries');
      }, 1000);
    } catch (error) {
      console.error('Failed to accept order:', error);
      const message = error.response?.data?.message || 'Failed to accept order';
      toast.error(message);
      
      // Refresh list in case order was taken by another agent
      await fetchAvailableOrders();
    } finally {
      setAccepting(null);
    }
  };

  if (loading) {
    return (
      <div className="container" style={{ padding: '2rem 1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-xl)' }}>
          <div>
            <div className="skeleton" style={{ height: '2rem', width: '12rem', marginBottom: '0.5rem' }} />
            <div className="skeleton" style={{ height: '1rem', width: '10rem' }} />
          </div>
          <div className="skeleton" style={{ height: '2.5rem', width: '8rem', borderRadius: 'var(--rounded-lg)' }} />
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="card" style={{ padding: 'var(--space-lg)' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem' }}>
                <div>
                  <div className="skeleton" style={{ height: '1.125rem', width: '12rem', marginBottom: '0.5rem' }} />
                  <div className="skeleton" style={{ height: '0.875rem', width: '8rem' }} />
                </div>
                <div className="skeleton" style={{ height: '1.5rem', width: '5rem', borderRadius: 'var(--rounded-full)' }} />
              </div>
              <div className="skeleton" style={{ height: '2.5rem', width: '10rem', borderRadius: 'var(--rounded-lg)' }} />
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="available-orders-page">
      <div className="container" style={{ padding: '2rem 1rem' }}>
        {/* Header */}
        <div className="page-header">
          <div>
            <h1 className="display-sm">Available Orders</h1>
            <p className="body-md text-muted">
              Orders ready for pickup - Accept to start delivery
            </p>
          </div>
          <div className="header-actions">
            <button
              className={`btn btn-outline btn-sm ${autoRefresh ? 'active' : ''}`}
              onClick={() => setAutoRefresh(!autoRefresh)}
            >
              {autoRefresh ? '🔄 Auto-refresh ON' : '⏸️ Auto-refresh OFF'}
            </button>
            <button 
              className="btn btn-outline"
              onClick={() => navigate('/agent/dashboard')}
            >
              ← Back to Dashboard
            </button>
          </div>
        </div>

        {/* Stats */}
        <div className="stats-bar">
          <div className="stat-item">
            <span className="stat-value">{availableOrders.length}</span>
            <span className="stat-label">Available Orders</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">
              {autoRefresh ? 'Live' : 'Paused'}
            </span>
            <span className="stat-label">Status</span>
          </div>
        </div>

        {/* Orders List */}
        {availableOrders.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📦</div>
            <h3 className="headline-sm">No Orders Available</h3>
            <p className="body-md text-muted">
              New orders will appear here when restaurants mark them as ready for pickup
            </p>
            <button 
              className="btn btn-primary"
              onClick={() => fetchAvailableOrders()}
            >
              Refresh
            </button>
          </div>
        ) : (
          <div className="orders-grid">
            {availableOrders.map(order => (
              <div key={order.orderId} className="order-card">
                <div className="card-header">
                  <div>
                    <h3 className="title-md">{order.restaurantName}</h3>
                    <p className="body-sm text-muted">
                      Order placed {new Date(order.createdAt).toLocaleTimeString()}
                    </p>
                  </div>
                  <div className="order-amount">
                    <span className="amount-label">Total</span>
                    <span className="amount-value">₹{order.totalAmount.toFixed(2)}</span>
                  </div>
                </div>

                <div className="card-body">
                  {/* Restaurant Address */}
                  <div className="info-row">
                    <span className="info-icon">🏪</span>
                    <div className="info-content">
                      <span className="info-label">Pickup from:</span>
                      <span className="info-value">{order.restaurantAddress}</span>
                    </div>
                  </div>

                  {/* Delivery Address */}
                  <div className="info-row">
                    <span className="info-icon">📍</span>
                    <div className="info-content">
                      <span className="info-label">Deliver to:</span>
                      <span className="info-value">{order.deliveryAddress}</span>
                    </div>
                  </div>

                  {/* Order Details */}
                  <div className="order-details">
                    <div className="detail-item">
                      <span className="detail-icon">🍽️</span>
                      <span className="detail-text">{order.itemCount} items</span>
                    </div>
                    <div className="detail-item">
                      <span className="detail-icon">💳</span>
                      <span className="detail-text">{order.paymentMethod}</span>
                    </div>
                    {order.estimatedDeliveryTime && (
                      <div className="detail-item">
                        <span className="detail-icon">⏱️</span>
                        <span className="detail-text">
                          ETA: {new Date(order.estimatedDeliveryTime).toLocaleTimeString()}
                        </span>
                      </div>
                    )}
                  </div>

                  {/* Delivery Instructions */}
                  {order.deliveryInstructions && (
                    <div className="instructions">
                      <span className="instructions-label">📝 Instructions:</span>
                      <span className="instructions-text">{order.deliveryInstructions}</span>
                    </div>
                  )}
                </div>

                <div className="card-footer">
                  <button
                    className="btn btn-primary btn-lg"
                    onClick={() => handleAcceptOrder(order.orderId)}
                    disabled={accepting === order.orderId}
                    style={{ width: '100%' }}
                  >
                    {accepting === order.orderId ? (
                      <>
                        <div className="spinner" style={{ width: 16, height: 16, borderWidth: 2 }} />
                        Accepting...
                      </>
                    ) : (
                      <>✅ Accept Order</>
                    )}
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default AvailableOrdersPage;
