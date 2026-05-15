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

app.MapGet("/api/orders/{orderId}", (OrderService orderService, Guid orderId) =>
{
    var order = orderService.GetOrder(orderId);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.MapPost("/api/orders", (OrderService orderService, OrderRequest request) =>
{
    var order = orderService.CreateOrder(request);
    return Results.Created($"/api/orders/{order.OrderId}", order);
});

app.MapPost("/api/payments", (OrderService orderService, PaymentRequest request) =>
{
    var result = orderService.ProcessPayment(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
