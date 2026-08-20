import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Package, Search, Filter, RefreshCw, XCircle } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import toast from 'react-hot-toast';
import { OrderCardSkeleton } from '../../components/common/Skeleton';
import './OrdersManagement.css';

export default function OrdersManagement() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [restaurant, setRestaurant] = useState(null);
  const [orders, setOrders] = useState([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [rejectionReason, setRejectionReason] = useState('');
  const [isRejecting, setIsRejecting] = useState(false);

  useEffect(() => {
    loadData();
    // Auto-refresh every 30 seconds
    const interval = setInterval(loadData, 30000);
    return () => clearInterval(interval);
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
      
      // Get orders
      const ordersRes = await api.get(API_ENDPOINTS.orders.ordersByRestaurant(myRestaurant.id));
      const ordersData = ordersRes.data?.data || ordersRes.data;
      setOrders(Array.isArray(ordersData) ? ordersData : []);
      
    } catch (error) {
      console.error('Failed to load orders:', error);
      toast.error('Failed to load orders');
    } finally {
      setLoading(false);
    }
  };

  const handleStatusUpdate = async (orderId, newStatus) => {
    try {
      await api.put(API_ENDPOINTS.orders.orderStatus(orderId), { newStatus: newStatus });
      toast.success(`Order status updated to ${getStatusLabel(newStatus)}`);
      loadData();
    } catch (error) {
      console.error('Failed to update status:', error);
      const errorMsg = error.response?.data?.message || 'Failed to update order status';
      toast.error(errorMsg);
    }
  };

  const handleRejectClick = (order) => {
    setSelectedOrder(order);
    setRejectionReason('');
    setShowRejectModal(true);
  };

  const handleRejectOrder = async () => {
    if (!rejectionReason.trim()) {
      toast.error('Please provide a rejection reason');
      return;
    }

    setIsRejecting(true);
    try {
      await api.post(API_ENDPOINTS.orders.rejectOrder(selectedOrder.id), {
        rejectionReason: rejectionReason.trim()
      });
      toast.success('Order rejected successfully');
      setShowRejectModal(false);
      setSelectedOrder(null);
      setRejectionReason('');
      loadData();
    } catch (error) {
      console.error('Failed to reject order:', error);
      const errorMsg = error.response?.data?.message || 'Failed to reject order';
      toast.error(errorMsg);
    } finally {
      setIsRejecting(false);
    }
  };

  const rejectionReasons = [
    'Items out of stock',
    'Restaurant too busy',
    'Kitchen closed',
    'Unable to deliver to location',
    'Technical issue',
    'Other'
  ];

  const getStatusBadgeClass = (status) => {
    const statusMap = {
      'Paid': 'badge-info',
      'AwaitingAcceptance': 'badge-info',
      'Accepted': 'badge-info',
      'Preparing': 'badge-primary',
      'ReadyForPickup': 'badge-success',
      'Delivered': 'badge-success',
      'Cancelled': 'badge-error',
      'PaymentFailed': 'badge-error',
      'RestaurantRejected': 'badge-error'
    };
    return statusMap[status] || 'badge-secondary';
  };

  const getNextStatus = (currentStatus) => {
    const statusFlow = {
      'Paid': 'Accepted',
      'AwaitingAcceptance': 'Accepted',
      'Accepted': 'Preparing',
      'Preparing': 'ReadyForPickup'
    };
    return statusFlow[currentStatus];
  };

  const getStatusLabel = (status) => {
    const labels = {
      'Paid': 'Paid',
      'AwaitingAcceptance': 'COD - Awaiting Acceptance',
      'Accepted': 'Accepted',
      'Preparing': 'Preparing',
      'ReadyForPickup': 'Ready for Pickup',
      'Delivered': 'Delivered',
      'Cancelled': 'Cancelled',
      'PaymentFailed': 'Payment Failed',
      'RestaurantRejected': 'Rejected'
    };
    return labels[status] || status;
  };

  const filteredOrders = orders.filter(order => {
    const matchesSearch = 
      order.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.customerName?.toLowerCase().includes(searchTerm.toLowerCase());
    
    let matchesStatus = true;
    if (statusFilter !== 'all') {
      if (statusFilter === 'new') {
        // New orders are Paid and AwaitingAcceptance
        matchesStatus = ['Paid', 'AwaitingAcceptance'].includes(order.status);
      } else {
        matchesStatus = order.status === statusFilter;
      }
    }
    
    return matchesSearch && matchesStatus;
  });

  const pendingOrders = filteredOrders.filter(o => ['Paid', 'AwaitingAcceptance', 'Accepted', 'Preparing', 'ReadyForPickup'].includes(o.status));
  const completedOrders = filteredOrders.filter(o => ['Delivered', 'Cancelled', 'PaymentFailed', 'RestaurantRejected'].includes(o.status));

  if (loading) {
    return (
      <div className="orders-management page-enter">
        <div className="container">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-xl)' }}>
            <div className="skeleton" style={{ height: '1.75rem', width: '10rem' }} />
            <div className="skeleton" style={{ height: '2.5rem', width: '8rem', borderRadius: 'var(--rounded-lg)' }} />
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
            {Array.from({ length: 5 }).map((_, i) => <OrderCardSkeleton key={i} />)}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="orders-management page-enter">
      <div className="container">
        <div className="page-header">
          <div>
            <h1 className="headline-lg">Orders Management</h1>
            <p className="body-md text-muted">{restaurant?.name}</p>
          </div>
          <button className="btn btn-outline" onClick={loadData}>
            <RefreshCw size={18} /> Refresh
          </button>
        </div>

        <div className="filters-bar">
          <div className="search-box">
            <Search size={18} />
            <input
              type="text"
              placeholder="Search orders..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>

          <div className="filter-group">
            <Filter size={18} />
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="form-select"
            >
              <option value="all">All Status</option>
              <option value="new">New Orders</option>
              <option value="Accepted">Accepted</option>
              <option value="Preparing">Preparing</option>
              <option value="ReadyForPickup">Ready for Pickup</option>
            </select>
          </div>
        </div>

        {/* Pending Orders */}
        {pendingOrders.length > 0 && (
          <div className="orders-section">
            <h2 className="headline-md">Active Orders ({pendingOrders.length})</h2>
            <div className="orders-grid">
              {pendingOrders.map(order => (
                <div key={order.id} className="order-card">
                  <div className="order-header">
                    <div>
                      <h3 className="headline-sm">Order #{order.id.substring(0, 8)}</h3>
                      <p className="body-sm text-muted">{new Date(order.createdAt).toLocaleString()}</p>
                    </div>
                    <span className={`badge ${getStatusBadgeClass(order.status)}`}>
                      {getStatusLabel(order.status)}
                    </span>
                  </div>

                  <div className="order-details">
                    <div className="detail-row">
                      <span className="label">Customer:</span>
                      <span className="value">{order.customerName || 'N/A'}</span>
                    </div>
                    <div className="detail-row">
                      <span className="label">Items:</span>
                      <span className="value">{order.items?.length || 0} items</span>
                    </div>
                    <div className="detail-row">
                      <span className="label">Total:</span>
                      <span className="value price">₹{order.totalAmount?.toFixed(2)}</span>
                    </div>
                    {order.deliveryAddress && (
                      <div className="detail-row">
                        <span className="label">Address:</span>
                        <span className="value">{order.deliveryAddress}</span>
                      </div>
                    )}
                  </div>

                  <div className="order-actions">
                    {getNextStatus(order.status) && (
                      <button
                        className="btn btn-primary btn-sm"
                        onClick={() => handleStatusUpdate(order.id, getNextStatus(order.status))}
                      >
                        Mark as {getNextStatus(order.status)}
                      </button>
                    )}
                    {(order.status === 'Paid' || order.status === 'AwaitingAcceptance') && (
                      <button
                        className="btn btn-error btn-sm"
                        onClick={() => handleRejectClick(order)}
                      >
                        <XCircle size={16} /> Reject
                      </button>
                    )}
                    <button
                      className="btn btn-outline btn-sm"
                      onClick={() => navigate(`/partner/orders/${order.id}`)}
                    >
                      View Details
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Completed Orders */}
        {completedOrders.length > 0 && (
          <div className="orders-section">
            <h2 className="headline-md">Completed Orders ({completedOrders.length})</h2>
            <div className="orders-table">
              <table>
                <thead>
                  <tr>
                    <th>Order ID</th>
                    <th>Customer</th>
                    <th>Items</th>
                    <th>Amount</th>
                    <th>Status</th>
                    <th>Date</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {completedOrders.map(order => (
                    <tr key={order.id}>
                      <td>#{order.id.substring(0, 8)}</td>
                      <td>{order.customerName || 'N/A'}</td>
                      <td>{order.items?.length || 0} items</td>
                      <td>₹{order.totalAmount?.toFixed(2)}</td>
                      <td>
                        <span className={`badge ${getStatusBadgeClass(order.status)}`}>
                          {getStatusLabel(order.status)}
                        </span>
                      </td>
                      <td>{new Date(order.createdAt).toLocaleDateString()}</td>
                      <td>
                        <button
                          className="btn btn-ghost btn-sm"
                          onClick={() => navigate(`/partner/orders/${order.id}`)}
                        >
                          View
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {filteredOrders.length === 0 && (
          <div className="empty-state">
            <Package size={64} className="text-muted" />
            <h2 className="headline-lg">No Orders Found</h2>
            <p className="body-lg text-muted">Orders will appear here when customers place them.</p>
          </div>
        )}
      </div>

      {/* Rejection Modal */}
      {showRejectModal && (
        <div className="modal-overlay" onClick={() => setShowRejectModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2 className="headline-lg">Reject Order</h2>
            <p className="body-md text-muted">
              Order #{selectedOrder?.id.substring(0, 8).toUpperCase()}
            </p>

            <div className="form-group">
              <label className="body-lg">Rejection Reason *</label>
              <select
                className="form-select"
                value={rejectionReason}
                onChange={(e) => setRejectionReason(e.target.value)}
              >
                <option value="">Select a reason</option>
                {rejectionReasons.map((reason) => (
                  <option key={reason} value={reason}>
                    {reason}
                  </option>
                ))}
              </select>
            </div>

            {rejectionReason === 'Other' && (
              <div className="form-group">
                <label className="body-lg">Custom Reason</label>
                <textarea
                  className="form-input"
                  placeholder="Please specify the reason..."
                  rows={3}
                  value={rejectionReason === 'Other' ? '' : rejectionReason}
                  onChange={(e) => setRejectionReason(e.target.value)}
                />
              </div>
            )}

            <div className="modal-actions">
              <button
                className="btn btn-text"
                onClick={() => setShowRejectModal(false)}
                disabled={isRejecting}
              >
                Cancel
              </button>
              <button
                className="btn btn-error"
                onClick={handleRejectOrder}
                disabled={isRejecting || !rejectionReason.trim()}
              >
                {isRejecting ? 'Rejecting...' : 'Reject Order'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
