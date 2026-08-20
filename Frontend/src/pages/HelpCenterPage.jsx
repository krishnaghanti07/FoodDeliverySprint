import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Search, ChevronDown, ChevronUp, ShoppingBag, Truck, CreditCard,
  Store, User, Star, MessageCircle, Phone, Mail, ArrowRight
} from 'lucide-react';
import './HelpCenterPage.css';

// ── FAQ data organised by category ───────────────────────────────────
const FAQ_CATEGORIES = [
  {
    id: 'ordering',
    icon: <ShoppingBag size={22} />,
    label: 'Ordering',
    color: 'var(--primary)',
    bg: 'var(--primary-fixed)',
    faqs: [
      {
        q: 'How do I place an order on FoodRush?',
        a: 'Browse restaurants on the Restaurants page, pick your items, add them to your cart, then head to Checkout. Choose a delivery address, select a payment method (Cash on Delivery, Online/Card, or Wallet), and tap "Place Order".'
      },
      {
        q: 'Can I order from multiple restaurants at once?',
        a: 'Each order is tied to a single restaurant. If you add items from a different restaurant, you\'ll be prompted to clear your current cart first. This ensures accurate preparation and delivery times.'
      },
      {
        q: 'How do I apply a coupon or discount code?',
        a: 'On the Cart page, enter your coupon code in the "Apply Coupon" field and tap Apply. Valid coupons will instantly reduce your order total. Only one coupon can be applied per order.'
      },
      {
        q: 'Can I schedule an order for later?',
        a: 'Currently FoodRush supports immediate orders only. Scheduled delivery is on our roadmap — stay tuned for updates!'
      },
      {
        q: 'What is the minimum order amount?',
        a: 'Minimum order amounts vary by restaurant. You\'ll see the minimum displayed on the restaurant\'s page and in your cart if your order falls below it.'
      },
    ]
  },
  {
    id: 'delivery',
    icon: <Truck size={22} />,
    label: 'Delivery',
    color: 'var(--secondary)',
    bg: 'var(--secondary-fixed)',
    faqs: [
      {
        q: 'How long does delivery take?',
        a: 'Average delivery time is 30–45 minutes depending on the restaurant\'s prep time and your distance. You can see the estimated prep time on each restaurant\'s page.'
      },
      {
        q: 'How do I track my order?',
        a: 'Go to My Orders and tap on your active order. You\'ll see real-time status updates: Accepted → Preparing → Ready for Pickup → Out for Delivery → Delivered.'
      },
      {
        q: 'What is the delivery fee?',
        a: 'Delivery fees are set by each restaurant and shown clearly before checkout. FoodRush also charges a fixed platform fee of ₹15 per order to maintain the platform.'
      },
      {
        q: 'What if my delivery agent is late?',
        a: 'If your order is significantly delayed, you can contact our support team via the Contact Us page. We\'ll investigate and, where applicable, offer compensation.'
      },
      {
        q: 'Can I change my delivery address after placing an order?',
        a: 'Delivery addresses cannot be changed after an order is placed. Please double-check your address at checkout. You can manage saved addresses in your Profile.'
      },
    ]
  },
  {
    id: 'payments',
    icon: <CreditCard size={22} />,
    label: 'Payments & Refunds',
    color: 'var(--tertiary)',
    bg: '#e0f7fa',
    faqs: [
      {
        q: 'What payment methods does FoodRush accept?',
        a: 'We accept Cash on Delivery (COD), Online payments via Razorpay (Credit/Debit cards, UPI, Net Banking), and FoodRush Wallet balance.'
      },
      {
        q: 'How does the FoodRush Wallet work?',
        a: 'Your Wallet is credited when refunds are processed. You can use your Wallet balance to pay for future orders. Check your balance anytime in Profile → Wallet.'
      },
      {
        q: 'How do I get a refund?',
        a: 'If you cancel a paid order (Card/Wallet), a refund request is automatically created. Our admin team reviews it and credits the refund amount to your FoodRush Wallet after deducting the platform fee (₹15) and a 5% cancellation charge.'
      },
      {
        q: 'How long does a refund take?',
        a: 'Wallet refunds are processed within 24–48 hours after admin approval. You\'ll see the credit in your Wallet balance once processed.'
      },
      {
        q: 'Is my payment information secure?',
        a: 'Yes. All online payments are processed through Razorpay, a PCI-DSS compliant payment gateway. FoodRush never stores your card details.'
      },
    ]
  },
  {
    id: 'account',
    icon: <User size={22} />,
    label: 'Account & Profile',
    color: '#7b1fa2',
    bg: '#f3e5f5',
    faqs: [
      {
        q: 'How do I create a FoodRush account?',
        a: 'Click "Sign Up" in the top navigation. Choose your role (Customer, Partner, or Delivery Agent), fill in your details, and you\'re ready to go!'
      },
      {
        q: 'How do I reset my password?',
        a: 'On the Login page, click "Forgot Password?". Enter your registered email and we\'ll send a 6-digit OTP. Use it on the Reset Password page to set a new password.'
      },
      {
        q: 'How do I update my profile or profile picture?',
        a: 'Go to Profile (click your name/avatar in the navbar). Click "Edit Profile" to update your name, mobile number, or upload a new profile photo.'
      },
      {
        q: 'How do I add or manage delivery addresses?',
        a: 'In your Profile page, scroll to "Saved Addresses". Click "Add Address" to add a new one, or use "Set as Default" / "Delete" to manage existing ones. Address management is available for Customer accounts only.'
      },
      {
        q: 'Can I delete my account?',
        a: 'To request account deletion, please contact our support team via the Contact Us page. We\'ll process your request within 7 business days.'
      },
    ]
  },
  {
    id: 'partners',
    icon: <Store size={22} />,
    label: 'Restaurant Partners',
    color: '#e65100',
    bg: '#fff3e0',
    faqs: [
      {
        q: 'How do I register my restaurant on FoodRush?',
        a: 'Sign up with the "Partner" role, then go to Partner Dashboard → My Restaurant → Register Restaurant. Fill in your restaurant details and submit for admin approval.'
      },
      {
        q: 'How long does restaurant approval take?',
        a: 'Our admin team reviews new restaurant applications within 1–3 business days. You\'ll be notified once your restaurant is approved and live on the platform.'
      },
      {
        q: 'How do I manage my menu?',
        a: 'From your Partner Dashboard, go to Menu Management. You can add categories, add/edit/delete menu items, set prices, upload photos, and toggle item availability.'
      },
      {
        q: 'How do I receive and manage orders?',
        a: 'New orders appear in your Orders Management page. You can Accept or Reject incoming orders, then update status through Preparing → Ready for Pickup as you fulfil them.'
      },
      {
        q: 'What commission does FoodRush charge?',
        a: 'FoodRush charges a 15% commission on the subtotal of each delivered order. This covers platform maintenance, payment processing, and delivery agent coordination.'
      },
    ]
  },
  {
    id: 'ratings',
    icon: <Star size={22} />,
    label: 'Ratings & Reviews',
    color: '#f57f17',
    bg: '#fffde7',
    faqs: [
      {
        q: 'How do I rate my order?',
        a: 'After your order is delivered, go to My Orders and open the completed order. You\'ll see a "Rate Order" option where you can give a food rating and delivery rating (1–5 stars) along with a comment.'
      },
      {
        q: 'Who can leave ratings?',
        a: 'Only Customers who have received a delivered order can leave ratings. Partners can view ratings but cannot submit them.'
      },
      {
        q: 'How is the restaurant rating calculated?',
        a: 'A restaurant\'s rating is the average of all order ratings, where each order rating = (food rating + delivery rating) / 2. Ratings update in real-time as new reviews come in.'
      },
      {
        q: 'Can I edit or delete my rating?',
        a: 'Currently ratings cannot be edited after submission. If you believe a rating was submitted in error, please contact our support team.'
      },
    ]
  },
];

// ── Single FAQ accordion item ─────────────────────────────────────────
function FaqItem({ faq }) {
  const [open, setOpen] = useState(false);
  return (
    <div className={`hc-faq-item ${open ? 'open' : ''}`}>
      <button className="hc-faq-q" onClick={() => setOpen(v => !v)}>
        <span>{faq.q}</span>
        {open ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
      </button>
      {open && <div className="hc-faq-a">{faq.a}</div>}
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────
export default function HelpCenterPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [activeCategory, setActiveCategory] = useState('all');

  // Filter FAQs by search query and active category
  const filteredCategories = FAQ_CATEGORIES
    .filter(cat => activeCategory === 'all' || cat.id === activeCategory)
    .map(cat => ({
      ...cat,
      faqs: cat.faqs.filter(faq =>
        !searchQuery ||
        faq.q.toLowerCase().includes(searchQuery.toLowerCase()) ||
        faq.a.toLowerCase().includes(searchQuery.toLowerCase())
      )
    }))
    .filter(cat => cat.faqs.length > 0);

  const totalResults = filteredCategories.reduce((sum, c) => sum + c.faqs.length, 0);

  return (
    <div className="hc-page page-enter">

      {/* ── Hero ── */}
      <section className="hc-hero">
        <div className="hc-hero-shapes">
          <div className="hc-shape hc-shape-1" />
          <div className="hc-shape hc-shape-2" />
        </div>
        <div className="container hc-hero-content">
          <span className="hc-hero-badge">🛟 Help Center</span>
          <h1 className="display-lg hc-hero-title">
            How can we <span className="text-gradient">help you?</span>
          </h1>
          <p className="body-lg hc-hero-sub">
            Find answers to common questions about ordering, delivery, payments, and more.
          </p>
          <div className="hc-search-wrap">
            <Search size={20} className="hc-search-icon" />
            <input
              type="text"
              className="hc-search-input"
              placeholder="Search for answers… e.g. 'refund', 'cancel order'"
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
            />
            {searchQuery && (
              <button className="hc-search-clear" onClick={() => setSearchQuery('')}>✕</button>
            )}
          </div>
        </div>
      </section>

      <div className="container hc-body">

        {/* ── Category pills ── */}
        <div className="hc-cats">
          <button
            className={`hc-cat-pill ${activeCategory === 'all' ? 'active' : ''}`}
            onClick={() => setActiveCategory('all')}
          >
            All Topics
          </button>
          {FAQ_CATEGORIES.map(cat => (
            <button
              key={cat.id}
              className={`hc-cat-pill ${activeCategory === cat.id ? 'active' : ''}`}
              onClick={() => setActiveCategory(cat.id)}
              style={activeCategory === cat.id ? { background: cat.bg, color: cat.color, borderColor: cat.color } : {}}
            >
              {cat.icon} {cat.label}
            </button>
          ))}
        </div>

        {/* ── Search results count ── */}
        {searchQuery && (
          <p className="hc-results-count">
            {totalResults > 0
              ? `${totalResults} result${totalResults !== 1 ? 's' : ''} for "${searchQuery}"`
              : `No results for "${searchQuery}"`}
          </p>
        )}

        {/* ── FAQ sections ── */}
        {filteredCategories.length === 0 ? (
          <div className="hc-empty">
            <span style={{ fontSize: 56 }}>🔍</span>
            <h3 className="headline-md">No answers found</h3>
            <p className="body-md text-muted">Try different keywords or browse all topics above.</p>
            <Link to="/contact" className="btn btn-primary" style={{ marginTop: '1rem' }}>
              <MessageCircle size={18} /> Contact Support
            </Link>
          </div>
        ) : (
          <div className="hc-sections">
            {filteredCategories.map(cat => (
              <section key={cat.id} className="hc-section">
                <div className="hc-section-header" style={{ '--cat-color': cat.color, '--cat-bg': cat.bg }}>
                  <div className="hc-section-icon">{cat.icon}</div>
                  <h2 className="headline-md">{cat.label}</h2>
                </div>
                <div className="hc-faq-list">
                  {cat.faqs.map((faq, i) => (
                    <FaqItem key={i} faq={faq} />
                  ))}
                </div>
              </section>
            ))}
          </div>
        )}

        {/* ── Still need help? ── */}
        <div className="hc-cta-strip">
          <div className="hc-cta-text">
            <h3 className="headline-md">Still need help?</h3>
            <p className="body-md text-muted">Our support team is available 9 AM – 9 PM, 7 days a week.</p>
          </div>
          <div className="hc-cta-actions">
            <Link to="/contact" className="btn btn-primary">
              <MessageCircle size={18} /> Contact Us
            </Link>
            <a href="mailto:support@foodrush.com" className="btn btn-outline">
              <Mail size={18} /> Email Support
            </a>
          </div>
        </div>

        {/* ── Quick links ── */}
        <div className="hc-quick-links">
          <h3 className="headline-sm" style={{ marginBottom: '1rem', color: 'var(--on-surface-variant)' }}>
            Popular Topics
          </h3>
          <div className="hc-quick-grid">
            {[
              { icon: '🔄', label: 'Cancel an Order', cat: 'ordering' },
              { icon: '💸', label: 'Request a Refund', cat: 'payments' },
              { icon: '📍', label: 'Track My Order', cat: 'delivery' },
              { icon: '🔑', label: 'Reset Password', cat: 'account' },
              { icon: '🏪', label: 'Register Restaurant', cat: 'partners' },
              { icon: '⭐', label: 'Leave a Rating', cat: 'ratings' },
            ].map(link => (
              <button
                key={link.label}
                className="hc-quick-card"
                onClick={() => { setActiveCategory(link.cat); setSearchQuery(''); window.scrollTo({ top: 300, behavior: 'smooth' }); }}
              >
                <span className="hc-quick-icon">{link.icon}</span>
                <span className="hc-quick-label">{link.label}</span>
                <ArrowRight size={14} className="hc-quick-arrow" />
              </button>
            ))}
          </div>
        </div>

      </div>
    </div>
  );
}
