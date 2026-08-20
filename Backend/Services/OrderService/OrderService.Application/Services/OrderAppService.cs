using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using FoodDelivery.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.Saga;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class OrderAppService : IOrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICartRepository _cartRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IOrderSaga _saga;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private const decimal GstRate = 0.05m;
    private const decimal DeliveryFeeFlat = 30.00m;
    private const decimal PlatformFee = 15.00m; // Fixed Rs. 15 platform fee
    private const decimal RestaurantCommissionRate = 0.15m; // 15% commission

    public OrderAppService(
        IOrderRepository orderRepo,
        ICartRepository cartRepo,
        IPaymentRepository paymentRepo,
        IOrderSaga saga,
        IRabbitMqPublisher publisher,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _orderRepo = orderRepo;
        _cartRepo = cartRepo;
        _paymentRepo = paymentRepo;
        _saga = saga;
        _publisher = publisher;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<OrderDto> PlaceOrderAsync(Guid customerId, PlaceOrderDto dto)
    {
        var cart = await _cartRepo.GetByCustomerIdAsync(customerId)
            ?? throw new InvalidOperationException("Cart is empty. Add items before placing an order.");

        if (!cart.Items.Any())
            throw new InvalidOperationException("Cart is empty.");

        // Validate restaurant is open before allowing order
        if (cart.RestaurantId != Guid.Empty)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync($"http://localhost:5003/api/catalog/restaurants/{cart.RestaurantId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var restaurantResponse = System.Text.Json.JsonSerializer.Deserialize<dynamic>(content);
                    
                    // Check if restaurant is open (manual toggle)
                    var restaurantData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                    if (restaurantData.TryGetProperty("data", out var dataElement))
                    {
                        if (dataElement.TryGetProperty("isOpen", out var isOpenElement))
                        {
                            bool isOpen = isOpenElement.GetBoolean();
                            if (!isOpen)
                            {
                                throw new InvalidOperationException("Restaurant is currently closed. Please try again when the restaurant is open.");
                            }
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our custom exception
            }
            catch (Exception)
            {
                // Continue with order placement if validation fails (fail-open for availability)
            }
        }

        var allowed = new[] { "COD", "CARD", "WALLET" };
        if (!allowed.Contains(dto.PaymentMethod.ToUpperInvariant()))
            throw new ArgumentException("Invalid payment method. Allowed: COD, Card, Wallet.");

        // For Card, payment details must be provided (payment already completed via Razorpay)
        // For Wallet, we'll deduct from wallet balance (no Razorpay needed)
        bool paymentCompleted = false;
        var paymentMethodUpper = dto.PaymentMethod.ToUpperInvariant();
        
        if (paymentMethodUpper == "CARD")
        {
            // Card payments require Razorpay details
            if (string.IsNullOrEmpty(dto.RazorpayOrderId) || 
                string.IsNullOrEmpty(dto.RazorpayPaymentId) || 
                string.IsNullOrEmpty(dto.RazorpaySignature))
            {
                throw new ArgumentException("Payment must be completed before placing order for Card payments.");
            }
            paymentCompleted = true;
        }
        else if (paymentMethodUpper == "WALLET")
        {
            // Wallet payments: deduct from wallet balance
            // Calculate total amount first
            var cartSubtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
            var cartGst = Math.Round(cartSubtotal * GstRate, 2);
            var cartTotal = cartSubtotal + DeliveryFeeFlat + cartGst + PlatformFee - cart.Discount;
            
            // Deduct from wallet via AuthService
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var deductRequest = new
                {
                    userId = customerId,
                    amount = cartTotal,
                    description = $"Order payment via wallet"
                };

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(deductRequest),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync("http://localhost:5001/api/auth/wallet/deduct", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Insufficient wallet balance or wallet deduction failed: {errorContent}");
                }
                
                paymentCompleted = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deduct from wallet: {ex.Message}");
            }
        }

        // Group items by restaurant
        var itemsByRestaurant = cart.Items.GroupBy(i => i.RestaurantId).ToList();
        
        // If multiple restaurants, create separate orders for each
        if (itemsByRestaurant.Count > 1)
        {
            var orders = new List<Order>();
            foreach (var restaurantGroup in itemsByRestaurant)
            {
                var restId = restaurantGroup.Key;
                var restItems = restaurantGroup.ToList();
                
                var restSubtotal = restItems.Sum(i => i.UnitPrice * i.Quantity);
                var restGst = Math.Round(restSubtotal * GstRate, 2);
                var restDeliveryFee = DeliveryFeeFlat;
                var restDiscount = itemsByRestaurant.Count > 1 ? 0 : cart.Discount; // Apply discount only to first order
                var restPlatformFee = PlatformFee;
                var restCommission = Math.Round(restSubtotal * RestaurantCommissionRate, 2);
                var restTotal = restSubtotal + restDeliveryFee + restGst + restPlatformFee - restDiscount;

                var restSagaRequest = new OrderSagaRequest
                {
                    CustomerId = customerId,
                    RestaurantId = restId,
                    RestaurantName = string.Empty, // Will be enriched
                    DeliveryAddress = dto.DeliveryAddress,
                    DeliveryInstructions = dto.DeliveryInstructions,
                    PaymentMethod = dto.PaymentMethod.ToUpperInvariant(),
                    CouponCode = restDiscount > 0 ? cart.CouponCode : null,
                    Subtotal = restSubtotal,
                    DeliveryFee = restDeliveryFee,
                    Discount = restDiscount,
                    GstAmount = restGst,
                    PlatformFee = restPlatformFee,
                    RestaurantCommission = restCommission,
                    TotalAmount = restTotal,
                    PaymentAlreadyCompleted = paymentCompleted,
                    Items = restItems.Select(i => new SagaOrderItem
                    {
                        MenuItemId = i.MenuItemId,
                        Name = i.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        IsVeg = i.IsVeg
                    }).ToList()
                };

                var restResult = await _saga.ExecuteAsync(restSagaRequest);
                if (!restResult.Success)
                    throw new InvalidOperationException(restResult.Message);

                var restOrder = await _orderRepo.GetByIdAsync(restResult.OrderId!.Value);
                if (restOrder != null)
                {
                    restOrder.EstimatedPreparationMinutes = 30;
                    restOrder.EstimatedDeliveryTime = DateTime.UtcNow
                        .AddMinutes(restOrder.EstimatedPreparationMinutes)
                        .AddMinutes(20);
                    await _orderRepo.UpdateAsync(restOrder);
                    orders.Add(restOrder);
                }
            }
            
            await _orderRepo.SaveChangesAsync();
            
            // Clear cart after successful orders
            await _cartRepo.DeleteAsync(customerId);
            await _cartRepo.SaveChangesAsync();
            
            // Return the first order (or we could return a list)
            return MapToDto(orders.First());
        }

        // Single restaurant - original logic
        var restaurantId = cart.Items.First().RestaurantId;
        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var gst = Math.Round(subtotal * GstRate, 2);
        var platformFee = PlatformFee;
        var commission = Math.Round(subtotal * RestaurantCommissionRate, 2);
        var total = subtotal + DeliveryFeeFlat + gst + platformFee - cart.Discount;

        // Fetch customer name/email and restaurant name for order enrichment
        string customerName = string.Empty;
        string customerEmail = string.Empty;
        string restaurantName = string.Empty;
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            // Fetch customer info from AuthService
            var authUrl = _configuration["Services:AuthService"] ?? "http://localhost:5001";
            var customerRes = await httpClient.GetAsync($"{authUrl}/api/auth/users/{customerId}/info");
            if (customerRes.IsSuccessStatusCode)
            {
                var cJson = System.Text.Json.JsonDocument.Parse(await customerRes.Content.ReadAsStringAsync());
                if (cJson.RootElement.TryGetProperty("data", out var cData))
                {
                    customerName = cData.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "" : "";
                    customerEmail = cData.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
                }
            }
            // Fetch restaurant name from CatalogService
            var catalogUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";
            var restRes = await httpClient.GetAsync($"{catalogUrl}/api/catalog/restaurants/{restaurantId}");
            if (restRes.IsSuccessStatusCode)
            {
                var rJson = System.Text.Json.JsonDocument.Parse(await restRes.Content.ReadAsStringAsync());
                if (rJson.RootElement.TryGetProperty("data", out var rData))
                    restaurantName = rData.TryGetProperty("name", out var rn) ? rn.GetString() ?? "" : "";
            }
        }
        catch { /* fail-open: names are cosmetic, don't block order placement */ }

        var sagaRequest = new OrderSagaRequest
        {
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            RestaurantId = restaurantId,
            RestaurantName = restaurantName,
            DeliveryAddress = dto.DeliveryAddress,
            DeliveryInstructions = dto.DeliveryInstructions,
            PaymentMethod = dto.PaymentMethod.ToUpperInvariant(),
            CouponCode = cart.CouponCode,
            Subtotal = subtotal,
            DeliveryFee = DeliveryFeeFlat,
            Discount = cart.Discount,
            GstAmount = gst,
            PlatformFee = platformFee,
            RestaurantCommission = commission,
            TotalAmount = total,
            PaymentAlreadyCompleted = paymentCompleted,
            Items = cart.Items.Select(i => new SagaOrderItem
            {
                MenuItemId = i.MenuItemId,
                Name = i.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                IsVeg = i.IsVeg
            }).ToList()
        };

        var result = await _saga.ExecuteAsync(sagaRequest);
        if (!result.Success)
            throw new InvalidOperationException(result.Message);

        // Update order with time estimates
        var order = await _orderRepo.GetByIdAsync(result.OrderId!.Value);
        if (order != null)
        {
            order.EstimatedPreparationMinutes = 30;
            order.EstimatedDeliveryTime = DateTime.UtcNow
                .AddMinutes(order.EstimatedPreparationMinutes)
                .AddMinutes(20);
            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveChangesAsync();
        }

        // Clear cart after successful order
        await _cartRepo.DeleteAsync(customerId);
        await _cartRepo.SaveChangesAsync();

        var orderWithDetails = await _orderRepo.GetByIdWithDetailsAsync(result.OrderId!.Value)
            ?? throw new InvalidOperationException("Order created but could not be retrieved.");

        return MapToDto(orderWithDetails);
    }

    public async Task<OrderDto> GetByIdAsync(Guid orderId, Guid requesterId, string role)
    {
        var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        // Customers can only see their own orders
        if (role == "Customer" && order.CustomerId != requesterId)
            throw new UnauthorizedAccessException("Access denied.");

        // Partners can only see orders for their restaurant
        if (role == "Partner")
        {
            // Get the partner's restaurant ID by calling CatalogService
            var restaurantId = await GetRestaurantIdByPartnerUserIdAsync(requesterId);
            if (restaurantId == null || order.RestaurantId != restaurantId.Value)
                throw new UnauthorizedAccessException("Access denied.");
        }

        return MapToDto(order);
    }

    private async Task<Guid?> GetRestaurantIdByPartnerUserIdAsync(Guid partnerUserId)
    {
        try
        {
            var catalogServiceUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";
            var client = _httpClientFactory.CreateClient();
            
            var response = await client.GetAsync($"{catalogServiceUrl}/api/catalog/restaurants/by-partner/{partnerUserId}");
            
            if (!response.IsSuccessStatusCode)
                return null;
            
            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            
            // Assuming the response is wrapped in ApiResponse<RestaurantDto>
            if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("id", out var idElement))
            {
                return Guid.Parse(idElement.GetString()!);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<OrderDto>> GetMyOrdersAsync(Guid customerId)
    {
        var orders = await _orderRepo.GetByCustomerIdAsync(customerId);
        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetByRestaurantIdAsync(Guid restaurantId)
    {
        var orders = await _orderRepo.GetByRestaurantIdAsync(restaurantId);
        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetAllAsync()
    {
        var orders = await _orderRepo.GetAllAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<PagedOrdersDto> SearchOrdersAsync(Guid customerId, OrderSearchDto search)
    {
        var (orders, totalCount) = await _orderRepo.SearchOrdersAsync(
            search.OrderNumber,
            search.Status,
            search.FromDate,
            search.ToDate,
            search.RestaurantId,
            search.Page,
            search.PageSize);
        
        var orderDtos = orders.Select(MapToDto).ToList();
        
        return new PagedOrdersDto
        {
            Orders = orderDtos,
            TotalCount = totalCount,
            Page = search.Page,
            PageSize = search.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)search.PageSize)
        };
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto, string role)
    {
        var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        if (!Enum.TryParse<OrderStatus>(dto.NewStatus, ignoreCase: true, out var newStatus))
            throw new ArgumentException($"Invalid status '{dto.NewStatus}'.");

        if (!OrderStatusTransitions.IsValid(order.Status, newStatus, role))
            throw new InvalidOperationException(
                $"Role '{role}' cannot move order from '{order.Status}' to '{newStatus}'.");

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Reason))
            order.CancellationReason = dto.Reason;

        // Publish ReadyForPickup event so delivery agents are notified
        if (newStatus == OrderStatus.ReadyForPickup)
        {
            _publisher.Publish(new OrderReadyForPickupEvent
            {
                OrderId = order.Id,
                RestaurantId = order.RestaurantId,
                RestaurantName = order.RestaurantName,
                DeliveryAddress = order.DeliveryAddress,
                TotalAmount = order.TotalAmount
            }, QueueNames.OrderReadyForPickup);
        }

        await _orderRepo.UpdateAsync(order);
        await _orderRepo.SaveChangesAsync();
        return MapToDto(order);
    }

    // ── Mapping ───────────────────────────────────────────────────────

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        CustomerName = o.CustomerName,
        CustomerEmail = o.CustomerEmail,
        RestaurantId = o.RestaurantId,
        RestaurantName = o.RestaurantName,
        DeliveryAddress = o.DeliveryAddress,
        DeliveryInstructions = o.DeliveryInstructions,
        CouponCode = o.CouponCode,
        Subtotal = o.Subtotal,
        DeliveryFee = o.DeliveryFee,
        Discount = o.Discount,
        GstAmount = o.GstAmount,
        PlatformFee = o.PlatformFee,
        RestaurantCommission = o.RestaurantCommission,
        TotalAmount = o.TotalAmount,
        PaymentMethod = o.PaymentMethod,
        Status = o.Status.ToString(),
        CancellationReason = o.CancellationReason,
        RejectionReason = o.RejectionReason,  // Add this field
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        EstimatedPreparationMinutes = o.EstimatedPreparationMinutes,
        EstimatedDeliveryTime = o.EstimatedDeliveryTime,
        ActualDeliveryTime = o.ActualDeliveryTime,
        IsDelayed = o.IsDelayed,
        Items = o.Items.Select(i => new DTOs.OrderItemDto
        {
            Id = i.Id,
            MenuItemId = i.MenuItemId,
            Name = i.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.UnitPrice * i.Quantity,
            IsVeg = i.IsVeg
        }).ToList(),
        Payment = o.Payment is null ? null : new PaymentSummaryDto
        {
            Id = o.Payment.Id,
            Amount = o.Payment.Amount,
            Method = o.Payment.Method,
            Status = o.Payment.Status.ToString(),
            TransactionId = o.Payment.TransactionId,
            PaidAt = o.Payment.PaidAt
        },
        DeliveryAssignment = o.DeliveryAssignment is null ? null : new DeliveryAssignmentDto
        {
            Id = o.DeliveryAssignment.Id,
            OrderId = o.DeliveryAssignment.OrderId,
            AgentId = o.DeliveryAssignment.AgentId,
            AgentName = o.DeliveryAssignment.AgentName,
            AgentMobile = o.DeliveryAssignment.AgentMobile,
            Status = o.DeliveryAssignment.Status.ToString(),
            AssignedAt = o.DeliveryAssignment.AssignedAt,
            PickedUpAt = o.DeliveryAssignment.PickedUpAt,
            OutForDeliveryAt = o.DeliveryAssignment.OutForDeliveryAt,
            DeliveredAt = o.DeliveryAssignment.DeliveredAt,
            EstimatedArrivalTime = o.DeliveryAssignment.EstimatedArrivalTime,
            ActualArrivalTime = o.DeliveryAssignment.ActualArrivalTime
        },
        Rating = o.Rating is null ? null : new OrderRatingDto
        {
            Id = o.Rating.Id,
            OrderId = o.Rating.OrderId,
            CustomerId = o.Rating.CustomerId,
            FoodRating = o.Rating.FoodRating,
            DeliveryRating = o.Rating.DeliveryRating,
            AverageRating = Math.Round((o.Rating.FoodRating + o.Rating.DeliveryRating) / 2.0, 1),
            Comment = o.Rating.Comment,
            Tags = string.IsNullOrEmpty(o.Rating.Tags) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(o.Rating.Tags) ?? new List<string>(),
            Photos = string.IsNullOrEmpty(o.Rating.Photos) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(o.Rating.Photos) ?? new List<string>(),
            CreatedAt = o.Rating.CreatedAt,
            UpdatedAt = o.Rating.UpdatedAt
        }
    };

    // ── New Methods for Order Management ──────────────────────────────

    public async Task<OrderDto> RejectOrderAsync(Guid orderId, string rejectionReason, Guid partnerUserId)
    {
        var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        // Verify partner owns this restaurant
        var restaurantId = await GetRestaurantIdByPartnerUserIdAsync(partnerUserId);
        if (restaurantId == null || order.RestaurantId != restaurantId.Value)
            throw new UnauthorizedAccessException("Access denied.");

        // Only allow rejection from certain statuses
        var allowedStatuses = new[] { OrderStatus.Paid, OrderStatus.AwaitingAcceptance };
        if (!allowedStatuses.Contains(order.Status))
            throw new InvalidOperationException($"Cannot reject order with status '{order.Status}'.");

        order.Status = OrderStatus.RestaurantRejected;
        order.RejectionReason = rejectionReason;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepo.UpdateAsync(order);
        await _orderRepo.SaveChangesAsync();

        // Publish event for refund if payment was made
        if (order.Payment != null && order.Payment.Status == PaymentStatus.Success)
        {
            _publisher.Publish(new OrderRejectedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                RestaurantId = order.RestaurantId,
                TotalAmount = order.TotalAmount,
                RejectionReason = rejectionReason,
                PaymentId = order.Payment.Id
            }, QueueNames.OrderRejected);
        }

        return MapToDto(order);
    }

    public async Task<bool> SoftDeleteOrderAsync(Guid orderId, Guid customerId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        // Verify customer owns this order
        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Access denied.");

        // Only allow deletion of completed, cancelled, or rejected orders
        var allowedStatuses = new[] { OrderStatus.Delivered, OrderStatus.Cancelled, OrderStatus.RestaurantRejected, OrderStatus.PaymentFailed };
        if (!allowedStatuses.Contains(order.Status))
            throw new InvalidOperationException("Cannot delete active orders.");

        order.IsDeletedByCustomer = true;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepo.UpdateAsync(order);
        await _orderRepo.SaveChangesAsync();

        return true;
    }

    public async Task<ReorderResponseDto> ReorderAsync(Guid orderId, Guid customerId)
    {
        var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        // Verify customer owns this order
        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Access denied.");

        // Get existing cart (without tracking to avoid conflicts)
        var existingCart = await _cartRepo.GetByCustomerIdAsync(customerId);
        
        if (existingCart != null)
        {
            // If cart exists, delete it first to avoid concurrency issues
            await _cartRepo.DeleteAsync(customerId);
            await _cartRepo.SaveChangesAsync();
        }

        // Create a fresh cart with order items
        var cart = new Cart
        {
            CustomerId = customerId,
            RestaurantId = order.RestaurantId,
            UpdatedAt = DateTime.UtcNow
        };

        // Add all order items to the new cart
        foreach (var item in order.Items)
        {
            cart.Items.Add(new CartItem
            {
                MenuItemId = item.MenuItemId,
                RestaurantId = order.RestaurantId,
                Name = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                IsVeg = item.IsVeg
            });
        }

        // Add the new cart
        await _cartRepo.AddAsync(cart);
        await _cartRepo.SaveChangesAsync();

        // Soft delete the original order so it doesn't show in customer's order list anymore
        // This makes sense because they've reordered it - no need to keep seeing the old rejected order
        order.IsDeletedByCustomer = true;
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order);
        await _orderRepo.SaveChangesAsync();

        return new ReorderResponseDto
        {
            Success = true,
            Message = "Items added to cart successfully",
            CartId = cart.Id,
            ItemsAdded = order.Items.Count
        };
    }

    public async Task<List<OrderDto>> GetMyOrdersFilteredAsync(Guid customerId, string? statusFilter)
    {
        var orders = await _orderRepo.GetByCustomerIdAsync(customerId);
        
        // Filter out soft-deleted orders
        orders = orders.Where(o => !o.IsDeletedByCustomer).ToList();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            switch (statusFilter.ToLower())
            {
                case "active":
                    orders = orders.Where(o => new[] { 
                        OrderStatus.PaymentPending, 
                        OrderStatus.Paid, 
                        OrderStatus.AwaitingAcceptance,
                        OrderStatus.Accepted, 
                        OrderStatus.Preparing, 
                        OrderStatus.ReadyForPickup,
                        OrderStatus.PickedUp,
                        OrderStatus.OutForDelivery 
                    }.Contains(o.Status)).ToList();
                    break;
                case "completed":
                    orders = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
                    break;
                case "rejected":
                    orders = orders.Where(o => new[] { 
                        OrderStatus.RestaurantRejected, 
                        OrderStatus.Cancelled,
                        OrderStatus.PaymentFailed 
                    }.Contains(o.Status)).ToList();
                    break;
            }
        }

        return orders.Select(MapToDto).OrderByDescending(o => o.CreatedAt).ToList();
    }

    public async Task<List<OrderDto>> GetRestaurantOrdersFilteredAsync(Guid restaurantId, string? statusFilter)
    {
        var orders = await _orderRepo.GetByRestaurantIdAsync(restaurantId);

        // Partners should see all orders except system-level statuses they can't control
        orders = orders.Where(o => 
            o.Status != OrderStatus.DraftCart && 
            o.Status != OrderStatus.CheckoutStarted &&
            o.Status != OrderStatus.PaymentPending &&
            o.Status != OrderStatus.CancelRequested &&
            o.Status != OrderStatus.RefundInitiated &&
            o.Status != OrderStatus.Refunded &&
            o.Status != OrderStatus.RefundRejected
        ).ToList();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            switch (statusFilter.ToLower())
            {
                case "new":
                    orders = orders.Where(o => new[] { 
                        OrderStatus.Paid, 
                        OrderStatus.AwaitingAcceptance 
                    }.Contains(o.Status)).ToList();
                    break;
                case "inprogress":
                    orders = orders.Where(o => new[] { 
                        OrderStatus.Accepted, 
                        OrderStatus.Preparing, 
                        OrderStatus.ReadyForPickup 
                    }.Contains(o.Status)).ToList();
                    break;
                case "completed":
                    orders = orders.Where(o => new[] {
                        OrderStatus.Delivered,
                        OrderStatus.Cancelled,
                        OrderStatus.RestaurantRejected,
                        OrderStatus.PaymentFailed
                    }.Contains(o.Status)).ToList();
                    break;
            }
        }

        return orders.Select(MapToDto).OrderByDescending(o => o.CreatedAt).ToList();
    }

    public async Task<OrderDto> CancelOrderAsync(Guid orderId, Guid customerId, string reason)
    {
        var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        // Verify customer owns this order
        if (order.CustomerId != customerId)
            throw new UnauthorizedAccessException("Access denied.");

        // Only allow cancellation from certain statuses
        var allowedStatuses = new[] { OrderStatus.Paid, OrderStatus.AwaitingAcceptance, OrderStatus.PaymentPending };
        if (!allowedStatuses.Contains(order.Status))
            throw new InvalidOperationException($"Cannot cancel order with status '{order.Status}'. Orders can only be cancelled before restaurant acceptance.");

        // Update order status
        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = reason;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledBy = customerId;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepo.UpdateAsync(order);
        await _orderRepo.SaveChangesAsync();

        // If payment was made (not COD), create refund request with smart calculation
        if (order.Payment != null && order.Payment.Status == PaymentStatus.Success && order.PaymentMethod.ToUpperInvariant() != "COD")
        {
            // Calculate refund using smart calculator
            var (refundAmount, platformFee, cancellationCharge) = 
                RefundCalculator.CalculateRefund(order.TotalAmount, order.PlatformFee);

            var refundRequest = new RefundRequest
            {
                OrderId = order.Id,
                CustomerId = customerId,
                OriginalAmount = order.TotalAmount,
                PlatformFee = platformFee,
                CancellationCharge = cancellationCharge,
                RefundAmount = refundAmount,
                Status = RefundStatus.PendingApproval,
                RequestedAt = DateTime.UtcNow
            };

            // Add refund request to database
            await _orderRepo.AddRefundRequestAsync(refundRequest);
            await _orderRepo.SaveChangesAsync();

            // Publish event for admin notification
            _publisher.Publish(new OrderCancelledEvent
            {
                OrderId = order.Id,
                CustomerId = customerId,
                RestaurantId = order.RestaurantId,
                TotalAmount = order.TotalAmount,
                Reason = reason,
                RefundRequired = true,
                RefundRequestId = refundRequest.Id
            }, QueueNames.OrderCancelled);
        }
        else
        {
            // COD order - no refund needed
            _publisher.Publish(new OrderCancelledEvent
            {
                OrderId = order.Id,
                CustomerId = customerId,
                RestaurantId = order.RestaurantId,
                TotalAmount = order.TotalAmount,
                Reason = reason,
                RefundRequired = false
            }, QueueNames.OrderCancelled);
        }

        return MapToDto(order);
    }

    public async Task<int> BackfillOrderNamesAsync()
    {
        var orders = await _orderRepo.GetAllAsync();
        var ordersToUpdate = orders.Where(o => string.IsNullOrEmpty(o.CustomerName) || string.IsNullOrEmpty(o.RestaurantName)).ToList();
        int updated = 0;

        var httpClient = _httpClientFactory.CreateClient();
        var authUrl = _configuration["Services:AuthService"] ?? "http://localhost:5001";
        var catalogUrl = _configuration["Services:CatalogService"] ?? "http://localhost:5002";

        // Cache to avoid repeated calls for same customer/restaurant
        var customerCache = new Dictionary<Guid, (string name, string email)>();
        var restaurantCache = new Dictionary<Guid, string>();

        foreach (var order in ordersToUpdate)
        {
            bool changed = false;

            // Fetch customer name if missing
            if (string.IsNullOrEmpty(order.CustomerName))
            {
                if (!customerCache.TryGetValue(order.CustomerId, out var customerInfo))
                {
                    try
                    {
                        var res = await httpClient.GetAsync($"{authUrl}/api/auth/users/{order.CustomerId}/info");
                        if (res.IsSuccessStatusCode)
                        {
                            var json = System.Text.Json.JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                            if (json.RootElement.TryGetProperty("data", out var d))
                            {
                                var name = d.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "" : "";
                                var email = d.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
                                customerInfo = (name, email);
                                customerCache[order.CustomerId] = customerInfo;
                            }
                        }
                    }
                    catch { }
                }
                if (!string.IsNullOrEmpty(customerInfo.name))
                {
                    order.CustomerName = customerInfo.name;
                    order.CustomerEmail = customerInfo.email;
                    changed = true;
                }
            }

            // Fetch restaurant name if missing
            if (string.IsNullOrEmpty(order.RestaurantName))
            {
                if (!restaurantCache.TryGetValue(order.RestaurantId, out var rName))
                {
                    try
                    {
                        var res = await httpClient.GetAsync($"{catalogUrl}/api/catalog/restaurants/{order.RestaurantId}");
                        if (res.IsSuccessStatusCode)
                        {
                            var json = System.Text.Json.JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                            if (json.RootElement.TryGetProperty("data", out var d))
                                rName = d.TryGetProperty("name", out var rn) ? rn.GetString() ?? "" : "";
                            restaurantCache[order.RestaurantId] = rName;
                        }
                    }
                    catch { }
                }
                if (!string.IsNullOrEmpty(rName))
                {
                    order.RestaurantName = rName;
                    changed = true;
                }
            }

            if (changed)
            {
                order.UpdatedAt = DateTime.UtcNow;
                await _orderRepo.UpdateAsync(order);
                updated++;
            }
        }

        if (updated > 0)
            await _orderRepo.SaveChangesAsync();

        return updated;
    }
}

