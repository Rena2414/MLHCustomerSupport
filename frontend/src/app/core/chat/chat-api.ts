import { Observable } from 'rxjs';
import { ChatMessage, Conversation } from './chat.models';

export abstract class ChatApi {
  abstract getConversations(): Observable<Conversation[]>;
  abstract getMessages(conversationId: string): Observable<ChatMessage[]>;
  abstract createConversation(): Observable<Conversation>;
  abstract streamReply(conversationId: string, text: string): Observable<string>;
}
