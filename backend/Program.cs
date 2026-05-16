using AirmasterOrderApi.Models;
using AirmasterOrderApi.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<OrderService>();
builder.Services.AddSingleton<AnalyticsService>();
builder.Services.AddSingleton<CircuitBreakerService>();
builder.Services.AddSingleton<AuthenticationService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for frontend communication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add authentication (JWT)
builder.Services.AddAuthorization();

// Add rate limiting
builder.Services.AddKeyedScoped<RateLimiter>("DefaultRateLimiter");

var app = builder.Build();

// Add middleware
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Rate limiting middleware
app.Use(async (context, next) =>
{
    var rateLimiter = context.RequestServices.GetRequiredKeyedService<RateLimiter>("DefaultRateLimiter");
    var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    
    if (!rateLimiter.IsAllowed(clientId))
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        await context.Response.WriteAsync("Rate limit exceeded. Maximum 100 requests per minute.");
        return;
    }
    
    await next();
});

// Authentication endpoints
app.MapPost("/api/auth/login", (AuthenticationService auth, LoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Email and password are required." });
    }

    var response = auth.Login(request);
    return response.Success ? Results.Ok(response) : Results.Unauthorized();
});

app.MapGet("/api/auth/profile", (AuthenticationService auth, HttpContext context) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
    {
        return Results.Unauthorized();
    }

    var token = authHeader.Substring("Bearer ".Length).Trim();
    var user = auth.ValidateToken(token);

    return user != null ? Results.Ok(user) : Results.Unauthorized();
});

app.MapGet("/api/products", (OrderService orderService, AnalyticsService analytics) => 
{
    analytics.LogEvent(new AnalyticsEvent 
    { 
        EventType = "ProductBrowse", 
        PageUrl = "/products",
        Timestamp = DateTime.UtcNow 
    });
    return Results.Ok(orderService.GetProducts());
});

app.MapGet("/api/products/{id}", (OrderService orderService, Guid id) =>
{
    var product = orderService.GetProduct(id);
    return product is null ? Results.NotFound(new { error = "Product not found" }) : Results.Ok(product);
});

app.MapGet("/api/orders", (OrderService orderService, Guid? userId) =>
{
    if (userId is null)
    {
        return Results.BadRequest(new { error = "Missing userId query parameter." });
    }

    var orders = orderService.GetOrdersByUser(userId.Value);
    return Results.Ok(orders);
});

app.MapGet("/api/orders/{orderId}", (OrderService orderService, Guid orderId) =>
{
    var order = orderService.GetOrder(orderId);
    return order is null ? Results.NotFound(new { error = "Order not found" }) : Results.Ok(order);
});

app.MapPost("/api/orders", (OrderService orderService, AnalyticsService analytics, OrderRequest request) =>
{
    // Validate input
    if (string.IsNullOrWhiteSpace(request.ShippingAddress) || string.IsNullOrWhiteSpace(request.BillingAddress))
    {
        return Results.BadRequest(new { error = "Shipping and billing addresses are required." });
    }

    if (!orderService.TryCreateOrder(request, out var order, out var error))
    {
        return Results.BadRequest(new { error });
    }

    analytics.LogEvent(new AnalyticsEvent 
    { 
        EventType = "OrderCreated", 
        PageUrl = "/checkout",
        Metadata = $"OrderId={order.OrderId}",
        Timestamp = DateTime.UtcNow 
    });

    return Results.Created($"/api/orders/{order.OrderId}", order);
});

app.MapPost("/api/payments", (OrderService orderService, CircuitBreakerService circuitBreaker, AnalyticsService analytics, PaymentRequest request) =>
{
    // Validate input
    if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Token))
    {
        return Results.BadRequest(new { error = "Invalid payment details provided." });
    }

    // Use circuit breaker for payment processing
    var result = circuitBreaker.ProcessPaymentWithRetry(() => 
        orderService.ProcessPayment(request), 
        3);
    
    if (!result.Success)
    {
        analytics.LogEvent(new AnalyticsEvent 
        { 
            EventType = "PaymentFailed", 
            PageUrl = "/checkout",
            Metadata = $"OrderId={request.OrderId}",
            Timestamp = DateTime.UtcNow 
        });
        return Results.BadRequest(result);
    }

    analytics.LogEvent(new AnalyticsEvent 
    { 
        EventType = "PaymentSuccess", 
        PageUrl = "/checkout",
        Metadata = $"OrderId={request.OrderId},Amount={request.Amount}",
        Timestamp = DateTime.UtcNow 
    });

    return Results.Ok(result);
});

app.MapPost("/api/orders/{orderId}/ship", (OrderService orderService, Guid orderId) =>
{
    if (!orderService.TryShipOrder(orderId, out var order, out var error))
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(order);
});

// Analytics endpoint for admin dashboard
app.MapGet("/api/admin/analytics", (AnalyticsService analytics) =>
{
    var stats = analytics.GetAnalyticsStats();
    return Results.Ok(stats);
});

app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();
