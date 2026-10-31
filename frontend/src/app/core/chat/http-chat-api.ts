import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChatApi } from './chat-api';
import { ChatMessage, Conversation } from './chat.models';
import { SseParser } from './sse-parser';

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

  streamReply(conversationId: string, text: string): Observable<string> {
    return new Observable<string>(subscriber => {
      const controller = new AbortController();
      this.readStream(`${this.baseUrl}/${conversationId}/messages/stream`, text, controller.signal, {
        delta: delta => subscriber.next(delta),
        done: () => subscriber.complete(),
        error: error => subscriber.error(error)
      });
      return () => controller.abort();
    });
  }

  private async readStream(
    url: string,
    text: string,
    signal: AbortSignal,
    handlers: { delta: (text: string) => void; done: () => void; error: (error: unknown) => void }
  ): Promise<void> {
    try {
      const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
        body: JSON.stringify({ text }),
        signal
      });
      if (!response.ok || !response.body) {
        throw new Error(`Chat request failed with status ${response.status}`);
      }
      const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
      const parser = new SseParser();
      for (;;) {
        const { value, done } = await reader.read();
        if (done) {
          break;
        }
        for (const event of parser.push(value)) {
          if (event.name === 'delta') {
            handlers.delta(JSON.parse(event.data).text);
          } else if (event.name === 'error') {
            throw new Error(JSON.parse(event.data).message);
          }
        }
      }
      handlers.done();
    } catch (error) {
      if (!signal.aborted) {
        handlers.error(error);
      }
    }
  }
}
