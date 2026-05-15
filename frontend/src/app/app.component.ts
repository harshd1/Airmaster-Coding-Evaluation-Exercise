import { Component, OnDestroy, OnInit } from '@angular/core';
import { interval, Subscription } from 'rxjs';
import { OrderService, Product, Order, OrderRequest, OrderItem, PaymentRequest, PaymentResult } from './services/order.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Airmaster Global Store';
  products: Product[] = [];
  loading = true;
  selectedProduct: Product | null = null;
  quantity = 1;
  shippingAddress = '123 Market Street, San Francisco, CA';
  billingAddress = '123 Market Street, San Francisco, CA';
  paymentToken = '';
  order: Order | null = null;
  paymentResult: PaymentResult | null = null;
  statusMessage = '';
  pollingSubscription: Subscription | null = null;

  constructor(private orderService: OrderService) {}

  ngOnInit(): void {
    this.orderService.getProducts().subscribe({
      next: data => {
        this.products = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.pollingSubscription?.unsubscribe();
  }

  selectProduct(product: Product): void {
    this.selectedProduct = product;
    this.quantity = 1;
    this.order = null;
    this.paymentResult = null;
    this.statusMessage = '';
  }

  get totalAmount(): number {
    if (!this.selectedProduct) {
      return 0;
    }
    return this.selectedProduct.price * this.quantity;
  }

  placeOrder(): void {
    if (!this.selectedProduct) {
      return;
    }

    const item: OrderItem = {
      productId: this.selectedProduct.productId,
      quantity: this.quantity,
      unitPrice: this.selectedProduct.price
    };

    const request: OrderRequest = {
      userId: '00000000-0000-0000-0000-000000000001',
      items: [item],
      shippingAddress: this.shippingAddress,
      billingAddress: this.billingAddress
    };

    this.orderService.createOrder(request).subscribe({
      next: order => {
        this.order = order;
        this.paymentResult = null;
        this.statusMessage = 'Order created. Complete payment to confirm.';
        this.startOrderPolling();
      },
      error: err => {
        this.statusMessage = err?.error?.error || 'Unable to create order. Please try again.';
      }
    });
  }

  payNow(): void {
    if (!this.order) {
      return;
    }

    const request: PaymentRequest = {
      orderId: this.order.orderId,
      paymentProvider: 'Stripe',
      token: this.paymentToken,
      amount: this.order.totalAmount,
      currency: this.order.currency
    };

    this.orderService.processPayment(request).subscribe({
      next: result => {
        this.paymentResult = result;
        this.statusMessage = result.message;
        if (result.success) {
          this.refreshOrder();
        }
      },
      error: err => {
        this.paymentResult = err?.error || { success: false, message: 'Payment failed. Please retry.' };
      }
    });
  }

  refreshOrder(): void {
    if (!this.order) {
      return;
    }

    this.orderService.getOrder(this.order.orderId).subscribe({
      next: order => {
        this.order = order;
      }
    });
  }

  shipOrder(): void {
    if (!this.order) {
      return;
    }

    this.orderService.shipOrder(this.order.orderId).subscribe({
      next: order => {
        this.order = order;
        this.statusMessage = 'Order shipped successfully.';
      },
      error: err => {
        this.statusMessage = err?.error?.error || 'Unable to ship order.';
      }
    });
  }

  private startOrderPolling(): void {
    this.pollingSubscription?.unsubscribe();
    this.pollingSubscription = interval(5000).subscribe(() => {
      if (this.order) {
        this.refreshOrder();
      }
    });
  }
}
