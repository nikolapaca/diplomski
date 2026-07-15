export interface BoardActivity {
  id: number;
  message: string;
  type: number;
  creatingUsername: string;
  cardId?: number;
  createdAt: string;
}