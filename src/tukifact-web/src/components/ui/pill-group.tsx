import { cn } from '@/lib/utils';

export interface PillOption<T extends string> {
  value: T;
  label: string;
  sub?: string;
  icon: React.ElementType;
}

interface PillGroupProps<T extends string> {
  value: T;
  onChange: (v: T) => void;
  options: readonly PillOption<T>[];
  cols?: 2 | 3 | 4 | 5;
  className?: string;
}

const COLS_CLASS: Record<number, string> = {
  2: 'grid-cols-2',
  3: 'grid-cols-2 md:grid-cols-3',
  4: 'grid-cols-2 md:grid-cols-4',
  5: 'grid-cols-2 md:grid-cols-5',
};

export function PillGroup<T extends string>({
  value,
  onChange,
  options,
  cols = 2,
  className,
}: PillGroupProps<T>) {
  return (
    <div className={cn('grid gap-2', COLS_CLASS[cols], className)}>
      {options.map((o) => {
        const Icon = o.icon;
        const active = value === o.value;
        return (
          <button
            key={o.value}
            type="button"
            onClick={() => onChange(o.value)}
            className="relative flex items-center gap-2.5 rounded-[var(--radius-md)] border px-3 py-2.5 transition-colors text-left min-w-0"
            style={{
              background: active
                ? 'color-mix(in oklch, var(--accent) 18%, transparent)'
                : 'var(--card)',
              borderColor: active ? 'var(--accent)' : 'var(--border)',
            }}
          >
            <span
              className="flex h-8 w-8 items-center justify-center rounded-md shrink-0"
              style={{
                background: active ? 'var(--accent)' : 'var(--muted)',
                color: active ? 'var(--accent-foreground)' : 'var(--muted-foreground)',
              }}
            >
              <Icon className="h-4 w-4" />
            </span>
            <span className="min-w-0 flex-1 whitespace-normal">
              <span className="block t-body-sm font-semibold leading-tight whitespace-normal">
                {o.label}
              </span>
              {o.sub && (
                <span
                  className="block t-caption leading-tight mt-0.5 whitespace-normal"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  {o.sub}
                </span>
              )}
            </span>
          </button>
        );
      })}
    </div>
  );
}
