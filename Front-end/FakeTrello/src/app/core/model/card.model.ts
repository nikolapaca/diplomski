export interface Card {
    id: number,
    name: string,
    description: string,
    cardListId: number,
    assignedUserUsernames?: string[],
    isPinned?: boolean
}