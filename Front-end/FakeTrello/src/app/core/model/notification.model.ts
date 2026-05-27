export interface Notification {
  id: number;
  message: string;
  type: string | number;
  isRead: boolean;
  createdAt: string;
  boardName?: string;
  boardOwnerUsername?: string;
  cardId?: number;
}