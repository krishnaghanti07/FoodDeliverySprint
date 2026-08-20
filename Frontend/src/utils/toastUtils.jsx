/**
 * toastUtils.jsx
 * Branded toast helpers built on top of react-hot-toast.
 * Each toast has a colored left-border accent, icon, and optional action button.
 */
import toast from 'react-hot-toast';
import { CheckCircle, XCircle, AlertTriangle, Info, ShoppingCart, Package, RotateCcw } from 'lucide-react';

// ── Base branded toast renderer ──────────────────────────────────────
function BrandedToast({ t, icon: Icon, iconColor, borderColor, title, message, action }) {
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: '0.75rem',
        padding: '0.875rem 1rem',
        background: 'var(--surface-container-lowest)',
        borderRadius: 'var(--rounded-xl)',
        boxShadow: 'var(--shadow-xl)',
        border: '1px solid var(--outline-variant)',
        borderLeft: `4px solid ${borderColor}`,
        fontFamily: 'var(--font-body)',
        maxWidth: '360px',
        width: '100%',
        opacity: t.visible ? 1 : 0,
        transform: t.visible ? 'translateX(0)' : 'translateX(100%)',
        transition: 'all 0.3s cubic-bezier(0.16, 1, 0.3, 1)',
      }}
    >
      {/* Icon */}
      <div style={{
        width: 32, height: 32, borderRadius: '50%',
        background: `${iconColor}18`,
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        flexShrink: 0, color: iconColor,
      }}>
        <Icon size={16} />
      </div>

      {/* Text */}
      <div style={{ flex: 1, minWidth: 0 }}>
        {title && (
          <p style={{
            margin: 0, marginBottom: message ? '2px' : 0,
            fontSize: '0.875rem', fontWeight: 700,
            color: 'var(--on-surface)', lineHeight: 1.3,
          }}>
            {title}
          </p>
        )}
        {message && (
          <p style={{
            margin: 0, fontSize: '0.8125rem',
            color: 'var(--on-surface-variant)', lineHeight: 1.45,
          }}>
            {message}
          </p>
        )}
        {/* Action button */}
        {action && (
          <button
            onClick={() => { action.onClick(); toast.dismiss(t.id); }}
            style={{
              marginTop: '0.5rem',
              display: 'inline-flex', alignItems: 'center', gap: 4,
              padding: '3px 10px',
              fontSize: '0.75rem', fontWeight: 700,
              color: iconColor,
              background: `${iconColor}14`,
              border: `1.5px solid ${iconColor}30`,
              borderRadius: 'var(--rounded-full)',
              cursor: 'pointer',
              transition: 'all 0.15s ease',
            }}
            onMouseEnter={e => { e.currentTarget.style.background = `${iconColor}25`; }}
            onMouseLeave={e => { e.currentTarget.style.background = `${iconColor}14`; }}
          >
            {action.icon && <action.icon size={11} />}
            {action.label}
          </button>
        )}
      </div>

      {/* Dismiss */}
      <button
        onClick={() => toast.dismiss(t.id)}
        style={{
          flexShrink: 0, padding: '2px',
          color: 'var(--outline)', cursor: 'pointer',
          borderRadius: 'var(--rounded)',
          transition: 'color 0.15s',
          lineHeight: 1,
        }}
        onMouseEnter={e => { e.currentTarget.style.color = 'var(--on-surface)'; }}
        onMouseLeave={e => { e.currentTarget.style.color = 'var(--outline)'; }}
        aria-label="Dismiss"
      >
        ✕
      </button>
    </div>
  );
}

// ── Public API ───────────────────────────────────────────────────────

export const showToast = {
  success(title, message, opts = {}) {
    return toast.custom(t => (
      <BrandedToast
        t={t}
        icon={CheckCircle}
        iconColor="#2e7d32"
        borderColor="#2e7d32"
        title={title}
        message={message}
        action={opts.action}
      />
    ), { duration: opts.duration || 3500, id: opts.id });
  },

  error(title, message, opts = {}) {
    return toast.custom(t => (
      <BrandedToast
        t={t}
        icon={XCircle}
        iconColor="var(--error)"
        borderColor="var(--error)"
        title={title}
        message={message}
        action={opts.action}
      />
    ), { duration: opts.duration || 4500, id: opts.id });
  },

  warning(title, message, opts = {}) {
    return toast.custom(t => (
      <BrandedToast
        t={t}
        icon={AlertTriangle}
        iconColor="#e65100"
        borderColor="#e65100"
        title={title}
        message={message}
        action={opts.action}
      />
    ), { duration: opts.duration || 4000, id: opts.id });
  },

  info(title, message, opts = {}) {
    return toast.custom(t => (
      <BrandedToast
        t={t}
        icon={Info}
        iconColor="var(--secondary)"
        borderColor="var(--secondary)"
        title={title}
        message={message}
        action={opts.action}
      />
    ), { duration: opts.duration || 3500, id: opts.id });
  },

  // ── Cart-specific toasts ─────────────────────────────────────────
  cartAdded(itemName) {
    return toast.custom(t => (
      <BrandedToast
        t={t}
        icon={ShoppingCart}
        iconColor="var(--primary)"
        borderColor="var(--primary)"
        title="Added to cart"
        message={itemName}
      />
    ), { duration: 2500 });
  },

  cartRemoved(itemName, onUndo) {
    return toast.custom(t => (
      <BrandedToast
        t={t}
        icon={ShoppingCart}
        iconColor="var(--on-surface-variant)"
        borderColor="var(--outline-variant)"
        title="Item removed"
        message={itemName}
        action={onUndo ? {
          label: 'Undo',
          icon: RotateCcw,
          onClick: onUndo,
        } : null}
      />
    ), { duration: 4000 });
  },

  // ── Order status toasts ──────────────────────────────────────────
  orderStatus(statusLabel, restaurantName, orderId, onView) {
    const statusColors = {
      'Accepted':        { color: 'var(--tertiary)',   border: 'var(--tertiary)' },
      'Preparing':       { color: 'var(--primary)',    border: 'var(--primary)' },
      'ReadyForPickup':  { color: '#f59e0b',           border: '#f59e0b' },
      'OutForDelivery':  { color: 'var(--secondary)',  border: 'var(--secondary)' },
      'Delivered':       { color: '#2e7d32',           border: '#2e7d32' },
      'Cancelled':       { color: 'var(--error)',      border: 'var(--error)' },
    };
    const sc = statusColors[statusLabel] || { color: 'var(--primary)', border: 'var(--primary)' };

    const statusMessages = {
      'Accepted':        'Your order has been accepted!',
      'Preparing':       'The restaurant is preparing your food',
      'ReadyForPickup':  'Your order is ready for pickup',
      'OutForDelivery':  'Your order is on the way!',
      'Delivered':       'Your order has been delivered. Enjoy!',
      'Cancelled':       'Your order has been cancelled',
    };

    return toast.custom(t => (
      <BrandedToast
        t={t}
        icon={Package}
        iconColor={sc.color}
        borderColor={sc.border}
        title={restaurantName}
        message={statusMessages[statusLabel] || `Order status: ${statusLabel}`}
        action={onView ? {
          label: 'View Order',
          icon: Package,
          onClick: onView,
        } : null}
      />
    ), { duration: 5000, id: `order-${orderId}-${statusLabel}` });
  },
};

// ── Drop-in replacements for plain toast calls ───────────────────────
// These keep backward compatibility with existing toast.success/error calls
// while using the branded style.
export function patchToast() {
  // We don't monkey-patch react-hot-toast — instead components import showToast directly.
  // This function is a no-op kept for documentation purposes.
}
