import { BrowserRouter, Routes, Route, useLocation, useNavigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import toast from 'react-hot-toast';
import { useEffect } from 'react';
import { AuthProvider, useAuth } from './context/AuthContext';
import { CartProvider } from './context/CartContext';
import Navbar from './components/Navbar/Navbar';
import Footer from './components/Footer/Footer';
import ProtectedRoute from './components/ProtectedRoute/ProtectedRoute';

// Pages
import HomePage from './pages/HomePage';
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage';
import ResetPasswordPage from './pages/auth/ResetPasswordPage';
import RestaurantsPage from './pages/RestaurantsPage';
import RestaurantDetailPage from './pages/RestaurantDetailPage';
import ProfilePage from './pages/profile/ProfilePage';
import HelpCenterPage from './pages/HelpCenterPage';
import ContactUsPage from './pages/ContactUsPage';

// Customer Pages
import CartPage from './pages/customer/CartPage';
import CheckoutPage from './pages/customer/CheckoutPage';
import MyOrdersPage from './pages/customer/MyOrdersPage';
import OrderDetailPage from './pages/customer/OrderDetailPage';
import WalletPage from './pages/customer/WalletPage';

// Partner Pages
import PartnerDashboard from './pages/partner/PartnerDashboard';
import RestaurantForm from './pages/partner/RestaurantForm';
import RestaurantManagement from './pages/partner/RestaurantManagement';
import MenuManagement from './pages/partner/MenuManagement';
import OrdersManagement from './pages/partner/OrdersManagement';
import CouponsManagement from './pages/partner/CouponsManagement';
import OperatingHoursManagement from './pages/partner/OperatingHoursManagement';

// Admin Pages
import AdminDashboard from './pages/admin/AdminDashboard';
import UsersManagement from './pages/admin/UsersManagement';
import RestaurantsManagement from './pages/admin/RestaurantsManagement';
import RestaurantApprovalPage from './pages/admin/RestaurantApprovalPage';
import AdminOrdersManagement from './pages/admin/OrdersManagement';
import AdminOrderDetail from './pages/admin/AdminOrderDetail';
import DeliveryAgentsManagement from './pages/admin/DeliveryAgentsManagement';
import ReportsPage from './pages/admin/ReportsPage';
import RefundManagementPage from './pages/admin/RefundManagementPage';

// Delivery Agent Pages
import DeliveryAgentDashboard from './pages/agent/DeliveryAgentDashboard';
import MyDeliveriesPage from './pages/agent/MyDeliveriesPage';
import DeliveryDetailPage from './pages/agent/DeliveryDetailPage';
import AvailableOrdersPage from './pages/agent/AvailableOrdersPage';

import './index.css';

// PublicOrCustomerRoute - Allows access for non-authenticated users and Customers only
// Redirects Partners and Admins to their respective dashboards
function PublicOrCustomerRoute({ children }) {
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();
  
  useEffect(() => {
    if (isAuthenticated && user) {
      const role = user.role?.toLowerCase();
      
      // Redirect Partners to their dashboard
      if (role === 'partner') {
        navigate('/partner', { replace: true });
        toast.error('Partners cannot access restaurant listings');
      }
      // Redirect Admins to their dashboard
      else if (role === 'admin') {
        navigate('/admin', { replace: true });
        toast.error('Admins cannot access restaurant listings');
      }
      // Redirect DeliveryAgents to their dashboard
      else if (role === 'deliveryagent') {
        navigate('/agent/dashboard', { replace: true });
        toast.error('Delivery agents cannot access restaurant listings');
      }
    }
  }, [isAuthenticated, user, navigate]);
  
  // Allow access for non-authenticated users and Customers
  if (!isAuthenticated || user?.role?.toLowerCase() === 'customer') {
    return children;
  }
  
  // Show loading while redirecting
  return null;
}

function AppLayout() {
  const location = useLocation();
  const isAuthPage = ['/login', '/register'].includes(location.pathname);

  return (
    <>
      {!isAuthPage && <Navbar />}
      <main style={{ flex: 1 }}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/help" element={<HelpCenterPage />} />
          <Route path="/contact" element={<ContactUsPage />} />
          {/* Restaurant pages - Only accessible by Customers and non-authenticated users */}
          <Route path="/restaurants" element={<PublicOrCustomerRoute><RestaurantsPage /></PublicOrCustomerRoute>} />
          <Route path="/restaurants/:id" element={<PublicOrCustomerRoute><RestaurantDetailPage /></PublicOrCustomerRoute>} />

          {/* Protected Customer Routes */}
          <Route path="/cart" element={<ProtectedRoute roles={['Customer']}><CartPage /></ProtectedRoute>} />
          <Route path="/checkout" element={<ProtectedRoute roles={['Customer']}><CheckoutPage /></ProtectedRoute>} />
          <Route path="/orders" element={<ProtectedRoute roles={['Customer']}><MyOrdersPage /></ProtectedRoute>} />
          <Route path="/orders/:id" element={<ProtectedRoute roles={['Customer', 'Partner']}><OrderDetailPage /></ProtectedRoute>} />
          <Route path="/wallet" element={<ProtectedRoute roles={['Customer']}><WalletPage /></ProtectedRoute>} />
          <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />

          {/* Protected Partner Routes */}
          <Route path="/partner" element={<ProtectedRoute roles={['Partner']}><PartnerDashboard /></ProtectedRoute>} />
          <Route path="/partner/restaurant" element={<ProtectedRoute roles={['Partner']}><RestaurantManagement /></ProtectedRoute>} />
          <Route path="/partner/restaurant/new" element={<ProtectedRoute roles={['Partner']}><RestaurantForm /></ProtectedRoute>} />
          <Route path="/partner/restaurant/:id/edit" element={<ProtectedRoute roles={['Partner']}><RestaurantForm /></ProtectedRoute>} />
          <Route path="/partner/menu" element={<ProtectedRoute roles={['Partner']}><MenuManagement /></ProtectedRoute>} />
          <Route path="/partner/orders" element={<ProtectedRoute roles={['Partner']}><OrdersManagement /></ProtectedRoute>} />
          <Route path="/partner/orders/:id" element={<ProtectedRoute roles={['Partner']}><OrderDetailPage /></ProtectedRoute>} />
          <Route path="/partner/coupons" element={<ProtectedRoute roles={['Partner']}><CouponsManagement /></ProtectedRoute>} />
          <Route path="/partner/hours" element={<ProtectedRoute roles={['Partner']}><OperatingHoursManagement /></ProtectedRoute>} />

          {/* Protected Admin Routes */}
          <Route path="/admin" element={<ProtectedRoute roles={['Admin']}><AdminDashboard /></ProtectedRoute>} />
          <Route path="/admin/users" element={<ProtectedRoute roles={['Admin']}><UsersManagement /></ProtectedRoute>} />
          <Route path="/admin/refunds" element={<ProtectedRoute roles={['Admin']}><RefundManagementPage /></ProtectedRoute>} />
          <Route path="/admin/restaurants" element={<ProtectedRoute roles={['Admin']}><RestaurantsManagement /></ProtectedRoute>} />
          <Route path="/admin/restaurants/approvals" element={<ProtectedRoute roles={['Admin']}><RestaurantApprovalPage /></ProtectedRoute>} />
          <Route path="/admin/orders" element={<ProtectedRoute roles={['Admin']}><AdminOrdersManagement /></ProtectedRoute>} />
          <Route path="/admin/orders/:id" element={<ProtectedRoute roles={['Admin']}><AdminOrderDetail /></ProtectedRoute>} />
          <Route path="/admin/delivery-agents" element={<ProtectedRoute roles={['Admin']}><DeliveryAgentsManagement /></ProtectedRoute>} />
          <Route path="/admin/reports" element={<ProtectedRoute roles={['Admin']}><ReportsPage /></ProtectedRoute>} />

          {/* Protected Delivery Agent Routes */}
          <Route path="/agent/dashboard" element={<ProtectedRoute roles={['DeliveryAgent']}><DeliveryAgentDashboard /></ProtectedRoute>} />
          <Route path="/agent/deliveries" element={<ProtectedRoute roles={['DeliveryAgent']}><MyDeliveriesPage /></ProtectedRoute>} />
          <Route path="/agent/deliveries/:id" element={<ProtectedRoute roles={['DeliveryAgent']}><DeliveryDetailPage /></ProtectedRoute>} />
          <Route path="/agent/available" element={<ProtectedRoute roles={['DeliveryAgent']}><AvailableOrdersPage /></ProtectedRoute>} />

          {/* Fallback */}
          <Route path="*" element={
            <div style={{ textAlign: 'center', padding: '4rem 1rem' }}>
              <h1 className="display-lg" style={{ marginBottom: '0.5rem' }}>404</h1>
              <p className="body-lg text-muted">Page not found</p>
            </div>
          } />
        </Routes>
      </main>
      {!isAuthPage && <Footer />}
    </>
  );
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <CartProvider>
          <AppLayout />
          <Toaster
            position="top-right"
            toastOptions={{
              duration: 3000,
              style: {
                background: 'var(--surface-container-lowest)',
                color: 'var(--on-surface)',
                borderRadius: 'var(--rounded-lg)',
                boxShadow: 'var(--shadow-lg)',
                fontFamily: 'var(--font-body)',
                border: '1px solid var(--outline-variant)',
              },
              success: { iconTheme: { primary: '#2e7d32', secondary: '#fff' } },
              error: { iconTheme: { primary: 'var(--error)', secondary: '#fff' } },
            }}
          />
        </CartProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
