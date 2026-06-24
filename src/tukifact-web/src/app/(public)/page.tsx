import type { Metadata } from 'next';
import Link from 'next/link';
import Image from 'next/image';
import {
  ArrowRight,
  FileSpreadsheet,
  Receipt,
  FileText,
  Truck,
  Sparkles,
  ShieldCheck,
  KeyRound,
  Repeat,
  CheckCircle2,
} from 'lucide-react';
import { buttonVariants } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { SoftwareApplicationJsonLd } from '@/components/seo/jsonld';

export const metadata: Metadata = {
  title: { absolute: 'TukiFact: Facturación electrónica SUNAT en Perú' },
  description:
    'TukiFact emite facturas, boletas, notas y guías electrónicas a SUNAT. Certificado digital, SOL, IA y API REST incluidos. Empieza gratis.',
  alternates: { canonical: '/' },
};

const FEATURES = [
  {
    icon: FileSpreadsheet,
    title: 'Facturas y boletas',
    body: 'Emite comprobantes electrónicos en segundos. Validación SUNAT en tiempo real con firma digital incluida.',
  },
  {
    icon: Truck,
    title: 'Guías de remisión',
    body: 'Genera GRE 2.0 con OAuth2 y QR. Cumple la última normativa SUNAT sin pelear con la SOAP.',
  },
  {
    icon: Receipt,
    title: 'Notas y anulaciones',
    body: 'Notas de crédito y débito ligadas al documento original. Comunicación de baja con un click.',
  },
  {
    icon: Repeat,
    title: 'Facturación recurrente',
    body: 'Programa emisiones mensuales, semanales o personalizadas. Tu contabilidad en piloto automático.',
  },
  {
    icon: Sparkles,
    title: 'Asistente con IA',
    body: 'Pregunta en lenguaje natural por reportes, rechazos o cómo emitir una nota de crédito. Tu propio copiloto SUNAT.',
  },
  {
    icon: KeyRound,
    title: 'API REST y webhooks',
    body: 'Integra TukiFact con tu ERP o e-commerce. Webhooks firmados con HMAC y SDK próximamente.',
  },
];

const STEPS = [
  {
    n: '01',
    title: 'Crea tu cuenta',
    body: 'En menos de 2 minutos: RUC, razón social y un correo. Verifica tu email y entra.',
  },
  {
    n: '02',
    title: 'Sube tu certificado',
    body: 'Carga tu certificado digital SUNAT (.pfx). Lo encriptamos y firmamos tus XML automáticamente.',
  },
  {
    n: '03',
    title: 'Emite tu primer comprobante',
    body: 'Configura tu serie, agrega un cliente y emite. SUNAT responde en segundos.',
  },
];

const STATS = [
  { value: '< 3s', label: 'Tiempo promedio de emisión' },
  { value: '99.9%', label: 'Uptime de la plataforma' },
  { value: '24/7', label: 'Soporte en español' },
  { value: 'En trámite', label: 'Certificación OSE SUNAT' },
];

export default function HomePage() {
  return (
    <>
      <SoftwareApplicationJsonLd />
      <section className="relative overflow-hidden border-b border-border bg-gradient-to-br from-background via-background to-slate-50">
        <div className="absolute inset-x-0 top-0 -z-10 h-[600px] opacity-30 [mask-image:radial-gradient(closest-side,white,transparent)]">
          <div className="absolute left-1/2 top-0 h-[400px] w-[800px] -translate-x-1/2 rounded-full bg-[var(--brand-toucan-yellow)] blur-3xl" />
        </div>

        <div className="mx-auto max-w-7xl px-6 pb-24 pt-20 lg:pt-28">
          <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-16">
            <div>
              <span className="inline-flex items-center gap-2 rounded-full border border-border bg-card px-3 py-1 text-xs font-medium text-slate-700 shadow-xs">
                <span className="inline-block h-2 w-2 rounded-full bg-[var(--brand-toucan-orange)]" />
                Hecho en Perú · Certificación OSE en proceso ante SUNAT
              </span>
              <h1 className="t-display-2xl mt-6 text-foreground">
                Facturación electrónica
                <br />
                <span className="bg-gradient-to-br from-[var(--brand-toucan-orange)] to-[var(--brand-toucan-yellow)] bg-clip-text text-transparent">
                  sin complicaciones.
                </span>
              </h1>
              <p className="mt-6 max-w-xl text-lg text-slate-600">
                Emite facturas, boletas, notas y guías de remisión electrónicas a SUNAT en segundos. Sin pelear con XML,
                sin perder noches con la SOAP. Tu certificado, tu SOL, tu plataforma.
              </p>
              <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                <Link
                  href="/register"
                  className={cn(buttonVariants({ size: 'lg' }), 'h-12 gap-2 bg-foreground px-6 text-background hover:bg-foreground/90')}
                >
                  Probar gratis <ArrowRight className="h-4 w-4" />
                </Link>
                <Link href="/planes" className={cn(buttonVariants({ variant: 'outline', size: 'lg' }), 'h-12 px-6')}>
                  Ver planes
                </Link>
              </div>
              <ul className="mt-8 flex flex-wrap gap-x-6 gap-y-2 text-sm text-slate-600">
                {['Hasta 10 comprobantes gratis al mes', 'Sin tarjeta para empezar', 'Soporte en español'].map((t) => (
                  <li key={t} className="inline-flex items-center gap-1.5">
                    <CheckCircle2 className="h-4 w-4 text-[var(--success,oklch(0.66_0.14_152))]" />
                    {t}
                  </li>
                ))}
              </ul>
            </div>

            <div className="relative">
              <div className="rounded-3xl border border-border bg-card p-2 shadow-xl">
                <div className="rounded-2xl bg-slate-50 p-6">
                  <div className="mb-4 flex items-center gap-2">
                    <span className="h-3 w-3 rounded-full bg-slate-300" />
                    <span className="h-3 w-3 rounded-full bg-slate-300" />
                    <span className="h-3 w-3 rounded-full bg-slate-300" />
                    <span className="ml-auto text-xs font-medium text-slate-500">app.tukifact.pe/documents</span>
                  </div>
                  <div className="grid gap-3">
                    <div className="flex items-center justify-between rounded-xl border border-border bg-card px-4 py-3">
                      <div className="flex items-center gap-3">
                        <FileSpreadsheet className="h-5 w-5 text-[var(--brand-toucan-orange)]" />
                        <div>
                          <div className="text-xs font-medium text-slate-500">F001-00342</div>
                          <div className="text-sm font-semibold text-foreground">Distribuidora ABC SAC</div>
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-sm font-semibold tabular-nums">S/ 1,180.00</div>
                        <span className="inline-flex items-center gap-1 rounded-full bg-[var(--success,oklch(0.66_0.14_152))]/15 px-2 py-0.5 text-[11px] font-semibold text-[var(--success,oklch(0.66_0.14_152))]">
                          Aceptada
                        </span>
                      </div>
                    </div>
                    <div className="flex items-center justify-between rounded-xl border border-border bg-card px-4 py-3">
                      <div className="flex items-center gap-3">
                        <Receipt className="h-5 w-5 text-slate-700" />
                        <div>
                          <div className="text-xs font-medium text-slate-500">B001-08210</div>
                          <div className="text-sm font-semibold text-foreground">María Salinas</div>
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-sm font-semibold tabular-nums">S/ 89.50</div>
                        <span className="inline-flex items-center gap-1 rounded-full bg-[var(--success,oklch(0.66_0.14_152))]/15 px-2 py-0.5 text-[11px] font-semibold text-[var(--success,oklch(0.66_0.14_152))]">
                          Aceptada
                        </span>
                      </div>
                    </div>
                    <div className="flex items-center justify-between rounded-xl border border-border bg-card px-4 py-3">
                      <div className="flex items-center gap-3">
                        <Truck className="h-5 w-5 text-slate-700" />
                        <div>
                          <div className="text-xs font-medium text-slate-500">T001-00094</div>
                          <div className="text-sm font-semibold text-foreground">Traslado a Surco</div>
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-sm font-semibold tabular-nums">—</div>
                        <span className="inline-flex items-center gap-1 rounded-full bg-[oklch(0.62_0.14_240)]/15 px-2 py-0.5 text-[11px] font-semibold text-[oklch(0.62_0.14_240)]">
                          Enviada
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="mt-4 flex items-center gap-2 rounded-xl border border-border bg-card p-3">
                    <Sparkles className="h-4 w-4 text-[var(--brand-toucan-orange)]" />
                    <div className="text-xs text-slate-600">
                      <span className="font-semibold text-foreground">Asistente:</span> Hoy emitiste 3 comprobantes por S/ 1,269.50.
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="border-b border-border bg-card py-16">
        <div className="mx-auto max-w-7xl px-6">
          <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
            {STATS.map((s) => (
              <div key={s.label}>
                <div className="text-3xl font-bold tabular-nums text-foreground">{s.value}</div>
                <div className="mt-1 text-sm text-slate-600">{s.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-24">
        <div className="mx-auto max-w-7xl px-6">
          <div className="mx-auto max-w-2xl text-center">
            <h2 className="t-display-lg text-foreground">Todo lo que necesitas para facturar en regla</h2>
            <p className="mt-4 text-lg text-slate-600">
              Una sola plataforma para todos los comprobantes electrónicos que SUNAT exige.
            </p>
          </div>

          <div className="mt-16 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {FEATURES.map((f) => (
              <div
                key={f.title}
                className="group rounded-2xl border border-border bg-card p-6 transition-all hover:-translate-y-0.5 hover:shadow-md"
              >
                <div className="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-[var(--brand-toucan-yellow)]/15 text-[var(--brand-toucan-orange)] transition-colors group-hover:bg-[var(--brand-toucan-yellow)]/25">
                  <f.icon className="h-5 w-5" />
                </div>
                <h3 className="mt-5 text-lg font-semibold text-foreground">{f.title}</h3>
                <p className="mt-2 text-sm text-slate-600">{f.body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="border-y border-border bg-slate-50 py-24">
        <div className="mx-auto max-w-7xl px-6">
          <div className="mx-auto max-w-2xl text-center">
            <span className="text-xs font-semibold uppercase tracking-wider text-[var(--brand-toucan-orange)]">
              Cómo funciona
            </span>
            <h2 className="t-display-lg mt-3 text-foreground">Listo en 5 minutos</h2>
            <p className="mt-4 text-lg text-slate-600">
              No tienes que aprender la SOAP de SUNAT ni configurar servidores.
            </p>
          </div>

          <div className="mt-16 grid gap-8 md:grid-cols-3">
            {STEPS.map((s) => (
              <div key={s.n} className="rounded-2xl border border-border bg-card p-8">
                <div className="font-mono text-sm font-semibold text-[var(--brand-toucan-orange)]">{s.n}</div>
                <h3 className="mt-2 text-lg font-semibold text-foreground">{s.title}</h3>
                <p className="mt-2 text-sm text-slate-600">{s.body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-24">
        <div className="mx-auto max-w-5xl px-6">
          <div className="overflow-hidden rounded-3xl border border-border bg-[var(--brand-ink)] p-1 shadow-2xl">
            <div className="rounded-[20px] bg-gradient-to-br from-[var(--brand-ink)] via-slate-900 to-slate-800 px-8 py-16 text-center text-background sm:px-16">
              <ShieldCheck className="mx-auto h-10 w-10 text-[var(--brand-toucan-yellow)]" />
              <h2 className="t-display-lg mt-6 text-background">¿Listo para empezar a facturar?</h2>
              <p className="mx-auto mt-4 max-w-xl text-slate-300">
                Crea tu cuenta gratis y emite tu primer comprobante hoy mismo. Sin tarjeta, sin compromisos.
              </p>
              <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
                <Link
                  href="/register"
                  className={cn(buttonVariants({ size: 'lg' }), 'h-12 gap-2 bg-[var(--brand-toucan-yellow)] px-6 text-[var(--brand-ink)] hover:bg-[var(--brand-toucan-yellow)]/90')}
                >
                  Crear cuenta gratis <ArrowRight className="h-4 w-4" />
                </Link>
                <Link
                  href="/contacto"
                  className={cn(buttonVariants({ variant: 'outline', size: 'lg' }), 'h-12 border-slate-700 bg-transparent px-6 text-background hover:bg-slate-800 hover:text-background')}
                >
                  Hablar con ventas
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>
    </>
  );
}
