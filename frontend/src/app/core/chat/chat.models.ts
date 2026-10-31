export interface Conversation {
  id: string;
  title: string;
  lastMessage: string;
  updatedAt: string;
}

export interface ChatMessage {
  id: string;
  author: 'customer' | 'agent';
  text: string;
  createdAt: string;
}
