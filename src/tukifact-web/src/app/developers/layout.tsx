'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import {
  Home,
  BookOpen,
  Zap,
  Package,
  FlaskConical,
  ScrollText,
  Activity,
  Menu,
  X,
  ExternalLink,
} from 'lucide-react';

const NAV_ITEMS = [
  { href: '/developers', label: 'Inicio', icon: Home, exact: true },
  { href: '/developers/docs', label: 'Documentación', icon: BookOpen },
  { href: '/developers/quickstart', label: 'Guía Rápida', icon: Zap },
  { href: '/developers/sdks', label: 'SDKs', icon: Package },
  { href: '/developers/sandbox', label: 'Sandbox', icon: FlaskConical },
  { href: '/developers/changelog', label: 'Changelog', icon: ScrollText },
  { href: '/developers/status', label: 'Estado', icon: Activity },
];

export default function DevelopersLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="min-h-screen flex flex-col bg-background">
      {/* Top Navbar */}
      <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="flex h-14 items-center gap-4 px-4 lg:px-6">
          {/* Mobile menu toggle */}
          <button
            className="lg:hidden rounded-md p-2 hover:bg-muted transition-colors"
            onClick={() => setSidebarOpen(!sidebarOpen)}
            aria-label="Toggle menu"
          >
            {sidebarOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>

          {/* Logo + title */}
          <Link href="/developers" className="flex items-center gap-2.5 font-semibold">
            <div className="flex items-center gap-1">
              <span className="text-lg font-bold text-foreground">TukiFact</span>
              <span className="hidden sm:inline-flex items-center rounded-md bg-blue-50 dark:bg-blue-950 px-2 py-0.5 text-xs font-medium text-blue-700 dark:text-blue-300 ring-1 ring-inset ring-blue-700/10 dark:ring-blue-300/20">
                Developer Portal
              </span>
            </div>
          </Link>

          <div className="ml-auto flex items-center gap-3">
            <Link
              href="/"
              className="hidden sm:flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
            >
              Ir a la app <ExternalLink className="h-3.5 w-3.5" />
            </Link>
            <Link href="/login">
              <Button size="sm">Iniciar Sesión</Button>
            </Link>
          </div>
        </div>
      </header>

      <div className="flex flex-1">
        {/* Mobile overlay */}
        {sidebarOpen && (
          <div
            className="fixed inset-0 z-30 bg-black/40 lg:hidden"
            onClick={() => setSidebarOpen(false)}
          />
        )}

        {/* Sidebar */}
        <aside
          className={cn(
            'fixed top-14 bottom-0 left-0 z-30 w-60 border-r bg-background overflow-y-auto transition-transform duration-200 lg:sticky lg:top-14 lg:translate-x-0 lg:self-start lg:h-[calc(100vh-3.5rem)]',
            sidebarOpen ? 'translate-x-0' : '-translate-x-full'
          )}
        >
          <nav className="py-4 px-3">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider px-3 mb-2">
              Navegación
            </p>
            <ul className="space-y-0.5">
              {NAV_ITEMS.map(({ href, label, icon: Icon, exact }) => {
                const isActive = exact ? pathname === href : pathname === href || pathname.startsWith(href + '/');
                return (
                  <li key={href}>
                    <Link
                      href={href}
                      onClick={() => setSidebarOpen(false)}
                      className={cn(
                        'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                        isActive
                          ? 'bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300'
                          : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                      )}
                    >
                      <Icon className="h-4 w-4 shrink-0" />
                      {label}
                    </Link>
                  </li>
                );
              })}
            </ul>

            <div className="mt-6 px-3">
              <div className="rounded-lg border bg-muted/50 p-3 text-xs text-muted-foreground space-y-1">
                <p className="font-medium text-foreground">¿Necesitas ayuda?</p>
                <p>Escríbenos a <span className="text-blue-600 dark:text-blue-400">soporte@tukifact.net.pe</span></p>
              </div>
            </div>
          </nav>
        </aside>

        {/* Main content */}
        <main className="flex-1 min-w-0">
          <div className="max-w-4xl mx-auto px-4 sm:px-6 py-8 lg:py-10">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
}
