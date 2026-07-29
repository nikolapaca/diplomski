export interface BoardMember {
  username: string;
  name: string;
  surname: string;
  role: number; // 0 = OWNER, 1 = COLLABORATOR (matches backend UserRole enum)
}
