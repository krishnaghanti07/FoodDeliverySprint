import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { useCart } from '../../context/CartContext';
import {
  Search, ShoppingCart, User, LogOut, ChefHat, LayoutDashboard,
  Menu, X, Package, Wallet, MapPin, Settings, ChevronDown,
  Truck, Shield, Bell, Home, UtensilsCrossed
} from 'lucide-react';
import { useState, useEffect, useRef, useCallback } from 'react';
import './Navbar.css';

// ── Cart Flyout Preview ──────────────────────────────────────────────
function CartFlyout({ cart, onClose, navigate }) {
  const items = cart?.items?.slice(0, 4) || [];
  const hasMore = (cart?.items?.length || 0) > 4;

  return (
    <div className="cart-flyout" role="dialog" aria-label="Cart preview">
      <div className="cart-flyout-header">
        <span className="cart-flyout-title">Your Cart</span>
        <span className="cart-flyout-count">{cart?.items?.length || 0} items</span>
      </div>

      {items.length === 0 ? (
        <div className="cart-flyout-empty">
          <ShoppingCart size={32} />
          <p>Your cart is empty</p>
        </div>
      ) : (
        <>
          <div className="cart-flyout-items">
            {items.map((item) => (
              <div key={item.id} className="cart-flyout-item">
                <div className="cfi-info">
                  <span className={`cfi-dot ${item.isVeg ? 'veg' : 'nonveg'}`} />
                  <span className="cfi-name">{item.name}</span>
                  <span className="cfi-qty">×{item.quantity}</span>
                </div>
                <span className="cfi-price">₹{item.lineTotal}</span>
              </div>
            ))}
            {hasMore && (
              <p className="cart-flyout-more">+{cart.items.length - 4} more items</p>
            )}
          </div>
          <div className="cart-flyout-footer">
            <div className="cart-flyout-total">
              <span>Total</span>
              <span className="cft-amount">₹{cart?.total?.toFixed(2) || '0.00'}</span>
            </div>
            <button
              className="btn btn-primary btn-sm cart-flyout-btn"
              onClick={() => { navigate('/cart'); onClose(); }}
            >
              View Cart
            </button>
          </div>
        </>
      )}
    </div>
  );
}

// ── User Dropdown ────────────────────────────────────────────────────
function UserDropdown({ user, role, onLogout, onClose, navigate }) {
  const profileImageUrl = user?.profileImageUrl;
  const userInitial = (user?.fullName || user?.name || 'U').charAt(0).toUpperCase();
  const userName = user?.fullName || user?.name || 'User';
  const userEmail = user?.email || '';

  const menuItems = [
    { icon: User, label: 'My Profile', path: '/profile' },
    ...(role === 'customer' ? [
      { icon: Package, label: 'My Orders', path: '/orders' },
      { icon: Wallet, label: 'Wallet', path: '/wallet' },
    ] : []),
    ...(role === 'partner' ? [
      { icon: ChefHat, label: 'Dashboard', path: '/partner' },
      { icon: UtensilsCrossed, label: 'Menu', path: '/partner/menu' },
    ] : []),
    ...(role === 'admin' ? [
      { icon: Shield, label: 'Admin Panel', path: '/admin' },
    ] : []),
    ...(role === 'deliveryagent' ? [
      { icon: Truck, label: 'My Deliveries', path: '/agent/deliveries' },
    ] : []),
  ];

  return (
    <div className="user-dropdown" role="menu" aria-label="User menu">
      {/* User info header */}
      <div className="ud-header">
        <div className="ud-avatar">
          {profileImageUrl ? (
            <img src={profileImageUrl} alt={userName} className="ud-avatar-img" />
          ) : (
            <div className="ud-avatar-initials">{userInitial}</div>
          )}
        </div>
        <div className="ud-info">
          <span className="ud-name">{userName}</span>
          <span className="ud-email">{userEmail}</span>
          <span className={`ud-role-badge role-${role}`}>{role}</span>
        </div>
      </div>

      <div className="ud-divider" />

      {/* Menu items */}
      <div className="ud-menu">
        {menuItems.map(({ icon: Icon, label, path }) => (
          <button
            key={path}
            className="ud-item"
            onClick={() => { navigate(path); onClose(); }}
            role="menuitem"
          >
            <Icon size={16} />
            <span>{label}</span>
          </button>
        ))}
      </div>

      <div className="ud-divider" />

      {/* Logout */}
      <button className="ud-item ud-logout" onClick={onLogout} role="menuitem">
        <LogOut size={16} />
        <span>Sign Out</span>
      </button>
    </div>
  );
}

// ── Main Navbar ──────────────────────────────────────────────────────
export default function Navbar() {
  const { user, isAuthenticated, logout } = useAuth();
  const { cart, cartItemCount, fetchCart } = useCart();
  const navigate = useNavigate();
  const location = useLocation();

  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [scrolled, setScrolled] = useState(false);
  const [userDropdownOpen, setUserDropdownOpen] = useState(false);
  const [cartFlyoutOpen, setCartFlyoutOpen] = useState(false);

  const userDropdownRef = useRef(null);
  const cartFlyoutRef = useRef(null);

  const role = user?.role?.toLowerCase();
  const isCustomer = role === 'customer';
  const isGuest = !isAuthenticated;
  const showSearch = isGuest || isCustomer;

  const profileImageUrl = user?.profileImageUrl;
  const userInitial = (user?.fullName || user?.name || 'U').charAt(0).toUpperCase();

  // ── Scroll-aware shrink ──────────────────────────────────────────
  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 12);
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  // ── Close mobile menu on route change ───────────────────────────
  useEffect(() => {
    setMobileOpen(false);
    setUserDropdownOpen(false);
    setCartFlyoutOpen(false);
  }, [location.pathname]);

  // ── Close dropdowns on outside click ────────────────────────────
  useEffect(() => {
    const handleClick = (e) => {
      if (userDropdownRef.current && !userDropdownRef.current.contains(e.target)) {
        setUserDropdownOpen(false);
      }
      if (cartFlyoutRef.current && !cartFlyoutRef.current.contains(e.target)) {
        setCartFlyoutOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  // ── Lock body scroll when mobile menu is open ────────────────────
  useEffect(() => {
    document.body.style.overflow = mobileOpen ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [mobileOpen]);

  // ── Fetch cart ───────────────────────────────────────────────────
  useEffect(() => {
    if (isAuthenticated && isCustomer) fetchCart();
  }, [isAuthenticated, isCustomer, fetchCart]);

  const handleLogout = useCallback(() => {
    logout();
    navigate('/');
    setMobileOpen(false);
    setUserDropdownOpen(false);
  }, [logout, navigate]);

  const handleSearch = (e) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      navigate(`/restaurants?search=${encodeURIComponent(searchQuery.trim())}`);
      setSearchQuery('');
      setMobileOpen(false);
    }
  };

  // ── Active link helper ───────────────────────────────────────────
  const isActive = (path) => {
    if (path === '/') return location.pathname === '/';
    return location.pathname.startsWith(path);
  };

  // ── Role-based nav links ─────────────────────────────────────────
  const getRoleNavLinks = () => {
    if (role === 'partner') return [
      { path: '/partner', label: 'Dashboard', icon: LayoutDashboard },
      { path: '/partner/orders', label: 'Orders', icon: Package },
      { path: '/partner/menu', label: 'Menu', icon: UtensilsCrossed },
    ];
    if (role === 'admin') return [
      { path: '/admin', label: 'Dashboard', icon: LayoutDashboard },
      { path: '/admin/orders', label: 'Orders', icon: Package },
      { path: '/admin/restaurants', label: 'Restaurants', icon: ChefHat },
    ];
    if (role === 'deliveryagent') return [
      { path: '/agent/dashboard', label: 'Dashboard', icon: LayoutDashboard },
      { path: '/agent/available', label: 'Available', icon: Bell },
      { path: '/agent/deliveries', label: 'My Deliveries', icon: Truck },
    ];
    return []; // customer links are inline
  };

  const roleLinks = getRoleNavLinks();

  return (
    <>
      <header className={`navbar ${scrolled ? 'navbar-scrolled' : ''}`}>
        <div className="navbar-container container">

          {/* ── Brand ── */}
          <Link to="/" className="navbar-brand" aria-label="FoodRush home">
            <span className="brand-icon" aria-hidden="true" />
            <span className="brand-text">FoodRush</span>
          </Link>

          {/* ── Search bar (desktop, customers & guests only) ── */}
          {showSearch && (
            <form className="navbar-search" onSubmit={handleSearch} role="search">
              <Search size={17} className="search-icon" aria-hidden="true" />
              <input
                type="search"
                placeholder="Search restaurants, cuisines..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="search-input"
                id="navbar-search-input"
                aria-label="Search restaurants"
                autoComplete="off"
              />
              {searchQuery && (
                <button
                  type="button"
                  className="search-clear"
                  onClick={() => setSearchQuery('')}
                  aria-label="Clear search"
                >
                  <X size={14} />
                </button>
              )}
            </form>
          )}

          {/* ── Desktop Nav ── */}
          <nav className="navbar-nav" aria-label="Main navigation">

            {/* Customer / Guest links */}
            {showSearch && (
              <Link
                to="/restaurants"
                className={`nav-link ${isActive('/restaurants') ? 'active' : ''}`}
              >
                Restaurants
              </Link>
            )}

            {/* Role-specific links (partner / admin / agent) */}
            {roleLinks.map(({ path, label, icon: Icon }) => (
              <Link
                key={path}
                to={path}
                className={`nav-link ${isActive(path) ? 'active' : ''}`}
              >
                <Icon size={16} aria-hidden="true" />
                {label}
              </Link>
            ))}

            {/* Help */}
            <Link
              to="/help"
              className={`nav-link ${isActive('/help') ? 'active' : ''}`}
            >
              Help
            </Link>

            {isAuthenticated ? (
              <div className="nav-actions">
                {/* Cart button with flyout (customers only) */}
                {isCustomer && (
                  <div className="cart-wrapper" ref={cartFlyoutRef}>
                    <button
                      className={`nav-cart-btn ${cartFlyoutOpen ? 'active' : ''}`}
                      onClick={() => { setCartFlyoutOpen(v => !v); setUserDropdownOpen(false); }}
                      aria-label={`Cart, ${cartItemCount} items`}
                      aria-expanded={cartFlyoutOpen}
                      aria-haspopup="dialog"
                    >
                      <ShoppingCart size={20} aria-hidden="true" />
                      {cartItemCount > 0 && (
                        <span className="cart-badge" aria-hidden="true">
                          {cartItemCount > 99 ? '99+' : cartItemCount}
                        </span>
                      )}
                    </button>
                    {cartFlyoutOpen && (
                      <CartFlyout
                        cart={cart}
                        onClose={() => setCartFlyoutOpen(false)}
                        navigate={navigate}
                      />
                    )}
                  </div>
                )}

                {/* User dropdown */}
                <div className="user-wrapper" ref={userDropdownRef}>
                  <button
                    className={`nav-user-btn ${userDropdownOpen ? 'active' : ''}`}
                    onClick={() => { setUserDropdownOpen(v => !v); setCartFlyoutOpen(false); }}
                    aria-label="User menu"
                    aria-expanded={userDropdownOpen}
                    aria-haspopup="menu"
                  >
                    {profileImageUrl ? (
                      <img
                        src={profileImageUrl}
                        alt={user?.fullName || 'Profile'}
                        className="nav-avatar-img"
                      />
                    ) : (
                      <div className="nav-avatar-initials" aria-hidden="true">
                        {userInitial}
                      </div>
                    )}
                    <span className="nav-user-name">
                      {(user?.fullName || user?.name || 'User').split(' ')[0]}
                    </span>
                    <ChevronDown
                      size={14}
                      className={`nav-chevron ${userDropdownOpen ? 'rotated' : ''}`}
                      aria-hidden="true"
                    />
                  </button>

                  {userDropdownOpen && (
                    <UserDropdown
                      user={user}
                      role={role}
                      onLogout={handleLogout}
                      onClose={() => setUserDropdownOpen(false)}
                      navigate={navigate}
                    />
                  )}
                </div>
              </div>
            ) : (
              <div className="nav-auth">
                <Link to="/login" className="btn btn-ghost btn-sm">Log In</Link>
                <Link to="/register" className="btn btn-primary btn-sm">Sign Up</Link>
              </div>
            )}
          </nav>

          {/* ── Mobile toggle ── */}
          <button
            className="mobile-toggle"
            onClick={() => setMobileOpen(v => !v)}
            aria-label={mobileOpen ? 'Close menu' : 'Open menu'}
            aria-expanded={mobileOpen}
            aria-controls="mobile-nav"
            id="mobile-menu-toggle"
          >
            <span className={`hamburger ${mobileOpen ? 'open' : ''}`} aria-hidden="true">
              <span /><span /><span />
            </span>
          </button>
        </div>
      </header>

      {/* ── Mobile Drawer ── */}
      <div
        className={`mobile-overlay ${mobileOpen ? 'open' : ''}`}
        onClick={() => setMobileOpen(false)}
        aria-hidden="true"
      />
      <nav
        id="mobile-nav"
        className={`mobile-drawer ${mobileOpen ? 'open' : ''}`}
        aria-label="Mobile navigation"
        aria-hidden={!mobileOpen}
      >
        {/* Drawer header */}
        <div className="mobile-drawer-header">
          <Link to="/" className="navbar-brand" onClick={() => setMobileOpen(false)}>
            <span className="brand-icon" aria-hidden="true" />
            <span className="brand-text">FoodRush</span>
          </Link>
          <button
            className="mobile-close"
            onClick={() => setMobileOpen(false)}
            aria-label="Close menu"
          >
            <X size={22} />
          </button>
        </div>

        {/* Mobile search */}
        {showSearch && (
          <form className="mobile-search" onSubmit={handleSearch} role="search">
            <Search size={16} aria-hidden="true" />
            <input
              type="search"
              placeholder="Search restaurants..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              aria-label="Search restaurants"
              autoComplete="off"
            />
          </form>
        )}

        {/* User info strip (authenticated) */}
        {isAuthenticated && (
          <div className="mobile-user-strip">
            <div className="mus-avatar">
              {profileImageUrl ? (
                <img src={profileImageUrl} alt={user?.fullName || 'Profile'} />
              ) : (
                <div className="mus-initials">{userInitial}</div>
              )}
            </div>
            <div className="mus-info">
              <span className="mus-name">{user?.fullName || user?.name}</span>
              <span className={`mus-role role-${role}`}>{role}</span>
            </div>
          </div>
        )}

        {/* Mobile nav links */}
        <div className="mobile-nav-links">
          {showSearch && (
            <Link
              to="/restaurants"
              className={`mobile-nav-link ${isActive('/restaurants') ? 'active' : ''}`}
              onClick={() => setMobileOpen(false)}
            >
              <UtensilsCrossed size={18} aria-hidden="true" />
              Restaurants
            </Link>
          )}

          {roleLinks.map(({ path, label, icon: Icon }) => (
            <Link
              key={path}
              to={path}
              className={`mobile-nav-link ${isActive(path) ? 'active' : ''}`}
              onClick={() => setMobileOpen(false)}
            >
              <Icon size={18} aria-hidden="true" />
              {label}
            </Link>
          ))}

          {isAuthenticated && isCustomer && (
            <>
              <Link
                to="/orders"
                className={`mobile-nav-link ${isActive('/orders') ? 'active' : ''}`}
                onClick={() => setMobileOpen(false)}
              >
                <Package size={18} aria-hidden="true" />
                My Orders
              </Link>
              <Link
                to="/wallet"
                className={`mobile-nav-link ${isActive('/wallet') ? 'active' : ''}`}
                onClick={() => setMobileOpen(false)}
              >
                <Wallet size={18} aria-hidden="true" />
                Wallet
              </Link>
              <Link
                to="/cart"
                className={`mobile-nav-link ${isActive('/cart') ? 'active' : ''}`}
                onClick={() => setMobileOpen(false)}
              >
                <ShoppingCart size={18} aria-hidden="true" />
                Cart
                {cartItemCount > 0 && (
                  <span className="mobile-cart-badge">{cartItemCount}</span>
                )}
              </Link>
            </>
          )}

          {isAuthenticated && (
            <Link
              to="/profile"
              className={`mobile-nav-link ${isActive('/profile') ? 'active' : ''}`}
              onClick={() => setMobileOpen(false)}
            >
              <User size={18} aria-hidden="true" />
              Profile
            </Link>
          )}

          <Link
            to="/help"
            className={`mobile-nav-link ${isActive('/help') ? 'active' : ''}`}
            onClick={() => setMobileOpen(false)}
          >
            <Settings size={18} aria-hidden="true" />
            Help & Support
          </Link>
        </div>

        {/* Mobile auth / logout */}
        <div className="mobile-drawer-footer">
          {isAuthenticated ? (
            <button className="mobile-logout-btn" onClick={handleLogout}>
              <LogOut size={18} aria-hidden="true" />
              Sign Out
            </button>
          ) : (
            <div className="mobile-auth-btns">
              <Link to="/login" className="btn btn-outline" onClick={() => setMobileOpen(false)}>
                Log In
              </Link>
              <Link to="/register" className="btn btn-primary" onClick={() => setMobileOpen(false)}>
                Sign Up
              </Link>
            </div>
          )}
        </div>
      </nav>
    </>
  );
}
