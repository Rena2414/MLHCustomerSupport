import { Provider } from '@angular/core';
import { MockChatApi } from '../mock/mock-chat-api';
import { ChatApi } from './chat-api';

export const chatProviders: Provider[] = [{ provide: ChatApi, useClass: MockChatApi }];
