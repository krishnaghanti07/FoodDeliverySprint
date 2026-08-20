import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import api from '../services/api';
import { API_ENDPOINTS } from '../config/api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    const savedUser = localStorage.getItem('user');
    
    console.log('[AuthContext] Initializing...', { hasToken: !!token, hasUser: !!savedUser });
    
    if (token && savedUser) {
      try {
        const parsedUser = JSON.parse(savedUser);
        console.log('[AuthContext] User loaded from localStorage:', parsedUser);
        setUser(parsedUser);
      } catch (error) {
        console.error('[AuthContext] Failed to parse user from localStorage:', error);
        localStorage.removeItem('user');
      }
    }
    setLoading(false);
  }, []);

  const login = useCallback(async (email, password) => {
    console.log('[AuthContext] Login attempt for:', email);
    
    try {
      const res = await api.post(API_ENDPOINTS.auth.login, { email, password });
      console.log('[AuthContext] Login response:', res.data);
      
      // Backend now consistently returns camelCase
      const authData = res.data?.data || res.data;
      console.log('[AuthContext] Auth data extracted:', authData);
      
      // Validate required fields
      if (!authData.accessToken) {
        console.error('[AuthContext] No access token in response!', authData);
        throw new Error('No access token received from server');
      }
      
      console.log('[AuthContext] Saving tokens and user data...');
      localStorage.setItem('accessToken', authData.accessToken);
      
      if (authData.refreshToken) {
        localStorage.setItem('refreshToken', authData.refreshToken);
      }
      
      const userData = { 
        email, 
        role: authData.role || 'Customer', 
        fullName: authData.fullName || email.split('@')[0],
        profileImageUrl: authData.profileImageUrl || null
      };
      
      console.log('[AuthContext] User data to save:', userData);
      localStorage.setItem('user', JSON.stringify(userData));
      setUser(userData);
      
      console.log('[AuthContext] ✅ Login complete, user state updated');
      return { user: userData, ...authData };
    } catch (error) {
      console.error('[AuthContext] ❌ Login failed:', error);
      throw error;
    }
  }, []);

  const register = useCallback(async (payload) => {
    const res = await api.post(API_ENDPOINTS.auth.register, payload);
    return res.data?.data || res.data;
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    setUser(null);
  }, []);

  const fetchProfile = useCallback(async () => {
    try {
      const res = await api.get(API_ENDPOINTS.auth.profile);
      const profileData = res.data?.data || res.data;
      localStorage.setItem('user', JSON.stringify(profileData));
      setUser(profileData);
      return profileData;
    } catch (error) {
      // If profile fetch fails, don't let the interceptor redirect
      // Just throw the error so the calling component can handle it
      console.error('Profile fetch failed:', error);
      throw error;
    }
  }, []);

  const updateProfile = useCallback(async (payload) => {
    const res = await api.put(API_ENDPOINTS.auth.profile, payload);
    const profileData = res.data?.data || res.data;
    localStorage.setItem('user', JSON.stringify(profileData));
    setUser(profileData);
    return profileData;
  }, []);

  const value = {
    user,
    loading,
    login,
    register,
    logout,
    fetchProfile,
    updateProfile,
    isAuthenticated: !!user,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

export default AuthContext;
