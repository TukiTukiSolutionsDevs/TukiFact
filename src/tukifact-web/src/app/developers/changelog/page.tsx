import { Badge } from '@/components/ui/badge';

const VERSIONS = [
  {
    version: 'v1.0.0',
    date: 'Abril 2026',
    status: 'latest',
    type: 'Major Release',
    changes: {
      new: [
        'API REST v1 — emisión de facturas, boletas y notas de crédito',
        'Autenticación mediante API Keys con prefijo TF_live_ / TF_test_',
        'Sandbox completo con RUCs de prueba y simulación de SUNAT',
        'SDKs oficiales para TypeScript (@tukifact/sdk) y Python (tukifact)',
        'Sistema de webhooks para eventos document.accepted, document.rejected y document.voided',
        'Paginación estándar en todos los endpoints de listado',
        'Rate limiting por plan con headers X-RateLimit-*',
        'Descarga de PDF y XML firmado por documento',
        'Endpoint de estado SUNAT en tiempo real',
        'Gestión de clientes y series vía API',
        'Notificaciones vía API',
        'Soporte para tipos de documento: Factura (01), Boleta (03), Nota de Crédito (07)',
        'Headers de paginación y metadatos en todas las respuestas',
        'Documentación completa del Developer Portal',
      ],
      improvements: [],
      fixes: [],
    },
  },
];

const UPCOMING = [
  {
    version: 'v1.1.0',
    date: 'Estimado: Junio 2026',
    status: 'upcoming',
    features: [
      'Facturas recurrentes — emitir automáticamente en intervalos configurables',
      'Operaciones masivas — crear hasta 100 documentos en un solo request',
      'Filtros avanzados en listado de documentos (por RUC, fecha, monto, estado)',
      'Endpoint de exportación a CSV/Excel',
      'Soporte para Guías de Remisión (09) vía API',
      'Mejoras en mensajes de error SUNAT — más descriptivos y con códigos específicos',
    ],
  },
  {
    version: 'v1.2.0',
    date: 'Estimado: Agosto 2026',
    status: 'planned',
    features: [
      'OAuth 2.0 — autenticación delegada para integraciones de terceros',
      'SDK para PHP (tukifact/tukifact-php)',
      'Webhooks con reintentos automáticos y backoff exponencial',
      'Dashboard de métricas API en el panel de administración',
      'Soporte multi-empresa — gestionar varias empresas con una sola API Key maestra',
    ],
  },
];

function StatusBadge({ status }: { status: string }) {
  if (status === 'latest') {
    return <Badge className="bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-300 hover:bg-green-100">Última versión</Badge>;
  }
  if (status === 'upcoming') {
    return <Badge variant="secondary">En desarrollo</Badge>;
  }
  return <Badge variant="outline">Planificado</Badge>;
}

export default function ChangelogPage() {
  return (
    <div className="space-y-10">
      {/* Header */}
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Changelog</h1>
        <p className="text-muted-foreground text-lg">
          Historial de versiones y mejoras de la API de TukiFact.
        </p>
      </div>

      {/* Versiones publicadas */}
      <section className="space-y-6">
        <h2 className="text-lg font-semibold text-muted-foreground uppercase tracking-wider text-xs border-b pb-2">
          Versiones publicadas
        </h2>

        {VERSIONS.map((v) => (
          <div key={v.version} className="relative">
            {/* Version header */}
            <div className="flex items-start gap-4">
              <div className="flex flex-col items-center">
                <div className="h-3 w-3 rounded-full bg-green-500 ring-4 ring-green-500/20 mt-1.5" />
                <div className="w-0.5 bg-border flex-1 min-h-8 mt-2" />
              </div>

              <div className="flex-1 pb-8 space-y-5 min-w-0">
                <div className="flex flex-wrap items-center gap-3">
                  <h3 className="text-xl font-bold font-mono">{v.version}</h3>
                  <StatusBadge status={v.status} />
                  <Badge variant="outline">{v.type}</Badge>
                  <span className="text-sm text-muted-foreground ml-auto">{v.date}</span>
                </div>

                {v.changes.new.length > 0 && (
                  <div className="space-y-3">
                    <div className="flex items-center gap-2">
                      <span className="inline-flex items-center rounded-full bg-green-100 dark:bg-green-950 px-2.5 py-0.5 text-xs font-semibold text-green-700 dark:text-green-300">
                        ✨ Nuevo
                      </span>
                    </div>
                    <ul className="space-y-2">
                      {v.changes.new.map((item, i) => (
                        <li key={i} className="flex items-start gap-2.5 text-sm">
                          <span className="text-green-500 mt-0.5 shrink-0">+</span>
                          <span className="text-muted-foreground">{item}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
      </section>

      {/* Próximas versiones */}
      <section className="space-y-6">
        <h2 className="text-lg font-semibold text-muted-foreground uppercase tracking-wider text-xs border-b pb-2">
          Próximas versiones
        </h2>

        {UPCOMING.map((v, idx) => (
          <div key={v.version} className="relative">
            <div className="flex items-start gap-4">
              <div className="flex flex-col items-center">
                <div className={`h-3 w-3 rounded-full ring-4 mt-1.5 ${
                  v.status === 'upcoming'
                    ? 'bg-blue-400 ring-blue-400/20'
                    : 'bg-muted-foreground/40 ring-muted-foreground/10'
                }`} />
                {idx < UPCOMING.length - 1 && (
                  <div className="w-0.5 bg-border flex-1 min-h-8 mt-2" />
                )}
              </div>

              <div className="flex-1 pb-8 space-y-4 min-w-0">
                <div className="flex flex-wrap items-center gap-3">
                  <h3 className="text-xl font-bold font-mono text-muted-foreground">{v.version}</h3>
                  <StatusBadge status={v.status} />
                  <span className="text-sm text-muted-foreground ml-auto">{v.date}</span>
                </div>

                <ul className="space-y-2">
                  {v.features.map((item, i) => (
                    <li key={i} className="flex items-start gap-2.5 text-sm">
                      <span className="text-blue-400 mt-0.5 shrink-0">◦</span>
                      <span className="text-muted-foreground">{item}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        ))}
      </section>

      {/* Suscripción */}
      <section className="rounded-xl border bg-muted/30 p-6 space-y-4">
        <h2 className="font-semibold">Mantenete al día</h2>
        <p className="text-sm text-muted-foreground">
          Las notas de versión completas y los cambios breaking se anuncian con al menos 30 días de anticipación.
          Seguinos para recibir avisos antes de cada release.
        </p>
        <div className="flex flex-wrap gap-3">
          <a
            href="https://github.com/tukifact"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 rounded-lg border bg-background px-4 py-2 text-sm font-medium hover:bg-muted transition-colors"
          >
            GitHub — Watch releases
          </a>
          <a
            href="mailto:developers@tukifact.net.pe"
            className="inline-flex items-center gap-2 rounded-lg border bg-background px-4 py-2 text-sm font-medium hover:bg-muted transition-colors"
          >
            Newsletter de API
          </a>
        </div>
      </section>
    </div>
  );
}
