# Payment & Refund System - Testing Guide

## Prerequisites
- All services running (AuthService, OrderService, AdminService, Gateway, Frontend)
- Test user accounts created
- Test restaurant and menu items available

---

## Test Scenario 1: Wallet Payment Flow

### Step 1: Add Funds to Wallet (Admin)
1. Login as Admin
2. Navigate to Admin Panel
3. Use admin wallet credit endpoint to add ₹1000 to test customer wallet

**API Test:**
```bash
POST /gateway/admin/wallet/credit
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "userId": "{customer_id}",
  "amount": 1000,
  "source": "TestCredit",
  "description": "Test wallet credit for testing"
}
```

**Expected Result:**
- Success response
- Customer wallet balance = ₹1000

---

### Step 2: Check Wallet Balance (Customer)
1. Login as Customer
2. Navigate to Checkout page
3. Verify wallet balance displays correctly

**API Test:**
```bash
GET /gateway/auth/wallet/balance
Authorization: Bearer {customer_token}
```

**Expected Result:**
- Response: `{ "balance": 1000 }`
- Wallet balance shows ₹1,000 in checkout page

---

### Step 3: Place Order with Wallet Payment
1. Add items to cart (total < ₹1000)
2. Go to checkout
3. Select "Digital Wallet" payment method
4. Verify wallet balance is sufficient
5. Place order

**Expected Result:**
- Order placed successfully
- Wallet balance deducted
- Order status: "Paid"
- New wallet balance = ₹1000 - Order Amount

---

## Test Scenario 2: Online Payment Cancellation & Refund

### Step 1: Place Order with Online Payment
1. Login as Customer
2. Add items to cart (e.g., ₹500 order)
3. Go to checkout
4. Select "Online Transaction (Razorpay)"
5. Complete payment (test mode)
6. Place order

**Expected Result:**
- Order placed successfully
- Order status: "Paid"
- Payment method: "CARD"

---

### Step 2: Cancel Order Before Partner Accepts
1. Go to "My Orders"
2. Find the order (status should be "Paid" or "AwaitingAcceptance")
3. Click "Cancel Order"
4. Enter cancellation reason
5. Confirm cancellation

**Expected Result:**
- Order status changed to "Cancelled"
- Refund request created automatically
- Refund status: "PendingApproval"

**Refund Calculation:**
- Original Amount: ₹500
- Platform Fee: -₹15
- Cancellation Charge (5%): -₹25
- **Refund Amount: ₹460**

---

### Step 3: Admin Reviews Refund Request
1. Login as Admin
2. Navigate to "Refund Management"
3. Click "Pending Refunds" tab
4. Verify refund request appears

**Expected Display:**
```
Refund Calculation:
  Original Amount:        ₹500
  Platform Fee:          -₹15
  Cancellation Charge:   -₹25
  ─────────────────────────────
  Refund Amount:         ₹460
```

**Customer Details:**
- Name: {customer_name}
- Email: {customer_email}
- Order ID: #{order_id}

---

### Step 4: Admin Approves Refund
1. Click "Approve Refund" button
2. Enter admin notes (optional)
3. Confirm approval

**API Test:**
```bash
POST /gateway/admin/refunds/{refund_id}/process
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "action": "Approve",
  "adminNotes": "Approved as per policy"
}
```

**Expected Result:**
- Success message: "Refund approved and wallet credited successfully"
- Refund status changed to "Approved"
- Customer wallet credited with ₹460
- Refund status eventually becomes "Completed"

---

### Step 5: Verify Wallet Credit (Customer)
1. Login as Customer
2. Go to Profile page
3. Check wallet balance

**Expected Result:**
- Wallet balance increased by ₹460
- If previous balance was ₹500, new balance = ₹960

---

## Test Scenario 3: COD Order Cancellation (No Refund)

### Step 1: Place COD Order
1. Login as Customer
2. Add items to cart
3. Select "Cash on Delivery"
4. Place order

**Expected Result:**
- Order placed successfully
- Order status: "PaymentPending"
- Payment method: "COD"

---

### Step 2: Cancel COD Order
1. Go to "My Orders"
2. Find the COD order
3. Click "Cancel Order"
4. Enter reason and confirm

**Expected Result:**
- Order status changed to "Cancelled"
- **NO refund request created** (because no payment was made)
- Wallet balance unchanged

---

## Test Scenario 4: Admin Rejects Refund

### Step 1: Create Refund Request
1. Place order with online payment
2. Cancel order before partner accepts
3. Verify refund request created

---

### Step 2: Admin Rejects Refund
1. Login as Admin
2. Go to "Refund Management"
3. Find pending refund
4. Click "Reject Refund"
5. Enter rejection reason
6. Confirm rejection

**Expected Result:**
- Refund status changed to "Rejected"
- Customer wallet NOT credited
- Admin notes saved with rejection reason

---

## Test Scenario 5: Insufficient Wallet Balance

### Step 1: Check Wallet Balance
1. Login as Customer
2. Go to checkout
3. Note current wallet balance (e.g., ₹100)

---

### Step 2: Try to Pay with Insufficient Balance
1. Add items to cart (total > wallet balance, e.g., ₹500)
2. Go to checkout
3. Observe "Digital Wallet" payment option

**Expected Result:**
- Wallet option is **disabled** (grayed out)
- Shows message: "Insufficient balance"
- Cannot select wallet as payment method
- Must use Online Transaction or COD

---

## Test Scenario 6: Multiple Refunds

### Step 1: Create Multiple Orders
1. Place 3 orders with online payment
2. Cancel all 3 orders
3. Verify 3 refund requests created

---

### Step 2: Admin Processes Multiple Refunds
1. Login as Admin
2. Go to "Refund Management"
3. Verify all 3 refunds appear in "Pending Refunds"
4. Approve 2 refunds
5. Reject 1 refund

**Expected Result:**
- 2 refunds approved and wallet credited
- 1 refund rejected
- All refunds show correct status
- Wallet balance reflects approved refunds only

---

## API Endpoints to Test

### Customer Endpoints

#### Get Wallet Balance
```bash
GET /gateway/auth/wallet/balance
Authorization: Bearer {customer_token}
```

#### Place Order with Wallet
```bash
POST /gateway/orders
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "deliveryAddress": "123 Test St",
  "paymentMethod": "WALLET",
  "razorpayOrderId": "test_order_id",
  "razorpayPaymentId": "test_payment_id",
  "razorpaySignature": "test_signature"
}
```

#### Cancel Order
```bash
POST /gateway/orders/{order_id}/cancel
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "reason": "Changed my mind"
}
```

---

### Admin Endpoints

#### Get Pending Refunds
```bash
GET /gateway/admin/refunds/pending
Authorization: Bearer {admin_token}
```

#### Get All Refunds
```bash
GET /gateway/admin/refunds
Authorization: Bearer {admin_token}
```

#### Get Refunds by Status
```bash
GET /gateway/admin/refunds?status=Approved
Authorization: Bearer {admin_token}
```

#### Process Refund (Approve)
```bash
POST /gateway/admin/refunds/{refund_id}/process
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "action": "Approve",
  "adminNotes": "Approved as per policy"
}
```

#### Process Refund (Reject)
```bash
POST /gateway/admin/refunds/{refund_id}/process
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "action": "Reject",
  "adminNotes": "Does not meet refund criteria"
}
```

#### Credit Wallet (Admin)
```bash
POST /gateway/admin/wallet/credit
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "userId": "{customer_id}",
  "amount": 1000,
  "source": "Refund",
  "referenceId": "{order_id}",
  "description": "Refund for cancelled order"
}
```

---

## Expected Database Changes

### After Order Cancellation (Online Payment)
**Orders Table:**
- Status: "Cancelled"
- CancelledAt: {timestamp}
- CancelledBy: {customer_id}
- CancellationReason: {reason}

**RefundRequests Table:**
- New record created
- OrderId: {order_id}
- CustomerId: {customer_id}
- OriginalAmount: {order_total}
- PlatformFee: 15.00
- CancellationCharge: {5% of order_total}
- RefundAmount: {calculated_refund}
- Status: "PendingApproval"
- RequestedAt: {timestamp}

---

### After Refund Approval
**RefundRequests Table:**
- Status: "Approved" → "Completed"
- ProcessedAt: {timestamp}
- ProcessedBy: {admin_id}
- AdminNotes: {notes}
- RefundedAt: {timestamp}

**Users Table (Customer):**
- WalletBalance: {previous_balance + refund_amount}

**WalletTransactions Table:**
- New record created
- UserId: {customer_id}
- Amount: {refund_amount}
- Type: "Credit"
- Source: "Refund"
- ReferenceId: {order_id}
- Description: "Refund for cancelled order"

---

## UI Verification Checklist

### Checkout Page
- [ ] Wallet balance displays correctly
- [ ] Wallet option disabled when insufficient balance
- [ ] Wallet option enabled when sufficient balance
- [ ] "Online Transaction" label (not "Credit/Debit Card")
- [ ] Payment method selection works
- [ ] Order placement successful

### Admin Refund Management
- [ ] "Pending Refunds" tab shows count
- [ ] Refund cards display correctly
- [ ] Refund breakdown shows all fields:
  - [ ] Original Amount
  - [ ] Platform Fee
  - [ ] Cancellation Charge
  - [ ] Refund Amount
- [ ] Customer details display (name, email)
- [ ] Order details display (order ID, restaurant)
- [ ] Cancellation reason displays
- [ ] Approve button works
- [ ] Reject button works
- [ ] Modal shows refund details
- [ ] Admin notes field works
- [ ] Success/error messages display

### Customer Profile
- [ ] Wallet balance displays
- [ ] Balance updates after refund approval
- [ ] Balance updates after wallet payment

### Customer Orders
- [ ] Can cancel paid orders
- [ ] Cannot cancel delivered orders
- [ ] Cancellation reason required
- [ ] Order status updates correctly

---

## Error Scenarios to Test

### 1. Insufficient Wallet Balance
**Test:** Try to pay with wallet when balance < order total
**Expected:** Error message, payment fails

### 2. Cancel Already Delivered Order
**Test:** Try to cancel order with status "Delivered"
**Expected:** Error message, cancellation fails

### 3. Process Already Processed Refund
**Test:** Try to approve/reject refund that's already processed
**Expected:** Error message, operation fails

### 4. Unauthorized Access
**Test:** Customer tries to access admin refund endpoints
**Expected:** 403 Forbidden error

### 5. Invalid Refund ID
**Test:** Try to process refund with non-existent ID
**Expected:** 404 Not Found error

---

## Performance Testing

### Load Test Scenarios
1. **Concurrent Orders:** 100 customers placing orders simultaneously
2. **Concurrent Cancellations:** 50 customers cancelling orders simultaneously
3. **Admin Processing:** Admin processing 100 refunds in quick succession
4. **Wallet Operations:** 100 concurrent wallet balance checks

### Expected Performance
- Order placement: < 2 seconds
- Order cancellation: < 1 second
- Refund approval: < 2 seconds
- Wallet balance check: < 500ms

---

## Security Testing

### Authentication Tests
- [ ] All endpoints require valid JWT token
- [ ] Admin endpoints reject customer tokens
- [ ] Customer endpoints reject expired tokens
- [ ] Wallet operations require authentication

### Authorization Tests
- [ ] Customer can only cancel own orders
- [ ] Customer can only view own wallet balance
- [ ] Admin can view all refunds
- [ ] Admin can process any refund

### Data Validation Tests
- [ ] Negative amounts rejected
- [ ] Zero amounts rejected
- [ ] Invalid order IDs rejected
- [ ] Invalid user IDs rejected

---

## Rollback Scenarios

### If Refund Approval Fails
1. Refund status remains "PendingApproval"
2. Wallet NOT credited
3. Admin can retry approval

### If Wallet Credit Fails After Approval
1. Refund status: "Approved" (not "Completed")
2. Manual wallet credit required
3. Admin notified of failure

---

## Success Criteria

✅ **All test scenarios pass**
✅ **No compilation errors**
✅ **All services running**
✅ **Database migrations applied**
✅ **UI displays correctly**
✅ **API endpoints respond correctly**
✅ **Refund calculation accurate**
✅ **Wallet operations work**
✅ **Admin controls functional**
✅ **Error handling works**
✅ **Security measures in place**

---

## Known Issues / Limitations

1. **Razorpay Test Mode:** Using test credentials, not real payments
2. **Email Notifications:** Not implemented yet for refund status
3. **Refund History:** Customer cannot view refund history in profile
4. **Partial Refunds:** Not supported (all or nothing)
5. **Refund Expiry:** No time limit on pending refunds

---

## Next Steps After Testing

1. **Add Email Notifications:**
   - Refund approved email
   - Refund rejected email
   - Wallet credited notification

2. **Add Customer Refund History:**
   - View refund requests in profile
   - Track refund status
   - View refund breakdown

3. **Add Refund Analytics:**
   - Total refunds processed
   - Average refund amount
   - Refund approval rate
   - Refund processing time

4. **Add Bulk Operations:**
   - Approve multiple refunds at once
   - Export refund reports
   - Filter by date range

5. **Add Wallet Transaction History:**
   - View all wallet transactions
   - Filter by type (credit/debit)
   - Export transaction history

---

**Testing Date:** May 4, 2026
**Tester:** Development Team
**Status:** Ready for Testing
