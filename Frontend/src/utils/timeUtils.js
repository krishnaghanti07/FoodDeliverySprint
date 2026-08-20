/**
 * Check if a restaurant is currently open based on operating hours AND restaurant status
 * @param {Array} operatingHours - Array of operating hour objects
 * @param {boolean} restaurantIsOpen - Restaurant's manual open/closed toggle
 * @returns {Object} { isOpen: boolean, nextOpenTime: string|null }
 */
export function isRestaurantOpen(operatingHours, restaurantIsOpen = true) {
  console.log('[TimeUtils] Checking restaurant status');
  console.log('[TimeUtils] Restaurant manual toggle (isOpen):', restaurantIsOpen);
  console.log('[TimeUtils] Operating hours:', operatingHours);
  
  // First check: If restaurant is manually closed by partner, it's closed regardless of hours
  if (restaurantIsOpen === false) {
    console.log('[TimeUtils] ❌ Restaurant is manually CLOSED by partner');
    return { isOpen: false, nextOpenTime: 'when partner reopens' };
  }
  
  // Second check: Operating hours
  if (!operatingHours || operatingHours.length === 0) {
    console.log('[TimeUtils] No operating hours found, using manual toggle:', restaurantIsOpen);
    // If no hours set, use the manual toggle
    return { isOpen: restaurantIsOpen, nextOpenTime: null };
  }

  const now = new Date();
  const currentDay = now.getDay(); // 0 = Sunday, 1 = Monday, etc.
  const currentTime = now.getHours() * 60 + now.getMinutes(); // Current time in minutes
  
  console.log('[TimeUtils] Current day:', currentDay, '(0=Sun, 1=Mon, etc.)');
  console.log('[TimeUtils] Current time (minutes):', currentTime, `(${now.getHours()}:${now.getMinutes()})`);

  // Find today's operating hours
  const todayHours = operatingHours.find(h => h.dayOfWeek === currentDay);
  
  console.log('[TimeUtils] Today\'s hours found:', todayHours);

  if (!todayHours) {
    console.log('[TimeUtils] ❌ No hours found for today (day ' + currentDay + '), assuming CLOSED');
    const nextOpenTime = findNextOpenTime(operatingHours, currentDay);
    return { isOpen: false, nextOpenTime };
  }

  // Check if closed (handle both camelCase and PascalCase)
  const isClosed = todayHours.isClosed || todayHours.IsClosed || false;
  console.log('[TimeUtils] Is today marked as closed?', isClosed);
  
  if (isClosed) {
    console.log('[TimeUtils] ❌ Restaurant is marked as CLOSED today');
    const nextOpenTime = findNextOpenTime(operatingHours, currentDay);
    return { isOpen: false, nextOpenTime };
  }

  // Parse open and close times
  const openTime = timeSpanToMinutes(todayHours.openTime);
  const closeTime = timeSpanToMinutes(todayHours.closeTime);
  
  console.log('[TimeUtils] Open time (minutes):', openTime, `(${Math.floor(openTime/60)}:${openTime%60})`);
  console.log('[TimeUtils] Close time (minutes):', closeTime, `(${Math.floor(closeTime/60)}:${closeTime%60})`);
  console.log('[TimeUtils] Current >= Open?', currentTime >= openTime);
  console.log('[TimeUtils] Current < Close?', currentTime < closeTime);

  // Check if currently within operating hours
  const isOpen = currentTime >= openTime && currentTime < closeTime;
  
  console.log('[TimeUtils] Final result: Is open?', isOpen ? '✅ OPEN' : '❌ CLOSED');

  if (!isOpen) {
    const nextOpenTime = findNextOpenTime(operatingHours, currentDay);
    return { isOpen: false, nextOpenTime };
  }

  return { isOpen: true, nextOpenTime: null };
}

/**
 * Convert TimeSpan string (HH:mm:ss) to minutes
 * @param {string} timeSpan - Time in format "HH:mm:ss"
 * @returns {number} Time in minutes
 */
function timeSpanToMinutes(timeSpan) {
  if (!timeSpan) return 0;
  const parts = timeSpan.split(':');
  const hours = parseInt(parts[0]) || 0;
  const minutes = parseInt(parts[1]) || 0;
  return hours * 60 + minutes;
}

/**
 * Find the next opening time
 * @param {Array} operatingHours - Array of operating hour objects
 * @param {number} currentDay - Current day of week (0-6)
 * @returns {string|null} Next opening time description
 */
function findNextOpenTime(operatingHours, currentDay) {
  const daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  
  // Check next 7 days
  for (let i = 1; i <= 7; i++) {
    const nextDay = (currentDay + i) % 7;
    const nextDayHours = operatingHours.find(h => h.dayOfWeek === nextDay);
    
    // Check if closed (handle both camelCase and PascalCase)
    const isClosed = nextDayHours?.isClosed || nextDayHours?.IsClosed;
    
    if (nextDayHours && !isClosed) {
      const dayName = i === 1 ? 'Tomorrow' : daysOfWeek[nextDay];
      const openTime = formatTime(nextDayHours.openTime);
      return `${dayName} at ${openTime}`;
    }
  }
  
  return null; // No opening time found in next 7 days
}

/**
 * Format time from TimeSpan to 12-hour format
 * @param {string} timeSpan - Time in format "HH:mm:ss"
 * @returns {string} Formatted time (e.g., "9:00 AM")
 */
export function formatTime(timeSpan) {
  if (!timeSpan) return '';
  const parts = timeSpan.split(':');
  if (parts.length < 2) return timeSpan;
  const hours = parseInt(parts[0]);
  const minutes = parts[1];
  const ampm = hours >= 12 ? 'PM' : 'AM';
  const displayHours = hours % 12 || 12;
  return `${displayHours}:${minutes} ${ampm}`;
}

/**
 * Get current day name
 * @returns {string} Current day name
 */
export function getCurrentDayName() {
  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  return days[new Date().getDay()];
}
