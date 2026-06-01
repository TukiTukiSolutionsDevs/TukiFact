import { cn } from '@/lib/utils';

interface ToolbarProps {
  children: React.ReactNode;
  className?: string;
}

export function Toolbar({ children, className }: ToolbarProps) {
  return (
    <div
      className={cn(
        'rounded-[var(--radius-lg)] border bg-card p-4 mb-[var(--gap-cards)] flex flex-wrap items-end gap-3',
        className
      )}
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      {children}
    </div>
  );
}

interface ChipProps {
  active?: boolean;
  onClick?: () => void;
  children: React.ReactNode;
  className?: string;
}

export function Chip({ active, onClick, children, className }: ChipProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        't-caption font-semibold px-2.5 py-1.5 rounded-full transition-colors',
        className
      )}
      style={{
        background: active
          ? 'color-mix(in oklch, var(--accent) 18%, transparent)'
          : 'var(--muted)',
        color: active ? 'var(--brand-ink)' : 'var(--muted-foreground)',
        border: `1px solid ${active ? 'var(--accent)' : 'transparent'}`,
      }}
    >
      {children}
    </button>
  );
}

interface ChipGroupProps<T extends string> {
  value: T;
  onChange: (v: T) => void;
  options: readonly { value: T; label: string }[];
  className?: string;
}

export function ChipGroup<T extends string>({
  value,
  onChange,
  options,
  className,
}: ChipGroupProps<T>) {
  return (
    <div className={cn('flex gap-2 flex-wrap', className)}>
      {options.map((o) => (
        <Chip key={o.value || 'all'} active={value === o.value} onClick={() => onChange(o.value)}>
          {o.label}
        </Chip>
      ))}
    </div>
  );
}
