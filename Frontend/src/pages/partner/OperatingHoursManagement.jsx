import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Clock, Save, ArrowLeft, AlertCircle } from 'lucide-react';
import api, { apiService } from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import toast from 'react-hot-toast';
import './OperatingHoursManagement.css';

const DAYS_OF_WEEK = [
  { value: 0, label: 'Sunday' },
  { value: 1, label: 'Monday' },
  { value: 2, label: 'Tuesday' },
  { value: 3, label: 'Wednesday' },
  { value: 4, label: 'Thursday' },
  { value: 5, label: 'Friday' },
  { value: 6, label: 'Saturday' },
];

export default function OperatingHoursManagement() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [restaurant, setRestaurant] = useState(null);
  const [hours, setHours] = useState([]);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      
      // Get partner's restaurant
      const restaurantsRes = await api.get(API_ENDPOINTS.catalog.restaurantsMyPartner);
      const restaurantData = restaurantsRes.data?.data || restaurantsRes.data;
      const restaurantsList = Array.isArray(restaurantData) ? restaurantData : [];
      const myRestaurant = restaurantsList[0];
      
      if (!myRestaurant) {
        toast.error('No restaurant found. Please create one first.');
        navigate('/partner');
        return;
      }
      
      setRestaurant(myRestaurant);
      
      // Get existing operating hours
      const hoursRes = await apiService.catalog.getOperatingHours(myRestaurant.id);
      const existingHours = hoursRes.data?.data || hoursRes.data || [];
      
      // Initialize hours for all days
      const initialHours = DAYS_OF_WEEK.map(day => {
        const existing = existingHours.find(h => h.dayOfWeek === day.value);
        return existing || {
          dayOfWeek: day.value,
          openTime: '09:00',
          closeTime: '22:00',
          isClosed: false,
        };
      });
      
      setHours(initialHours);
    } catch (error) {
      console.error('Failed to load operating hours:', error);
      toast.error('Failed to load operating hours');
    } finally {
      setLoading(false);
    }
  };

  const handleTimeChange = (dayOfWeek, field, value) => {
    setHours(prev => prev.map(h => 
      h.dayOfWeek === dayOfWeek ? { ...h, [field]: value } : h
    ));
  };

  const handleToggleClosed = (dayOfWeek) => {
    setHours(prev => prev.map(h => 
      h.dayOfWeek === dayOfWeek ? { ...h, isClosed: !h.isClosed } : h
    ));
  };

  const handleSave = async () => {
    // Validate times
    for (const hour of hours) {
      if (!hour.isClosed) {
        if (!hour.openTime || !hour.closeTime) {
          toast.error(`Please set both open and close times for ${DAYS_OF_WEEK[hour.dayOfWeek].label}`);
          return;
        }
        
        // Convert to comparable format
        const openMinutes = timeToMinutes(hour.openTime);
        const closeMinutes = timeToMinutes(hour.closeTime);
        
        if (openMinutes >= closeMinutes) {
          toast.error(`Close time must be after open time for ${DAYS_OF_WEEK[hour.dayOfWeek].label}`);
          return;
        }
      }
    }

    setSaving(true);
    try {
      // Convert time strings to TimeSpan format (HH:mm:ss)
      const hoursData = hours.map(h => ({
        dayOfWeek: h.dayOfWeek,
        openTime: h.isClosed ? '00:00:00' : `${h.openTime}:00`,
        closeTime: h.isClosed ? '00:00:00' : `${h.closeTime}:00`,
        isClosed: h.isClosed,
      }));
      
      await apiService.catalog.setOperatingHours(restaurant.id, hoursData);
      toast.success('Operating hours updated successfully!');
    } catch (error) {
      console.error('Failed to save operating hours:', error);
      const msg = error.response?.data?.message || 'Failed to save operating hours';
      toast.error(msg);
    } finally {
      setSaving(false);
    }
  };

  const timeToMinutes = (timeStr) => {
    const [hours, minutes] = timeStr.split(':').map(Number);
    return hours * 60 + minutes;
  };

  const handleCopyToAll = (dayOfWeek) => {
    const sourceDay = hours.find(h => h.dayOfWeek === dayOfWeek);
    if (!sourceDay) return;
    
    if (window.confirm(`Copy ${DAYS_OF_WEEK[dayOfWeek].label}'s hours to all other days?`)) {
      setHours(prev => prev.map(h => ({
        ...h,
        openTime: sourceDay.openTime,
        closeTime: sourceDay.closeTime,
        isClosed: sourceDay.isClosed,
      })));
      toast.success('Hours copied to all days');
    }
  };

  if (loading) {
    return (
      <div className="operating-hours-page page-enter">
        <div className="container">
          <div className="loading-spinner">Loading...</div>
        </div>
      </div>
    );
  }

  if (!restaurant) {
    return (
      <div className="operating-hours-page page-enter">
        <div className="container">
          <p className="text-muted">No restaurant found</p>
        </div>
      </div>
    );
  }

  return (
    <div className="operating-hours-page page-enter">
      <div className="container">
        <div className="page-header">
          <button className="btn btn-text" onClick={() => navigate('/partner')}>
            <ArrowLeft size={20} /> Back to Dashboard
          </button>
        </div>

        <div className="hours-header">
          <div>
            <h1 className="headline-xl">
              <Clock size={32} /> Operating Hours
            </h1>
            <p className="body-lg text-muted">{restaurant.name}</p>
          </div>
          <button 
            className="btn btn-primary btn-lg" 
            onClick={handleSave}
            disabled={saving}
          >
            <Save size={20} /> {saving ? 'Saving...' : 'Save Changes'}
          </button>
        </div>

        <div className="info-banner">
          <AlertCircle size={20} />
          <div>
            <strong>Note:</strong> These hours will be displayed to customers when they view your restaurant. 
            Make sure to keep them updated to avoid confusion.
          </div>
        </div>

        <div className="hours-grid">
          {hours.map((hour) => {
            const day = DAYS_OF_WEEK.find(d => d.value === hour.dayOfWeek);
            return (
              <div key={hour.dayOfWeek} className="hour-card">
                <div className="hour-card-header">
                  <h3 className="headline-sm">{day.label}</h3>
                  <div className="hour-actions">
                    <button
                      className="btn btn-text btn-sm"
                      onClick={() => handleCopyToAll(hour.dayOfWeek)}
                      title="Copy to all days"
                    >
                      Copy to All
                    </button>
                    <label className="toggle-switch">
                      <input
                        type="checkbox"
                        checked={hour.isClosed}
                        onChange={() => handleToggleClosed(hour.dayOfWeek)}
                      />
                      <span className="toggle-slider"></span>
                      <span className="toggle-label">Closed</span>
                    </label>
                  </div>
                </div>

                {!hour.isClosed ? (
                  <div className="time-inputs">
                    <div className="time-input-group">
                      <label className="label-sm">Open Time</label>
                      <input
                        type="time"
                        value={hour.openTime}
                        onChange={(e) => handleTimeChange(hour.dayOfWeek, 'openTime', e.target.value)}
                        className="time-input"
                      />
                    </div>
                    <span className="time-separator">to</span>
                    <div className="time-input-group">
                      <label className="label-sm">Close Time</label>
                      <input
                        type="time"
                        value={hour.closeTime}
                        onChange={(e) => handleTimeChange(hour.dayOfWeek, 'closeTime', e.target.value)}
                        className="time-input"
                      />
                    </div>
                  </div>
                ) : (
                  <div className="closed-indicator">
                    <p className="body-md text-muted">Restaurant is closed on this day</p>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
