import { useState, useEffect } from 'react';
import { Search, Filter, UserCheck, UserX, Eye } from 'lucide-react';
import api, { apiService } from '../../services/api';
import toast from 'react-hot-toast';
import { TableRowSkeleton } from '../../components/common/Skeleton';
import './UsersManagement.css';

export default function UsersManagement() {
  const [loading, setLoading] = useState(true);
  const [users, setUsers] = useState([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [selectedUser, setSelectedUser] = useState(null);
  const [showDetailModal, setShowDetailModal] = useState(false);

  useEffect(() => {
    loadUsers();
  }, [roleFilter, statusFilter]);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      if (roleFilter !== 'all') params.append('role', roleFilter);
      if (statusFilter !== 'all') params.append('isActive', statusFilter === 'active');
      
      // Call AuthService directly to get real-time user data
      const res = await api.get(`/gateway/auth/admin/users?${params.toString()}`);
      setUsers(res.data || []);
    } catch (error) {
      console.error('Failed to load users:', error);
      toast.error('Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleStatus = async (userId, currentStatus) => {
    const newStatus = !currentStatus;
    const action = newStatus ? 'activating' : 'deactivating';
    const reason = prompt(`Please provide a reason for ${action} this user:`);
    
    if (reason === null) return; // User cancelled
    
    if (!reason || !reason.trim()) {
      toast.error('Reason is required');
      return;
    }

    try {
      await apiService.admin.toggleUserStatus(userId, {
        isActive: newStatus,
        reason: reason.trim()
      });
      toast.success(`User ${newStatus ? 'activated' : 'deactivated'} successfully`);
      loadUsers();
    } catch (error) {
      console.error('Failed to update user status:', error);
      toast.error('Failed to update user status');
    }
  };

  const handleViewDetails = async (userId) => {
    try {
      const response = await api.get(`/gateway/admin/users/${userId}`);
      setSelectedUser(response.data?.data || response.data);
      setShowDetailModal(true);
    } catch (error) {
      console.error('Failed to fetch user details:', error);
      toast.error('Failed to load user details');
    }
  };

  const handleToggleVerification = async (userId, currentStatus) => {
    const newStatus = !currentStatus;
    const action = newStatus ? 'verify' : 'unverify';
    const reason = prompt(`Please provide a reason for ${action}ing this user's email:`);
    
    if (reason === null) return; // User cancelled
    
    if (!reason || !reason.trim() || reason.trim().length < 5) {
      toast.error('Reason must be at least 5 characters');
      return;
    }

    try {
      const adminUser = JSON.parse(localStorage.getItem('user') || '{}');
      await api.patch(`/gateway/admin/users/${userId}/toggle-verification`, {
        isVerified: newStatus,
        adminId: adminUser.id,
        reason: reason.trim()
      });
      toast.success(`User email ${newStatus ? 'verified' : 'unverified'} successfully`);
      loadUsers();
    } catch (error) {
      console.error('Failed to toggle verification:', error);
      toast.error('Failed to toggle verification status');
    }
  };

  const filteredUsers = users.filter(user =>
    user.fullName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    user.email?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  if (loading) {
    return (
      <div className="users-management page-enter">
        <div className="container">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-xl)' }}>
            <div className="skeleton" style={{ height: '1.75rem', width: '10rem' }} />
            <div className="skeleton" style={{ height: '2.5rem', width: '14rem', borderRadius: 'var(--rounded-lg)' }} />
          </div>
          <div className="orders-table">
            <table>
              <thead>
                <tr>
                  {['Name', 'Email', 'Role', 'Status', 'Actions'].map(h => <th key={h}>{h}</th>)}
                </tr>
              </thead>
              <tbody>
                {Array.from({ length: 8 }).map((_, i) => <TableRowSkeleton key={i} columns={5} />)}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="users-management page-enter">
      <div className="container">
        <div className="page-header">
          <div>
            <h1 className="headline-lg">Users Management</h1>
            <p className="body-md text-muted">Manage all platform users</p>
          </div>
        </div>

        <div className="filters-bar">
          <div className="search-box">
            <Search size={18} />
            <input
              type="text"
              placeholder="Search users..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>

          <div className="filter-group">
            <Filter size={18} />
            <select value={roleFilter} onChange={(e) => setRoleFilter(e.target.value)} className="form-select">
              <option value="all">All Roles</option>
              <option value="Customer">Customer</option>
              <option value="Partner">Partner</option>
              <option value="DeliveryAgent">Delivery Agent</option>
              <option value="Admin">Admin</option>
            </select>
          </div>

          <div className="filter-group">
            <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="form-select">
              <option value="all">All Status</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>
        </div>

        <div className="users-table">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Mobile</th>
                <th>Role</th>
                <th>Status</th>
                <th>Verified</th>
                <th>Joined</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map(user => (
                <tr key={user.id}>
                  <td>{user.fullName}</td>
                  <td>{user.email}</td>
                  <td>{user.mobile || 'N/A'}</td>
                  <td><span className="badge badge-secondary">{user.role}</span></td>
                  <td>
                    <span className={`badge ${user.isActive ? 'badge-success' : 'badge-error'}`}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    {user.isEmailVerified ? (
                      <span className="badge badge-success">✓</span>
                    ) : (
                      <span className="badge badge-warning">✗</span>
                    )}
                  </td>
                  <td>{user.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'N/A'}</td>
                  <td>
                    <div className="action-buttons">
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={() => handleViewDetails(user.id)}
                        title="View Details"
                      >
                        <Eye size={16} />
                      </button>
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={() => handleToggleStatus(user.id, user.isActive)}
                        title={user.isActive ? 'Deactivate' : 'Activate'}
                      >
                        {user.isActive ? <UserX size={16} /> : <UserCheck size={16} />}
                      </button>
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={() => handleToggleVerification(user.id, user.isEmailVerified)}
                        title={user.isEmailVerified ? 'Mark as Unverified' : 'Mark as Verified'}
                      >
                        {user.isEmailVerified ? '✓' : '✗'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {filteredUsers.length === 0 && (
          <div className="empty-state">
            <p className="body-lg text-muted">No users found</p>
          </div>
        )}
      </div>

      {/* User Detail Modal */}
      {showDetailModal && selectedUser && (
        <div className="modal-overlay" onClick={() => setShowDetailModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>User Details</h3>
              <button className="modal-close" onClick={() => setShowDetailModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <div className="detail-section">
                <h4>Personal Information</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Full Name:</span>
                    <span className="detail-value">{selectedUser.fullName}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Email:</span>
                    <span className="detail-value">{selectedUser.email}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Mobile:</span>
                    <span className="detail-value">{selectedUser.mobile || 'N/A'}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Role:</span>
                    <span className="badge badge-secondary">{selectedUser.role}</span>
                  </div>
                </div>
              </div>

              <div className="detail-section">
                <h4>Account Status</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Status:</span>
                    <span className={`badge ${selectedUser.isActive ? 'badge-success' : 'badge-error'}`}>
                      {selectedUser.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Email Verified:</span>
                    <span className={`badge ${selectedUser.isEmailVerified ? 'badge-success' : 'badge-warning'}`}>
                      {selectedUser.isEmailVerified ? 'Yes' : 'No'}
                    </span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">2FA Enabled:</span>
                    <span className={`badge ${selectedUser.twoFactorEnabled ? 'badge-success' : 'badge-secondary'}`}>
                      {selectedUser.twoFactorEnabled ? 'Yes' : 'No'}
                    </span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Wallet Balance:</span>
                    <span className="detail-value">₹{selectedUser.walletBalance?.toFixed(2) || '0.00'}</span>
                  </div>
                </div>
              </div>

              {selectedUser.role === 'DeliveryAgent' && (
                <div className="detail-section">
                  <h4>Delivery Agent Information</h4>
                  <div className="detail-grid">
                    <div className="detail-item">
                      <span className="detail-label">Vehicle Type:</span>
                      <span className="detail-value">{selectedUser.vehicleType || 'N/A'}</span>
                    </div>
                    <div className="detail-item">
                      <span className="detail-label">Vehicle Number:</span>
                      <span className="detail-value">{selectedUser.vehicleNumber || 'N/A'}</span>
                    </div>
                    <div className="detail-item">
                      <span className="detail-label">Approval Status:</span>
                      <span className={`badge ${selectedUser.isApproved ? 'badge-success' : 'badge-warning'}`}>
                        {selectedUser.isApproved ? 'Approved' : 'Pending'}
                      </span>
                    </div>
                    {selectedUser.approvedAt && (
                      <div className="detail-item">
                        <span className="detail-label">Approved At:</span>
                        <span className="detail-value">{new Date(selectedUser.approvedAt).toLocaleString()}</span>
                      </div>
                    )}
                  </div>
                </div>
              )}



              <div className="detail-section">
                <h4>Account Information</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Registered:</span>
                    <span className="detail-value">{new Date(selectedUser.createdAt).toLocaleString()}</span>
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


    </div>
  );
}
