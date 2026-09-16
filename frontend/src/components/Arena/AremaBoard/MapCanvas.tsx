'use client';

import { useEffect, useRef } from "react";
import { useArenaStore } from "@/src/components/Store/useArenaStore";
import { useGameCanvasStore } from "@/src/components/Store/useGameCanvasStore";
import { useCanvasResize } from "../hooks/useCanvasResize";
import { drawWalls } from "../utils/DrawCanvasHelper";
import { CELL, WORLD_MARGIN } from "../utils/drawGame";

export default function MapCanvas() {
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const map = useArenaStore((s) => s.roomState?.map);
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
        ctx.restore();
    }, [map, screen]);

    return <canvas ref={canvasRef} className="rounded-xl border border-white/5" />;
}
