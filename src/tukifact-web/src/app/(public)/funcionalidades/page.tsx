import type { Metadata } from 'next';
import Link from 'next/link';
import {
  FileSpreadsheet,
  Receipt,
  FileMinus,
  FilePlus,
  Truck,
  Percent,
  Repeat,
  BarChart3,
  Sparkles,
  Webhook,
  KeyRound,
  Users,
  ArrowRight,
} from 'lucide-react';
import { buttonVariants } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { BreadcrumbJsonLd, TUKIFACT_BRAND } from '@/components/seo/jsonld';

export const metadata: Metadata = {
  title: 'Funcionalidades del facturador electrónico',
  description:
    'Todo lo que TukiFact emite y maneja: facturas, boletas, notas de crédito y débito, guías de remisión 2.0, percepciones, retenciones, facturación recurrente, IA y API REST.',
  alternates: { canonical: '/funcionalidades' },
  openGraph: {
    title: 'Funcionalidades de TukiFact',
    description:
      'Comprobantes electrónicos SUNAT, asistente IA, API REST, webhooks firmados y multi-usuario en una sola plataforma.',
    url: '/funcionalidades',
  },
};

const SECTIONS = [
  {
    icon: FileSpreadsheet,
    title: 'Facturas electrónicas (Tipo 01)',
    body: 'Para operaciones con empresas. Validación de RUC en línea, cálculo automático de IGV, ICBPER y total. Soporte para gravadas, exoneradas, inafectas y gratuitas. Moneda PEN y USD con tipo de cambio SBS.',
  },
  {
    icon: Receipt,
    title: 'Boletas de venta (Tipo 03)',
    body: 'Para consumidores finales. Validación de DNI opcional. Emisión por unidad o resúmenes diarios automáticos cuando supera el límite SUNAT.',
  },
  {
    icon: FileMinus,
    title: 'Notas de crédito (Tipo 07)',
    body: 'Anulación parcial o total, devoluciones, descuentos. Referencia automática al documento original. Catálogo SUNAT 09 completo.',
  },
  {
    icon: FilePlus,
    title: 'Notas de débito (Tipo 08)',
    body: 'Intereses por mora, recuperación de costos, otras razones. Cumple validaciones SUNAT y se enlaza al comprobante original.',
  },
  {
    icon: Truck,
    title: 'Guías de remisión 2.0 (Tipo 09 / GRE)',
    body: 'API REST OAuth2 de SUNAT (la nueva, no la SOAP vieja). Soporte para remitente, transportista, productos peligrosos y vehículos.',
  },
  {
    icon: Percent,
    title: 'Percepciones y retenciones (Tipo 40 / 20)',
    body: 'Para agentes designados por SUNAT. Cálculo automático de porcentajes por régimen. Referencias a facturas y pagos.',
  },
  {
    icon: Repeat,
    title: 'Facturación recurrente',
    body: 'Programa emisiones diarias, semanales, quincenales, mensuales o anuales. Scheduler con manejo de feriados y reanudación tras pausa.',
  },
  {
    icon: BarChart3,
    title: 'Reportes y exportación',
    body: 'Reportes de ventas, IGV, retenciones, percepciones. Exportación CSV/Excel para tu contador. Filtros por fecha, cliente, serie.',
  },
  {
    icon: Sparkles,
    title: 'Asistente con IA',
    body: 'Conversa con tus comprobantes en lenguaje natural. "Muéstrame los rechazos de esta semana", "¿Cómo emito una nota de crédito?". Tu propia API key (Gemini, Claude, Grok, DeepSeek, OpenAI).',
  },
  {
    icon: Webhook,
    title: 'Webhooks firmados',
    body: 'Recibe push notifications cada vez que un comprobante se acepta, rechaza o anula. Firma HMAC-SHA256 y reintentos con backoff exponencial.',
  },
  {
    icon: KeyRound,
    title: 'API REST y API Keys',
    body: 'Integra con tu ERP, POS, e-commerce o app móvil. Endpoints REST documentados, autenticación con API keys por permiso (emit / query / void).',
  },
  {
    icon: Users,
    title: 'Multi-usuario con roles',
    body: 'Invita a tu equipo. Roles admin (control total), emisor (solo emisión), contable (solo lectura). Audit log que rastrea cada acción.',
  },
];

export default function FuncionalidadesPage() {
  return (
    <>
      <BreadcrumbJsonLd
        items={[
          { name: 'Inicio', url: `${TUKIFACT_BRAND.url}/` },
          { name: 'Funcionalidades', url: `${TUKIFACT_BRAND.url}/funcionalidades` },
        ]}
      />
      <section className="border-b border-border bg-gradient-to-br from-background to-slate-50 py-20">
        <div className="mx-auto max-w-3xl px-6 text-center">
          <h1 className="t-display-xl text-foreground">Todo lo que necesitas, en una plataforma</h1>
          <p className="mt-4 text-lg text-slate-600">
            Cobertura completa de comprobantes electrónicos SUNAT, integraciones modernas y un asistente IA que entiende tu negocio.
          </p>
        </div>
      </section>

      <section className="py-20">
        <div className="mx-auto max-w-7xl px-6">
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {SECTIONS.map((s) => (
              <article key={s.title} className="rounded-2xl border border-border bg-card p-6">
                <div className="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-[var(--brand-toucan-yellow)]/15 text-[var(--brand-toucan-orange)]">
                  <s.icon className="h-5 w-5" />
                </div>
                <h3 className="mt-5 text-lg font-semibold text-foreground">{s.title}</h3>
                <p className="mt-2 text-sm text-slate-600">{s.body}</p>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="border-t border-border bg-[var(--brand-ink)] py-20 text-background">
        <div className="mx-auto max-w-3xl px-6 text-center">
          <h2 className="t-display-lg text-background">¿Listo para probar?</h2>
          <p className="mt-4 text-slate-300">Empieza gratis. Sin tarjeta, sin compromisos.</p>
          <Link
            href="/register"
            className={cn(buttonVariants({ size: 'lg' }), 'mt-8 inline-flex h-12 gap-2 bg-[var(--brand-toucan-yellow)] px-6 text-[var(--brand-ink)] hover:bg-[var(--brand-toucan-yellow)]/90')}
          >
            Crear cuenta gratis <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
      </section>
    </>
  );
}
