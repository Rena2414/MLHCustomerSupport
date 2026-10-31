import { Injectable } from '@angular/core';
import { Observable, delay, of } from 'rxjs';
import { ChatApi } from '../chat/chat-api';
import { ChatMessage, Conversation } from '../chat/chat.models';
import { MOCK_AGENT_REPLY, MOCK_CONVERSATIONS, MOCK_GREETING, MOCK_MESSAGES } from './mock-chat-data';

@Injectable()
export class MockChatApi extends ChatApi {
  private readonly conversations: Conversation[] = [...MOCK_CONVERSATIONS];
  private readonly messages: Record<string, ChatMessage[]> = { ...MOCK_MESSAGES };

  getConversations(): Observable<Conversation[]> {
    return of([...this.conversations]).pipe(delay(150));
  }

  getMessages(conversationId: string): Observable<ChatMessage[]> {
    return of([...(this.messages[conversationId] ?? [])]).pipe(delay(100));
  }

  createConversation(): Observable<Conversation> {
    const id = crypto.randomUUID();
    const now = new Date().toISOString();
    const conversation: Conversation = { id, title: 'New conversation', lastMessage: MOCK_GREETING, updatedAt: now };
    this.conversations.unshift(conversation);
    this.messages[id] = [{ id: crypto.randomUUID(), author: 'agent', text: MOCK_GREETING, createdAt: now }];
    return of(conversation).pipe(delay(100));
  }

  sendMessage(conversationId: string, text: string): Observable<ChatMessage> {
    const now = new Date().toISOString();
    const thread = this.messages[conversationId] ?? (this.messages[conversationId] = []);
    thread.push({ id: crypto.randomUUID(), author: 'customer', text, createdAt: now });
    const reply: ChatMessage = { id: crypto.randomUUID(), author: 'agent', text: MOCK_AGENT_REPLY, createdAt: now };
    thread.push(reply);
    return of(reply).pipe(delay(600));
  }
}
