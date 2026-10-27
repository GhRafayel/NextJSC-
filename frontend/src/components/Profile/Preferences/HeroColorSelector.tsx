'use client';
import { Palette } from 'lucide-react';
import Image from 'next/image';
import { useAuth } from '@/src/components/Provider/UserProvider';

const HERO_COLORS = [
    { color: '#22c55e', avatar: 'hero-green.svg' },
    { color: '#22d3ee', avatar: 'hero-cyan.svg' },
    { color: '#d946ef', avatar: 'hero-magenta.svg' },
    { color: '#f97316', avatar: 'hero-orange.svg' },
    { color: '#fbbf24', avatar: 'hero-yellow.svg' },
    { color: '#ef4444', avatar: 'hero-red.svg' },
    { color: '#a3e635', avatar: 'hero-lime.svg' },
    { color: '#f472b6', avatar: 'hero-pink.svg' },
];

export default function HeroColorSelector() {
  const { cntUser, LENUAGE, ChangingCallback } = useAuth();
  const PF_LENG = LENUAGE.Profile.settings;

  return (
    <div className="pf-row pf-rowStack">
        <div className="pf-lbl">
          <span className="pf-iconWrap"><Palette size={16} /></span>
          {PF_LENG.heroColor}
        </div>
        <div className="pf-val">
            <div className="pf-swatchGroup ">
              {HERO_COLORS.map(({ color, avatar }) => (
                <button key={color} type="button" aria-label={color}
                  className={`pf-swatch ${cntUser?.color === color ? 'pf-swatchActive' : ''}`}
                  onClick={async () => ChangingCallback({color}, "change-color")}
                >
                  <Image src={`/avatar/${avatar}`} alt="" width={28} height={28} className="h-full w-full object-cover" />
                </button>
              ))}
            </div>
        </div>
    </div>
  );
}
