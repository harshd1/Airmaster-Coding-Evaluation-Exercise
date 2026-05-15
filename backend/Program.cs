using AirmasterOrderApi.Models;
using AirmasterOrderApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OrderService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/products", (OrderService orderService) => Results.Ok(orderService.GetProducts()));
app.MapGet("/api/products/{id}", (OrderService orderService, Guid id) =>
{
    var product = orderService.GetProduct(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
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
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.MapPost("/api/orders", (OrderService orderService, OrderRequest request) =>
{
    if (!orderService.TryCreateOrder(request, out var order, out var error))
    {
        return Results.BadRequest(new { error });
    }

    return Results.Created($"/api/orders/{order.OrderId}", order);
});

app.MapPost("/api/payments", (OrderService orderService, PaymentRequest request) =>
{
    var result = orderService.ProcessPayment(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/orders/{orderId}/ship", (OrderService orderService, Guid orderId) =>
{
    if (!orderService.TryShipOrder(orderId, out var order, out var error))
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(order);
});

app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
