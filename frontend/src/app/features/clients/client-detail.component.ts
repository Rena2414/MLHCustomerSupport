import { AsyncPipe, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CustomerService } from '../../core/customer.service';

@Component({
  selector: 'app-client-detail',
  imports: [AsyncPipe, CurrencyPipe, DatePipe, RouterLink],
  template: `
    <a routerLink="/clients">&larr; Back to clients</a>
    @if (customer$ | async; as c) {
      <h2>{{ c.fullName }}</h2>
      <dl>
        <dt>Id</dt><dd>{{ c.id }}</dd>
        <dt>Email</dt><dd>{{ c.email }}</dd>
        <dt>Plan</dt><dd>{{ c.plan }}</dd>
        <dt>Subscription status</dt><dd>{{ c.subscriptionStatus }}</dd>
        <dt>Next payment date</dt><dd>{{ (c.nextPaymentDate | date: 'mediumDate') ?? '-' }}</dd>
        <dt>Next payment amount</dt><dd>{{ (c.nextPaymentAmount | currency) ?? '-' }}</dd>
        <dt>Created</dt><dd>{{ c.createdAt | date: 'medium' }}</dd>
      </dl>
    }
  `
})
export class ClientDetailComponent {
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;
  readonly customer$ = inject(CustomerService).getById(this.id);
}
