import { Injectable, inject, signal } from '@angular/core';
import { ChatApi } from './chat-api';
import { ChatMessage, Conversation } from './chat.models';

@Injectable({ providedIn: 'root' })
export class ChatStore {
  private readonly api = inject(ChatApi);

  readonly conversations = signal<Conversation[]>([]);
  readonly activeId = signal<string | null>(null);
  readonly messages = signal<ChatMessage[]>([]);
  readonly waitingForReply = signal(false);

  load(): void {
    this.api.getConversations().subscribe(list => {
      this.conversations.set(list);
      if (list.length) {
        this.select(list[0].id);
      } else {
        this.startNew();
      }
    });
  }

  select(id: string): void {
    this.activeId.set(id);
    this.messages.set([]);
    this.api.getMessages(id).subscribe(list => {
      if (this.activeId() === id) {
        this.messages.set(list);
      }
    });
  }

  startNew(): void {
    this.api.createConversation().subscribe(conversation => {
      this.conversations.update(list => [conversation, ...list]);
      this.select(conversation.id);
    });
  }

  send(text: string): void {
    const conversationId = this.activeId();
    if (!conversationId) {
      return;
    }
    const customerMessage: ChatMessage = {
      id: crypto.randomUUID(),
      author: 'customer',
      text,
      createdAt: new Date().toISOString()
    };
    this.messages.update(list => [...list, customerMessage]);
    this.touchConversation(conversationId, text);
    this.waitingForReply.set(true);
    this.api.sendMessage(conversationId, text).subscribe({
      next: reply => {
        this.waitingForReply.set(false);
        if (this.activeId() === conversationId) {
          this.messages.update(list => [...list, reply]);
        }
        this.touchConversation(conversationId, reply.text);
      },
      error: () => this.waitingForReply.set(false)
    });
  }

  private touchConversation(id: string, lastMessage: string): void {
    this.conversations.update(list => {
      const current = list.find(c => c.id === id);
      if (!current) {
        return list;
      }
      const updated = { ...current, lastMessage, updatedAt: new Date().toISOString() };
      return [updated, ...list.filter(c => c.id !== id)];
    });
  }
}
