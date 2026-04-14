'use client';

import { useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/lib/auth-context';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Separator } from '@/components/ui/separator';
import {
  LayoutDashboard,
  FileText,
  Plus,
  Settings,
  ListOrdered,
  LogOut,
  KeyRound,
  Users,
  Ban,
  Menu,
  X,
  Sparkles,
  CreditCard,
  BarChart3,
  ReceiptText,
  Bot,
  Webhook,
  ScrollText,
  Package,
  Contact,
  Shield,
  Truck,
  ShieldCheck,
  ShieldAlert,
  FileSpreadsheet,
  Repeat,
  DollarSign,
  BookOpen,
} from 'lucide-react';
import { useState } from 'react';
import { cn } from '@/lib/utils';
import { ThemeToggle } from '@/components/theme-toggle';
import { FloatingChat } from '@/components/floating-chat';
import Image from 'next/image';

const NAV_ITEMS = [
  { href: '/welcome', label: 'Inicio', icon: Sparkles },
  { href: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/documents', label: 'Comprobantes', icon: FileText },
  { href: '/documents/new', label: 'Emitir', icon: Plus },
  { href: '/documents/credit-note', label: 'Nota Crédito', icon: ReceiptText },
  { href: '/quotations', label: 'Cotizaciones', icon: FileSpreadsheet },
  { href: '/despatch-advices', label: 'Guías Remisión', icon: Truck },
  { href: '/retentions', label: 'Retenciones', icon: ShieldCheck },
  { href: '/perceptions', label: 'Percepciones', icon: ShieldAlert },
  { href: '/recurring-invoices', label: 'Recurrentes', icon: Repeat },
  { href: '/exchange-rates', label: 'Tipo de Cambio', icon: DollarSign },
  { href: '/catalogs', label: 'Catálogos SUNAT', icon: BookOpen },
  { href: '/products', label: 'Productos', icon: Package },
  { href: '/customers', label: 'Clientes', icon: Contact },
  { href: '/reports', label: 'Reportes', icon: BarChart3 },
  { href: '/series', label: 'Series', icon: ListOrdered },
  { href: '/voided', label: 'Bajas', icon: Ban },
  { href: '/ai', label: 'Copiloto IA', icon: Bot },
];

const ADMIN_ITEMS = [
  { href: '/certificate', label: 'Certificado Digital', icon: Shield },
  { href: '/users', label: 'Usuarios', icon: Users },
  { href: '/api-keys', label: 'API Keys', icon: KeyRound },
  { href: '/webhooks', label: 'Webhooks', icon: Webhook },
  { href: '/audit-log', label: 'Audit Log', icon: ScrollText },
  { href: '/plan', label: 'Plan', icon: CreditCard },
  { href: '/settings', label: 'Configuración', icon: Settings },
];

export default function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  const { user, isLoading, isAuthenticated, logout } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push('/login');
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-blue-600 border-t-transparent" />
      </div>
    );
  }

  if (!isAuthenticated) return null;

  const handleLogout = () => {
    logout();
    router.push('/login');
  };

  const initials =
    user?.fullName
      ?.split(' ')
      .map((n) => n[0])
      .join('')
      .slice(0, 2)
      .toUpperCase() ?? 'TF';
  const isAdmin = user?.role === 'admin';

  return (
    <div className="min-h-screen flex bg-background">
      {/* Mobile overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-20 bg-black/50 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-30 w-64 bg-card border-r flex flex-col transform transition-transform duration-200 ease-in-out lg:static lg:translate-x-0',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        {/* Logo */}
        <div className="flex h-16 items-center gap-3 px-6 border-b">
          <Image src="/logo.png" alt="TukiFact" width={180} height={40} className="object-contain" />
          <button
            className="ml-auto lg:hidden"
            onClick={() => setSidebarOpen(false)}
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Nav */}
        <nav className="flex-1 overflow-y-auto py-4 px-3">
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider px-3 mb-2">
            Principal
          </p>
          <ul className="space-y-1">
            {NAV_ITEMS.map(({ href, label, icon: Icon }) => (
              <li key={href}>
                <Link
                  href={href}
                  onClick={() => setSidebarOpen(false)}
                  className={cn(
                    'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                    pathname === href || pathname.startsWith(href + '/')
                      ? 'bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300'
                      : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                  )}
                >
                  <Icon className="h-4 w-4 shrink-0" />
                  {label}
                </Link>
              </li>
            ))}
          </ul>

          {isAdmin && (
            <>
              <Separator className="my-3" />
              <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider px-3 mb-2">
                Admin
              </p>
              <ul className="space-y-1">
                {ADMIN_ITEMS.map(({ href, label, icon: Icon }) => (
                  <li key={href}>
                    <Link
                      href={href}
                      onClick={() => setSidebarOpen(false)}
                      className={cn(
                        'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                        pathname === href || pathname.startsWith(href + '/')
                          ? 'bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300'
                          : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                      )}
                    >
                      <Icon className="h-4 w-4 shrink-0" />
                      {label}
                    </Link>
                  </li>
                ))}
              </ul>
            </>
          )}
        </nav>

        {/* User footer */}
        <div className="border-t p-3 flex items-center gap-2">
          <ThemeToggle />
          <div className="flex-1 min-w-0">
          <DropdownMenu>
            <DropdownMenuTrigger className="w-full flex items-center gap-2 rounded-md px-2 py-2 text-sm hover:bg-muted transition-colors text-left">
              <Avatar className="h-8 w-8 shrink-0">
                <AvatarFallback className="bg-blue-100 text-blue-600 text-xs">
                  {initials}
                </AvatarFallback>
              </Avatar>
              <div className="flex flex-col items-start min-w-0">
                <span className="text-sm font-medium truncate max-w-[140px]">
                  {user?.fullName ?? user?.email}
                </span>
                <span className="text-xs text-muted-foreground capitalize">{user?.role}</span>
              </div>
            </DropdownMenuTrigger>
            <DropdownMenuContent side="top" align="start" className="w-56">
              <DropdownMenuItem onClick={() => router.push('/settings')}>
                <Settings className="mr-2 h-4 w-4" /> Configuración
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem variant="destructive" onClick={handleLogout}>
                <LogOut className="mr-2 h-4 w-4" /> Cerrar Sesión
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          </div>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Top bar (mobile) */}
        <header className="sticky top-0 z-10 flex h-16 items-center gap-4 border-b bg-background/95 backdrop-blur px-4 lg:hidden">
          <button
            onClick={() => setSidebarOpen(true)}
            className="rounded-md p-2 hover:bg-muted"
          >
            <Menu className="h-5 w-5" />
          </button>
          <Image src="/logo.png" alt="TukiFact" width={140} height={32} className="object-contain" />
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-6">
          {children}
        </main>
      </div>

      {/* Floating AI Chat - available on all pages */}
      <FloatingChat />
    </div>
  );
}
