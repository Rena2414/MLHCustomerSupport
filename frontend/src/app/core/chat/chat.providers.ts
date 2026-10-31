import { Provider } from '@angular/core';
import { ChatApi } from './chat-api';
import { HttpChatApi } from './http-chat-api';

export const chatProviders: Provider[] = [{ provide: ChatApi, useClass: HttpChatApi }];
