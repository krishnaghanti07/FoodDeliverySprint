import { useState, useEffect } from 'react';
import { DollarSign, CheckCircle, XCircle, Clock, User, Package } from 'lucide-react';
import api from '../../services/api';
import { toast } from 'react-hot-toast';
import { TableRowSkeleton } from '../../components/common/Skeleton';

const RefundManagementPage = () => {
  const [refunds, setRefunds] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('pending');
  const [selectedRefund, setSelectedRefund] = useState(null);
  const [showProcessModal, setShowProcessModal] = useState(false);
  const [adminNotes, setAdminNotes] = useState('');
  const [processing, setProcessing] = useState(false);

  useEffect(() => {
    fetchRefunds();
  }, [filter]);

  const fetchRefunds = async () => {
    try {
      setLoading(true);
      const endpoint = filter === 'pending' 
        ? '/gateway/admin/refunds/pending'
        : `/gateway/admin/refunds?status=${filter}`;
      
      const response = await api.get(endpoint);
      if (response.data.success) {
        setRefunds(response.data.data);
      }
    } catch (error) {
      console.error('Error fetching refunds:', error);
      toast.error('Failed to load refund requests');
    } finally {
      setLoading(false);
    }
  };

  const handleProcessRefund = async (action) => {
    if (!selectedRefund) return;

    setProcessing(true);
    try {
      const response = await api.post(
        `/gateway/admin/refunds/${selectedRefund.id}/process`,
        {
          action,
          adminNotes: adminNotes.trim() || null
        }
      );

      if (response.data.success) {
        toast.success(response.data.message || `Refund ${action.toLowerCase()}d successfully`);
        setShowProcessModal(false);
        setSelectedRefund(null);
        setAdminNotes('');
        fetchRefunds();
      }
    } catch (error) {
      console.error('Error processing refund:', error);
      toast.error(error.response?.data?.message || 'Failed to process refund');
    } finally {
      setProcessing(false);
    }
  };

  const openProcessModal = (refund) => {
    setSelectedRefund(refund);
    setShowProcessModal(true);
    setAdminNotes('');
  };

  const getStatusBadge = (status) => {
    const badges = {
      'PendingApproval': { color: 'bg-yellow-100 text-yellow-800', label: 'Pending' },
      'Approved': { color: 'bg-green-100 text-green-800', label: 'Approved' },
      'Rejected': { color: 'bg-red-100 text-red-800', label: 'Rejected' },
      'Completed': { color: 'bg-blue-100 text-blue-800', label: 'Completed' }
    };
    const badge = badges[status] || badges['PendingApproval'];
    return (
      <span className={`px-3 py-1 rounded-full text-sm font-medium ${badge.color}`}>
        {badge.label}
      </span>
    );
  };

  if (loading) {
    return (
      <div className="max-w-7xl mx-auto p-6">
        <div style={{ marginBottom: 'var(--space-xl)' }}>
          <div className="skeleton" style={{ height: '2rem', width: '14rem', marginBottom: '0.5rem' }} />
          <div className="skeleton" style={{ height: '1rem', width: '20rem' }} />
        </div>
        <div style={{ display: 'flex', gap: '0.75rem', marginBottom: 'var(--space-xl)' }}>
          {[80, 90, 100].map((w, i) => (
            <div key={i} className="skeleton" style={{ height: '2.25rem', width: w, borderRadius: 'var(--rounded-full)' }} />
          ))}
        </div>
        <div className="card" style={{ overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                {['Order ID', 'Customer', 'Amount', 'Reason', 'Status', 'Actions'].map(h => (
                  <th key={h} style={{ padding: 'var(--space-md)', textAlign: 'left', borderBottom: '1px solid var(--outline-variant)' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {Array.from({ length: 6 }).map((_, i) => <TableRowSkeleton key={i} columns={6} />)}
            </tbody>
          </table>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">Refund Management</h1>
        <p className="text-gray-600">Review and process customer refund requests</p>
      </div>

      {/* Filter Tabs */}
      <div className="bg-white rounded-lg shadow-md mb-6">
        <div className="flex border-b">
          {['pending', 'approved', 'rejected'].map((tab) => (
            <button
              key={tab}
              onClick={() => setFilter(tab)}
              className={`flex-1 px-6 py-3 font-medium capitalize ${
                filter === tab
                  ? 'border-b-2 border-orange-600 text-orange-600'
                  : 'text-gray-600 hover:text-gray-800'
              }`}
            >
              {tab}
            </button>
          ))}
        </div>
      </div>

      {/* Refunds List */}
      <div className="space-y-4">
        {refunds.length === 0 ? (
          <div className="bg-white rounded-lg shadow-md p-12 text-center">
            <DollarSign size={48} className="mx-auto mb-4 text-gray-300" />
            <p className="text-gray-500">No {filter} refund requests</p>
          </div>
        ) : (
          refunds.map((refund) => (
            <div key={refund.id} className="bg-white rounded-lg shadow-md p-6">
              <div className="flex justify-between items-start mb-4">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <h3 className="text-lg font-bold">
                      Refund Request #{refund.id.substring(0, 8).toUpperCase()}
                    </h3>
                    {getStatusBadge(refund.status)}
                  </div>
                  <div className="grid grid-cols-2 gap-4 text-sm text-gray-600">
                    <div className="flex items-center gap-2">
                      <User size={16} />
                      <span>{refund.customerName || 'Unknown Customer'}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <Package size={16} />
                      <span>Order #{refund.orderId.substring(0, 8).toUpperCase()}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <Clock size={16} />
                      <span>{new Date(refund.requestedAt).toLocaleString()}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <DollarSign size={16} />
                      <span className="font-bold text-lg">₹{refund.amount.toFixed(2)}</span>
                    </div>
                  </div>
                </div>
              </div>

              {refund.orderCancellationReason && (
                <div className="bg-gray-50 rounded-lg p-3 mb-4">
                  <p className="text-sm font-medium text-gray-700 mb-1">Cancellation Reason:</p>
                  <p className="text-sm text-gray-600">{refund.orderCancellationReason}</p>
                </div>
              )}

              {refund.adminNotes && (
                <div className="bg-blue-50 rounded-lg p-3 mb-4">
                  <p className="text-sm font-medium text-blue-700 mb-1">Admin Notes:</p>
                  <p className="text-sm text-blue-600">{refund.adminNotes}</p>
                </div>
              )}

              {refund.status === 'PendingApproval' && (
                <div className="flex gap-3 mt-4">
                  <button
                    onClick={() => openProcessModal(refund)}
                    className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 flex items-center justify-center gap-2"
                  >
                    <CheckCircle size={20} />
                    Approve Refund
                  </button>
                  <button
                    onClick={() => {
                      setSelectedRefund(refund);
                      setShowProcessModal(true);
                    }}
                    className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 flex items-center justify-center gap-2"
                  >
                    <XCircle size={20} />
                    Reject Refund
                  </button>
                </div>
              )}

              {refund.processedAt && (
                <div className="mt-4 text-sm text-gray-500">
                  Processed on {new Date(refund.processedAt).toLocaleString()}
                </div>
              )}
            </div>
          ))
        )}
      </div>

      {/* Process Refund Modal */}
      {showProcessModal && selectedRefund && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg max-w-md w-full p-6">
            <h2 className="text-xl font-bold mb-4">Process Refund Request</h2>
            
            <div className="mb-4">
              <p className="text-gray-600 mb-2">
                Refund Amount: <span className="font-bold text-lg">₹{selectedRefund.amount.toFixed(2)}</span>
              </p>
              <p className="text-gray-600 mb-2">
                Customer: {selectedRefund.customerName}
              </p>
            </div>

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Admin Notes (Optional)
              </label>
              <textarea
                value={adminNotes}
                onChange={(e) => setAdminNotes(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                rows="3"
                placeholder="Add any notes about this decision..."
                disabled={processing}
              />
            </div>

            <div className="flex gap-3">
              <button
                onClick={() => {
                  setShowProcessModal(false);
                  setSelectedRefund(null);
                  setAdminNotes('');
                }}
                disabled={processing}
                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                onClick={() => handleProcessRefund('Reject')}
                disabled={processing}
                className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50"
              >
                {processing ? 'Processing...' : 'Reject'}
              </button>
              <button
                onClick={() => handleProcessRefund('Approve')}
                disabled={processing}
                className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
              >
                {processing ? 'Processing...' : 'Approve'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default RefundManagementPage;
