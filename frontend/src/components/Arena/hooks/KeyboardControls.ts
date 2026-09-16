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
        const KEY_TO_DIR: Record<string, 'UP' | 'DOWN' | 'LEFT' | 'RIGHT'> = {
            ArrowUp: 'UP',
            ArrowDown: 'DOWN',
            ArrowLeft: 'LEFT',
            ArrowRight: 'RIGHT',
        };

        const isEnded = () => {
            const state = useGameCanvasStore.getState().internalGameState;
            return state === 'OVER' || state === 'WIN' || state === 'END';
        };

        const sendDirection = (dir: 'UP' | 'DOWN' | 'LEFT' | 'RIGHT' | null) => {
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

            const dir = KEY_TO_DIR[e.key];
            if (dir) {
                setGameDir(dir);
                sendDirection(dir);
            } else if (e.key === 'Escape') {
                setGameDir(null);
                useGameCanvasStore.getState().setInternalGameState('END');
                setGameState('END');
            }
        };

        const handleKeyUp = (e: KeyboardEvent) => {
            const releasedDir = KEY_TO_DIR[e.key];
            if (!releasedDir) return;

            // only stop if the key released is the one currently driving movement —
            // otherwise an old key's keyup could stop a hero moved by a newer keydown
            if (useArenaStore.getState().gameDir !== releasedDir) return;

            setGameDir(null);
            sendDirection(null);
        };

        window.addEventListener('keydown', handleKeyDown);
        window.addEventListener('keyup', handleKeyUp);
        return () => {
            window.removeEventListener('keydown', handleKeyDown);
            window.removeEventListener('keyup', handleKeyUp);
        };
    }, [socket, myUserId, setGameState, setGameDir]);
}
