# NUnit Testing Implementation

> **Status:** All 5 microservice test suites passing — 97 tests, 0 failures.

---

## Table of Contents

1. [Plan](#1-plan)
2. [Test Infrastructure Setup](#2-test-infrastructure-setup)
3. [AuthService Tests](#3-authservice-tests)
4. [OrderService Tests](#4-orderservice-tests)
5. [PaymentService Tests](#5-paymentservice-tests)
6. [CatalogService Tests](#6-catalogservice-tests)
7. [AdminService Tests](#7-adminservice-tests)
8. [Execution Results](#8-execution-results)
9. [Design Decisions](#9-design-decisions)

---

## 1. Plan

### Objective
Implement unit tests for all 5 microservices using NUnit 4 and Moq, targeting the Application layer (business logic) exclusively. No database, no HTTP, no RabbitMQ — all external dependencies are mocked.

### Scope — What is tested
| Layer | Tested? | Reason |
|---|---|---|
| Application Services | ✅ Yes | Core business logic lives here |
| Domain Entities | ✅ Indirectly | Exercised through service tests |
| Controllers (API) | ❌ No | Integration concern, not unit |
| Repositories (Infrastructure) | ❌ No | Mocked via interfaces |
| RabbitMQ consumers | ❌ No | Integration concern |

### Test categories per service
| Service | Test Class | Tests |
|---|---|---|
| AuthService | `AuthServiceTests` | 29 |
| OrderService | `CartServiceTests` | 18 |
| PaymentService | `PaymentSimulationServiceTests` + `RefundServiceTests` | 18 |
| CatalogService | `CatalogServiceTests` | 19 |
| AdminService | `AdminDashboardServiceTests` + `AdminUserServiceTests` | 13 |
| **Total** | | **97** |

### Testing strategy
- **Arrange–Act–Assert (AAA)** pattern throughout
- **Moq** for all interface mocks (repositories, JWT, email, RabbitMQ)
- **Positive tests** — happy path, valid inputs, expected outputs
- **Negative tests** — invalid inputs, missing records, business rule violations
- **Boundary tests** — zero amounts, empty collections, edge-case enums
- **Verification tests** — confirm side effects (repo calls, event publishes)

---

## 2. Test Infrastructure Setup

### Packages added to all 5 test projects
```xml
<PackageReference Include="NUnit"              Version="4.3.2" />
<PackageReference Include="NUnit3TestAdapter"  Version="5.0.0" />
<PackageReference Include="NUnit.Analyzers"    Version="4.7.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.0" />
<PackageReference Include="coverlet.collector" Version="6.0.4" />
<PackageReference Include="Moq"                Version="4.20.72" />
```

### Project references added
```
AuthService.Tests    → AuthService.Application + AuthService.Domain
OrderService.Tests   → OrderService.Application + OrderService.Domain
PaymentService.Tests → PaymentService.Application + PaymentService.Domain
CatalogService.Tests → CatalogService.Application + CatalogService.Domain
AdminService.Tests   → AdminService.Application + AdminService.Domain
```

### Commands used
```bash
# Add Moq to all test projects
dotnet add <project>.csproj package Moq --version 4.20.72

# Add missing domain project references
dotnet add <Tests>.csproj reference <Domain>.csproj
dotnet add <Tests>.csproj reference <Application>.csproj

# Run all tests
dotnet test <Tests>.csproj --no-build --verbosity minimal
```

### Test file structure
```
Backend/Services/
├── AuthService/AuthService.Tests/
│   ├── AuthService.Tests.csproj   ← updated with Moq + Domain ref
│   ├── UnitTest1.cs               ← original placeholder (kept)
│   └── AuthServiceTests.cs        ← new: 29 tests
│
├── OrderService/OrderService.Tests/
│   ├── OrderService.Tests.csproj  ← updated
│   ├── UnitTest1.cs
│   └── CartServiceTests.cs        ← new: 18 tests
│
├── PaymentService/PaymentService.Tests/
│   ├── PaymentService.Tests.csproj ← updated
│   ├── UnitTest1.cs
│   └── PaymentServiceTests.cs      ← new: 18 tests (2 classes)
│
├── CatalogService/CatalogService.Tests/
│   ├── CatalogService.Tests.csproj ← updated
│   ├── UnitTest1.cs
│   └── CatalogServiceTests.cs      ← new: 19 tests
│
└── AdminService/AdminService.Tests/
    ├── AdminService.Tests.csproj   ← updated
    ├── UnitTest1.cs
    └── AdminServiceTests.cs        ← new: 13 tests (2 classes)
```

---

## 3. AuthService Tests

**File:** `AuthService.Tests/AuthServiceTests.cs`
**Class:** `AuthServiceTests`
**Tests:** 29

### Mocked dependencies
```csharp
Mock<IUserRepository>    _userRepo
Mock<IJwtService>        _jwtSvc
Mock<IEmailService>      _emailSvc
Mock<IRabbitMqPublisher> _publisher
```

### Test coverage

#### Registration (5 tests)
| Test | Purpose |
|---|---|
| `Register_ValidCustomer_ReturnsTokens` | Happy path — Customer role registers, gets JWT + refresh token |
| `Register_DuplicateEmail_ThrowsInvalidOperationException` | Duplicate email guard |
| `Register_AdminRole_ThrowsArgumentException` | Admin accounts cannot be self-registered |
| `Register_DeliveryAgent_WithoutVehicleType_ThrowsArgumentException` | VehicleType is mandatory for agents |
| `Register_DeliveryAgent_IsApproved_False` | New agents start unapproved — requires admin approval |

#### Login (6 tests)
| Test | Purpose |
|---|---|
| `Login_ValidCredentials_ReturnsTokens` | Happy path — correct password returns tokens |
| `Login_WrongPassword_ThrowsUnauthorized` | BCrypt mismatch rejected |
| `Login_DeletedUser_ThrowsUnauthorized` | Soft-deleted accounts cannot log in |
| `Login_InactiveUser_ThrowsUnauthorized` | Deactivated accounts cannot log in |
| `Login_UnapprovedDeliveryAgent_ThrowsUnauthorized` | Unapproved agents blocked |
| `Login_NonExistentUser_ThrowsUnauthorized` | Unknown email returns generic error |

#### OTP Verification (3 tests)
| Test | Purpose |
|---|---|
| `VerifyOtp_ValidCode_ReturnsTokens` | Correct OTP within expiry issues tokens |
| `VerifyOtp_ExpiredCode_ThrowsUnauthorized` | Expired OTP rejected |
| `VerifyOtp_WrongCode_ThrowsUnauthorized` | Mismatched OTP rejected |

#### Refresh Token (3 tests)
| Test | Purpose |
|---|---|
| `RefreshToken_ValidToken_ReturnsNewTokens` | Valid refresh token issues new access token |
| `RefreshToken_ExpiredToken_ThrowsUnauthorized` | Expired refresh token rejected |
| `RefreshToken_InvalidToken_ThrowsUnauthorized` | Unknown token rejected |

#### Password Management (4 tests)
| Test | Purpose |
|---|---|
| `ResetPassword_ValidOtp_UpdatesHash` | Password reset with valid OTP updates BCrypt hash |
| `ChangePassword_CorrectCurrentPassword_Succeeds` | Correct current password allows change |
| `ChangePassword_WrongCurrentPassword_ThrowsUnauthorized` | Wrong current password blocked |

#### Profile (2 tests)
| Test | Purpose |
|---|---|
| `GetProfile_ExistingUser_ReturnsDto` | Returns mapped profile DTO with wallet balance |
| `GetProfile_NonExistentUser_ThrowsInvalidOperation` | Missing user throws |

#### Wallet (3 tests)
| Test | Purpose |
|---|---|
| `DeductFromWallet_SufficientBalance_DeductsCorrectly` | Balance decremented by exact amount |
| `DeductFromWallet_InsufficientBalance_ThrowsInvalidOperation` | Overdraft blocked |
| `AddToWallet_ValidAmount_IncreasesBalance` | Balance incremented correctly |

#### Soft Delete / Restore (3 tests)
| Test | Purpose |
|---|---|
| `SoftDeleteUser_ActiveUser_SetsDeletedFlags` | IsDeleted=true, IsActive=false, RefreshToken=null |
| `SoftDeleteUser_AlreadyDeleted_ThrowsInvalidOperation` | Double-delete blocked |
| `RestoreUser_DeletedUser_ClearsDeletedFlags` | IsDeleted=false, IsActive=true |

### Key assertion example
```csharp
[Test]
public async Task Register_DeliveryAgent_IsApproved_False()
{
    User? capturedUser = null;
    _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
             .Callback<User>(u => capturedUser = u)
             .Returns(Task.CompletedTask);

    await _sut.RegisterAsync(dto);

    Assert.That(capturedUser!.IsApproved, Is.False,
        "Delivery agents must start unapproved until admin approves them.");
}
```

---

## 4. OrderService Tests

**File:** `OrderService.Tests/CartServiceTests.cs`
**Class:** `CartServiceTests`
**Tests:** 18

### Mocked dependencies
```csharp
Mock<ICartRepository>          _cartRepo
Mock<ILogger<CartAppService>>  _logger
```

### Test coverage

#### Get Cart (2 tests)
| Test | Purpose |
|---|---|
| `GetCart_NoExistingCart_ReturnsEmptyCart` | Returns empty DTO when no cart exists |
| `GetCart_ExistingCart_ReturnsCartWithItems` | Subtotal and ItemCount calculated correctly |

#### Add Item (2 tests)
| Test | Purpose |
|---|---|
| `AddItem_NewCart_CreatesCartWithItem` | First item creates a new cart via `AddAsync` |
| `AddItem_ExistingItem_IncreasesQuantity` | Same MenuItemId merges quantity (delete-and-recreate pattern) |

#### Update Item (4 tests)
| Test | Purpose |
|---|---|
| `UpdateItem_ValidItem_UpdatesQuantity` | Quantity updated, subtotal recalculated |
| `UpdateItem_QuantityZero_RemovesItem` | Qty=0 removes item; RestaurantId cleared when cart empty |
| `UpdateItem_NonExistentCart_ThrowsKeyNotFoundException` | Missing cart throws |
| `UpdateItem_NonExistentItem_ThrowsKeyNotFoundException` | Missing item throws |

#### Remove Item (1 test)
| Test | Purpose |
|---|---|
| `RemoveItem_ValidItem_RemovesFromCart` | Correct item removed, other items preserved |

#### Apply / Remove Coupon (4 tests)
| Test | Purpose |
|---|---|
| `ApplyCoupon_ValidCode_AppliesDiscount` | FLAT50 applies ₹50 discount on ₹250 subtotal |
| `ApplyCoupon_EmptyCart_ThrowsInvalidOperation` | Cannot apply coupon to empty cart |
| `ApplyCoupon_InvalidCode_ThrowsInvalidOperation` | Unknown coupon code rejected |
| `RemoveCoupon_AppliedCoupon_ClearsDiscount` | CouponCode=null, Discount=0 |

#### Clear Cart (1 test)
| Test | Purpose |
|---|---|
| `ClearCart_ExistingCart_DeletesCart` | `DeleteAsync` and `SaveChangesAsync` called once each |

#### Checkout Context (3 tests)
| Test | Purpose |
|---|---|
| `GetCheckoutContext_ValidCart_ReturnsContextWithFees` | Verifies fee formula: subtotal + ₹30 delivery + 5% GST + ₹15 platform fee |
| `GetCheckoutContext_EmptyCart_ThrowsInvalidOperation` | Empty cart cannot proceed to checkout |
| `GetCheckoutContext_NoCart_ThrowsKeyNotFoundException` | Missing cart throws |

### Fee formula verified
```
Subtotal  = 180 × 2 = ₹360
GST (5%)  = ₹18
Delivery  = ₹30
Platform  = ₹15
─────────────────
Total     = ₹423   ✅
```

### Important fix applied
`Cart.Items` is typed as `ICollection<CartItem>` (no indexer). All assertions use `.First()` instead of `[0]`:
```csharp
// ❌ Fails — ICollection has no indexer
Assert.That(cart.Items[0].Name, Is.EqualTo("Burger"));

// ✅ Correct
Assert.That(cart.Items.First().Name, Is.EqualTo("Burger"));
```

---

## 5. PaymentService Tests

**File:** `PaymentService.Tests/PaymentServiceTests.cs`
**Classes:** `PaymentSimulationServiceTests` (13) + `RefundServiceTests` (5)
**Tests:** 18

### Mocked dependencies
```csharp
Mock<IPaymentTransactionRepository> _repo
Mock<IRabbitMqPublisher>            _publisher
Mock<ILogger<PaymentSimulationService>> _log
```

### PaymentSimulationService — 13 tests

#### Simulate Success / Failure (2 tests)
| Test | Purpose |
|---|---|
| `Simulate_SuccessfulPayment_ReturnsSuccessResult` | Status=Success, GatewayTxnId populated, FailureReason=null |
| `Simulate_FailedPayment_ReturnsFailedResult` | Status=Failed, FailureReason populated, GatewayTxnId=null |

#### Method Validation (4 tests)
| Test | Purpose |
|---|---|
| `Simulate_InvalidMethod_ThrowsArgumentException` | "BITCOIN" rejected |
| `Simulate_AllValidMethods_Succeed` (parameterized) | COD, CARD, WALLET all accepted |

#### Amount Validation (2 tests)
| Test | Purpose |
|---|---|
| `Simulate_ZeroAmount_ThrowsArgumentException` | Amount=0 rejected |
| `Simulate_NegativeAmount_ThrowsArgumentException` | Negative amount rejected |

#### Duplicate Payment Guard (1 test)
| Test | Purpose |
|---|---|
| `Simulate_AlreadyPaidOrder_ThrowsInvalidOperation` | Second payment on already-paid order blocked |

#### RabbitMQ Event Publishing (2 tests)
| Test | Purpose |
|---|---|
| `Simulate_SuccessfulPayment_PublishesCompletedEventTwice` | Publishes to OrderService queue AND AdminService queue |
| `Simulate_FailedPayment_PublishesFailedEventTwice` | Publishes to OrderService queue AND general failed queue |

#### Persistence (1 test)
| Test | Purpose |
|---|---|
| `Simulate_AnyPayment_PersistsTransactionToRepository` | `AddAsync` + `SaveChangesAsync` called exactly once |

### RefundService — 5 tests
| Test | Purpose |
|---|---|
| `ProcessRefund_FullRefund_SetsStatusRefunded` | Full refund sets `PaymentStatus.Refunded` |
| `ProcessRefund_PartialRefund_SetsStatusPartialRefund` | Partial refund sets `PaymentStatus.PartialRefund` |
| `ProcessRefund_AmountExceedsPaid_ThrowsInvalidOperation` | PRD rule: refund ≤ paid amount |
| `ProcessRefund_NotSuccessfulPayment_ThrowsInvalidOperation` | Only `Success` payments can be refunded |
| `ProcessRefund_NoPaymentRecord_ThrowsKeyNotFoundException` | Missing transaction throws |

### Parameterized test example
```csharp
[Test]
[TestCase("COD")]
[TestCase("CARD")]
[TestCase("WALLET")]
public async Task Simulate_AllValidMethods_Succeed(string method)
{
    // Runs 3 times — one per method
    var result = await _sut.SimulateAsync(dto with { Method = method });
    Assert.That(result.Status, Is.EqualTo("Success"));
}
```

---

## 6. CatalogService Tests

**File:** `CatalogService.Tests/CatalogServiceTests.cs`
**Class:** `CatalogServiceTests`
**Tests:** 19

### Mocked dependencies
```csharp
Mock<IRestaurantRepository>    _restaurantRepo
Mock<IMenuItemRepository>      _menuItemRepo
Mock<ICategoryRepository>      _categoryRepo
Mock<IOperatingHourRepository> _operatingHourRepo
Mock<IReviewRepository>        _reviewRepo
```

### Test coverage

#### Create Restaurant (3 tests)
| Test | Purpose |
|---|---|
| `CreateRestaurant_NewPartner_CreatesRestaurant` | Restaurant created with `IsApproved=false` |
| `CreateRestaurant_PartnerAlreadyHasRestaurant_ThrowsInvalidOperation` | One-restaurant-per-partner rule enforced |
| `CreateRestaurant_PartnerHasDeletedRestaurant_ThrowsInvalidOperation` | Deleted restaurant blocks new creation |

#### Approve Restaurant (2 tests)
| Test | Purpose |
|---|---|
| `ApproveRestaurant_PendingRestaurant_SetsApprovedTrue` | `IsApproved` flipped to true |
| `ApproveRestaurant_NonExistent_ThrowsKeyNotFoundException` | Missing restaurant throws |

#### Toggle Open Status (2 tests)
| Test | Purpose |
|---|---|
| `ToggleOpenStatus_OpenRestaurant_ClosesIt` | IsOpen toggled from true → false |
| `ToggleOpenStatus_WrongPartner_ThrowsUnauthorized` | Partner can only toggle their own restaurant |

#### Soft Delete / Restore (5 tests)
| Test | Purpose |
|---|---|
| `DeleteRestaurant_PartnerOwned_SoftDeletes` | IsDeleted=true, DeletedAt set |
| `DeleteRestaurant_WrongPartner_ThrowsUnauthorized` | Ownership enforced |
| `RestoreRestaurant_DeletedRestaurant_ClearsDeletedFlags` | IsDeleted=false, DeletedAt=null |
| `RestoreRestaurant_NotDeleted_ThrowsInvalidOperation` | Cannot restore active restaurant |
| `RestoreRestaurant_PartnerAlreadyHasActiveRestaurant_ThrowsInvalidOperation` | One-restaurant rule on restore |

#### Menu Items (2 tests)
| Test | Purpose |
|---|---|
| `AddMenuItem_ValidData_CreatesMenuItem` | MenuItem created with correct name and price |
| `ToggleMenuItemAvailability_AvailableItem_MakesUnavailable` | IsAvailable toggled |

#### Reviews (3 tests)
| Test | Purpose |
|---|---|
| `AddReview_NewReview_CreatesReview` | Review created, restaurant rating updated |
| `AddReview_DuplicateReview_ThrowsInvalidOperation` | One review per user per restaurant |
| `AddReview_InvalidRating_ThrowsArgumentException` (parameterized) | Rating 0 and 6 both rejected |

### Parameterized boundary test
```csharp
[Test]
[TestCase(0)]   // below minimum
[TestCase(6)]   // above maximum
public void AddReview_InvalidRating_ThrowsArgumentException(int rating)
{
    // Rating must be 1–5 per business rule
    Assert.ThrowsAsync<ArgumentException>(() =>
        _sut.AddReviewAsync(restaurantId, userId, "User",
            new CreateReviewDto { Rating = rating, Comment = "Test" }));
}
```

---

## 7. AdminService Tests

**File:** `AdminService.Tests/AdminServiceTests.cs`
**Classes:** `AdminDashboardServiceTests` (9) + `AdminUserServiceTests` (4)
**Tests:** 13

### AdminDashboardService — 9 tests

#### Mocked dependencies
```csharp
Mock<IOrderSnapshotRepository> _orderRepo
Mock<IUserSnapshotRepository>  _userRepo
```

| Test | Purpose |
|---|---|
| `GetDashboard_NoOrders_ReturnsZeroMetrics` | Empty state returns all zeros |
| `GetDashboard_WithDeliveredOrders_CalculatesRevenueCorrectly` | Platform fee + 15% commission formula verified |
| `GetDashboard_WithRefundRejectedOrders_IncludesCancellationCharge` | Platform fee + 5% cancellation charge formula verified |
| `GetDashboard_TodayOrders_CountedSeparately` | Today vs all-time orders split correctly |
| `GetDashboard_OrderStatusBreakdown_CountsCorrectly` | Paid/Delivered/Cancelled/InProgress counts |
| `GetDashboard_ActiveDeliveryAgents_CountedCorrectly` | Only active DeliveryAgent role users counted |
| `GetDashboard_TopRestaurants_MappedCorrectly` | TopRestaurantDto fields mapped from repository tuple |

### Revenue formula verified
```
Delivered order (₹400):
  Platform Fee  = ₹15
  Commission    = ₹400 × 15% = ₹60
  Admin Revenue = ₹75

Delivered order (₹200):
  Platform Fee  = ₹15
  Commission    = ₹200 × 15% = ₹30
  Admin Revenue = ₹45

Total Admin Revenue = ₹120  ✅

RefundRejected order (₹300):
  Platform Fee         = ₹15
  Cancellation Charge  = ₹300 × 5% = ₹15
  Admin Revenue        = ₹30  ✅
```

### AdminUserService — 4 tests
| Test | Purpose |
|---|---|
| `GetAllUsers_NoFilter_ReturnsAllUsers` | All users returned when no filter applied |
| `GetUserById_ExistingUser_ReturnsDto` | Correct DTO fields mapped |
| `GetUserById_NonExistentUser_ReturnsNull` | Returns null (not throws) for missing user |
| `ToggleUserStatus_ExistingUser_UpdatesSnapshotAndLogsAudit` | Snapshot updated AND audit log written |
| `ToggleUserStatus_NonExistentUser_ThrowsKeyNotFoundException` | Missing user throws |

### Audit log verification
```csharp
_auditRepo.Verify(r => r.AddAsync(It.Is<AdminAuditLog>(
    a => a.Action == "DeactivateUser" && a.EntityId == userId)), Times.Once);
```

---

## 8. Execution Results

### Final test run — all services

```
dotnet test Backend/Services/AuthService/AuthService.Tests/AuthService.Tests.csproj
Passed!  - Failed: 0, Passed: 29, Skipped: 0, Total: 29  (2 s)

dotnet test Backend/Services/OrderService/OrderService.Tests/OrderService.Tests.csproj
Passed!  - Failed: 0, Passed: 18, Skipped: 0, Total: 18  (169 ms)

dotnet test Backend/Services/PaymentService/PaymentService.Tests/PaymentService.Tests.csproj
Passed!  - Failed: 0, Passed: 18, Skipped: 0, Total: 18  (331 ms)

dotnet test Backend/Services/CatalogService/CatalogService.Tests/CatalogService.Tests.csproj
Passed!  - Failed: 0, Passed: 19, Skipped: 0, Total: 19  (372 ms)

dotnet test Backend/Services/AdminService/AdminService.Tests/AdminService.Tests.csproj
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13  (4 s)
```

### Summary table

| Service | Tests | Passed | Failed | Duration |
|---|---|---|---|---|
| AuthService | 29 | 29 | 0 | ~2 s |
| OrderService | 18 | 18 | 0 | ~169 ms |
| PaymentService | 18 | 18 | 0 | ~331 ms |
| CatalogService | 19 | 19 | 0 | ~372 ms |
| AdminService | 13 | 13 | 0 | ~4 s |
| **Total** | **97** | **97** | **0** | |

### Issues encountered and fixed

| Issue | Root Cause | Fix |
|---|---|---|
| `CS0021` — cannot apply indexing to `ICollection<CartItem>` | `Cart.Items` is `ICollection<T>`, not `List<T>` | Replaced `cart.Items[0]` with `cart.Items.First()` |
| Missing project references in OrderService, PaymentService, AdminService tests | `.csproj` only had Application ref, not Domain | Added Domain project references via `dotnet add reference` |

---

## 9. Design Decisions

### Why Application layer only?
Controllers are thin wrappers — they validate HTTP input and delegate to services. All business rules live in the Application layer. Testing controllers would require spinning up ASP.NET Core middleware, which is an integration test concern.

### Why Moq over other mocking libraries?
Moq is the de-facto standard for .NET mocking. It integrates cleanly with the interface-based repository pattern already used across all services. `NSubstitute` would work equally well but Moq was already implied by the project structure.

### Why no InMemory EF Core database?
InMemory EF Core tests are not true unit tests — they test EF Core behavior, not your business logic. Using mocked repositories keeps tests fast (sub-second), deterministic, and focused on the service logic alone.

### Callback pattern for capturing created entities
When a service calls `repo.AddAsync(entity)`, the entity is created inside the service and not returned. Moq's `.Callback<T>()` captures it for assertion:
```csharp
User? capturedUser = null;
_userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
         .Callback<User>(u => capturedUser = u)
         .Returns(Task.CompletedTask);

await _sut.RegisterAsync(dto);

Assert.That(capturedUser!.IsApproved, Is.False);
```

### Parameterized tests with `[TestCase]`
Used for boundary conditions and enum-like inputs to avoid duplicating test bodies:
```csharp
[TestCase("COD")]
[TestCase("CARD")]
[TestCase("WALLET")]
public async Task Simulate_AllValidMethods_Succeed(string method) { ... }

[TestCase(0)]
[TestCase(6)]
public void AddReview_InvalidRating_ThrowsArgumentException(int rating) { ... }
```

### How to run all tests at once
```bash
# From workspace root — runs all 5 suites
dotnet test Backend/Services/AuthService/AuthService.Tests/AuthService.Tests.csproj --no-build
dotnet test Backend/Services/OrderService/OrderService.Tests/OrderService.Tests.csproj --no-build
dotnet test Backend/Services/PaymentService/PaymentService.Tests/PaymentService.Tests.csproj --no-build
dotnet test Backend/Services/CatalogService/CatalogService.Tests/CatalogService.Tests.csproj --no-build
dotnet test Backend/Services/AdminService/AdminService.Tests/AdminService.Tests.csproj --no-build
```


---

## 10. Frontend Testing (Sample Suite)

### Framework & Tools

| Tool | Version | Role |
|---|---|---|
| **Vitest** | 4.1.5 | Test runner (Vite-native, replaces Jest) |
| **@testing-library/react** | latest | `renderHook`, `render`, `screen`, `act` |
| **@testing-library/jest-dom** | latest | DOM matchers (`toBeNull`, `toBeInTheDocument`) |
| **@testing-library/user-event** | latest | Simulated user interactions |
| **jsdom** | bundled | Browser environment simulation |

### Setup

**`vite.config.js`** — Vitest config added:
```js
test: {
  environment: 'jsdom',
  globals: true,
  setupFiles: ['./src/tests/setup.js'],
}
```

**`src/tests/setup.js`** — global jest-dom matchers:
```js
import '@testing-library/jest-dom';
```

**`package.json`** — scripts added:
```json
"test":          "vitest --run",
"test:watch":    "vitest",
"test:coverage": "vitest --run --coverage"
```

**Run command:**
```bash
npm test          # from /Frontend — single run
npm run test:watch  # watch mode during development
```

---

### Test File

**`Frontend/src/tests/FoodRush.sample.test.jsx`** — 15 tests across 4 categories

---

### Category 1 — Validation Utilities (5 tests)

**Module:** `src/utils/validation.js`
**Type:** Pure function tests — no DOM, no async, no mocks

| ID | Test Name | What it verifies |
|---|---|---|
| TC-V-01 | `email validator — accepts a valid email address` | Standard and complex valid emails return `null` |
| TC-V-02 | `email validator — rejects malformed emails` | Empty, no-@, no-domain all return error strings |
| TC-V-03 | `password validator — enforces complexity rules` | Short, no-uppercase, no-lowercase, no-digit each caught separately |
| TC-V-04 | `mobile validator — accepts valid Indian numbers only` | Starts-with-5 rejected; starts-with-6/9 accepted; 9-digit rejected |
| TC-V-05 | `validateForm — returns aggregated errors for multiple fields` | Multi-field form returns `isValid=false` with one error per failing field |

**Sample:**
```js
it('TC-V-03 | password validator — enforces complexity rules', () => {
  expect(validators.password('Ab1')).toBe('Password must be at least 8 characters');
  expect(validators.password('abcdefg1')).toBe('Password must contain at least one uppercase letter');
  expect(validators.password('Secure@123')).toBeNull(); // valid
});
```

---

### Category 2 — Time Utilities (3 tests)

**Module:** `src/utils/timeUtils.js`
**Type:** Pure function tests — deterministic by controlling input data

| ID | Test Name | What it verifies |
|---|---|---|
| TC-T-01 | `isRestaurantOpen — returns closed when partner manually closes` | `isOpen=false` toggle overrides all operating hours |
| TC-T-02 | `isRestaurantOpen — returns open:true when no hours configured and toggle is on` | Empty hours array falls back to manual toggle |
| TC-T-03 | `formatTime — converts HH:mm:ss TimeSpan to 12-hour format` | 09:00→9:00 AM, 13:30→1:30 PM, 00:00→12:00 AM, null→'' |

**Sample:**
```js
it('TC-T-03 | formatTime — converts HH:mm:ss TimeSpan to 12-hour format', () => {
  expect(formatTime('09:00:00')).toBe('9:00 AM');
  expect(formatTime('13:30:00')).toBe('1:30 PM');
  expect(formatTime('00:00:00')).toBe('12:00 AM');
  expect(formatTime(null)).toBe('');
});
```

---

### Category 3 — AuthContext Hook (4 tests)

**Module:** `src/context/AuthContext.jsx`
**Type:** React hook tests using `renderHook` + `act`
**API mock:** `vi.mock('../services/api')` — no real HTTP

| ID | Test Name | What it verifies |
|---|---|---|
| TC-A-01 | `initial state — unauthenticated when localStorage is empty` | `user=null`, `isAuthenticated=false`, `loading=false` on fresh mount |
| TC-A-02 | `initial state — restores user from localStorage on mount` | Persisted token+user in localStorage → `isAuthenticated=true` on reload |
| TC-A-03 | `login — stores tokens and sets user state on success` | Successful API response → tokens in localStorage, user state updated |
| TC-A-04 | `logout — clears user state and removes all tokens from localStorage` | `logout()` → `user=null`, all 3 localStorage keys removed |

**Sample:**
```js
it('TC-A-03 | login — stores tokens and sets user state on success', async () => {
  api.post.mockResolvedValueOnce({
    data: { data: { accessToken: 'new-access-token', role: 'Customer', fullName: 'Jane Doe' } }
  });

  const { result } = renderHook(() => useAuth(), { wrapper });

  await act(async () => { await result.current.login('jane@example.com', 'Password@123'); });

  expect(result.current.isAuthenticated).toBe(true);
  expect(localStorage.getItem('accessToken')).toBe('new-access-token');
});
```

---

### Category 4 — CartContext Hook (3 tests)

**Module:** `src/context/CartContext.jsx`
**Type:** React hook tests using `renderHook` + `act`
**API mock:** `vi.mock('../services/api')` — no real HTTP

| ID | Test Name | What it verifies |
|---|---|---|
| TC-C-01 | `initial state — cart is null and itemCount is 0` | Fresh provider has `cart=null`, `cartItemCount=0`, `cartLoading=false` |
| TC-C-02 | `fetchCart — populates cart state from API response` | API response sets `cart` state; `cartItemCount` = sum of all item quantities |
| TC-C-03 | `clearCart — resets cart to null after API call` | After `clearCart()`, `cart=null`, `cartItemCount=0`, `DELETE` called once |

**Sample:**
```js
it('TC-C-02 | fetchCart — populates cart state from API response', async () => {
  const mockCart = {
    items: [
      { id: 'item-1', quantity: 2 },
      { id: 'item-2', quantity: 1 },
    ]
  };
  api.get.mockResolvedValueOnce({ data: { data: mockCart } });

  await act(async () => { await result.current.fetchCart(); });

  expect(result.current.cart).toEqual(mockCart);
  expect(result.current.cartItemCount).toBe(3); // 2 + 1
});
```

---

### Execution Result

```
 RUN  v4.1.5

 Test Files  1 passed (1)
      Tests  15 passed (15)
   Start at  16:18:04
   Duration  2.47s
```

### Full Test Summary (Backend + Frontend)

| Layer | Suite | Tests | Passed | Failed |
|---|---|---|---|---|
| Backend | AuthService | 29 | 29 | 0 |
| Backend | OrderService | 18 | 18 | 0 |
| Backend | PaymentService | 18 | 18 | 0 |
| Backend | CatalogService | 19 | 19 | 0 |
| Backend | AdminService | 13 | 13 | 0 |
| Frontend | FoodRush.sample | 15 | 15 | 0 |
| **Total** | | **112** | **112** | **0** |
