export interface SseEvent {
  name: string;
  data: string;
}

export class SseParser {
  private buffer = '';

  push(chunk: string): SseEvent[] {
    this.buffer += chunk.replace(/\r\n/g, '\n');
    const events: SseEvent[] = [];
    let boundary = this.buffer.indexOf('\n\n');
    while (boundary !== -1) {
      const event = this.parseBlock(this.buffer.slice(0, boundary));
      if (event) {
        events.push(event);
      }
      this.buffer = this.buffer.slice(boundary + 2);
      boundary = this.buffer.indexOf('\n\n');
    }
    return events;
  }

  private parseBlock(block: string): SseEvent | null {
    let name = 'message';
    const dataLines: string[] = [];
    for (const line of block.split('\n')) {
      if (line.startsWith('event:')) {
        name = line.slice(6).trim();
      } else if (line.startsWith('data:')) {
        dataLines.push(line.slice(5).trimStart());
      }
    }
    return dataLines.length ? { name, data: dataLines.join('\n') } : null;
  }
}
