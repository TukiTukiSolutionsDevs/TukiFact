import type { Metadata } from 'next';
import Link from 'next/link';
import { Check, ArrowRight } from 'lucide-react';
import { buttonVariants } from '@/components/ui/button';
import { cn } from '@/lib/utils';

export const metadata: Metadata = {
  title: 'Planes y precios',
  description: 'Planes flexibles para empresas peruanas. Desde Gratis hasta Empresa, con API, IA y webhooks incluidos sin sobreprecio.',
  alternates: { canonical: '/planes' },
};

type FeatureMap = Record<string, string | number | boolean>;

type PlanDto = {
  id: string;
  name: string;
  priceMonthly: number;
  maxDocumentsPerMonth: number;
  features: FeatureMap;
  isActive: boolean;
};

const FEATURED_NAME = 'Negocio';

const FALLBACK_PLANS: PlanDto[] = [
  {
    id: 'gratis',
    name: 'Gratis',
    priceMonthly: 0,
    maxDocumentsPerMonth: 10,
    features: { api: false, ai: false, users: 1, series: 1, support: 'none', trial: true },
    isActive: true,
  },
  {
    id: 'emprendedor',
    name: 'Emprendedor',
    priceMonthly: 35,
    maxDocumentsPerMonth: 200,
    features: { api: false, ai: false, users: 2, series: 1, support: 'email' },
    isActive: true,
  },
  {
    id: 'negocio',
    name: 'Negocio',
    priceMonthly: 79,
    maxDocumentsPerMonth: 2000,
    features: { ai: 'basic', ai_queries: 100, api: true, api_rate_limit: 100, users: 5, series: 'multiple', support: 'email+tickets', webhooks: true },
    isActive: true,
  },
  {
    id: 'profesional',
    name: 'Profesional',
    priceMonthly: 179,
    maxDocumentsPerMonth: 5000,
    features: { ai: 'full', ai_queries: 500, api: true, api_rate_limit: 500, byok: true, sdks: true, users: 15, series: 'multiple', support: 'priority', webhooks: true, custom_branding: true, reports: 'advanced' },
    isActive: true,
  },
  {
    id: 'empresa',
    name: 'Empresa',
    priceMonthly: 349,
    maxDocumentsPerMonth: 15000,
    features: { ai: 'full_all_agents', ai_queries: 'unlimited', api: true, api_rate_limit: 1000, byok: true, sdks: true, users: 'unlimited', series: 'multiple', support: 'sla_99.9', webhooks: true, custom_branding: true, reports: 'advanced', dedicated_api: true, onboarding: true },
    isActive: true,
  },
];

const SUPPORT_LABELS: Record<string, string> = {
  none: 'Soporte comunitario',
  email: 'Soporte por email',
  'email+tickets': 'Soporte por email + tickets',
  priority: 'Soporte prioritario',
  docs: 'Documentación + comunidad',
  sla_99: 'SLA 99% y soporte dedicado',
  'sla_99.9': 'SLA 99.9% y soporte dedicado',
};

const AI_LABELS: Record<string, string> = {
  basic: 'Asistente IA básico',
  full: 'Asistente IA completo',
  copilot: 'Copiloto de desarrollo',
  full_all_agents: 'IA completa con agentes autónomos',
};

async function fetchPlans(): Promise<PlanDto[]> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5186';
    const res = await fetch(`${apiUrl}/v1/plans`, { next: { revalidate: 300 } });
    if (!res.ok) return FALLBACK_PLANS;
    const data = await res.json();
    const plans: PlanDto[] = Array.isArray(data) ? data : data?.items ?? data?.data ?? [];
    return plans.length ? plans.filter((p) => p.isActive !== false) : FALLBACK_PLANS;
  } catch {
    return FALLBACK_PLANS;
  }
}

function formatPrice(amount: number) {
  if (amount === 0) return 'Gratis';
  return `S/ ${Number(amount).toLocaleString('es-PE', { maximumFractionDigits: 0 })}`;
}

function describeFeatures(f: FeatureMap): string[] {
  const lines: string[] = [];

  if (typeof f.ai === 'string' && AI_LABELS[f.ai]) {
    const baseLabel = AI_LABELS[f.ai];
    if (f.ai_queries === 'unlimited') lines.push(`${baseLabel} · consultas ilimitadas`);
    else if (typeof f.ai_queries === 'number') lines.push(`${baseLabel} · ${f.ai_queries} consultas/mes`);
    else lines.push(baseLabel);
  }

  if (f.api === true) {
    if (typeof f.api_rate_limit === 'number') lines.push(`API REST · ${f.api_rate_limit} req/min`);
    else lines.push('API REST');
  }

  if (f.byok) lines.push('Trae tu propia API key de IA (Gemini, Claude…)');
  if (f.webhooks) lines.push('Webhooks firmados con HMAC');
  if (f.sdks) lines.push('SDKs oficiales (Node, Python, .NET)');
  if (f.sandbox) lines.push('Sandbox para pruebas');
  if (f.onboarding) lines.push('Onboarding personalizado');
  if (f.trial) lines.push('Solo para probar la plataforma');

  if (typeof f.users === 'number') lines.push(`Hasta ${f.users} ${f.users === 1 ? 'usuario' : 'usuarios'}`);
  else if (f.users === 'unlimited') lines.push('Usuarios ilimitados');

  if (typeof f.series === 'number') lines.push(`${f.series} ${f.series === 1 ? 'serie' : 'series'} configurables`);
  else if (f.series === 'multiple') lines.push('Múltiples series configurables');

  if (f.dedicated_api) lines.push('Endpoint API dedicado');
  if (f.custom_branding) lines.push('Marca personalizada en PDFs y emails');
  if (f.reports === 'advanced') lines.push('Reportes avanzados');

  if (typeof f.support === 'string' && SUPPORT_LABELS[f.support]) lines.push(SUPPORT_LABELS[f.support]);

  return lines;
}

export default async function PlanesPage() {
  const plans = await fetchPlans();
  const featuredIdx = Math.max(0, plans.findIndex((p) => p.name === FEATURED_NAME));

  return (
    <>
      <section className="border-b border-border bg-gradient-to-br from-background to-slate-50 py-20">
        <div className="mx-auto max-w-3xl px-6 text-center">
          <h1 className="t-display-xl text-foreground">Planes pensados para crecer contigo</h1>
          <p className="mt-4 text-lg text-slate-600">
            Empieza gratis y escala cuando tu negocio lo pida. Sin contratos ni permanencia.
          </p>
        </div>
      </section>

      <section className="py-20">
        <div className="mx-auto max-w-7xl px-6">
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {plans.map((plan, idx) => {
              const featured = idx === featuredIdx;
              const features = describeFeatures(plan.features ?? {});
              return (
                <div
                  key={plan.id}
                  className={cn(
                    'relative flex flex-col rounded-2xl border p-8',
                    featured ? 'border-foreground bg-foreground text-background shadow-xl' : 'border-border bg-card',
                  )}
                >
                  {featured && (
                    <span className="absolute right-6 top-6 inline-flex items-center rounded-full bg-[var(--brand-toucan-yellow)] px-2.5 py-1 text-[11px] font-semibold uppercase tracking-wide text-[var(--brand-ink)]">
                      Más elegido
                    </span>
                  )}
                  <h3 className={cn('text-lg font-semibold', featured ? 'text-background' : 'text-foreground')}>
                    {plan.name}
                  </h3>
                  <div className="mt-4 flex items-baseline gap-2">
                    <span className={cn('text-4xl font-bold tabular-nums', featured ? 'text-background' : 'text-foreground')}>
                      {formatPrice(plan.priceMonthly)}
                    </span>
                    {plan.priceMonthly > 0 && (
                      <span className={featured ? 'text-sm text-slate-300' : 'text-sm text-slate-500'}>/mes</span>
                    )}
                  </div>
                  <p className={cn('mt-1 text-sm tabular-nums', featured ? 'text-slate-300' : 'text-slate-500')}>
                    {(plan.maxDocumentsPerMonth ?? 0).toLocaleString('es-PE')} comprobantes / mes
                  </p>

                  <ul className="mt-6 flex-1 space-y-3">
                    {features.map((feature) => (
                      <li key={feature} className="flex items-start gap-2.5 text-sm">
                        <Check
                          className={cn(
                            'mt-0.5 h-4 w-4 shrink-0',
                            featured ? 'text-[var(--brand-toucan-yellow)]' : 'text-[var(--success,oklch(0.66_0.14_152))]',
                          )}
                        />
                        <span className={featured ? 'text-slate-100' : 'text-slate-700'}>{feature}</span>
                      </li>
                    ))}
                  </ul>

                  <Link
                    href="/register"
                    className={cn(
                      buttonVariants({ size: 'lg' }),
                      'mt-8 h-12 w-full justify-center gap-2 px-6',
                      featured
                        ? 'bg-[var(--brand-toucan-yellow)] text-[var(--brand-ink)] hover:bg-[var(--brand-toucan-yellow)]/90'
                        : 'bg-foreground text-background hover:bg-foreground/90',
                    )}
                  >
                    {plan.priceMonthly === 0 ? 'Empezar gratis' : 'Suscribirme'}
                    <ArrowRight className="h-4 w-4" />
                  </Link>
                </div>
              );
            })}
          </div>

          <div className="mt-16 rounded-2xl border border-border bg-card p-8 text-center">
            <h2 className="text-xl font-semibold text-foreground">¿Necesitas algo más grande?</h2>
            <p className="mt-2 text-slate-600">
              Más de 10,000 comprobantes al mes, integraciones a medida o atención dedicada — armamos un plan a tu medida.
            </p>
            <Link
              href="/contacto"
              className={cn(buttonVariants({ variant: 'outline', size: 'lg' }), 'mt-6 inline-flex h-12 px-6')}
            >
              Hablar con ventas
            </Link>
          </div>
        </div>
      </section>

      <section className="border-t border-border bg-slate-50 py-20">
        <div className="mx-auto max-w-3xl px-6">
          <h2 className="t-display-lg text-center text-foreground">Preguntas frecuentes</h2>
          <dl className="mt-12 space-y-8">
            {[
              {
                q: '¿Puedo cambiar de plan en cualquier momento?',
                a: 'Sí. Sube o baja de plan cuando quieras desde tu panel — el cobro se prorratea automáticamente.',
              },
              {
                q: '¿Qué pasa si me paso del límite de comprobantes?',
                a: 'Te avisamos antes de llegar al límite. Si lo superas, cobramos por comprobante adicional al precio del tier siguiente.',
              },
              {
                q: '¿Incluye soporte para producción SUNAT?',
                a: 'Sí, todos los planes emiten contra producción SUNAT con tu certificado digital y credenciales SOL.',
              },
              {
                q: '¿Hay permanencia o contratos?',
                a: 'No. Pagas mes a mes y puedes darte de baja cuando quieras. Tu data te pertenece y puedes exportarla siempre.',
              },
            ].map((item) => (
              <div key={item.q}>
                <dt className="font-semibold text-foreground">{item.q}</dt>
                <dd className="mt-2 text-slate-600">{item.a}</dd>
              </div>
            ))}
          </dl>
        </div>
      </section>
    </>
  );
}
