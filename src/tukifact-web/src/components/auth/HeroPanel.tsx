import Image from 'next/image';
import { CheckCircle2 } from 'lucide-react';

type Props = {
  headline: string;
  sub: string;
  bullets?: string[];
};

export function HeroPanel({ headline, sub, bullets }: Props) {
  return (
    <div className="relative hidden lg:flex flex-col overflow-hidden bg-[var(--hero-panel)] text-[var(--hero-foreground)] px-12 py-10">
      <div
        aria-hidden
        className="pointer-events-none absolute -top-32 -right-32 h-[380px] w-[380px] rounded-full opacity-90"
        style={{ background: 'radial-gradient(circle, color-mix(in oklch, var(--brand-toucan-yellow) 22%, transparent), transparent 70%)' }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute -bottom-24 -left-24 h-[300px] w-[300px] rounded-full opacity-90"
        style={{ background: 'radial-gradient(circle, color-mix(in oklch, var(--brand-toucan-orange) 16%, transparent), transparent 70%)' }}
      />

      <div className="relative z-10 flex items-center gap-2.5">
        <Image src="/icon.png" alt="" width={38} height={38} className="object-contain" />
        <span className="brand-wordmark text-[24px] font-semibold leading-none">
          <span className="text-white">Tuki</span>
          <span style={{ color: 'var(--brand-toucan-yellow)' }}>Fact</span>
        </span>
      </div>

      <div className="relative z-10 my-auto max-w-[420px] flex flex-col">
        <Image
          src="/icon.png"
          alt=""
          width={132}
          height={132}
          className="object-contain mb-7 drop-shadow-[0_12px_28px_rgba(0,0,0,0.4)]"
        />
        <h1 className="t-display-xl text-white m-0" style={{ textWrap: 'balance' as 'balance' }}>
          {headline}
        </h1>
        <p className="t-body-lg mt-3.5 mb-0" style={{ color: 'var(--slate-300)' }}>
          {sub}
        </p>
        {bullets && (
          <ul className="list-none p-0 mt-7 flex flex-col gap-3.5">
            {bullets.map((b) => (
              <li key={b} className="t-body flex items-center gap-3" style={{ color: 'var(--slate-200)' }}>
                <span
                  className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full"
                  style={{ background: 'color-mix(in oklch, var(--accent) 20%, transparent)' }}
                >
                  <CheckCircle2 className="h-4 w-4" style={{ color: 'var(--accent)' }} />
                </span>
                {b}
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="relative z-10 flex gap-4 t-caption" style={{ color: 'var(--slate-400)' }}>
        <a href="/privacy" className="hover:text-white no-underline" style={{ color: 'inherit' }}>
          Privacidad
        </a>
        <a href="/terms" className="hover:text-white no-underline" style={{ color: 'inherit' }}>
          Términos
        </a>
        <span>© 2026 TukiFact</span>
      </div>
    </div>
  );
}
