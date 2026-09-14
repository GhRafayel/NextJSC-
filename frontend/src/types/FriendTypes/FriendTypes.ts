
export type StatusType = "ACCEPTED" | "PENDING" | "REJECTED";

export interface FriendType {
  id: number;
  username: string;
  score: number;
  requestId: number;
  status: StatusType;
  senderId: number;
  isOnline: boolean;
}