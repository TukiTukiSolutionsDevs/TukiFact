import { CircleDot, type LucideIcon } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface TimelineItem {
  title: string;
  body?: string;
  time?: string;
  icon?: LucideIcon;
  color?: string;
}

interface TimelineProps {
  items: TimelineItem[];
  className?: string;
}

export function Timeline({ items, className }: TimelineProps) {
  return (
    <div className={cn('relative pl-1', className)}>
      {items.map((it, i) => {
        const Icon = it.icon ?? CircleDot;
        const color = it.color ?? 'var(--slate-400)';
        const isLast = i === items.length - 1;
        return (
          <div
            key={i}
            className="relative flex gap-3.5"
            style={{ paddingBottom: isLast ? 0 : 22 }}
          >
            {!isLast && (
              <span
                className="absolute top-[22px] bottom-0 w-[2px]"
                style={{ left: 9, background: 'var(--border)' }}
                aria-hidden
              />
            )}
            <span
              className="flex h-5 w-5 items-center justify-center rounded-full shrink-0 mt-px z-10"
              style={{ background: `color-mix(in oklch, ${color} 16%, transparent)` }}
            >
              <Icon className="h-3 w-3" style={{ color }} />
            </span>
            <div className="flex-1 min-w-0">
              <div className="flex items-baseline justify-between gap-2 flex-wrap">
                <span className="t-body-sm font-semibold">{it.title}</span>
                {it.time && (
                  <span
                    className="t-caption mono"
                    style={{ color: 'var(--muted-foreground)' }}
                  >
                    {it.time}
                  </span>
                )}
              </div>
              {it.body && (
                <p
                  className="t-body-sm mt-0.5 m-0"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  {it.body}
                </p>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
