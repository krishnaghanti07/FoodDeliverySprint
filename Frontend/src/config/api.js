const API_BASE_URL = '/gateway';

export const API_ENDPOINTS = {
  // Auth Service
  auth: {
    register: `${API_BASE_URL}/auth/register`,
    login: `${API_BASE_URL}/auth/login`,
    refresh: `${API_BASE_URL}/auth/refresh`,
    sendOtp: `${API_BASE_URL}/auth/send-otp`,
    verifyOtp: `${API_BASE_URL}/auth/verify-otp`,
    resendOtp: `${API_BASE_URL}/auth/resend-otp`,
    verifyEmail: `${API_BASE_URL}/auth/verify-email`,
    toggle2fa: `${API_BASE_URL}/auth/toggle-2fa`,
    forgotPassword: `${API_BASE_URL}/auth/forgot-password`,
    resetPassword: `${API_BASE_URL}/auth/reset-password`,
    changePassword: `${API_BASE_URL}/auth/change-password`,
    profile: `${API_BASE_URL}/auth/profile`,
    addresses: `${API_BASE_URL}/auth/addresses`,
    addressDefault: `${API_BASE_URL}/auth/addresses/default`,
    addressById: (id) => `${API_BASE_URL}/auth/addresses/${id}`,
    setDefaultAddress: (id) => `${API_BASE_URL}/auth/addresses/${id}/set-default`,
  },

  // Catalog Service
  catalog: {
    home: `${API_BASE_URL}/catalog/home`,
    restaurantsNearby: `${API_BASE_URL}/catalog/restaurants/nearby`,
    restaurants: `${API_BASE_URL}/catalog/restaurants`,
    restaurantsMyPartner: `${API_BASE_URL}/catalog/restaurants/my`,
    restaurantById: (id) => `${API_BASE_URL}/catalog/restaurants/${id}`,
    restaurantSearch: `${API_BASE_URL}/catalog/restaurants/search`,
    restaurantToggleOpen: (id) => `${API_BASE_URL}/catalog/restaurants/${id}/toggle-open`,
    restaurantApprove: (id) => `${API_BASE_URL}/catalog/restaurants/${id}/approve`,
    restaurantsAdminAll: `${API_BASE_URL}/catalog/restaurants/admin/all`,
    menuItems: `${API_BASE_URL}/catalog/menu-items`,
    menuItemById: (id) => `${API_BASE_URL}/catalog/menu-items/${id}`,
    menuItemToggle: (id) => `${API_BASE_URL}/catalog/menu-items/${id}/toggle-availability`,
    categories: `${API_BASE_URL}/catalog/categories`,
    categoryById: (id) => `${API_BASE_URL}/catalog/categories/${id}`,
    categoriesReorder: `${API_BASE_URL}/catalog/categories/reorder`,
    operatingHours: `${API_BASE_URL}/catalog/operating-hours`,
    reviews: `${API_BASE_URL}/catalog/reviews`,
    reviewById: (id) => `${API_BASE_URL}/catalog/reviews/${id}`,
    reviewsSummary: `${API_BASE_URL}/catalog/reviews/summary`,
    reviewHelpful: (id) => `${API_BASE_URL}/catalog/reviews/${id}/helpful`,
  },

  // Order Service
  orders: {
    // Cart endpoints
    cart: `${API_BASE_URL}/orders/cart`,
    cartItems: `${API_BASE_URL}/orders/cart/items`,
    cartItemById: (id) => `${API_BASE_URL}/orders/cart/items/${id}`,
    cartApplyCoupon: `${API_BASE_URL}/orders/cart/apply-coupon`,
    cartRemoveCoupon: `${API_BASE_URL}/orders/cart/remove-coupon`,
    cartCheckoutContext: `${API_BASE_URL}/orders/cart/checkout-context`,
    
    // Order endpoints
    placeOrder: `${API_BASE_URL}/orders/orders`,
    orders: `${API_BASE_URL}/orders/orders`,
    myOrders: `${API_BASE_URL}/orders/orders/my`,
    orderById: (id) => `${API_BASE_URL}/orders/orders/${id}`,
    orderStatus: (id) => `${API_BASE_URL}/orders/orders/${id}/status`,
    ordersByRestaurant: (id) => `${API_BASE_URL}/orders/orders/restaurant/${id}`,
    orderSearch: `${API_BASE_URL}/orders/orders/search`,
    
    // New Order Management Endpoints
    rejectOrder: (id) => `${API_BASE_URL}/orders/orders/${id}/reject`,
    deleteOrder: (id) => `${API_BASE_URL}/orders/orders/${id}`,
    reorderOrder: (id) => `${API_BASE_URL}/orders/orders/${id}/reorder`,
    myOrdersFiltered: (filter) => `${API_BASE_URL}/orders/orders/my/filtered?filter=${filter || ''}`,
    restaurantOrdersFiltered: (restaurantId, filter) => `${API_BASE_URL}/orders/orders/restaurant/${restaurantId}/filtered?filter=${filter || ''}`,
    
    // Coupon endpoints
    coupons: `${API_BASE_URL}/orders/coupons`,
    myCoupons: `${API_BASE_URL}/orders/coupons/my`,
    couponsByRestaurant: (restaurantId) => `${API_BASE_URL}/orders/coupons/my?restaurantId=${restaurantId}`,
    couponById: (id) => `${API_BASE_URL}/orders/coupons/${id}`,
    couponValidate: `${API_BASE_URL}/orders/coupons/validate`,
    couponsActive: `${API_BASE_URL}/orders/coupons/active`,
    
    // Rating endpoints
    orderRating: (orderId) => `${API_BASE_URL}/orders/orders/${orderId}/rating`,
    ratingById: (id) => `${API_BASE_URL}/orders/ratings/${id}`,
    myRatings: `${API_BASE_URL}/orders/ratings/my`,
    cancellationReasons: `${API_BASE_URL}/orders/cancellation-reasons`,
    canCancelOrder: (orderId) => `${API_BASE_URL}/orders/orders/${orderId}/can-cancel`,
    cancelOrder: (orderId) => `${API_BASE_URL}/orders/orders/${orderId}/cancel`,
    
    // Payment endpoints
    paymentSimulate: `${API_BASE_URL}/orders/payments/simulate`,
    paymentByOrder: (orderId) => `${API_BASE_URL}/orders/payments/order/${orderId}`,
  },

  // Delivery
  deliveries: {
    assign: `${API_BASE_URL}/deliveries/assign`,
    pending: `${API_BASE_URL}/deliveries/pending`,
    my: `${API_BASE_URL}/deliveries/my`,
    available: `${API_BASE_URL}/deliveries/available`,
    accept: (orderId) => `${API_BASE_URL}/deliveries/${orderId}/accept`,
    track: (orderId) => `${API_BASE_URL}/deliveries/track/${orderId}`,
    byId: (id) => `${API_BASE_URL}/deliveries/${id}`,
    status: (id) => `${API_BASE_URL}/deliveries/${id}/status`,
  },

  // Payment Service
  payments: {
    simulate: `${API_BASE_URL}/payments/simulate`,
    byOrder: (orderId) => `${API_BASE_URL}/payments/order/${orderId}`,
    byId: (id) => `${API_BASE_URL}/payments/${id}`,
    my: `${API_BASE_URL}/payments/my`,
    all: `${API_BASE_URL}/payments`,
    refund: `${API_BASE_URL}/payments/refund`,
    razorpayCreate: `${API_BASE_URL}/payments/razorpay/create-order`,
    razorpayCreateOnly: `${API_BASE_URL}/payments/razorpay/create-order-only`,
    razorpayVerify: `${API_BASE_URL}/payments/razorpay/verify`,
    razorpayCancel: `${API_BASE_URL}/payments/razorpay/cancel`,
  },

  // Admin Service
  admin: {
    dashboard: `${API_BASE_URL}/admin/dashboard`,
    users: `${API_BASE_URL}/admin/users`,
    userById: (id) => `${API_BASE_URL}/admin/users/${id}`,
    userStatus: (id) => `${API_BASE_URL}/admin/users/${id}/status`,
    orders: `${API_BASE_URL}/admin/orders`,
    orderById: (id) => `${API_BASE_URL}/admin/orders/${id}`,
    orderStatus: (id) => `${API_BASE_URL}/admin/orders/${id}/status`,
    reportsSales: `${API_BASE_URL}/admin/reports/sales`,
    reportsPartners: `${API_BASE_URL}/admin/reports/partners`,
  },
};

export default API_BASE_URL;
