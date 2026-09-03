export interface BoardUpdate {
    oldBoardName: string;
    newBoardName: string;
    oldBoardDescription: string;
    newBoardDescription: string;
    ownerUsername: string;
    isOwner?: boolean;
}