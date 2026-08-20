/**
 * cuisineImages.js
 * Maps cuisine type strings to high-quality Unsplash food images.
 * Each cuisine has a primary image + a pool of alternates for variety.
 * The restaurant ID is used to deterministically pick from the pool
 * so the same restaurant always shows the same image.
 */

// ── URL validation helper ────────────────────────────────────────────
/**
 * Returns true only if the URL is a valid http/https URL or a well-formed
 * data URI (starts with "data:image/"). Rejects bare base64 strings,
 * truncated data URIs, and anything else that would cause ERR_INVALID_URL.
 */
export function isValidImageUrl(url) {
  if (!url || typeof url !== 'string') return false;
  const trimmed = url.trim();
  // Valid http/https URL
  if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) return true;
  // Valid data URI — must have the full prefix and a comma separator
  if (trimmed.startsWith('data:image/') && trimmed.includes(',')) return true;
  return false;
}

// ── Cuisine image pools ──────────────────────────────────────────────
// Each entry: { primary, pool[] }
// All images are from Unsplash (free to use, no attribution required for display)

const CUISINE_IMAGES = {
  // ── Indian ──────────────────────────────────────────────────────
  indian: {
    primary: 'https://images.unsplash.com/photo-1585937421612-70a008356fbe?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1585937421612-70a008356fbe?w=600&q=80', // butter chicken
      'https://images.unsplash.com/photo-1631515243349-e0cb75fb8d3a?w=600&q=80', // biryani
      'https://images.unsplash.com/photo-1567188040759-fb8a883dc6d8?w=600&q=80', // dal makhani
      'https://images.unsplash.com/photo-1596797038530-2c107229654b?w=600&q=80', // paneer
      'https://images.unsplash.com/photo-1606491956689-2ea866880c84?w=600&q=80', // naan & curry
    ],
  },

  // ── Chinese ─────────────────────────────────────────────────────
  chinese: {
    primary: 'https://images.unsplash.com/photo-1563245372-f21724e3856d?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1563245372-f21724e3856d?w=600&q=80', // noodles
      'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=600&q=80', // dim sum
      'https://images.unsplash.com/photo-1617196034183-421b4040ed20?w=600&q=80', // fried rice
      'https://images.unsplash.com/photo-1569050467447-ce54b3bbc37d?w=600&q=80', // dumplings
      'https://images.unsplash.com/photo-1552611052-33e04de081de?w=600&q=80', // wonton soup
    ],
  },

  // ── Italian ─────────────────────────────────────────────────────
  italian: {
    primary: 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=600&q=80', // pizza
      'https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?w=600&q=80', // pasta
      'https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=600&q=80', // margherita
      'https://images.unsplash.com/photo-1612874742237-6526221588e3?w=600&q=80', // spaghetti
      'https://images.unsplash.com/photo-1595295333158-4742f28fbd85?w=600&q=80', // risotto
    ],
  },

  // ── Pizza ───────────────────────────────────────────────────────
  pizza: {
    primary: 'https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=600&q=80',
      'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=600&q=80',
      'https://images.unsplash.com/photo-1513104890138-7c749659a591?w=600&q=80',
      'https://images.unsplash.com/photo-1571407970349-bc81e7e96d47?w=600&q=80',
      'https://images.unsplash.com/photo-1628840042765-356cda07504e?w=600&q=80',
    ],
  },

  // ── Japanese / Sushi ────────────────────────────────────────────
  japanese: {
    primary: 'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=600&q=80', // sushi platter
      'https://images.unsplash.com/photo-1617196034183-421b4040ed20?w=600&q=80', // ramen
      'https://images.unsplash.com/photo-1569050467447-ce54b3bbc37d?w=600&q=80', // sashimi
      'https://images.unsplash.com/photo-1611143669185-af224c5e3252?w=600&q=80', // maki rolls
      'https://images.unsplash.com/photo-1562802378-063ec186a863?w=600&q=80', // japanese bowl
    ],
  },

  sushi: {
    primary: 'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=600&q=80',
      'https://images.unsplash.com/photo-1611143669185-af224c5e3252?w=600&q=80',
      'https://images.unsplash.com/photo-1569050467447-ce54b3bbc37d?w=600&q=80',
      'https://images.unsplash.com/photo-1562802378-063ec186a863?w=600&q=80',
    ],
  },

  // ── Mexican ─────────────────────────────────────────────────────
  mexican: {
    primary: 'https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=600&q=80', // tacos
      'https://images.unsplash.com/photo-1552332386-f8dd00dc2f85?w=600&q=80', // burrito
      'https://images.unsplash.com/photo-1513456852971-30c0b8199d4d?w=600&q=80', // nachos
      'https://images.unsplash.com/photo-1599974579688-8dbdd335c77f?w=600&q=80', // quesadilla
      'https://images.unsplash.com/photo-1551504734-5ee1c4a1479b?w=600&q=80', // guacamole
    ],
  },

  // ── Thai ────────────────────────────────────────────────────────
  thai: {
    primary: 'https://images.unsplash.com/photo-1559314809-0d155014e29e?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1559314809-0d155014e29e?w=600&q=80', // pad thai
      'https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?w=600&q=80', // green curry
      'https://images.unsplash.com/photo-1562565652-a0d8f0c59eb4?w=600&q=80', // tom yum
      'https://images.unsplash.com/photo-1569050467447-ce54b3bbc37d?w=600&q=80', // thai bowl
    ],
  },

  // ── American / Burgers ──────────────────────────────────────────
  american: {
    primary: 'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=600&q=80', // burger
      'https://images.unsplash.com/photo-1550547660-d9450f859349?w=600&q=80', // bbq burger
      'https://images.unsplash.com/photo-1561758033-d89a9ad46330?w=600&q=80', // fries & burger
      'https://images.unsplash.com/photo-1586816001966-79b736744398?w=600&q=80', // hot dog
      'https://images.unsplash.com/photo-1553979459-d2229ba7433b?w=600&q=80', // smash burger
    ],
  },

  burger: {
    primary: 'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=600&q=80',
      'https://images.unsplash.com/photo-1550547660-d9450f859349?w=600&q=80',
      'https://images.unsplash.com/photo-1553979459-d2229ba7433b?w=600&q=80',
      'https://images.unsplash.com/photo-1561758033-d89a9ad46330?w=600&q=80',
    ],
  },

  // ── Mediterranean / Middle Eastern ──────────────────────────────
  mediterranean: {
    primary: 'https://images.unsplash.com/photo-1544025162-d76694265947?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1544025162-d76694265947?w=600&q=80', // mezze
      'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?w=600&q=80', // falafel
      'https://images.unsplash.com/photo-1561043433-aaf687c4cf04?w=600&q=80', // hummus
      'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=600&q=80', // salad
    ],
  },

  // ── Biryani ─────────────────────────────────────────────────────
  biryani: {
    primary: 'https://images.unsplash.com/photo-1631515243349-e0cb75fb8d3a?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1631515243349-e0cb75fb8d3a?w=600&q=80',
      'https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?w=600&q=80',
      'https://images.unsplash.com/photo-1589302168068-964664d93dc0?w=600&q=80',
    ],
  },

  // ── Seafood ─────────────────────────────────────────────────────
  seafood: {
    primary: 'https://images.unsplash.com/photo-1559737558-2f5a35f4523b?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1559737558-2f5a35f4523b?w=600&q=80', // grilled fish
      'https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?w=600&q=80', // lobster
      'https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=600&q=80', // shrimp
      'https://images.unsplash.com/photo-1534482421-64566f976cfa?w=600&q=80', // fish & chips
    ],
  },

  // ── Healthy / Salads / Vegan ─────────────────────────────────────
  healthy: {
    primary: 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=600&q=80', // salad bowl
      'https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=600&q=80', // acai bowl
      'https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?w=600&q=80', // veggie bowl
      'https://images.unsplash.com/photo-1498837167922-ddd27525d352?w=600&q=80', // fresh salad
    ],
  },

  vegan: {
    primary: 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=600&q=80',
      'https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?w=600&q=80',
      'https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=600&q=80',
    ],
  },

  // ── Desserts / Bakery ────────────────────────────────────────────
  desserts: {
    primary: 'https://images.unsplash.com/photo-1551024601-bec78aea704b?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1551024601-bec78aea704b?w=600&q=80', // cake
      'https://images.unsplash.com/photo-1563729784474-d77dbb933a9e?w=600&q=80', // cupcakes
      'https://images.unsplash.com/photo-1488477181946-6428a0291777?w=600&q=80', // donuts
      'https://images.unsplash.com/photo-1578985545062-69928b1d9587?w=600&q=80', // chocolate cake
      'https://images.unsplash.com/photo-1464349095431-e9a21285b5f3?w=600&q=80', // macarons
    ],
  },

  bakery: {
    primary: 'https://images.unsplash.com/photo-1509440159596-0249088772ff?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1509440159596-0249088772ff?w=600&q=80', // bread
      'https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=600&q=80', // croissant
      'https://images.unsplash.com/photo-1517433670267-08bbd4be890f?w=600&q=80', // pastries
    ],
  },

  // ── Fast Food ────────────────────────────────────────────────────
  'fast food': {
    primary: 'https://images.unsplash.com/photo-1561758033-d89a9ad46330?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1561758033-d89a9ad46330?w=600&q=80', // fries
      'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=600&q=80', // burger
      'https://images.unsplash.com/photo-1550547660-d9450f859349?w=600&q=80', // combo
      'https://images.unsplash.com/photo-1586816001966-79b736744398?w=600&q=80', // hot dog
    ],
  },

  // ── Korean ───────────────────────────────────────────────────────
  korean: {
    primary: 'https://images.unsplash.com/photo-1590301157890-4810ed352733?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1590301157890-4810ed352733?w=600&q=80', // bibimbap
      'https://images.unsplash.com/photo-1498654896293-37aacf113fd9?w=600&q=80', // korean bbq
      'https://images.unsplash.com/photo-1617196034183-421b4040ed20?w=600&q=80', // ramen
    ],
  },

  // ── Continental ──────────────────────────────────────────────────
  continental: {
    primary: 'https://images.unsplash.com/photo-1544025162-d76694265947?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1544025162-d76694265947?w=600&q=80',
      'https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=600&q=80',
      'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&q=80',
      'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600&q=80',
    ],
  },

  // ── Mughlai ──────────────────────────────────────────────────────
  mughlai: {
    primary: 'https://images.unsplash.com/photo-1631515243349-e0cb75fb8d3a?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1631515243349-e0cb75fb8d3a?w=600&q=80',
      'https://images.unsplash.com/photo-1585937421612-70a008356fbe?w=600&q=80',
      'https://images.unsplash.com/photo-1596797038530-2c107229654b?w=600&q=80',
    ],
  },

  // ── South Indian ─────────────────────────────────────────────────
  'south indian': {
    primary: 'https://images.unsplash.com/photo-1630383249896-424e482df921?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1630383249896-424e482df921?w=600&q=80', // dosa
      'https://images.unsplash.com/photo-1589301760014-d929f3979dbc?w=600&q=80', // idli sambar
      'https://images.unsplash.com/photo-1567188040759-fb8a883dc6d8?w=600&q=80', // thali
    ],
  },

  // ── North Indian ─────────────────────────────────────────────────
  'north indian': {
    primary: 'https://images.unsplash.com/photo-1585937421612-70a008356fbe?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1585937421612-70a008356fbe?w=600&q=80',
      'https://images.unsplash.com/photo-1606491956689-2ea866880c84?w=600&q=80',
      'https://images.unsplash.com/photo-1596797038530-2c107229654b?w=600&q=80',
      'https://images.unsplash.com/photo-1567188040759-fb8a883dc6d8?w=600&q=80',
    ],
  },

  // ── Cafe / Coffee ────────────────────────────────────────────────
  cafe: {
    primary: 'https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=600&q=80', // coffee
      'https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=600&q=80', // latte art
      'https://images.unsplash.com/photo-1554118811-1e0d58224f24?w=600&q=80', // cafe food
      'https://images.unsplash.com/photo-1517433670267-08bbd4be890f?w=600&q=80', // pastry & coffee
    ],
  },

  // ── Multi Cuisine / Default ──────────────────────────────────────
  default: {
    primary: 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&q=80',
    pool: [
      'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&q=80',
      'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600&q=80',
      'https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=600&q=80',
      'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=600&q=80',
      'https://images.unsplash.com/photo-1482049016688-2d3e1b311543?w=600&q=80',
      'https://images.unsplash.com/photo-1467003909585-2f8a72700288?w=600&q=80',
      'https://images.unsplash.com/photo-1476224203421-9ac39bcb3327?w=600&q=80',
      'https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?w=600&q=80',
    ],
  },
};

/**
 * Normalise a cuisine string to a lookup key.
 * Handles partial matches: "North Indian & Chinese" → "north indian"
 */
function normaliseCuisine(cuisineStr) {
  if (!cuisineStr) return 'default';
  const lower = cuisineStr.toLowerCase().trim();

  // Direct match
  if (CUISINE_IMAGES[lower]) return lower;

  // Partial / keyword match — order matters (more specific first)
  const keywords = [
    'south indian', 'north indian', 'fast food',
    'biryani', 'mughlai', 'sushi', 'pizza', 'burger',
    'seafood', 'vegan', 'healthy', 'bakery', 'dessert',
    'cafe', 'coffee', 'korean', 'thai', 'mexican',
    'mediterranean', 'continental', 'japanese', 'chinese',
    'italian', 'american', 'indian',
  ];

  for (const kw of keywords) {
    if (lower.includes(kw)) return kw;
  }

  return 'default';
}

/**
 * Get a deterministic cuisine image URL for a restaurant.
 *
 * @param {string} cuisineType  - The restaurant's cuisine string
 * @param {string} restaurantId - Used to pick consistently from the pool
 * @param {boolean} usePrimary  - Force the primary image (e.g. for hero banners)
 * @returns {string} Unsplash image URL
 */
export function getCuisineImage(cuisineType, restaurantId = '', usePrimary = false) {
  const key = normaliseCuisine(cuisineType);
  const entry = CUISINE_IMAGES[key] || CUISINE_IMAGES.default;

  if (usePrimary) return entry.primary;

  // Use restaurant ID to deterministically pick from pool
  // so the same restaurant always gets the same image
  const hash = restaurantId
    ? restaurantId.split('').reduce((acc, ch) => acc + ch.charCodeAt(0), 0)
    : 0;

  return entry.pool[hash % entry.pool.length];
}

/**
 * Returns a CSS background-image value for a restaurant card.
 * Falls back to cuisine image when no uploaded image exists or URL is invalid.
 *
 * @param {object} restaurant - Restaurant object with imageUrl/logoUrl/cuisineType/id
 * @returns {string} CSS url(...) string
 */
export function getRestaurantCardImage(restaurant) {
  const uploaded = restaurant?.imageUrl || restaurant?.logoUrl;
  if (isValidImageUrl(uploaded)) return `url(${uploaded})`;

  const cuisineImg = getCuisineImage(
    restaurant?.cuisineType || restaurant?.cuisine,
    restaurant?.id || ''
  );
  return `url(${cuisineImg})`;
}
