import { DirectionType, BombType, BonusType } from "@/src/types/GameTypes/GameTypes";
import { CELL } from "./drawGame";

export function drawHeroFace(ctx: CanvasRenderingContext2D, renderX: number, renderY: number, facing: DirectionType) {
    let eye1 = { x: 0, y: 0 };
    let eye2 = { x: 0, y: 0 };

    if (facing === "RIGHT") {
        eye1 = { x: renderX + CELL - 8, y: renderY + 4 };
        eye2 = { x: renderX + CELL - 8, y: renderY + CELL - 9 };
    } else if (facing === "LEFT") {
        eye1 = { x: renderX + 4, y: renderY + 4 };
        eye2 = { x: renderX + 4, y: renderY + CELL - 9 };
    } else if (facing === "UP") {
        eye1 = { x: renderX + 4, y: renderY + 4 };
        eye2 = { x: renderX + CELL - 9, y: renderY + 4 };
    } else {
        eye1 = { x: renderX + 4, y: renderY + CELL - 8 };
        eye2 = { x: renderX + CELL - 9, y: renderY + CELL - 8 };
    }

    ctx.fillStyle = "#ffffff";
    ctx.beginPath();
    ctx.arc(eye1.x + 2, eye1.y + 2, 2.2, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(eye2.x + 2, eye2.y + 2, 2.2, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = "#111111";
    ctx.beginPath();
    ctx.arc(eye1.x + 2, eye1.y + 2, 1, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(eye2.x + 2, eye2.y + 2, 1, 0, Math.PI * 2);
    ctx.fill();
}

export function drawBombs(ctx: CanvasRenderingContext2D, bombs: BombType[], now: number) {
    for (const bomb of bombs) {
        const cx = bomb.position.col * CELL + CELL / 2;
        const cy = bomb.position.row * CELL + CELL / 2;
        const remaining = Math.max(0, bomb.detonatesAt - now);
        const pulse = 1 + Math.sin(now / 120) * 0.08 * (remaining < 1000 ? 2 : 1);

        ctx.save();
        ctx.fillStyle = "#1c1c1c";
        ctx.beginPath();
        ctx.arc(cx, cy, (CELL / 2 - 3) * pulse, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = "rgba(255,255,255,0.15)";
        ctx.beginPath();
        ctx.arc(cx - 3, cy - 3, 2.5, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
    }
}

const BONUS_COLORS: Record<BonusType["kind"], string> = {
    BLAST_LENGTH: "#ff8c42",
    BOMB_COUNT: "#4ea1ff",
};

export function drawBonuses(ctx: CanvasRenderingContext2D, bonuses: BonusType[]) {
    for (const bonus of bonuses) {
        const cx = bonus.position.col * CELL + CELL / 2;
        const cy = bonus.position.row * CELL + CELL / 2;

        ctx.save();
        ctx.fillStyle = BONUS_COLORS[bonus.kind];
        ctx.shadowColor = BONUS_COLORS[bonus.kind];
        ctx.shadowBlur = 6;
        ctx.beginPath();
        ctx.roundRect(cx - CELL / 4, cy - CELL / 4, CELL / 2, CELL / 2, 4);
        ctx.fill();
        ctx.restore();
    }
}
