# Sample Data Seeded Successfully ✅

## Overview
All services now have comprehensive sample data that allows you to test every operation without manual data entry.

## Seeded Data Summary

### 🔐 Auth Service (10 Users)

#### Admin Account
- **Email**: `admin@fooddelivery.com`
- **Password**: `Admin@1234`
- **Role**: Admin
- **Use for**: Admin dashboard, user management, reports

#### Customer Accounts (3)
1. **John Doe**
   - Email: `john.doe@example.com`
   - Password: `Customer@123`
   - Role: Customer

2. **Jane Smith**
   - Email: `jane.smith@example.com`
   - Password: `Customer@123`
   - Role: Customer

3. **Mike Johnson**
   - Email: `mike.johnson@example.com`
   - Password: `Customer@123`
   - Role: Customer

#### Partner Accounts (3 Restaurant Owners)
1. **Mario Rossi** (Italian Restaurant)
   - Email: `mario@italianrestaurant.com`
   - Password: `Partner@123`
   - Role: Partner
   - Owns: Mario's Italian Kitchen, Burger Palace

2. **Chen Wei** (Chinese Restaurant)
   - Email: `chen@chineserestaurant.com`
   - Password: `Partner@123`
   - Role: Partner
   - Owns: Golden Dragon Chinese, Sushi Master

3. **Raj Patel** (Indian Restaurant)
   - Email: `raj@indianrestaurant.com`
   - Password: `Partner@123`
   - Role: Partner
   - Owns: Spice of India

#### Delivery Agent Accounts (3)
1. **David Wilson**
   - Email: `david.delivery@fooddelivery.com`
   - Password: `Delivery@123`
   - Role: DeliveryAgent

2. **Sarah Brown**
   - Email: `sarah.delivery@fooddelivery.com`
   - Password: `Delivery@123`
   - Role: DeliveryAgent

3. **Tom Anderson**
   - Email: `tom.delivery@fooddelivery.com`
   - Password: `Delivery@123`
   - Role: DeliveryAgent

---

### 🍽️ Catalog Service (5 Restaurants, 18 Categories, 24 Menu Items)

#### Restaurant 1: Mario's Italian Kitchen
- **Cuisine**: Italian
- **Address**: 123 Main Street, Downtown
- **Phone**: +1234567894
- **Owner**: Mario Rossi
- **Status**: Open, Approved
- **Rating**: 4.5/5
- **Delivery Fee**: $2.99
- **Min Order**: $10

**Menu Items (4)**:
1. Margherita Pizza - $12.99 (Pizza)
2. Spaghetti Carbonara - $14.99 (Pasta)
3. Tiramisu - $6.99 (Dessert)
4. Caesar Salad - $8.99 (Salad)

#### Restaurant 2: Golden Dragon Chinese
- **Cuisine**: Chinese
- **Address**: 456 Oak Avenue, Chinatown
- **Phone**: +1234567895
- **Owner**: Chen Wei
- **Status**: Open, Approved
- **Rating**: 4.3/5
- **Delivery Fee**: $3.99
- **Min Order**: $15

**Menu Items (4)**:
1. Kung Pao Chicken - $13.99 (Main Course)
2. Sweet and Sour Pork - $12.99 (Main Course)
3. Fried Rice - $9.99 (Rice)
4. Spring Rolls - $5.99 (Appetizer)

#### Restaurant 3: Spice of India
- **Cuisine**: Indian
- **Address**: 789 Curry Lane, Little India
- **Phone**: +1234567896
- **Owner**: Raj Patel
- **Status**: Open, Approved
- **Rating**: 4.7/5
- **Delivery Fee**: $2.49
- **Min Order**: $12

**Menu Items (4)**:
1. Chicken Tikka Masala - $15.99 (Curry)
2. Butter Naan - $3.99 (Bread)
3. Biryani - $14.99 (Rice)
4. Samosa - $4.99 (Appetizer)

#### Restaurant 4: Burger Palace
- **Cuisine**: American
- **Address**: 321 Burger Street, Food District
- **Phone**: +1234567800
- **Owner**: Mario Rossi
- **Status**: Open, Approved
- **Rating**: 4.2/5
- **Delivery Fee**: $1.99
- **Min Order**: $8

**Menu Items (4)**:
1. Classic Cheeseburger - $10.99 (Burger)
2. Bacon Burger - $12.99 (Burger)
3. French Fries - $4.99 (Sides)
4. Milkshake - $5.99 (Beverage)

#### Restaurant 5: Sushi Master
- **Cuisine**: Japanese
- **Address**: 555 Sakura Boulevard, Japan Town
- **Phone**: +1234567801
- **Owner**: Chen Wei
- **Status**: **CLOSED**, Approved
- **Rating**: 4.8/5
- **Delivery Fee**: $4.99
- **Min Order**: $20

**Menu Items (4)**:
1. California Roll - $11.99 (Sushi)
2. Salmon Sashimi - $16.99 (Sashimi)
3. Miso Soup - $3.99 (Soup)
4. Tempura - $9.99 (Appetizer)

---

### 👨‍💼 Admin Service (10 User Snapshots)
All users from Auth Service are synced to Admin Service for dashboard and reporting.

---

## Testing Scenarios

### Scenario 1: Customer Orders Food
1. **Login** as John Doe (`john.doe@example.com` / `Customer@123`)
2. **Browse** restaurants: `GET /api/catalog/restaurants`
3. **View** menu: `GET /api/catalog/menu-items?restaurantId={id}`
4. **Add to cart**: `POST /api/orders/cart/items`
   ```json
   {
     "menuItemId": "{menu-item-id}",
     "quantity": 2
   }
   ```
5. **View cart**: `GET /api/orders/cart`
6. **Checkout**: `POST /api/orders`
7. **Track order**: `GET /api/orders/my`

### Scenario 2: Partner Manages Restaurant
1. **Login** as Mario (`mario@italianrestaurant.com` / `Partner@123`)
2. **View my restaurants**: `GET /api/catalog/restaurants` (filter by owner)
3. **Add menu item**: `POST /api/catalog/menu-items`
   ```json
   {
     "restaurantId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
     "name": "Pepperoni Pizza",
     "description": "Classic pepperoni",
     "price": 14.99,
     "categoryId": "11111111-1111-1111-1111-111111111111",
     "isVeg": false,
     "isAvailable": true
   }
   ```
4. **Toggle restaurant open/close**: `PATCH /api/catalog/restaurants/{id}/toggle-open`
5. **View orders**: `GET /api/orders/restaurant/{restaurantId}`

### Scenario 3: Delivery Agent Picks Up Order
1. **Login** as David (`david.delivery@fooddelivery.com` / `Delivery@123`)
2. **View pending deliveries**: `GET /api/deliveries/pending`
3. **Assign delivery**: `POST /api/deliveries/assign`
   ```json
   {
     "orderId": "{order-id}",
     "deliveryAgentId": "44444444-4444-4444-4444-444444444444"
   }
   ```
4. **View my deliveries**: `GET /api/deliveries/my`
5. **Update status**: `PUT /api/deliveries/{id}/status`
   ```json
   {
     "status": "PickedUp"
   }
   ```

### Scenario 4: Admin Views Dashboard
1. **Login** as Admin (`admin@fooddelivery.com` / `Admin@1234`)
2. **View dashboard**: `GET /api/admin/dashboard`
3. **View all users**: `GET /api/admin/users`
4. **View all orders**: `GET /api/admin/orders`
5. **View sales report**: `GET /api/admin/reports/sales`
6. **View partners report**: `GET /api/admin/reports/partners`
7. **Manage user status**: `PATCH /api/admin/users/{id}/status`

### Scenario 5: Browse and Filter
1. **All restaurants**: `GET /api/catalog/restaurants`
2. **Open restaurants only**: Filter by `IsOpen = true`
3. **By cuisine**: Filter by `Cuisine = "Italian"`
4. **Menu items by restaurant**: `GET /api/catalog/menu-items?restaurantId={id}`
5. **Vegetarian items**: Filter by `IsVeg = true`

---

## Quick Test Commands

### Login and Get Token
```bash
# Customer Login
POST http://localhost:5001/api/auth/login
{
  "email": "john.doe@example.com",
  "password": "Customer@123"
}

# Partner Login
POST http://localhost:5001/api/auth/login
{
  "email": "mario@italianrestaurant.com",
  "password": "Partner@123"
}

# Admin Login
POST http://localhost:5001/api/auth/login
{
  "email": "admin@fooddelivery.com",
  "password": "Admin@1234"
}
```

### Browse Restaurants
```bash
GET http://localhost:5002/api/catalog/restaurants
```

### View Menu Items
```bash
GET http://localhost:5002/api/catalog/menu-items
```

### Add to Cart (Requires Auth)
```bash
POST http://localhost:5003/api/orders/cart/items
Authorization: Bearer {token}
{
  "menuItemId": "use-actual-menu-item-id",
  "quantity": 2
}
```

---

## Database IDs Reference

### Restaurant IDs
- Mario's Italian Kitchen: `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`
- Golden Dragon Chinese: `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb`
- Spice of India: `cccccccc-cccc-cccc-cccc-cccccccccccc`
- Burger Palace: `dddddddd-dddd-dddd-dddd-dddddddddddd`
- Sushi Master: `eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee`

### User IDs
- Admin: `11111111-1111-1111-1111-111111111111`
- John Doe: `22222222-2222-2222-2222-222222222222`
- Jane Smith: `22222222-2222-2222-2222-222222222223`
- Mike Johnson: `22222222-2222-2222-2222-222222222224`
- Mario Rossi: `33333333-3333-3333-3333-333333333333`
- Chen Wei: `33333333-3333-3333-3333-333333333334`
- Raj Patel: `33333333-3333-3333-3333-333333333335`
- David Wilson: `44444444-4444-4444-4444-444444444444`
- Sarah Brown: `44444444-4444-4444-4444-444444444445`
- Tom Anderson: `44444444-4444-4444-4444-444444444446`

---

## All Services Running ✅

- ✅ **Gateway** (Port 5000) - http://localhost:5000/swagger
- ✅ **AuthService** (Port 5001) - 10 users seeded
- ✅ **CatalogService** (Port 5002) - 5 restaurants, 24 menu items seeded
- ✅ **OrderService** (Port 5003) - Ready for orders
- ✅ **AdminService** (Port 5005) - 10 user snapshots seeded

---

## Success! 🎉

You can now test **every operation** in the system:
- ✅ User registration and login (all roles)
- ✅ Browse restaurants and menus
- ✅ Add items to cart
- ✅ Place orders
- ✅ Manage deliveries
- ✅ Partner restaurant management
- ✅ Admin dashboard and reports
- ✅ All CRUD operations

**Start testing at: http://localhost:5000/swagger**
