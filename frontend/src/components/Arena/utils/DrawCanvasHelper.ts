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

const BRICK_SHADES = ["#c2703f", "#b8632f", "#c97a44", "#a85a2d"];

function brickShade(seed: number) {
    return BRICK_SHADES[((seed % BRICK_SHADES.length) + BRICK_SHADES.length) % BRICK_SHADES.length];
}

function drawBrickWall(ctx: CanvasRenderingContext2D, x: number, y: number, row: number, col: number) {
    const half = CELL / 2;

    ctx.fillStyle = "#e8cbb0";
    ctx.fillRect(x, y, CELL, CELL);

    const seed = row * 7 + col * 3;
    ctx.fillStyle = brickShade(seed);
    ctx.fillRect(x + 1, y + 1, half - 2, half - 2);
    ctx.fillStyle = brickShade(seed + 1);
    ctx.fillRect(x + half + 1, y + 1, half - 2, half - 2);
    ctx.fillStyle = brickShade(seed + 2);
    ctx.fillRect(x + 1, y + half + 1, CELL - 2, half - 2);
}

export function drawWalls(ctx: CanvasRenderingContext2D, map: number[][]) {
    for (let row = 0; row < map.length; row++) {
        for (let col = 0; col < map[row].length; col++) {
            const cell = map[row][col];
            if (cell === 1) {
                ctx.fillStyle = "#4a4a4a";
                ctx.fillRect(col * CELL, row * CELL, CELL, CELL);
                ctx.fillStyle = "rgba(255,255,255,0.06)";
                ctx.fillRect(col * CELL, row * CELL, CELL, 2);
            } else if (cell === 2) {
                drawBrickWall(ctx, col * CELL, row * CELL, row, col);
            }
        }
    }
}
