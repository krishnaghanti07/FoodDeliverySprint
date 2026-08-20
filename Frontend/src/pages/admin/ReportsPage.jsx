import { useState, useEffect, useCallback } from 'react';
import { BarChart3, Calendar, DollarSign, TrendingUp, Package, Users } from 'lucide-react';
import { apiService } from '../../services/api';
import toast from 'react-hot-toast';
import './ReportsPage.css';

export default function ReportsPage() {
  const [activeTab, setActiveTab] = useState('sales');
  const [dateRange, setDateRange] = useState({
    from: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    to: new Date().toISOString().split('T')[0]
  });
  const [salesReport, setSalesReport] = useState(null);
  const [partnerReport, setPartnerReport] = useState(null);
  const [loading, setLoading] = useState(false);

  // ── Helpers ──────────────────────────────────────────────────────────
  const buildParams = useCallback((range) => {
    return new URLSearchParams({
      from: new Date(range.from).toISOString(),
      to: new Date(range.to + 'T23:59:59').toISOString()
    }).toString();
  }, []);

  const formatCurrency = (amount) =>
    new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', minimumFractionDigits: 0 }).format(amount || 0);

  const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  };

  const formatPercentage = (value) => `${(value || 0).toFixed(1)}%`;

  // ── Fetch functions ──────────────────────────────────────────────────
  const fetchSalesReport = useCallback(async (range) => {
    try {
      setLoading(true);
      const params = buildParams(range);
      const response = await apiService.admin.getSalesReport(params);
      const reportData = response.data?.data || response.data;
      setSalesReport(reportData);
    } catch (error) {
      console.error('Failed to fetch sales report:', error);
      toast.error('Failed to load sales report');
    } finally {
      setLoading(false);
    }
  }, [buildParams]);

  const fetchPartnerReport = useCallback(async (range) => {
    try {
      setLoading(true);
      const params = buildParams(range);
      const response = await apiService.admin.getPartnerReport(params);
      const reportData = response.data?.data || response.data;
      setPartnerReport(reportData);
    } catch (error) {
      console.error('Failed to fetch partner report:', error);
      toast.error('Failed to load partner report');
    } finally {
      setLoading(false);
    }
  }, [buildParams]);

  // Auto-load sales report on mount
  useEffect(() => {
    fetchSalesReport(dateRange);
  }, [fetchSalesReport]); // eslint-disable-line react-hooks/exhaustive-deps

  // ── Generate button handler ──────────────────────────────────────────
  const handleGenerateReport = () => {
    if (!dateRange.from || !dateRange.to) {
      toast.error('Please select both from and to dates');
      return;
    }
    if (new Date(dateRange.from) > new Date(dateRange.to)) {
      toast.error('From date cannot be after To date');
      return;
    }
    if (activeTab === 'sales') {
      fetchSalesReport(dateRange);
    } else {
      fetchPartnerReport(dateRange);
    }
  };

  // Switch tabs and auto-load
  const handleTabChange = (tab) => {
    setActiveTab(tab);
    if (tab === 'sales' && !salesReport) fetchSalesReport(dateRange);
    if (tab === 'partners' && !partnerReport) fetchPartnerReport(dateRange);
  };

  // ── Render ───────────────────────────────────────────────────────────
  return (
    <div className="reports-page container">
      <div className="reports-header">
        <h1><BarChart3 size={32} /> Reports & Analytics</h1>
      </div>

      {/* Tabs */}
      <div className="reports-tabs">
        <button className={`tab-btn ${activeTab === 'sales' ? 'active' : ''}`} onClick={() => handleTabChange('sales')}>
          <DollarSign size={20} /> Sales Report
        </button>
        <button className={`tab-btn ${activeTab === 'partners' ? 'active' : ''}`} onClick={() => handleTabChange('partners')}>
          <Users size={20} /> Partner Performance
        </button>
      </div>

      {/* Date Filter */}
      <div className="date-filter-card">
        <div className="filter-header">
          <Calendar size={20} />
          <span>Select Date Range</span>
        </div>
        <div className="date-inputs">
          <div className="input-group">
            <label>From Date</label>
            <input
              type="date"
              value={dateRange.from}
              onChange={(e) => setDateRange(prev => ({ ...prev, from: e.target.value }))}
              max={dateRange.to}
            />
          </div>
          <div className="input-group">
            <label>To Date</label>
            <input
              type="date"
              value={dateRange.to}
              onChange={(e) => setDateRange(prev => ({ ...prev, to: e.target.value }))}
              min={dateRange.from}
              max={new Date().toISOString().split('T')[0]}
            />
          </div>
          <button className="btn btn-primary" onClick={handleGenerateReport} disabled={loading}>
            {loading ? 'Generating...' : 'Generate Report'}
          </button>
        </div>
      </div>

      {/* Loading */}
      {loading && (
        <div className="loading-state">
          <div className="spinner"></div>
          <p>Generating report...</p>
        </div>
      )}

      {/* ── Sales Report ── */}
      {!loading && activeTab === 'sales' && salesReport && (
        <div className="report-content">
          {/* KPI Cards */}
          <div className="report-summary">
            <div className="summary-card">
              <div className="card-icon gmv"><DollarSign size={24} /></div>
              <div className="card-content">
                <span className="card-label">Total GMV</span>
                <span className="card-value">{formatCurrency(salesReport.totalGMV || salesReport.totalRevenue)}</span>
              </div>
            </div>
            <div className="summary-card">
              <div className="card-icon orders"><Package size={24} /></div>
              <div className="card-content">
                <span className="card-label">Total Orders</span>
                <span className="card-value">{salesReport.totalOrders || 0}</span>
              </div>
            </div>
            <div className="summary-card">
              <div className="card-icon avg"><TrendingUp size={24} /></div>
              <div className="card-content">
                <span className="card-label">Avg Order Value</span>
                <span className="card-value">{formatCurrency(salesReport.averageOrderValue)}</span>
              </div>
            </div>
            <div className="summary-card">
              <div className="card-icon cancel"><BarChart3 size={24} /></div>
              <div className="card-content">
                <span className="card-label">Cancellation Rate</span>
                <span className="card-value">{formatPercentage(salesReport.cancellationRate)}</span>
              </div>
            </div>
          </div>

          {/* Delivered vs Cancelled */}
          <div className="report-section">
            <h3>Order Summary</h3>
            <div className="status-grid">
              <div className="status-item">
                <span className="status-name">Delivered</span>
                <span className="status-count" style={{ color: '#22c55e' }}>{salesReport.deliveredOrders || 0}</span>
              </div>
              <div className="status-item">
                <span className="status-name">Cancelled</span>
                <span className="status-count" style={{ color: '#ef4444' }}>{salesReport.cancelledOrders || 0}</span>
              </div>
              <div className="status-item">
                <span className="status-name">Total</span>
                <span className="status-count">{salesReport.totalOrders || 0}</span>
              </div>
            </div>
          </div>

          {/* Orders by Status */}
          {salesReport.ordersByStatus && Object.keys(salesReport.ordersByStatus).length > 0 && (
            <div className="report-section">
              <h3>Orders by Status</h3>
              <div className="status-grid">
                {Object.entries(salesReport.ordersByStatus).map(([status, count]) => (
                  <div key={status} className="status-item">
                    <span className="status-name">{status}</span>
                    <span className="status-count">{count}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Payment Methods */}
          {salesReport.paymentMethodBreakdown && Object.keys(salesReport.paymentMethodBreakdown).length > 0 && (
            <div className="report-section">
              <h3>Payment Methods</h3>
              <div className="payment-grid">
                {Object.entries(salesReport.paymentMethodBreakdown).map(([method, data]) => (
                  <div key={method} className="payment-item">
                    <div className="payment-header">
                      <span className="payment-method">{method}</span>
                      <span className="payment-count">{data.count} orders</span>
                    </div>
                    <div className="payment-amount">{formatCurrency(data.amount || data.revenue)}</div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Daily Breakdown */}
          {salesReport.dailyBreakdown && salesReport.dailyBreakdown.length > 0 && (
            <div className="report-section">
              <h3>Daily Breakdown</h3>
              <div className="table-container">
                <table className="report-table">
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Orders</th>
                      <th>Revenue</th>
                      <th>Avg Order Value</th>
                    </tr>
                  </thead>
                  <tbody>
                    {salesReport.dailyBreakdown.map((day, idx) => (
                      <tr key={idx}>
                        <td>{formatDate(day.date)}</td>
                        <td>{day.orderCount || day.orders || 0}</td>
                        <td>{formatCurrency(day.revenue)}</td>
                        <td>{formatCurrency(day.averageOrderValue)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ── Partner Report ── */}
      {!loading && activeTab === 'partners' && partnerReport && (
        <div className="report-content">
          {/* KPI Cards */}
          <div className="report-summary">
            <div className="summary-card">
              <div className="card-icon partners"><Users size={24} /></div>
              <div className="card-content">
                <span className="card-label">Total Partners</span>
                <span className="card-value">{partnerReport.totalPartners || 0}</span>
              </div>
            </div>
            <div className="summary-card">
              <div className="card-icon active"><TrendingUp size={24} /></div>
              <div className="card-content">
                <span className="card-label">Active Partners</span>
                <span className="card-value">{partnerReport.activePartners || 0}</span>
              </div>
            </div>
            <div className="summary-card">
              <div className="card-icon revenue"><DollarSign size={24} /></div>
              <div className="card-content">
                <span className="card-label">Total Revenue</span>
                <span className="card-value">{formatCurrency(partnerReport.totalRevenue)}</span>
              </div>
            </div>
            <div className="summary-card">
              <div className="card-icon orders"><Package size={24} /></div>
              <div className="card-content">
                <span className="card-label">Total Orders</span>
                <span className="card-value">{partnerReport.totalOrders || 0}</span>
              </div>
            </div>
          </div>

          {/* Partner Performance Table */}
          {(partnerReport.partnerPerformance || partnerReport.partners || []).length > 0 && (
            <div className="report-section">
              <h3>Partner Performance</h3>
              <div className="table-container">
                <table className="report-table">
                  <thead>
                    <tr>
                      <th>Restaurant</th>
                      <th>Orders</th>
                      <th>Revenue</th>
                      <th>Avg Order Value</th>
                      <th>Fulfillment Rate</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(partnerReport.partnerPerformance || partnerReport.partners || []).map((partner, idx) => (
                      <tr key={idx}>
                        <td className="restaurant-name">{partner.restaurantName || partner.name}</td>
                        <td>{partner.totalOrders || 0}</td>
                        <td>{formatCurrency(partner.totalRevenue)}</td>
                        <td>{formatCurrency(partner.averageOrderValue)}</td>
                        <td>
                          <span className={`fulfillment-rate ${
                            (partner.fulfillmentRate || partner.fulfillRate || 0) >= 90 ? 'high' :
                            (partner.fulfillmentRate || partner.fulfillRate || 0) >= 70 ? 'medium' : 'low'
                          }`}>
                            {formatPercentage(partner.fulfillmentRate || partner.fulfillRate)}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Empty state */}
      {!loading && !salesReport && !partnerReport && (
        <div className="empty-state">
          <BarChart3 size={64} />
          <h3>No Report Generated</h3>
          <p>Select a date range and click "Generate Report" to view analytics</p>
        </div>
      )}
    </div>
  );
}
