import { cn } from '@/lib/utils';

interface SectionProps {
  title?: string;
  desc?: string;
  right?: React.ReactNode;
  className?: string;
  bodyClassName?: string;
  children: React.ReactNode;
}

export function Section({ title, desc, right, className, bodyClassName, children }: SectionProps) {
  return (
    <section
      className={cn('rounded-[var(--radius-lg)] border bg-card p-6', className)}
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      {(title || right) && (
        <div className="flex items-start justify-between gap-3 mb-4">
          <div>
            {title && <h2 className="t-h2 m-0">{title}</h2>}
            {desc && (
              <p className="t-body-sm m-0 mt-0.5" style={{ color: 'var(--muted-foreground)' }}>
                {desc}
              </p>
            )}
          </div>
          {right}
        </div>
      )}
      <div className={bodyClassName}>{children}</div>
    </section>
  );
}
