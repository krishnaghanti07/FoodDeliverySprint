# FoodDelivery - Microservices Architecture

A complete Food Delivery & Restaurant Aggregator system built with ASP.NET Core microservices, Ocelot API Gateway, and SQL Server.

## 🚀 System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              API Gateway (Port 5000) - Ocelot                │
└────────────┬────────────┬────────────┬────────────┬─────────┘
             │            │            │            │
    ┌────────▼───┐  ┌────▼─────┐  ┌──▼──────┐  ┌─▼─────────┐
    │   Auth     │  │ Catalog  │  │  Order  │  │   Admin   │
    │  Service   │  │ Service  │  │ Service │  │  Service  │
    │  (5001)    │  │  (5002)  │  │ (5003)  │  │  (5005)   │
    └────────────┘  └──────────┘  └─────────┘  └───────────┘
         │               │              │             │
    ┌────▼───────────────▼──────────────▼─────────────▼────┐
    │         SQL Server (KRISHNA\SQL_SERVER_2025)          │
    │  AuthDb  │  CatalogDB  │  OrderDB  │  AdminDB        │
    └──────────────────────────────────────────────────────┘
```

## ✅ Services Running

All services are configured and running:

- **Gateway**: http://localhost:5000 (Ocelot API Gateway)
- **AuthService**: http://localhost:5001 ([Swagger](http://localhost:5001/swagger))
- **CatalogService**: http://localhost:5002 ([Swagger](http://localhost:5002/swagger))
- **OrderService**: http://localhost:5003 ([Swagger](http://localhost:5003/swagger))
- **AdminService**: http://localhost:5005 ([Swagger](http://localhost:5005/swagger))

## 📋 Features Implemented

### 1. Authentication Service (Port 5001)
- User registration (Customer, Partner, Admin, DeliveryAgent)
- JWT-based authentication
- Token refresh mechanism
- Email service integration
- Role-based access control

### 2. Catalog Service (Port 5002)
- Restaurant CRUD operations
- Menu item management
- Category management
- Restaurant search and filtering
- Availability management

### 3. Order Service (Port 5003)
- Shopping cart management
- Order placement and tracking
- Payment simulation
- Delivery assignment
- Order status lifecycle management
- RabbitMQ event publishing

### 4. Admin Service (Port 5005)
- Dashboard with KPIs
- User management
- Order supervision
- Sales and partner reports
- Audit logging
- RabbitMQ event consumers

## 🔧 Technology Stack

- **Backend**: ASP.NET Core 10.0
- **Database**: SQL Server (EF Core Code-First)
- **API Gateway**: Ocelot
- **Authentication**: JWT Bearer Tokens
- **Messaging**: RabbitMQ
- **Documentation**: Swagger/OpenAPI
- **Architecture**: Microservices with Clean Architecture

## 🚦 Getting Started

### Prerequisites
- .NET 10.0 SDK
- SQL Server (KRISHNA\SQL_SERVER_2025)
- RabbitMQ (optional, for event-driven features)

### Running the Services

All services are currently running. To restart them:

```bash
# AuthService
cd Services/AuthService/AuthService.API
dotnet run

# CatalogService
cd Services/CatalogService/CatalogService.API
dotnet run

# OrderService
cd Services/OrderService/OrderService.API
dotnet run

# AdminService
cd Services/AdminService/AdminService.API
dotnet run

# Gateway
cd Gateway/FoodDelivery.Gateway
dotnet run
```

## 📖 Testing Guide

See [TESTING_GUIDE.md](TESTING_GUIDE.md) for comprehensive testing instructions.

### Quick Test

1. **Register a customer**:
```powershell
$body = @{fullName='Test User';email='test@example.com';mobile='1234567890';password='Test@1234';role='Customer'} | ConvertTo-Json
Invoke-WebRequest -Uri 'http://localhost:5000/gateway/auth/register' -Method POST -Body $body -ContentType 'application/json'
```

2. **Login**:
```powershell
$body = @{email='test@example.com';password='Test@1234'} | ConvertTo-Json
Invoke-WebRequest -Uri 'http://localhost:5000/gateway/auth/login' -Method POST -Body $body -ContentType 'application/json'
```

3. **Access Swagger UIs**:
- AuthService: http://localhost:5001/swagger
- CatalogService: http://localhost:5002/swagger
- OrderService: http://localhost:5003/swagger
- AdminService: http://localhost:5005/swagger

## 🔐 Pre-seeded Admin Account

- **Email**: admin@fooddelivery.com
- **Password**: Admin@1234

## 🌐 Gateway Routes

All services are accessible through the Gateway:

| Route Pattern | Target Service | Port |
|--------------|----------------|------|
| `/gateway/auth/*` | AuthService | 5001 |
| `/gateway/catalog/*` | CatalogService | 5002 |
| `/gateway/orders/*` | OrderService | 5003 |
| `/gateway/deliveries/*` | OrderService | 5003 |
| `/gateway/admin/*` | AdminService | 5005 |

## 📊 Database Schema

All databases have been created with EF Core migrations:

- **FoodDelivery_AuthDb**: Users table
- **FoodDelivery_CatalogDB**: Restaurants, Categories, MenuItems
- **FoodDelivery_OrderDB**: Carts, Orders, Payments, DeliveryAssignments
- **FoodDelivery_AdminDB**: UserSnapshots, OrderSnapshots, AuditLogs

## 🔄 Order Lifecycle

```
Cart → Checkout → Payment → Order Placed → Restaurant Accepted → 
Preparing → Ready for Pickup → Picked Up → Out for Delivery → Delivered
```

## 🎯 Key Features

✅ JWT Authentication across all services  
✅ Ocelot API Gateway with routing  
✅ EF Core Code-First migrations  
✅ Clean Architecture (Domain, Application, Infrastructure, API)  
✅ Repository pattern  
✅ DTO-based API contracts  
✅ Swagger documentation for all services  
✅ Role-based authorization  
✅ Event-driven architecture with RabbitMQ  
✅ Comprehensive error handling  
✅ Audit logging  

## 📝 Project Structure

```
FoodDelivery/
├── Gateway/
│   └── FoodDelivery.Gateway/          # Ocelot API Gateway
├── Services/
│   ├── AuthService/                   # Authentication & Authorization
│   ├── CatalogService/                # Restaurant & Menu Management
│   ├── OrderService/                  # Orders, Cart, Payments, Delivery
│   └── AdminService/                  # Admin Dashboard & Reports
├── Shared/
│   └── FoodDelivery.Shared/           # Shared DTOs and Messaging
└── TESTING_GUIDE.md                   # Comprehensive testing guide
```

## 🧪 Testing

All services can be tested via:
1. **Swagger UI** - Individual service endpoints
2. **Gateway** - Unified API access
3. **Postman** - Import Swagger definitions
4. **curl/PowerShell** - Command-line testing

## 📚 Documentation

- [Testing Guide](TESTING_GUIDE.md) - Complete API testing workflow
- Swagger UI available for each service
- OpenAPI specifications at `/swagger/v1/swagger.json`

## 🎓 Learning Outcomes

This project demonstrates:
- Microservices architecture design
- API Gateway pattern with Ocelot
- JWT authentication and authorization
- EF Core Code-First approach
- Clean Architecture principles
- Event-driven communication
- RESTful API design
- Swagger/OpenAPI documentation

## 🔧 Configuration

All services use consistent JWT configuration:
- **Issuer**: FoodDelivery.AuthService
- **Audience**: FoodDelivery.Clients
- **Key**: FoodDelivery_SuperSecret_Key_2024_MustBe32Chars!!

## 🚀 Next Steps

1. Test all endpoints using Swagger UI
2. Verify complete order flow
3. Test role-based access control
4. Explore admin dashboard and reports
5. Test event-driven features with RabbitMQ

## 📞 Support

For issues or questions, refer to the [TESTING_GUIDE.md](TESTING_GUIDE.md) for troubleshooting tips.
