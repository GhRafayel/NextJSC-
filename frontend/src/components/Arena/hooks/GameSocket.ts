import { useEffect } from "react";
import * as signalR from "@microsoft/signalr"
import { MatchStateType } from "@/src/types/GameTypes/GameTypes";
import { useGameCanvasStore } from "@/src/components/Store/useGameCanvasStore";
import { useArenaStore } from "@/src/components/Store/useArenaStore";
import { useAuth } from "@/src/components/Provider/UserProvider";
import { playEatSound, playExplosionSound, playDeathSound, playWinSound } from "../utils/sound";

interface UseGameSocketParamsType {
    socket: signalR.HubConnection | null;
    myUserId: string | number | undefined;
}

export function GameSocket({ socket, myUserId }: UseGameSocketParamsType) {
    const setArenaGameState = useArenaStore((s) => s.setGameState);
    const { ChangingCallback } = useAuth();

    useEffect(() => {
        if (!socket) return;

        const handleMatchState = (data: MatchStateType) => {
            const store = useGameCanvasStore.getState();
            const prevMyHero = store.currGame?.heroes.find((h) => String(h.userId) === String(myUserId));

            // the server now broadcasts every tick, but a hero's position only actually
            // changes once every few ticks (see TicksPerMove) — only restart the
            // move-interpolation clock on ticks where something actually moved, otherwise
            // every no-op broadcast would cut the glide animation short and cause stutter
            const positionsChanged = !store.currGame || data.heroes.some((hero) => {
                const prev = store.currGame!.heroes.find((h) => String(h.userId) === String(hero.userId));
                return !prev || prev.position.row !== hero.position.row || prev.position.col !== hero.position.col;
            });

            store.setGames(data);
            if (positionsChanged) {
                store.setStateTime(performance.now());
                store.toggleStep();
            }

            if (data.blasts?.length) {
                store.addBlasts(data.blasts, 400);
                playExplosionSound();
            }

            const myHero = data.heroes.find((h) => String(h.userId) === String(myUserId));
            if (myHero && prevMyHero && myHero.alive && (
                myHero.maxBombs > prevMyHero.maxBombs || myHero.blastLength > prevMyHero.blastLength
            )) {
                playEatSound();
            }

            if (data.status === 'finished') {
                const won = String(data.winnerId) === String(myUserId);
                const nextState = won ? 'WIN' : 'OVER';
                store.setInternalGameState(nextState);
                setArenaGameState(nextState);
                ChangingCallback(undefined, "me");
                if (won) playWinSound(); else playDeathSound();
            } else {
                if (myHero && !myHero.alive && store.internalGameState !== 'OVER') {
                    store.setInternalGameState('OVER');
                    setArenaGameState('OVER');
                    playDeathSound();
                }
            }
        };

        socket.on("match-state", handleMatchState);

        return () => {
            socket.off("match-state", handleMatchState);
        };
    }, [socket, myUserId, setArenaGameState, ChangingCallback]);
}
