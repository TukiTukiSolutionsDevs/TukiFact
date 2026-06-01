import { cn } from '@/lib/utils';
import { type LucideIcon } from 'lucide-react';

interface KpiCardProps {
  label: string;
  value: string;
  icon: LucideIcon;
  accent: string;
  span?: 1 | 2;
  className?: string;
}

export function KpiCard({ label, value, icon: Icon, accent, span = 1, className }: KpiCardProps) {
  return (
    <div
      className={cn(
        'rounded-[var(--radius-lg)] border bg-card p-5 flex items-center gap-3.5',
        span === 2 && 'sm:col-span-2',
        className
      )}
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      <span
        className="flex h-11 w-11 items-center justify-center rounded-xl shrink-0"
        style={{
          background: `color-mix(in oklch, ${accent} 14%, transparent)`,
          color: accent,
        }}
      >
        <Icon className="h-5 w-5" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
          {label}
        </p>
        <p className="t-num-md mono tnum mt-0.5">{value}</p>
      </div>
    </div>
  );
}
