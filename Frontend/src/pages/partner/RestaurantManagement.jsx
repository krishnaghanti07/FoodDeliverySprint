import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Store, Edit, MapPin, Phone, DollarSign, Clock, Star, CheckCircle, XCircle, AlertCircle } from 'lucide-react';
import { apiService } from '../../services/api';
import toast from 'react-hot-toast';
import './RestaurantManagement.css';

export default function RestaurantManagement() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [restaurant, setRestaurant] = useState(null);
  const [toggling, setToggling] = useState(false);

  useEffect(() => {
    loadRestaurant();
  }, []);

  const loadRestaurant = async () => {
    try {
      setLoading(true);
      // First get the restaurant list to get the ID
      const listResponse = await apiService.catalog.getMyRestaurants();
      const listData = listResponse.data?.data || listResponse.data;
      const restaurants = Array.isArray(listData) ? listData : [];
      
      if (restaurants.length > 0) {
        // Then get the full restaurant details with all fields
        const detailResponse = await apiService.catalog.getRestaurantById(restaurants[0].id);
        const restaurantDetail = detailResponse.data?.data || detailResponse.data;
        setRestaurant(restaurantDetail);
      } else {
        setRestaurant(null);
      }
    } catch (error) {
      console.error('Failed to load restaurant:', error);
      toast.error('Failed to load restaurant details');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleOpen = async () => {
    if (!restaurant) return;
    
    try {
      setToggling(true);
      const response = await apiService.catalog.toggleRestaurantOpen(restaurant.id);
      const newStatus = response.data?.data;
      
      setRestaurant(prev => ({
        ...prev,
        isOpen: newStatus
      }));
      
      toast.success(`Restaurant is now ${newStatus ? 'Open' : 'Closed'}`);
    } catch (error) {
      console.error('Failed to toggle status:', error);
      toast.error('Failed to update restaurant status');
    } finally {
      setToggling(false);
    }
  };

  if (loading) {
    return (
      <div className="restaurant-management page-enter">
        <div className="container">
          <div className="loading-spinner">Loading restaurant details...</div>
        </div>
      </div>
    );
  }

  if (!restaurant) {
    return (
      <div className="restaurant-management page-enter">
        <div className="container">
          <div className="empty-state">
            <Store size={64} className="text-muted" />
            <h2 className="headline-lg">No Restaurant Found</h2>
            <p className="body-lg text-muted">You haven't registered a restaurant yet. Register one to start managing your business.</p>
            <button className="btn btn-primary" onClick={() => navigate('/partner/restaurant/new')}>
              <Store size={18} /> Register Restaurant
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="restaurant-management page-enter">
      <div className="container">
        {/* Header */}
        <div className="page-header">
          <div>
            <h1 className="headline-lg">
              <Store size={32} /> My Restaurant
            </h1>
            <p className="body-md text-muted">Manage your restaurant details and settings</p>
          </div>
          <div className="header-actions">
            <button 
              className="btn btn-outline"
              onClick={() => navigate(`/partner/restaurant/${restaurant.id}/edit`)}
            >
              <Edit size={18} /> Edit Details
            </button>
            <button 
              className={`btn ${restaurant.isOpen ? 'btn-error' : 'btn-success'}`}
              onClick={handleToggleOpen}
              disabled={toggling}
            >
              {toggling ? 'Updating...' : (restaurant.isOpen ? 'Close Restaurant' : 'Open Restaurant')}
            </button>
          </div>
        </div>

        {/* Approval Status Banner */}
        {!restaurant.isApproved && (
          <div className="alert alert-warning">
            <AlertCircle size={20} />
            <div>
              <strong>Pending Approval</strong>
              <p>Your restaurant is awaiting admin approval. You'll be notified once it's approved.</p>
            </div>
          </div>
        )}

        {/* Restaurant Details */}
        <div className="restaurant-details">
          {/* Main Info Card */}
          <div className="card">
            <div className="card-body">
              <div className="restaurant-header">
                {restaurant.logoUrl && (
                  <div className="restaurant-logo">
                    <img src={restaurant.logoUrl} alt={restaurant.name} />
                  </div>
                )}
                <div className="restaurant-info">
                  <div className="restaurant-title">
                    <h2 className="headline-lg">{restaurant.name}</h2>
                    <div className="status-badges">
                      <span className={`badge ${restaurant.isOpen ? 'badge-success' : 'badge-error'}`}>
                        {restaurant.isOpen ? (
                          <><CheckCircle size={14} /> Open</>
                        ) : (
                          <><XCircle size={14} /> Closed</>
                        )}
                      </span>
                      <span className={`badge ${restaurant.isApproved ? 'badge-success' : 'badge-warning'}`}>
                        {restaurant.isApproved ? (
                          <><CheckCircle size={14} /> Approved</>
                        ) : (
                          <><AlertCircle size={14} /> Pending Approval</>
                        )}
                      </span>
                    </div>
                  </div>
                  <p className="body-lg text-muted">{restaurant.cuisine}</p>
                  {restaurant.description && (
                    <p className="body-md">{restaurant.description}</p>
                  )}
                  <div className="restaurant-rating">
                    <Star size={20} fill="var(--warning)" color="var(--warning)" />
                    <span className="headline-sm">{restaurant.rating?.toFixed(1) || '0.0'}</span>
                    <span className="body-sm text-muted">Rating</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Details Grid */}
          <div className="details-grid">
            {/* Location Card */}
            <div className="card">
              <div className="card-body">
                <h3 className="headline-md">
                  <MapPin size={20} /> Location
                </h3>
                <div className="detail-item">
                  <span className="detail-label">Address</span>
                  <span className="detail-value">{restaurant.address}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">City</span>
                  <span className="detail-value">{restaurant.city}</span>
                </div>
              </div>
            </div>

            {/* Contact Card */}
            <div className="card">
              <div className="card-body">
                <h3 className="headline-md">
                  <Phone size={20} /> Contact
                </h3>
                <div className="detail-item">
                  <span className="detail-label">Phone</span>
                  <span className="detail-value">{restaurant.phone}</span>
                </div>
              </div>
            </div>

            {/* Delivery Settings Card */}
            <div className="card">
              <div className="card-body">
                <h3 className="headline-md">
                  <DollarSign size={20} /> Delivery Settings
                </h3>
                <div className="detail-item">
                  <span className="detail-label">Delivery Fee</span>
                  <span className="detail-value">₹{restaurant.deliveryFee?.toFixed(2) || '0.00'}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Minimum Order</span>
                  <span className="detail-value">₹{restaurant.minOrderAmount?.toFixed(2) || '0.00'}</span>
                </div>
              </div>
            </div>

            {/* Timing Card */}
            <div className="card">
              <div className="card-body">
                <h3 className="headline-md">
                  <Clock size={20} /> Timing
                </h3>
                <div className="detail-item">
                  <span className="detail-label">Prep Time</span>
                  <span className="detail-value">{restaurant.prepTimeMinutes || 30} minutes</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="quick-actions-section">
          <h2 className="headline-md">Quick Actions</h2>
          <div className="actions-grid">
            <button className="action-card" onClick={() => navigate('/partner/menu')}>
              <Store size={32} />
              <h3 className="headline-sm">Manage Menu</h3>
              <p className="body-sm text-muted">Add, edit, or remove menu items</p>
            </button>

            <button className="action-card" onClick={() => navigate('/partner/orders')}>
              <Store size={32} />
              <h3 className="headline-sm">View Orders</h3>
              <p className="body-sm text-muted">Manage incoming orders</p>
            </button>

            <button className="action-card" onClick={() => navigate('/partner/coupons')}>
              <Store size={32} />
              <h3 className="headline-sm">Manage Coupons</h3>
              <p className="body-sm text-muted">Create and manage discount coupons</p>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
