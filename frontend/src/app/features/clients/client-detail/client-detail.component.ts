import { AsyncPipe, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CustomerService } from '../../../core/customer.service';

@Component({
  selector: 'app-client-detail',
  imports: [AsyncPipe, CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './client-detail.component.html',
  styleUrl: './client-detail.component.css'
})
export class ClientDetailComponent {
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;
  readonly customer$ = inject(CustomerService).getById(this.id);
}
