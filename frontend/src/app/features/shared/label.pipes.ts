import { Pipe, PipeTransform } from '@angular/core';
import { SubscriptionType } from '../../core/auth/auth.models';

@Pipe({ name: 'humanize' })
export class HumanizePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) {
      return '';
    }
    return value
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/[._]+/g, ' ')
      .toLowerCase()
      .replace(/^./, first => first.toUpperCase());
  }
}

@Pipe({ name: 'subscription' })
export class SubscriptionPipe implements PipeTransform {
  transform(value: SubscriptionType | null | undefined): string {
    return value === 1 ? 'Subscribed' : 'No subscription';
  }
}
