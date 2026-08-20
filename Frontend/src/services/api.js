import axios from 'axios';

const api = axios.create({
  headers: {
    'Content-Type': 'application/json',
  },
});

// Track if a refresh is in progress to prevent concurrent refreshes
let isRefreshing = false;
let refreshSubscribers = [];

function subscribeTokenRefresh(cb) {
  refreshSubscribers.push(cb);
}

function onRefreshed(token) {
  refreshSubscribers.forEach(cb => cb(token));
  refreshSubscribers = [];
}

// Request interceptor - attach token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
      console.log('[API] Request to:', config.url, '- Token attached');
    } else {
      console.log('[API] Request to:', config.url, '- No token');
    }
    return config;
  },
  (error) => {
    console.error('[API] Request error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor - handle 401 & token refresh
api.interceptors.response.use(
  (response) => {
    console.log('[API] ✅ Response from:', response.config.url, '- Status:', response.status);
    return response;
  },
  async (error) => {
    const originalRequest = error.config;
    
    console.error('[API] ❌ Response error:', {
      url: originalRequest?.url,
      status: error.response?.status,
      message: error.message
    });

    // Don't retry if this is already a retry or if it's a refresh token request
    if (error.response?.status === 401 && !originalRequest._retry && !originalRequest.url?.includes('/refresh')) {
      originalRequest._retry = true;

      // If already refreshing, queue this request
      if (isRefreshing) {
        console.log('[API] ⏳ Refresh in progress, queuing request...');
        return new Promise((resolve) => {
          subscribeTokenRefresh((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            resolve(api(originalRequest));
          });
        });
      }

      const refreshToken = localStorage.getItem('refreshToken');
      console.log('[API] 🔄 401 Unauthorized - Attempting token refresh...');

      if (refreshToken) {
        isRefreshing = true;

        try {
          console.log('[API] Calling refresh token endpoint...');
          const res = await axios.post(
            '/gateway/auth/refresh',
            { refreshToken },
            {
              headers: { 'Content-Type': 'application/json' }
            }
          );
          
          // Backend now consistently returns camelCase
          const authData = res.data?.data || res.data;
          const newAccessToken = authData.accessToken;
          
          if (newAccessToken) {
            console.log('[API] ✅ Token refresh successful');
            localStorage.setItem('accessToken', newAccessToken);
            
            if (authData.refreshToken) {
              localStorage.setItem('refreshToken', authData.refreshToken);
            }
            
            // Update the original request with new token
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
            
            // Notify all queued requests
            onRefreshed(newAccessToken);
            isRefreshing = false;
            
            console.log('[API] 🔄 Retrying original request with new token...');
            // Retry the original request
            return api(originalRequest);
          } else {
            throw new Error('No access token in refresh response');
          }
        } catch (refreshError) {
          console.error('[API] ❌ Token refresh failed:', refreshError);
          console.log('[API] 🚪 Clearing auth data and redirecting to login...');
          
          isRefreshing = false;
          refreshSubscribers = [];
          
          // Only clear auth and redirect if refresh actually failed
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
          localStorage.removeItem('user');
          
          // Avoid redirect loop - only redirect if not already on login page
          if (!window.location.pathname.includes('/login')) {
            window.location.href = '/login';
          }
          
          return Promise.reject(refreshError);
        }
      } else {
        // No refresh token available
        console.warn('[API] ⚠️ No refresh token available, redirecting to login');
        localStorage.removeItem('accessToken');
        localStorage.removeItem('user');
        
        if (!window.location.pathname.includes('/login')) {
          window.location.href = '/login';
        }
      }
    }

    return Promise.reject(error);
  }
);

// ============================================================================
// API SERVICE WRAPPER
// ============================================================================

const apiService = {
  // ========== AUTH SERVICE ==========
  auth: {
    register: (data) => api.post('/gateway/auth/register', data),
    login: (data) => api.post('/gateway/auth/login', data),
    refresh: (data) => api.post('/gateway/auth/refresh', data),
    getProfile: () => api.get('/gateway/auth/profile'),
    updateProfile: (data) => api.put('/gateway/auth/profile', data),
    changePassword: (data) => api.post('/gateway/auth/change-password', data),
    getAddresses: () => api.get('/gateway/auth/addresses'),
    addAddress: (data) => api.post('/gateway/auth/addresses', data),
    updateAddress: (id, data) => api.put(`/gateway/auth/addresses/${id}`, data),
    deleteAddress: (id) => api.delete(`/gateway/auth/addresses/${id}`),
    setDefaultAddress: (id) => api.patch(`/gateway/auth/addresses/${id}/set-default`),
  },

  // ========== CATALOG SERVICE ==========
  catalog: {
    // Restaurants
    getHome: () => api.get('/gateway/catalog/home'),
    getNearbyRestaurants: (city) => api.get(`/gateway/catalog/restaurants/nearby${city ? `?city=${city}` : ''}`),
    getRestaurants: (params) => api.get(`/gateway/catalog/restaurants?${params}`),
    getRestaurantById: (id) => api.get(`/gateway/catalog/restaurants/${id}`),
    getMyRestaurants: () => api.get('/gateway/catalog/restaurants/my'),
    searchRestaurants: (params) => api.get(`/gateway/catalog/restaurants/search?${params}`),
    getAllRestaurantsAdmin: () => api.get('/gateway/catalog/restaurants/admin/all'),
    createRestaurant: (data) => api.post('/gateway/catalog/restaurants', data),
    updateRestaurant: (id, data) => api.put(`/gateway/catalog/restaurants/${id}`, data),
    deleteRestaurant: (id) => api.delete(`/gateway/catalog/restaurants/${id}`),
    toggleRestaurantOpen: (id) => api.patch(`/gateway/catalog/restaurants/${id}/toggle-open`),
    approveRestaurant: (id, data) => api.patch(`/gateway/catalog/restaurants/${id}/approve`, data),
    
    // Menu Items
    getMenuItems: (params) => api.get(`/gateway/catalog/menu-items?${params}`),
    getMenuItemById: (id) => api.get(`/gateway/catalog/menu-items/${id}`),
    createMenuItem: (data) => api.post('/gateway/catalog/menu-items', data),
    updateMenuItem: (id, data) => api.put(`/gateway/catalog/menu-items/${id}`, data),
    deleteMenuItem: (id) => api.delete(`/gateway/catalog/menu-items/${id}`),
    toggleMenuItemAvailability: (id) => api.patch(`/gateway/catalog/menu-items/${id}/toggle-availability`),
    
    // Categories
    getCategories: (restaurantId) => api.get(`/gateway/catalog/categories?restaurantId=${restaurantId}`),
    createCategory: (data) => api.post('/gateway/catalog/categories', data),
    updateCategory: (id, data) => api.put(`/gateway/catalog/categories/${id}`, data),
    deleteCategory: (id) => api.delete(`/gateway/catalog/categories/${id}`),
    reorderCategories: (restaurantId, data) => api.post(`/gateway/catalog/categories/reorder?restaurantId=${restaurantId}`, data),
    
    // Operating Hours
    getOperatingHours: (restaurantId) => api.get(`/gateway/catalog/operating-hours?restaurantId=${restaurantId}`),
    setOperatingHours: (restaurantId, data) => api.post(`/gateway/catalog/operating-hours?restaurantId=${restaurantId}`, data),
    
    // Reviews & Ratings
    getReviews: (restaurantId, page = 1, pageSize = 10) => api.get(`/gateway/catalog/reviews?restaurantId=${restaurantId}&page=${page}&pageSize=${pageSize}`),
    getReviewsSummary: (restaurantId) => api.get(`/gateway/catalog/reviews/summary?restaurantId=${restaurantId}`),
    addReview: (restaurantId, data) => api.post(`/gateway/catalog/reviews?restaurantId=${restaurantId}`, data),
    updateReview: (id, data) => api.put(`/gateway/catalog/reviews/${id}`, data),
    deleteReview: (id) => api.delete(`/gateway/catalog/reviews/${id}`),
    markReviewHelpful: (id) => api.post(`/gateway/catalog/reviews/${id}/helpful`),
  },

  // ========== ORDER SERVICE ==========
  orders: {
    // Cart
    getCart: () => api.get('/gateway/orders/cart'),
    addToCart: (data) => api.post('/gateway/orders/cart/items', data),
    updateCartItem: (id, data) => api.put(`/gateway/orders/cart/items/${id}`, data),
    removeFromCart: (id) => api.delete(`/gateway/orders/cart/items/${id}`),
    clearCart: () => api.delete('/gateway/orders/cart'),
    applyCoupon: (data) => api.post('/gateway/orders/cart/apply-coupon', data),
    getCheckoutContext: () => api.get('/gateway/orders/cart/checkout-context'),
    
    // Orders
    placeOrder: (data) => api.post('/gateway/orders/orders', data),
    createOrder: (data) => api.post('/gateway/orders/orders', data),
    getMyOrders: () => api.get('/gateway/orders/orders/my'),
    searchOrders: (params) => api.get(`/gateway/orders/orders/search?${params}`),
    getOrderById: (id) => api.get(`/gateway/orders/orders/${id}`),
    getOrdersByRestaurant: (restaurantId) => api.get(`/gateway/orders/orders/restaurant/${restaurantId}`),
    getAllOrders: () => api.get('/gateway/orders/orders'),
    updateOrderStatus: (id, data) => api.put(`/gateway/orders/orders/${id}/status`, data),
    
    // New Order Management Endpoints
    rejectOrder: (id, data) => api.post(`/gateway/orders/orders/${id}/reject`, data),
    deleteOrder: (id) => api.delete(`/gateway/orders/orders/${id}`),
    reorderOrder: (id) => api.post(`/gateway/orders/orders/${id}/reorder`),
    getMyOrdersFiltered: (filter) => api.get(`/gateway/orders/orders/my/filtered?filter=${filter || ''}`),
    getRestaurantOrdersFiltered: (restaurantId, filter) => api.get(`/gateway/orders/orders/restaurant/${restaurantId}/filtered?filter=${filter || ''}`),
    
    // Coupons
    getCoupons: () => api.get('/gateway/orders/coupons'),
    getCouponById: (id) => api.get(`/gateway/orders/coupons/${id}`),
    getMyCoupons: (restaurantId) => api.get(`/gateway/orders/coupons/my?restaurantId=${restaurantId}`),
    getActiveCoupons: () => api.get('/gateway/orders/coupons/active'),
    createCoupon: (data) => api.post('/gateway/orders/coupons', data),
    updateCoupon: (id, data) => api.put(`/gateway/orders/coupons/${id}`, data),
    deleteCoupon: (id) => api.delete(`/gateway/orders/coupons/${id}`),
    validateCoupon: (data) => api.post('/gateway/orders/coupons/validate', data),
    
    // Payments
    simulatePayment: (data) => api.post('/gateway/orders/payments/simulate', data),
    getPaymentByOrder: (orderId) => api.get(`/gateway/orders/payments/order/${orderId}`),
    
    // Deliveries
    assignDelivery: (data) => api.post('/gateway/deliveries/assign', data),
    getPendingDeliveries: () => api.get('/gateway/deliveries/pending'),
    getMyDeliveries: () => api.get('/gateway/deliveries/my'),
    trackDelivery: (orderId) => api.get(`/gateway/deliveries/track/${orderId}`),
    getDeliveryById: (id) => api.get(`/gateway/deliveries/${id}`),
    updateDeliveryStatus: (id, data) => api.put(`/gateway/deliveries/${id}/status`, data),
    
    // Ratings
    addRating: (orderId, data) => api.post(`/gateway/orders/orders/${orderId}/rating`, data),
    getRating: (orderId) => api.get(`/gateway/orders/orders/${orderId}/rating`),
    updateRating: (ratingId, data) => api.put(`/gateway/orders/orders/ratings/${ratingId}`, data),
    deleteRating: (ratingId) => api.delete(`/gateway/orders/orders/ratings/${ratingId}`),
    getMyRatings: () => api.get('/gateway/orders/orders/ratings/my'),
    getCancellationReasons: () => api.get('/gateway/orders/orders/cancellation-reasons'),
    canCancelOrder: (orderId) => api.get(`/gateway/orders/orders/${orderId}/can-cancel`),
    cancelOrder: (orderId, data) => api.post(`/gateway/orders/orders/${orderId}/cancel`, data),
  },

  // ========== ADMIN SERVICE ==========
  admin: {
    // Dashboard
    getDashboard: () => api.get('/gateway/admin/dashboard'),
    
    // Users Management
    getAllUsers: (params) => api.get(`/gateway/admin/users?${params}`),
    getUserById: (id) => api.get(`/gateway/admin/users/${id}`),
    toggleUserStatus: (id, data) => api.patch(`/gateway/admin/users/${id}/status`, data),
    softDeleteUser: (id, data) => api.delete(`/gateway/admin/users/${id}`, { data }),
    
    // Orders Management
    getAllOrders: (params) => api.get(`/gateway/admin/orders?${params}`),
    getOrderById: (id) => api.get(`/gateway/admin/orders/${id}`),
    updateOrderStatus: (id, data) => api.put(`/gateway/admin/orders/${id}/status`, data),
    
    // Restaurants Management
    getAllRestaurants: (params) => api.get(`/gateway/admin/restaurants?${params}`),
    getRestaurantById: (id) => api.get(`/gateway/admin/restaurants/${id}`),
    approveRestaurant: (id, data) => api.patch(`/gateway/admin/restaurants/${id}/approve`, data),
    rejectRestaurant: (id, data) => api.post(`/gateway/admin/restaurants/${id}/reject`, data),
    toggleRestaurantActive: (id, data) => api.post(`/gateway/admin/restaurants/${id}/toggle-active`, data),
    updateRestaurantStatus: (id, data) => api.patch(`/gateway/admin/restaurants/${id}/status`, data),
    softDeleteRestaurant: (id, data) => api.delete(`/gateway/admin/restaurants/${id}`, { data }),
    restoreRestaurant: (id, data) => api.post(`/gateway/admin/restaurants/${id}/restore`, data),
    permanentlyDeleteRestaurant: (id) => api.delete(`/gateway/admin/restaurants/${id}/permanent`),
    
    // Delivery Agents Management
    getAllDeliveryAgents: (params) => api.get(`/gateway/admin/delivery-agents?${params}`),
    getDeliveryAgentById: (id) => api.get(`/gateway/admin/delivery-agents/${id}`),
    updateDeliveryAgentStatus: (id, data) => api.patch(`/gateway/admin/delivery-agents/${id}/status`, data),
    softDeleteAgent: (id, data) => api.delete(`/gateway/admin/delivery-agents/${id}`, { data }),
    restoreAgent: (id, data) => api.post(`/gateway/admin/delivery-agents/${id}/restore`, data),
    syncDeliveryAgents: () => api.post('/gateway/admin/delivery-agents/sync'),
    
    // Refund Management
    getPendingRefunds: () => api.get('/gateway/admin/refunds/pending'),
    getAllRefunds: (status) => api.get(`/gateway/admin/refunds${status ? `?status=${status}` : ''}`),
    processRefund: (id, data) => api.post(`/gateway/admin/refunds/${id}/process`, data),
    
    // Reports
    getSalesReport: (params) => api.get(`/gateway/admin/reports/sales?${params}`),
    getPartnerReport: (params) => api.get(`/gateway/admin/reports/partners?${params}`),
    
    // Complaints Management
    getAllComplaints: (params) => api.get(`/gateway/admin/complaints?${params}`),
    getComplaintById: (id) => api.get(`/gateway/admin/complaints/${id}`),
    resolveComplaint: (id, data) => api.post(`/gateway/admin/complaints/${id}/resolve`, data),
  },
};

// Export the axios instance as default for backward compatibility
export default api;

// Export the service wrapper as a named export
export { apiService };
