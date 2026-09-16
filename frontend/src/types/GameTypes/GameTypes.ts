
export type DirectionType = 'UP' | 'DOWN' | 'LEFT' | 'RIGHT' | null;
export type GameStateType = 'START' | 'PAUSE' | 'END' | 'WIN' | 'OVER' | null;


export interface RoomStateType  {
	players: number;
	roomId: string;
	roomStatus: string;
	map?: number[][];
};

export interface RoomCountdownType {
	roomId: string;
	seconds: number;
};

export interface PositionType {
    row: number;
    col: number;
}

export interface HeroType {
	userId: number;
	position: PositionType;
	direction: DirectionType;
	alive: boolean;
	bombCount: number;
	maxBombs: number;
	blastLength: number;
	color: string;
}

export interface BombType {
	id: string;
	position: PositionType;
	ownerId: number;
	blastLength: number;
	detonatesAt: number;
}

export type BonusKindType = 'BLAST_LENGTH' | 'BOMB_COUNT';

export interface BonusType {
	id: string;
	position: PositionType;
	kind: BonusKindType;
}

export interface MatchStateType {
	roomId: string;
	map: number[][];
	heroes: HeroType[];
	bombs: BombType[];
	bonuses: BonusType[];
	status: 'playing' | 'finished';
	winnerId: number | null;
	tick: number;
}

export interface RoomInviteType {
  roomId: string;
  from: {
    id: number;
    Username: string;
  };
}
export interface DrawGameParamsType {
    ctx: CanvasRenderingContext2D;
    curr: MatchStateType;
    prev: MatchStateType | null;
    alpha: number;
    step: boolean;
    screen: {
        width: number;
        height: number;
    };
    myUserId: string | number | undefined;
}

export interface ArenaBoardType  {
    "WAITING": string;
    "PLAYING": string;
    "STARTING": string;
    "FINISHED": string;
    "connecting": string;
}