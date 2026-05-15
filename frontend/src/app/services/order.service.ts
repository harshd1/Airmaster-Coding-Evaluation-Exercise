import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Product {
  productId: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  imageUrl?: string;
}

export interface OrderItem {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface OrderRequest {
  userId: string;
  items: OrderItem[];
  shippingAddress: string;
  billingAddress: string;
}

export interface Order {
  orderId: string;
  userId: string;
  createdAt: string;
  totalAmount: number;
  currency: string;
  status: string;
  shippingProvider: string;
  trackingNumber?: string;
  shippingAddress: string;
  billingAddress: string;
  items: OrderItem[];
}

export interface PaymentRequest {
  orderId: string;
  paymentProvider: string;
  token: string;
  amount: number;
  currency: string;
}

export interface PaymentResult {
  success: boolean;
  message: string;
  transactionId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private baseUrl = '/api';

  constructor(private http: HttpClient) {}

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/products`);
  }

  createOrder(request: OrderRequest): Observable<Order> {
    return this.http.post<Order>(`${this.baseUrl}/orders`, request);
  }

  processPayment(request: PaymentRequest): Observable<PaymentResult> {
    return this.http.post<PaymentResult>(`${this.baseUrl}/payments`, request);
  }

  getOrder(orderId: string): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/orders/${orderId}`);
  }

  getUserOrders(userId: string) {
    return this.http.get<Order[]>(`${this.baseUrl}/orders?userId=${userId}`);
  }

  shipOrder(orderId: string) {
    return this.http.post<Order>(`${this.baseUrl}/orders/${orderId}/ship`, {});
  }
}
