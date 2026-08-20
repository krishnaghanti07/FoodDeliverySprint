import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import {
  Package, Truck, CheckCircle, DollarSign, Bell, RefreshCw,
  Clock, XCircle, MapPin, ChevronRight,
} from 'lucide-react';
import {
  StatCard, DashSectionHeader, DonutChart,
} from '../../components/common/DashboardWidgets';
import '../../components/common/DashboardWidgets.css';
import './DeliveryAgentDashboard.css';

// ── Delivery Timeline ────────────────────────────────────────────────
function DeliveryTimeline({ delivery }) {
  const steps = [
    { key: 'assignedAt', label: 'Assigned', status: 'Assigned' },
    { key: 'pickedUpAt', label: 'Picked Up', status: 'PickedUp' },
    { key: 'outForDeliveryAt', label: 'Out for Delivery', status: 'OutForDelivery' },
    { key: 'deliveredAt', label: 'Delivered', status: 'Delivered' },
  ];
  const currentIdx = steps.findIndex(s => s.status === delivery.status);

  return (
    <div className="da-timeline">
      {steps.map((step, i) => {
        const done = i < currentIdx || delivery[step.key];
        const current = i === currentIdx;
        return (
          <div key={step.key} className={`da-tl-step ${done ? 'done' : ''} ${current ? 'current' : ''}`}>
            {i > 0 && <div className={`da-tl-line ${done ? 'done' : ''}`} />}
            <div className="da-tl-dot">
              {done ? <CheckCircle size={12} /> : <div className="da-tl-inner" />}
            </div>
            <span className="da-tl-label">{step.label}</span>
          </div>
        );
      })}
    </div>
  );
}

// ── Delivery Card ────────────────────────────────────────────────────
function DeliveryCard({ delivery, onClick }) {
  const statusColors = {
    Assigned: { bg: 'var(--warning-container)', color: '#e65100' },
    PickedUp: { bg: 'var(--secondary-fixed)', color: 'var(--secondary)' },
    OutForDelivery: { bg: 'var(--primary-fixed)', color: 'var(--primary)' },
    Delivered: { bg: 'var(--success-container)', color: 'var(--success)' },
    Failed: { bg: 'var(--error-container)', color: 'var(--error)' },
  };
  const sc = statusColors[delivery.status] || { bg: 'var(--surface-container)', color: 'var(--on-surface-variant)' };

  return (
    <div className="da-delivery-card" onClick={onClick} role="button" tabIndex={0}
      onKeyDown={e => e.key === 'Enter' && onClick()}>
      <div className="da-dc-header">
        <div className="da-dc-info">
          <span className="da-dc-id">Order #{delivery.orderId?.substring(0, 8).toUpperCase()}</span>
          <span className="da-dc-time">
            <Clock size={12} />
            {new Date(delivery.assignedAt).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' })}
          </span>
        </div>
        <span className="da-dc-status" style={{ background: sc.bg, color: sc.color }}>
          {delivery.status}
        </span>
      </div>

      {delivery.estimatedArrivalTime && (
        <div className="da-dc-eta">
          <MapPin size={12} />
          ETA: {new Date(delivery.estimatedArrivalTime).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' })}
        </div>
      )}

      <DeliveryTimeline delivery={delivery} />

      <div className="da-dc-footer">
        <span className="da-dc-earnings">₹30 earning</span>
        <span className="da-dc-cta">View Details <ChevronRight size={14} /></span>
      </div>
    </div>
  );
}

// ── Main Component ───────────────────────────────────────────────────
function DeliveryAgentDashboard() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [deliveries, setDeliveries] = useState([]);
  const [availableCount, setAvailableCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [stats, setStats] = useState({
    total: 0, assigned: 0, pickedUp: 0, outForDelivery: 0,
    delivered: 0, failed: 0, totalEarnings: 0, todayEarnings: 0,
  });
  const [statusDonut, setStatusDonut] = useState([]);

  const fetchAvailableCount = useCallback(async () => {
    try {
      const response = await api.get('/gateway/deliveries/available');
      setAvailableCount(response.data?.data?.length || 0);
    } catch {}
  }, []);

  const fetchMyDeliveries = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    else setRefreshing(true);
    try {
      const response = await api.get('/gateway/deliveries/my');
      const data = response.data?.data || [];
      setDeliveries(data);

      const profileRes = await api.get('/gateway/auth/profile');
      const walletBalance = profileRes.data?.data?.walletBalance || 0;

      const today = new Date().toDateString();
      const todayDeliveries = data.filter(d =>
        d.status === 'Delivered' && new Date(d.deliveredAt).toDateString() === today
      );

      const newStats = {
        total: data.length,
        assigned: data.filter(d => d.status === 'Assigned').length,
        pickedUp: data.filter(d => d.status === 'PickedUp').length,
        outForDelivery: data.filter(d => d.status === 'OutForDelivery').length,
        delivered: data.filter(d => d.status === 'Delivered').length,
        failed: data.filter(d => d.status === 'Failed').length,
        totalEarnings: walletBalance,
        todayEarnings: todayDeliveries.length * 30,
      };
      setStats(newStats);

      // Status donut
      setStatusDonut([
        { label: 'Delivered', value: newStats.delivered, color: '#2e7d32' },
        { label: 'Active', value: newStats.assigned + newStats.pickedUp + newStats.outForDelivery, color: 'var(--primary)' },
        { label: 'Failed', value: newStats.failed, color: 'var(--error)' },
      ].filter(s => s.value > 0));

    } catch (error) {
      console.error('Failed to fetch deliveries:', error);
      if (!silent) toast.error('Failed to load deliveries');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    fetchMyDeliveries();
    fetchAvailableCount();
    const interval = setInterval(fetchAvailableCount, 30000);
    return () => clearInterval(interval);
  }, [fetchMyDeliveries, fetchAvailableCount]);

  const activeDeliveries = deliveries.filter(d => ['Assigned', 'PickedUp', 'OutForDelivery'].includes(d.status));
  const completedDeliveries = deliveries.filter(d => ['Delivered', 'Failed'].includes(d.status));

  if (loading) {
    return (
      <div className="dash-page">
        <div className="container" style={{ padding: '2rem 1rem' }}>
          <div style={{ marginBottom: 'var(--space-xl)' }}>
            <div className="skeleton" style={{ height: '2rem', width: '14rem', marginBottom: '0.5rem' }} />
            <div className="skeleton" style={{ height: '1rem', width: '10rem' }} />
          </div>
          <div className="dash-stats-grid">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="dash-stat-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem' }}>
                  <div className="skeleton" style={{ height: '2.75rem', width: '2.75rem', borderRadius: 'var(--rounded-xl)' }} />
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
      <div className="container" style={{ padding: '2rem 1rem' }}>

        {/* ── Page Header ── */}
        <div className="dash-page-header">
          <div>
            <h1 className="dash-page-title">Delivery Dashboard</h1>
            <p className="dash-page-sub">Welcome back, {user?.fullName?.split(' ')[0] || 'Agent'}!</p>
          </div>
          <div className="dash-page-actions">
            {availableCount > 0 && (
              <button className="btn btn-primary btn-sm" onClick={() => navigate('/agent/available')}>
                <Bell size={15} /> {availableCount} Available
              </button>
            )}
            <button className="btn btn-outline btn-sm" onClick={() => navigate('/agent/deliveries')}>
              All Deliveries
            </button>
            <button
              className={`odp-refresh-btn ${refreshing ? 'spinning' : ''}`}
              onClick={() => { fetchMyDeliveries(true); fetchAvailableCount(); }}
              title="Refresh"
            >
              <RefreshCw size={16} />
            </button>
          </div>
        </div>

        {/* ── Stat Cards ── */}
        <div className="dash-stats-grid">
          <StatCard
            label="Total Deliveries"
            value={stats.total.toLocaleString()}
            icon={Package}
            iconBg="var(--primary-fixed)"
            iconColor="var(--primary)"
            accent="primary"
          />
          <StatCard
            label="Total Earnings"
            value={`₹${stats.totalEarnings.toFixed(0)}`}
            sublabel={`Today: ₹${stats.todayEarnings}`}
            icon={DollarSign}
            iconBg="var(--success-container)"
            iconColor="var(--success)"
            accent="success"
          />
          <StatCard
            label="Active"
            value={(stats.assigned + stats.pickedUp + stats.outForDelivery).toString()}
            sublabel="In progress"
            icon={Truck}
            iconBg="var(--warning-container)"
            iconColor="var(--warning)"
            accent="warning"
            badge={availableCount > 0 ? `${availableCount} available` : null}
            badgeVariant="primary"
            onClick={() => navigate('/agent/available')}
          />
          <StatCard
            label="Delivered"
            value={stats.delivered.toLocaleString()}
            sublabel="₹30 per delivery"
            icon={CheckCircle}
            iconBg="var(--success-container)"
            iconColor="var(--success)"
            accent="success"
          />
          <StatCard
            label="Failed"
            value={stats.failed.toLocaleString()}
            icon={XCircle}
            iconBg="var(--error-container)"
            iconColor="var(--error)"
            accent="error"
          />
        </div>

        {/* ── Charts Row ── */}
        {statusDonut.length > 0 && (
          <div className="dash-charts-row" style={{ gridTemplateColumns: '1fr 260px' }}>
            {/* Earnings info card */}
            <div className="dash-chart-card">
              <p className="dash-chart-title">Earnings Summary</p>
              <div className="da-earnings-grid">
                <div className="da-earning-item">
                  <span className="da-ei-label">Today</span>
                  <span className="da-ei-value">₹{stats.todayEarnings}</span>
                  <span className="da-ei-sub">{stats.todayEarnings / 30 | 0} deliveries</span>
                </div>
                <div className="da-earning-item">
                  <span className="da-ei-label">Total</span>
                  <span className="da-ei-value">₹{stats.totalEarnings.toFixed(0)}</span>
                  <span className="da-ei-sub">Wallet balance</span>
                </div>
                <div className="da-earning-item">
                  <span className="da-ei-label">Per Delivery</span>
                  <span className="da-ei-value">₹30</span>
                  <span className="da-ei-sub">Fixed rate</span>
                </div>
              </div>
            </div>

            <div className="dash-donut-card">
              <p className="dash-chart-title" style={{ margin: 0 }}>Delivery Status</p>
              <DonutChart
                segments={statusDonut}
                size={110}
                thickness={14}
                label={stats.total.toString()}
                sublabel="total"
              />
              <div className="dash-donut-legend">
                {statusDonut.map(seg => (
                  <div key={seg.label} className="ddl-item">
                    <div className="ddl-dot" style={{ background: seg.color }} />
                    <span className="ddl-label">{seg.label}</span>
                    <span className="ddl-value">{seg.value}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* ── Active Deliveries ── */}
        <div className="dash-section">
          <DashSectionHeader
            title={`Active Deliveries (${activeDeliveries.length})`}
            action={activeDeliveries.length > 0 ? () => navigate('/agent/deliveries') : null}
          />
          {activeDeliveries.length === 0 ? (
            <div className="da-empty">
              <Truck size={40} style={{ color: 'var(--outline)', opacity: 0.5 }} />
              <p className="body-lg text-muted">No active deliveries</p>
              {availableCount > 0 && (
                <button className="btn btn-primary btn-sm" onClick={() => navigate('/agent/available')}>
                  <Bell size={15} /> View {availableCount} Available Orders
                </button>
              )}
            </div>
          ) : (
            <div className="da-deliveries-grid">
              {activeDeliveries.map(d => (
                <DeliveryCard
                  key={d.id}
                  delivery={d}
                  onClick={() => navigate(`/agent/deliveries/${d.id}`)}
                />
              ))}
            </div>
          )}
        </div>

        {/* ── Recent Completed ── */}
        {completedDeliveries.length > 0 && (
          <div className="dash-section">
            <DashSectionHeader
              title={`Recent Completed (${Math.min(completedDeliveries.length, 5)})`}
              action={() => navigate('/agent/deliveries')}
            />
            <div className="da-completed-list">
              {completedDeliveries.slice(0, 5).map(d => (
                <div
                  key={d.id}
                  className="da-completed-item"
                  onClick={() => navigate(`/agent/deliveries/${d.id}`)}
                >
                  <div className={`da-ci-dot ${d.status === 'Delivered' ? 'delivered' : 'failed'}`} />
                  <div className="da-ci-info">
                    <span className="da-ci-id">Order #{d.orderId?.substring(0, 8).toUpperCase()}</span>
                    <span className="da-ci-time">
                      {d.deliveredAt
                        ? `Delivered ${new Date(d.deliveredAt).toLocaleString('en-IN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}`
                        : `Failed ${new Date(d.assignedAt).toLocaleString('en-IN', { day: '2-digit', month: 'short' })}`}
                    </span>
                  </div>
                  <span className={`badge ${d.status === 'Delivered' ? 'badge-success' : 'badge-error'}`} style={{ fontSize: '0.6875rem' }}>
                    {d.status}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

      </div>
    </div>
  );
}

export default DeliveryAgentDashboard;
