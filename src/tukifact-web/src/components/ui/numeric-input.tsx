'use client';

import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';

interface NumericInputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  currency?: 'PEN' | 'USD';
  symbol?: string;
}

export function NumericInput({
  currency = 'PEN',
  symbol,
  className,
  ...rest
}: NumericInputProps) {
  const sym = symbol ?? (currency === 'USD' ? '$' : 'S/');
  return (
    <div className="relative">
      <span
        className="absolute left-3 top-1/2 -translate-y-1/2 t-body-sm mono pointer-events-none"
        style={{ color: 'var(--muted-foreground)' }}
      >
        {sym}
      </span>
      <Input
        type="number"
        step="0.01"
        min="0"
        inputMode="decimal"
        placeholder="0.00"
        className={cn('mono tnum text-right pl-8', className)}
        {...rest}
      />
    </div>
  );
}
