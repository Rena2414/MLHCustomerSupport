import { ChatMessage, Conversation } from '../chat/chat.models';

const hoursAgo = (hours: number): string => new Date(Date.now() - hours * 3_600_000).toISOString();

export const MOCK_CONVERSATIONS: Conversation[] = [
  { id: 'c-1', title: 'Charged after free trial', lastMessage: 'I have turned off auto-renewal for you.', updatedAt: hoursAgo(2) },
  { id: 'c-2', title: 'How do I cancel?', lastMessage: 'Open Settings, then Subscriptions.', updatedAt: hoursAgo(26) },
  { id: 'c-3', title: 'Progress after phone change', lastMessage: 'I have passed this to our technical team.', updatedAt: hoursAgo(75) }
];

export const MOCK_MESSAGES: Record<string, ChatMessage[]> = {
  'c-1': [
    { id: 'm-1', author: 'customer', text: 'I was charged after my free trial, I did not know it would renew.', createdAt: hoursAgo(2.1) },
    { id: 'm-2', author: 'agent', text: 'Sorry about that. I can see the trial ended on Oct 27. I have turned off auto-renewal for you.', createdAt: hoursAgo(2) }
  ],
  'c-2': [
    { id: 'm-3', author: 'customer', text: 'How do I cancel my subscription?', createdAt: hoursAgo(26.1) },
    { id: 'm-4', author: 'agent', text: 'Your plan was bought in the App Store. Open Settings, then Subscriptions.', createdAt: hoursAgo(26) }
  ],
  'c-3': [
    { id: 'm-5', author: 'customer', text: 'My progress disappeared after I changed phones.', createdAt: hoursAgo(75.1) },
    { id: 'm-6', author: 'agent', text: 'I have passed this to our technical team.', createdAt: hoursAgo(75) }
  ]
};

export const MOCK_AGENT_REPLY = 'The support agent is not connected yet.';
export const MOCK_GREETING = 'Hi! How can I help you today?';
