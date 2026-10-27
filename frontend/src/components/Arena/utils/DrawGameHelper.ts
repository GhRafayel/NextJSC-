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
    const wallClockNow = Date.now();
    for (const bomb of bombs) {
        const cx = bomb.position.col * CELL + CELL / 2;
        const cy = bomb.position.row * CELL + CELL / 2;
        const remaining = Math.max(0, bomb.detonatesAt - wallClockNow);
        const urgent = remaining < 1000;
        const pulse = 1 + Math.sin(now / 120) * 0.1 * (urgent ? 2 : 1);

        ctx.save();
        ctx.translate(cx, cy);
        ctx.scale(pulse, pulse);

        ctx.beginPath();
        ctx.ellipse(0, CELL * 0.28, CELL * 0.3, CELL * 0.12, 0, 0, Math.PI * 2);
        ctx.fillStyle = "rgba(0,0,0,0.35)";
        ctx.fill();

        ctx.shadowColor = urgent ? "#ff3b1f" : "#ff8c00";
        ctx.shadowBlur = urgent ? 20 : 11;
        ctx.font = `${CELL - 2}px serif`;
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText("💣", 0, -CELL * 0.04);

        ctx.shadowBlur = 0;
        ctx.font = `${CELL * 0.45}px serif`;
        ctx.fillText("🔥", CELL * 0.22, -CELL * 0.32);
        ctx.restore();
    }
}

export function drawBlasts(ctx: CanvasRenderingContext2D, blasts: { row: number; col: number; expiresAt: number }[], now: number) {
    for (const blast of blasts) {
        const remaining = blast.expiresAt - now;
        if (remaining <= 0) continue;
        const life = Math.min(1, remaining / 400);
        const cx = blast.col * CELL + CELL / 2;
        const cy = blast.row * CELL + CELL / 2;

        ctx.save();
        ctx.globalAlpha = life;

        const gradient = ctx.createRadialGradient(cx, cy, 0, cx, cy, CELL / 2);
        gradient.addColorStop(0, "rgba(255,255,200,0.95)");
        gradient.addColorStop(0.4, "rgba(255,140,0,0.85)");
        gradient.addColorStop(1, "rgba(255,40,0,0)");
        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.arc(cx, cy, (CELL / 2) * (0.6 + 0.4 * (1 - life)), 0, Math.PI * 2);
        ctx.fill();

        ctx.font = `${CELL - 2}px serif`;
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText("🔥", cx, cy + 1);
        ctx.restore();
    }
}

// placeholder emojis until the user picks final bonus art
const BONUS_EMOJI: Record<BonusType["kind"], string> = {
    BLAST_LENGTH: "🔥",
    BOMB_COUNT: "🎁",
};

const BONUS_GLOW_COLOR: Record<BonusType["kind"], string> = {
    BLAST_LENGTH: "#ff8c42",
    BOMB_COUNT: "#4ea1ff",
};

export function drawBonuses(ctx: CanvasRenderingContext2D, bonuses: BonusType[], now: number) {
    for (const bonus of bonuses) {
        const cx = bonus.position.col * CELL + CELL / 2;
        const cy = bonus.position.row * CELL + CELL / 2;
        const color = BONUS_GLOW_COLOR[bonus.kind];
        const pulse = 1 + Math.sin(now / 220 + cx) * 0.15;

        ctx.save();

        const glowRadius = CELL * 0.75 * pulse;
        const glow = ctx.createRadialGradient(cx, cy, 0, cx, cy, glowRadius);
        glow.addColorStop(0, `${color}dd`);
        glow.addColorStop(0.55, `${color}55`);
        glow.addColorStop(1, `${color}00`);
        ctx.fillStyle = glow;
        ctx.beginPath();
        ctx.arc(cx, cy, glowRadius, 0, Math.PI * 2);
        ctx.fill();

        ctx.shadowColor = color;
        ctx.shadowBlur = 16;
        ctx.font = `${CELL - 4}px serif`;
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText(BONUS_EMOJI[bonus.kind], cx, cy + 1);
        ctx.restore();
    }
}
