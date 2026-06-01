'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { usePathname, useRouter } from 'next/navigation';
import {
  LayoutDashboard,
  FileText,
  FileSpreadsheet,
  Truck,
  Repeat,
  Package,
  Contact,
  ListOrdered,
  BarChart3,
  Ban,
  DollarSign,
  ShieldAlert,
  ShieldCheck,
  KeyRound,
  Webhook,
  Bot,
  Users,
  Shield,
  CreditCard,
  ScrollText,
  Settings,
  Plus,
  ChevronLeft,
  ChevronRight,
  ChevronsUpDown,
  Crown,
  ArrowUpRight,
  LogOut,
  X,
  User as UserIcon,
  LifeBuoy,
  BookOpen,
  type LucideIcon,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/lib/auth-context';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

type NavItem = { href: string; label: string; icon: LucideIcon; badge?: number };
type NavGroup = { group: string; items: NavItem[] };

const NAV: NavGroup[] = [
  {
    group: 'Operación',
    items: [
      { href: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
      { href: '/documents', label: 'Comprobantes', icon: FileText },
      { href: '/quotations', label: 'Cotizaciones', icon: FileSpreadsheet },
      { href: '/despatch-advices', label: 'Guías de remisión', icon: Truck },
      { href: '/recurring-invoices', label: 'Recurrentes', icon: Repeat },
    ],
  },
  {
    group: 'Catálogo',
    items: [
      { href: '/products', label: 'Productos', icon: Package },
      { href: '/customers', label: 'Clientes', icon: Contact },
      { href: '/series', label: 'Series', icon: ListOrdered },
      { href: '/catalogs', label: 'Catálogos SUNAT', icon: BookOpen },
    ],
  },
  {
    group: 'Finanzas',
    items: [
      { href: '/reports', label: 'Reportes', icon: BarChart3 },
      { href: '/voided', label: 'Anulados', icon: Ban },
      { href: '/exchange-rates', label: 'Tipos de cambio', icon: DollarSign },
      { href: '/perceptions', label: 'Percepciones', icon: ShieldAlert },
      { href: '/retentions', label: 'Retenciones', icon: ShieldCheck },
    ],
  },
  {
    group: 'Integración',
    items: [
      { href: '/api-keys', label: 'API Keys', icon: KeyRound },
      { href: '/webhooks', label: 'Webhooks', icon: Webhook },
      { href: '/ai', label: 'Asistente IA', icon: Bot },
    ],
  },
];

const ADMIN_GROUP: NavGroup = {
  group: 'Configuración',
  items: [
    { href: '/users', label: 'Usuarios', icon: Users },
    { href: '/certificate', label: 'Certificado digital', icon: Shield },
    { href: '/plan', label: 'Plan', icon: Crown },
    { href: '/audit-log', label: 'Audit log', icon: ScrollText },
    { href: '/settings', label: 'Empresa', icon: Settings },
  ],
};

type Props = {
  collapsed: boolean;
  setCollapsed: (v: boolean) => void;
  mobileOpen: boolean;
  setMobileOpen: (v: boolean) => void;
};

function Brand({ collapsed }: { collapsed: boolean }) {
  return (
    <div className="flex min-w-0 items-center gap-2.5">
      <Image src="/icon.png" alt="TukiFact" width={34} height={34} className="object-contain shrink-0" />
      {!collapsed && (
        <div className="flex flex-col leading-none min-w-0">
          <span className="brand-wordmark text-[21px] font-semibold whitespace-nowrap">
            <span style={{ color: 'var(--sidebar-foreground)' }}>Tuki</span>
            <span style={{ color: 'var(--brand-toucan-yellow)' }}>Fact</span>
          </span>
          <span
            className="t-overline mt-0.5"
            style={{ color: 'var(--muted-foreground)', fontSize: 8, letterSpacing: '0.14em' }}
          >
            Facturación inteligente
          </span>
        </div>
      )}
    </div>
  );
}

function CompanySwitcher({
  collapsed,
  initials,
  primary,
  secondary,
}: {
  collapsed: boolean;
  initials: string;
  primary: string;
  secondary: string;
}) {
  if (collapsed) {
    return (
      <div className="flex justify-center py-2.5 border-b border-[var(--sidebar-border)]">
        <span
          className="flex h-8 w-8 items-center justify-center rounded-[var(--radius-md)] text-[12px] font-bold"
          style={{
            background: 'var(--accent)',
            color: 'var(--brand-ink)',
            letterSpacing: '-0.02em',
          }}
        >
          {initials}
        </span>
      </div>
    );
  }
  return (
    <button
      type="button"
      className="flex w-full items-center gap-2.5 px-4 py-3 text-left border-b border-[var(--sidebar-border)] hover:bg-[var(--sidebar-accent)] transition-colors"
    >
      <span
        className="flex h-9 w-9 items-center justify-center rounded-[var(--radius-md)] text-[13px] font-bold shrink-0"
        style={{
          background: 'var(--accent)',
          color: 'var(--brand-ink)',
          letterSpacing: '-0.02em',
        }}
      >
        {initials}
      </span>
      <div className="min-w-0 flex-1">
        <div
          className="t-label truncate"
          style={{ color: 'var(--sidebar-foreground)', fontWeight: 600 }}
        >
          {primary}
        </div>
        <div className="t-caption mono truncate" style={{ color: 'var(--muted-foreground)' }}>
          {secondary}
        </div>
      </div>
      <ChevronsUpDown className="h-4 w-4" style={{ color: 'var(--muted-foreground)' }} />
    </button>
  );
}

function NavRow({
  item,
  active,
  collapsed,
  onNavigate,
}: {
  item: NavItem;
  active: boolean;
  collapsed: boolean;
  onNavigate?: () => void;
}) {
  const Icon = item.icon;
  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      title={collapsed ? item.label : undefined}
      className={cn(
        'relative flex items-center rounded-[var(--radius-md)] transition-colors',
        collapsed
          ? 'mx-auto my-0.5 h-10 w-10 justify-center'
          : 'mx-2 my-0.5 h-10 gap-3 px-3'
      )}
      style={{
        background: active ? 'var(--sidebar-accent)' : 'transparent',
        color: active ? 'var(--sidebar-accent-foreground)' : 'var(--sidebar-foreground)',
      }}
    >
      {active && !collapsed && (
        <span
          className="absolute -left-2 top-1.5 bottom-1.5 w-[3px] rounded-full"
          style={{ background: 'var(--accent)' }}
        />
      )}
      <span className="relative flex h-[18px] w-[18px] items-center justify-center shrink-0">
        {active && (
          <span
            className="absolute -inset-1.5 rounded-md"
            style={{ background: 'var(--accent)', opacity: 0.15 }}
          />
        )}
        <Icon
          className="relative h-[18px] w-[18px]"
          strokeWidth={active ? 2 : 1.5}
          style={{
            color: active ? 'var(--sidebar-accent-foreground)' : 'var(--slate-500)',
          }}
        />
      </span>
      {!collapsed && (
        <>
          <span
            className="t-body flex-1 whitespace-nowrap"
            style={{ fontWeight: active ? 600 : 400 }}
          >
            {item.label}
          </span>
          {item.badge && (
            <span
              className="t-caption rounded-full px-2 py-[1px] font-semibold"
              style={{
                background: 'color-mix(in oklch, var(--warning) 14%, transparent)',
                color: 'var(--warning)',
              }}
            >
              {item.badge}
            </span>
          )}
        </>
      )}
    </Link>
  );
}

export function Sidebar({ collapsed, setCollapsed, mobileOpen, setMobileOpen }: Props) {
  const pathname = usePathname();
  const router = useRouter();
  const { user, logout } = useAuth();
  const isAdmin = user?.role === 'admin' || user?.role === 'superadmin';

  const groups = isAdmin ? [...NAV, ADMIN_GROUP] : NAV;

  const isActive = (href: string) =>
    pathname === href || pathname.startsWith(href + '/');

  // Company switcher data — derived from current user
  const companyInitials = (user?.fullName || user?.email || 'TF')
    .split(/[\s@.]/)
    .filter(Boolean)
    .map((s) => s[0]!)
    .join('')
    .slice(0, 2)
    .toUpperCase();
  const companyPrimary = user?.fullName || user?.email || 'Mi empresa';
  const companySecondary = user?.tenantId
    ? 'ID ' + user.tenantId.slice(0, 8)
    : '';

  const userInitials = (user?.fullName || user?.email || 'TF')
    .split(/\s+/)
    .filter(Boolean)
    .map((s) => s[0]!)
    .join('')
    .slice(0, 2)
    .toUpperCase();

  const handleLogout = () => {
    logout();
    router.push('/login');
  };

  const onItemClick = () => setMobileOpen(false);

  return (
    <>
      {/* Mobile overlay */}
      {mobileOpen && (
        <div
          className="fixed inset-0 z-20 bg-black/40 lg:hidden"
          onClick={() => setMobileOpen(false)}
          aria-hidden
        />
      )}

      <aside
        className={cn(
          'flex flex-col shrink-0 transition-[width,transform] duration-200 ease-out',
          'fixed inset-y-0 left-0 z-30 lg:static lg:translate-x-0',
          mobileOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'
        )}
        style={{
          width: collapsed ? 72 : 256,
          background: 'var(--sidebar)',
          borderRight: '1px solid var(--sidebar-border)',
        }}
      >
        {/* Brand header */}
        <div
          className={cn(
            'flex h-16 shrink-0 items-center border-b',
            collapsed ? 'justify-center px-0' : 'justify-between pl-5 pr-3'
          )}
          style={{ borderColor: 'var(--sidebar-border)' }}
        >
          <Brand collapsed={collapsed} />
          {!collapsed && (
            <div className="flex items-center">
              <button
                onClick={() => setCollapsed(true)}
                className="hidden lg:flex h-8 w-8 items-center justify-center rounded-md hover:bg-[var(--sidebar-accent)] transition-colors"
                aria-label="Colapsar"
              >
                <ChevronLeft className="h-4 w-4" style={{ color: 'var(--muted-foreground)' }} />
              </button>
              <button
                onClick={() => setMobileOpen(false)}
                className="lg:hidden h-8 w-8 flex items-center justify-center rounded-md hover:bg-[var(--sidebar-accent)] transition-colors"
                aria-label="Cerrar"
              >
                <X className="h-4 w-4" style={{ color: 'var(--muted-foreground)' }} />
              </button>
            </div>
          )}
        </div>

        {collapsed && (
          <div className="flex justify-center pt-2">
            <button
              onClick={() => setCollapsed(false)}
              className="h-8 w-8 flex items-center justify-center rounded-md hover:bg-[var(--sidebar-accent)] transition-colors"
              aria-label="Expandir"
            >
              <ChevronRight className="h-4 w-4" style={{ color: 'var(--muted-foreground)' }} />
            </button>
          </div>
        )}

        {/* Company switcher */}
        <CompanySwitcher
          collapsed={collapsed}
          initials={companyInitials}
          primary={companyPrimary}
          secondary={companySecondary}
        />

        {/* Primary CTA */}
        <div className={cn('shrink-0', collapsed ? 'py-3 flex justify-center' : 'p-4')}>
          {collapsed ? (
            <Link
              href="/documents/new"
              onClick={onItemClick}
              title="Emitir comprobante"
              className="flex h-10 w-10 items-center justify-center rounded-[var(--radius-md)] font-semibold transition-opacity hover:opacity-90"
              style={{ background: 'var(--accent)', color: 'var(--accent-foreground)' }}
            >
              <Plus className="h-5 w-5" />
            </Link>
          ) : (
            <Link
              href="/documents/new"
              onClick={onItemClick}
              className="flex h-10 w-full items-center justify-center gap-2 rounded-[var(--radius-md)] font-semibold text-[14px] transition-opacity hover:opacity-90"
              style={{ background: 'var(--accent)', color: 'var(--accent-foreground)' }}
            >
              <Plus className="h-4 w-4" />
              Emitir comprobante
            </Link>
          )}
        </div>

        {/* Nav */}
        <nav className="flex-1 overflow-y-auto pb-2">
          {groups.map((g) => (
            <div key={g.group} className="mb-1">
              {!collapsed ? (
                <div
                  className="t-overline px-4 pt-3 pb-1.5"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  {g.group}
                </div>
              ) : (
                <div
                  className="mx-4 my-2"
                  style={{ height: 1, background: 'var(--sidebar-border)' }}
                />
              )}
              {g.items.map((it) => (
                <NavRow
                  key={it.href}
                  item={it}
                  active={isActive(it.href)}
                  collapsed={collapsed}
                  onNavigate={onItemClick}
                />
              ))}
            </div>
          ))}
        </nav>

        {/* Footer: plan + user */}
        <div className="shrink-0 border-t" style={{ borderColor: 'var(--sidebar-border)' }}>
          {!collapsed && (
            <div className="px-4 py-3 border-b" style={{ borderColor: 'var(--sidebar-border)' }}>
              <div className="flex items-center justify-between mb-2">
                <span className="t-label inline-flex items-center gap-1.5">
                  <Crown className="h-3.5 w-3.5" style={{ color: 'var(--brand-toucan-orange)' }} />
                  Plan Pro
                </span>
                <span className="t-caption tnum" style={{ color: 'var(--muted-foreground)' }}>
                  1,284 / 3,000
                </span>
              </div>
              <div
                className="w-full overflow-hidden rounded-full"
                style={{ height: 6, background: 'var(--muted)' }}
              >
                <div
                  className="h-full rounded-full"
                  style={{
                    width: `${Math.min(100, (1284 / 3000) * 100)}%`,
                    background: 'var(--accent)',
                    transition: 'width 400ms var(--ease-out)',
                  }}
                />
              </div>
              <Link
                href="/plan"
                onClick={onItemClick}
                className="t-caption mt-2 inline-flex items-center gap-1 font-semibold"
                style={{ color: 'var(--info)' }}
              >
                Mejorar plan <ArrowUpRight className="h-3.5 w-3.5" />
              </Link>
            </div>
          )}

          <div className={cn(collapsed ? 'py-2.5 flex justify-center' : 'p-3')}>
            <DropdownMenu>
              <DropdownMenuTrigger
                className={cn(
                  'flex items-center rounded-md outline-none transition-colors hover:bg-[var(--sidebar-accent)]',
                  collapsed ? 'h-10 w-10 justify-center' : 'w-full gap-2.5 px-2 py-1.5'
                )}
              >
                <span
                  className="flex h-8 w-8 items-center justify-center rounded-[var(--radius-md)] text-[12px] font-bold shrink-0 text-white"
                  style={{ background: 'var(--slate-700)' }}
                >
                  {userInitials}
                </span>
                {!collapsed && (
                  <>
                    <div className="min-w-0 flex-1 text-left">
                      <div
                        className="t-body-sm truncate"
                        style={{ fontWeight: 600 }}
                      >
                        {user?.fullName || user?.email}
                      </div>
                      <div
                        className="t-caption truncate"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        {user?.email}
                      </div>
                    </div>
                    <ChevronsUpDown className="h-3.5 w-3.5 shrink-0" style={{ color: 'var(--muted-foreground)' }} />
                  </>
                )}
              </DropdownMenuTrigger>
              <DropdownMenuContent side="top" align="start" className="w-56">
                <DropdownMenuItem onClick={() => router.push('/settings')}>
                  <UserIcon className="mr-2 h-4 w-4" /> Mi perfil
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => router.push('/settings')}>
                  <Settings className="mr-2 h-4 w-4" /> Ajustes
                </DropdownMenuItem>
                <DropdownMenuItem>
                  <LifeBuoy className="mr-2 h-4 w-4" /> Soporte
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem variant="destructive" onClick={handleLogout}>
                  <LogOut className="mr-2 h-4 w-4" /> Cerrar sesión
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      </aside>
    </>
  );
}

export function useSidebarCollapsed() {
  const [collapsed, setCollapsedState] = useState(false);

  useEffect(() => {
    const stored = localStorage.getItem('tf-sidebar-collapsed');
    if (stored === '1') setCollapsedState(true);
  }, []);

  const setCollapsed = (v: boolean) => {
    setCollapsedState(v);
    try {
      localStorage.setItem('tf-sidebar-collapsed', v ? '1' : '0');
    } catch {
      /* ignore */
    }
  };

  return [collapsed, setCollapsed] as const;
}

// Re-export the NAV map so Topbar can build breadcrumbs
export const FLAT_NAV: Record<string, string> = (() => {
  const m: Record<string, string> = {};
  [...NAV, ADMIN_GROUP].forEach((g) => g.items.forEach((it) => (m[it.href] = it.label)));
  m['/documents/new'] = 'Emitir comprobante';
  m['/welcome'] = 'Inicio';
  return m;
})();
