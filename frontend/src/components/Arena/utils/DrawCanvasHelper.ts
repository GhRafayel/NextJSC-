import { CELL } from "./drawGame";

let gridCache: { key: string; canvas: HTMLCanvasElement; } | null = null;

export function getGridCanvas( worldWidth: number, worldHeight: number ): HTMLCanvasElement {
    const key = `${worldWidth}x${worldHeight}`;
    if (gridCache && gridCache.key === key) {
        return gridCache.canvas;
    }
    const canvas = document.createElement("canvas");
    canvas.width = worldWidth;
    canvas.height = worldHeight;
    const gctx = canvas.getContext("2d");

    if (!gctx)  throw new Error("Could not create grid canvas context");

    gctx.fillStyle = "#15191c";
    gctx.fillRect(0, 0, worldWidth, worldHeight);
    getGridCanvasDoublLoop(gctx, worldWidth, worldHeight);
    gctx.strokeStyle = "rgba(255, 255, 255, 0.025)";
    gctx.lineWidth = 1;
    getGridCanvasLoop(gctx, worldWidth);
    getGridCanvasLoop(gctx, worldHeight);
    gridCache = { key, canvas, };
    return canvas;
};

export function getGridCanvasLoop(gctx: CanvasRenderingContext2D, world : number) {
    for (let x = 0; x <= world; x += CELL) {
            gctx.beginPath();
            gctx.moveTo(x + 0.5, 0);
            gctx.lineTo(x + 0.5, world);
            gctx.stroke();
    }
}

export function getGridCanvasDoublLoop (gctx: CanvasRenderingContext2D, worldWidth: number, worldHeight: number ) {
    for (let x = 0; x < worldWidth; x += CELL) {
        for (let y = 0; y < worldHeight; y += CELL) {
            gctx.fillStyle = "#202529";
            gctx.fillRect( x + 1, y + 1, CELL - 2, CELL - 2 );
            gctx.fillStyle = "#292f34";
            gctx.fillRect( x + 1, y + 1, 1, 1 );
        }
    }
}

export function drawWalls(ctx: CanvasRenderingContext2D, map: number[][]) {
    for (let row = 0; row < map.length; row++) {
        for (let col = 0; col < map[row].length; col++) {
            if (map[row][col] !== 1) continue;
            ctx.fillStyle = "#4a4a4a";
            ctx.fillRect(col * CELL, row * CELL, CELL, CELL);
            ctx.fillStyle = "rgba(255,255,255,0.06)";
            ctx.fillRect(col * CELL, row * CELL, CELL, 2);
        }
    }
}
