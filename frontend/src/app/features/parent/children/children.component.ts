import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { describeApiError } from '../../../core/api/api-error';
import { ChildInfo } from '../../../core/api/api.models';
import { FamilyService } from '../../../core/api/family.service';
import { SubscriptionService } from '../../../core/api/subscription.service';
import { PRODUCT_NAME } from '../../../core/brand';
import { SubscriptionPipe } from '../../shared/label.pipes';

@Component({
  selector: 'app-children',
  imports: [FormsModule, SubscriptionPipe],
  templateUrl: './children.component.html',
  styleUrl: './children.component.css'
})
export class ChildrenComponent {
  private readonly family = inject(FamilyService);
  private readonly subscriptions = inject(SubscriptionService);

  readonly productName = PRODUCT_NAME;
  readonly children = signal<ChildInfo[]>([]);
  readonly loading = signal(true);
  readonly childLogin = signal('');
  readonly linking = signal(false);
  readonly linkError = signal<string | null>(null);
  readonly busyChildIds = signal<ReadonlySet<string>>(new Set());
  readonly childError = signal<{ id: string; message: string } | null>(null);

  constructor() {
    this.reload();
  }

  link(): void {
    const login = this.childLogin().trim();
    if (!login || this.linking()) {
      return;
    }
    this.linking.set(true);
    this.linkError.set(null);
    this.family.linkChild(login).subscribe({
      next: () => {
        this.linking.set(false);
        this.childLogin.set('');
        this.reload();
      },
      error: error => {
        this.linking.set(false);
        this.linkError.set(describeApiError(error, 'No student with this login.'));
      }
    });
  }

  subscribe(child: ChildInfo): void {
    this.runForChild(child.id, this.subscriptions.subscribeChild(child.id), 'Could not start the subscription.');
  }

  unlink(child: ChildInfo): void {
    if (!confirm(`Unlink ${child.name} from your account?`)) {
      return;
    }
    this.runForChild(child.id, this.family.unlinkChild(child.id), 'Could not unlink the child.');
  }

  private runForChild(childId: string, request: ReturnType<FamilyService['unlinkChild']>, fallback: string): void {
    if (this.busyChildIds().has(childId)) {
      return;
    }
    this.busyChildIds.update(ids => new Set(ids).add(childId));
    this.childError.set(null);
    const done = () =>
      this.busyChildIds.update(ids => {
        const next = new Set(ids);
        next.delete(childId);
        return next;
      });
    request.subscribe({
      next: () => {
        done();
        this.reload();
      },
      error: error => {
        done();
        this.childError.set({ id: childId, message: describeApiError(error, fallback) });
      }
    });
  }

  private reload(): void {
    this.family.getChildren().subscribe({
      next: list => {
        this.children.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
