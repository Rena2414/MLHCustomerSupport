import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomerService } from '../../../core/customer.service';

@Component({
  selector: 'app-clients-list',
  imports: [AsyncPipe, RouterLink],
  templateUrl: './clients-list.component.html',
  styleUrl: './clients-list.component.css'
})
export class ClientsListComponent {
  readonly customers$ = inject(CustomerService).getAll();
}
