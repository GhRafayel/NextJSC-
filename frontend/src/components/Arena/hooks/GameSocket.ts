import { useEffect } from "react";
import * as signalR from "@microsoft/signalr"
import { MatchStateType } from "@/src/types/GameTypes/GameTypes";
import { useGameCanvasStore } from "@/src/components/Store/useGameCanvasStore";
import { useArenaStore } from "@/src/components/Store/useArenaStore";

interface UseGameSocketParamsType {
    socket: signalR.HubConnection | null;
    myUserId: string | number | undefined;
}

export function GameSocket({ socket, myUserId }: UseGameSocketParamsType) {
    const setArenaGameState = useArenaStore((s) => s.setGameState);

    useEffect(() => {
        if (!socket) return;

        const handleMatchState = (data: MatchStateType) => {
            const store = useGameCanvasStore.getState();
            store.setGames(data);
            store.setStateTime(performance.now());
            store.toggleStep();

            if (data.status === 'finished') {
                const won = String(data.winnerId) === String(myUserId);
                const nextState = won ? 'WIN' : 'OVER';
                store.setInternalGameState(nextState);
                setArenaGameState(nextState);
            } else {
                const myHero = data.heroes.find((h) => String(h.userId) === String(myUserId));
                if (myHero && !myHero.alive && store.internalGameState !== 'OVER') {
                    store.setInternalGameState('OVER');
                    setArenaGameState('OVER');
                }
            }
        };

        socket.on("match-state", handleMatchState);

        return () => {
            socket.off("match-state", handleMatchState);
        };
    }, [socket, myUserId, setArenaGameState]);
}
