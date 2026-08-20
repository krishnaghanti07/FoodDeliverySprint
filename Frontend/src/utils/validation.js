/**
 * Shared form validation utilities for FoodRush
 */

// ── Regex patterns ────────────────────────────────────────────────────
export const PATTERNS = {
  email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
  mobile: /^[6-9]\d{9}$/,           // Indian mobile: starts 6-9, 10 digits
  pincode: /^\d{6}$/,               // Indian 6-digit pincode
  otpCode: /^\d{6}$/,
  vehicleNumber: /^[A-Z]{2}\d{2}[A-Z]{1,2}\d{4}$/i,  // e.g. MH12AB1234
  couponCode: /^[A-Z0-9_-]{3,20}$/,
  positiveNumber: /^\d+(\.\d{1,2})?$/,
  time: /^([01]\d|2[0-3]):([0-5]\d)$/,
};

// ── Field validators ──────────────────────────────────────────────────
export const validators = {
  required: (value, label = 'This field') =>
    !value || (typeof value === 'string' && !value.trim())
      ? `${label} is required`
      : null,

  email: (value) => {
    if (!value) return 'Email is required';
    if (!PATTERNS.email.test(value.trim())) return 'Enter a valid email address';
    return null;
  },

  mobile: (value) => {
    if (!value) return 'Mobile number is required';
    const digits = value.replace(/\D/g, '');
    if (digits.length !== 10) return 'Mobile number must be exactly 10 digits';
    if (!PATTERNS.mobile.test(digits)) return 'Enter a valid Indian mobile number (starts with 6-9)';
    return null;
  },

  password: (value) => {
    if (!value) return 'Password is required';
    if (value.length < 8) return 'Password must be at least 8 characters';
    if (!/[A-Z]/.test(value)) return 'Password must contain at least one uppercase letter';
    if (!/[a-z]/.test(value)) return 'Password must contain at least one lowercase letter';
    if (!/\d/.test(value)) return 'Password must contain at least one number';
    return null;
  },

  confirmPassword: (value, original) => {
    if (!value) return 'Please confirm your password';
    if (value !== original) return 'Passwords do not match';
    return null;
  },

  otpCode: (value) => {
    if (!value) return 'Verification code is required';
    if (!PATTERNS.otpCode.test(value)) return 'Enter a valid 6-digit code';
    return null;
  },

  fullName: (value) => {
    if (!value || !value.trim()) return 'Full name is required';
    if (value.trim().length < 2) return 'Name must be at least 2 characters';
    if (value.trim().length > 100) return 'Name must be less than 100 characters';
    if (!/^[a-zA-Z\s.'-]+$/.test(value.trim())) return 'Name can only contain letters, spaces, and . \' -';
    return null;
  },

  phone: (value) => {
    if (!value || !value.trim()) return 'Phone number is required';
    const digits = value.replace(/\D/g, '');
    if (digits.length < 7 || digits.length > 15) return 'Enter a valid phone number (7-15 digits)';
    return null;
  },

  pincode: (value) => {
    if (!value) return 'Pincode is required';
    if (!PATTERNS.pincode.test(value)) return 'Enter a valid 6-digit pincode';
    return null;
  },

  positiveNumber: (value, label = 'Value', min = 0, max = null) => {
    const num = parseFloat(value);
    if (value === '' || value === null || value === undefined) return `${label} is required`;
    if (isNaN(num)) return `${label} must be a valid number`;
    if (num < min) return `${label} must be at least ${min}`;
    if (max !== null && num > max) return `${label} must be at most ${max}`;
    return null;
  },

  couponCode: (value) => {
    if (!value || !value.trim()) return 'Coupon code is required';
    if (value.trim().length < 3) return 'Coupon code must be at least 3 characters';
    if (value.trim().length > 20) return 'Coupon code must be at most 20 characters';
    if (!PATTERNS.couponCode.test(value.trim())) return 'Coupon code can only contain letters, numbers, _ and -';
    return null;
  },

  dateRange: (from, to) => {
    if (!from) return 'Start date is required';
    if (!to) return 'End date is required';
    if (new Date(from) >= new Date(to)) return 'End date must be after start date';
    if (new Date(from) < new Date(new Date().toDateString())) return 'Start date cannot be in the past';
    return null;
  },

  timeRange: (openTime, closeTime) => {
    if (!openTime) return 'Open time is required';
    if (!closeTime) return 'Close time is required';
    if (openTime >= closeTime) return 'Close time must be after open time';
    return null;
  },

  address: (value) => {
    if (!value || !value.trim()) return 'Address is required';
    if (value.trim().length < 10) return 'Please enter a complete address (min 10 characters)';
    return null;
  },

  city: (value) => {
    if (!value || !value.trim()) return 'City is required';
    if (value.trim().length < 2) return 'City name must be at least 2 characters';
    return null;
  },

  state: (value) => {
    if (!value || !value.trim()) return 'State is required';
    if (value.trim().length < 2) return 'State name must be at least 2 characters';
    return null;
  },

  menuItemName: (value) => {
    if (!value || !value.trim()) return 'Item name is required';
    if (value.trim().length < 2) return 'Item name must be at least 2 characters';
    if (value.trim().length > 100) return 'Item name must be less than 100 characters';
    return null;
  },

  restaurantName: (value) => {
    if (!value || !value.trim()) return 'Restaurant name is required';
    if (value.trim().length < 2) return 'Restaurant name must be at least 2 characters';
    if (value.trim().length > 100) return 'Restaurant name must be less than 100 characters';
    return null;
  },

  cuisine: (value) => {
    if (!value || !value.trim()) return 'Cuisine type is required';
    if (value.trim().length < 2) return 'Cuisine type must be at least 2 characters';
    return null;
  },

  cancellationReason: (value) => {
    if (!value || !value.trim()) return 'Cancellation reason is required';
    if (value.trim().length < 5) return 'Please provide a more detailed reason (min 5 characters)';
    if (value.trim().length > 500) return 'Reason must be less than 500 characters';
    return null;
  },

  rejectionReason: (value) => {
    if (!value || !value.trim()) return 'Rejection reason is required';
    if (value.trim().length < 10) return 'Please provide a detailed reason (min 10 characters)';
    return null;
  },
};

/**
 * Validate a form object against a set of rules.
 * @param {Object} data - Form data
 * @param {Object} rules - { fieldName: validatorFn | [validatorFn, ...args] }
 * @returns {{ isValid: boolean, errors: Object }}
 */
export function validateForm(data, rules) {
  const errors = {};
  for (const [field, rule] of Object.entries(rules)) {
    let error = null;
    if (typeof rule === 'function') {
      error = rule(data[field]);
    } else if (Array.isArray(rule)) {
      const [fn, ...args] = rule;
      error = fn(data[field], ...args);
    }
    if (error) errors[field] = error;
  }
  return { isValid: Object.keys(errors).length === 0, errors };
}
