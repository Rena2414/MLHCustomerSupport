import { Component, inject } from '@angular/core';
import { ChatStore } from '../../../core/chat/chat.store';
import { ChatWindowComponent } from '../chat-window/chat-window.component';
import { ConversationListComponent } from '../conversation-list/conversation-list.component';

@Component({
  selector: 'app-chat-page',
  imports: [ConversationListComponent, ChatWindowComponent],
  templateUrl: './chat-page.component.html',
  styleUrl: './chat-page.component.css'
})
export class ChatPageComponent {
  constructor() {
    inject(ChatStore).load();
  }
}
