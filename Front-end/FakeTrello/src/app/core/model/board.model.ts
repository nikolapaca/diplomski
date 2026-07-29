import { BoardStatus } from "./board-status.enum";

export interface Board {
    name: string;
    description: string;
    status: BoardStatus;
    ownerUsername: string;
    isFavorite: boolean;
}

