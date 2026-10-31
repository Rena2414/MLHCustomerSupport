export interface CustomerListItem {
  id: string;
  fullName: string;
  email: string;
  plan: string;
  subscriptionStatus: string;
}

export interface CustomerDetails extends CustomerListItem {
  nextPaymentDate: string | null;
  nextPaymentAmount: number | null;
  createdAt: string;
}
