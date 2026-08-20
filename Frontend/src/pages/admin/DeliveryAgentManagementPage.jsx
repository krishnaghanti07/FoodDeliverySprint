import { useState, useEffect } from 'react';
import { api } from '../../services/api';
import './DeliveryAgentManagementPage.css';

const DeliveryAgentManagementPage = () => {
  const [agents, setAgents] = useState([]);
  const [pendingAgents, setPendingAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('pending'); // 'pending' | 'all'
  const [selectedAgent, setSelectedAgent] = useState(null);
  const [showApprovalModal, setShowApprovalModal] = useState(false);
  const [approvalAction, setApprovalAction] = useState(null); // 'approve' | 'reject'
  const [approvalNotes, setApprovalNotes] = useState('');
  const [processing, setProcessing] = useState(false);

  useEffect(() => {
    fetchAgents();
  }, [activeTab]);

  const fetchAgents = async () => {
    try {
      setLoading(true);
      if (activeTab === 'pending') {
        const response = await api.get('/gateway/admin/delivery-agents/pending');
        setPendingAgents(response.data);
      } else {
        const response = await api.get('/gateway/admin/delivery-agents');
        setAgents(response.data);
      }
    } catch (error) {
      console.error('Error fetching agents:', error);
    } finally {
      setLoading(false);
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
      setProcessing(true);
      const endpoint = `/gateway/admin/delivery-agents/${selectedAgent.id}/${approvalAction}`;
      const payload = {
        notes: approvalNotes || (approvalAction === 'approve' ? 'Approved by admin' : 'Rejected by admin')
      };

      await api.post(endpoint, payload);
      
      setShowApprovalModal(false);
      setSelectedAgent(null);
      setApprovalNotes('');
      fetchAgents();
    } catch (error) {
      console.error(`Error ${approvalAction}ing agent:`, error);
      alert(`Failed to ${approvalAction} agent. Please try again.`);
    } finally {
      setProcessing(false);
    }
  };

  const getStatusBadge = (agent) => {
    if (!agent.isApproved) {
      return <span className="status-badge pending">Pending Approval</span>;
    }
    if (!agent.isActive) {
      return <span className="status-badge inactive">Inactive</span>;
    }
    if (agent.isAvailableForDelivery) {
      return <span className="status-badge available">Available</span>;
    }
    return <span className="status-badge unavailable">Unavailable</span>;
  };

  const renderAgentCard = (agent) => (
    <div key={agent.id} className="agent-card">
      <div className="agent-header">
        <div className="agent-info">
          <h3>{agent.fullName}</h3>
          <p className="agent-email">{agent.email}</p>
          <p className="agent-mobile">{agent.mobile}</p>
        </div>
        <div className="agent-status">
          {getStatusBadge(agent)}
        </div>
      </div>

      <div className="agent-details">
        <div className="detail-row">
          <span className="detail-label">Vehicle Type:</span>
          <span className="detail-value">{agent.vehicleType || 'N/A'}</span>
        </div>
        <div className="detail-row">
          <span className="detail-label">Vehicle Number:</span>
          <span className="detail-value">{agent.vehicleNumber || 'N/A'}</span>
        </div>
        <div className="detail-row">
          <span className="detail-label">Total Deliveries:</span>
          <span className="detail-value">{agent.totalDeliveries || 0}</span>
        </div>
        <div className="detail-row">
          <span className="detail-label">Average Rating:</span>
          <span className="detail-value">
            {agent.averageRating ? `⭐ ${agent.averageRating.toFixed(1)}` : 'No ratings yet'}
          </span>
        </div>
        <div className="detail-row">
          <span className="detail-label">Registered:</span>
          <span className="detail-value">
            {new Date(agent.createdAt).toLocaleDateString()}
          </span>
        </div>
      </div>

      {!agent.isApproved && (
        <div className="agent-actions">
          <button
            className="btn-approve"
            onClick={() => handleApprovalClick(agent, 'approve')}
          >
            ✓ Approve
          </button>
          <button
            className="btn-reject"
            onClick={() => handleApprovalClick(agent, 'reject')}
          >
            ✗ Reject
          </button>
        </div>
      )}

      {agent.approvalNotes && (
        <div className="approval-notes">
          <strong>Notes:</strong> {agent.approvalNotes}
        </div>
      )}
    </div>
  );

  return (
    <div className="delivery-agent-management">
      <div className="page-header">
        <h1>Delivery Agent Management</h1>
        <p>Manage and approve delivery agents</p>
      </div>

      <div className="tabs">
        <button
          className={`tab ${activeTab === 'pending' ? 'active' : ''}`}
          onClick={() => setActiveTab('pending')}
        >
          Pending Approval ({pendingAgents.length})
        </button>
        <button
          className={`tab ${activeTab === 'all' ? 'active' : ''}`}
          onClick={() => setActiveTab('all')}
        >
          All Agents ({agents.length})
        </button>
      </div>

      <div className="agents-container">
        {loading ? (
          <div className="loading">Loading agents...</div>
        ) : (
          <>
            {activeTab === 'pending' && (
              <>
                {pendingAgents.length === 0 ? (
                  <div className="empty-state">
                    <p>No pending agent approvals</p>
                  </div>
                ) : (
                  <div className="agents-grid">
                    {pendingAgents.map(renderAgentCard)}
                  </div>
                )}
              </>
            )}

            {activeTab === 'all' && (
              <>
                {agents.length === 0 ? (
                  <div className="empty-state">
                    <p>No delivery agents found</p>
                  </div>
                ) : (
                  <div className="agents-grid">
                    {agents.map(renderAgentCard)}
                  </div>
                )}
              </>
            )}
          </>
        )}
      </div>

      {showApprovalModal && (
        <div className="modal-overlay" onClick={() => setShowApprovalModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2>
              {approvalAction === 'approve' ? 'Approve' : 'Reject'} Delivery Agent
            </h2>
            
            <div className="modal-body">
              <p>
                <strong>Agent:</strong> {selectedAgent?.fullName}
              </p>
              <p>
                <strong>Email:</strong> {selectedAgent?.email}
              </p>
              <p>
                <strong>Vehicle:</strong> {selectedAgent?.vehicleType} - {selectedAgent?.vehicleNumber}
              </p>

              <div className="form-group">
                <label>Notes (Optional):</label>
                <textarea
                  value={approvalNotes}
                  onChange={(e) => setApprovalNotes(e.target.value)}
                  placeholder={`Enter reason for ${approvalAction}...`}
                  rows={4}
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
                disabled={processing}
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

export default DeliveryAgentManagementPage;
