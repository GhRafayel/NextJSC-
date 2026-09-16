'use client'

import { useEffect }        from "react";
import { Loader }           from "lucide-react";
import { useArenaStore }    from "@/src/components/Store/useArenaStore";
import { useAuth }          from "@/src/components/Provider/UserProvider";
import { ArenaBoardType }   from "@/src/types/GameTypes/GameTypes";
import MapCanvas            from "./MapCanvas";

type RoomStatusType = keyof ArenaBoardType;

export default function GameBoard() {
    const {cntUser, LENUAGE } = useAuth();

    const roomState = useArenaStore((s) => s.roomState);
    const countdownSeconds = useArenaStore((s) => s.countdownSeconds);
    const gameState = useArenaStore((s) => s.gameState);
    const AR_LENG = LENUAGE.Arena.board;
    const rawStatus = roomState?.roomStatus;
    const status = roomState ? AR_LENG[roomState.roomStatus as RoomStatusType] : AR_LENG.connecting;
    const isGameOver = gameState === "WIN" || gameState === "OVER" || gameState === "END";
    const showCanvas = rawStatus === "PLAYING" || isGameOver;

    useEffect(() => {
        if (rawStatus !== "STARTING") return;
        const id = setInterval(() => {
            useArenaStore.getState().setCountdownSeconds((prev) => (prev !== null && prev > 0 ? prev - 1 : prev));
        }, 1000);
        return () => clearInterval(id);
    }, [rawStatus]);

    return (
        <div id="canvas-container" className="col-span-4 mx-auto aspect-square w-full max-w-[calc(100vh-250px)] max-h-[calc(100vh-250px)] flex flex-col items-center justify-start mt-2 relative" >
            <MapCanvas />
            {!showCanvas && (
                <div className={`absolute inset-0 flex flex-col items-center justify-center gap-3 w-full h-full bg-black/35 rounded-xl ${cntUser?.theme ?? true ? "text-green-400" : "text-blue-300"}`}>
                    <Loader className="w-15 h-15 animate-spin" />
                    <span>{status}</span>
                    {rawStatus === "STARTING" && countdownSeconds !== null && (
                        <span className="text-2xl font-bold tabular-nums">{countdownSeconds}s</span>
                    )}
                </div>
            )}
        </div>
    );
}