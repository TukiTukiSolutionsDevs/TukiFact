import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ArrowRight, Code2, Webhook, Package, FlaskConical, Zap, Shield, Globe } from 'lucide-react';

const FEATURES = [
  {
    icon: Code2,
    title: 'API REST',
    description: 'Integra facturación electrónica directamente en tu sistema con nuestra API RESTful. Crea, consulta y gestiona comprobantes con JSON.',
    badge: 'v1.0',
    badgeVariant: 'default' as const,
  },
  {
    icon: Webhook,
    title: 'Webhooks',
    description: 'Recibe notificaciones en tiempo real cuando SUNAT procesa tus documentos. Configura endpoints y reacciona a eventos al instante.',
    badge: 'Tiempo real',
    badgeVariant: 'secondary' as const,
  },
  {
    icon: Package,
    title: 'SDKs Oficiales',
    description: 'Librerías para TypeScript y Python. Tipado completo, manejo de errores, reintentos automáticos y ejemplos listos para producción.',
    badge: 'Open source',
    badgeVariant: 'outline' as const,
  },
  {
    icon: FlaskConical,
    title: 'Sandbox',
    description: 'Prueba tu integración sin afectar producción. RUCs de prueba incluidos, respuestas simuladas de SUNAT y logs detallados.',
    badge: 'Gratis',
    badgeVariant: 'secondary' as const,
  },
];

const STATS = [
  { label: 'Disponibilidad', value: '99.9%' },
  { label: 'Latencia promedio', value: '<200ms' },
  { label: 'Endpoints', value: '40+' },
  { label: 'Países', value: 'Perú' },
];

export default function DevelopersPage() {
  return (
    <div className="space-y-16">
      {/* Hero */}
      <section className="space-y-6">
        <div className="space-y-3">
          <div className="inline-flex items-center gap-2 rounded-full border bg-muted/50 px-3 py-1 text-sm text-muted-foreground">
            <Zap className="h-3.5 w-3.5 text-blue-500" />
            API v1.0 — Disponible ahora
          </div>
          <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">
            Construye con la{' '}
            <span className="text-blue-600 dark:text-blue-400">API de TukiFact</span>
          </h1>
          <p className="text-xl text-muted-foreground max-w-2xl">
            Integra facturación electrónica peruana (SUNAT) en tu aplicación en minutos.
            API REST simple, webhooks en tiempo real y SDKs para los lenguajes más populares.
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          <Link href="/developers/docs">
            <Button size="lg">
              Ver Documentación <ArrowRight className="ml-2 h-4 w-4" />
            </Button>
          </Link>
          <Link href="/login">
            <Button variant="outline" size="lg">Obtener API Key</Button>
          </Link>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 pt-2">
          {STATS.map((stat) => (
            <div key={stat.label} className="rounded-lg border bg-muted/30 px-4 py-3">
              <div className="text-2xl font-bold">{stat.value}</div>
              <div className="text-xs text-muted-foreground mt-0.5">{stat.label}</div>
            </div>
          ))}
        </div>
      </section>

      {/* Code snippet */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold">Emite una factura en segundos</h2>
        <div className="rounded-xl overflow-hidden border">
          <div className="flex items-center gap-2 bg-zinc-900 px-4 py-2.5 border-b border-zinc-700">
            <div className="flex gap-1.5">
              <span className="h-3 w-3 rounded-full bg-red-500" />
              <span className="h-3 w-3 rounded-full bg-yellow-500" />
              <span className="h-3 w-3 rounded-full bg-green-500" />
            </div>
            <span className="ml-2 text-xs text-zinc-400 font-mono">cURL</span>
          </div>
          <pre className="bg-zinc-900 text-green-400 text-sm p-5 overflow-x-auto font-mono leading-relaxed">
{`curl -X POST https://api.tukifact.net.pe/v1/documents \\
  -H "Authorization: Bearer TF_live_xxxxxxxxxxxx" \\
  -H "Content-Type: application/json" \\
  -d '{
    "type": "01",
    "series": "F001",
    "customer": {
      "documentType": "6",
      "documentNumber": "20100066603",
      "name": "EMPRESA CLIENTE SAC"
    },
    "items": [{
      "description": "Servicio de consultoría",
      "quantity": 1,
      "unitPrice": 1000.00,
      "igv": 180.00
    }]
  }'`}
          </pre>
        </div>
        <p className="text-sm text-muted-foreground">
          Respuesta en <code className="rounded bg-muted px-1 py-0.5 text-xs font-mono">{"<"}200ms</code> con el XML firmado, PDF y estado SUNAT incluidos.
        </p>
      </section>

      {/* Features */}
      <section className="space-y-6">
        <h2 className="text-2xl font-semibold">Todo lo que necesitás para integrar</h2>
        <div className="grid sm:grid-cols-2 gap-4">
          {FEATURES.map((feature) => {
            const Icon = feature.icon;
            return (
              <Card key={feature.title} className="hover:shadow-md transition-shadow">
                <CardHeader className="pb-3">
                  <div className="flex items-start justify-between">
                    <div className="rounded-lg bg-blue-50 dark:bg-blue-950 p-2 w-fit">
                      <Icon className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                    </div>
                    <Badge variant={feature.badgeVariant}>{feature.badge}</Badge>
                  </div>
                  <CardTitle className="text-lg mt-3">{feature.title}</CardTitle>
                </CardHeader>
                <CardContent>
                  <CardDescription className="text-sm leading-relaxed">
                    {feature.description}
                  </CardDescription>
                </CardContent>
              </Card>
            );
          })}
        </div>
      </section>

      {/* Trust */}
      <section className="rounded-xl border bg-muted/30 p-6 space-y-4">
        <h2 className="text-lg font-semibold">Diseñado para producción</h2>
        <div className="grid sm:grid-cols-3 gap-4 text-sm">
          <div className="flex items-start gap-3">
            <Shield className="h-5 w-5 text-blue-500 shrink-0 mt-0.5" />
            <div>
              <p className="font-medium">Seguridad</p>
              <p className="text-muted-foreground text-xs mt-0.5">TLS 1.3, API keys con permisos granulares, audit log completo.</p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <Zap className="h-5 w-5 text-yellow-500 shrink-0 mt-0.5" />
            <div>
              <p className="font-medium">Alta disponibilidad</p>
              <p className="text-muted-foreground text-xs mt-0.5">99.9% uptime garantizado. Reintentos automáticos ante fallas de SUNAT.</p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <Globe className="h-5 w-5 text-green-500 shrink-0 mt-0.5" />
            <div>
              <p className="font-medium">Cumplimiento SUNAT</p>
              <p className="text-muted-foreground text-xs mt-0.5">100% compatible con el reglamento de facturación electrónica peruano.</p>
            </div>
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="text-center space-y-4 py-4">
        <h2 className="text-2xl font-semibold">¿Listo para empezar?</h2>
        <p className="text-muted-foreground">Creá tu cuenta gratis y obtené tu API Key en menos de 2 minutos.</p>
        <div className="flex flex-wrap justify-center gap-3">
          <Link href="/login">
            <Button size="lg">Crear cuenta gratis</Button>
          </Link>
          <Link href="/developers/quickstart">
            <Button variant="outline" size="lg">Ver Guía Rápida</Button>
          </Link>
        </div>
      </section>
    </div>
  );
}
