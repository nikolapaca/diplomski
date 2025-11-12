export interface Card {
    id: number,
    name: string,
    description: string,
    cardListId: number,
    assignedUserUsername?: string
}