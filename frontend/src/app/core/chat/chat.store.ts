import { Injectable, computed, inject, signal } from '@angular/core';
import { ChatApi } from './chat-api';
import { ChatMessage, Conversation } from './chat.models';

const DEFAULT_TITLE = 'New conversation';
const TITLE_MAX_LENGTH = 40;

@Injectable({ providedIn: 'root' })
export class ChatStore {
  private readonly api = inject(ChatApi);

  readonly conversations = signal<Conversation[]>([]);
  readonly activeId = signal<string | null>(null);
  readonly messages = signal<ChatMessage[]>([]);
  readonly waitingForReply = signal<string | null>(null);
  readonly sendError = signal<string | null>(null);
  readonly activeConversationWaiting = computed(
    () => this.waitingForReply() !== null && this.waitingForReply() === this.activeId()
  );

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
    this.sendError.set(null);
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
    this.appendMessage('customer', text);
    this.touchConversation(conversationId, text);
    this.sendError.set(null);
    this.waitingForReply.set(conversationId);

    let replyId: string | null = null;
    let reply = '';
    this.api.streamReply(conversationId, text).subscribe({
      next: delta => {
        reply += delta;
        if (this.activeId() !== conversationId) {
          return;
        }
        if (replyId === null) {
          this.clearWaiting(conversationId);
          replyId = this.appendMessage('agent', reply);
        } else {
          this.updateMessage(replyId, reply);
        }
      },
      complete: () => {
        this.clearWaiting(conversationId);
        if (reply) {
          this.touchConversation(conversationId, reply);
        }
      },
      error: () => {
        this.clearWaiting(conversationId);
        if (this.activeId() === conversationId) {
          this.sendError.set('The agent did not answer. Please try again.');
        }
      }
    });
  }

  private appendMessage(author: ChatMessage['author'], text: string): string {
    const id = crypto.randomUUID();
    this.messages.update(list => [...list, { id, author, text, createdAt: new Date().toISOString() }]);
    return id;
  }

  private updateMessage(id: string, text: string): void {
    this.messages.update(list => list.map(message => (message.id === id ? { ...message, text } : message)));
  }

  private buildTitle(text: string): string {
    const trimmed = text.trim();
    return trimmed.length <= TITLE_MAX_LENGTH ? trimmed : `${trimmed.slice(0, TITLE_MAX_LENGTH).trimEnd()}...`;
  }

  private clearWaiting(conversationId: string): void {
    if (this.waitingForReply() === conversationId) {
      this.waitingForReply.set(null);
    }
  }

  private touchConversation(id: string, lastMessage: string): void {
    this.conversations.update(list => {
      const current = list.find(c => c.id === id);
      if (!current) {
        return list;
      }
      const title = current.title === DEFAULT_TITLE ? this.buildTitle(lastMessage) : current.title;
      const updated = { ...current, title, lastMessage, updatedAt: new Date().toISOString() };
      return [updated, ...list.filter(c => c.id !== id)];
    });
  }
}
