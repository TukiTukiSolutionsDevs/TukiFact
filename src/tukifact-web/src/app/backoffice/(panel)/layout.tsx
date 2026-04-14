'use client';

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { useBackofficeAuth } from '@/lib/backoffice-auth-context';
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
  Building2,
  FileSearch,
  Users,
  LogOut,
  Shield,
  Menu,
  X,
  Settings,
} from 'lucide-react';
import { cn } from '@/lib/utils';

const NAV_ITEMS = [
  { href: '/backoffice/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/backoffice/tenants', label: 'Tenants', icon: Building2 },
  { href: '/backoffice/documents', label: 'Documentos', icon: FileSearch },
  { href: '/backoffice/employees', label: 'Empleados', icon: Users },
];

export default function BackofficePanelLayout({ children }: { children: React.ReactNode }) {
  const { user, isLoading, isAuthenticated, logout } = useBackofficeAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push('/backoffice/login');
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-950">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
      </div>
    );
  }

  if (!isAuthenticated) return null;

  const handleLogout = () => {
    logout();
    router.push('/backoffice/login');
  };

  const initials =
    user?.fullName
      ?.split(' ')
      .map((n) => n[0])
      .join('')
      .slice(0, 2)
      .toUpperCase() ?? 'BO';

  return (
    <div className="min-h-screen flex bg-slate-950 text-slate-100">
      {/* Mobile overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-20 bg-black/60 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-30 w-64 bg-slate-900 border-r border-slate-800 flex flex-col transform transition-transform duration-200 ease-in-out lg:static lg:translate-x-0',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        {/* Logo */}
        <div className="flex h-16 items-center gap-3 px-6 border-b border-slate-800">
          <Shield className="h-7 w-7 text-indigo-400" />
          <div>
            <span className="font-bold text-lg text-white">TukiFact</span>
            <span className="ml-1 text-xs font-medium text-indigo-400 bg-indigo-950 px-1.5 py-0.5 rounded">
              Backoffice
            </span>
          </div>
          <button className="ml-auto lg:hidden" onClick={() => setSidebarOpen(false)}>
            <X className="h-5 w-5 text-slate-400" />
          </button>
        </div>

        {/* Nav */}
        <nav className="flex-1 overflow-y-auto py-4 px-3">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider px-3 mb-2">
            Plataforma
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
                      ? 'bg-indigo-600/20 text-indigo-300 ring-1 ring-indigo-500/30'
                      : 'text-slate-400 hover:bg-slate-800 hover:text-slate-200'
                  )}
                >
                  <Icon className="h-4 w-4 shrink-0" />
                  {label}
                </Link>
              </li>
            ))}
          </ul>

          <Separator className="my-4 bg-slate-800" />

          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider px-3 mb-2">
            Sistema
          </p>
          <ul className="space-y-1">
            <li>
              <Link
                href="/backoffice/employees"
                className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-slate-400 hover:bg-slate-800 hover:text-slate-200 transition-colors"
              >
                <Settings className="h-4 w-4 shrink-0" />
                Configuración
              </Link>
            </li>
          </ul>
        </nav>

        {/* User footer */}
        <div className="border-t border-slate-800 p-3">
          <DropdownMenu>
            <DropdownMenuTrigger className="w-full flex items-center gap-2 rounded-md px-2 py-2 text-sm hover:bg-slate-800 transition-colors text-left">
              <Avatar className="h-8 w-8 shrink-0">
                <AvatarFallback className="bg-indigo-600/30 text-indigo-300 text-xs">
                  {initials}
                </AvatarFallback>
              </Avatar>
              <div className="flex flex-col items-start min-w-0">
                <span className="text-sm font-medium truncate max-w-[140px] text-slate-200">
                  {user?.fullName ?? user?.email}
                </span>
                <span className="text-xs text-slate-500 capitalize">{user?.role}</span>
              </div>
            </DropdownMenuTrigger>
            <DropdownMenuContent side="top" align="start" className="w-56 bg-slate-800 border-slate-700 text-slate-200">
              <DropdownMenuItem className="text-slate-300 focus:bg-slate-700 focus:text-white">
                <Settings className="mr-2 h-4 w-4" /> Configuración
              </DropdownMenuItem>
              <DropdownMenuSeparator className="bg-slate-700" />
              <DropdownMenuItem
                variant="destructive"
                onClick={handleLogout}
                className="text-red-400 focus:bg-red-950 focus:text-red-300"
              >
                <LogOut className="mr-2 h-4 w-4" /> Cerrar Sesión
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Top bar (mobile) */}
        <header className="sticky top-0 z-10 flex h-16 items-center gap-4 border-b border-slate-800 bg-slate-950/95 backdrop-blur px-4 lg:hidden">
          <button onClick={() => setSidebarOpen(true)} className="rounded-md p-2 hover:bg-slate-800">
            <Menu className="h-5 w-5 text-slate-400" />
          </button>
          <div className="flex items-center gap-2">
            <Shield className="h-5 w-5 text-indigo-400" />
            <span className="font-bold text-white">Backoffice</span>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-6">{children}</main>
      </div>
    </div>
  );
}
