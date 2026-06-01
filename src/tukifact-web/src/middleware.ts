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

/**
 * Build an absolute redirect URL anchored at the requested public host.
 *
 * `request.nextUrl` is derived from the Next server's internal binding
 * (HOSTNAME=0.0.0.0, PORT=3000), so cloning it and just tweaking the path
 * leaks `0.0.0.0:3000` into the Location header. We always set hostname,
 * port and protocol explicitly so the value we emit is what the user sees.
 */
function buildRedirectUrl(request: NextRequest, targetHost: string, targetPath: string): URL {
  const url = request.nextUrl.clone();
  url.protocol = 'https:';
  url.hostname = targetHost;
  url.port = '';
  url.pathname = targetPath;
  return url;
}

export function middleware(request: NextRequest) {
  // Prefer x-forwarded-host (nginx-proxy sets it from the original Host) and
  // strip any container port that leaks through Host (e.g. `:3000`).
  const rawHost = (request.headers.get('x-forwarded-host') ?? request.headers.get('host') ?? '')
    .toLowerCase()
    .split(',')[0]
    .trim();
  const host = rawHost.replace(/:\d+$/, '');
  const pathname = request.nextUrl.pathname;

  if (isLocalHost(host)) return NextResponse.next();
  if (startsWithAny(pathname, SHARED_PATHS)) return NextResponse.next();

  const isAppHost = host.startsWith('app.');
  // Strip BOTH `app.` and `www.` so the rootDomain is always the apex.
  const rootDomain = host.replace(/^(?:app|www)\./, '');
  const appHost = `app.${rootDomain}`;

  if (isAppHost) {
    // On app.tukifact.pe — only auth + portal + shared allowed.
    if (pathname === '/') {
      return NextResponse.redirect(buildRedirectUrl(request, appHost, '/dashboard'), 307);
    }
    if (startsWithAny(pathname, PUBLIC_ONLY_PATHS)) {
      return NextResponse.redirect(buildRedirectUrl(request, rootDomain, pathname), 301);
    }
  } else {
    // On tukifact.pe (or www.) — only marketing + shared. Auth + portal → app.
    if (startsWithAny(pathname, APP_PORTAL_PREFIXES) || startsWithAny(pathname, APP_AUTH_PATHS)) {
      return NextResponse.redirect(buildRedirectUrl(request, appHost, pathname), 301);
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon\\.ico|sitemap\\.xml|robots\\.txt|.*\\..*).*)'],
};
