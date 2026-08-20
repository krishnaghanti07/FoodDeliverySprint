import { useState, useEffect } from 'react';
import { DollarSign, Search, Filter, CheckCircle, XCircle, Clock, User, Package } from 'lucide-react';
import { apiService } from '../../services/api';
import toast from 'react-hot-toast';
import './RefundManagement.css';

export default function RefundManagement() {
  const [refunds, setRefunds] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('pending'); // 'pending' | 'all'
  const [statusFilter, setStatusFilter] = useState('');
  const [selectedRefund, setSelectedRefund] = useState(null);
  const [showProcessModal, setShowProcessModal] = useState(false);
  const [processAction, setProcessAction] = useState(null); // 'approve' | 'reject'
  const [adminNotes, setAdminNotes] = useState('');

  useEffect(() => {
    fetchRefunds();
  }, [activeTab, statusFilter]);

  const fetchRefunds = async () => {
    try {
      setLoading(true);
      let response;
      
      if (activeTab === 'pending') {
        response = await apiService.admin.getPendingRefunds();
      } else {
        response = await apiService.admin.getAllRefunds(statusFilter);
      }
      
      const refundsData = response.data?.data || response.data || [];
      setRefunds(Array.isArray(refundsData) ? refundsData : []);
    } catch (error) {
      console.error('Failed to fetch refunds:', error);
      toast.error('Failed to load refunds');
      setRefunds([]);
    } finally {
      setLoading(false);
    }
  };

  const handleProcessClick = (refund, action) => {
    setSelectedRefund(refund);
    setProcessAction(action);
    setAdminNotes('');
    setShowProcessModal(true);
  };

  const handleProcessSubmit = async () => {
    if (!selectedRefund || !processAction) return;

    try {
      await apiService.admin.processRefund(selectedRefund.id, {
        action: processAction,
        adminNotes: adminNotes || (processAction === 'approve' ? 'Approved by admin' : 'Rejected by admin')
      });
      
      toast.success(`Refund ${processAction}d successfully`);
      setShowProcessModal(false);
      setSelectedRefund(null);
      setAdminNotes('');
      fetchRefunds();
    } catch (error) {
      console.error(`Error ${processAction}ing refund:`, error);
      toast.error(error.response?.data?.message || `Failed to ${processAction} refund`);
    }
  };

  const getStatusColor = (status) => {
    const colors = {
      Pending: 'warning',
      Approved: 'success',
      Rejected: 'error',
      Processed: 'success'
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
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (loading) {
    return (
      <div className="refund-management container">
        <div className="loading-state">
          <div className="spinner"></div>
          <p>Loading refunds...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="refund-management container">
      <div className="refund-header">
        <h1><DollarSign size={32} /> Refund Management</h1>
      </div>

      <div className="tabs-container">
        <button
          className={`tab ${activeTab === 'pending' ? 'active' : ''}`}
          onClick={() => setActiveTab('pending')}
        >
          Pending Refunds ({refunds.filter(r => r.status === 'Pending').length})
        </button>
        <button
          className={`tab ${activeTab === 'all' ? 'active' : ''}`}
          onClick={() => setActiveTab('all')}
        >
          All Refunds ({refunds.length})
        </button>
      </div>

      {activeTab === 'all' && (
        <div className="refund-filters-card">
          <div className="filters-header">
            <Filter size={20} />
            <span>Filters</span>
          </div>
          <div className="filters-grid">
            <div className="filter-group">
              <label>Status</label>
              <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="">All Statuses</option>
                <option value="Pending">Pending</option>
                <option value="Approved">Approved</option>
                <option value="Rejected">Rejected</option>
                <option value="Processed">Processed</option>
              </select>
            </div>
          </div>
          {statusFilter && (
            <button className="btn-clear-filters" onClick={() => setStatusFilter('')}>
              Clear Filters
            </button>
          )}
        </div>
      )}

      {refunds.length === 0 ? (
        <div className="empty-state">
          <DollarSign size={64} />
          <h3>No Refunds Found</h3>
          <p>{activeTab === 'pending' ? 'No pending refund requests' : 'No refunds match your filters'}</p>
        </div>
      ) : (
        <div className="refunds-list">
          {refunds.map(refund => (
            <div key={refund.id} className="refund-card">
              <div className="refund-header-section">
                <div className="refund-id-section">
                  <span className="refund-id">#{refund.id.substring(0, 8)}</span>
                  <span className={`status-badge ${getStatusColor(refund.status)}`}>
                    {refund.status}
                  </span>
                </div>
                <div className="refund-amount">
                  <DollarSign size={20} />
                  <strong>{formatCurrency(refund.refundAmount || refund.amount)}</strong>
                </div>
              </div>

              {/* Refund Breakdown */}
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
                    <span className="detail-muted">{refund.customerEmail}</span>
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

                {refund.restaurantName && (
                  <div className="detail-row">
                    <Package size={16} />
                    <div className="detail-content">
                      <span className="detail-label">Restaurant:</span>
                      <span className="detail-value">{refund.restaurantName}</span>
                    </div>
                  </div>
                )}

                {refund.adminNotes && (
                  <div className="detail-row">
                    <div className="detail-content full-width">
                      <span className="detail-label">Admin Notes:</span>
                      <p className="detail-value">{refund.adminNotes}</p>
                    </div>
                  </div>
                )}

                {refund.processedAt && (
                  <div className="detail-row">
                    <Clock size={16} />
                    <div className="detail-content">
                      <span className="detail-label">Processed:</span>
                      <span className="detail-value">{formatDate(refund.processedAt)}</span>
                    </div>
                  </div>
                )}
              </div>

              {refund.status === 'Pending' && (
                <div className="refund-actions">
                  <button
                    className="btn btn-success btn-sm"
                    onClick={() => handleProcessClick(refund, 'approve')}
                  >
                    <CheckCircle size={16} />
                    Approve Refund
                  </button>
                  <button
                    className="btn btn-danger btn-sm"
                    onClick={() => handleProcessClick(refund, 'reject')}
                  >
                    <XCircle size={16} />
                    Reject Refund
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Process Refund Modal */}
      {showProcessModal && (
        <div className="modal-overlay" onClick={() => setShowProcessModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{processAction === 'approve' ? 'Approve' : 'Reject'} Refund</h3>
              <button className="modal-close" onClick={() => setShowProcessModal(false)}>&times;</button>
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
                  {selectedRefund?.orderCancellationReason && (
                    <div className="detail-item full-width">
                      <span className="detail-label">Cancellation Reason:</span>
                      <span className="detail-value">{selectedRefund?.orderCancellationReason}</span>
                    </div>
                  )}
                </div>
              </div>
              
              {processAction === 'approve' && (
                <div className="alert alert-info">
                  <p><strong>Note:</strong> Approving this refund will credit {formatCurrency(selectedRefund?.refundAmount || selectedRefund?.amount)} to the customer's wallet.</p>
                </div>
              )}

              <div className="form-group">
                <label>Admin Notes (Optional):</label>
                <textarea
                  value={adminNotes}
                  onChange={(e) => setAdminNotes(e.target.value)}
                  placeholder={`Enter notes for ${processAction}ing this refund...`}
                  rows={4}
                  className="form-control"
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => setShowProcessModal(false)}>
                Cancel
              </button>
              <button
                className={`btn ${processAction === 'approve' ? 'btn-success' : 'btn-danger'}`}
                onClick={handleProcessSubmit}
              >
                Confirm {processAction === 'approve' ? 'Approval' : 'Rejection'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
