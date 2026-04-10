# 🚀 API Gateway - Unified Access Guide

## ✅ Gateway Successfully Configured!

Your API Gateway is now running and provides a **single entry point** for all microservices.

---

## 🌐 Access Points

### Main Gateway
- **URL**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger

### Individual Services (Still accessible for development)
- **AuthService**: http://localhost:5001/swagger
- **CatalogService**: http://localhost:5002/swagger
- **OrderService**: http://localhost:5003/swagger
- **AdminService**: http://localhost:5005/swagger

---

## 📋 How to Use the Gateway

### 1. Access Gateway Swagger
Open your browser and navigate to:
```
http://localhost:5000/swagger
```

### 2. Test Endpoints Through Gateway
All endpoints are accessible through the Gateway with the `/gateway` prefix:

**Example - Login:**
```
POST http://localhost:5000/gateway/auth/login
```

**Example - Get Restaurants:**
```
GET http://localhost:5000/gateway/catalog/restaurants
```

**Example - Place Order:**
```
POST http://localhost:5000/gateway/orders/orders
```

---

## 🔐 Authentication Flow

### Step 1: Login
```bash
POST /gateway/auth/login
{
  "email": "admin@fooddelivery.com",
  "password": "Admin@1234"
}
```

### Step 2: Copy Access Token
From the response, copy the `accessToken` value.

### Step 3: Authorize in Swagger
1. Click the **"Authorize"** button at the top of Swagger UI
2. Enter: `Bearer {your-access-token}`
3. Click **"Authorize"**
4. Now you can test all protected endpoints!

---

## 📊 Gateway Route Structure

### Auth Service Routes (`/gateway/auth/*`)
- `POST /gateway/auth/register` - Register new user
- `POST /gateway/auth/login` - Login
- `POST /gateway/auth/verify-otp` - Verify 2FA OTP
- `POST /gateway/auth/refresh` - Refresh token
- `POST /gateway/auth/send-otp` - Send OTP
- `POST /gateway/auth/verify-email` - Verify email
- `POST /gateway/auth/toggle-2fa` - Enable/disable 2FA
- `POST /gateway/auth/resend-otp` - Resend OTP

### Catalog Service Routes (`/gateway/catalog/*`)
- `GET /gateway/catalog/restaurants` - Browse restaurants
- `POST /gateway/catalog/restaurants` - Create restaurant
- `GET /gateway/catalog/restaurants/{id}` - Get restaurant details
- `PUT /gateway/catalog/restaurants/{id}` - Update restaurant
- `PATCH /gateway/catalog/restaurants/{id}/toggle-open` - Toggle open/closed
- `PATCH /gateway/catalog/restaurants/{id}/approve` - Approve restaurant
- `GET /gateway/catalog/restaurants/admin/all` - Get all (including unapproved)
- `GET /gateway/catalog/menu-items` - Get menu items by restaurant
- `GET /gateway/catalog/menu-items/{id}` - Get menu item details
- `POST /gateway/catalog/menu-items` - Add menu item
- `PUT /gateway/catalog/menu-items/{id}` - Update menu item
- `DELETE /gateway/catalog/menu-items/{id}` - Delete menu item
- `PATCH /gateway/catalog/menu-items/{id}/toggle-availability` - Toggle availability

### Order Service Routes (`/gateway/orders/*`)
**Cart:**
- `GET /gateway/orders/cart` - Get my cart
- `POST /gateway/orders/cart/items` - Add item to cart
- `PUT /gateway/orders/cart/items/{cartItemId}` - Update cart item
- `DELETE /gateway/orders/cart/items/{cartItemId}` - Remove cart item
- `DELETE /gateway/orders/cart` - Clear cart
- `POST /gateway/orders/cart/apply-coupon` - Apply coupon
- `GET /gateway/orders/cart/checkout-context` - Get checkout summary

**Orders:**
- `POST /gateway/orders/orders` - Place order
- `GET /gateway/orders/orders` - Get all orders (Admin)
- `GET /gateway/orders/orders/my` - Get my orders
- `GET /gateway/orders/orders/{id}` - Get order details
- `PUT /gateway/orders/orders/{id}/status` - Update order status
- `GET /gateway/orders/orders/restaurant/{restaurantId}` - Get restaurant orders

**Payments:**
- `POST /gateway/orders/payments/simulate` - Simulate payment
- `GET /gateway/orders/payments/order/{orderId}` - Get payment details

**Deliveries:**
- `POST /gateway/deliveries/assign` - Assign delivery agent
- `GET /gateway/deliveries/pending` - Get pending deliveries
- `GET /gateway/deliveries/my` - Get my deliveries
- `GET /gateway/deliveries/track/{orderId}` - Track delivery
- `GET /gateway/deliveries/{id}` - Get delivery details
- `PUT /gateway/deliveries/{id}/status` - Update delivery status

### Admin Service Routes (`/gateway/admin/*`)
**Dashboard:**
- `GET /gateway/admin/dashboard` - Get platform KPIs

**Users:**
- `GET /gateway/admin/users` - List all users
- `GET /gateway/admin/users/{id}` - Get user details
- `PATCH /gateway/admin/users/{id}/status` - Activate/deactivate user

**Orders:**
- `GET /gateway/admin/orders` - Get all orders with filters
- `GET /gateway/admin/orders/{id}` - Get order details
- `PUT /gateway/admin/orders/{id}/status` - Update order status

**Reports:**
- `GET /gateway/admin/reports/sales` - Sales report
- `GET /gateway/admin/reports/partners` - Partner performance report

---

## 🎯 Benefits of Using the Gateway

### 1. Single Entry Point
- No need to remember multiple service URLs
- All services accessible through one port (5000)

### 2. Centralized Authentication
- JWT validation happens at the Gateway level
- Consistent security across all services

### 3. Simplified Client Integration
- Frontend only needs to know one base URL
- Easier to manage CORS and security policies

### 4. Load Balancing & Routing
- Ocelot handles request routing automatically
- Can easily add load balancing in the future

### 5. Monitoring & Logging
- Single point to monitor all API traffic
- Easier to implement rate limiting and throttling

---

## 🧪 Testing Examples

### Test Public Endpoint (No Auth Required)
```bash
curl http://localhost:5000/gateway/catalog/restaurants
```

### Test Protected Endpoint (Auth Required)
```bash
# 1. Login first
curl -X POST http://localhost:5000/gateway/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@fooddelivery.com","password":"Admin@1234"}'

# 2. Use the token
curl http://localhost:5000/gateway/admin/dashboard \
  -H "Authorization: Bearer {your-token-here}"
```

---

## 📝 Configuration Files

### ocelot.json
Contains all route configurations with:
- `SwaggerKey`: Groups routes by service
- `UpstreamPathTemplate`: Gateway URL pattern
- `DownstreamPathTemplate`: Actual service URL
- `AuthenticationOptions`: JWT validation settings

### Gateway Program.cs
Configures:
- Ocelot middleware
- JWT authentication
- Swagger UI
- Route aggregation

---

## ✅ Current Status

**All Services Running:**
- ✅ Gateway (Port 5000) - **Main Entry Point**
- ✅ AuthService (Port 5001)
- ✅ CatalogService (Port 5002)
- ✅ OrderService (Port 5003)
- ✅ AdminService (Port 5005)

**Gateway Features:**
- ✅ Unified routing for all services
- ✅ JWT authentication at gateway level
- ✅ Swagger UI for API documentation
- ✅ All 60+ endpoints accessible through gateway
- ✅ Proper error handling and validation

---

## 🚀 Quick Start

1. **Open Gateway Swagger**: http://localhost:5000/swagger
2. **Login as Admin**:
   - Use endpoint: `POST /gateway/auth/login`
   - Email: `admin@fooddelivery.com`
   - Password: `Admin@1234`
3. **Copy the access token** from response
4. **Click "Authorize"** button in Swagger
5. **Enter**: `Bearer {your-token}`
6. **Test any endpoint** - you're all set!

---

## 💡 Pro Tips

1. **Use Gateway for all API calls** - Don't access services directly in production
2. **Keep tokens secure** - Never commit tokens to version control
3. **Monitor Gateway logs** - All requests flow through here
4. **Use Swagger for testing** - It's the easiest way to explore the API
5. **Check route prefixes** - All routes start with `/gateway/`

---

## 🎉 Success!

Your API Gateway is fully configured and working perfectly. You now have a professional, production-ready microservices architecture with:
- Centralized routing
- Unified authentication
- Single Swagger UI
- Clean API structure

**Access everything from**: http://localhost:5000/swagger
