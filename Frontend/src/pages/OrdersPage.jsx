import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Package, Clock, ChevronRight, RefreshCw } from 'lucide-react';
import api from '../services/api';
import { API_ENDPOINTS } from '../config/api';
import './OrdersPage.css';

const statusColors = {
  pending: 'badge-warning',
  confirmed: 'badge-secondary',
  preparing: 'badge-primary',
  ready: 'badge-success',
  pickedup: 'badge-secondary',
  outfordelivery: 'badge-primary',
  delivered: 'badge-success',
  cancelled: 'badge-error',
};

export default function OrdersPage() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);

  const fetchOrders = async () => {
    setLoading(true);
    try {
      const { data } = await api.get(API_ENDPOINTS.orders.myOrders);
      setOrders(Array.isArray(data) ? data : data?.orders || data?.items || []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchOrders();
  }, []);

  return (
    <div className="orders-page page-enter">
      <div className="container">
        <div className="op-header">
          <h1 className="headline-lg">My Orders</h1>
          <button className="btn btn-ghost btn-sm" onClick={fetchOrders}>
            <RefreshCw size={16} /> Refresh
          </button>
        </div>

        {loading ? (
          <div className="op-list">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="skeleton" style={{ height: 120, borderRadius: 'var(--rounded-xl)', marginBottom: 'var(--space-md)' }} />
            ))}
          </div>
        ) : orders.length === 0 ? (
          <div className="op-empty">
            <span className="op-empty-icon">📦</span>
            <h3 className="headline-md">No orders yet</h3>
            <p className="body-md text-muted">Start ordering from your favourite restaurants</p>
            <Link to="/restaurants" className="btn btn-primary">Browse Restaurants</Link>
          </div>
        ) : (
          <div className="op-list">
            {orders.map((order) => (
              <Link key={order.id} to={`/orders/${order.id}`} className="order-card card" id={`order-${order.id}`}>
                <div className="card-body oc-content">
                  <div className="oc-left">
                    <div className="oc-icon">
                      <Package size={24} />
                    </div>
                    <div>
                      <h4 className="headline-sm">{order.restaurantName || 'Order'}</h4>
                      <p className="body-sm text-muted">
                        {order.items?.length || 0} items • ₹{order.totalAmount?.toFixed(2) || order.total?.toFixed(2) || '0.00'}
                      </p>
                      <p className="body-sm text-muted" style={{ fontSize: 12 }}>
                        <Clock size={12} style={{ display: 'inline', verticalAlign: -2 }} />{' '}
                        {new Date(order.createdAt || order.orderDate).toLocaleDateString('en-IN', {
                          day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
                        })}
                      </p>
                    </div>
                  </div>
                  <div className="oc-right">
                    <span className={`badge ${statusColors[(order.status || '').toLowerCase()] || 'badge-primary'}`}>
                      {order.status}
                    </span>
                    <ChevronRight size={20} className="text-muted" />
                  </div>
                </div>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
