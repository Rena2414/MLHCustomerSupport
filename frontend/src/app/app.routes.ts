import { Routes } from '@angular/router';
import { ChatPageComponent } from './features/chat/chat-page/chat-page.component';
import { ClientDetailComponent } from './features/clients/client-detail/client-detail.component';
import { ClientsListComponent } from './features/clients/clients-list/clients-list.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'clients' },
  { path: 'clients', component: ClientsListComponent },
  { path: 'clients/:id', component: ClientDetailComponent },
  { path: 'chat', component: ChatPageComponent }
];
