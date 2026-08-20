import { useState, useEffect, useMemo, useCallback } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  Search, Star, Clock, X, SlidersHorizontal, ChevronDown,
  Heart, MapPin, Zap, TrendingUp, Filter
} from 'lucide-react';
import api from '../services/api';
import { API_ENDPOINTS } from '../config/api';
import { isRestaurantOpen } from '../utils/timeUtils';
import { RestaurantCardSkeleton } from '../components/common/Skeleton';
import { getRestaurantCardImage } from '../utils/cuisineImages';
import './RestaurantsPage.css';

const RATING_OPTIONS = [
  { label: 'All Ratings', value: 0 },
  { label: '4.5+', value: 4.5 },
  { label: '4.0+', value: 4.0 },
  { label: '3.5+', value: 3.5 },
];

const SORT_OPTIONS = [
  { label: 'Relevance', value: 'relevance' },
  { label: 'Rating', value: 'rating' },
  { label: 'Delivery Time', value: 'delivery' },
  { label: 'Cost: Low to High', value: 'cost_asc' },
  { label: 'Cost: High to Low', value: 'cost_desc' },
];

// ── Individual Restaurant Card ───────────────────────────────────────
function RestaurantCard({ restaurant, isOpen, nextOpenTime }) {
  const r = restaurant;
  const [faved, setFaved] = useState(false);

  const handleFav = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setFaved(v => !v);
  };

  const isNew = !r.rating || r.rating === 0;
  const isTrending = r.isPromoted || (r.rating >= 4.5);

  return (
    <Link
      to={`/restaurants/${r.id}`}
      className={`rlc-card ${!isOpen ? 'rlc-card-closed' : ''}`}
      id={`rest-${r.id}`}
      aria-label={`${r.name}, ${isOpen ? 'Open' : 'Closed'}`}
    >
      {/* ── Image ── */}
      <div className="rlc-image-wrap">
        <div
          className="rlc-image"
          style={{ backgroundImage: getRestaurantCardImage(r) }}
          role="img"
          aria-label={r.name}
        />

        {/* Gradient overlay */}
        <div className="rlc-overlay" />

        {/* Closed dim */}
        {!isOpen && <div className="rlc-closed-dim" />}

        {/* Top-left ribbons */}
        <div className="rlc-ribbons">
          {isTrending && !isNew && (
            <span className="rlc-ribbon rlc-ribbon-trending">
              <TrendingUp size={10} /> Trending
            </span>
          )}
          {isNew && (
            <span className="rlc-ribbon rlc-ribbon-new">
              ✨ New
            </span>
          )}
        </div>

        {/* Open/Closed badge */}
        <span className={`rlc-status-badge ${isOpen ? 'open' : 'closed'}`}>
          <span className="rlc-status-dot" />
          {isOpen ? 'Open' : 'Closed'}
        </span>

        {/* Delivery time — bottom right */}
        {r.deliveryTime && (
          <span className="rlc-delivery-badge">
            <Zap size={11} /> {r.deliveryTime} min
          </span>
        )}

        {/* Favourite button */}
        <button
          className={`rlc-fav-btn ${faved ? 'faved' : ''}`}
          onClick={handleFav}
          aria-label={faved ? 'Remove from favourites' : 'Add to favourites'}
          aria-pressed={faved}
        >
          <Heart size={15} fill={faved ? 'currentColor' : 'none'} />
        </button>
      </div>

      {/* ── Info ── */}
      <div className="rlc-info">
        <div className="rlc-name-row">
          <h3 className="rlc-name">{r.name}</h3>
        </div>

        <p className="rlc-cuisine">
          {r.cuisineType || r.cuisine || 'Multi Cuisine'}
        </p>

        {/* Opens when info */}
        {!isOpen && nextOpenTime && nextOpenTime !== 'when partner reopens' && (
          <p className="rlc-opens-at">
            <Clock size={11} /> Opens {nextOpenTime}
          </p>
        )}

        {/* Meta row */}
        <div className="rlc-meta">
          <span className="rlc-rating">
            <Star size={13} fill="var(--gold)" color="var(--gold)" />
            <span>{r.rating > 0 ? r.rating.toFixed(1) : 'New'}</span>
          </span>

          {r.averageCostForTwo && (
            <>
              <span className="rlc-meta-dot" />
              <span className="rlc-cost">₹{r.averageCostForTwo} for two</span>
            </>
          )}

          {r.deliveryFee !== undefined && (
            <>
              <span className="rlc-meta-dot" />
              <span className="rlc-fee">
                {r.deliveryFee === 0 ? (
                  <span className="rlc-free-delivery">Free delivery</span>
                ) : (
                  `₹${r.deliveryFee} delivery`
                )}
              </span>
            </>
          )}
        </div>
      </div>
    </Link>
  );
}

// ── Main Page ────────────────────────────────────────────────────────
export default function RestaurantsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [restaurants, setRestaurants] = useState([]);
  const [restaurantHours, setRestaurantHours] = useState({});
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState(searchParams.get('search') || '');

  // Filter state
  const [filterOpen, setFilterOpen] = useState('all');
  const [filterRating, setFilterRating] = useState(0);
  const [sortBy, setSortBy] = useState('relevance');
  const [filterDrawerOpen, setFilterDrawerOpen] = useState(false);

  useEffect(() => {
    fetchRestaurants();
  }, [searchParams]);

  // Close filter drawer on resize to desktop
  useEffect(() => {
    const onResize = () => {
      if (window.innerWidth > 768) setFilterDrawerOpen(false);
    };
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  // Lock body scroll when filter drawer is open on mobile
  useEffect(() => {
    document.body.style.overflow = filterDrawerOpen ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [filterDrawerOpen]);

  const fetchRestaurants = async () => {
    setLoading(true);
    try {
      const search = searchParams.get('search');
      let res;
      if (search) {
        res = await api.get(API_ENDPOINTS.catalog.restaurantSearch, {
          params: { query: search, searchTerm: search },
        });
      } else {
        res = await api.get(API_ENDPOINTS.catalog.restaurants);
      }
      const restaurantData = res.data?.data || res.data;
      const restaurantList = Array.isArray(restaurantData)
        ? restaurantData
        : restaurantData?.restaurants || restaurantData?.items || [];
      setRestaurants(restaurantList);

      // Fetch operating hours in parallel
      const hoursResults = await Promise.all(
        restaurantList.map(async (r) => {
          try {
            const hoursRes = await api.get(
              `${API_ENDPOINTS.catalog.operatingHours}?restaurantId=${r.id}`
            );
            const hours = hoursRes.data?.data || hoursRes.data || [];
            return { id: r.id, hours: Array.isArray(hours) ? hours : [] };
          } catch {
            return { id: r.id, hours: [] };
          }
        })
      );
      const hoursMap = {};
      hoursResults.forEach(({ id, hours }) => { hoursMap[id] = hours; });
      setRestaurantHours(hoursMap);
    } catch (err) {
      console.error('Failed to fetch restaurants:', err);
      setRestaurants([]);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      setSearchParams({ search: searchQuery.trim() });
    } else {
      setSearchParams({});
    }
  };

  const clearSearch = useCallback(() => {
    setSearchQuery('');
    setSearchParams({});
  }, [setSearchParams]);

  const clearFilters = useCallback(() => {
    setFilterOpen('all');
    setFilterRating(0);
    setSortBy('relevance');
  }, []);

  const hasActiveFilters = filterOpen !== 'all' || filterRating > 0 || sortBy !== 'relevance';
  const activeFilterCount = (filterOpen !== 'all' ? 1 : 0) + (filterRating > 0 ? 1 : 0) + (sortBy !== 'relevance' ? 1 : 0);

  // Apply filters + sort client-side
  const filteredRestaurants = useMemo(() => {
    let list = restaurants.filter((r) => {
      const hours = restaurantHours[r.id] || [];
      const { isOpen } = isRestaurantOpen(hours, r.isOpen);
      if (filterOpen === 'open' && !isOpen) return false;
      if (filterOpen === 'closed' && isOpen) return false;
      if (filterRating > 0 && (r.rating || 0) < filterRating) return false;
      return true;
    });

    // Sort
    if (sortBy === 'rating') {
      list = [...list].sort((a, b) => (b.rating || 0) - (a.rating || 0));
    } else if (sortBy === 'delivery') {
      list = [...list].sort((a, b) => (a.deliveryTime || 99) - (b.deliveryTime || 99));
    } else if (sortBy === 'cost_asc') {
      list = [...list].sort((a, b) => (a.averageCostForTwo || 0) - (b.averageCostForTwo || 0));
    } else if (sortBy === 'cost_desc') {
      list = [...list].sort((a, b) => (b.averageCostForTwo || 0) - (a.averageCostForTwo || 0));
    }

    return list;
  }, [restaurants, restaurantHours, filterOpen, filterRating, sortBy]);

  const searchTerm = searchParams.get('search');

  // ── Filter Panel (shared between desktop inline + mobile drawer) ──
  const FilterPanel = () => (
    <div className="rp-filter-panel-content">
      {/* Status */}
      <div className="rp-filter-group">
        <span className="rp-filter-label">Status</span>
        <div className="rp-filter-options">
          {[
            { label: 'All', value: 'all' },
            { label: '● Open Now', value: 'open' },
            { label: '● Closed', value: 'closed' },
          ].map(opt => (
            <button
              key={opt.value}
              className={`rp-filter-opt ${filterOpen === opt.value ? 'active' : ''}`}
              onClick={() => setFilterOpen(opt.value)}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      {/* Rating */}
      <div className="rp-filter-group">
        <span className="rp-filter-label">Min Rating</span>
        <div className="rp-filter-options">
          {RATING_OPTIONS.map(opt => (
            <button
              key={opt.value}
              className={`rp-filter-opt ${filterRating === opt.value ? 'active' : ''}`}
              onClick={() => setFilterRating(opt.value)}
            >
              {opt.value === 0 ? 'Any' : (
                <><Star size={11} fill="var(--gold)" color="var(--gold)" /> {opt.label}</>
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Sort */}
      <div className="rp-filter-group">
        <span className="rp-filter-label">Sort By</span>
        <div className="rp-filter-options">
          {SORT_OPTIONS.map(opt => (
            <button
              key={opt.value}
              className={`rp-filter-opt ${sortBy === opt.value ? 'active' : ''}`}
              onClick={() => setSortBy(opt.value)}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      {hasActiveFilters && (
        <button className="btn btn-ghost btn-sm rp-clear-all" onClick={clearFilters}>
          <X size={14} /> Clear All Filters
        </button>
      )}
    </div>
  );

  return (
    <div className="restaurants-page page-enter">
      <div className="container">

        {/* ── Page Header ── */}
        <div className="rp-header">
          <div className="rp-header-text">
            <h1 className="rp-title">
              {searchTerm ? (
                <>Results for <span className="rp-title-query">"{searchTerm}"</span></>
              ) : (
                'All Restaurants'
              )}
            </h1>
            {!loading && (
              <p className="rp-subtitle">
                {filteredRestaurants.length} restaurant{filteredRestaurants.length !== 1 ? 's' : ''} available
              </p>
            )}
          </div>

          {/* Search bar */}
          <form className="rp-search" onSubmit={handleSearch} role="search">
            <Search size={17} className="rp-search-icon" aria-hidden="true" />
            <input
              type="search"
              placeholder="Search restaurants, cuisines..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="rp-search-input"
              id="restaurants-search"
              aria-label="Search restaurants"
              autoComplete="off"
            />
            {searchQuery && (
              <button
                type="button"
                className="rp-search-clear"
                onClick={clearSearch}
                aria-label="Clear search"
              >
                <X size={14} />
              </button>
            )}
          </form>
        </div>

        {/* ── Filter / Sort Bar ── */}
        <div className="rp-filter-bar">
          {/* Mobile: Filter drawer trigger */}
          <button
            className={`rp-filter-btn ${filterDrawerOpen ? 'active' : ''} ${hasActiveFilters ? 'has-filters' : ''}`}
            onClick={() => setFilterDrawerOpen(v => !v)}
            aria-expanded={filterDrawerOpen}
          >
            <Filter size={15} />
            Filters
            {activeFilterCount > 0 && (
              <span className="rp-filter-count">{activeFilterCount}</span>
            )}
          </button>

          {/* Quick chips — desktop */}
          <div className="rp-chips-row">
            {/* Open/Closed quick chips */}
            <button
              className={`rp-chip ${filterOpen === 'open' ? 'active' : ''}`}
              onClick={() => setFilterOpen(v => v === 'open' ? 'all' : 'open')}
            >
              <span className="rp-chip-dot open" />
              Open Now
            </button>
            <button
              className={`rp-chip ${filterOpen === 'closed' ? 'active' : ''}`}
              onClick={() => setFilterOpen(v => v === 'closed' ? 'all' : 'closed')}
            >
              <span className="rp-chip-dot closed" />
              Closed
            </button>

            {/* Rating chips */}
            {RATING_OPTIONS.slice(1).map(opt => (
              <button
                key={opt.value}
                className={`rp-chip ${filterRating === opt.value ? 'active' : ''}`}
                onClick={() => setFilterRating(v => v === opt.value ? 0 : opt.value)}
              >
                <Star size={11} fill={filterRating === opt.value ? 'var(--primary)' : 'var(--gold)'} color={filterRating === opt.value ? 'var(--primary)' : 'var(--gold)'} />
                {opt.label}
              </button>
            ))}

            {/* Sort dropdown */}
            <div className="rp-sort-wrap">
              <select
                className="rp-sort-select"
                value={sortBy}
                onChange={e => setSortBy(e.target.value)}
                aria-label="Sort restaurants"
              >
                {SORT_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
              <ChevronDown size={13} className="rp-sort-chevron" aria-hidden="true" />
            </div>

            {/* Clear all */}
            {hasActiveFilters && (
              <button className="rp-chip rp-chip-clear" onClick={clearFilters}>
                <X size={12} /> Clear
              </button>
            )}
          </div>
        </div>

        {/* ── Desktop Inline Filter Panel ── */}
        {filterDrawerOpen && (
          <div className="rp-filter-panel rp-filter-panel-desktop">
            <FilterPanel />
          </div>
        )}

        {/* ── Results count ── */}
        {!loading && hasActiveFilters && (
          <p className="rp-results-count">
            Showing <strong>{filteredRestaurants.length}</strong> of {restaurants.length} restaurants
          </p>
        )}

        {/* ── Grid ── */}
        {loading ? (
          <div className="rp-grid">
            {Array.from({ length: 8 }).map((_, i) => (
              <RestaurantCardSkeleton key={i} />
            ))}
          </div>
        ) : filteredRestaurants.length === 0 ? (
          <div className="rp-empty">
            <div className="rp-empty-illustration" aria-hidden="true">
              <div className="rp-empty-img" />
              <div className="rp-empty-circle rp-empty-circle-1" />
              <div className="rp-empty-circle rp-empty-circle-2" />
            </div>
            <h3 className="headline-md">No restaurants found</h3>
            <p className="body-md text-muted">
              {hasActiveFilters
                ? 'Try adjusting your filters or clearing them'
                : searchTerm
                  ? `No results for "${searchTerm}". Try a different search.`
                  : 'No restaurants available right now'}
            </p>
            <div className="rp-empty-actions">
              {searchTerm && (
                <button className="btn btn-outline" onClick={clearSearch}>
                  Clear Search
                </button>
              )}
              {hasActiveFilters && (
                <button className="btn btn-primary" onClick={clearFilters}>
                  Clear Filters
                </button>
              )}
            </div>
          </div>
        ) : (
          <div className="rp-grid">
            {filteredRestaurants.map((r) => {
              const hours = restaurantHours[r.id] || [];
              const { isOpen, nextOpenTime } = isRestaurantOpen(hours, r.isOpen);
              return (
                <RestaurantCard
                  key={r.id}
                  restaurant={r}
                  isOpen={isOpen}
                  nextOpenTime={nextOpenTime}
                />
              );
            })}
          </div>
        )}
      </div>

      {/* ── Mobile Filter Drawer ── */}
      <div
        className={`rp-drawer-overlay ${filterDrawerOpen ? 'open' : ''}`}
        onClick={() => setFilterDrawerOpen(false)}
        aria-hidden="true"
      />
      <div
        className={`rp-filter-drawer ${filterDrawerOpen ? 'open' : ''}`}
        aria-label="Filter options"
        aria-hidden={!filterDrawerOpen}
      >
        <div className="rp-drawer-header">
          <h3 className="headline-sm">Filters & Sort</h3>
          <button
            className="rp-drawer-close"
            onClick={() => setFilterDrawerOpen(false)}
            aria-label="Close filters"
          >
            <X size={20} />
          </button>
        </div>
        <div className="rp-drawer-body">
          <FilterPanel />
        </div>
        <div className="rp-drawer-footer">
          <button
            className="btn btn-primary"
            onClick={() => setFilterDrawerOpen(false)}
          >
            Show {filteredRestaurants.length} Results
          </button>
        </div>
      </div>
    </div>
  );
}
