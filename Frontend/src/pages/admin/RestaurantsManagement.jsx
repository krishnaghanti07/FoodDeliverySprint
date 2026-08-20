import { useState, useEffect } from 'react';
import { Search, CheckCircle, XCircle, Eye, Trash2, DollarSign, Star, MapPin, Phone, Clock } from 'lucide-react';
import api, { apiService } from '../../services/api';
import toast from 'react-hot-toast';
import { TableRowSkeleton } from '../../components/common/Skeleton';
import './RestaurantsManagement.css';

export default function RestaurantsManagement() {
  const [loading, setLoading] = useState(true);
  const [restaurants, setRestaurants] = useState([]);
  const [deletedRestaurants, setDeletedRestaurants] = useState([]);
  const [activeTab, setActiveTab] = useState('all'); // 'all' or 'deleted'
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRestaurant, setSelectedRestaurant] = useState(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [showDisapproveModal, setShowDisapproveModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showRestoreModal, setShowRestoreModal] = useState(false);
  const [showPermanentDeleteModal, setShowPermanentDeleteModal] = useState(false);
  const [disapproveReason, setDisapproveReason] = useState('');
  const [deleteReason, setDeleteReason] = useState('');
  const [restoreReason, setRestoreReason] = useState('');

  useEffect(() => {
    loadRestaurants();
  }, []);

  const loadRestaurants = async () => {
    try {
      setLoading(true);
      const res = await apiService.catalog.getAllRestaurantsAdmin();
      const allRestaurants = res.data?.data || [];
      
      // Separate active and deleted restaurants
      setRestaurants(allRestaurants.filter(r => !r.isDeleted));
      setDeletedRestaurants(allRestaurants.filter(r => r.isDeleted));
    } catch (error) {
      console.error('Failed to load restaurants:', error);
      toast.error('Failed to load restaurants');
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async (restaurantId) => {
    try {
      await apiService.catalog.approveRestaurant(restaurantId);
      toast.success('Restaurant approved successfully');
      loadRestaurants();
    } catch (error) {
      console.error('Failed to approve restaurant:', error);
      toast.error('Failed to approve restaurant');
    }
  };

  const viewRestaurantDetails = async (restaurantId) => {
    try {
      const response = await apiService.admin.getRestaurantById(restaurantId);
      setSelectedRestaurant(response.data);
      setShowDetailModal(true);
    } catch (error) {
      console.error('Failed to fetch restaurant details:', error);
      toast.error('Failed to load restaurant details');
    }
  };

  const handleDisapprove = (restaurant) => {
    setSelectedRestaurant(restaurant);
    setDisapproveReason('');
    setShowDisapproveModal(true);
  };

  const confirmDisapprove = async () => {
    if (!disapproveReason.trim()) {
      toast.error('Please provide a reason for disapproving');
      return;
    }

    try {
      await apiService.admin.updateRestaurantStatus(selectedRestaurant.id, {
        status: 'Pending',
        reason: disapproveReason
      });
      toast.success('Restaurant disapproved successfully');
      setShowDisapproveModal(false);
      setSelectedRestaurant(null);
      setDisapproveReason('');
      loadRestaurants();
    } catch (error) {
      console.error('Failed to disapprove restaurant:', error);
      toast.error('Failed to disapprove restaurant');
    }
  };

  const handleDelete = (restaurant) => {
    setSelectedRestaurant(restaurant);
    setDeleteReason('');
    setShowDeleteModal(true);
  };

  const confirmDelete = async () => {
    if (!deleteReason.trim()) {
      toast.error('Please provide a reason for deletion');
      return;
    }

    try {
      await apiService.admin.softDeleteRestaurant(selectedRestaurant.id, {
        reason: deleteReason
      });
      toast.success('Restaurant deleted successfully');
      setShowDeleteModal(false);
      setSelectedRestaurant(null);
      setDeleteReason('');
      loadRestaurants();
    } catch (error) {
      console.error('Failed to delete restaurant:', error);
      toast.error('Failed to delete restaurant');
    }
  };

  const handleRestore = (restaurant) => {
    setSelectedRestaurant(restaurant);
    setRestoreReason('');
    setShowRestoreModal(true);
  };

  const confirmRestore = async () => {
    if (!restoreReason.trim() || restoreReason.length < 10) {
      toast.error('Please provide a reason (minimum 10 characters)');
      return;
    }

    try {
      await apiService.admin.restoreRestaurant(selectedRestaurant.id, {
        reason: restoreReason
      });
      toast.success('Restaurant restored successfully');
      setShowRestoreModal(false);
      setSelectedRestaurant(null);
      setRestoreReason('');
      loadRestaurants();
    } catch (error) {
      console.error('Failed to restore restaurant:', error);
      
      // Extract error message from response
      const errorMessage = error.response?.data?.error 
        || error.response?.data?.message 
        || error.message 
        || 'Failed to restore restaurant';
      
      // Show detailed error message
      if (errorMessage.includes('already has an active restaurant')) {
        toast.error(
          'Cannot restore: Partner already has an active restaurant. ' +
          'Please delete the current restaurant first.',
          { duration: 6000 }
        );
      } else {
        toast.error(errorMessage, { duration: 5000 });
      }
    }
  };

  const handlePermanentDelete = (restaurant) => {
    setSelectedRestaurant(restaurant);
    setShowPermanentDeleteModal(true);
  };

  const confirmPermanentDelete = async () => {
    try {
      await apiService.admin.permanentlyDeleteRestaurant(selectedRestaurant.id);
      toast.success('Restaurant permanently deleted. Partner can now create a new restaurant.');
      setShowPermanentDeleteModal(false);
      setSelectedRestaurant(null);
      loadRestaurants();
    } catch (error) {
      console.error('Failed to permanently delete restaurant:', error);
      
      const errorMessage = error.response?.data?.error 
        || error.response?.data?.message 
        || error.message 
        || 'Failed to permanently delete restaurant';
      
      toast.error(errorMessage, { duration: 5000 });
    }
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

  const currentRestaurants = activeTab === 'all' ? restaurants : deletedRestaurants;
  const filteredRestaurants = currentRestaurants.filter(r =>
    r.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    r.cuisine?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  if (loading) {
    return (
      <div className="restaurants-management page-enter">
        <div className="container">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-xl)' }}>
            <div className="skeleton" style={{ height: '1.75rem', width: '14rem' }} />
            <div className="skeleton" style={{ height: '2.5rem', width: '12rem', borderRadius: 'var(--rounded-lg)' }} />
          </div>
          <div className="orders-table">
            <table>
              <thead>
                <tr>
                  {['Name', 'Cuisine', 'Owner', 'Status', 'Rating', 'Actions'].map(h => <th key={h}>{h}</th>)}
                </tr>
              </thead>
              <tbody>
                {Array.from({ length: 8 }).map((_, i) => <TableRowSkeleton key={i} columns={6} />)}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="restaurants-management page-enter">
      <div className="container">
        <div className="page-header">
          <div>
            <h1 className="headline-lg">Restaurants Management</h1>
            <p className="body-md text-muted">Approve and manage restaurants</p>
          </div>
        </div>

        {/* Tabs */}
        <div className="tabs-container">
          <button
            className={`tab ${activeTab === 'all' ? 'active' : ''}`}
            onClick={() => setActiveTab('all')}
          >
            All Restaurants ({restaurants.length})
          </button>
          <button
            className={`tab ${activeTab === 'deleted' ? 'active' : ''}`}
            onClick={() => setActiveTab('deleted')}
          >
            Deleted ({deletedRestaurants.length})
          </button>
        </div>

        <div className="filters-bar">
          <div className="search-box">
            <Search size={18} />
            <input
              type="text"
              placeholder="Search restaurants..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
        </div>

        <div className={`restaurants-grid ${activeTab === 'deleted' ? 'deleted-grid' : ''}`}>
          {filteredRestaurants.map(restaurant => (
            <div key={restaurant.id} className={`restaurant-card ${activeTab === 'deleted' ? 'deleted-card' : ''}`}>
              {restaurant.imageUrl && (
                <div className="restaurant-image">
                  <img src={restaurant.imageUrl} alt={restaurant.name} />
                </div>
              )}
              
              <div className="restaurant-content">
                <h3 className="headline-sm">{restaurant.name}</h3>
                <p className="body-sm text-muted">{restaurant.cuisine}</p>
                <p className="body-sm">{restaurant.address}</p>
                
                <div className="restaurant-meta">
                  {activeTab === 'deleted' ? (
                    <span className="badge badge-error">Deleted</span>
                  ) : (
                    <>
                      <span className={`badge ${restaurant.isApproved ? 'badge-success' : 'badge-warning'}`}>
                        {restaurant.isApproved ? 'Approved' : 'Pending'}
                      </span>
                      <span className={`badge ${restaurant.isOpen ? 'badge-success' : 'badge-error'}`}>
                        {restaurant.isOpen ? 'Open' : 'Closed'}
                      </span>
                    </>
                  )}
                  <span className="rating">⭐ {restaurant.rating?.toFixed(1) || 'N/A'}</span>
                </div>

                <div className="restaurant-actions">
                  {activeTab === 'deleted' ? (
                    <>
                      <button
                        className="btn btn-primary btn-sm"
                        onClick={() => handleRestore(restaurant)}
                      >
                        <CheckCircle size={16} /> Restore
                      </button>
                      <button
                        className="btn btn-danger btn-sm"
                        onClick={() => handlePermanentDelete(restaurant)}
                        title="Permanently delete this restaurant (cannot be undone)"
                      >
                        <Trash2 size={16} /> Delete Permanently
                      </button>
                    </>
                  ) : (
                    <>
                      <button
                        className="btn btn-outline btn-sm"
                        onClick={() => viewRestaurantDetails(restaurant.id)}
                      >
                        <Eye size={16} /> View Details
                      </button>
                      
                      {!restaurant.isApproved && (
                        <button
                          className="btn btn-primary btn-sm"
                          onClick={() => handleApprove(restaurant.id)}
                        >
                          <CheckCircle size={16} /> Approve
                        </button>
                      )}
                      
                      {restaurant.isApproved && (
                        <button
                          className="btn btn-warning btn-sm"
                          onClick={() => handleDisapprove(restaurant)}
                        >
                          <XCircle size={16} /> Disapprove
                        </button>
                      )}
                      
                      <button
                        className="btn btn-danger btn-sm"
                        onClick={() => handleDelete(restaurant)}
                      >
                        <Trash2 size={16} /> Delete
                      </button>
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>

        {filteredRestaurants.length === 0 && (
          <div className="empty-state">
            <p className="body-lg text-muted">
              {activeTab === 'deleted' ? 'No deleted restaurants' : 'No restaurants found'}
            </p>
          </div>
        )}
      </div>

      {/* Restaurant Detail Modal */}
      {showDetailModal && selectedRestaurant && (
        <div className="modal-overlay" onClick={() => setShowDetailModal(false)}>
          <div className="modal-content large-modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Restaurant Details</h3>
              <button className="modal-close" onClick={() => setShowDetailModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <div className="detail-section">
                <h4>Basic Information</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Name:</span>
                    <span className="detail-value">{selectedRestaurant.name}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Cuisine:</span>
                    <span className="detail-value">{selectedRestaurant.cuisine}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Phone:</span>
                    <span className="detail-value"><Phone size={14} /> {selectedRestaurant.phone}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Address:</span>
                    <span className="detail-value"><MapPin size={14} /> {selectedRestaurant.address}, {selectedRestaurant.city}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Status:</span>
                    <span className={`badge ${selectedRestaurant.isApproved ? 'badge-success' : 'badge-warning'}`}>
                      {selectedRestaurant.isApproved ? 'Approved' : 'Pending'}
                    </span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Currently:</span>
                    <span className={`badge ${selectedRestaurant.isOpen ? 'badge-success' : 'badge-error'}`}>
                      {selectedRestaurant.isOpen ? 'Open' : 'Closed'}
                    </span>
                  </div>
                </div>
              </div>

              <div className="detail-section">
                <h4>Business Metrics</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Rating:</span>
                    <span className="detail-value"><Star size={14} /> {selectedRestaurant.rating?.toFixed(1) || 'N/A'} / 5.0</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Total Reviews:</span>
                    <span className="detail-value">{selectedRestaurant.totalReviews || 0}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Total Revenue:</span>
                    <span className="detail-value"><DollarSign size={14} /> {formatCurrency(selectedRestaurant.totalRevenue || 0)}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Total Orders:</span>
                    <span className="detail-value">{selectedRestaurant.totalOrders || 0}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Prep Time:</span>
                    <span className="detail-value"><Clock size={14} /> {selectedRestaurant.prepTimeMinutes} mins</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Delivery Fee:</span>
                    <span className="detail-value">{formatCurrency(selectedRestaurant.deliveryFee)}</span>
                  </div>
                </div>
              </div>

              {selectedRestaurant.description && (
                <div className="detail-section">
                  <h4>Description</h4>
                  <p>{selectedRestaurant.description}</p>
                </div>
              )}

              <div className="detail-section">
                <h4>Account Information</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Registered:</span>
                    <span className="detail-value">{formatDate(selectedRestaurant.createdAt)}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Partner ID:</span>
                    <span className="detail-value">{selectedRestaurant.partnerUserId?.substring(0, 8)}</span>
                  </div>
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => setShowDetailModal(false)}>
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Disapprove Modal */}
      {showDisapproveModal && (
        <div className="modal-overlay" onClick={() => setShowDisapproveModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Disapprove Restaurant</h3>
              <button className="modal-close" onClick={() => setShowDisapproveModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <p>Are you sure you want to disapprove <strong>{selectedRestaurant?.name}</strong>?</p>
              <p className="text-muted">This will change the restaurant status back to Pending.</p>
              <div className="form-group">
                <label>Reason for Disapproval *</label>
                <textarea
                  value={disapproveReason}
                  onChange={(e) => setDisapproveReason(e.target.value)}
                  placeholder="Enter reason for disapproving this restaurant..."
                  rows="4"
                  className="form-control"
                  required
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => setShowDisapproveModal(false)}>
                Cancel
              </button>
              <button className="btn btn-warning" onClick={confirmDisapprove}>
                Confirm Disapproval
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete Modal */}
      {showDeleteModal && (
        <div className="modal-overlay" onClick={() => setShowDeleteModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Delete Restaurant</h3>
              <button className="modal-close" onClick={() => setShowDeleteModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <p>Are you sure you want to delete <strong>{selectedRestaurant?.name}</strong>?</p>
              <p className="text-muted">This action will soft delete the restaurant. It can be recovered later if needed.</p>
              <div className="form-group">
                <label>Reason for Deletion *</label>
                <textarea
                  value={deleteReason}
                  onChange={(e) => setDeleteReason(e.target.value)}
                  placeholder="Enter reason for deleting this restaurant..."
                  rows="4"
                  className="form-control"
                  required
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => setShowDeleteModal(false)}>
                Cancel
              </button>
              <button className="btn btn-danger" onClick={confirmDelete}>
                Confirm Deletion
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Restore Modal */}
      {showRestoreModal && (
        <div className="modal-overlay" onClick={() => setShowRestoreModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Restore Restaurant</h3>
              <button className="modal-close" onClick={() => setShowRestoreModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <p>Are you sure you want to restore <strong>{selectedRestaurant?.name}</strong>?</p>
              {selectedRestaurant?.deletionReason && (
                <div className="info-box">
                  <p className="body-sm"><strong>Original Deletion Reason:</strong></p>
                  <p className="body-sm text-muted">{selectedRestaurant.deletionReason}</p>
                </div>
              )}
              <div className="form-group">
                <label>Reason for Restoration * (minimum 10 characters)</label>
                <textarea
                  value={restoreReason}
                  onChange={(e) => setRestoreReason(e.target.value)}
                  placeholder="Enter reason for restoring this restaurant..."
                  rows="4"
                  className="form-control"
                  required
                />
                <small className="text-muted">{restoreReason.length}/10 characters minimum</small>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => setShowRestoreModal(false)}>
                Cancel
              </button>
              <button 
                className="btn btn-primary" 
                onClick={confirmRestore}
                disabled={restoreReason.length < 10}
              >
                Confirm Restoration
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Permanent Delete Modal */}
      {showPermanentDeleteModal && (
        <div className="modal-overlay" onClick={() => setShowPermanentDeleteModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>⚠️ Permanently Delete Restaurant</h3>
              <button className="modal-close" onClick={() => setShowPermanentDeleteModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <div className="warning-box">
                <p><strong>⚠️ WARNING: This action cannot be undone!</strong></p>
                <p>You are about to permanently delete <strong>{selectedRestaurant?.name}</strong>.</p>
              </div>
              
              <div className="info-box" style={{ marginTop: '1rem' }}>
                <p className="body-sm"><strong>What will happen:</strong></p>
                <ul className="body-sm" style={{ marginLeft: '1.5rem', marginTop: '0.5rem' }}>
                  <li>Restaurant will be completely removed from the database</li>
                  <li>All menu items, categories, and settings will be deleted</li>
                  <li>This action <strong>CANNOT be reversed</strong></li>
                  <li>The partner will be able to create a new restaurant</li>
                </ul>
              </div>

              {selectedRestaurant?.deletionReason && (
                <div className="info-box" style={{ marginTop: '1rem' }}>
                  <p className="body-sm"><strong>Original Deletion Reason:</strong></p>
                  <p className="body-sm text-muted">{selectedRestaurant.deletionReason}</p>
                </div>
              )}

              <p style={{ marginTop: '1rem', color: 'var(--error)', fontWeight: 600 }}>
                Are you absolutely sure you want to proceed?
              </p>
            </div>
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => setShowPermanentDeleteModal(false)}>
                Cancel
              </button>
              <button 
                className="btn btn-danger" 
                onClick={confirmPermanentDelete}
              >
                Yes, Delete Permanently
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
