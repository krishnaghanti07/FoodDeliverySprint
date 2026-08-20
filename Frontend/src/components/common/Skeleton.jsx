import './Skeleton.css';

// ═══════════════════════════════════════════════════════
// BASE SKELETON COMPONENTS
// ═══════════════════════════════════════════════════════

export function Skeleton({ width, height, circle, className = '', style = {} }) {
  return (
    <div
      className={`skeleton ${circle ? 'skeleton-circle' : ''} ${className}`}
      style={{
        width: width || '100%',
        height: height || '1rem',
        ...style,
      }}
    />
  );
}

export function SkeletonText({ lines = 1, lastLineWidth = '70%', className = '' }) {
  return (
    <div className={`skeleton-text ${className}`}>
      {Array.from({ length: lines }).map((_, i) => (
        <Skeleton
          key={i}
          height="0.875rem"
          width={i === lines - 1 ? lastLineWidth : '100%'}
          style={{ marginBottom: i < lines - 1 ? '0.5rem' : 0 }}
        />
      ))}
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// RESTAURANT CARD SKELETON
// ═══════════════════════════════════════════════════════

export function RestaurantCardSkeleton({ promoted = false }) {
  return (
    <div className={`card restaurant-card-skeleton ${promoted ? 'promoted' : ''}`}>
      <Skeleton height={promoted ? 200 : 180} style={{ borderRadius: 'var(--rounded-lg) var(--rounded-lg) 0 0' }} />
      <div className="card-body" style={{ padding: 'var(--space-md)' }}>
        <Skeleton height="1.25rem" width="70%" style={{ marginBottom: '0.5rem' }} />
        <Skeleton height="0.875rem" width="50%" style={{ marginBottom: '0.75rem' }} />
        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
          <Skeleton height="0.875rem" width="3rem" />
          <Skeleton height="0.875rem" width="5rem" />
        </div>
      </div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// MENU ITEM SKELETON
// ═══════════════════════════════════════════════════════

export function MenuItemSkeleton() {
  return (
    <div className="menu-item-skeleton">
      <div className="mi-skeleton-info">
        <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
          <Skeleton height="1.25rem" width="3rem" style={{ borderRadius: 'var(--rounded-full)' }} />
          <Skeleton height="1.25rem" width="5rem" style={{ borderRadius: 'var(--rounded-full)' }} />
        </div>
        <Skeleton height="1.25rem" width="60%" style={{ marginBottom: '0.5rem' }} />
        <Skeleton height="1rem" width="4rem" style={{ marginBottom: '0.75rem' }} />
        <SkeletonText lines={2} lastLineWidth="80%" />
      </div>
      <div className="mi-skeleton-action">
        <Skeleton height="5rem" width="5rem" circle />
      </div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// STAT CARD SKELETON (Dashboards)
// ═══════════════════════════════════════════════════════

export function StatCardSkeleton() {
  return (
    <div className="card stat-card-skeleton">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1rem' }}>
        <div style={{ flex: 1 }}>
          <Skeleton height="0.875rem" width="60%" style={{ marginBottom: '0.75rem' }} />
          <Skeleton height="2rem" width="50%" />
        </div>
        <Skeleton height="3rem" width="3rem" circle />
      </div>
      <Skeleton height="0.75rem" width="40%" />
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// ORDER CARD SKELETON
// ═══════════════════════════════════════════════════════

export function OrderCardSkeleton() {
  return (
    <div className="card order-card-skeleton">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1rem' }}>
        <div style={{ flex: 1 }}>
          <Skeleton height="1.125rem" width="70%" style={{ marginBottom: '0.5rem' }} />
          <Skeleton height="0.875rem" width="50%" style={{ marginBottom: '0.5rem' }} />
          <Skeleton height="0.875rem" width="40%" />
        </div>
        <Skeleton height="1.5rem" width="5rem" style={{ borderRadius: 'var(--rounded-full)' }} />
      </div>
      <div style={{ display: 'flex', gap: '1rem', paddingTop: '1rem', borderTop: '1px solid var(--outline-variant)' }}>
        <Skeleton height="0.875rem" width="6rem" />
        <Skeleton height="0.875rem" width="5rem" />
      </div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// TABLE ROW SKELETON
// ═══════════════════════════════════════════════════════

export function TableRowSkeleton({ columns = 5 }) {
  return (
    <tr className="table-row-skeleton">
      {Array.from({ length: columns }).map((_, i) => (
        <td key={i}>
          <Skeleton height="0.875rem" width={i === 0 ? '60%' : '80%'} />
        </td>
      ))}
    </tr>
  );
}

// ═══════════════════════════════════════════════════════
// CART ITEM SKELETON
// ═══════════════════════════════════════════════════════

export function CartItemSkeleton() {
  return (
    <div className="cart-item-skeleton">
      <Skeleton height="4rem" width="4rem" style={{ borderRadius: 'var(--rounded-md)' }} />
      <div style={{ flex: 1 }}>
        <Skeleton height="1.125rem" width="60%" style={{ marginBottom: '0.5rem' }} />
        <Skeleton height="0.875rem" width="40%" style={{ marginBottom: '0.5rem' }} />
        <Skeleton height="1.5rem" width="6rem" />
      </div>
      <div style={{ textAlign: 'right' }}>
        <Skeleton height="1.25rem" width="4rem" style={{ marginBottom: '0.5rem' }} />
        <Skeleton height="2rem" width="2rem" circle />
      </div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// HERO BANNER SKELETON
// ═══════════════════════════════════════════════════════

export function HeroBannerSkeleton() {
  return (
    <div className="hero-banner-skeleton">
      <div className="container" style={{ padding: 'var(--space-2xl) var(--gutter)' }}>
        <Skeleton height="3rem" width="70%" style={{ marginBottom: '1rem' }} />
        <Skeleton height="1.5rem" width="50%" style={{ marginBottom: '2rem' }} />
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          <Skeleton height="1rem" width="4rem" style={{ borderRadius: 'var(--rounded-full)' }} />
          <Skeleton height="1rem" width="5rem" style={{ borderRadius: 'var(--rounded-full)' }} />
          <Skeleton height="1rem" width="4.5rem" style={{ borderRadius: 'var(--rounded-full)' }} />
        </div>
      </div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// ADDRESS CARD SKELETON
// ═══════════════════════════════════════════════════════

export function AddressCardSkeleton() {
  return (
    <div className="card address-card-skeleton">
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
        <Skeleton height="1rem" width="4rem" style={{ borderRadius: 'var(--rounded-full)' }} />
        <Skeleton height="1.5rem" width="1.5rem" circle />
      </div>
      <Skeleton height="1.125rem" width="60%" style={{ marginBottom: '0.5rem' }} />
      <SkeletonText lines={2} lastLineWidth="90%" />
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// TRANSACTION ITEM SKELETON
// ═══════════════════════════════════════════════════════

export function TransactionItemSkeleton() {
  return (
    <div className="transaction-item-skeleton">
      <Skeleton height="2.5rem" width="2.5rem" circle />
      <div style={{ flex: 1 }}>
        <Skeleton height="1rem" width="50%" style={{ marginBottom: '0.5rem' }} />
        <Skeleton height="0.875rem" width="40%" />
      </div>
      <div style={{ textAlign: 'right' }}>
        <Skeleton height="1.125rem" width="4rem" style={{ marginBottom: '0.25rem' }} />
        <Skeleton height="0.75rem" width="3rem" />
      </div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// PROFILE SECTION SKELETON
// ═══════════════════════════════════════════════════════

export function ProfileSectionSkeleton() {
  return (
    <div className="card profile-section-skeleton">
      <div style={{ display: 'flex', alignItems: 'center', gap: '1.5rem', marginBottom: '1.5rem' }}>
        <Skeleton height="5rem" width="5rem" circle />
        <div style={{ flex: 1 }}>
          <Skeleton height="1.5rem" width="50%" style={{ marginBottom: '0.5rem' }} />
          <Skeleton height="1rem" width="40%" />
        </div>
      </div>
      <div style={{ display: 'grid', gap: '1rem' }}>
        <div>
          <Skeleton height="0.875rem" width="30%" style={{ marginBottom: '0.5rem' }} />
          <Skeleton height="2.5rem" width="100%" />
        </div>
        <div>
          <Skeleton height="0.875rem" width="25%" style={{ marginBottom: '0.5rem' }} />
          <Skeleton height="2.5rem" width="100%" />
        </div>
      </div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════
// CUISINE CARD SKELETON
// ═══════════════════════════════════════════════════════

export function CuisineCardSkeleton() {
  return (
    <div className="cuisine-card-skeleton">
      <Skeleton height="4rem" width="4rem" circle style={{ marginBottom: '0.75rem' }} />
      <Skeleton height="0.875rem" width="80%" />
    </div>
  );
}
