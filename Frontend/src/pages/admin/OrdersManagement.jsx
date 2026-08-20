import { useState, useEffect } from 'react';
import { Package, Search, Filter, Calendar, DollarSign, Clock, MapPin, User, ChevronDown, ChevronUp, CheckCircle, XCircle } from 'lucide-react';
import api, { apiService } from '../../services/api';
import toast from 'react-hot-toast';
import './OrdersManagement.css';

export default function OrdersManagement() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('all'); // 'all' | 'refunds'
  const [filters, setFilters] = useState({
    status: '',
    restaurantId: '',
    from: '',
    to: ''
  });
  const [restaurants, setRestaurants] = useState([]);
  const [expandedOrder, setExpandedOrder] = useState(null);
  const [statusUpdateModal, setStatusUpdateModal] = useState({ show: false, orderId: null, currentStatus: '' });
  const [statusForm, setStatusForm] = useState({ newStatus: '', reason: '' });
  const [refunds, setRefunds] = useState([]);
  const [showRefundModal, setShowRefundModal] = useState(false);
  const [selectedRefund, setSelectedRefund] = useState(null);
  const [refundAction, setRefundAction] = useState(null);
  const [adminNotes, setAdminNotes] = useState('');

  useEffect(() => {
    if (activeTab === 'all') {
      fetchOrders();
    } else if (activeTab === 'refunds') {
      fetchRefunds();
    }
    fetchRestaurants();
  }, [filters, activeTab]);

  const fetchOrders = async () => {
    try {
      setLoading(true);
      
      // Call OrderService directly instead of AdminService snapshots
      const response = await api.get('/gateway/orders/orders');
      
      // Handle response format
      let ordersData = [];
      if (response.data?.data) {
        ordersData = Array.isArray(response.data.data) ? response.data.data : [];
      } else if (Array.isArray(response.data)) {
        ordersData = response.data;
      }
      
      // Apply filters on frontend if needed
      let filteredOrders = ordersData;
      if (filters.status) {
        filteredOrders = filteredOrders.filter(o => o.status === filters.status);
      }
      if (filters.restaurantId) {
        filteredOrders = filteredOrders.filter(o => o.restaurantId === filters.restaurantId);
      }
      if (filters.from) {
        const fromDate = new Date(filters.from);
        filteredOrders = filteredOrders.filter(o => new Date(o.createdAt) >= fromDate);
      }
      if (filters.to) {
        const toDate = new Date(filters.to);
        filteredOrders = filteredOrders.filter(o => new Date(o.createdAt) <= toDate);
      }
      
      setOrders(filteredOrders);
    } catch (error) {
      console.error('Failed to fetch orders:', error);
      toast.error('Failed to load orders');
      setOrders([]);
    } finally {
      setLoading(false);
    }
  };

  const fetchRefunds = async () => {
    try {
      setLoading(true);
      
      // Fetch all orders and filter for Cancelled + CARD/Wallet payment (paid orders)
      const response = await api.get('/gateway/orders/orders');
      let ordersData = [];
      if (response.data?.data) {
        ordersData = Array.isArray(response.data.data) ? response.data.data : [];
      } else if (Array.isArray(response.data)) {
        ordersData = response.data;
      }
      
      // Filter for cancelled orders with CARD/Wallet payment method (paid orders requiring refund approval)
      // Exclude orders that are already Refunded or RefundRejected
      const cancelledPaidOrders = ordersData.filter(order => 
        order.status === 'Cancelled' && 
        (order.paymentMethod === 'CARD' || order.paymentMethod === 'Card' || order.paymentMethod === 'WALLET' || order.paymentMethod === 'Wallet')
      );
      
      // Transform orders into refund format
      const refundRequests = cancelledPaidOrders.map(order => ({
        id: order.id,
        orderId: order.id,
        customerId: order.customerId,
        customerName: order.customerName || 'Unknown',
        customerEmail: order.customerEmail || '',
        restaurantName: order.restaurantName || '',
        originalAmount: order.totalAmount,
        platformFee: order.platformFee || 15,
        cancellationCharge: (order.totalAmount * 0.05), // 5% cancellation charge
        refundAmount: order.totalAmount - (order.platformFee || 15) - (order.totalAmount * 0.05),
        status: 'Pending',
        paymentMethod: order.paymentMethod,
        orderCancellationReason: order.cancellationReason || 'No reason provided',
        requestedAt: order.cancelledAt || order.updatedAt,
        items: order.items || []
      }));
      
      setRefunds(refundRequests);
    } catch (error) {
      console.error('Failed to fetch refunds:', error);
      toast.error('Failed to load refund requests');
      setRefunds([]);
    } finally {
      setLoading(false);
    }
  };

  const fetchRestaurants = async () => {
    try {
      const response = await apiService.catalog.getAllRestaurantsAdmin();
      const restaurantsData = response.data?.data || response.data || [];
      setRestaurants(Array.isArray(restaurantsData) ? restaurantsData : []);
    } catch (error) {
      console.error('Failed to fetch restaurants:', error);
      setRestaurants([]); // Set empty array on error
    }
  };

  const handleFilterChange = (key, value) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  const clearFilters = () => {
    setFilters({ status: '', restaurantId: '', from: '', to: '' });
  };

  const openStatusModal = (orderId, currentStatus) => {
    setStatusUpdateModal({ show: true, orderId, currentStatus });
    setStatusForm({ newStatus: '', reason: '' });
  };

  const closeStatusModal = () => {
    setStatusUpdateModal({ show: false, orderId: null, currentStatus: '' });
    setStatusForm({ newStatus: '', reason: '' });
  };

  const handleStatusUpdate = async (e) => {
    e.preventDefault();
    if (!statusForm.newStatus) {
      toast.error('Please select a new status');
      return;
    }

    const requiresReason = ['Cancelled', 'RefundInitiated', 'Refunded'].includes(statusForm.newStatus);
    if (requiresReason && !statusForm.reason.trim()) {
      toast.error('Reason is required for this status change');
      return;
    }

    try {
      await apiService.admin.updateOrderStatus(statusUpdateModal.orderId, statusForm);
      toast.success('Order status updated successfully');
      closeStatusModal();
      fetchOrders();
    } catch (error) {
      console.error('Failed to update order status:', error);
      toast.error(error.response?.data?.message || 'Failed to update order status');
    }
  };

  const handleRefundClick = (refund, action) => {
    setSelectedRefund(refund);
    setRefundAction(action);
    setAdminNotes('');
    setShowRefundModal(true);
  };

  const handleRefundProcess = async () => {
    if (!selectedRefund || !refundAction) return;

    try {
      if (refundAction === 'approve') {
        // Approve refund: Credit wallet and update order status to Refunded
        const refundData = {
          orderId: selectedRefund.orderId,
          customerId: selectedRefund.customerId,
          originalAmount: selectedRefund.originalAmount,
          platformFee: selectedRefund.platformFee,
          cancellationCharge: selectedRefund.cancellationCharge,
          refundAmount: selectedRefund.refundAmount,
          adminNotes: adminNotes || 'Approved by admin'
        };
        
        // Call refund approval endpoint
        await api.post('/gateway/orders/refunds/approve', refundData);
        
        toast.success('Refund approved successfully. Amount credited to customer wallet.');
      } else {
        // Reject refund: Just update order status to Rejected (no wallet credit)
        await api.post(`/gateway/orders/refunds/reject`, {
          orderId: selectedRefund.orderId,
          adminNotes: adminNotes || 'Rejected by admin'
        });
        
        toast.success('Refund rejected successfully');
      }
      
      setShowRefundModal(false);
      setSelectedRefund(null);
      setAdminNotes('');
      fetchRefunds();
    } catch (error) {
      console.error(`Error ${refundAction}ing refund:`, error);
      toast.error(error.response?.data?.message || `Failed to ${refundAction} refund`);
    }
  };

  const getStatusColor = (status) => {
    const colors = {
      Pending: 'warning',
      Confirmed: 'info',
      Preparing: 'info',
      ReadyForPickup: 'primary',
      OutForDelivery: 'primary',
      Delivered: 'success',
      Cancelled: 'error',
      RefundInitiated: 'warning',
      Refunded: 'error'
    };
    return colors[status] || 'default';
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 0
    }).format(amount);
  };

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const statusOptions = [
    'Pending', 'Confirmed', 'Preparing', 'ReadyForPickup',
    'OutForDelivery', 'Delivered', 'Cancelled', 'RefundInitiated', 'Refunded'
  ];

  if (loading) {
    return (
      <div className="orders-management container">
        <div className="loading-state">
          <div className="spinner"></div>
          <p>Loading orders...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="orders-management container">
      <div className="orders-header">
        <h1><Package size={32} /> Orders Management</h1>
      </div>

      {/* Tabs */}
      <div className="tabs-container">
        <button
          className={`tab ${activeTab === 'all' ? 'active' : ''}`}
          onClick={() => setActiveTab('all')}
        >
          All Orders ({orders.length})
        </button>
        <button
          className={`tab ${activeTab === 'refunds' ? 'active' : ''}`}
          onClick={() => setActiveTab('refunds')}
        >
          Refund Actions ({refunds.length})
        </button>
      </div>

      {activeTab === 'all' ? (
        <>
          <div className="orders-filters-card">
        <div className="filters-header">
          <Filter size={20} />
          <span>Filters</span>
        </div>
        <div className="filters-grid">
          <div className="filter-group">
            <label>Status</label>
            <select value={filters.status} onChange={(e) => handleFilterChange('status', e.target.value)}>
              <option value="">All Statuses</option>
              {statusOptions.map(status => (
                <option key={status} value={status}>{status}</option>
              ))}
            </select>
          </div>
          <div className="filter-group">
            <label>Restaurant</label>
            <select value={filters.restaurantId} onChange={(e) => handleFilterChange('restaurantId', e.target.value)}>
              <option value="">All Restaurants</option>
              {restaurants.map(r => (
                <option key={r.id} value={r.id}>{r.name}</option>
              ))}
            </select>
          </div>
          <div className="filter-group">
            <label>From Date</label>
            <input
              type="date"
              value={filters.from}
              onChange={(e) => handleFilterChange('from', e.target.value)}
            />
          </div>
          <div className="filter-group">
            <label>To Date</label>
            <input
              type="date"
              value={filters.to}
              onChange={(e) => handleFilterChange('to', e.target.value)}
            />
          </div>
        </div>
        {(filters.status || filters.restaurantId || filters.from || filters.to) && (
          <button className="btn-clear-filters" onClick={clearFilters}>
            Clear Filters
          </button>
        )}
      </div>

      {orders.length === 0 ? (
        <div className="empty-state">
          <Package size={64} />
          <h3>No Orders Found</h3>
          <p>No orders match your current filters</p>
        </div>
      ) : (
        <div className="orders-list">
          {orders.map(order => (
            <div key={order.id} className="order-card">
              <div className="order-header" onClick={() => setExpandedOrder(expandedOrder === order.id ? null : order.id)}>
                <div className="order-id-section">
                  <span className="order-id">#{order.orderNumber || order.id.substring(0, 8)}</span>
                  <span className={`status-badge ${getStatusColor(order.status)}`}>
                    {order.status}
                  </span>
                </div>
                <div className="order-summary">
                  <div className="summary-item">
                    <MapPin size={16} />
                    <span>{order.restaurantName || `Restaurant #${order.restaurantId?.substring(0, 8)}`}</span>
                  </div>
                  <div className="summary-item">
                    <User size={16} />
                    <span>{order.customerName || `Customer #${order.customerId?.substring(0, 8) || 'N/A'}`}</span>
                  </div>
                  <div className="summary-item">
                    <DollarSign size={16} />
                    <strong>{formatCurrency(order.totalAmount)}</strong>
                  </div>
                  <div className="summary-item">
                    <Clock size={16} />
                    <span>{formatDate(order.createdAt)}</span>
                  </div>
                </div>
                <button className="expand-btn">
                  {expandedOrder === order.id ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
                </button>
              </div>

              {expandedOrder === order.id && (
                <div className="order-details">
                  <div className="details-grid">
                    <div className="detail-section">
                      <h4>Order Items</h4>
                      <div className="order-items">
                        {order.items && order.items.length > 0 ? (
                          order.items.map((item, idx) => (
                            <div key={idx} className="order-item">
                              <span className="item-name">
                                {item.quantity}x {item.name || item.menuItemName}
                                {item.isVeg && <span className="veg-badge"> 🟢</span>}
                              </span>
                              <span className="item-price">
                                {formatCurrency(item.lineTotal || (item.unitPrice * item.quantity) || (item.price * item.quantity))}
                              </span>
                            </div>
                          ))
                        ) : (
                          <p className="text-muted">No items</p>
                        )}
                      </div>
                    </div>

                    <div className="detail-section">
                      <h4>Delivery Address</h4>
                      <p className="address-text">
                        {order.deliveryAddress || 'N/A'}
                      </p>
                    </div>

                    <div className="detail-section">
                      <h4>Payment Details</h4>
                      <div className="payment-details">
                        <div className="payment-row">
                          <span>Subtotal:</span>
                          <span>{formatCurrency(order.subtotal || 0)}</span>
                        </div>
                        <div className="payment-row">
                          <span>Delivery Fee:</span>
                          <span>{formatCurrency(order.deliveryFee || 0)}</span>
                        </div>
                        <div className="payment-row">
                          <span>GST:</span>
                          <span>{formatCurrency(order.gstAmount || 0)}</span>
                        </div>
                        <div className="payment-row">
                          <span>Platform Fee:</span>
                          <span>{formatCurrency(order.platformFee || 15)}</span>
                        </div>
                        {order.discount > 0 && (
                          <div className="payment-row discount">
                            <span>Discount:</span>
                            <span>-{formatCurrency(order.discount)}</span>
                          </div>
                        )}
                        <div className="payment-row total">
                          <strong>Total:</strong>
                          <strong>{formatCurrency(order.totalAmount)}</strong>
                        </div>
                        <div className="payment-row">
                          <span>Payment Method:</span>
                          <span className="payment-method">{order.paymentMethod || 'N/A'}</span>
                        </div>
                        <div className="payment-row">
                          <span>Payment Status:</span>
                          <span className={`badge ${
                            (order.payment?.status === 'Success' || order.status === 'Paid' || order.status === 'Delivered' || order.status === 'Accepted' || order.status === 'Preparing' || order.status === 'ReadyForPickup' || order.status === 'PickedUp' || order.status === 'OutForDelivery') 
                              ? 'badge-success' 
                              : order.payment?.status === 'Failed' ? 'badge-error' : 'badge-warning'
                          }`}>
                            {order.payment?.status === 'Success' ? 'Success' :
                             (order.status === 'Paid' || order.status === 'Delivered' || order.status === 'Accepted' || order.status === 'Preparing' || order.status === 'ReadyForPickup' || order.status === 'PickedUp' || order.status === 'OutForDelivery') ? 'Success' :
                             order.payment?.status === 'Failed' ? 'Failed' :
                             order.paymentMethod === 'COD' ? 'COD (On Delivery)' : 'Pending'}
                          </span>
                        </div>
                      </div>
                    </div>

                    {order.deliveryAssignment && (
                      <div className="detail-section">
                        <h4>Delivery Agent</h4>
                        <p><strong>{order.deliveryAssignment.agentName || 'Not Assigned'}</strong></p>
                        {order.deliveryAssignment.agentMobile && (
                          <p className="text-muted">📞 {order.deliveryAssignment.agentMobile}</p>
                        )}
                        {order.deliveryAssignment.status && (
                          <p>Status: <span className="badge badge-info">{order.deliveryAssignment.status}</span></p>
                        )}
                      </div>
                    )}
                  </div>

                  {/* Admin cannot change order status - only view details */}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
        </>
      ) : (
        /* Refund Actions Tab */
        <div className="refunds-section">
          {refunds.length === 0 ? (
            <div className="empty-state">
              <DollarSign size={64} />
              <h3>No Pending Refunds</h3>
              <p>All refund requests have been processed</p>
            </div>
          ) : (
            <div className="refunds-list">
              {refunds.map(refund => (
                <div key={refund.id} className="refund-card">
                  <div className="refund-header-section">
                    <div className="refund-id-section">
                      <span className="refund-id">Refund #{refund.id.substring(0, 8)}</span>
                      <span className="status-badge warning">Pending Approval</span>
                    </div>
                    <div className="refund-amount">
                      <DollarSign size={20} />
                      <strong>{formatCurrency(refund.refundAmount || refund.amount)}</strong>
                    </div>
                  </div>

                  {refund.originalAmount && (
                    <div className="refund-breakdown">
                      <h4>Refund Calculation</h4>
                      <div className="breakdown-items">
                        <div className="breakdown-item">
                          <span className="breakdown-label">Original Amount:</span>
                          <span className="breakdown-value positive">{formatCurrency(refund.originalAmount)}</span>
                        </div>
                        <div className="breakdown-item">
                          <span className="breakdown-label">Platform Fee:</span>
                          <span className="breakdown-value negative">- {formatCurrency(refund.platformFee)}</span>
                        </div>
                        <div className="breakdown-item">
                          <span className="breakdown-label">Cancellation Charge (5%):</span>
                          <span className="breakdown-value negative">- {formatCurrency(refund.cancellationCharge)}</span>
                        </div>
                        <div className="breakdown-item total">
                          <span className="breakdown-label"><strong>Refund Amount:</strong></span>
                          <span className="breakdown-value"><strong>{formatCurrency(refund.refundAmount)}</strong></span>
                        </div>
                      </div>
                    </div>
                  )}

                  <div className="refund-details">
                    <div className="detail-row">
                      <User size={16} />
                      <div className="detail-content">
                        <span className="detail-label">Customer:</span>
                        <span className="detail-value">{refund.customerName || 'Unknown'}</span>
                      </div>
                    </div>

                    <div className="detail-row">
                      <Package size={16} />
                      <div className="detail-content">
                        <span className="detail-label">Order ID:</span>
                        <span className="detail-value">#{refund.orderId?.substring(0, 8)}</span>
                      </div>
                    </div>

                    <div className="detail-row">
                      <Clock size={16} />
                      <div className="detail-content">
                        <span className="detail-label">Requested:</span>
                        <span className="detail-value">{formatDate(refund.requestedAt)}</span>
                      </div>
                    </div>

                    {refund.orderCancellationReason && (
                      <div className="detail-row">
                        <div className="detail-content full-width">
                          <span className="detail-label">Cancellation Reason:</span>
                          <p className="detail-value">{refund.orderCancellationReason}</p>
                        </div>
                      </div>
                    )}
                  </div>

                  <div className="refund-actions">
                    <button
                      className="btn btn-success btn-sm"
                      onClick={() => handleRefundClick(refund, 'approve')}
                    >
                      <CheckCircle size={16} />
                      Approve Refund
                    </button>
                    <button
                      className="btn btn-danger btn-sm"
                      onClick={() => handleRefundClick(refund, 'reject')}
                    >
                      <XCircle size={16} />
                      Reject Refund
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Status Update Modal */}
      {statusUpdateModal.show && (
        <div className="modal-overlay" onClick={closeStatusModal}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Update Order Status</h3>
              <button className="modal-close" onClick={closeStatusModal}>&times;</button>
            </div>
            <form onSubmit={handleStatusUpdate}>
              <div className="modal-body">
                <div className="form-group">
                  <label>Current Status</label>
                  <input type="text" value={statusUpdateModal.currentStatus} disabled />
                </div>
                <div className="form-group">
                  <label>New Status *</label>
                  <select
                    value={statusForm.newStatus}
                    onChange={(e) => setStatusForm({ ...statusForm, newStatus: e.target.value })}
                    required
                  >
                    <option value="">Select new status</option>
                    {statusOptions.map(status => (
                      <option key={status} value={status}>{status}</option>
                    ))}
                  </select>
                </div>
                {['Cancelled', 'RefundInitiated', 'Refunded'].includes(statusForm.newStatus) && (
                  <div className="form-group">
                    <label>Reason *</label>
                    <textarea
                      value={statusForm.reason}
                      onChange={(e) => setStatusForm({ ...statusForm, reason: e.target.value })}
                      placeholder="Enter reason for status change"
                      rows="3"
                      required
                    />
                  </div>
                )}
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-outline" onClick={closeStatusModal}>
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary">
                  Update Status
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Refund Process Modal */}
      {showRefundModal && (
        <div className="modal-overlay" onClick={() => setShowRefundModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{refundAction === 'approve' ? 'Approve' : 'Reject'} Refund</h3>
              <button className="modal-close" onClick={() => setShowRefundModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <div className="detail-section">
                <h4>Refund Information</h4>
                <div className="detail-grid">
                  {selectedRefund?.originalAmount ? (
                    <>
                      <div className="detail-item">
                        <span className="detail-label">Original Amount:</span>
                        <span className="detail-value">{formatCurrency(selectedRefund?.originalAmount)}</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Platform Fee:</span>
                        <span className="detail-value negative">- {formatCurrency(selectedRefund?.platformFee)}</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Cancellation Charge:</span>
                        <span className="detail-value negative">- {formatCurrency(selectedRefund?.cancellationCharge)}</span>
                      </div>
                      <div className="detail-item highlight">
                        <span className="detail-label"><strong>Refund Amount:</strong></span>
                        <span className="detail-value"><strong>{formatCurrency(selectedRefund?.refundAmount)}</strong></span>
                      </div>
                    </>
                  ) : (
                    <div className="detail-item">
                      <span className="detail-label">Amount:</span>
                      <span className="detail-value">{formatCurrency(selectedRefund?.amount)}</span>
                    </div>
                  )}
                  <div className="detail-item">
                    <span className="detail-label">Customer:</span>
                    <span className="detail-value">{selectedRefund?.customerName}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Order ID:</span>
                    <span className="detail-value">#{selectedRefund?.orderId?.substring(0, 8)}</span>
                  </div>
                </div>
              </div>
              
              {refundAction === 'approve' && (
                <div className="alert alert-info">
                  <p><strong>Note:</strong> Approving this refund will credit {formatCurrency(selectedRefund?.refundAmount || selectedRefund?.amount)} to the customer's wallet.</p>
                </div>
              )}

              <div className="form-group">
                <label>Admin Notes (Optional):</label>
                <textarea
                  value={adminNotes}
                  onChange={(e) => setAdminNotes(e.target.value)}
                  placeholder={`Enter notes for ${refundAction}ing this refund...`}
                  rows={4}
                  className="form-control"
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => setShowRefundModal(false)}>
                Cancel
              </button>
              <button
                className={`btn ${refundAction === 'approve' ? 'btn-success' : 'btn-danger'}`}
                onClick={handleRefundProcess}
              >
                Confirm {refundAction === 'approve' ? 'Approval' : 'Rejection'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
