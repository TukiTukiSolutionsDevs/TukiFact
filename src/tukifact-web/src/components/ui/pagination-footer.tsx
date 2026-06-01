import { Button } from '@/components/ui/button';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';

interface PaginationFooterProps {
  page: number;
  totalPages: number;
  totalCount: number;
  onPrev: () => void;
  onNext: () => void;
  className?: string;
}

export function PaginationFooter({
  page,
  totalPages,
  totalCount,
  onPrev,
  onNext,
  className,
}: PaginationFooterProps) {
  if (totalPages <= 1) return null;
  return (
    <div
      className={cn(
        'flex items-center justify-between flex-wrap gap-3',
        className
      )}
    >
      <p className="t-body-sm" style={{ color: 'var(--muted-foreground)' }}>
        Página <span className="mono tnum font-semibold">{page}</span> de{' '}
        <span className="mono tnum font-semibold">{totalPages}</span> ·{' '}
        <span className="mono tnum">{totalCount}</span> total
      </p>
      <div className="flex gap-2">
        <Button variant="outline" size="sm" disabled={page <= 1} onClick={onPrev}>
          <ChevronLeft className="h-4 w-4 mr-1" /> Anterior
        </Button>
        <Button
          variant="outline"
          size="sm"
          disabled={page >= totalPages}
          onClick={onNext}
        >
          Siguiente <ChevronRight className="h-4 w-4 ml-1" />
        </Button>
      </div>
    </div>
  );
}
