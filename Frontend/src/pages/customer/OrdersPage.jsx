import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Package, Clock, CheckCircle, XCircle, Trash2, RotateCcw, Eye } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import './OrdersPage.css';

export default function OrdersPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [orders, setOrders] = useState([]);
  const [activeTab, setActiveTab] = useState('active');
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [orderToDelete, setOrderToDelete] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    loadOrders();
  }, [activeTab]);

  const loadOrders = async () => {
    try {
      setLoading(true);
      const res = await api.get(`/gateway/orders/orders/my/filtered?filter=${activeTab}`);
      const ordersData = res.data?.data || res.data;
      setOrders(Array.isArray(ordersData) ? ordersData : []);
    } catch (error) {
      console.error('Failed to load orders:', error);
      toast.error('Failed to load orders');
    } finally {
      setLoading(false);
    }
  };

  const handleReorder = async (orderId) => {
    try {
      const res = await api.post(`/gateway/orders/orders/${orderId}/reorder`);
      const result = res.data?.data || res.data;
      toast.success(result.message || 'Items added to cart!');
      navigate('/cart');
    } catch (error) {
      console.error('Failed to reorder:', error);
      const errorMsg = error.response?.data?.message || 'Failed to reorder';
      toast.error(errorMsg);
    }
  };

  const handleDeleteClick = (order) => {
    setOrderToDelete(order);
    setShowDeleteModal(true);
  };

  const handleDeleteOrder = async () => {
    setIsDeleting(true);
    try {
      await api.delete(`/gateway/orders/orders/${orderToDelete.id}`);
      toast.success('Order deleted successfully');
      setShowDeleteModal(false);
      setOrderToDelete(null);
      loadOrders();
    } catch (error) {
      console.error('Failed to delete order:', error);
      const errorMsg = error.response?.data?.message || 'Failed to delete order';
      toast.error(errorMsg);
    } finally {
      setIsDeleting(false);
    }
  };

  const getStatusBadgeClass = (status) => {
    const statusMap = {
      'PaymentPending': 'badge-warning',
      'Paid': 'badge-info',
      'AwaitingAcceptance': 'badge-info',
      'Accepted': 'badge-info',
      'Preparing': 'badge-primary',
      'ReadyForPickup': 'badge-success',
      'PickedUp': 'badge-success',
      'OutForDelivery': 'badge-success',
      'Delivered': 'badge-success',
      'Cancelled': 'badge-error',
      'PaymentFailed': 'badge-error',
      'RestaurantRejected': 'badge-error'
    };
    return statusMap[status] || 'badge-secondary';
  };

  const getStatusLabel = (status) => {
    const labels = {
      'PaymentPending': 'Payment Pending',
      'Paid': 'Paid',
      'AwaitingAcceptance': 'Awaiting Acceptance',
      'Accepted': 'Accepted',
      'Preparing': 'Preparing',
      'ReadyForPickup': 'Ready for Pickup',
      'PickedUp': 'Picked Up',
      'OutForDelivery': 'Out for Delivery',
      'Delivered': 'Delivered',
      'Cancelled': 'Cancelled',
      'PaymentFailed': 'Payment Failed',
      'RestaurantRejected': 'Rejected by Restaurant'
    };
    return labels[status] || status;
  };

  const getStatusIcon = (status) => {
    if (['Delivered'].includes(status)) return <CheckCircle size={20} />;
    if (['Cancelled', 'PaymentFailed', 'RestaurantRejected'].includes(status)) return <XCircle size={20} />;
    if (['Preparing', 'ReadyForPickup', 'PickedUp', 'OutForDelivery'].includes(status)) return <Clock size={20} />;
    return <Package size={20} />;
  };

  const tabs = [
    { id: 'active', label: 'Active Orders', icon: Clock },
    { id: 'completed', label: 'Completed', icon: CheckCircle },
    { id: 'rejected', label: 'Rejected/Cancelled', icon: XCircle },
  ];

  if (loading) {
    return (
      <div className="orders-page page-enter">
        <div className="container">
          <div className="loading-spinner">Loading orders...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="orders-page page-enter">
      <div className="container">
        <div className="page-header">
          <h1 className="headline-xl">My Orders</h1>
        </div>

        {/* Tabs */}
        <div className="tabs">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <button
                key={tab.id}
                className={`tab ${activeTab === tab.id ? 'active' : ''}`}
                onClick={() => setActiveTab(tab.id)}
              >
                <Icon size={18} />
                {tab.label}
              </button>
            );
          })}
        </div>

        {/* Orders List */}
        <div className="orders-list">
          {orders.length === 0 ? (
            <div className="empty-state">
              <Package size={64} className="text-muted" />
              <h2 className="headline-lg">No Orders Found</h2>
              <p className="body-lg text-muted">
                {activeTab === 'active' && "You don't have any active orders."}
                {activeTab === 'completed' && "You haven't completed any orders yet."}
                {activeTab === 'rejected' && "You don't have any rejected or cancelled orders."}
              </p>
            </div>
          ) : (
            orders.map((order) => (
              <div key={order.id} className="order-card">
                <div className="order-card-header">
                  <div className="order-info">
                    <h3 className="headline-md">{order.restaurantName}</h3>
                    <p className="body-sm text-muted">
                      Order #{order.id.substring(0, 8).toUpperCase()} • {new Date(order.createdAt).toLocaleDateString()}
                    </p>
                  </div>
                  <span className={`badge ${getStatusBadgeClass(order.status)}`}>
                    {getStatusIcon(order.status)}
                    {getStatusLabel(order.status)}
                  </span>
                </div>

                <div className="order-card-body">
                  <div className="order-items">
                    <p className="body-md">
                      {order.items?.length || 0} items • ₹{order.totalAmount?.toFixed(2)}
                    </p>
                    <p className="body-sm text-muted">
                      {order.items?.slice(0, 2).map(item => item.name).join(', ')}
                      {order.items?.length > 2 && ` +${order.items.length - 2} more`}
                    </p>
                  </div>

                  {order.status === 'RestaurantRejected' && order.rejectionReason && (
                    <div className="rejection-reason">
                      <XCircle size={16} />
                      <span className="body-sm">Reason: {order.rejectionReason}</span>
                    </div>
                  )}

                  {order.deliveryAddress && (
                    <p className="body-sm text-muted delivery-address">
                      📍 {order.deliveryAddress}
                    </p>
                  )}
                </div>

                <div className="order-card-actions">
                  <button
                    className="btn btn-outline btn-sm"
                    onClick={() => navigate(`/orders/${order.id}`)}
                  >
                    <Eye size={16} /> View Details
                  </button>

                  {(activeTab === 'completed' || activeTab === 'rejected') && (
                    <>
                      <button
                        className="btn btn-primary btn-sm"
                        onClick={() => handleReorder(order.id)}
                      >
                        <RotateCcw size={16} /> Reorder
                      </button>
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={() => handleDeleteClick(order)}
                      >
                        <Trash2 size={16} /> Delete
                      </button>
                    </>
                  )}

                  {activeTab === 'active' && (
                    <button
                      className="btn btn-primary btn-sm"
                      onClick={() => navigate(`/orders/${order.id}`)}
                    >
                      Track Order
                    </button>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteModal && (
        <div className="modal-overlay" onClick={() => setShowDeleteModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2 className="headline-lg">Delete Order?</h2>
            <p className="body-md text-muted">
              Are you sure you want to delete this order from your history? This action cannot be undone.
            </p>
            <p className="body-sm">
              Order #{orderToDelete?.id.substring(0, 8).toUpperCase()} from {orderToDelete?.restaurantName}
            </p>

            <div className="modal-actions">
              <button
                className="btn btn-text"
                onClick={() => setShowDeleteModal(false)}
                disabled={isDeleting}
              >
                Cancel
              </button>
              <button
                className="btn btn-error"
                onClick={handleDeleteOrder}
                disabled={isDeleting}
              >
                {isDeleting ? 'Deleting...' : 'Delete Order'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
