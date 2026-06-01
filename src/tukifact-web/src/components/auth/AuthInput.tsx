'use client';

import * as React from 'react';
import type { LucideIcon } from 'lucide-react';
import { cn } from '@/lib/utils';

type Props = Omit<React.InputHTMLAttributes<HTMLInputElement>, 'size'> & {
  label?: string;
  helper?: string;
  error?: string;
  leadingIcon?: LucideIcon;
  trailingIcon?: LucideIcon;
  onTrailingClick?: () => void;
  suffix?: string;
  numeric?: boolean;
  size?: 'sm' | 'md';
  containerClassName?: string;
};

export const AuthInput = React.forwardRef<HTMLInputElement, Props>(function AuthInput(
  {
    label,
    helper,
    error,
    leadingIcon: Leading,
    trailingIcon: Trailing,
    onTrailingClick,
    suffix,
    numeric,
    size = 'md',
    id,
    name,
    className,
    containerClassName,
    style,
    ...rest
  },
  ref
) {
  const generated = React.useId();
  const inputId = id || name || generated;
  const h = size === 'sm' ? 32 : 40;
  const padL = Leading ? 36 : 12;
  const padR = Trailing || suffix ? 40 : 12;
  const helperId = helper || error ? `${inputId}-help` : undefined;

  return (
    <div className={cn('w-full', containerClassName)}>
      {label && (
        <label htmlFor={inputId} className="t-label mb-1.5 block text-foreground">
          {label}
        </label>
      )}
      <div className="relative flex items-center">
        {Leading && (
          <span className="pointer-events-none absolute left-3 flex text-[var(--muted-foreground)]">
            <Leading className="h-4 w-4" />
          </span>
        )}
        <input
          ref={ref}
          id={inputId}
          name={name}
          aria-invalid={error ? true : undefined}
          aria-describedby={helperId}
          className={cn(
            'w-full rounded-[var(--radius-md)] border bg-card text-foreground outline-none transition-[border-color,box-shadow] duration-150',
            'placeholder:text-[var(--muted-foreground)]',
            'focus-visible:border-[var(--ring)]',
            error ? 'border-[var(--danger)]' : 'border-[var(--input)]',
            numeric && 'mono tnum text-right',
            className
          )}
          style={{
            height: h,
            paddingLeft: padL,
            paddingRight: padR,
            fontSize: 14,
            ...style,
          }}
          {...rest}
        />
        {suffix && (
          <span
            className="t-body-sm mono pointer-events-none absolute right-3"
            style={{ color: 'var(--muted-foreground)' }}
          >
            {suffix}
          </span>
        )}
        {Trailing && (
          <button
            type="button"
            onClick={onTrailingClick}
            tabIndex={onTrailingClick ? 0 : -1}
            className="absolute right-2 flex p-1 text-[var(--muted-foreground)] hover:text-foreground transition-colors"
            style={{ cursor: onTrailingClick ? 'pointer' : 'default' }}
            aria-label={onTrailingClick ? 'Acción del campo' : undefined}
          >
            <Trailing className="h-4 w-4" />
          </button>
        )}
      </div>
      {(helper || error) && (
        <p
          id={helperId}
          className="t-body-sm mt-1.5"
          style={{ color: error ? 'var(--danger)' : 'var(--muted-foreground)' }}
        >
          {error || helper}
        </p>
      )}
    </div>
  );
});
