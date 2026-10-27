import { create } from 'zustand';
import { MatchStateType, GameStateType, PositionType } from "@/src/types/GameTypes/GameTypes";
import { DEFAULT_STEP } from '@/src/components/Arena/utils/drawGame';

export interface ActiveBlastType extends PositionType {
    expiresAt: number;
}

interface GameCanvasStoreType {
    prevGame: MatchStateType | null;
    currGame: MatchStateType | null;
    stateTime: number;
    alpha: number;
    step: boolean;
    stepSeconds: number;
    screen: { width: number; height: number };
    internalGameState: GameStateType;
    activeBlasts: ActiveBlastType[];

    setGames: (curr: MatchStateType) => void;
    setStateTime: (time: number) => void;
    setAlpha: (alpha: number) => void;
    toggleStep: () => void;
    setStepSeconds: (seconds: number) => void;
    setScreen: (screen: { width: number; height: number }) => void;
    setInternalGameState: (state: GameStateType) => void;
    addBlasts: (cells: PositionType[], durationMs: number) => void;
    resetGameCanvas: () => void;
}

export const useGameCanvasStore = create<GameCanvasStoreType>((set) => ({
    prevGame: null,
    currGame: null,
    stateTime: 0,
    alpha: 0,
    step: false,
    stepSeconds: DEFAULT_STEP,
    screen: { width: 0, height: 0 },
    internalGameState: 'START',
    activeBlasts: [],

    setGames: (curr) => set((state) => ({ prevGame: state.currGame, currGame: curr })),
    setStateTime: (time) => set({ stateTime: time }),
    setAlpha: (alpha) => set({ alpha }),
    toggleStep: () => set((state) => ({ step: !state.step })),
    setStepSeconds: (seconds) => set({ stepSeconds: seconds }),
    setScreen: (screen) => set({ screen }),
    setInternalGameState: (state) => set({ internalGameState: state }),
    addBlasts: (cells, durationMs) =>
        set((state) => {
            const now = Date.now();
            const expiresAt = now + durationMs;
            const kept = state.activeBlasts.filter((b) => b.expiresAt > now);
            return { activeBlasts: [...kept, ...cells.map((c) => ({ ...c, expiresAt }))] };
        }),

    resetGameCanvas: () =>
        set({
            prevGame: null,
            currGame: null,
            stateTime: 0,
            alpha: 0,
            step: false,
            stepSeconds: DEFAULT_STEP,
            screen: { width: 0, height: 0 },
            internalGameState: 'START',
            activeBlasts: [],
        }),
}));
