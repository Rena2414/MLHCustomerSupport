import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChatApi } from './chat-api';
import { ChatMessage, Conversation } from './chat.models';

@Injectable()
export class HttpChatApi extends ChatApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/conversations`;

  getConversations(): Observable<Conversation[]> {
    return this.http.get<Conversation[]>(this.baseUrl);
  }

  getMessages(conversationId: string): Observable<ChatMessage[]> {
    return this.http.get<ChatMessage[]>(`${this.baseUrl}/${conversationId}/messages`);
  }

  createConversation(): Observable<Conversation> {
    return this.http.post<Conversation>(this.baseUrl, {});
  }

  sendMessage(conversationId: string, text: string): Observable<ChatMessage> {
    return this.http.post<ChatMessage>(`${this.baseUrl}/${conversationId}/messages`, { text });
  }
}
