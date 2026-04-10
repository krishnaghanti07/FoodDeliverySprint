# 🚀 Quick Start Guide - Food Delivery Microservices

## ✅ System Status: ALL RUNNING

```
✅ Gateway (5000)        → http://localhost:5000
✅ AuthService (5001)    → http://localhost:5001/swagger
✅ CatalogService (5002) → http://localhost:5002/swagger
✅ OrderService (5003)   → http://localhost:5003/swagger
✅ AdminService (5005)   → http://localhost:5005/swagger
```

## 🔐 Pre-configured Accounts

### Admin Account
```
Email: admin@fooddelivery.com
Password: Admin@1234
```

### Test Customer
```
Email: customer@test.com
Password: Test@1234
```

## 🎯 Quick Test Flow (5 Minutes)

### 1. Login as Admin (30 seconds)
1. Open: http://localhost:5001/swagger
2. Expand `POST /api/auth/login`
3. Click "Try it out"
4. Use admin credentials:
```json
{
  "email": "admin@fooddelivery.com",
  "password": "Admin@1234"
}
```
5. Click "Execute"
6. Copy the `accessToken` from response

### 2. Create a Restaurant (1 minute)
1. Open: http://localhost:5002/swagger
2. Click "Authorize" button (top right)
3. Enter: `Bearer {paste_your_token}`
4. Click "Authorize" then "Close"
5. Expand `POST /api/catalog/restaurants`
6. Click "Try it out"
7. Use this data:
```json
{
  "name": "Pizza Palace",
  "description": "Best pizzas in town",
  "address": "123 Main St",
  "city": "New York",
  "state": "NY",
  "zipCode": "10001",
  "phone": "1234567890",
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
8. Click "Execute"
9. Copy the restaurant `id` from response

### 3. Add Menu Item (1 minute)
1. Still in CatalogService Swagger
2. Expand `POST /api/catalog/menu-items`
3. Click "Try it out"
4. Use this data (replace `restaurantId` with your restaurant ID):
```json
{
  "restaurantId": "YOUR_RESTAURANT_ID_HERE",
  "categoryName": "Pizzas",
  "name": "Margherita Pizza",
  "description": "Classic tomato and mozzarella",
  "price": 12.99,
  "isVegetarian": true,
  "isAvailable": true,
  "preparationTime": 20
}
```
5. Click "Execute"
6. Copy the menu item `id`

### 4. Register as Customer (30 seconds)
1. Open: http://localhost:5001/swagger
2. Expand `POST /api/auth/register`
3. Click "Try it out"
4. Use this data:
```json
{
  "fullName": "Jane Customer",
  "email": "jane@example.com",
  "mobile": "9876543210",
  "password": "Test@1234",
  "role": "Customer"
}
```
5. Click "Execute"
6. Copy the `accessToken` from response

### 5. Add Item to Cart (1 minute)
1. Open: http://localhost:5003/swagger
2. Click "Authorize" and use customer token
3. Expand `POST /api/orders/cart/items`
4. Click "Try it out"
5. Use this data:
```json
{
  "restaurantId": "YOUR_RESTAURANT_ID",
  "menuItemId": "YOUR_MENU_ITEM_ID",
  "quantity": 2,
  "specialInstructions": "Extra cheese"
}
```
6. Click "Execute"

### 6. View Cart (30 seconds)
1. Still in OrderService Swagger
2. Expand `GET /api/orders/cart`
3. Click "Try it out"
4. Click "Execute"
5. See your cart with items and totals

### 7. Place Order (1 minute)
1. Expand `POST /api/orders`
2. Click "Try it out"
3. Use this data:
```json
{
  "deliveryAddress": "456 Oak Ave, New York, NY 10002",
  "deliveryInstructions": "Ring doorbell",
  "scheduledDeliveryTime": null,
  "paymentMethod": "Card"
}
```
4. Click "Execute"
5. Copy the order `id`

### 8. View Admin Dashboard (30 seconds)
1. Open: http://localhost:5005/swagger
2. Click "Authorize" and use admin token
3. Expand `GET /api/admin/dashboard`
4. Click "Try it out"
5. Click "Execute"
6. See platform statistics

## 🌐 Test Through Gateway

All endpoints can also be accessed through the Gateway at `http://localhost:5000/gateway/*`

### Example: Login through Gateway
```powershell
$body = @{email='admin@fooddelivery.com';password='Admin@1234'} | ConvertTo-Json
Invoke-WebRequest -Uri 'http://localhost:5000/gateway/auth/login' -Method POST -Body $body -ContentType 'application/json'
```

## 📊 Gateway Routes

| Route | Service | Port |
|-------|---------|------|
| `/gateway/auth/*` | AuthService | 5001 |
| `/gateway/catalog/*` | CatalogService | 5002 |
| `/gateway/orders/*` | OrderService | 5003 |
| `/gateway/deliveries/*` | OrderService | 5003 |
| `/gateway/admin/*` | AdminService | 5005 |

## 🔧 Common Operations

### Get All Restaurants
```
GET http://localhost:5002/api/catalog/restaurants
or
GET http://localhost:5000/gateway/catalog/restaurants
```

### Get Restaurant by ID
```
GET http://localhost:5002/api/catalog/restaurants/{id}
```

### Get My Orders (Customer)
```
GET http://localhost:5003/api/orders/my
Headers: Authorization: Bearer {customer_token}
```

### Get All Orders (Admin)
```
GET http://localhost:5005/api/admin/orders
Headers: Authorization: Bearer {admin_token}
```

## 📚 Full Documentation

- **Complete Testing Guide**: [TESTING_GUIDE.md](TESTING_GUIDE.md)
- **Deployment Summary**: [DEPLOYMENT_SUMMARY.md](DEPLOYMENT_SUMMARY.md)
- **Project Overview**: [README.md](README.md)

## 🆘 Troubleshooting

### Service Not Responding
Check if service is running:
```powershell
netstat -ano | findstr :5001
```

### Unauthorized Error
Make sure you:
1. Have a valid JWT token
2. Clicked "Authorize" in Swagger
3. Entered: `Bearer {your_token}` (with space after Bearer)

### Database Error
Services auto-migrate on startup. If issues persist, restart the service.

## 🎯 Key Features to Test

- ✅ User Registration & Login
- ✅ JWT Authentication
- ✅ Restaurant Management
- ✅ Menu Management
- ✅ Shopping Cart
- ✅ Order Placement
- ✅ Payment Simulation
- ✅ Order Tracking
- ✅ Admin Dashboard
- ✅ Role-Based Access Control

## 🚀 You're All Set!

The system is fully configured and ready to test. Start with the 5-minute quick test flow above, then explore the full API capabilities using the Swagger UIs.

**Happy Testing! 🎉**
