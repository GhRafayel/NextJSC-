import { HeroType, DrawGameParamsType } from "@/src/types/GameTypes/GameTypes";
import { drawHeroFace, drawBombs, drawBonuses } from "./DrawGameHelper";
import { drawWalls } from "./DrawCanvasHelper";
import { lerp } from "./canvas";

export const DEFAULT_STEP = 150 / 1000;
export const CELL = 32;
export const HERO_SCALE = 1.15;
export const WORLD_MARGIN = CELL / 2;

function drawHeroes(ctx: CanvasRenderingContext2D, heroes: HeroType[], prev: HeroType[] | undefined, alpha: number) {
    for (const hero of heroes) {
        if (!hero.alive) continue;

        const prevHero = prev?.find((h) => String(h.userId) === String(hero.userId));
        const renderX = lerp(prevHero?.position.col ?? hero.position.col, hero.position.col, alpha) * CELL;
        const renderY = lerp(prevHero?.position.row ?? hero.position.row, hero.position.row, alpha) * CELL;

        ctx.save();
        ctx.translate(renderX + CELL / 2, renderY + CELL / 2);
        ctx.scale(HERO_SCALE, HERO_SCALE);
        ctx.translate(-(renderX + CELL / 2), -(renderY + CELL / 2));

        const gradient = ctx.createLinearGradient(renderX, renderY, renderX + CELL, renderY + CELL);
        gradient.addColorStop(0, hero.color);
        gradient.addColorStop(1, "rgba(0,0,0,0.25)");
        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.roundRect(renderX + 2, renderY + 2, CELL - 4, CELL - 4, 8);
        ctx.fill();

        drawHeroFace(ctx, renderX, renderY, hero.direction);
        ctx.restore();
    }
}

export function drawGame({ ctx, curr, prev, alpha, screen, myUserId }: DrawGameParamsType) {
    const { width: SCREEN_WIDTH, height: SCREEN_HEIGHT } = screen;
    const rows = curr.map.length;
    const cols = curr.map[0]?.length ?? 0;
    const WORLD_WIDTH = cols * CELL;
    const WORLD_HEIGHT = rows * CELL;

    const myHero = curr.heroes.find((h) => String(h.userId) === String(myUserId));
    if (!myHero) {
        ctx.clearRect(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
        return;
    }

    const scale = Math.min(SCREEN_WIDTH / (WORLD_WIDTH + WORLD_MARGIN * 2), SCREEN_HEIGHT / (WORLD_HEIGHT + WORLD_MARGIN * 2));
    ctx.clearRect(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
    const background = ctx.createLinearGradient(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
    background.addColorStop(0, "#101417");
    background.addColorStop(1, "#1c2226");
    ctx.fillStyle = background;
    ctx.fillRect(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);

    ctx.save();
    ctx.scale(scale, scale);
    ctx.translate(WORLD_MARGIN, WORLD_MARGIN);

    ctx.fillStyle = "#1e2224";
    ctx.fillRect(0, 0, WORLD_WIDTH, WORLD_HEIGHT);
    drawWalls(ctx, curr.map);
    drawBonuses(ctx, curr.bonuses);
    drawBombs(ctx, curr.bombs, performance.now());
    drawHeroes(ctx, curr.heroes, prev?.heroes, alpha);

    ctx.restore();
}
