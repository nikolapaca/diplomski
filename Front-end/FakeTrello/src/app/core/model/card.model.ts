export interface Card {
    id: number,
    name: string,
    description: string,
    cardListId: number,
    assignedUserUsernames?: string[],
    isPinned?: boolean,
    dueDate?: string | null,
    coverImageUrl?: string | null,
    attachmentCount?: number
}