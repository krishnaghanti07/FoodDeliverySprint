import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import toast from 'react-hot-toast';
import './MyDeliveriesPage.css';

function MyDeliveriesPage() {
  const navigate = useNavigate();
  const [deliveries, setDeliveries] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('active'); // active, completed, all
  const [updatingStatus, setUpdatingStatus] = useState(null);

  useEffect(() => {
    fetchDeliveries();
  }, []);

  const fetchDeliveries = async () => {
    try {
      setLoading(true);
      const response = await api.get('/gateway/deliveries/my');
      setDeliveries(response.data?.data || []);
    } catch (error) {
      console.error('Failed to fetch deliveries:', error);
      toast.error('Failed to load deliveries');
    } finally {
      setLoading(false);
    }
  };

  const getFilteredDeliveries = () => {
    if (filter === 'active') {
      return deliveries.filter(d => 
        ['Assigned', 'PickedUp', 'OutForDelivery'].includes(d.status)
      );
    } else if (filter === 'completed') {
      return deliveries.filter(d => 
        ['Delivered', 'Failed'].includes(d.status)
      );
    }
    return deliveries;
  };

  const getNextAction = (status) => {
    switch (status) {
      case 'Assigned': return { action: 'PickedUp', label: 'Mark as Picked Up', icon: '📦' };
      case 'PickedUp': return { action: 'OutForDelivery', label: 'Start Delivery', icon: '🚗' };
      case 'OutForDelivery': return { action: 'Delivered', label: 'Mark as Delivered', icon: '✅' };
      default: return null;
    }
  };

  const handleStatusUpdate = async (deliveryId, newStatus) => {
    try {
      setUpdatingStatus(deliveryId);
      
      let note = '';
      if (newStatus === 'Delivered') {
        note = prompt('Add delivery note (optional):') || 'Delivered successfully';
      }

      await api.put(`/gateway/deliveries/${deliveryId}/status`, {
        status: newStatus,
        note: note || undefined
      });

      toast.success(`Status updated to ${newStatus}`);
      await fetchDeliveries();
    } catch (error) {
      console.error('Failed to update status:', error);
      toast.error(error.response?.data?.message || 'Failed to update status');
    } finally {
      setUpdatingStatus(null);
    }
  };

  const handleMarkAsFailed = async (deliveryId) => {
    const reason = prompt('Please provide a reason for failure:');
    if (!reason) {
      toast.error('Failure reason is required');
      return;
    }

    try {
      setUpdatingStatus(deliveryId);
      await api.put(`/gateway/deliveries/${deliveryId}/status`, {
        status: 'Failed',
        note: reason
      });

      toast.success('Delivery marked as failed');
      await fetchDeliveries();
    } catch (error) {
      console.error('Failed to update status:', error);
      toast.error(error.response?.data?.message || 'Failed to update status');
    } finally {
      setUpdatingStatus(null);
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

  const filteredDeliveries = getFilteredDeliveries();

  if (loading) {
    return (
      <div className="container" style={{ padding: '2rem 1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-xl)' }}>
          <div>
            <div className="skeleton" style={{ height: '2rem', width: '10rem', marginBottom: '0.5rem' }} />
            <div className="skeleton" style={{ height: '1rem', width: '14rem' }} />
          </div>
        </div>
        <div style={{ display: 'flex', gap: '0.75rem', marginBottom: 'var(--space-xl)' }}>
          {[80, 90, 100, 80].map((w, i) => (
            <div key={i} className="skeleton" style={{ height: '2.25rem', width: w, borderRadius: 'var(--rounded-full)' }} />
          ))}
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="card" style={{ padding: 'var(--space-lg)' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem' }}>
                <div>
                  <div className="skeleton" style={{ height: '1.125rem', width: '10rem', marginBottom: '0.5rem' }} />
                  <div className="skeleton" style={{ height: '0.875rem', width: '8rem' }} />
                </div>
                <div className="skeleton" style={{ height: '1.5rem', width: '6rem', borderRadius: 'var(--rounded-full)' }} />
              </div>
              <div style={{ display: 'flex', gap: '0.75rem' }}>
                <div className="skeleton" style={{ height: '2.25rem', width: '8rem', borderRadius: 'var(--rounded-lg)' }} />
                <div className="skeleton" style={{ height: '2.25rem', width: '8rem', borderRadius: 'var(--rounded-lg)' }} />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="my-deliveries-page">
      <div className="container" style={{ padding: '2rem 1rem' }}>
        {/* Header */}
        <div className="page-header">
          <div>
            <h1 className="display-sm">My Deliveries</h1>
            <p className="body-md text-muted">Manage your assigned deliveries</p>
          </div>
          <button 
            className="btn btn-outline"
            onClick={() => navigate('/agent/dashboard')}
          >
            ← Back to Dashboard
          </button>
        </div>

        {/* Filter Tabs */}
        <div className="filter-tabs">
          <button
            className={`tab ${filter === 'active' ? 'active' : ''}`}
            onClick={() => setFilter('active')}
          >
            Active ({deliveries.filter(d => ['Assigned', 'PickedUp', 'OutForDelivery'].includes(d.status)).length})
          </button>
          <button
            className={`tab ${filter === 'completed' ? 'active' : ''}`}
            onClick={() => setFilter('completed')}
          >
            Completed ({deliveries.filter(d => ['Delivered', 'Failed'].includes(d.status)).length})
          </button>
          <button
            className={`tab ${filter === 'all' ? 'active' : ''}`}
            onClick={() => setFilter('all')}
          >
            All ({deliveries.length})
          </button>
        </div>

        {/* Deliveries List */}
        {filteredDeliveries.length === 0 ? (
          <div className="empty-state">
            <p className="body-lg text-muted">
              {filter === 'active' ? 'No active deliveries' : 
               filter === 'completed' ? 'No completed deliveries yet' : 
               'No deliveries found'}
            </p>
          </div>
        ) : (
          <div className="deliveries-grid">
            {filteredDeliveries.map(delivery => {
              const nextAction = getNextAction(delivery.status);
              const isUpdating = updatingStatus === delivery.id;

              return (
                <div key={delivery.id} className="delivery-card">
                  <div className="card-header">
                    <div>
                      <h3 className="title-md">Order #{delivery.orderId.substring(0, 8)}</h3>
                      <p className="body-sm text-muted">
                        Assigned {new Date(delivery.assignedAt).toLocaleString()}
                      </p>
                    </div>
                    <span className={`status-badge ${getStatusColor(delivery.status)}`}>
                      {delivery.status}
                    </span>
                  </div>

                  {/* Contact Info */}
                  <div className="info-section">
                    <div className="info-item">
                      <span className="body-sm text-muted">Agent:</span>
                      <span className="body-sm">{delivery.agentName}</span>
                    </div>
                    <div className="info-item">
                      <span className="body-sm text-muted">Mobile:</span>
                      <span className="body-sm">{delivery.agentMobile}</span>
                    </div>
                  </div>

                  {/* Timeline */}
                  {delivery.estimatedArrivalTime && (
                    <div className="eta-info">
                      ⏱️ ETA: {new Date(delivery.estimatedArrivalTime).toLocaleTimeString()}
                    </div>
                  )}

                  {/* Status History */}
                  {delivery.statusHistory && delivery.statusHistory.length > 0 && (
                    <div className="status-history">
                      <p className="body-sm" style={{ fontWeight: 500, marginBottom: '0.5rem' }}>
                        Status History:
                      </p>
                      {delivery.statusHistory.slice(-3).map((history, idx) => (
                        <div key={idx} className="history-item">
                          <span className="body-xs text-muted">
                            {new Date(history.changedAt).toLocaleString()}
                          </span>
                          <span className="body-xs">
                            {history.status}
                            {history.note && ` - ${history.note}`}
                          </span>
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Actions */}
                  <div className="card-actions">
                    <button
                      className="btn btn-outline btn-sm"
                      onClick={() => navigate(`/agent/deliveries/${delivery.id}`)}
                    >
                      View Details
                    </button>

                    {nextAction && (
                      <button
                        className="btn btn-primary btn-sm"
                        onClick={() => handleStatusUpdate(delivery.id, nextAction.action)}
                        disabled={isUpdating}
                      >
                        {isUpdating ? 'Updating...' : `${nextAction.icon} ${nextAction.label}`}
                      </button>
                    )}

                    {['Assigned', 'PickedUp', 'OutForDelivery'].includes(delivery.status) && (
                      <button
                        className="btn btn-error btn-sm"
                        onClick={() => handleMarkAsFailed(delivery.id)}
                        disabled={isUpdating}
                      >
                        Mark as Failed
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

export default MyDeliveriesPage;
