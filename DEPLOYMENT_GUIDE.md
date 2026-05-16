# Airmaster Platform - Deployment & Operations Guide

## Quick Start

### Local Development

1. **Start Backend**
   ```powershell
   cd backend
   dotnet run
   ```
   Backend runs on: `http://localhost:5000` (HTTP) or `https://localhost:5001` (HTTPS)

2. **Start Frontend** (in another terminal)
   ```powershell
   cd frontend
   npm install  # Only first time
   npm start
   ```
   Frontend runs on: `http://localhost:4200`

3. **Access Application**
   - Open browser to `http://localhost:4200`
   - Login with demo credentials:
     - Customer: `customer@example.com` / `password123`
     - Admin: `admin@example.com` / `admin123`

## Architecture Components

### Backend Services

| Service | Responsibility | Port |
|---------|---------------|------|
| Product Service | Product catalog | :5000 |
| Order Service | Order management | :5000 |
| Payment Service | Payment processing | :5000 |
| Shipping Service | Shipment tracking | :5000 |
| Analytics Service | Event tracking | :5000 |
| Auth Service | JWT tokens | :5000 |

### Frontend

| Component | Purpose |
|-----------|---------|
| Product List | Browse available products |
| Order Form | Place new orders |
| Payment Form | Process payments |
| Order Tracking | Monitor order status |
| Admin Dashboard | View analytics |

## Configuration

### Backend Configuration (Program.cs)

#### CORS Settings
```csharp
// Allowed origins for frontend
.WithOrigins("http://localhost:4200", "https://localhost:4200")
```

#### Rate Limiting
```csharp
// 100 requests per minute per client
const int RequestsPerMinute = 100;
const int WindowSeconds = 60;
```

#### Payment Circuit Breaker
```csharp
// 5 failures to open circuit
const int FailureThreshold = 5;
// 60 seconds before trying recovery
const int TimeoutSeconds = 60;
```

### Frontend Configuration

#### API Proxy (proxy.conf.json)
```json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  }
}
```

#### Auth Token Storage
```typescript
// LocalStorage key
localStorage.getItem('authToken')
localStorage.setItem('authToken', token)
```

## API Reference

### Authentication Endpoints

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "customer@example.com",
  "password": "password123"
}

Response 200:
{
  "success": true,
  "message": "Login successful.",
  "token": "eyJ...",
  "user": {
    "userId": "00000000-0000-0000-0000-000000000001",
    "email": "customer@example.com",
    "name": "John Customer",
    "role": "Customer"
  }
}
```

#### Get Profile
```http
GET /api/auth/profile
Authorization: Bearer eyJ...

Response 200:
{
  "userId": "00000000-0000-0000-0000-000000000001",
  "email": "customer@example.com",
  "name": "John Customer",
  "role": "Customer"
}
```

### Product Endpoints

#### List Products
```http
GET /api/products

Response 200:
[
  {
    "productId": "...",
    "name": "Airmaster Running Shoes",
    "description": "High-performance shoes",
    "price": 129.99,
    "currency": "USD",
    "imageUrl": "https://example.com/shoes.png",
    "stockQuantity": 200,
    "isActive": true,
    "updatedAt": "2024-01-01T00:00:00Z"
  }
]
```

#### Get Product
```http
GET /api/products/{productId}

Response 200:
{
  "productId": "...",
  "name": "Airmaster Running Shoes",
  ...
}
```

### Order Endpoints

#### Create Order
```http
POST /api/orders
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "userId": "00000000-0000-0000-0000-000000000001",
  "items": [
    {
      "productId": "...",
      "quantity": 2,
      "unitPrice": 129.99
    }
  ],
  "shippingAddress": "123 Market St, San Francisco, CA",
  "billingAddress": "123 Market St, San Francisco, CA"
}

Response 201:
{
  "orderId": "...",
  "userId": "...",
  "createdAt": "2024-01-01T00:00:00Z",
  "totalAmount": 259.98,
  "currency": "USD",
  "status": "Pending",
  "items": [...]
}
```

#### Get Orders
```http
GET /api/orders?userId=00000000-0000-0000-0000-000000000001
Authorization: Bearer eyJ...

Response 200:
[
  {
    "orderId": "...",
    "userId": "...",
    "status": "Pending",
    ...
  }
]
```

#### Get Order Details
```http
GET /api/orders/{orderId}
Authorization: Bearer eyJ...

Response 200:
{
  "orderId": "...",
  "userId": "...",
  ...
}
```

### Payment Endpoints

#### Process Payment
```http
POST /api/payments
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "orderId": "...",
  "paymentProvider": "Stripe",
  "token": "tok_...",
  "amount": 259.98,
  "currency": "USD"
}

Response 200:
{
  "success": true,
  "message": "Payment successful.",
  "transactionId": "..."
}
```

### Shipping Endpoints

#### Ship Order
```http
POST /api/orders/{orderId}/ship
Authorization: Bearer eyJ...

Response 200:
{
  "orderId": "...",
  "status": "Shipped",
  "shippingProvider": "FedEx",
  "trackingNumber": "FDX-..."
}
```

### Analytics Endpoints

#### Get Analytics Dashboard
```http
GET /api/admin/analytics
Authorization: Bearer eyJ... (Admin role required)

Response 200:
{
  "totalEvents": 150,
  "totalProductBrowses": 120,
  "totalOrdersCreated": 25,
  "paymentSuccesses": 23,
  "paymentFailures": 2,
  "conversionRate": 20.8,
  "recentEvents": [...]
}
```

### System Endpoints

#### Health Check
```http
GET /api/health

Response 200:
{
  "status": "Healthy",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## Error Responses

### 400 Bad Request
```json
{
  "error": "Order must contain at least one item."
}
```

### 401 Unauthorized
```json
{
  "error": "Invalid or missing authentication token."
}
```

### 404 Not Found
```json
{
  "error": "Product not found"
}
```

### 429 Too Many Requests
```
Rate limit exceeded. Maximum 100 requests per minute.
```

## Monitoring & Troubleshooting

### Backend Issues

#### Connection Errors
- Verify backend is running: `curl http://localhost:5000/api/health`
- Check port 5000 is available: `netstat -ano | findstr :5000`
- Try HTTPS port: `https://localhost:5001`

#### Rate Limiting
- Check client rate limit status
- Wait for rate limit window to reset (60 seconds)
- Consider load balancing for production

#### Payment Failures
- Circuit breaker may be open after failures
- Automatic recovery after 60 seconds (Half-Open state)
- Check payment token validity

### Frontend Issues

#### CORS Errors
- Verify backend CORS policy allows localhost:4200
- Check network tab in browser developer tools
- Ensure proxy is configured in proxy.conf.json

#### Auth Token Issues
- Clear browser localStorage
- Re-login to get new token
- Check token expiration (1 hour in demo)

## Performance Optimization

### Caching Strategy
- Product catalog: Cache for 1 hour (static data)
- User orders: Cache for 5 minutes
- Analytics: Real-time, no caching

### Rate Limiting Benefits
- Prevents DDoS attacks
- Ensures fair resource usage
- Protects payment processing

### Circuit Breaker Benefits
- Fast failure detection
- Automatic recovery
- Graceful degradation

## Security Checklist

- [x] HTTPS enforced in production
- [x] CORS configured for specific origins
- [x] JWT tokens validated on all protected endpoints
- [x] Rate limiting prevents abuse
- [x] Input validation on all forms
- [x] No sensitive data in logs
- [x] Payment tokens not stored in database

## Production Deployment

### Azure Deployment

1. **Deploy Backend to App Service**
   ```bash
   dotnet publish -c Release
   # Upload to Azure App Service
   ```

2. **Deploy Frontend to Static Web App or Azure Storage + CDN**
   ```bash
   npm run build
   # Upload dist/ folder
   ```

3. **Configure Azure SQL Database**
   - Migrate from in-memory to persistent storage
   - Set up proper backups
   - Enable transparent data encryption

4. **Enable Azure Application Insights**
   - Monitor performance
   - Track exceptions
   - Analyze user behavior

5. **Set Up Azure Key Vault**
   - Store JWT signing keys
   - Store connection strings
   - Enable managed identities

6. **Configure API Management**
   - Add API rate limiting
   - Enable authentication policies
   - Set up analytics

## Maintenance

### Regular Tasks
- Monitor application logs
- Review analytics dashboard
- Update dependencies
- Test disaster recovery

### Database Maintenance
- Backup strategy (daily)
- Index optimization
- Archive old analytics data

### Security Updates
- Apply security patches
- Review authentication logs
- Audit access controls
