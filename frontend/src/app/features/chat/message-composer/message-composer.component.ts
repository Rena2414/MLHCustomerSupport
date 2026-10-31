import { Component, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-message-composer',
  imports: [FormsModule],
  templateUrl: './message-composer.component.html',
  styleUrl: './message-composer.component.css'
})
export class MessageComposerComponent {
  readonly sent = output<string>();
  protected draft = '';

  protected submit(): void {
    const text = this.draft.trim();
    if (!text) {
      return;
    }
    this.sent.emit(text);
    this.draft = '';
  }
}
