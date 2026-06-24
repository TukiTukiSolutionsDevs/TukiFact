import type { Metadata } from 'next';
import Link from 'next/link';
import { ShieldCheck, KeyRound, Database, Lock, FileCheck2, Server, Eye, RefreshCcw } from 'lucide-react';
import { buttonVariants } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { BreadcrumbJsonLd, TUKIFACT_BRAND } from '@/components/seo/jsonld';

export const metadata: Metadata = {
  title: 'Seguridad y cumplimiento SUNAT',
  description:
    'Cómo TukiFact protege tu data fiscal: certificado digital encriptado AES-256, aislamiento multi-tenant, TLS 1.3, audit log inmutable y backups diarios.',
  alternates: { canonical: '/seguridad' },
};

const PILLARS = [
  {
    icon: ShieldCheck,
    title: 'Cumplimiento SUNAT',
    body: 'Certificación OSE en proceso ante SUNAT. Firma XML conforme XAdES-BES y envío SOAP con tu RUC + SOL.',
  },
  {
    icon: KeyRound,
    title: 'Certificado digital encriptado',
    body: 'Tu .pfx y tu contraseña viven encriptados con AES-256 + Data Protection. Solo se desencriptan en RAM al firmar.',
  },
  {
    icon: Database,
    title: 'Aislamiento por tenant',
    body: 'Cada empresa vive en su propio espacio lógico. Validamos TenantId en cada query — sin posibilidad de leaks entre cuentas.',
  },
  {
    icon: Lock,
    title: 'TLS 1.3 punta a punta',
    body: 'Toda comunicación entre tu navegador, nuestra API y SUNAT viaja cifrada. HSTS forzado.',
  },
  {
    icon: FileCheck2,
    title: 'Audit log inmutable',
    body: 'Quién hizo qué, cuándo y desde dónde — cada acción crítica se persiste y nunca se borra.',
  },
  {
    icon: Server,
    title: 'Infra en Perú',
    body: 'Servidores en datacenters tier III con redundancia. Tu data fiscal no sale del país.',
  },
  {
    icon: Eye,
    title: 'IDOR y RBAC verificados',
    body: 'Auditamos cada flujo SUNAT para evitar lectura cruzada. Roles admin / emisor / contable separan responsabilidades.',
  },
  {
    icon: RefreshCcw,
    title: 'Backups diarios',
    body: 'Snapshots cifrados con retención de 30 días. Restauración point-in-time disponible para planes Empresa.',
  },
];

export default function SeguridadPage() {
  return (
    <>
      <BreadcrumbJsonLd
        items={[
          { name: 'Inicio', url: `${TUKIFACT_BRAND.url}/` },
          { name: 'Seguridad', url: `${TUKIFACT_BRAND.url}/seguridad` },
        ]}
      />
      <section className="border-b border-border bg-gradient-to-br from-background to-slate-50 py-20">
        <div className="mx-auto max-w-3xl px-6 text-center">
          <ShieldCheck className="mx-auto h-12 w-12 text-[var(--brand-toucan-orange)]" />
          <h1 className="t-display-xl mt-4 text-foreground">Tu data fiscal, segura por diseño</h1>
          <p className="mt-4 text-lg text-slate-600">
            TukiFact se construyó con seguridad como requisito, no como add-on. Esto es lo que hacemos para que duermas tranquilo.
          </p>
        </div>
      </section>

      <section className="py-20">
        <div className="mx-auto max-w-7xl px-6">
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {PILLARS.map((p) => (
              <div key={p.title} className="rounded-2xl border border-border bg-card p-6">
                <div className="inline-flex h-10 w-10 items-center justify-center rounded-lg bg-[var(--brand-toucan-yellow)]/15 text-[var(--brand-toucan-orange)]">
                  <p.icon className="h-5 w-5" />
                </div>
                <h3 className="mt-4 font-semibold text-foreground">{p.title}</h3>
                <p className="mt-2 text-sm text-slate-600">{p.body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="border-y border-border bg-slate-50 py-20">
        <div className="mx-auto max-w-3xl px-6">
          <h2 className="t-display-lg text-foreground">Reportar una vulnerabilidad</h2>
          <p className="mt-4 text-slate-600">
            Si descubriste un problema de seguridad, queremos saberlo. Escríbenos a{' '}
            <a className="font-semibold text-foreground underline" href="mailto:security@tukifact.com.pe">
              security@tukifact.com.pe
            </a>{' '}
            con los detalles. Respondemos en menos de 48h hábiles y reconocemos a investigadores responsables en nuestro hall of fame.
          </p>
          <Link
            href="/contacto"
            className={cn(buttonVariants({ size: 'lg' }), 'mt-8 inline-flex h-12 bg-foreground px-6 text-background hover:bg-foreground/90')}
          >
            ¿Tienes dudas? Contáctanos
          </Link>
        </div>
      </section>
    </>
  );
}
