import { useState, useEffect } from 'react';
import {
  Wallet, ArrowDownCircle, ArrowUpCircle, Clock,
  RefreshCw, ShoppingBag, Gift, Shield, AlertCircle,
} from 'lucide-react';
import api from '../../services/api';
import { toast } from 'react-hot-toast';
import { TransactionItemSkeleton, Skeleton } from '../../components/common/Skeleton';
import './WalletPage.css';

// ── Source icon + label map ──────────────────────────────────────────
const SOURCE_META = {
  Refund:       { label: 'Refund',        icon: Gift,         color: 'var(--tertiary)' },
  OrderPayment: { label: 'Order Payment', icon: ShoppingBag,  color: 'var(--primary)' },
  AdminCredit:  { label: 'Admin Credit',  icon: Shield,       color: 'var(--secondary)' },
  AdminDebit:   { label: 'Admin Debit',   icon: AlertCircle,  color: 'var(--error)' },
};

function getSourceMeta(source) {
  return SOURCE_META[source] || { label: source || 'Transaction', icon: Wallet, color: 'var(--outline)' };
}

function formatDate(dateStr) {
  return new Date(dateStr).toLocaleDateString('en-IN', {
    day: '2-digit', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

// ── Transaction Row ──────────────────────────────────────────────────
function TransactionRow({ tx, isLast }) {
  const isCredit = tx.type === 'Credit';
  const meta = getSourceMeta(tx.source);
  const SourceIcon = meta.icon;

  return (
    <div className={`wlt-tx-row ${isLast ? 'last' : ''}`}>
      {/* Timeline dot + line */}
      <div className="wlt-tx-timeline">
        <div className={`wlt-tx-dot ${isCredit ? 'credit' : 'debit'}`}>
          {isCredit
            ? <ArrowDownCircle size={14} />
            : <ArrowUpCircle size={14} />}
        </div>
        {!isLast && <div className="wlt-tx-line" />}
      </div>

      {/* Icon */}
      <div className="wlt-tx-icon" style={{ background: `${meta.color}18`, color: meta.color }}>
        <SourceIcon size={16} />
      </div>

      {/* Info */}
      <div className="wlt-tx-info">
        <span className="wlt-tx-desc">{tx.description || meta.label}</span>
        <span className="wlt-tx-meta">
          <span className="wlt-tx-source">{meta.label}</span>
          <span className="wlt-tx-sep">·</span>
          <span className="wlt-tx-date">{formatDate(tx.createdAt)}</span>
        </span>
      </div>

      {/* Amount */}
      <span className={`wlt-tx-amount ${isCredit ? 'credit' : 'debit'}`}>
        {isCredit ? '+' : '−'}₹{Number(tx.amount).toFixed(2)}
      </span>
    </div>
  );
}

// ── Main Component ───────────────────────────────────────────────────
const WalletPage = () => {
  const [balance, setBalance] = useState(0);
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [syncing, setSyncing] = useState(false);

  useEffect(() => { fetchWalletData(); }, []);

  const fetchWalletData = async (silent = false) => {
    if (!silent) setLoading(true);
    else setRefreshing(true);
    try {
      const [balanceRes, txRes] = await Promise.all([
        api.get('/gateway/auth/wallet/balance'),
        api.get('/gateway/auth/wallet/transactions'),
      ]);
      const currentBalance = balanceRes.data?.data ?? 0;
      const txList = txRes.data?.data ?? [];
      setBalance(currentBalance);
      setTransactions(Array.isArray(txList) ? txList : []);
      if (currentBalance > 0 && txList.length === 0) {
        await syncWalletBalance(currentBalance);
      }
    } catch (error) {
      console.error('Error fetching wallet data:', error);
      if (!silent) toast.error('Failed to load wallet data');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const syncWalletBalance = async () => {
    try {
      setSyncing(true);
      const response = await api.post('/gateway/auth/wallet/sync');
      if (response.data?.data?.synced) {
        const txRes = await api.get('/gateway/auth/wallet/transactions');
        setTransactions(Array.isArray(txRes.data?.data) ? txRes.data.data : []);
      }
    } catch (error) {
      console.error('Wallet sync error:', error);
    } finally {
      setSyncing(false);
    }
  };

  // ── Stats derived from transactions ─────────────────────────────
  const totalCredits = transactions.filter(t => t.type === 'Credit').reduce((s, t) => s + Number(t.amount), 0);
  const totalDebits  = transactions.filter(t => t.type === 'Debit').reduce((s, t) => s + Number(t.amount), 0);

  // ── Loading skeleton ─────────────────────────────────────────────
  if (loading) {
    return (
      <div className="wlt-page page-enter">
        <div className="wlt-container">
          {/* Balance card skeleton */}
          <div className="wlt-balance-skeleton">
            <div>
              <Skeleton height="0.875rem" width="8rem" style={{ marginBottom: '0.75rem', opacity: 0.4 }} />
              <Skeleton height="3rem" width="10rem" style={{ opacity: 0.4 }} />
            </div>
            <Skeleton height="4rem" width="4rem" circle style={{ opacity: 0.3 }} />
          </div>
          {/* History skeleton */}
          <div className="card" style={{ padding: 'var(--space-xl)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 'var(--space-xl)' }}>
              <Skeleton height="1.25rem" width="12rem" />
              <Skeleton height="1.25rem" width="5rem" />
            </div>
            {Array.from({ length: 5 }).map((_, i) => <TransactionItemSkeleton key={i} />)}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="wlt-page page-enter">
      <div className="wlt-container">

        {/* ── Balance Card ── */}
        <div className="wlt-balance-card">
          {/* Background decoration */}
          <div className="wlt-balance-deco" aria-hidden="true">
            <div className="wlt-deco-circle wlt-deco-1" />
            <div className="wlt-deco-circle wlt-deco-2" />
          </div>

          <div className="wlt-balance-content">
            <div className="wlt-balance-left">
              <span className="wlt-balance-label">Wallet Balance</span>
              <span className="wlt-balance-amount">₹{balance.toFixed(2)}</span>
              <span className="wlt-balance-sub">
                {transactions.length} transaction{transactions.length !== 1 ? 's' : ''}
              </span>
            </div>
            <div className="wlt-balance-icon" aria-hidden="true">
              <Wallet size={36} />
            </div>
          </div>

          {/* Mini stats row */}
          {transactions.length > 0 && (
            <div className="wlt-balance-stats">
              <div className="wlt-bs-item">
                <ArrowDownCircle size={14} />
                <span>+₹{totalCredits.toFixed(2)}</span>
                <span className="wlt-bs-label">Total Credits</span>
              </div>
              <div className="wlt-bs-divider" />
              <div className="wlt-bs-item">
                <ArrowUpCircle size={14} />
                <span>−₹{totalDebits.toFixed(2)}</span>
                <span className="wlt-bs-label">Total Debits</span>
              </div>
            </div>
          )}
        </div>

        {/* ── Transaction History ── */}
        <div className="wlt-history-card">
          <div className="wlt-history-header">
            <div className="wlt-history-title-wrap">
              <Clock size={18} />
              <h2 className="wlt-history-title">Transaction History</h2>
              {transactions.length > 0 && (
                <span className="wlt-history-count">{transactions.length}</span>
              )}
            </div>
            <button
              className={`wlt-refresh-btn ${refreshing || syncing ? 'spinning' : ''}`}
              onClick={() => fetchWalletData(true)}
              disabled={refreshing || syncing}
              aria-label="Refresh transactions"
            >
              <RefreshCw size={15} />
              <span>{syncing ? 'Syncing…' : 'Refresh'}</span>
            </button>
          </div>

          {syncing && (
            <div className="wlt-sync-banner">
              <RefreshCw size={14} className="spinning-icon" />
              Syncing your wallet history…
            </div>
          )}

          {transactions.length === 0 ? (
            <div className="wlt-empty">
              <div className="wlt-empty-icon">
                <Wallet size={36} />
              </div>
              <h3 className="wlt-empty-title">No transactions yet</h3>
              <p className="wlt-empty-sub">
                Your transaction history will appear here once you make a payment or receive a refund.
              </p>
            </div>
          ) : (
            <div className="wlt-tx-list">
              {transactions.map((tx, i) => (
                <TransactionRow
                  key={tx.id}
                  tx={tx}
                  isLast={i === transactions.length - 1}
                />
              ))}
            </div>
          )}
        </div>

      </div>
    </div>
  );
};

export default WalletPage;
