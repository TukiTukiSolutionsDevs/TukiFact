import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

/**
 * Per-host routing for the marketing site vs the tenant portal.
 *
 *  tukifact.pe      → public marketing (`(public)` group)
 *  app.tukifact.pe  → tenant portal (`(authenticated)` group + auth pages)
 *
 * In local dev (`localhost:3000`) the split is disabled so both halves
 * remain accessible from a single host. JWT cookies live on the `app.`
 * subdomain so the marketing host can never read them.
 */

const PUBLIC_ONLY_PATHS = ['/planes', '/funcionalidades', '/seguridad', '/contacto'];
const APP_AUTH_PATHS = ['/login', '/register', '/welcome', '/forgot-password', '/reset-password'];
const APP_PORTAL_PREFIXES = [
  '/dashboard', '/documents', '/customers', '/products', '/series', '/settings',
  '/plan', '/audit-log', '/ai', '/api-keys', '/webhooks', '/exchange-rates',
  '/catalogs', '/voided', '/perceptions', '/retentions', '/recurring-invoices',
  '/despatch-advices', '/quotations', '/users', '/certificate', '/reports',
  '/backoffice', '/developers',
];
const SHARED_PATHS = ['/privacy', '/terms']; // accessible on both hosts

function isLocalHost(host: string): boolean {
  return (
    host.startsWith('localhost') ||
    host.startsWith('127.0.0.1') ||
    host.startsWith('0.0.0.0') ||
    host.endsWith('.local') ||
    host.endsWith('.test')
  );
}

function startsWithAny(pathname: string, prefixes: string[]): boolean {
  return prefixes.some((p) => pathname === p || pathname.startsWith(`${p}/`));
}

export function middleware(request: NextRequest) {
  const host = (request.headers.get('host') ?? '').toLowerCase();
  const pathname = request.nextUrl.pathname;

  if (isLocalHost(host)) return NextResponse.next();
  if (startsWithAny(pathname, SHARED_PATHS)) return NextResponse.next();

  const isAppHost = host.startsWith('app.');
  const rootDomain = host.replace(/^app\./, '');

  if (isAppHost) {
    // On app.tukifact.pe — only auth + portal + shared allowed.
    if (pathname === '/') {
      const url = request.nextUrl.clone();
      url.pathname = '/dashboard';
      return NextResponse.redirect(url, 307);
    }
    if (startsWithAny(pathname, PUBLIC_ONLY_PATHS)) {
      const url = request.nextUrl.clone();
      url.host = rootDomain;
      return NextResponse.redirect(url, 301);
    }
  } else {
    // On tukifact.pe — only marketing + shared. Auth + portal → app.
    if (startsWithAny(pathname, APP_PORTAL_PREFIXES) || startsWithAny(pathname, APP_AUTH_PATHS)) {
      const url = request.nextUrl.clone();
      url.host = `app.${rootDomain}`;
      return NextResponse.redirect(url, 301);
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon\\.ico|sitemap\\.xml|robots\\.txt|.*\\..*).*)'],
};
