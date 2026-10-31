import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomerService } from '../../core/customer.service';

@Component({
  selector: 'app-clients-list',
  imports: [AsyncPipe, RouterLink],
  template: `
    <h2>Clients</h2>
    <table>
      <thead>
        <tr><th>Name</th><th>Email</th><th>Plan</th><th>Status</th></tr>
      </thead>
      <tbody>
        @for (c of customers$ | async; track c.id) {
          <tr>
            <td><a [routerLink]="['/clients', c.id]">{{ c.fullName }}</a></td>
            <td>{{ c.email }}</td>
            <td>{{ c.plan }}</td>
            <td>{{ c.subscriptionStatus }}</td>
          </tr>
        }
      </tbody>
    </table>
  `
})
export class ClientsListComponent {
  readonly customers$ = inject(CustomerService).getAll();
}
