"use client";

import { useEffect }                        from "react";
import * as signalR                         from "@microsoft/signalr";
import { useAuth }                          from "./UserProvider";
import { useSocket as getSocket }           from "@/src/components/Socket/Socket";
import { useInviteStore }                   from "@/src/components/Store/useInviteStore";
import { RoomInviteType }                   from "@/src/types/GameTypes/GameTypes";
import { useArenaStore }                    from "../Store/useArenaStore";
import { useUserStore }                     from "../Store/useUserStore";
import { RoomStateType, RoomCountdownType } from "@/src/types/GameTypes/GameTypes";
import { OnlineUsersType }                  from "@/src/types/UserTypes/UserTypes";

export default function SocketProvider({ children }: { children: React.ReactNode }) {
    const { cntUser } = useAuth();

    useEffect(() => {
      if (!cntUser?.id) return;
      const userId = cntUser.id;

      const connection = getSocket();
      if (!connection) return;

      const handleDisconnect = (error?: Error) => {
        console.log("❌ Socket disconnected:", error?.message ?? "")
      };

      const handleOnlineUsers = (gameData: OnlineUsersType[]) => {
          useUserStore.setState({ onlineUsers: gameData });
      };

      const handleRoomUpdate = (gameData: RoomStateType) => {
          useArenaStore.getState().setRoomState({ ...gameData });
          if (gameData.roomStatus !== "STARTING")
              useArenaStore.getState().setCountdownSeconds(null);
      };

      const handleRoomCountdown = (gameData: RoomCountdownType) => {
          useArenaStore.getState().setCountdownSeconds(gameData.seconds);
      };

      const handleRoomInvite = (invite: RoomInviteType) => {
        if (invite.from.id === userId) return;
        useInviteStore.getState().addInvite(invite);
    };

      connection.onclose(handleDisconnect);

      connection.on("online-users", handleOnlineUsers);
      connection.on("room-update", handleRoomUpdate);
      connection.on("room-countdown", handleRoomCountdown);
      connection.on("room-invite", handleRoomInvite);

      const connect = async () => {
        if (connection.state === signalR.HubConnectionState.Disconnected) {
          try {
            await connection.start();
            console.log("✅ Socket connected!", connection.connectionId);
          } catch (err) {
            console.log("⚠️ Socket connect error:", err);
          }
        }
      };
      connect();

      return () => {
        connection.off("online-users", handleOnlineUsers);
        connection.off("room-update", handleRoomUpdate);
        connection.off("room-countdown", handleRoomCountdown);
        connection.off("room-invite", handleRoomInvite);
        connection.stop();
      };

    },[cntUser?.id])

    return children;
}
