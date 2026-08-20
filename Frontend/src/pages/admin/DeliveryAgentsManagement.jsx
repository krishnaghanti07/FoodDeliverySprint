import { useState, useEffect } from 'react';
import { Bike, Search, Filter, MapPin, Phone, Mail, CheckCircle, XCircle, Clock, Package, Trash2 } from 'lucide-react';
import api, { apiService } from '../../services/api';
import toast from 'react-hot-toast';
import './DeliveryAgentsManagement.css';

export default function DeliveryAgentsManagement() {
  const [agents, setAgents] = useState([]);
  const [pendingAgents, setPendingAgents] = useState([]);
  const [deletedAgents, setDeletedAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('all'); // 'all' | 'pending' | 'deleted'
  const [filters, setFilters] = useState({
    isActive: '',
    isOnline: ''
  });
  const [selectedAgent, setSelectedAgent] = useState(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [showApprovalModal, setShowApprovalModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showRestoreModal, setShowRestoreModal] = useState(false);
  const [approvalAction, setApprovalAction] = useState(null); // 'approve' | 'reject'
  const [approvalNotes, setApprovalNotes] = useState('');
  const [deleteReason, setDeleteReason] = useState('');
  const [restoreReason, setRestoreReason] = useState('');

  useEffect(() => {
    // Sync delivery agents from AuthService on first load
    syncAgents();
  }, []);

  useEffect(() => {
    fetchAgents();
  }, [filters, activeTab]);

  const syncAgents = async () => {
    try {
      await apiService.admin.syncDeliveryAgents();
      console.log('Delivery agents synced successfully');
    } catch (error) {
      console.error('Failed to sync delivery agents:', error);
      // Don't show error to user, just log it
    }
  };

  const fetchAgents = async () => {
    try {
      setLoading(true);
      
      if (activeTab === 'pending') {
        const response = await api.get('/gateway/admin/delivery-agents/pending');
        const pendingData = response.data?.data || response.data || [];
        setPendingAgents(Array.isArray(pendingData) ? pendingData : []);
      } else if (activeTab === 'deleted') {
        // Fetch all agents and filter deleted ones
        const response = await apiService.admin.getAllDeliveryAgents('');
        const agentsData = response.data?.data || response.data || [];
        const allAgents = Array.isArray(agentsData) ? agentsData : [];
        
        // Filter deleted agents (you'll need to add isDeleted field from backend)
        // For now, we'll fetch from AuthService directly
        try {
          const authResponse = await api.get('/gateway/auth/admin/users?role=DeliveryAgent');
          const allUsers = authResponse.data || [];
          const deleted = allUsers.filter(user => user.isDeleted);
          setDeletedAgents(deleted);
        } catch (error) {
          console.error('Failed to fetch deleted agents:', error);
          setDeletedAgents([]);
        }
      } else {
        const params = new URLSearchParams();
        if (filters.isActive !== '') params.append('isActive', filters.isActive);
        if (filters.isOnline !== '') params.append('isOnline', filters.isOnline);

        // Fetch from AuthService to get isDeleted field
        try {
          const authResponse = await api.get('/gateway/auth/admin/users?role=DeliveryAgent');
          const allUsers = authResponse.data || [];
          
          // Filter out deleted users for "All Agents" tab
          const activeUsers = allUsers.filter(user => !user.isDeleted);
          
          // Apply additional filters
          let filtered = activeUsers;
          if (filters.isActive !== '') {
            filtered = filtered.filter(u => u.isActive === (filters.isActive === 'true'));
          }
          
          setAgents(filtered);
        } catch (error) {
          console.error('Failed to fetch agents from AuthService:', error);
          // Fallback to AdminService
          const response = await apiService.admin.getAllDeliveryAgents(params.toString());
          const agentsData = response.data?.data || response.data || [];
          setAgents(Array.isArray(agentsData) ? agentsData : []);
        }
      }
    } catch (error) {
      console.error('Failed to fetch delivery agents:', error);
      toast.error('Failed to load delivery agents');
      setAgents([]);
      setPendingAgents([]);
      setDeletedAgents([]);
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (key, value) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  const clearFilters = () => {
    setFilters({ isActive: '', isOnline: '' });
  };

  const viewAgentDetails = async (agentId) => {
    try {
      const response = await apiService.admin.getDeliveryAgentById(agentId);
      setSelectedAgent(response.data);
      setShowDetailModal(true);
    } catch (error) {
      console.error('Failed to fetch agent details:', error);
      toast.error('Failed to load agent details');
    }
  };

  const toggleAgentStatus = async (agentId, currentStatus) => {
    const newStatus = !currentStatus;
    const reason = prompt(`Please provide a reason for ${newStatus ? 'activating' : 'deactivating'} this agent:`);
    
    if (reason === null) return; // User cancelled
    if (!reason.trim()) {
      toast.error('Reason is required');
      return;
    }

    try {
      await apiService.admin.updateDeliveryAgentStatus(agentId, {
        isActive: newStatus,
        reason: reason.trim()
      });
      toast.success(`Agent ${newStatus ? 'activated' : 'deactivated'} successfully`);
      fetchAgents();
      if (selectedAgent?.id === agentId) {
        setShowDetailModal(false);
        setSelectedAgent(null);
      }
    } catch (error) {
      console.error('Failed to update agent status:', error);
      const errorMessage = error.response?.data?.error || error.message;
      
      // Check if it's a deleted user error
      if (errorMessage.includes('deleted user') || errorMessage.includes('restore')) {
        toast.error('This agent is deleted. Please use the Restore function from the Deleted tab.');
      } else {
        toast.error(errorMessage || 'Failed to update agent status');
      }
    }
  };

  const handleApprovalClick = (agent, action) => {
    setSelectedAgent(agent);
    setApprovalAction(action);
    setApprovalNotes('');
    setShowApprovalModal(true);
  };

  const handleApprovalSubmit = async () => {
    if (!selectedAgent || !approvalAction) return;

    try {
      const endpoint = `/gateway/admin/delivery-agents/${selectedAgent.id}/${approvalAction}`;
      const payload = {
        notes: approvalNotes || (approvalAction === 'approve' ? 'Approved by admin' : 'Rejected by admin')
      };

      await api.post(endpoint, payload);
      toast.success(`Agent ${approvalAction}d successfully`);
      
      setShowApprovalModal(false);
      setSelectedAgent(null);
      setApprovalNotes('');
      fetchAgents();
    } catch (error) {
      console.error(`Error ${approvalAction}ing agent:`, error);
      toast.error(`Failed to ${approvalAction} agent. Please try again.`);
    }
  };

  const handleDelete = (agent) => {
    setSelectedAgent(agent);
    setDeleteReason('');
    setShowDeleteModal(true);
  };

  const confirmDelete = async () => {
    if (!deleteReason.trim()) {
      toast.error('Please provide a reason for deletion');
      return;
    }

    if (deleteReason.trim().length < 10) {
      toast.error('Reason must be at least 10 characters long');
      return;
    }

    try {
      await apiService.admin.softDeleteAgent(selectedAgent.id, {
        reason: deleteReason.trim()
      });
      toast.success('Agent deleted successfully');
      setShowDeleteModal(false);
      setSelectedAgent(null);
      setDeleteReason('');
      fetchAgents();
    } catch (error) {
      console.error('Failed to delete agent:', error);
      toast.error('Failed to delete agent');
    }
  };

  const handleRestore = (agent) => {
    setSelectedAgent(agent);
    setRestoreReason('');
    setShowRestoreModal(true);
  };

  const confirmRestore = async () => {
    if (!restoreReason.trim()) {
      toast.error('Please provide a reason for restoration');
      return;
    }

    if (restoreReason.trim().length < 10) {
      toast.error('Reason must be at least 10 characters long');
      return;
    }

    try {
      await apiService.admin.restoreAgent(selectedAgent.id, {
        reason: restoreReason.trim()
      });
      toast.success('Agent restored successfully');
      setShowRestoreModal(false);
      setSelectedAgent(null);
      setRestoreReason('');
      fetchAgents();
    } catch (error) {
      console.error('Failed to restore agent:', error);
      const errorMessage = error.response?.data?.error || error.response?.data?.details || error.message;
      toast.error(errorMessage || 'Failed to restore agent');
    }
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
      <div className="agents-management container">
        <div className="loading-state">
          <div className="spinner"></div>
          <p>Loading delivery agents...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="agents-management container">
      <div className="agents-header">
        <h1><Bike size={32} /> Delivery Agents Management</h1>
      </div>

      <div className="tabs-container">
        <button
          className={`tab ${activeTab === 'all' ? 'active' : ''}`}
          onClick={() => setActiveTab('all')}
        >
          All Agents ({agents.length})
        </button>
        <button
          className={`tab ${activeTab === 'pending' ? 'active' : ''}`}
          onClick={() => setActiveTab('pending')}
        >
          Pending Approval ({pendingAgents.length})
        </button>
        <button
          className={`tab ${activeTab === 'deleted' ? 'active' : ''}`}
          onClick={() => setActiveTab('deleted')}
        >
          Deleted ({deletedAgents.length})
        </button>
      </div>

      {activeTab === 'all' && (
        <>
          <div className="agents-filters-card">
            <div className="filters-header">
              <Filter size={20} />
              <span>Filters</span>
            </div>
            <div className="filters-grid">
              <div className="filter-group">
                <label>Status</label>
                <select value={filters.isActive} onChange={(e) => handleFilterChange('isActive', e.target.value)}>
                  <option value="">All</option>
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
              <div className="filter-group">
                <label>Online Status</label>
                <select value={filters.isOnline} onChange={(e) => handleFilterChange('isOnline', e.target.value)}>
                  <option value="">All</option>
                  <option value="true">Online</option>
                  <option value="false">Offline</option>
                </select>
              </div>
            </div>
            {(filters.isActive !== '' || filters.isOnline !== '') && (
              <button className="btn-clear-filters" onClick={clearFilters}>
                Clear Filters
              </button>
            )}
          </div>

          {agents.length === 0 ? (
            <div className="empty-state">
              <Bike size={64} />
              <h3>No Delivery Agents Found</h3>
              <p>No agents match your current filters</p>
            </div>
          ) : (
            <div className="agents-grid">
              {agents.map(agent => (
                <div key={agent.id} className="agent-card">
                  <div className="agent-header">
                    <div className="agent-avatar">
                      <Bike size={32} />
                    </div>
                    <div className="agent-badges">
                      <span className={`status-badge ${agent.isActive ? 'active' : 'inactive'}`}>
                        <span className={`status-indicator ${agent.isActive ? 'active' : 'inactive'}`}></span>
                        {agent.isActive ? 'Active' : 'Inactive'}
                      </span>
                      {agent.isOnline && (
                        <span className="status-badge online">
                          <span className="status-indicator online"></span>
                          Online
                        </span>
                      )}
                    </div>
                  </div>

                  <div className="agent-info">
                    <h3>{agent.fullName}</h3>
                    <div className="info-row">
                      <Mail size={16} />
                      <span>{agent.email}</span>
                    </div>
                    <div className="info-row">
                      <Phone size={16} />
                      <span>{agent.phoneNumber || 'N/A'}</span>
                    </div>
                    {agent.currentLocation && (
                      <div className="info-row">
                        <MapPin size={16} />
                        <span>{agent.currentLocation}</span>
                      </div>
                    )}
                  </div>

                  <div className="agent-stats">
                    <div className="stat-item">
                      <Package size={20} />
                      <div className="stat-content">
                        <span className="stat-value">{agent.totalDeliveries || 0}</span>
                        <span className="stat-label">Total Deliveries</span>
                      </div>
                    </div>
                    <div className="stat-item">
                      <Clock size={20} />
                      <div className="stat-content">
                        <span className="stat-value">{agent.activeDeliveries || 0}</span>
                        <span className="stat-label">Active</span>
                      </div>
                    </div>
                  </div>

                  <div className="agent-actions">
                    <button
                      className="btn btn-outline btn-sm"
                      onClick={() => viewAgentDetails(agent.id)}
                    >
                      View Details
                    </button>
                    {agent.isDeleted ? (
                      <button
                        className="btn btn-warning btn-sm"
                        disabled
                        title="This agent is deleted. Use the Deleted tab to restore."
                      >
                        Deleted - Use Restore
                      </button>
                    ) : (
                      <>
                        <button
                          className={`btn btn-sm ${agent.isActive ? 'btn-danger' : 'btn-success'}`}
                          onClick={() => toggleAgentStatus(agent.id, agent.isActive)}
                        >
                          {agent.isActive ? <XCircle size={16} /> : <CheckCircle size={16} />}
                          {agent.isActive ? 'Deactivate' : 'Activate'}
                        </button>
                        <button
                          className="btn btn-danger btn-sm"
                          onClick={() => handleDelete(agent)}
                        >
                          <Trash2 size={16} />
                          Delete
                        </button>
                      </>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}

      {activeTab === 'pending' && (
        <>
          {pendingAgents.length === 0 ? (
            <div className="empty-state">
              <Bike size={64} />
              <h3>No Pending Approvals</h3>
              <p>All delivery agents have been reviewed</p>
            </div>
          ) : (
            <div className="agents-grid">
              {pendingAgents.map(agent => (
                <div key={agent.id} className="agent-card pending-card">
                  <div className="pending-badge">
                    <Clock size={16} />
                    <span>Pending Approval</span>
                  </div>
                  
                  <div className="agent-header">
                    <div className="agent-avatar">
                      <Bike size={32} />
                    </div>
                  </div>

                  <div className="agent-info">
                    <h3>{agent.fullName}</h3>
                    <div className="info-row">
                      <Mail size={16} />
                      <span>{agent.email}</span>
                    </div>
                    <div className="info-row">
                      <Phone size={16} />
                      <span>{agent.mobile || 'N/A'}</span>
                    </div>
                    {agent.vehicleType && (
                      <div className="info-row">
                        <Bike size={16} />
                        <span>{agent.vehicleType} - {agent.vehicleNumber || 'N/A'}</span>
                      </div>
                    )}
                    <div className="info-row">
                      <Clock size={16} />
                      <span>Registered: {formatDate(agent.createdAt)}</span>
                    </div>
                  </div>

                  <div className="agent-actions approval-actions">
                    <button
                      className="btn btn-success btn-sm"
                      onClick={() => handleApprovalClick(agent, 'approve')}
                    >
                      <CheckCircle size={16} />
                      Approve
                    </button>
                    <button
                      className="btn btn-danger btn-sm"
                      onClick={() => handleApprovalClick(agent, 'reject')}
                    >
                      <XCircle size={16} />
                      Reject
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}

      {activeTab === 'deleted' && (
        <>
          {deletedAgents.length === 0 ? (
            <div className="empty-state">
              <Trash2 size={64} />
              <h3>No Deleted Agents</h3>
              <p>No agents have been deleted</p>
            </div>
          ) : (
            <div className="agents-grid">
              {deletedAgents.map(agent => (
                <div key={agent.id} className="agent-card deleted-card">
                  <div className="deleted-badge">
                    <Trash2 size={16} />
                    <span>Deleted</span>
                  </div>
                  
                  <div className="agent-header">
                    <div className="agent-avatar">
                      <Bike size={32} />
                    </div>
                  </div>

                  <div className="agent-info">
                    <h3>{agent.fullName}</h3>
                    <div className="info-row">
                      <Mail size={16} />
                      <span>{agent.email}</span>
                    </div>
                    <div className="info-row">
                      <Phone size={16} />
                      <span>{agent.mobile || 'N/A'}</span>
                    </div>
                    {agent.vehicleType && (
                      <div className="info-row">
                        <Bike size={16} />
                        <span>{agent.vehicleType}</span>
                      </div>
                    )}
                    {agent.deletedAt && (
                      <div className="info-row">
                        <Clock size={16} />
                        <span>Deleted: {formatDate(agent.deletedAt)}</span>
                      </div>
                    )}
                    {agent.deletionReason && (
                      <div className="info-row deletion-reason">
                        <span className="detail-label">Reason:</span>
                        <span className="detail-value">{agent.deletionReason}</span>
                      </div>
                    )}
                  </div>

                  <div className="agent-actions">
                    <button
                      className="btn btn-success btn-sm"
                      onClick={() => handleRestore(agent)}
                    >
                      <CheckCircle size={16} />
                      Restore Agent
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}

      {showApprovalModal && (
        <div className="modal-overlay" onClick={() => setShowApprovalModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{approvalAction === 'approve' ? 'Approve' : 'Reject'} Delivery Agent</h3>
              <button className="modal-close" onClick={() => setShowApprovalModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <div className="detail-section">
                <h4>Agent Information</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Name:</span>
                    <span className="detail-value">{selectedAgent?.fullName}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Email:</span>
                    <span className="detail-value">{selectedAgent?.email}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Vehicle:</span>
                    <span className="detail-value">{selectedAgent?.vehicleType} - {selectedAgent?.vehicleNumber}</span>
                  </div>
                </div>
              </div>
              
              <div className="form-group">
                <label>Notes (Optional):</label>
                <textarea
                  value={approvalNotes}
                  onChange={(e) => setApprovalNotes(e.target.value)}
                  placeholder={`Enter reason for ${approvalAction}...`}
                  rows={4}
                  className="form-control"
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => setShowApprovalModal(false)}>
                Cancel
              </button>
              <button
                className={`btn ${approvalAction === 'approve' ? 'btn-success' : 'btn-danger'}`}
                onClick={handleApprovalSubmit}
              >
                Confirm {approvalAction === 'approve' ? 'Approval' : 'Rejection'}
              </button>
            </div>
          </div>
        </div>
      )}

      {showDetailModal && selectedAgent && (
        <div className="modal-overlay" onClick={() => setShowDetailModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Delivery Agent Details</h3>
              <button className="modal-close" onClick={() => setShowDetailModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <div className="detail-section">
                <h4>Personal Information</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Full Name:</span>
                    <span className="detail-value">{selectedAgent.fullName}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Email:</span>
                    <span className="detail-value">{selectedAgent.email}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Phone:</span>
                    <span className="detail-value">{selectedAgent.phoneNumber || 'N/A'}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Status:</span>
                    <span className={`status-badge ${selectedAgent.isActive ? 'active' : 'inactive'}`}>
                      {selectedAgent.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </div>
                </div>
              </div>

              <div className="detail-section">
                <h4>Delivery Statistics</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Total Deliveries:</span>
                    <span className="detail-value">{selectedAgent.totalDeliveries || 0}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Active Deliveries:</span>
                    <span className="detail-value">{selectedAgent.activeDeliveries || 0}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Completed Today:</span>
                    <span className="detail-value">{selectedAgent.completedToday || 0}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Average Rating:</span>
                    <span className="detail-value">{selectedAgent.averageRating?.toFixed(1) || 'N/A'}</span>
                  </div>
                </div>
              </div>

              {selectedAgent.currentLocation && (
                <div className="detail-section">
                  <h4>Current Location</h4>
                  <div className="detail-item">
                    <MapPin size={16} />
                    <span>{selectedAgent.currentLocation}</span>
                  </div>
                </div>
              )}

              <div className="detail-section">
                <h4>Account Information</h4>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Joined:</span>
                    <span className="detail-value">{formatDate(selectedAgent.createdAt)}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Last Active:</span>
                    <span className="detail-value">{formatDate(selectedAgent.lastActiveAt)}</span>
                  </div>
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button
                className={`btn ${selectedAgent.isActive ? 'btn-danger' : 'btn-success'}`}
                onClick={() => {
                  toggleAgentStatus(selectedAgent.id, selectedAgent.isActive);
                }}
              >
                {selectedAgent.isActive ? 'Deactivate Agent' : 'Activate Agent'}
              </button>
              <button className="btn btn-outline" onClick={() => setShowDetailModal(false)}>
                Close
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
              <h3>Delete Delivery Agent</h3>
              <button className="modal-close" onClick={() => setShowDeleteModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <p>Are you sure you want to delete <strong>{selectedAgent?.fullName}</strong>?</p>
              <p className="text-muted">This action will soft delete the agent. It can be recovered later if needed.</p>
              <div className="form-group">
                <label>Reason for Deletion * (minimum 10 characters)</label>
                <textarea
                  value={deleteReason}
                  onChange={(e) => setDeleteReason(e.target.value)}
                  placeholder="Enter reason for deleting this agent (at least 10 characters)..."
                  rows="4"
                  className="form-control"
                  required
                  minLength={10}
                />
                <small className="text-muted">
                  {deleteReason.length}/10 characters minimum
                </small>
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
              <h3>Restore Delivery Agent</h3>
              <button className="modal-close" onClick={() => setShowRestoreModal(false)}>&times;</button>
            </div>
            <div className="modal-body">
              <p>Are you sure you want to restore <strong>{selectedAgent?.fullName}</strong>?</p>
              <p className="text-muted">This will reactivate the agent and allow them to login again.</p>
              
              {selectedAgent?.deletionReason && (
                <div className="alert alert-info">
                  <strong>Original Deletion Reason:</strong>
                  <p>{selectedAgent.deletionReason}</p>
                </div>
              )}
              
              <div className="form-group">
                <label>Reason for Restoration * (minimum 10 characters)</label>
                <textarea
                  value={restoreReason}
                  onChange={(e) => setRestoreReason(e.target.value)}
                  placeholder="Enter reason for restoring this agent (at least 10 characters)..."
                  rows="4"
                  className="form-control"
                  required
                  minLength={10}
                />
                <small className="text-muted">
                  {restoreReason.length}/10 characters minimum
                </small>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-outline" onClick={() => setShowRestoreModal(false)}>
                Cancel
              </button>
              <button className="btn btn-success" onClick={confirmRestore}>
                Confirm Restoration
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
