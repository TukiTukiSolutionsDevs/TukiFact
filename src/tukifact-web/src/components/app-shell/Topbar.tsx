'use client';

import { useEffect, useState } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { Menu, Search } from 'lucide-react';
import { ThemeToggle } from '@/components/theme-toggle';
import { NotificationBell } from '@/components/notifications/NotificationBell';
import { FLAT_NAV } from './Sidebar';
import { useAuth } from '@/lib/auth-context';

type Props = {
  onOpenMobile: () => void;
};

function buildBreadcrumbs(pathname: string): string[] {
  if (FLAT_NAV[pathname]) return [FLAT_NAV[pathname]];
  // Try section matches first (longest prefix)
  const keys = Object.keys(FLAT_NAV)
    .filter((k) => pathname.startsWith(k + '/'))
    .sort((a, b) => b.length - a.length);
  if (keys.length > 0) {
    const root = FLAT_NAV[keys[0]];
    const rest = pathname.slice(keys[0].length + 1).split('/')[0];
    return [root, decodeURIComponent(rest)];
  }
  const segs = pathname.split('/').filter(Boolean);
  return segs.length ? [segs[segs.length - 1]] : ['Inicio'];
}

export function Topbar({ onOpenMobile }: Props) {
  const pathname = usePathname();
  const router = useRouter();
  const { user } = useAuth();
  const crumb = buildBreadcrumbs(pathname);
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 4);
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  const userInitials = (user?.fullName || user?.email || 'TF')
    .split(/\s+/)
    .filter(Boolean)
    .map((s) => s[0]!)
    .join('')
    .slice(0, 2)
    .toUpperCase();

  return (
    <header
      className="sticky top-0 z-20 h-16 shrink-0 flex items-center gap-3 px-4 backdrop-blur-md transition-shadow"
      style={{
        background: 'color-mix(in oklch, var(--background) 80%, transparent)',
        borderBottom: scrolled ? '1px solid var(--border)' : '1px solid transparent',
      }}
    >
      <button
        type="button"
        onClick={onOpenMobile}
        aria-label="Abrir menú"
        className="lg:hidden flex h-9 w-9 items-center justify-center rounded-md hover:bg-muted transition-colors"
      >
        <Menu className="h-5 w-5" />
      </button>

      <nav
        aria-label="Breadcrumb"
        className="t-body flex items-center min-w-0"
        style={{ color: 'var(--muted-foreground)' }}
      >
        {crumb.map((c, i) => (
          <span key={`${i}-${c}`} className="inline-flex items-center min-w-0">
            {i > 0 && (
              <span className="mx-1.5" style={{ color: 'var(--slate-300)' }}>
                /
              </span>
            )}
            <span
              className="truncate"
              style={{
                fontWeight: i === crumb.length - 1 ? 600 : 400,
                color: i === crumb.length - 1 ? 'var(--foreground)' : undefined,
              }}
            >
              {c}
            </span>
          </span>
        ))}
      </nav>

      <div className="flex-1" />

      <button
        type="button"
        className="hidden md:flex items-center gap-2 h-9 px-2.5 rounded-[var(--radius-md)] border bg-card transition-colors hover:border-[var(--ring)] hover:text-foreground"
        style={{
          color: 'var(--muted-foreground)',
          width: 300,
          maxWidth: '32vw',
          borderColor: 'var(--input)',
        }}
        onClick={() => {
          /* TODO: command palette */
        }}
      >
        <Search className="h-4 w-4" />
        <span className="t-body-sm flex-1 text-left truncate">
          Buscar comprobante, cliente, producto…
        </span>
        <kbd
          className="inline-flex items-center gap-0.5 mono px-1.5 py-0.5 rounded border text-[11px]"
          style={{ borderColor: 'var(--border)', color: 'var(--muted-foreground)' }}
        >
          ⌘K
        </kbd>
      </button>

      <button
        type="button"
        className="md:hidden flex h-9 w-9 items-center justify-center rounded-md hover:bg-muted transition-colors"
        aria-label="Buscar"
      >
        <Search className="h-5 w-5" />
      </button>

      <NotificationBell />
      <ThemeToggle />

      <button
        type="button"
        onClick={() => router.push('/settings')}
        className="h-8 w-8 flex items-center justify-center rounded-[var(--radius-md)] text-[12px] font-bold text-white shrink-0"
        style={{ background: 'var(--slate-700)' }}
        aria-label="Perfil"
      >
        {userInitials}
      </button>
    </header>
  );
}
