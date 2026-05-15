# Airmaster Global E-Commerce Order Management Platform

## Overview

This solution proposes a scalable, fault-tolerant, cost-efficient architecture for a global e-commerce order management platform on Azure. It includes a C# backend API, an Angular frontend, and a service-oriented design suitable for large order volumes, real-time updates, and secure payments.

## Architecture Diagram

```mermaid
flowchart TD
  subgraph Client
    A[Web / Mobile App] -->|HTTPS| B[Azure Front Door / CDN]
  end

  subgraph API
    B --> C[Azure API Management]
    C --> D[Order API Service (ASP.NET Core)]
    C --> E[Product API Service (ASP.NET Core)]
    C --> F[Payment Service (Azure Function)]
    C --> G[Analytics Service (Azure Function)]
    C --> H[Auth Service (Azure AD B2C / OAuth)]
  end

  subgraph Data
    E --> I[(Azure SQL / Cosmos DB Product Catalog)]
    D --> J[(Azure SQL Order Store)]
    F --> K[(Azure SQL Payment Records)]
    D --> L[(Azure Blob / Redis Cache)]
    G --> M[(Azure Data Explorer / Log Analytics)]
  end

  subgraph Integrations
    F -->|Stripe / PayPal API| N[Payment Provider]
    D -->|FedEx / UPS API| O[Shipping Gateway]
  end

  D -->|Event| P[Azure Service Bus]
  P --> Q[Order Processing Worker]
  P --> R[Dead-letter Queue]
  P --> S[Real-time Notifications]
  S --> T[SignalR / WebSocket Hub]

  style A fill:#f9f,stroke:#333,stroke-width:1px
  style B fill:#bbf,stroke:#333,stroke-width:1px
  style C fill:#bfb,stroke:#333,stroke-width:1px
  style D fill:#fdc,stroke:#333,stroke-width:1px
  style E fill:#fdc,stroke:#333,stroke-width:1px
  style F fill:#fdc,stroke:#333,stroke-width:1px
  style G fill:#fdc,stroke:#333,stroke-width:1px
  style H fill:#fea,stroke:#333,stroke-width:1px
  style I fill:#eef,stroke:#333,stroke-width:1px
  style J fill:#eef,stroke:#333,stroke-width:1px
  style M fill:#eef,stroke:#333,stroke-width:1px
  style N fill:#fcc,stroke:#333,stroke-width:1px
  style O fill:#fcc,stroke:#333,stroke-width:1px
  style P fill:#ffe,stroke:#333,stroke-width:1px
  style Q fill:#ffe,stroke:#333,stroke-width:1px
  style R fill:#fcc,stroke:#333,stroke-width:1px
  style S fill:#cff,stroke:#333,stroke-width:1px
  style T fill:#cff,stroke:#333,stroke-width:1px
```

## System Design

### Functional Requirements

- Browse product catalog via web/mobile frontend.
- Place orders and process payments through Stripe/PayPal.
- Real-time order status updates.
- Ship orders via FedEx/UPS.
- Admin analytics dashboard with clickstream and conversion metrics.

### Non-functional Requirements

- Handle 10M+ orders/day with <1s latency for critical checkout and order status flows.
- Support 100K+ concurrent users during spikes.
- Ensure fault tolerance, retries, dead-letter handling, and auto-scaling.
- Enforce PCI-DSS-level security and rate limiting.
- Optimize costs using Azure serverless and managed services.

## Microservice Boundaries

1. **API Gateway / Management**
   - Azure API Management for authentication, rate limiting, and request aggregation.
   - Front Door / CDN for global traffic distribution and caching.

2. **Product Service**
   - Serves product catalog data.
   - Uses cached static data from Azure Cache for Redis.
   - Reads from Azure SQL or Cosmos DB.

3. **Order Service**
   - Handles order creation, updates, status tracking.
   - Persists order and shipment data.
   - Publishes events to Service Bus for downstream processing.

4. **Payment Service**
   - Processes payments via Stripe / PayPal integration.
   - Uses circuit breakers and retries for external APIs.
   - Writes payment success/failure logs.

5. **Shipping Service**
   - Integrates with FedEx/UPS APIs to create shipping labels and track shipments.
   - Updates order shipping status.

6. **Analytics Service**
   - Collects clickstream and conversion events.
   - Stores analytics data in Azure Data Explorer or Log Analytics.

7. **Auth Service**
   - Handles JWT / OAuth token issuance.
   - Uses Azure AD B2C or Identity Server for secure authentication.

## Database Schema

### Product Catalog

- `Product`
  - `ProductId` (GUID)
  - `Name`
  - `Description`
  - `Price`
  - `Currency`
  - `Category`
  - `ImageUrl`
  - `StockQuantity`
  - `IsActive`
  - `UpdatedAt`

### Orders

- `Order`
  - `OrderId` (GUID)
  - `UserId` (GUID)
  - `CreatedAt`
  - `TotalAmount`
  - `Currency`
  - `OrderStatus` (Pending, Paid, Processing, Shipped, Delivered, Failed)
  - `ShippingStatus`
  - `PaymentStatus`
  - `ShippingProvider`
  - `TrackingNumber`
  - `ShippingAddress`
  - `BillingAddress`

### Payment Records

- `PaymentRecord`
  - `PaymentId` (GUID)
  - `OrderId` (GUID)
  - `PaymentProvider` (Stripe, PayPal)
  - `Amount`
  - `Currency`
  - `Status`
  - `TransactionId`
  - `CreatedAt`
  - `LastUpdatedAt`

### Analytics Events

- `AnalyticsEvent`
  - `EventId` (GUID)
  - `UserId` (GUID)
  - `SessionId`
  - `EventType`
  - `PageUrl`
  - `ProductId`
  - `Timestamp`
  - `Metadata`

## Scaling Strategy

- Use Azure Front Door for global request routing and DDoS protection.
- Deploy backend API services to Azure App Service with autoscale rules based on CPU/memory and request queue length.
- Use Azure SQL Hyperscale or Cosmos DB for predictable scalability.
- Cache catalog and frequent reads in Azure Cache for Redis.
- Use Azure Service Bus for asynchronous order event processing and dead-letter handling.
- Use Azure Functions for payments, shipping callbacks, and analytics ingestion.
- Use CDN for static assets and Angular SPA delivery.

## Fault Tolerance

- Circuit breaker patterns around Stripe/PayPal and FedEx/UPS.
- Retry policies with exponential backoff for transient failures.
- Dead-letter queue for failed order processing events.
- Health probes and failover for API services.
- Use multiple availability zones for database and service redundancy.

## Security Considerations

- Enforce TLS for all endpoints.
- JWT/OAuth authentication for users and admin access.
- Role-based authorization for admin dashboards.
- Sensitive card/payment data is handled by Stripe/PayPal tokenization; only non-sensitive references are stored.
- Encrypt data at rest using Azure SQL Transparent Data Encryption and Azure Key Vault for secrets.
- Use API rate limiting and WAF rules.
- Audit logs for order, payment, and admin actions.

## Cost Optimization

- Use Azure Functions for event-driven, low-cost compute workloads.
- Use serverless SQL or Cosmos DB with autoscale when workloads are variable.
- Cache static catalog responses to reduce database reads.
- Use reserved capacity for stable baseline usage.
- Optimize storage and log retention for analytics data.

## Real-Time Order Status

- Use SignalR Service for browser/mobile real-time updates.
- Publish order events to Service Bus and notify connected clients.
- Compare WebSockets vs Kafka:
  - WebSockets / SignalR: best for direct push updates to connected clients, low latency, easier for browser apps.
  - Kafka: best for durable event streaming, analytics, and integrating many consumers. Use Kafka for backend event pipeline and SignalR for end-user notifications.

## GDPR and Data Privacy

- Store only required user data.
- Use soft-delete and data retention policies.
- Encrypt PII at rest.
- Provide user data export/delete functionality.
- Keep data residency controls for region-specific compliance.

## A/B Testing Design

- Use a dedicated experimentation service.
- Route frontend clients to variants using a feature flagging service.
- Capture conversion metrics in analytics.
- Store variant assignments per user and test in separate analytics streams.

## Implementation Notes

- The backend project uses ASP.NET Core Web API to serve orders, catalog, and payment endpoints.
- The frontend project uses Angular components and services to present products, place orders, and show order status.
- This repository is structured to present a realistic architecture and initial implementation for the evaluation exercise.
