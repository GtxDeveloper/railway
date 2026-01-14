export interface AdminStats {
  totalRevenue: number;
  totalTurnover: number;
  totalUsers: number;
  totalBusinesses: number;
  totalTransactions: number;
}

export interface AdminTransaction {
  id: string;
  amount: number;
  platformFee: number;
  workerName: string;
  businessName: string;
  createdAt: string; // Или Date, если будем мапить
}

export interface AdminWorkerDetail {
  id: string;
  name: string;
  job: string;
  avatarUrl: string | null;
  stripeAccountId: string | null;
  isOnboarded: boolean;
  isLinked: boolean;
  linkedUserId: string | null;
}

export interface AdminBusiness {
  id: string;
  brandName: string;
  city: string;
  ownerId: string;
  ownerEmail: string;
  ownerName: string;
  avatarUrl: string;
  workers: AdminWorkerDetail[];
}
