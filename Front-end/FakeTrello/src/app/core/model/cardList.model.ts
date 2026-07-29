import { Card } from "./card.model";

export interface CardList {
    id: number,
    name: string,
    boardName: string,
    boardUsername: string,
    cards: Card[],
    isPinned?: boolean
}