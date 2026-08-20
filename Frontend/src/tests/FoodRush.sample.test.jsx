/**
 * FoodRush — Frontend Sample Test Suite
 *
 * 15 test cases across 4 categories:
 *   1. Validation Utilities   (5 tests) — pure functions, no DOM
 *   2. Time Utilities         (3 tests) — pure functions, no DOM
 *   3. AuthContext Hook       (4 tests) — React context + localStorage
 *   4. CartContext Hook       (3 tests) — React context + mocked API
 *
 * Framework : Vitest + @testing-library/react + jsdom
 * Run       : npm test  (from /Frontend)
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderHook } from '@testing-library/react';

// ── Modules under test ────────────────────────────────────────────────
import { validators, validateForm } from '../utils/validation';
import { isRestaurantOpen, formatTime } from '../utils/timeUtils';
import { AuthProvider, useAuth } from '../context/AuthContext';
import { CartProvider, useCart } from '../context/CartContext';

// ── API mock (prevents real HTTP calls in all tests) ──────────────────
vi.mock('../services/api', () => {
  const mockApi = {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
    interceptors: {
      request: { use: vi.fn() },
      response: { use: vi.fn() },
    },
  };
  return { default: mockApi };
});

import api from '../services/api';

// ═════════════════════════════════════════════════════════════════════
// CATEGORY 1 — VALIDATION UTILITIES
// Pure functions — no DOM, no async, no mocks needed
// ═════════════════════════════════════════════════════════════════════
describe('Validation Utilities', () => {

  // ── Test 1 ──────────────────────────────────────────────────────────
  it('TC-V-01 | email validator — accepts a valid email address', () => {
    expect(validators.email('user@example.com')).toBeNull();
    expect(validators.email('john.doe+tag@sub.domain.org')).toBeNull();
  });

  // ── Test 2 ──────────────────────────────────────────────────────────
  it('TC-V-02 | email validator — rejects malformed emails', () => {
    expect(validators.email('')).toBe('Email is required');
    expect(validators.email('notanemail')).toBe('Enter a valid email address');
    expect(validators.email('missing@')).toBe('Enter a valid email address');
    expect(validators.email('@nodomain.com')).toBe('Enter a valid email address');
  });

  // ── Test 3 ──────────────────────────────────────────────────────────
  it('TC-V-03 | password validator — enforces complexity rules', () => {
    // Too short
    expect(validators.password('Ab1')).toBe('Password must be at least 8 characters');
    // Missing uppercase
    expect(validators.password('abcdefg1')).toBe('Password must contain at least one uppercase letter');
    // Missing lowercase
    expect(validators.password('ABCDEFG1')).toBe('Password must contain at least one lowercase letter');
    // Missing digit
    expect(validators.password('Abcdefgh')).toBe('Password must contain at least one number');
    // Valid
    expect(validators.password('Secure@123')).toBeNull();
  });

  // ── Test 4 ──────────────────────────────────────────────────────────
  it('TC-V-04 | mobile validator — accepts valid Indian numbers only', () => {
    // Valid — starts with 9, 10 digits
    expect(validators.mobile('9876543210')).toBeNull();
    // Valid — starts with 6
    expect(validators.mobile('6000000000')).toBeNull();
    // Invalid — starts with 5 (not 6-9)
    expect(validators.mobile('5123456789')).toBe('Enter a valid Indian mobile number (starts with 6-9)');
    // Invalid — only 9 digits
    expect(validators.mobile('987654321')).toBe('Mobile number must be exactly 10 digits');
    // Empty
    expect(validators.mobile('')).toBe('Mobile number is required');
  });

  // ── Test 5 ──────────────────────────────────────────────────────────
  it('TC-V-05 | validateForm — returns aggregated errors for multiple fields', () => {
    const formData = { email: 'bad-email', password: 'weak' };
    const rules = {
      email: validators.email,
      password: validators.password,
    };

    const { isValid, errors } = validateForm(formData, rules);

    expect(isValid).toBe(false);
    expect(errors.email).toBe('Enter a valid email address');
    expect(errors.password).toBe('Password must be at least 8 characters');
    // No extra keys
    expect(Object.keys(errors)).toHaveLength(2);
  });

});

// ═════════════════════════════════════════════════════════════════════
// CATEGORY 2 — TIME UTILITIES
// Pure functions — deterministic by controlling input data
// ═════════════════════════════════════════════════════════════════════
describe('Time Utilities', () => {

  // ── Test 6 ──────────────────────────────────────────────────────────
  it('TC-T-01 | isRestaurantOpen — returns closed when partner manually closes', () => {
    const hours = [
      { dayOfWeek: 0, openTime: '09:00:00', closeTime: '22:00:00', isClosed: false },
      { dayOfWeek: 1, openTime: '09:00:00', closeTime: '22:00:00', isClosed: false },
      { dayOfWeek: 2, openTime: '09:00:00', closeTime: '22:00:00', isClosed: false },
      { dayOfWeek: 3, openTime: '09:00:00', closeTime: '22:00:00', isClosed: false },
      { dayOfWeek: 4, openTime: '09:00:00', closeTime: '22:00:00', isClosed: false },
      { dayOfWeek: 5, openTime: '09:00:00', closeTime: '22:00:00', isClosed: false },
      { dayOfWeek: 6, openTime: '09:00:00', closeTime: '22:00:00', isClosed: false },
    ];

    // isOpen=false means partner manually closed the restaurant
    const result = isRestaurantOpen(hours, false);

    expect(result.isOpen).toBe(false);
    expect(result.nextOpenTime).toBe('when partner reopens');
  });

  // ── Test 7 ──────────────────────────────────────────────────────────
  it('TC-T-02 | isRestaurantOpen — returns open:true when no hours configured and toggle is on', () => {
    // No operating hours set — fall back to manual toggle
    const result = isRestaurantOpen([], true);

    expect(result.isOpen).toBe(true);
    expect(result.nextOpenTime).toBeNull();
  });

  // ── Test 8 ──────────────────────────────────────────────────────────
  it('TC-T-03 | formatTime — converts HH:mm:ss TimeSpan to 12-hour format', () => {
    expect(formatTime('09:00:00')).toBe('9:00 AM');
    expect(formatTime('13:30:00')).toBe('1:30 PM');
    expect(formatTime('00:00:00')).toBe('12:00 AM');
    expect(formatTime('12:00:00')).toBe('12:00 PM');
    expect(formatTime('')).toBe('');
    expect(formatTime(null)).toBe('');
  });

});

// ═════════════════════════════════════════════════════════════════════
// CATEGORY 3 — AUTH CONTEXT HOOK
// Tests the AuthProvider state management and localStorage integration
// ═════════════════════════════════════════════════════════════════════
describe('AuthContext Hook', () => {

  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    localStorage.clear();
  });

  // ── Test 9 ──────────────────────────────────────────────────────────
  it('TC-A-01 | initial state — unauthenticated when localStorage is empty', () => {
    const wrapper = ({ children }) => <AuthProvider>{children}</AuthProvider>;
    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.user).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.loading).toBe(false);
  });

  // ── Test 10 ─────────────────────────────────────────────────────────
  it('TC-A-02 | initial state — restores user from localStorage on mount', () => {
    const savedUser = { email: 'test@example.com', role: 'Customer', fullName: 'Test User' };
    localStorage.setItem('accessToken', 'mock-token');
    localStorage.setItem('user', JSON.stringify(savedUser));

    const wrapper = ({ children }) => <AuthProvider>{children}</AuthProvider>;
    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.user).toEqual(savedUser);
    expect(result.current.isAuthenticated).toBe(true);
  });

  // ── Test 11 ─────────────────────────────────────────────────────────
  it('TC-A-03 | login — stores tokens and sets user state on success', async () => {
    api.post.mockResolvedValueOnce({
      data: {
        data: {
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
          role: 'Customer',
          fullName: 'Jane Doe',
        },
      },
    });

    const wrapper = ({ children }) => <AuthProvider>{children}</AuthProvider>;
    const { result } = renderHook(() => useAuth(), { wrapper });

    await act(async () => {
      await result.current.login('jane@example.com', 'Password@123');
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user.role).toBe('Customer');
    expect(result.current.user.fullName).toBe('Jane Doe');
    expect(localStorage.getItem('accessToken')).toBe('new-access-token');
    expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token');
  });

  // ── Test 12 ─────────────────────────────────────────────────────────
  it('TC-A-04 | logout — clears user state and removes all tokens from localStorage', async () => {
    // Pre-populate auth state
    localStorage.setItem('accessToken', 'existing-token');
    localStorage.setItem('refreshToken', 'existing-refresh');
    localStorage.setItem('user', JSON.stringify({ email: 'user@test.com', role: 'Customer' }));

    const wrapper = ({ children }) => <AuthProvider>{children}</AuthProvider>;
    const { result } = renderHook(() => useAuth(), { wrapper });

    act(() => {
      result.current.logout();
    });

    expect(result.current.user).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('refreshToken')).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
  });

});

// ═════════════════════════════════════════════════════════════════════
// CATEGORY 4 — CART CONTEXT HOOK
// Tests CartProvider state management with mocked API calls
// ═════════════════════════════════════════════════════════════════════
describe('CartContext Hook', () => {

  beforeEach(() => {
    vi.clearAllMocks();
  });

  // ── Test 13 ─────────────────────────────────────────────────────────
  it('TC-C-01 | initial state — cart is null and itemCount is 0', () => {
    const wrapper = ({ children }) => <CartProvider>{children}</CartProvider>;
    const { result } = renderHook(() => useCart(), { wrapper });

    expect(result.current.cart).toBeNull();
    expect(result.current.cartItemCount).toBe(0);
    expect(result.current.cartLoading).toBe(false);
  });

  // ── Test 14 ─────────────────────────────────────────────────────────
  it('TC-C-02 | fetchCart — populates cart state from API response', async () => {
    const mockCart = {
      id: 'cart-123',
      customerId: 'user-456',
      items: [
        { id: 'item-1', name: 'Biryani', quantity: 2, unitPrice: 180 },
        { id: 'item-2', name: 'Raita',   quantity: 1, unitPrice: 40  },
      ],
      subtotal: 400,
      total: 400,
    };
    api.get.mockResolvedValueOnce({ data: { data: mockCart } });

    const wrapper = ({ children }) => <CartProvider>{children}</CartProvider>;
    const { result } = renderHook(() => useCart(), { wrapper });

    await act(async () => {
      await result.current.fetchCart();
    });

    expect(result.current.cart).toEqual(mockCart);
    // cartItemCount = sum of quantities: 2 + 1 = 3
    expect(result.current.cartItemCount).toBe(3);
  });

  // ── Test 15 ─────────────────────────────────────────────────────────
  it('TC-C-03 | clearCart — resets cart to null after API call', async () => {
    // Start with a populated cart
    const mockCart = {
      id: 'cart-123',
      items: [{ id: 'item-1', name: 'Pizza', quantity: 1, unitPrice: 250 }],
    };
    api.get.mockResolvedValueOnce({ data: { data: mockCart } });
    api.delete.mockResolvedValueOnce({ data: {} });

    const wrapper = ({ children }) => <CartProvider>{children}</CartProvider>;
    const { result } = renderHook(() => useCart(), { wrapper });

    // Populate cart first
    await act(async () => {
      await result.current.fetchCart();
    });
    expect(result.current.cart).not.toBeNull();

    // Now clear it
    await act(async () => {
      await result.current.clearCart(false); // false = no toast
    });

    expect(result.current.cart).toBeNull();
    expect(result.current.cartItemCount).toBe(0);
    expect(api.delete).toHaveBeenCalledTimes(1);
  });

});
