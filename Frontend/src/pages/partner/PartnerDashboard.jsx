import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Store, Menu, Clock, Tag, DollarSign, ShoppingBag, Star, RefreshCw, UtensilsCrossed } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import toast from 'react-hot-toast';
import {
  StatCard, ActionCard, DashSectionHeader, DashOrdersTable,
  DonutChart, MiniBarChart,
} from '../../components/common/DashboardWidgets';
import '../../components/common/DashboardWidgets.css';
import './PartnerDashboard.css';

function StatusBadge({ status }) {
  const map = {
    Delivered: 'badge-success', Cancelled: 'badge-error',
    Preparing: 'badge-primary', OutForDelivery: 'badge-secondary',
    Accepted: 'badge-info', AwaitingAcceptance: 'badge-warning',
    Paid: 'badge-info', PaymentPending: 'badge-warning',
    RestaurantRejected: 'badge-error',
  };
  const labels = { AwaitingAcceptance: 'Awaiting', OutForDelivery: 'On Way', RestaurantRejected: 'Rejected' };
  return (
    <span className={`badge ${map[status] || 'badge-secondary'}`} style={{ fontSize: '0.6875rem' }}>
      {labels[status] || status}
    </span>
  );
}

export default function PartnerDashboard() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [stats, setStats] = useState({
    totalOrders: 0, pendingOrders: 0, revenue: 0,
    avgRating: 0, totalMenuItems: 0, activeCoupons: 0,
  });
  const [restaurant, setRestaurant] = useState(null);
  const [recentOrders, setRecentOrders] = useState([]);
  const [statusDonut, setStatusDonut] = useState([]);
  const [revenueData, setRevenueData] = useState([]);

  const loadDashboardData = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    else setRefreshing(true);
    try {
      const restaurantsRes = await api.get(API_ENDPOINTS.catalog.restaurantsMyPartner);
      const restaurantData = restaurantsRes.data?.data || restaurantsRes.data;
      const restaurantsList = Array.isArray(restaurantData) ? restaurantData : [];
      const myRestaurant = restaurantsList[0];

      if (!myRestaurant) {
        if (!silent) toast.error('No restaurant found. Please create one first.');
        setLoading(false); setRefreshing(false);
        return;
      }
      setRestaurant(myRestaurant);

      const ordersRes = await api.get(API_ENDPOINTS.orders.ordersByRestaurant(myRestaurant.id));
      const orders = ordersRes.data?.data || ordersRes.data || [];
      const orderList = Array.isArray(orders) ? orders : [];
      setRecentOrders(orderList.slice(0, 8));

      // Stats
      const pendingStatuses = ['Paid', 'AwaitingAcceptance', 'Accepted', 'Preparing', 'ReadyForPickup'];
      const pendingCount = orderList.filter(o => pendingStatuses.includes(o.status)).length;
      const totalRevenue = orderList.filter(o => o.status === 'Delivered').reduce((s, o) => s + (o.totalAmount || 0), 0);

      const ratedOrders = orderList.filter(o => o.rating?.foodRating);
      const avgRating = ratedOrders.length
        ? ratedOrders.reduce((s, o) => s + ((o.rating.foodRating + (o.rating.deliveryRating || o.rating.foodRating)) / 2), 0) / ratedOrders.length
        : myRestaurant.rating || 0;

      const menuRes = await api.get(`${API_ENDPOINTS.catalog.menuItems}?restaurantId=${myRestaurant.id}`);
      const menuItems = Array.isArray(menuRes.data?.data || menuRes.data) ? (menuRes.data?.data || menuRes.data) : [];

      const couponsRes = await api.get(API_ENDPOINTS.orders.couponsByRestaurant(myRestaurant.id));
      const coupons = Array.isArray(couponsRes.data?.data || couponsRes.data) ? (couponsRes.data?.data || couponsRes.data) : [];

      setStats({
        totalOrders: orderList.length,
        pendingOrders: pendingCount,
        revenue: totalRevenue,
        avgRating,
        totalMenuItems: menuItems.length,
        activeCoupons: coupons.filter(c => c.isActive).length,
      });

      // Status donut
      const statusCounts = {};
      orderList.forEach(o => { statusCounts[o.status] = (statusCounts[o.status] || 0) + 1; });
      const donutColors = {
        Delivered: '#2e7d32', Preparing: 'var(--primary)', Cancelled: 'var(--error)',
        AwaitingAcceptance: '#e65100', Accepted: 'var(--tertiary)', OutForDelivery: 'var(--secondary)',
      };
      setStatusDonut(
        Object.entries(statusCounts).slice(0, 5).map(([status, count]) => ({
          label: status, value: count, color: donutColors[status] || 'var(--outline-variant)',
        }))
      );

      // Revenue bar chart (last 7 days)
      const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
      const today = new Date().getDay();
      const revenueByDay = Array(7).fill(0);
      orderList.filter(o => o.status === 'Delivered').forEach(o => {
        const d = new Date(o.createdAt || o.placedAt);
        const dayIdx = d.getDay() === 0 ? 6 : d.getDay() - 1;
        revenueByDay[dayIdx] += o.totalAmount || 0;
      });
      setRevenueData(days.map((label, i) => ({
        label, value: Math.round(revenueByDay[i]),
        color: i === (today === 0 ? 6 : today - 1) ? 'var(--primary)' : 'var(--primary-fixed-dim)',
      })));

    } catch (error) {
      console.error('[PartnerDashboard] Failed to load:', error);
      if (!silent) toast.error('Failed to load dashboard data');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { loadDashboardData(); }, [loadDashboardData]);

  const orderColumns = [
    { key: 'id', label: 'Order ID', render: r => `#${r.id.substring(0, 8)}` },
    { key: 'customerName', label: 'Customer', render: r => r.customerName || 'Customer' },
    { key: 'items', label: 'Items', render: r => `${r.items?.length || 0} items` },
    { key: 'totalAmount', label: 'Amount', render: r => `₹${r.totalAmount?.toFixed(2)}` },
    { key: 'status', label: 'Status', render: r => <StatusBadge status={r.status} /> },
    { key: 'createdAt', label: 'Date', render: r => new Date(r.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }) },
  ];

  if (loading) {
    return (
      <div className="dash-page">
        <div className="container">
          <div style={{ marginBottom: 'var(--space-xl)' }}>
            <div className="skeleton" style={{ height: '2rem', width: '16rem', marginBottom: '0.5rem' }} />
            <div className="skeleton" style={{ height: '1rem', width: '12rem' }} />
          </div>
          <div className="dash-stats-grid">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="dash-stat-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem' }}>
                  <div className="skeleton" style={{ height: '2.75rem', width: '2.75rem', borderRadius: 'var(--rounded-xl)' }} />
                  <div className="skeleton" style={{ height: '2.25rem', width: '4.5rem' }} />
                </div>
                <div className="skeleton" style={{ height: '2rem', width: '60%', marginBottom: '0.5rem' }} />
                <div className="skeleton" style={{ height: '0.875rem', width: '50%' }} />
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (!restaurant) {
    return (
      <div className="dash-page">
        <div className="container">
          <div className="empty-state">
            <Store size={64} style={{ color: 'var(--outline)', opacity: 0.5 }} />
            <h2 className="headline-lg">No Restaurant Found</h2>
            <p className="body-lg text-muted">You haven't registered a restaurant yet.</p>
            <button className="btn btn-primary" onClick={() => navigate('/partner/restaurant/new')}>
              <Store size={18} /> Register Restaurant
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="dash-page">
      <div className="container">

        {/* ── Page Header ── */}
        <div className="dash-page-header">
          <div>
            <h1 className="dash-page-title">{restaurant.name}</h1>
            <p className="dash-page-sub">
              {restaurant.cuisineType || restaurant.cuisine || 'Multi Cuisine'}
              {restaurant.address && ` · ${restaurant.address}`}
            </p>
          </div>
          <div className="dash-page-actions">
            <span className={`badge ${restaurant.isOpen ? 'badge-success' : 'badge-error'}`} style={{ fontSize: '0.8125rem', padding: '0.375rem 1rem' }}>
              {restaurant.isOpen ? '● Open' : '● Closed'}
            </span>
            <button className="btn btn-outline btn-sm" onClick={() => navigate('/partner/restaurant')}>
              <Store size={16} /> Manage
            </button>
            <button
              className={`odp-refresh-btn ${refreshing ? 'spinning' : ''}`}
              onClick={() => loadDashboardData(true)}
              title="Refresh"
            >
              <RefreshCw size={16} />
            </button>
          </div>
        </div>

        {/* ── Stat Cards ── */}
        <div className="dash-stats-grid">
          <StatCard
            label="Total Orders"
            value={stats.totalOrders.toLocaleString()}
            icon={ShoppingBag}
            iconBg="var(--primary-fixed)"
            iconColor="var(--primary)"
            accent="primary"
            onClick={() => navigate('/partner/orders')}
          />
          <StatCard
            label="Pending Orders"
            value={stats.pendingOrders.toLocaleString()}
            icon={Clock}
            iconBg="var(--warning-container)"
            iconColor="var(--warning)"
            accent="warning"
            badge={stats.pendingOrders > 0 ? 'Needs attention' : null}
            badgeVariant="warning"
            onClick={() => navigate('/partner/orders')}
          />
          <StatCard
            label="Total Revenue"
            value={`₹${(stats.revenue / 1000).toFixed(1)}K`}
            icon={DollarSign}
            iconBg="var(--success-container)"
            iconColor="var(--success)"
            accent="success"
            sparkData={revenueData.map(d => d.value)}
            sparkColor="var(--success)"
          />
          <StatCard
            label="Avg Rating"
            value={stats.avgRating > 0 ? stats.avgRating.toFixed(1) : 'New'}
            sublabel={stats.avgRating > 0 ? '★ out of 5' : 'No ratings yet'}
            icon={Star}
            iconBg="linear-gradient(135deg, #fff8e1, #ffe082)"
            iconColor="#f59e0b"
            accent="warning"
          />
          <StatCard
            label="Menu Items"
            value={stats.totalMenuItems.toLocaleString()}
            icon={UtensilsCrossed}
            iconBg="var(--secondary-fixed)"
            iconColor="var(--secondary)"
            accent="secondary"
            onClick={() => navigate('/partner/menu')}
          />
          <StatCard
            label="Active Coupons"
            value={stats.activeCoupons.toLocaleString()}
            icon={Tag}
            iconBg="var(--tertiary-container)"
            iconColor="var(--tertiary)"
            accent="tertiary"
            onClick={() => navigate('/partner/coupons')}
          />
        </div>

        {/* ── Charts Row ── */}
        {revenueData.length > 0 && (
          <div className="dash-charts-row">
            <div className="dash-chart-card">
              <p className="dash-chart-title">Revenue This Week</p>
              <MiniBarChart data={revenueData} color="var(--primary)" height={80} />
            </div>
            {statusDonut.length > 0 && (
              <div className="dash-donut-card">
                <p className="dash-chart-title" style={{ margin: 0 }}>Order Breakdown</p>
                <DonutChart
                  segments={statusDonut}
                  size={110}
                  thickness={14}
                  label={stats.totalOrders.toString()}
                  sublabel="total"
                />
                <div className="dash-donut-legend">
                  {statusDonut.slice(0, 4).map(seg => (
                    <div key={seg.label} className="ddl-item">
                      <div className="ddl-dot" style={{ background: seg.color }} />
                      <span className="ddl-label">{seg.label}</span>
                      <span className="ddl-value">{seg.value}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {/* ── Quick Actions ── */}
        <div className="dash-section">
          <DashSectionHeader title="Quick Actions" />
          <div className="dash-actions-grid">
            <ActionCard icon={ShoppingBag} iconBg="var(--primary-fixed)" iconColor="var(--primary)"
              title="Manage Orders" desc="View and update order status"
              badge={stats.pendingOrders > 0 ? stats.pendingOrders : null}
              onClick={() => navigate('/partner/orders')} />
            <ActionCard icon={UtensilsCrossed} iconBg="var(--secondary-fixed)" iconColor="var(--secondary)"
              title="Menu" desc="Add or edit menu items"
              onClick={() => navigate('/partner/menu')} />
            <ActionCard icon={Tag} iconBg="var(--tertiary-container)" iconColor="var(--tertiary)"
              title="Coupons" desc="Create discount coupons"
              onClick={() => navigate('/partner/coupons')} />
            <ActionCard icon={Clock} iconBg="var(--warning-container)" iconColor="var(--warning)"
              title="Hours" desc="Set restaurant timings"
              onClick={() => navigate('/partner/hours')} />
          </div>
        </div>

        {/* ── Recent Orders ── */}
        {recentOrders.length > 0 && (
          <div className="dash-section">
            <DashSectionHeader
              title="Recent Orders"
              subtitle={`${recentOrders.length} most recent`}
              action={() => navigate('/partner/orders')}
            />
            <DashOrdersTable
              orders={recentOrders}
              columns={orderColumns}
              onRowClick={row => navigate(`/partner/orders/${row.id}`)}
            />
          </div>
        )}

      </div>
    </div>
  );
}
