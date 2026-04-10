# Food Delivery Microservices - Testing Guide

## System Status ✅

All services are running successfully:

- **Gateway**: http://localhost:5000 (Ocelot API Gateway)
- **AuthService**: http://localhost:5001 (Swagger: http://localhost:5001/swagger)
- **CatalogService**: http://localhost:5002 (Swagger: http://localhost:5002/swagger)
- **OrderService**: http://localhost:5003 (Swagger: http://localhost:5003/swagger)
- **AdminService**: http://localhost:5005 (Swagger: http://localhost:5005/swagger)

## Database Configuration

All databases have been created and migrated successfully:
- FoodDelivery_AuthDb
- FoodDelivery_CatalogDB
- FoodDelivery_OrderDB
- FoodDelivery_AdminDB

## Testing Workflow

### Step 1: Register & Login (AuthService)

#### 1.1 Register a Customer
**Endpoint**: `POST http://localhost:5001/api/auth/register`

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "mobile": "1234567890",
  "password": "Test@1234",
  "role": "Customer"
}
```

#### 1.2 Send OTP for Email Verification (NEW!)
**Endpoint**: `POST http://localhost:5001/api/auth/send-otp`

```json
{
  "email": "john@example.com",
  "purpose": "EmailVerification"
}
```

**Response**: OTP will be sent to the email address (check email or console logs)

#### 1.3 Verify Email with OTP (NEW!)
**Endpoint**: `POST http://localhost:5001/api/auth/verify-email`

```json
{
  "email": "john@example.com",
  "otpCode": "123456"
}
```

#### 1.4 Login
**Endpoint**: `POST http://localhost:5001/api/auth/login`

```json
{
  "email": "john@example.com",
  "password": "Test@1234"
}
```

**Response**: Copy the `token` from the response - you'll need it for authenticated requests.

#### 1.5 Enable Two-Factor Authentication (NEW!)
**Endpoint**: `POST http://localhost:5001/api/auth/toggle-2fa`
**Headers**: `Authorization: Bearer {your_token}`

```json
{
  "enable": true
}
```

**Note**: After enabling 2FA, future logins will require OTP verification.

#### 1.6 Admin Login (Pre-seeded)
**Endpoint**: `POST http://localhost:5001/api/auth/login`

```json
{
  "email": "admin@fooddelivery.com",
  "password": "Admin@1234"
}
```

### Step 2: Test Through Gateway (Recommended)

All services can be accessed through the Gateway at `http://localhost:5000/gateway/*`

#### Gateway Routes:
- Auth: `/gateway/auth/*` → AuthService (5001)
- Catalog: `/gateway/catalog/*` → CatalogService (5002)
- Orders: `/gateway/orders/*` → OrderService (5003)
- Deliveries: `/gateway/deliveries/*` → OrderService (5003)
- Admin: `/gateway/admin/*` → AdminService (5005)

### Step 3: Create Restaurant (Admin/Partner)

**Endpoint**: `POST http://localhost:5002/api/catalog/restaurants`
**Headers**: `Authorization: Bearer {your_token}`

```json
{
  "name": "Pizza Palace",
  "description": "Best pizzas in town",
  "address": "123 Main St",
  "city": "New York",
  "state": "NY",
  "zipCode": "10001",
  "phone": "+1234567890",
  "email": "contact@pizzapalace.com",
  "cuisineType": "Italian",
  "openingTime": "10:00",
  "closingTime": "22:00",
  "deliveryRadius": 5.0,
  "minimumOrderAmount": 10.0,
  "deliveryFee": 2.99,
  "estimatedDeliveryTime": 30,
  "commissionRate": 15.0
}
```

### Step 4: Add Menu Items

**Endpoint**: `POST http://localhost:5002/api/catalog/menu-items`
**Headers**: `Authorization: Bearer {your_token}`

```json
{
  "restaurantId": "{restaurant_id_from_step3}",
  "categoryName": "Pizzas",
  "name": "Margherita Pizza",
  "description": "Classic tomato and mozzarella",
  "price": 12.99,
  "isVegetarian": true,
  "isAvailable": true,
  "preparationTime": 20
}
```

### Step 5: Browse Restaurants (Customer)

**Endpoint**: `GET http://localhost:5002/api/catalog/restaurants`

### Step 6: Add Items to Cart (Customer)

**Endpoint**: `POST http://localhost:5003/api/orders/cart/items`
**Headers**: `Authorization: Bearer {customer_token}`

```json
{
  "restaurantId": "{restaurant_id}",
  "menuItemId": "{menu_item_id}",
  "quantity": 2,
  "specialInstructions": "Extra cheese please"
}
```

### Step 7: View Cart

**Endpoint**: `GET http://localhost:5003/api/orders/cart`
**Headers**: `Authorization: Bearer {customer_token}`

### Step 8: Get Checkout Context

**Endpoint**: `GET http://localhost:5003/api/orders/cart/checkout-context`
**Headers**: `Authorization: Bearer {customer_token}`

### Step 9: Simulate Payment

**Endpoint**: `POST http://localhost:5003/api/orders/payments/simulate`
**Headers**: `Authorization: Bearer {customer_token}`

```json
{
  "amount": 25.99,
  "paymentMethod": "Card",
  "shouldSucceed": true
}
```

### Step 10: Place Order

**Endpoint**: `POST http://localhost:5003/api/orders`
**Headers**: `Authorization: Bearer {customer_token}`

```json
{
  "deliveryAddress": "456 Oak Ave, New York, NY 10002",
  "deliveryInstructions": "Ring doorbell",
  "scheduledDeliveryTime": null,
  "paymentMethod": "Card"
}
```

### Step 11: View Order History

**Endpoint**: `GET http://localhost:5003/api/orders/my`
**Headers**: `Authorization: Bearer {customer_token}`

### Step 12: Admin Dashboard

**Endpoint**: `GET http://localhost:5005/api/admin/dashboard`
**Headers**: `Authorization: Bearer {admin_token}`

## Testing via Swagger UI

### AuthService Swagger
1. Open: http://localhost:5001/swagger
2. Test `/api/auth/register` and `/api/auth/login`
3. Copy the JWT token from login response

### CatalogService Swagger
1. Open: http://localhost:5002/swagger
2. Click "Authorize" button (top right)
3. Enter: `Bearer {your_token}`
4. Test restaurant and menu endpoints

### OrderService Swagger
1. Open: http://localhost:5003/swagger
2. Click "Authorize" and add your token
3. Test cart, order, payment, and delivery endpoints

### AdminService Swagger
1. Open: http://localhost:5005/swagger
2. Use admin token for authorization
3. Test dashboard, user management, and reports

## Testing via Gateway

You can also test through the Gateway using tools like Postman or curl:

```bash
# Register via Gateway
curl -X POST http://localhost:5000/gateway/auth/register \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Test User","email":"test@example.com","mobile":"+1234567890","password":"Test@1234","role":"Customer"}'

# Login via Gateway
curl -X POST http://localhost:5000/gateway/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@1234"}'

# Get restaurants via Gateway
curl -X GET http://localhost:5000/gateway/catalog/restaurants
```

## Common Issues & Solutions

### Issue: "Unauthorized" error
**Solution**: Make sure you've included the Bearer token in the Authorization header

### Issue: "Invalid object name" error
**Solution**: Database migrations may not have run. Restart the service.

### Issue: Service not responding
**Solution**: Check if the service is running on the correct port using `netstat -ano | findstr :<port>`

## Service Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway (Port 5000)                   │
│                         Ocelot                               │
└────────────┬────────────┬────────────┬────────────┬─────────┘
             │            │            │            │
    ┌────────▼───┐  ┌────▼─────┐  ┌──▼──────┐  ┌─▼─────────┐
    │   Auth     │  │ Catalog  │  │  Order  │  │   Admin   │
    │  Service   │  │ Service  │  │ Service │  │  Service  │
    │  (5001)    │  │  (5002)  │  │ (5003)  │  │  (5005)   │
    └────────────┘  └──────────┘  └─────────┘  └───────────┘
         │               │              │             │
    ┌────▼───────────────▼──────────────▼─────────────▼────┐
    │              SQL Server (KRISHNA\SQL_SERVER_2025)     │
    │  AuthDb  │  CatalogDB  │  OrderDB  │  AdminDB        │
    └──────────────────────────────────────────────────────┘
```

## Next Steps

1. Test all endpoints using Swagger UI
2. Verify JWT authentication works across services
3. Test the complete order flow from cart to delivery
4. Check admin dashboard and reports
5. Test role-based access control (Customer vs Admin vs Partner)

## Notes

- All services use the same JWT secret key for token validation
- The Gateway validates JWT tokens before routing to services
- RabbitMQ consumers are running in OrderService and AdminService for event-driven communication
- Admin user is pre-seeded with email: admin@fooddelivery.com, password: Admin@1234
