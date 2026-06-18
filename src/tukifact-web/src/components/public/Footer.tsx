import Link from 'next/link';
import Image from 'next/image';

const COLUMNS = [
  {
    title: 'Producto',
    links: [
      { href: '/funcionalidades', label: 'Funcionalidades' },
      { href: '/planes', label: 'Planes' },
      { href: '/seguridad', label: 'Seguridad' },
      { href: '/developers', label: 'API y desarrolladores' },
    ],
  },
  {
    title: 'Empresa',
    links: [
      { href: '/contacto', label: 'Contacto' },
      { href: 'mailto:administration@tukisolutions.com', label: 'administration@tukisolutions.com' },
      { href: 'https://wa.me/51966388258', label: '+51 966 388 258' },
    ],
  },
  {
    title: 'Legal',
    links: [
      { href: '/terms', label: 'Términos del servicio' },
      { href: '/privacy', label: 'Política de privacidad' },
      { href: '/devoluciones', label: 'Devoluciones y reembolsos' },
      { href: '/reclamaciones', label: 'Libro de reclamaciones' },
    ],
  },
];

export function PublicFooter() {
  const year = new Date().getFullYear();
  return (
    <footer className="border-t border-border bg-slate-50">
      <div className="mx-auto max-w-7xl px-6 py-16">
        <div className="grid gap-12 md:grid-cols-[1.5fr_repeat(3,1fr)]">
          <div>
            <Link href="/" aria-label="TukiFact inicio">
              <Image src="/logo.png" alt="TukiFact" width={160} height={36} className="h-9 w-auto" />
            </Link>
            <p className="mt-4 max-w-xs text-sm text-slate-600">
              Facturación electrónica para empresas peruanas. Emite, firma y envía a SUNAT en segundos.
            </p>
          </div>
          {COLUMNS.map((col) => (
            <div key={col.title}>
              <h3 className="mb-4 text-xs font-semibold uppercase tracking-wider text-slate-500">{col.title}</h3>
              <ul className="space-y-2.5">
                {col.links.map((link) => (
                  <li key={link.href + link.label}>
                    <Link href={link.href} className="text-sm text-slate-700 transition-colors hover:text-foreground">
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
        <div className="mt-12 flex flex-col items-start justify-between gap-3 border-t border-border pt-6 text-xs text-slate-500 md:flex-row md:items-center">
          <p>© {year} TukiFact. Hecho en Lima, Perú.</p>
          <p>Plataforma autorizada por SUNAT como OSE (Operador de Servicios Electrónicos).</p>
        </div>
      </div>
    </footer>
  );
}
