import { useState } from 'react';
import { X } from 'lucide-react';
import api from '../../services/api';
import { toast } from 'react-hot-toast';
import './CancelOrderModal.css';

const CancelOrderModal = ({ order, onClose, onSuccess }) => {
  const [reason, setReason] = useState('');
  const [loading, setLoading] = useState(false);

  const handleCancel = async () => {
    if (!reason.trim()) {
      toast.error('Please provide a cancellation reason');
      return;
    }

    console.log('[CancelOrderModal] Cancelling order:', order.id, 'Reason:', reason);
    setLoading(true);
    try {
      const response = await api.post(`/gateway/orders/orders/${order.id}/cancel`, {
        reason: reason.trim()
      });

      console.log('[CancelOrderModal] Cancel response:', response.data);

      if (response.data.success) {
        toast.success(response.data.message || 'Order cancelled successfully');
        onSuccess();
        onClose();
      } else {
        toast.error(response.data.message || 'Failed to cancel order');
      }
    } catch (error) {
      console.error('[CancelOrderModal] Error cancelling order:', error);
      const errorMsg = error.response?.data?.message || error.message || 'Failed to cancel order';
      toast.error(errorMsg);
    } finally {
      setLoading(false);
    }
  };

  const isPaidOrder = order.paymentMethod !== 'COD' && order.status === 'Paid';

  return (
    <div className="cancel-modal-overlay" onClick={onClose}>
      <div className="cancel-modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="cancel-modal-header">
          <h2 className="headline-lg">Cancel Order</h2>
          <button onClick={onClose} className="close-btn">
            <X size={24} />
          </button>
        </div>

        <div className="cancel-modal-body">
          <p className="body-md">
            Are you sure you want to cancel this order?
          </p>
          {isPaidOrder && (
            <div className="refund-info">
              <p className="body-sm">
                <strong>Refund Information:</strong> Since you've already paid for this order, 
                a refund request will be created. Once approved by admin, the amount will be 
                credited to your wallet.
              </p>
            </div>
          )}
        </div>

        <div className="cancel-modal-form">
          <label className="body-md">
            Cancellation Reason *
          </label>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="cancel-reason-input"
            rows="4"
            placeholder="Please tell us why you're cancelling this order..."
            disabled={loading}
          />
        </div>

        <div className="cancel-modal-actions">
          <button
            onClick={onClose}
            disabled={loading}
            className="btn btn-outline"
          >
            Keep Order
          </button>
          <button
            onClick={handleCancel}
            disabled={loading}
            className="btn btn-danger"
          >
            {loading ? 'Cancelling...' : 'Cancel Order'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default CancelOrderModal;
