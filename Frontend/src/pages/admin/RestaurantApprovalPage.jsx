import { useState, useEffect } from 'react';
import { Store, MapPin, Phone, Mail, Clock, CheckCircle, XCircle } from 'lucide-react';
import api from '../../services/api';
import toast from 'react-hot-toast';
import './RestaurantApprovalPage.css';

const RestaurantApprovalPage = () => {
  const [pendingRestaurants, setRestaurants] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedRestaurant, setSelectedRestaurant] = useState(null);
  const [showApprovalModal, setShowApprovalModal] = useState(false);
  const [approvalAction, setApprovalAction] = useState(null);
  const [approvalNotes, setApprovalNotes] = useState('');
  const [processing, setProcessing] = useState(false);

  useEffect(() => {
    fetchPendingRestaurants();
  }, []);

  const fetchPendingRestaurants = async () => {
    try {
      setLoading(true);
      const response = await api.get('/gateway/admin/restaurants?status=Pending');
      setRestaurants(response.data || []);
    } catch (error) {
      console.error('Error fetching pending restaurants:', error);
      toast.error('Failed to load pending restaurants');
    } finally {
      setLoading(false);
    }
  };

  const handleApprovalClick = (restaurant, action) => {
    setSelectedRestaurant(restaurant);
    setApprovalAction(action);
    setApprovalNotes('');
    setShowApprovalModal(true);
  };

  const handleApprovalSubmit = async () => {
    if (!selectedRestaurant || !approvalAction) return;

    try {
      setProcessing(true);
      
      if (approvalAction === 'approve') {
        await api.patch(`/gateway/admin/restaurants/${selectedRestaurant.id}/approve`, {
          notes: approvalNotes || 'Approved by admin'
        });
        toast.success('Restaurant approved successfully');
      } else {
        await api.post(`/gateway/admin/restaurants/${selectedRestaurant.id}/reject`, {
          reason: approvalNotes || 'Rejected by admin'
        });
        toast.success('Restaurant rejected successfully');
      }
      
      setShowApprovalModal(false);
      setSelectedRestaurant(null);
      setApprovalNotes('');
      fetchPendingRestaurants();
    } catch (error) {
      console.error(`Error ${approvalAction}ing restaurant:`, error);
      toast.error(`Failed to ${approvalAction} restaurant`);
    } finally {
      setProcessing(false);
    }
  };

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (loading) {
    return (
      <div className="restaurant-approval-page">
        <div className="loading">Loading pending restaurants...</div>
      </div>
    );
  }

  return (
    <div className="restaurant-approval-page">
      <div className="page-header">
        <h1>Restaurant Approvals</h1>
        <p>Review and approve pending restaurant registrations</p>
      </div>

      {pendingRestaurants.length === 0 ? (
        <div className="empty-state">
          <Store size={64} />
          <h3>No Pending Approvals</h3>
          <p>All restaurant registrations have been reviewed</p>
        </div>
      ) : (
        <div className="restaurants-grid">
          {pendingRestaurants.map(restaurant => (
            <div key={restaurant.id} className="restaurant-card">
              <div className="pending-badge">
                <Clock size={16} />
                <span>Pending Approval</span>
              </div>

              {restaurant.logoUrl && (
                <div className="restaurant-logo">
                  <img src={restaurant.logoUrl} alt={restaurant.name} />
                </div>
              )}

              <div className="restaurant-info">
                <h3>{restaurant.name}</h3>
                
                {restaurant.description && (
                  <p className="description">{restaurant.description}</p>
                )}

                <div className="info-row">
                  <MapPin size={16} />
                  <span>{restaurant.address}</span>
                </div>

                {restaurant.phone && (
                  <div className="info-row">
                    <Phone size={16} />
                    <span>{restaurant.phone}</span>
                  </div>
                )}

                {restaurant.partnerName && (
                  <div className="info-row">
                    <Mail size={16} />
                    <span>Partner: {restaurant.partnerName}</span>
                  </div>
                )}

                <div className="info-row">
                  <Clock size={16} />
                  <span>Registered: {formatDate(restaurant.createdAt)}</span>
                </div>

                {restaurant.cuisineTypes && restaurant.cuisineTypes.length > 0 && (
                  <div className="cuisine-tags">
                    {restaurant.cuisineTypes.map((cuisine, idx) => (
                      <span key={idx} className="cuisine-tag">{cuisine}</span>
                    ))}
                  </div>
                )}
              </div>

              <div className="restaurant-actions">
                <button
                  className="btn-approve"
                  onClick={() => handleApprovalClick(restaurant, 'approve')}
                >
                  <CheckCircle size={16} />
                  Approve
                </button>
                <button
                  className="btn-reject"
                  onClick={() => handleApprovalClick(restaurant, 'reject')}
                >
                  <XCircle size={16} />
                  Reject
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {showApprovalModal && (
        <div className="modal-overlay" onClick={() => setShowApprovalModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{approvalAction === 'approve' ? 'Approve' : 'Reject'} Restaurant</h3>
              <button className="modal-close" onClick={() => setShowApprovalModal(false)}>
                &times;
              </button>
            </div>

            <div className="modal-body">
              <div className="restaurant-summary">
                <h4>{selectedRestaurant?.name}</h4>
                <p><strong>Partner:</strong> {selectedRestaurant?.partnerName}</p>
                <p><strong>Address:</strong> {selectedRestaurant?.address}</p>
              </div>

              <div className="form-group">
                <label>Notes {approvalAction === 'reject' ? '(Required)' : '(Optional)'}:</label>
                <textarea
                  value={approvalNotes}
                  onChange={(e) => setApprovalNotes(e.target.value)}
                  placeholder={`Enter reason for ${approvalAction}...`}
                  rows={4}
                  required={approvalAction === 'reject'}
                />
              </div>
            </div>

            <div className="modal-actions">
              <button
                className="btn-cancel"
                onClick={() => setShowApprovalModal(false)}
                disabled={processing}
              >
                Cancel
              </button>
              <button
                className={approvalAction === 'approve' ? 'btn-approve' : 'btn-reject'}
                onClick={handleApprovalSubmit}
                disabled={processing || (approvalAction === 'reject' && !approvalNotes.trim())}
              >
                {processing ? 'Processing...' : `Confirm ${approvalAction === 'approve' ? 'Approval' : 'Rejection'}`}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default RestaurantApprovalPage;
