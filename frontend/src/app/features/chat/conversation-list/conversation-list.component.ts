import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ChatStore } from '../../../core/chat/chat.store';

@Component({
  selector: 'app-conversation-list',
  imports: [DatePipe],
  templateUrl: './conversation-list.component.html',
  styleUrl: './conversation-list.component.css'
})
export class ConversationListComponent {
  protected readonly store = inject(ChatStore);
}
