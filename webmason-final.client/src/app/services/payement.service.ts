// src/app/services/payment.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { loadStripe, Stripe } from '@stripe/stripe-js';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private stripe: Stripe | null = null;
  private apiUrl = '/api/payments';

  constructor(private http: HttpClient) {
    this.loadStripe();
  }

  private async loadStripe() {
    this.stripe = await loadStripe('pk_test_51PQVleP1wlY61QCvaeS7VkLBmveyZlaLb7tO8VfaD9Pv0Qho3tuJpXbe74XpEDnlfqCJia1NFWAX2cPMZeBWY7sQ001YPOIWYe');
  }

  createPaymentIntent(amount: number) {
    return this.http.post<{ clientSecret: string }>(`${this.apiUrl}/create-checkout-session`, { amount });
  }

  getStripe() {
    return this.stripe;
  }
}
