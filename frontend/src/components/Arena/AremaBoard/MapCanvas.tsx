'use client';

import { useEffect, useRef } from "react";
import { useArenaStore } from "@/src/components/Store/useArenaStore";
import { useGameCanvasStore } from "@/src/components/Store/useGameCanvasStore";
import { useCanvasResize } from "../hooks/useCanvasResize";
import { drawWalls } from "../utils/DrawCanvasHelper";
import { drawHeroFace } from "../utils/DrawGameHelper";
import { CELL, WORLD_MARGIN } from "../utils/drawGame";

function getSpawnPosition(index: number, rows: number, cols: number) {
    const lastRow = rows - 2;
    const lastCol = cols - 2;
    switch (index) {
        case 0: return { row: 1, col: 1 };
        case 1: return { row: 1, col: lastCol };
        case 2: return { row: lastRow, col: 1 };
        case 3: return { row: lastRow, col: lastCol };
        default: return { row: 1, col: 1 };
    }
}

export default function MapCanvas() {
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const map = useArenaStore((s) => s.roomState?.map);
    const playerList = useArenaStore((s) => s.roomState?.playerList);
    const screen = useGameCanvasStore((s) => s.screen);

    useCanvasResize(canvasRef, "canvas-container");

    useEffect(() => {
        const canvas = canvasRef.current;
        const ctx = canvas?.getContext("2d");
        if (!canvas || !ctx || !map || screen.width === 0) return;

        const rows = map.length;
        const cols = map[0]?.length ?? 0;
        const worldWidth = cols * CELL;
        const worldHeight = rows * CELL;
        const scale = Math.min(
            screen.width / (worldWidth + WORLD_MARGIN * 2),
            screen.height / (worldHeight + WORLD_MARGIN * 2)
        );

        ctx.clearRect(0, 0, screen.width, screen.height);
        ctx.save();
        ctx.scale(scale, scale);
        ctx.translate(WORLD_MARGIN, WORLD_MARGIN);
        ctx.fillStyle = "#1e2224";
        ctx.fillRect(0, 0, worldWidth, worldHeight);
        drawWalls(ctx, map);

        playerList?.forEach((player, index) => {
            const { row, col } = getSpawnPosition(index, rows, cols);
            const x = col * CELL;
            const y = row * CELL;

            ctx.save();
            const gradient = ctx.createLinearGradient(x, y, x + CELL, y + CELL);
            gradient.addColorStop(0, player.color);
            gradient.addColorStop(1, "rgba(0,0,0,0.25)");
            ctx.globalAlpha = 0.85;
            ctx.fillStyle = gradient;
            ctx.beginPath();
            ctx.roundRect(x + 2, y + 2, CELL - 4, CELL - 4, 8);
            ctx.fill();
            ctx.globalAlpha = 1;
            drawHeroFace(ctx, x, y, "DOWN");
            ctx.restore();
        });

        ctx.restore();
    }, [map, playerList, screen]);

    return <canvas ref={canvasRef} className="rounded-xl border border-white/5" />;
}
