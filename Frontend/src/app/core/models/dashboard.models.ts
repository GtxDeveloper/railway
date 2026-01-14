export interface ProfileResponse{
  firstName: string,
  lastName: string,
  email: string,
  brandName: string,
  phoneNumber: string,
  city: string,
  avatarUrl: string,
}

export interface Worker {
  id: string;
  name: string;
  job: string;
  isOnboarded: boolean;
  stripeAccountId: string | null;
  avatarUrl: string;
  isLinked: boolean;
}

export interface Summary {
  todayEarnings: number;
  monthEarnings: number;
  totalEarnings: number;
  transactionsCount: number;

}

export interface Balance {
  available : number,
  pending: number,
  currency : string,
}

export interface LinkResponse {
  url: string;
}

export interface Transaction {
  id: string;
  amount: number;       // Сколько заплатил клиент
  workerAmount: number; // Чистые чаевые работнику
  currency: string;
  createdAt: string;    // Дата приходит строкой
}

export interface UserProfilePayload {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  city?: string;      // Опционально, если редактируем только личные данные
  brandName?: string; // Опционально, если редактируем только личные данные
}

export interface BusinessProfile {
  id: string;
  name: string;       // Название бизнеса
  logoUrl: string | null;
  workersCount: number;
}

// Ответ при загрузке картинки (у вас он { url: "..." })
export interface UploadResponse {
  url: string;
}

export interface UpdateWorkerPayload {
  firstName: string;
  lastName: string;
  job: string;
}

export interface CreateWorkerPayload {
  name: string;
  job: string;
}

export type WorkersResponse = Worker[];

export interface UserContext {
  userId: string;
  email: string;
  firstName: string;

  // Строго типизируем роль, чтобы избежать опечаток в проверках
  role: 'Owner' | 'Worker' | 'New';

  // Эти поля могут быть null (например, у Owner нет workerId, а у New нет ничего)
  businessId: string | null;
  workerId: string | null;

  avatarUrl: string | null;
}

export interface PublicWorker {
  id: string;
  name: string;
  job: string;
  avatarUrl: string | null;
}
