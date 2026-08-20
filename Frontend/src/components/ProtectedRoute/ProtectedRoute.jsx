import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { useEffect } from 'react';

export default function ProtectedRoute({ children, roles }) {
  const { user, isAuthenticated, loading } = useAuth();
  const location = useLocation();

  useEffect(() => {
    console.log('[ProtectedRoute]', {
      path: location.pathname,
      loading,
      isAuthenticated,
      user,
      requiredRoles: roles
    });
  }, [location.pathname, loading, isAuthenticated, user, roles]);

  if (loading) {
    console.log('[ProtectedRoute] Still loading auth state...');
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <div className="spinner" />
      </div>
    );
  }

  if (!isAuthenticated) {
    console.warn('[ProtectedRoute] Not authenticated, redirecting to login');
    // Save the attempted location so we can redirect back after login
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (roles && roles.length > 0) {
    const userRole = user?.role?.toLowerCase();
    const allowed = roles.map(r => r.toLowerCase());
    if (!allowed.includes(userRole)) {
      console.warn('[ProtectedRoute] User role not allowed', { userRole, allowed });
      return <Navigate to="/" replace />;
    }
  }

  console.log('[ProtectedRoute] Access granted');
  return children;
}
