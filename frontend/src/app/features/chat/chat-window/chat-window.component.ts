import { DatePipe } from '@angular/common';
import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { ChatStore } from '../../../core/chat/chat.store';
import { MarkdownPipe } from '../../../core/markdown.pipe';
import { MessageComposerComponent } from '../message-composer/message-composer.component';

@Component({
  selector: 'app-chat-window',
  imports: [DatePipe, MarkdownPipe, MessageComposerComponent],
  templateUrl: './chat-window.component.html',
  styleUrl: './chat-window.component.css'
})
export class ChatWindowComponent {
  protected readonly store = inject(ChatStore);
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');

  constructor() {
    effect(() => {
      this.store.messages();
      queueMicrotask(() => {
        const element = this.scroller()?.nativeElement;
        if (element) {
          element.scrollTop = element.scrollHeight;
        }
      });
    });
  }
}
