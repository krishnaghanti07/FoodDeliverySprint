import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { MapPin, CreditCard, Wallet, DollarSign, ArrowLeft, AlertCircle } from 'lucide-react';
import api from '../../services/api';
import { API_ENDPOINTS } from '../../config/api';
import { useCart } from '../../context/CartContext';
import { useAuth } from '../../context/AuthContext';
import { isRestaurantOpen } from '../../utils/timeUtils';
import toast from 'react-hot-toast';
import { AddressCardSkeleton } from '../../components/common/Skeleton';
import './CheckoutPage.css';

export default function CheckoutPage() {
  const navigate = useNavigate();
  const { cart, clearCart } = useCart();
  const { isAuthenticated } = useAuth();
  const [loading, setLoading] = useState(true);
  const [placing, setPlacing] = useState(false);
  const [orderPlaced, setOrderPlaced] = useState(false);
  const [checkoutContext, setCheckoutContext] = useState(null);
  const [addresses, setAddresses] = useState([]);
  const [selectedAddress, setSelectedAddress] = useState(null);
  const [paymentMethod, setPaymentMethod] = useState('COD');
  const [deliveryInstructions, setDeliveryInstructions] = useState('');
  const [processingPayment, setProcessingPayment] = useState(false); // Add flag to prevent duplicate calls
  const [walletBalance, setWalletBalance] = useState(0);
  const [restaurantStatus, setRestaurantStatus] = useState({ isOpen: true, loading: true });

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    if (orderPlaced) return;
    if (!cart || !cart.items || cart.items.length === 0) {
      toast.error('Your cart is empty');
      navigate('/cart');
      return;
    }
    // Only run once on mount — cart is intentionally excluded from deps
    // to prevent re-fetching every time cart state updates during checkout
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated, orderPlaced]);

  const fetchData = async () => {
    try {
      const [checkoutRes, addressRes, profileRes] = await Promise.all([
        api.get(API_ENDPOINTS.orders.cartCheckoutContext),
        api.get(API_ENDPOINTS.auth.addresses),
        api.get(API_ENDPOINTS.auth.profile),
      ]);

      const checkoutData = checkoutRes.data?.data || checkoutRes.data;
      const addressData = addressRes.data?.data || addressRes.data;
      const profileData = profileRes.data?.data || profileRes.data;

      setCheckoutContext(checkoutData);
      setAddresses(Array.isArray(addressData) ? addressData : []);
      setWalletBalance(profileData?.walletBalance || 0);

      // Select default address
      const defaultAddr = addressData.find((a) => a.isDefault);
      if (defaultAddr) {
        setSelectedAddress(defaultAddr.id);
      } else if (addressData.length > 0) {
        setSelectedAddress(addressData[0].id);
      }

      // Check restaurant status — use restaurantId from checkout context or cart
      const restaurantId = checkoutData?.restaurantId || cart?.restaurantId;

      if (restaurantId) {
        try {
          const [restaurantRes, hoursRes] = await Promise.all([
            api.get(API_ENDPOINTS.catalog.restaurantById(restaurantId)),
            api.get(`${API_ENDPOINTS.catalog.operatingHours}?restaurantId=${restaurantId}`)
          ]);

          const restaurant = restaurantRes.data?.data || restaurantRes.data;
          const hours = hoursRes.data?.data || hoursRes.data || [];

          const { isOpen, nextOpenTime } = isRestaurantOpen(hours, restaurant.isOpen);
          
          setRestaurantStatus({ 
            isOpen, 
            nextOpenTime, 
            restaurantName: restaurant.name,
            loading: false 
          });

          // If restaurant is closed, show error and redirect to cart
          if (!isOpen) {
            toast.error(`${restaurant.name} is currently closed. Cannot place order.`);
            setTimeout(() => navigate('/cart'), 2000);
          }
        } catch (error) {
          console.error('Failed to check restaurant status:', error);
          // Fail open — allow checkout if we can't verify hours
          setRestaurantStatus({ isOpen: true, loading: false });
        }
      } else {
        // No restaurantId available — fail open so checkout is not blocked
        setRestaurantStatus({ isOpen: true, loading: false });
      }
    } catch (err) {
      console.error('Failed to load checkout data:', err);
      toast.error('Failed to load checkout data');
      // Always clear loading state even on outer error
      setRestaurantStatus(prev => ({ ...prev, loading: false }));
    } finally {
      setLoading(false);
    }
  };

  const handlePlaceOrder = async () => {
    if (placing || processingPayment) {
      console.log('Already processing order, ignoring duplicate call');
      return;
    }

    // Check if restaurant is open
    if (!restaurantStatus.isOpen) {
      toast.error('Restaurant is currently closed. Cannot place order.');
      navigate('/cart');
      return;
    }

    if (!selectedAddress) {
      toast.error('Please select a delivery address');
      return;
    }

    const address = addresses.find((a) => a.id === selectedAddress);
    if (!address) {
      toast.error('Invalid address selected');
      return;
    }

    // For COD, place order directly
    if (paymentMethod === 'COD') {
      await placeOrderDirect();
      return;
    }

    // For Wallet, process wallet payment
    if (paymentMethod === 'Wallet') {
      await processWalletPayment();
      return;
    }

    // For Online (Razorpay), initiate payment
    await initiateRazorpayPayment();
  };

  const placeOrderDirect = async () => {
    const address = addresses.find((a) => a.id === selectedAddress);
    
    setPlacing(true);
    try {
      const orderData = {
        deliveryAddress: `${address.fullAddress}, ${address.city}, ${address.state} ${address.pincode}`,
        deliveryInstructions: deliveryInstructions.trim() || null,
        paymentMethod,
      };

      const res = await api.post(API_ENDPOINTS.orders.placeOrder, orderData);
      const order = res.data?.data || res.data;

      toast.success('Order placed successfully!');
      setOrderPlaced(true);
      await clearCart(false);
      navigate(`/orders/${order.id}`);
    } catch (err) {
      console.error('Failed to place order:', err);
      const msg = err.response?.data?.message || 'Failed to place order';
      toast.error(msg);
    } finally {
      setPlacing(false);
    }
  };

  const initiateRazorpayPayment = async () => {
    if (processingPayment) {
      console.log('Payment already in progress, ignoring duplicate call');
      return;
    }

    const address = addresses.find((a) => a.id === selectedAddress);
    
    setPlacing(true);
    setProcessingPayment(true);
    
    try {
      // Step 1: Create Razorpay order (NO database order yet)
      // We'll pass the cart data to create Razorpay order
      const razorpayOrderData = {
        amount: checkoutContext.totalAmount,
        currency: 'INR',
        // Store order details in notes for later
        notes: {
          deliveryAddress: `${address.fullAddress}, ${address.city}, ${address.state} ${address.pincode}`,
          deliveryInstructions: deliveryInstructions.trim() || '',
          paymentMethod: paymentMethod
        }
      };
      
      console.log('Creating Razorpay order (no DB order yet):', razorpayOrderData);
      
      const razorpayOrderRes = await api.post(API_ENDPOINTS.payments.razorpayCreateOnly, razorpayOrderData);
      const razorpayData = razorpayOrderRes.data?.data || razorpayOrderRes.data;
      console.log('Razorpay order created:', razorpayData);

      // Validate Razorpay data
      if (!razorpayData.key || !razorpayData.razorpayOrderId) {
        throw new Error('Invalid Razorpay order data received');
      }

      // Step 2: Open Razorpay payment modal
      const options = {
        key: razorpayData.key,
        amount: razorpayData.amount * 100, // Razorpay expects amount in paise
        currency: razorpayData.currency || 'INR',
        name: 'FoodRush',
        description: 'Order Payment',
        order_id: razorpayData.razorpayOrderId,
        handler: async function (response) {
          console.log('Payment successful:', response);
          // NOW create the order after payment succeeds
          await createOrderAfterPayment(response, razorpayData.razorpayOrderId);
        },
        prefill: {
          name: address.fullAddress,
          email: '',
          contact: '',
        },
        theme: {
          color: '#FF6B35',
        },
        modal: {
          ondismiss: function () {
            console.log('Payment modal dismissed - payment cancelled');
            toast.error('Payment cancelled. Order was not placed.');
            setPlacing(false);
            setProcessingPayment(false);
            navigate('/cart');
          },
        },
      };

      console.log('Opening Razorpay modal with options:', options);
      
      if (!window.Razorpay) {
        throw new Error('Razorpay SDK not loaded');
      }

      const razorpay = new window.Razorpay(options);
      
      razorpay.on('payment.failed', function (response) {
        console.error('Payment failed:', response);
        const errorMsg = response.error?.description || response.error?.reason || 'Payment failed';
        toast.error('Payment failed: ' + errorMsg);
        setPlacing(false);
        setProcessingPayment(false);
        navigate('/cart');
      });

      try {
        razorpay.open();
      } catch (razorpayError) {
        console.error('Razorpay open error:', razorpayError);
        toast.error('Failed to open payment modal: ' + razorpayError.message);
        setPlacing(false);
        setProcessingPayment(false);
      }
    } catch (err) {
      console.error('Failed to initiate payment:', err);
      const msg = err.response?.data?.message || err.message || 'Failed to initiate payment';
      toast.error(msg);
      setPlacing(false);
      setProcessingPayment(false);
    }
  };

  const processWalletPayment = async () => {
    const address = addresses.find((a) => a.id === selectedAddress);
    const totalAmount = checkoutContext?.totalAmount || 0;

    if (walletBalance < totalAmount) {
      toast.error('Insufficient wallet balance');
      return;
    }

    setPlacing(true);
    setProcessingPayment(true);
    
    try {
      // Place order with Wallet payment method
      const orderData = {
        deliveryAddress: `${address.fullAddress}, ${address.city}, ${address.state} ${address.pincode}`,
        deliveryInstructions: deliveryInstructions.trim() || null,
        paymentMethod: 'Wallet',
      };

      console.log('Placing order with wallet payment:', orderData);
      const orderRes = await api.post(API_ENDPOINTS.orders.placeOrder, orderData);
      const order = orderRes.data?.data || orderRes.data;
      console.log('Order created:', order);

      // Clear cart and show success
      await clearCart();
      setOrderPlaced(true);
      toast.success('Order placed successfully!');
      
      setTimeout(() => {
        navigate(`/orders/${order.id}`);
      }, 1500);
    } catch (err) {
      console.error('Failed to place order with wallet:', err);
      const msg = err.response?.data?.message || err.message || 'Failed to place order';
      toast.error(msg);
    } finally {
      setPlacing(false);
      setProcessingPayment(false);
    }
  };

  const createOrderAfterPayment = async (paymentResponse, razorpayOrderId) => {
    try {
      const address = addresses.find((a) => a.id === selectedAddress);
      
      // Create order with payment details
      // Convert "Online" to "CARD" for backend
      const backendPaymentMethod = paymentMethod === 'Online' ? 'CARD' : paymentMethod;
      
      const orderData = {
        deliveryAddress: `${address.fullAddress}, ${address.city}, ${address.state} ${address.pincode}`,
        deliveryInstructions: deliveryInstructions.trim() || null,
        paymentMethod: backendPaymentMethod,
        razorpayOrderId: razorpayOrderId,
        razorpayPaymentId: paymentResponse.razorpay_payment_id,
        razorpaySignature: paymentResponse.razorpay_signature,
      };

      console.log('Creating order after successful payment:', orderData);
      const orderRes = await api.post(API_ENDPOINTS.orders.placeOrder, orderData);
      const order = orderRes.data?.data || orderRes.data;
      console.log('Order created:', order);

      toast.success('Payment successful! Order placed.');
      setOrderPlaced(true);
      await clearCart(false);
      navigate(`/orders/${order.id}`);
    } catch (err) {
      console.error('Failed to create order after payment:', err);
      toast.error('Payment succeeded but order creation failed. Please contact support.');
    } finally {
      setPlacing(false);
      setProcessingPayment(false);
    }
  };



  if (loading) {
    return (
      <div className="checkout-page page-enter">
        <div className="container">
          {/* Back button skeleton */}
          <div className="skeleton" style={{ height: '1.5rem', width: '8rem', marginBottom: 'var(--space-lg)' }} />
          <div className="skeleton" style={{ height: '2rem', width: '10rem', marginBottom: 'var(--space-xl)' }} />
          <div className="checkout-layout">
            {/* Main content skeleton */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-xl)' }}>
              {/* Address section */}
              <div className="card" style={{ padding: 'var(--space-xl)' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: 'var(--space-lg)' }}>
                  <div className="skeleton skeleton-circle" style={{ height: '1.5rem', width: '1.5rem' }} />
                  <div className="skeleton" style={{ height: '1.5rem', width: '12rem' }} />
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
                  {Array.from({ length: 2 }).map((_, i) => (
                    <AddressCardSkeleton key={i} />
                  ))}
                </div>
              </div>
              {/* Payment section */}
              <div className="card" style={{ padding: 'var(--space-xl)' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: 'var(--space-lg)' }}>
                  <div className="skeleton skeleton-circle" style={{ height: '1.5rem', width: '1.5rem' }} />
                  <div className="skeleton" style={{ height: '1.5rem', width: '10rem' }} />
                </div>
                {Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="skeleton" style={{ height: '4.5rem', borderRadius: 'var(--rounded-lg)', marginBottom: 'var(--space-md)' }} />
                ))}
              </div>
            </div>
            {/* Summary skeleton */}
            <div className="card" style={{ padding: 'var(--space-xl)', alignSelf: 'flex-start' }}>
              <div className="skeleton" style={{ height: '1.25rem', width: '8rem', marginBottom: 'var(--space-lg)' }} />
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                  <div className="skeleton" style={{ height: '0.875rem', width: '6rem' }} />
                  <div className="skeleton" style={{ height: '0.875rem', width: '4rem' }} />
                </div>
              ))}
              <div style={{ borderTop: '1px solid var(--outline-variant)', paddingTop: 'var(--space-md)', marginTop: 'var(--space-sm)', marginBottom: 'var(--space-lg)' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <div className="skeleton" style={{ height: '1.25rem', width: '5rem' }} />
                  <div className="skeleton" style={{ height: '1.25rem', width: '5rem' }} />
                </div>
              </div>
              <div className="skeleton" style={{ height: '3rem', width: '100%', borderRadius: 'var(--rounded-lg)' }} />
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!checkoutContext) {
    return (
      <div className="checkout-page page-enter">
        <div className="container">
          <p className="text-muted">Unable to load checkout information</p>
        </div>
      </div>
    );
  }

  return (
    <div className="checkout-page page-enter">
      <div className="container">
        <button className="btn btn-text back-btn" onClick={() => navigate('/cart')}>
          <ArrowLeft size={20} /> Back to Cart
        </button>

        <h1 className="headline-xl">Checkout</h1>

        <div className="checkout-layout">
          {/* Main Content */}
          <div className="checkout-main">
            {/* Delivery Address */}
            <div className="checkout-section">
              <h2 className="headline-lg">
                <MapPin size={24} /> Delivery Address
              </h2>

              {addresses.length === 0 ? (
                <div className="empty-addresses">
                  <p className="body-lg text-muted">No saved addresses</p>
                  <button className="btn btn-secondary" onClick={() => navigate('/profile')}>
                    Add Address
                  </button>
                </div>
              ) : (
                <div className="address-list">
                  {addresses.map((addr) => (
                    <label key={addr.id} className={`address-card ${selectedAddress === addr.id ? 'selected' : ''}`}>
                      <input
                        type="radio"
                        name="address"
                        value={addr.id}
                        checked={selectedAddress === addr.id}
                        onChange={() => setSelectedAddress(addr.id)}
                      />
                      <div className="address-content">
                        <div className="address-header">
                          <span className="headline-sm">{addr.label || 'Address'}</span>
                          {addr.isDefault && <span className="badge badge-primary">Default</span>}
                        </div>
                        <p className="body-md">
                          {addr.fullAddress}, {addr.city}
                        </p>
                        <p className="body-sm text-muted">
                          {addr.state} {addr.pincode}
                        </p>
                      </div>
                    </label>
                  ))}
                </div>
              )}
            </div>

            {/* Payment Method */}
            <div className="checkout-section">
              <h2 className="headline-lg">
                <CreditCard size={24} /> Payment Method
              </h2>

              <div className="payment-methods">
                <label className={`payment-card ${paymentMethod === 'COD' ? 'selected' : ''}`}>
                  <input
                    type="radio"
                    name="payment"
                    value="COD"
                    checked={paymentMethod === 'COD'}
                    onChange={() => setPaymentMethod('COD')}
                  />
                  <div className="payment-content">
                    <DollarSign size={24} />
                    <div>
                      <span className="headline-sm">Cash on Delivery</span>
                      <p className="body-sm text-muted">Pay when you receive</p>
                    </div>
                  </div>
                </label>

                <label className={`payment-card ${paymentMethod === 'Online' ? 'selected' : ''}`}>
                  <input
                    type="radio"
                    name="payment"
                    value="Online"
                    checked={paymentMethod === 'Online'}
                    onChange={() => setPaymentMethod('Online')}
                  />
                  <div className="payment-content">
                    <CreditCard size={24} />
                    <div>
                      <span className="headline-sm">Online Transaction</span>
                      <p className="body-sm text-muted">Pay via Razorpay (Cards, UPI, etc.)</p>
                    </div>
                  </div>
                </label>

                <label className={`payment-card ${paymentMethod === 'Wallet' ? 'selected' : ''} ${walletBalance < (checkoutContext?.totalAmount || 0) ? 'disabled' : ''}`}>
                  <input
                    type="radio"
                    name="payment"
                    value="Wallet"
                    checked={paymentMethod === 'Wallet'}
                    onChange={() => setPaymentMethod('Wallet')}
                    disabled={walletBalance < (checkoutContext?.totalAmount || 0)}
                  />
                  <div className="payment-content">
                    <Wallet size={24} />
                    <div>
                      <span className="headline-sm">Digital Wallet</span>
                      <p className="body-sm text-muted">
                        Balance: ₹{walletBalance.toFixed(2)}
                        {walletBalance < (checkoutContext?.totalAmount || 0) && (
                          <span style={{color: 'var(--error)', display: 'block'}}>Insufficient balance</span>
                        )}
                      </p>
                    </div>
                  </div>
                </label>
              </div>
            </div>

            {/* Delivery Instructions */}
            <div className="checkout-section">
              <h2 className="headline-lg">Delivery Instructions (Optional)</h2>
              <textarea
                className="delivery-instructions"
                placeholder="E.g., Ring the doorbell, Leave at door, etc."
                value={deliveryInstructions}
                onChange={(e) => setDeliveryInstructions(e.target.value)}
                maxLength={300}
                rows={3}
              />
            </div>
          </div>

          {/* Order Summary */}
          <div className="checkout-summary">
            <h3 className="headline-md">Order Summary</h3>

            {/* Restaurant Closed Warning */}
            {!restaurantStatus.loading && !restaurantStatus.isOpen && (
              <div className="alert alert-error" style={{ 
                padding: 'var(--space-sm)', 
                marginBottom: 'var(--space-md)', 
                borderRadius: 'var(--rounded-lg)',
                backgroundColor: 'var(--error-container)',
                color: 'var(--on-error-container)',
                display: 'flex',
                alignItems: 'flex-start',
                gap: 'var(--space-xs)',
                fontSize: '0.875rem'
              }}>
                <AlertCircle size={18} style={{ flexShrink: 0, marginTop: '2px' }} />
                <div>
                  <strong>Restaurant Closed</strong>
                  <p style={{ margin: 0, marginTop: '4px' }}>
                    {restaurantStatus.restaurantName} is currently closed. 
                    {restaurantStatus.nextOpenTime && restaurantStatus.nextOpenTime !== 'when partner reopens' && (
                      <span> Opens {restaurantStatus.nextOpenTime}.</span>
                    )}
                  </p>
                </div>
              </div>
            )}

            <div className="summary-items">
              <p className="body-sm text-muted">{checkoutContext.cart?.itemCount || 0} items</p>
            </div>

            <div className="summary-row">
              <span>Subtotal</span>
              <span>₹{checkoutContext.cart?.subtotal?.toFixed(2) || '0.00'}</span>
            </div>

            <div className="summary-row">
              <span>Delivery Fee</span>
              <span>₹{checkoutContext.deliveryFee?.toFixed(2) || '0.00'}</span>
            </div>

            <div className="summary-row">
              <span>GST ({checkoutContext.gstRate}%)</span>
              <span>₹{checkoutContext.gstAmount?.toFixed(2) || '0.00'}</span>
            </div>

            <div className="summary-row">
              <span>Platform Fee</span>
              <span>₹{checkoutContext.platformFee?.toFixed(2) || '15.00'}</span>
            </div>

            {checkoutContext.cart?.discount > 0 && (
              <div className="summary-row discount">
                <span>Discount</span>
                <span>-₹{checkoutContext.cart.discount?.toFixed(2)}</span>
              </div>
            )}

            <div className="summary-row total">
              <span className="headline-sm">Total Amount</span>
              <span className="headline-sm">₹{checkoutContext.totalAmount?.toFixed(2) || '0.00'}</span>
            </div>

            <button
              className="btn btn-primary btn-lg place-order-btn"
              onClick={handlePlaceOrder}
              disabled={placing || !selectedAddress || !restaurantStatus.isOpen || restaurantStatus.loading}
              title={!restaurantStatus.isOpen ? 'Restaurant is currently closed' : 'Place your order'}
            >
              {restaurantStatus.loading ? 'Checking...' : (
                !restaurantStatus.isOpen ? 'Restaurant Closed' : (
                  placing ? 'Placing Order...' : 'Place Order'
                )
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
