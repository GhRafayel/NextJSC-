import { useEffect } from "react";
import * as signalR from "@microsoft/signalr"
import { useArenaStore } from "@/src/components/Store/useArenaStore";
import { useGameCanvasStore } from "@/src/components/Store/useGameCanvasStore";

interface KeyboardControlsParamsType {
    socket: signalR.HubConnection | null;
    myUserId: string | number | undefined;
}

export function KeyboardControls({ socket, myUserId }: KeyboardControlsParamsType) {
    const setGameState = useArenaStore((s) => s.setGameState);
    const setGameDir = useArenaStore((s) => s.setGameDir);

    useEffect(() => {
        const isEnded = () => {
            const state = useGameCanvasStore.getState().internalGameState;
            return state === 'OVER' || state === 'WIN' || state === 'END';
        };

        const advanceHero = (dir: 'UP' | 'DOWN' | 'LEFT' | 'RIGHT') => {
            const room = useGameCanvasStore.getState().currGame?.roomId ?? useArenaStore.getState().roomState?.roomId;
            if (!room || !socket) return;
            socket.send('ChangeDirection', { direction: dir, roomId: room, userId: myUserId });
        };

        const handleKeyDown = (e: KeyboardEvent) => {
            if (!['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Escape', ' '].includes(e.key)) return;
            e.preventDefault();

            if (isEnded()) return;

            if (useArenaStore.getState().gameState === 'PAUSE') {
                useGameCanvasStore.getState().setInternalGameState('START');
                setGameState('START');
            }

            if (e.key === 'ArrowUp') {
                setGameDir('UP');
                advanceHero('UP');
            } else if (e.key === 'ArrowDown') {
                setGameDir('DOWN');
                advanceHero('DOWN');
            } else if (e.key === 'ArrowLeft') {
                setGameDir('LEFT');
                advanceHero('LEFT');
            } else if (e.key === 'ArrowRight') {
                setGameDir('RIGHT');
                advanceHero('RIGHT');
            } else if (e.key === 'Escape') {
                setGameDir(null);
                useGameCanvasStore.getState().setInternalGameState('END');
                setGameState('END');
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [socket, myUserId, setGameState, setGameDir]);
}
