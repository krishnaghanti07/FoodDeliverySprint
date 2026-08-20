import { createContext, useContext, useState, useCallback } from 'react';
import api from '../services/api';
import { API_ENDPOINTS } from '../config/api';
import toast from 'react-hot-toast';

const CartContext = createContext(null);

export function CartProvider({ children }) {
  const [cart, setCart] = useState(null);
  const [cartLoading, setCartLoading] = useState(false);

  const fetchCart = useCallback(async () => {
    setCartLoading(true);
    try {
      const res = await api.get(API_ENDPOINTS.orders.cart);
      const cartData = res.data?.data || res.data;
      setCart(cartData);
      return cartData;
    } catch (err) {
      if (err.response?.status !== 404) console.error('Cart fetch error:', err);
      setCart(null);
    } finally {
      setCartLoading(false);
    }
  }, []);

  const addToCart = useCallback(async (item) => {
    console.log('🛒 [CART] Adding item to cart:', JSON.stringify(item, null, 2));
    console.log('🛒 [CART] Endpoint:', API_ENDPOINTS.orders.cartItems);
    
    try {
      const res = await api.post(API_ENDPOINTS.orders.cartItems, item);
      console.log('✅ [CART] Item added successfully:', res.data);
      const cartData = res.data?.data || res.data;
      setCart(cartData);
      toast.success('Added to cart!');
      return cartData;
    } catch (err) {
      console.error('❌ [CART] Failed to add item:', {
        status: err.response?.status,
        statusText: err.response?.statusText,
        data: err.response?.data,
        message: err.message,
        requestPayload: item
      });
      
      const msg = err.response?.data?.message || err.response?.data?.title || 'Failed to add item';
      
      // If it's a mixed cart error, suggest clearing the cart
      if (msg.includes('another restaurant')) {
        toast.error(msg + ' Go to cart to clear it.', { duration: 5000 });
      } else {
        toast.error(msg);
      }
      throw err;
    }
  }, []);

  const updateCartItem = useCallback(async (cartItemId, payload) => {
    try {
      const res = await api.put(API_ENDPOINTS.orders.cartItemById(cartItemId), payload);
      const cartData = res.data?.data || res.data;
      setCart(cartData);
      return cartData;
    } catch (err) {
      toast.error('Failed to update item');
      throw err;
    }
  }, []);

  const removeCartItem = useCallback(async (cartItemId) => {
    try {
      const res = await api.delete(API_ENDPOINTS.orders.cartItemById(cartItemId));
      const cartData = res.data?.data || res.data;
      setCart(cartData);
      toast.success('Item removed');
      return cartData;
    } catch (err) {
      toast.error('Failed to remove item');
      throw err;
    }
  }, []);

  const clearCart = useCallback(async (showToast = true) => {
    try {
      await api.delete(API_ENDPOINTS.orders.cart);
      setCart(null);
      if (showToast) toast.success('Cart cleared');
    } catch (err) {
      if (showToast) toast.error('Failed to clear cart');
    }
  }, []);

  const applyCoupon = useCallback(async (couponCode) => {
    try {
      const res = await api.post(API_ENDPOINTS.orders.cartApplyCoupon, { couponCode });
      const cartData = res.data?.data || res.data;
      setCart(cartData);
      toast.success('Coupon applied!');
      return cartData;
    } catch (err) {
      const msg = err.response?.data?.message || 'Invalid coupon';
      toast.error(msg);
      throw err;
    }
  }, []);

  const removeCoupon = useCallback(async () => {
    try {
      const res = await api.delete(API_ENDPOINTS.orders.cartRemoveCoupon);
      const cartData = res.data?.data || res.data;
      setCart(cartData);
      toast.success('Coupon removed');
      return cartData;
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to remove coupon';
      toast.error(msg);
      throw err;
    }
  }, []);

  const cartItemCount = cart?.items?.reduce((sum, item) => sum + (item.quantity || 1), 0) || 0;

  const value = {
    cart,
    cartLoading,
    cartItemCount,
    fetchCart,
    addToCart,
    updateCartItem,
    removeCartItem,
    clearCart,
    applyCoupon,
    removeCoupon,
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error('useCart must be used within CartProvider');
  return ctx;
}

export default CartContext;
