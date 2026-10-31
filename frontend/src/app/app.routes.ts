import { Routes } from '@angular/router';
import { ChatComponent } from './features/chat/chat.component';
import { ClientDetailComponent } from './features/clients/client-detail.component';
import { ClientsListComponent } from './features/clients/clients-list.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'clients' },
  { path: 'clients', component: ClientsListComponent },
  { path: 'clients/:id', component: ClientDetailComponent },
  { path: 'chat', component: ChatComponent }
];
