import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Users, Store, ShoppingBag, DollarSign, TrendingUp,
  Package, Truck, CheckCircle, Clock, AlertCircle,
  RefreshCw, BarChart2, Shield
} from 'lucide-react';
import api, { apiService } from '../../services/api';
import toast from 'react-hot-toast';
import {
  StatCard, ActionCard, DashSectionHeader, DashOrdersTable,
  DonutChart, MiniBarChart, DateRangeTabs,
} from '../../components/common/DashboardWidgets';
import '../../components/common/DashboardWidgets.css';
import './AdminDashboard.css';

// ── Status badge helper ──────────────────────────────────────────────
function StatusBadge({ status }) {
  const map = {
    Delivered: 'badge-success', Cancelled: 'badge-error',
    RestaurantRejected: 'badge-error', PaymentFailed: 'badge-error',
    Preparing: 'badge-primary', OutForDelivery: 'badge-secondary',
    PickedUp: 'badge-secondary', Accepted: 'badge-info',
    AwaitingAcceptance: 'badge-warning', Paid: 'badge-info',
    PaymentPending: 'badge-warning', CancelRequested: 'badge-warning',
    RefundInitiated: 'badge-warning', Refunded: 'badge-secondary',
  };
  const labels = {
    AwaitingAcceptance: 'Awaiting', OutForDelivery: 'On Way',
    RestaurantRejected: 'Rejected', PaymentFailed: 'Pmt Failed',
    PaymentPending: 'Pmt Pending', CancelRequested: 'Cancel Req',
    RefundInitiated: 'Refunding',
  };
  return (
    <span className={`badge ${map[status] || 'badge-secondary'}`} style={{ fontSize: '0.6875rem' }}>
      {labels[status] || status}
    </span>
  );
}

export default function AdminDashboard() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [dateRange, setDateRange] = useState('week');
  const [dashboard, setDashboard] = useState({
    totalUsers: 0, totalRestaurants: 0, totalOrders: 0,
    totalRevenue: 0, adminRevenue: 0, adminRevenueToday: 0,
    pendingRestaurants: 0, activeDeliveryAgents: 0, pendingAgents: 0,
    todayOrders: 0, todayRevenue: 0,
  });
  const [recentOrders, setRecentOrders] = useState([]);
  const [orderStatusData, setOrderStatusData] = useState([]);
  const [revenueData, setRevenueData] = useState([]);

  const loadDashboard = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    else setRefreshing(true);
    try {
      const dashRes = await apiService.admin.getDashboard();
      const dashData = dashRes.data?.data || {};

      let pendingAgentsCount = 0;
      try {
        const pendingRes = await api.get('/gateway/admin/delivery-agents/pending');
        pendingAgentsCount = (pendingRes.data || []).length;
      } catch {}

      setDashboard({
        totalUsers: dashData.totalUsers || 0,
        totalRestaurants: dashData.totalRestaurants || 0,
        totalOrders: dashData.totalOrders || 0,
        totalRevenue: dashData.totalRevenue || 0,
        adminRevenue: dashData.adminRevenue || 0,
        adminRevenueToday: dashData.adminRevenueToday || 0,
        pendingRestaurants: dashData.pendingRestaurants || 0,
        activeDeliveryAgents: dashData.activeDeliveryAgents || 0,
        pendingAgents: pendingAgentsCount,
        todayOrders: dashData.todayOrders || 0,
        todayRevenue: dashData.todayRevenue || 0,
      });

      // Recent orders
      const ordersRes = await api.get('/gateway/orders/orders');
      const allOrders = ordersRes.data?.data || [];
      const sorted = [...allOrders].sort((a, b) =>
        new Date(b.createdAt || b.placedAt || 0) - new Date(a.createdAt || a.placedAt || 0)
      );
      setRecentOrders(sorted.slice(0, 10));

      // Build order status donut data
      const statusCounts = {};
      allOrders.forEach(o => { statusCounts[o.status] = (statusCounts[o.status] || 0) + 1; });
      const donutColors = {
        Delivered: '#2e7d32', Preparing: 'var(--primary)', OutForDelivery: 'var(--secondary)',
        Cancelled: 'var(--error)', AwaitingAcceptance: '#e65100', Accepted: 'var(--tertiary)',
      };
      setOrderStatusData(
        Object.entries(statusCounts)
          .sort((a, b) => b[1] - a[1])
          .slice(0, 6)
          .map(([status, count]) => ({
            label: status, value: count,
            color: donutColors[status] || 'var(--outline-variant)',
          }))
      );

      // Build revenue bar chart (last 7 days)
      const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
      const today = new Date().getDay();
      const revenueByDay = Array(7).fill(0);
      allOrders.filter(o => o.status === 'Delivered').forEach(o => {
        const d = new Date(o.createdAt || o.placedAt);
        const dayIdx = d.getDay() === 0 ? 6 : d.getDay() - 1;
        revenueByDay[dayIdx] += o.totalAmount || 0;
      });
      setRevenueData(days.map((label, i) => ({
        label,
        value: Math.round(revenueByDay[i]),
        color: i === (today === 0 ? 6 : today - 1) ? 'var(--primary)' : 'var(--primary-fixed-dim)',
      })));

    } catch (error) {
      console.error('Failed to load dashboard:', error);
      if (!silent) toast.error('Failed to load dashboard data');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { loadDashboard(); }, [loadDashboard]);

  const totalDonut = orderStatusData.reduce((s, d) => s + d.value, 0);

  // ── Table columns ────────────────────────────────────────────────
  const orderColumns = [
    { key: 'id', label: 'Order ID', width: '100px', render: r => `#${r.id.substring(0, 8)}` },
    { key: 'restaurantName', label: 'Restaurant', render: r => r.restaurantName || '—' },
    { key: 'customerName', label: 'Customer', render: r => r.customerName || `#${r.customerId?.substring(0, 6)}` },
    { key: 'totalAmount', label: 'Amount', render: r => `₹${r.totalAmount?.toFixed(2)}` },
    { key: 'status', label: 'Status', render: r => <StatusBadge status={r.status} /> },
    {
      key: 'createdAt', label: 'Date',
      render: r => new Date(r.createdAt || r.placedAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }),
    },
  ];

  if (loading) {
    return (
      <div className="dash-page">
        <div className="container">
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 'var(--space-xl)' }}>
            <div>
              <div className="skeleton" style={{ height: '2rem', width: '14rem', marginBottom: '0.5rem' }} />
              <div className="skeleton" style={{ height: '1rem', width: '10rem' }} />
            </div>
          </div>
          <div className="dash-stats-grid">
            {Array.from({ length: 7 }).map((_, i) => (
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

  return (
    <div className="dash-page">
      <div className="container">

        {/* ── Page Header ── */}
        <div className="dash-page-header">
          <div>
            <h1 className="dash-page-title">Admin Dashboard</h1>
            <p className="dash-page-sub">Platform overview & management</p>
          </div>
          <div className="dash-page-actions">
            <DateRangeTabs value={dateRange} onChange={setDateRange} />
            <button
              className={`odp-refresh-btn ${refreshing ? 'spinning' : ''}`}
              onClick={() => loadDashboard(true)}
              title="Refresh"
            >
              <RefreshCw size={16} />
            </button>
          </div>
        </div>

        {/* ── Stat Cards ── */}
        <div className="dash-stats-grid">
          <StatCard
            label="Total Users"
            value={dashboard.totalUsers.toLocaleString()}
            sublabel={`+${dashboard.todayOrders} today`}
            icon={Users}
            iconBg="var(--primary-fixed)"
            iconColor="var(--primary)"
            accent="primary"
            onClick={() => navigate('/admin/users')}
          />
          <StatCard
            label="Restaurants"
            value={dashboard.totalRestaurants.toLocaleString()}
            icon={Store}
            iconBg="var(--secondary-fixed)"
            iconColor="var(--secondary)"
            accent="secondary"
            badge={dashboard.pendingRestaurants > 0 ? `${dashboard.pendingRestaurants} pending` : null}
            badgeVariant="warning"
            onClick={() => navigate('/admin/restaurants')}
          />
          <StatCard
            label="Total Orders"
            value={dashboard.totalOrders.toLocaleString()}
            sublabel={`Today: ${dashboard.todayOrders}`}
            icon={ShoppingBag}
            iconBg="var(--tertiary-container)"
            iconColor="var(--tertiary)"
            accent="tertiary"
            onClick={() => navigate('/admin/orders')}
          />
          <StatCard
            label="Total Revenue"
            value={`₹${(dashboard.totalRevenue / 1000).toFixed(1)}K`}
            sublabel={`Today: ₹${dashboard.todayRevenue.toFixed(0)}`}
            icon={DollarSign}
            iconBg="var(--success-container)"
            iconColor="var(--success)"
            accent="success"
            sparkData={revenueData.map(d => d.value)}
            sparkColor="var(--success)"
          />
          <StatCard
            label="Admin Revenue"
            value={`₹${(dashboard.adminRevenue / 1000).toFixed(1)}K`}
            sublabel={`Today: ₹${dashboard.adminRevenueToday.toFixed(0)}`}
            icon={TrendingUp}
            iconBg="var(--primary-fixed)"
            iconColor="var(--primary)"
            accent="primary"
          />
          <StatCard
            label="Delivery Agents"
            value={dashboard.activeDeliveryAgents.toLocaleString()}
            icon={Truck}
            iconBg="var(--warning-container)"
            iconColor="var(--warning)"
            accent="warning"
            badge={dashboard.pendingAgents > 0 ? `${dashboard.pendingAgents} pending` : null}
            badgeVariant="warning"
            onClick={() => navigate('/admin/delivery-agents')}
          />
          <StatCard
            label="Reports"
            value="View"
            sublabel="Sales & analytics"
            icon={BarChart2}
            iconBg="var(--error-container)"
            iconColor="var(--error)"
            accent="error"
            onClick={() => navigate('/admin/reports')}
          />
        </div>

        {/* ── Charts Row ── */}
        {revenueData.length > 0 && (
          <div className="dash-charts-row">
            {/* Revenue bar chart */}
            <div className="dash-chart-card">
              <p className="dash-chart-title">Revenue This Week</p>
              <MiniBarChart data={revenueData} color="var(--primary)" height={80} />
            </div>

            {/* Order status donut */}
            {orderStatusData.length > 0 && (
              <div className="dash-donut-card">
                <p className="dash-chart-title" style={{ margin: 0 }}>Order Status</p>
                <DonutChart
                  segments={orderStatusData}
                  size={120}
                  thickness={16}
                  label={totalDonut.toString()}
                  sublabel="orders"
                />
                <div className="dash-donut-legend">
                  {orderStatusData.slice(0, 4).map(seg => (
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
            <ActionCard
              icon={Users}
              iconBg="var(--primary-fixed)"
              iconColor="var(--primary)"
              title="Manage Users"
              desc="View and manage all users"
              onClick={() => navigate('/admin/users')}
            />
            <ActionCard
              icon={Store}
              iconBg="var(--secondary-fixed)"
              iconColor="var(--secondary)"
              title="Restaurants"
              desc="Approve & manage restaurants"
              badge={dashboard.pendingRestaurants > 0 ? dashboard.pendingRestaurants : null}
              onClick={() => navigate('/admin/restaurants')}
            />
            <ActionCard
              icon={Package}
              iconBg="var(--tertiary-container)"
              iconColor="var(--tertiary)"
              title="Orders"
              desc="Monitor all orders"
              onClick={() => navigate('/admin/orders')}
            />
            <ActionCard
              icon={Truck}
              iconBg="var(--warning-container)"
              iconColor="var(--warning)"
              title="Delivery Agents"
              desc="Manage delivery fleet"
              badge={dashboard.pendingAgents > 0 ? dashboard.pendingAgents : null}
              onClick={() => navigate('/admin/delivery-agents')}
            />
            <ActionCard
              icon={DollarSign}
              iconBg="var(--success-container)"
              iconColor="var(--success)"
              title="Refunds"
              desc="Process refund requests"
              onClick={() => navigate('/admin/refunds')}
            />
            <ActionCard
              icon={Shield}
              iconBg="var(--error-container)"
              iconColor="var(--error)"
              title="Approvals"
              desc="Pending restaurant approvals"
              badge={dashboard.pendingRestaurants > 0 ? dashboard.pendingRestaurants : null}
              onClick={() => navigate('/admin/restaurants/approvals')}
            />
          </div>
        </div>

        {/* ── Recent Orders ── */}
        {recentOrders.length > 0 && (
          <div className="dash-section">
            <DashSectionHeader
              title="Recent Orders"
              subtitle={`${recentOrders.length} most recent`}
              action={() => navigate('/admin/orders')}
            />
            <DashOrdersTable
              orders={recentOrders}
              columns={orderColumns}
              onRowClick={row => navigate(`/admin/orders/${row.id}`)}
            />
          </div>
        )}

      </div>
    </div>
  );
}
