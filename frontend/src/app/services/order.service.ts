import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';

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

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  token: string;
  user: {
    userId: string;
    email: string;
    name: string;
    role: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private baseUrl = '/api';
  private authTokenSubject = new BehaviorSubject<string | null>(this.getToken());
  public authToken$ = this.authTokenSubject.asObservable();

  constructor(private http: HttpClient) {}

  private getToken(): string | null {
    return localStorage.getItem('authToken');
  }

  private setToken(token: string): void {
    localStorage.setItem('authToken', token);
    this.authTokenSubject.next(token);
  }

  private getHeaders(): HttpHeaders {
    const token = this.getToken();
    return new HttpHeaders({
      'Authorization': token ? `Bearer ${token}` : ''
    });
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/login`, { email, password });
  }

  logout(): void {
    localStorage.removeItem('authToken');
    this.authTokenSubject.next(null);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/products`);
  }

  createOrder(request: OrderRequest): Observable<Order> {
    return this.http.post<Order>(`${this.baseUrl}/orders`, request, {
      headers: this.getHeaders()
    });
  }

  processPayment(request: PaymentRequest): Observable<PaymentResult> {
    return this.http.post<PaymentResult>(`${this.baseUrl}/payments`, request, {
      headers: this.getHeaders()
    });
  }

  getOrder(orderId: string): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/orders/${orderId}`, {
      headers: this.getHeaders()
    });
  }

  getUserOrders(userId: string) {
    return this.http.get<Order[]>(`${this.baseUrl}/orders?userId=${userId}`, {
      headers: this.getHeaders()
    });
  }

  shipOrder(orderId: string) {
    return this.http.post<Order>(`${this.baseUrl}/orders/${orderId}/ship`, {}, {
      headers: this.getHeaders()
    });
  }

  getAnalytics() {
    return this.http.get(`${this.baseUrl}/admin/analytics`, {
      headers: this.getHeaders()
    });
  }
}
