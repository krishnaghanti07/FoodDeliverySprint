import { useState, useEffect, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Search, MapPin, Clock, Star, ArrowRight, ChevronRight,
  Utensils, Truck, Shield, Zap, Users, Award, ChevronDown
} from 'lucide-react';
import api from '../services/api';
import { API_ENDPOINTS } from '../config/api';
import { RestaurantCardSkeleton, CuisineCardSkeleton } from '../components/common/Skeleton';
import { getRestaurantCardImage } from '../utils/cuisineImages';
import './HomePage.css';

// ── Animated counter hook ────────────────────────────────────────────
function useCountUp(target, duration = 1800, start = false) {
  const [count, setCount] = useState(0);
  useEffect(() => {
    if (!start) return;
    let startTime = null;
    const step = (timestamp) => {
      if (!startTime) startTime = timestamp;
      const progress = Math.min((timestamp - startTime) / duration, 1);
      // Ease-out cubic
      const eased = 1 - Math.pow(1 - progress, 3);
      setCount(Math.floor(eased * target));
      if (progress < 1) requestAnimationFrame(step);
    };
    requestAnimationFrame(step);
  }, [target, duration, start]);
  return count;
}

// ── Stats counter component ──────────────────────────────────────────
function StatCounter({ value, suffix, label, icon: Icon, delay = 0 }) {
  const ref = useRef(null);
  const [visible, setVisible] = useState(false);
  const count = useCountUp(value, 1600, visible);

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => { if (entry.isIntersecting) setVisible(true); },
      { threshold: 0.3 }
    );
    if (ref.current) observer.observe(ref.current);
    return () => observer.disconnect();
  }, []);

  return (
    <div className="stat-item" ref={ref} style={{ animationDelay: `${delay}ms` }}>
      <div className="stat-icon-wrap">
        <Icon size={22} />
      </div>
      <div className="stat-number">
        {count.toLocaleString()}{suffix}
      </div>
      <div className="stat-label">{label}</div>
    </div>
  );
}

// ── Restaurant Card ──────────────────────────────────────────────────
function RestaurantCard({ restaurant, promoted }) {
  const r = restaurant;
  return (
    <Link
      to={`/restaurants/${r.id}`}
      className={`restaurant-card card ${promoted ? 'promoted-card' : ''}`}
      id={`restaurant-${r.id}`}
    >
      <div className="rc-image-wrap">
        <div
          className="rc-image"
          style={{ backgroundImage: getRestaurantCardImage(r) }}
        >
          {/* Gradient overlay */}
          <div className="rc-image-overlay" />
        </div>

        {/* Badges */}
        <div className="rc-badges">
          {r.isPromoted && (
            <span className="rc-badge rc-badge-promoted">⭐ Promoted</span>
          )}
          {r.isNew && (
            <span className="rc-badge rc-badge-new">✨ New</span>
          )}
        </div>

        {/* Delivery time */}
        {r.deliveryTime && (
          <span className="rc-delivery-time">
            <Clock size={11} aria-hidden="true" /> {r.deliveryTime} min
          </span>
        )}

        {/* Favourite button */}
        <button
          className="rc-fav-btn"
          aria-label="Save restaurant"
          onClick={(e) => e.preventDefault()}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z" />
          </svg>
        </button>
      </div>

      <div className="card-body rc-info">
        <h3 className="rc-name">{r.name}</h3>
        <p className="rc-cuisine">{r.cuisineType || r.cuisine || 'Multi Cuisine'}</p>
        <div className="rc-meta">
          <span className="rc-rating">
            <Star size={13} fill="var(--gold)" color="var(--gold)" aria-hidden="true" />
            <span>{r.rating > 0 ? r.rating.toFixed(1) : 'New'}</span>
          </span>
          {r.averageCostForTwo && (
            <span className="rc-dot" aria-hidden="true" />
          )}
          {r.averageCostForTwo && (
            <span className="rc-cost">₹{r.averageCostForTwo} for two</span>
          )}
        </div>
      </div>
    </Link>
  );
}

// ── Main HomePage ────────────────────────────────────────────────────
export default function HomePage() {
  const navigate = useNavigate();
  const [homeData, setHomeData] = useState(null);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchFocused, setSearchFocused] = useState(false);

  useEffect(() => {
    const fetchHome = async () => {
      try {
        const res = await api.get(API_ENDPOINTS.catalog.home);
        const data = res.data?.data || res.data;
        setHomeData(data);
        setCategories(data?.popularCuisines?.slice(0, 8) || []);
      } catch (err) {
        console.error('Failed to fetch home data:', err);
      } finally {
        setLoading(false);
      }
    };
    fetchHome();
  }, []);

  const promotedRestaurants = homeData?.promotedRestaurants || homeData?.featured || [];
  const nearbyRestaurants = homeData?.nearbyRestaurants || homeData?.restaurants || [];

  const cuisineEmojis = ['🍕', '🍔', '🍣', '🌮', '🍜', '🥗', '🍱', '🧁', '🥘', '🍝'];
  const fallbackCuisines = ['Indian', 'Chinese', 'Italian', 'Mexican', 'Japanese', 'Thai', 'American', 'Healthy'];

  const handleSearch = (e) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      navigate(`/restaurants?search=${encodeURIComponent(searchQuery.trim())}`);
    } else {
      navigate('/restaurants');
    }
  };

  const quickTags = ['Butter Chicken', 'Sushi', 'Vegan', 'Pizza', 'Biryani'];

  return (
    <div className="home-page page-enter">

      {/* ══════════════════════════════════════════════
          HERO SECTION
      ══════════════════════════════════════════════ */}
      <section className="hero-section" aria-label="Hero">
        {/* Animated background blobs */}
        <div className="hero-bg" aria-hidden="true">
          <div className="hero-blob hero-blob-1" />
          <div className="hero-blob hero-blob-2" />
          <div className="hero-blob hero-blob-3" />
          <div className="hero-grid-overlay" />
        </div>

        <div className="container hero-content">
          {/* Left — text + search */}
          <div className="hero-text">
            <div className="hero-badge-wrap">
              <span className="hero-badge">
                <span className="hero-badge-dot" aria-hidden="true" />
                #1 Food Delivery Platform
              </span>
            </div>

            <h1 className="hero-title">
              What are you{' '}
              <span className="hero-title-highlight">
                <span className="hero-title-word">craving</span>
                <span className="hero-title-underline" aria-hidden="true" />
              </span>
              {' '}today?
            </h1>

            <p className="hero-subtitle">
              Discover delicious meals from top restaurants near you.
              Fast delivery, amazing taste — every single time.
            </p>

            {/* Search bar */}
            <form
              className={`hero-search-bar ${searchFocused ? 'focused' : ''}`}
              onSubmit={handleSearch}
              role="search"
            >
              <div className="hsb-location">
                <MapPin size={18} aria-hidden="true" />
                <span>Nearby</span>
              </div>
              <div className="hsb-divider" aria-hidden="true" />
              <div className="hsb-input-wrap">
                <Search size={17} className="hsb-search-icon" aria-hidden="true" />
                <input
                  type="search"
                  placeholder="Search restaurants, cuisines, dishes..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onFocus={() => setSearchFocused(true)}
                  onBlur={() => setSearchFocused(false)}
                  className="hsb-input"
                  id="hero-search-input"
                  aria-label="Search for food"
                  autoComplete="off"
                />
              </div>
              <button type="submit" className="btn btn-primary hsb-btn">
                <Search size={17} aria-hidden="true" />
                <span>Find Food</span>
              </button>
            </form>

            {/* Quick tags */}
            <div className="hero-tags" role="list" aria-label="Popular searches">
              <span className="hero-tags-label">Trending:</span>
              {quickTags.map((tag) => (
                <Link
                  key={tag}
                  to={`/restaurants?search=${encodeURIComponent(tag)}`}
                  className="hero-tag"
                  role="listitem"
                >
                  {tag}
                </Link>
              ))}
            </div>
          </div>

          {/* Right — visual */}
          <div className="hero-visual" aria-hidden="true">
            {/* Central food image */}
            <div className="hero-food-ring">
              <div className="hero-food-img" />
              <div className="hero-food-ring-glow" />
            </div>

            {/* Floating info cards */}
            <div className="hero-float-card hero-float-1">
              <span className="hfc-emoji">🚀</span>
              <div className="hfc-text">
                <span className="hfc-value">30 min</span>
                <span className="hfc-label">Avg delivery</span>
              </div>
            </div>
            <div className="hero-float-card hero-float-2">
              <span className="hfc-emoji">⭐</span>
              <div className="hfc-text">
                <span className="hfc-value">4.8 / 5</span>
                <span className="hfc-label">Avg rating</span>
              </div>
            </div>
            <div className="hero-float-card hero-float-3">
              <span className="hfc-emoji">🏪</span>
              <div className="hfc-text">
                <span className="hfc-value">500+</span>
                <span className="hfc-label">Restaurants</span>
              </div>
            </div>

            {/* Order notification card */}
            <div className="hero-notif-card">
              <div className="hnc-avatar">🧑‍🍳</div>
              <div className="hnc-text">
                <span className="hnc-title">Order confirmed!</span>
                <span className="hnc-sub">Your food is being prepared</span>
              </div>
              <div className="hnc-pulse" />
            </div>
          </div>
        </div>

        {/* Scroll hint */}
        <div className="hero-scroll-hint" aria-hidden="true">
          <ChevronDown size={20} />
        </div>
      </section>

      {/* ══════════════════════════════════════════════
          SOCIAL PROOF BAR
      ══════════════════════════════════════════════ */}
      <section className="stats-bar" aria-label="Platform statistics">
        <div className="container stats-bar-inner">
          <StatCounter value={500} suffix="+" label="Restaurants" icon={Utensils} delay={0} />
          <div className="stats-divider" aria-hidden="true" />
          <StatCounter value={50000} suffix="+" label="Happy Customers" icon={Users} delay={100} />
          <div className="stats-divider" aria-hidden="true" />
          <StatCounter value={30} suffix=" min" label="Avg Delivery" icon={Zap} delay={200} />
          <div className="stats-divider" aria-hidden="true" />
          <StatCounter value={4.8} suffix="★" label="Avg Rating" icon={Award} delay={300} />
        </div>
      </section>

      {/* ══════════════════════════════════════════════
          POPULAR CUISINES
      ══════════════════════════════════════════════ */}
      <section className="section cuisines-section" aria-label="Popular cuisines">
        <div className="container">
          <div className="section-header">
            <div>
              <h2 className="headline-lg">Popular Cuisines</h2>
              <p className="body-sm text-muted" style={{ marginTop: '0.25rem' }}>
                Explore by what you're in the mood for
              </p>
            </div>
            <Link to="/restaurants" className="section-link-btn">
              View All <ChevronRight size={15} aria-hidden="true" />
            </Link>
          </div>

          <div className="cuisines-grid">
            {(loading ? Array.from({ length: 8 }) : (categories.length > 0 ? categories : Array.from({ length: 8 }))).map((cat, i) => (
              loading || !cat ? (
                <CuisineCardSkeleton key={i} />
              ) : (
                <Link
                  key={cat?.id || i}
                  to={`/restaurants?category=${encodeURIComponent(cat?.name || '')}`}
                  className="cuisine-card"
                  style={{ '--delay': `${i * 40}ms` }}
                >
                  <div className="cuisine-icon-wrap">
                    <span className="cuisine-emoji" aria-hidden="true">
                      {cuisineEmojis[i % cuisineEmojis.length]}
                    </span>
                  </div>
                  <span className="cuisine-name">
                    {cat?.name || fallbackCuisines[i]}
                  </span>
                </Link>
              )
            ))}
          </div>
        </div>
      </section>

      {/* ══════════════════════════════════════════════
          PROMOTED RESTAURANTS
      ══════════════════════════════════════════════ */}
      {(promotedRestaurants.length > 0 || loading) && (
        <section className="section promoted-section" aria-label="Promoted restaurants">
          <div className="container">
            <div className="section-header">
              <div>
                <h2 className="headline-lg">Featured Picks</h2>
                <p className="body-sm text-muted" style={{ marginTop: '0.25rem' }}>
                  Top-rated restaurants handpicked for you
                </p>
              </div>
              <Link to="/restaurants" className="section-link-btn">
                See All <ChevronRight size={15} aria-hidden="true" />
              </Link>
            </div>
            <div className="restaurants-grid promoted">
              {loading
                ? Array.from({ length: 3 }).map((_, i) => (
                    <RestaurantCardSkeleton key={i} promoted />
                  ))
                : promotedRestaurants.slice(0, 3).map((r) => (
                    <RestaurantCard key={r.id} restaurant={r} promoted />
                  ))}
            </div>
          </div>
        </section>
      )}

      {/* ══════════════════════════════════════════════
          NEARBY RESTAURANTS
      ══════════════════════════════════════════════ */}
      <section className="section nearby-section" aria-label="Nearby restaurants">
        <div className="container">
          <div className="section-header">
            <div>
              <h2 className="headline-lg">Restaurants Nearby</h2>
              <p className="body-sm text-muted" style={{ marginTop: '0.25rem' }}>
                Delivering to your area right now
              </p>
            </div>
            <Link to="/restaurants" className="section-link-btn">
              View All <ChevronRight size={15} aria-hidden="true" />
            </Link>
          </div>
          <div className="restaurants-grid">
            {loading
              ? Array.from({ length: 8 }).map((_, i) => (
                  <RestaurantCardSkeleton key={i} />
                ))
              : nearbyRestaurants.slice(0, 8).map((r) => (
                  <RestaurantCard key={r.id} restaurant={r} />
                ))}
          </div>
        </div>
      </section>

      {/* ══════════════════════════════════════════════
          HOW IT WORKS
      ══════════════════════════════════════════════ */}
      <section className="section how-section" aria-label="How it works">
        <div className="container">
          <div className="how-header">
            <span className="how-eyebrow">Simple & Fast</span>
            <h2 className="headline-lg">How FoodRush Works</h2>
            <p className="body-lg text-muted how-subtitle">
              From craving to doorstep in three easy steps
            </p>
          </div>
          <div className="how-steps">
            {[
              {
                step: '01',
                emoji: '📍',
                title: 'Choose Your Location',
                desc: 'Enter your delivery address and discover restaurants near you.',
                color: 'primary',
              },
              {
                step: '02',
                emoji: '🍽️',
                title: 'Pick Your Meal',
                desc: 'Browse menus, read reviews, and add your favourites to cart.',
                color: 'secondary',
              },
              {
                step: '03',
                emoji: '🚀',
                title: 'Fast Delivery',
                desc: 'Track your order in real-time as it races to your door.',
                color: 'tertiary',
              },
            ].map(({ step, emoji, title, desc, color }, i) => (
              <div key={step} className={`how-step how-step-${color}`} style={{ '--step-delay': `${i * 120}ms` }}>
                <div className="how-step-number">{step}</div>
                <div className="how-step-icon">{emoji}</div>
                <h3 className="how-step-title">{title}</h3>
                <p className="how-step-desc">{desc}</p>
                {i < 2 && <div className="how-connector" aria-hidden="true" />}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ══════════════════════════════════════════════
          WHY FOODRUSH
      ══════════════════════════════════════════════ */}
      <section className="section features-section" aria-label="Why choose FoodRush">
        <div className="container">
          <div className="features-header">
            <span className="how-eyebrow">Our Promise</span>
            <h2 className="headline-lg">Why Choose FoodRush?</h2>
          </div>
          <div className="features-grid">
            {[
              {
                icon: Truck,
                color: 'primary',
                title: 'Lightning Fast',
                desc: 'Average delivery in under 30 minutes. Track every step in real-time with live GPS.',
                stat: '< 30 min',
              },
              {
                icon: Utensils,
                color: 'secondary',
                title: '500+ Restaurants',
                desc: 'Curated selection of the finest restaurants and cuisines across your city.',
                stat: '500+',
              },
              {
                icon: Shield,
                color: 'tertiary',
                title: 'Safe & Secure',
                desc: 'Secure payments, hygienic packaging, and quality-checked meals every time.',
                stat: '100%',
              },
            ].map(({ icon: Icon, color, title, desc, stat }) => (
              <div key={title} className={`feature-card feature-card-${color}`}>
                <div className={`feature-icon feature-icon-${color}`}>
                  <Icon size={26} aria-hidden="true" />
                </div>
                <div className="feature-stat">{stat}</div>
                <h3 className="feature-title">{title}</h3>
                <p className="feature-desc">{desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ══════════════════════════════════════════════
          PARTNER CTA
      ══════════════════════════════════════════════ */}
      <section className="cta-section" aria-label="Partner with us">
        <div className="container">
          <div className="cta-card">
            {/* Background decoration */}
            <div className="cta-bg-deco" aria-hidden="true">
              <div className="cta-deco-circle cta-deco-1" />
              <div className="cta-deco-circle cta-deco-2" />
            </div>

            <div className="cta-content">
              <span className="cta-eyebrow">For Restaurant Owners</span>
              <h2 className="cta-title">Grow Your Business with FoodRush</h2>
              <p className="cta-subtitle">
                Join 500+ restaurants already earning more with India's fastest delivery platform.
                Zero setup fees, instant onboarding.
              </p>
              <div className="cta-actions">
                <Link to="/register?role=Partner" className="btn btn-primary btn-lg cta-btn-primary">
                  Start for Free <ArrowRight size={18} aria-hidden="true" />
                </Link>
                <Link to="/help" className="btn cta-btn-ghost">
                  Learn More
                </Link>
              </div>
              <div className="cta-trust">
                <span>✓ Free to join</span>
                <span>✓ No hidden fees</span>
                <span>✓ 24/7 support</span>
              </div>
            </div>

            <div className="cta-visual" aria-hidden="true">
              <div className="cta-img" />
              <div className="cta-img-badge">
                <span className="cta-img-badge-value">₹2L+</span>
                <span className="cta-img-badge-label">avg monthly revenue</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ══════════════════════════════════════════════
          MOBILE STICKY CTA
      ══════════════════════════════════════════════ */}
      <div className="mobile-sticky-cta" aria-label="Order food now">
        <Link to="/restaurants" className="btn btn-primary mobile-sticky-btn">
          <Utensils size={18} aria-hidden="true" />
          Order Food Now
        </Link>
      </div>

    </div>
  );
}
