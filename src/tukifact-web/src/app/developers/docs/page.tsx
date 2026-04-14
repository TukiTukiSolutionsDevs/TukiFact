import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

const ENDPOINTS = [
  {
    group: 'Documentos',
    items: [
      { method: 'POST', path: '/v1/documents', description: 'Crear y emitir un comprobante electrónico' },
      { method: 'GET', path: '/v1/documents', description: 'Listar comprobantes con filtros y paginación' },
      { method: 'GET', path: '/v1/documents/:id', description: 'Obtener un comprobante por ID' },
      { method: 'GET', path: '/v1/documents/:id/pdf', description: 'Descargar el PDF del comprobante' },
      { method: 'GET', path: '/v1/documents/:id/xml', description: 'Descargar el XML firmado' },
      { method: 'GET', path: '/v1/documents/:id/status', description: 'Consultar estado en SUNAT' },
      { method: 'POST', path: '/v1/documents/:id/void', description: 'Anular un comprobante emitido' },
    ],
  },
  {
    group: 'Clientes',
    items: [
      { method: 'GET', path: '/v1/customers', description: 'Listar clientes del negocio' },
      { method: 'POST', path: '/v1/customers', description: 'Crear un nuevo cliente' },
      { method: 'GET', path: '/v1/customers/:id', description: 'Obtener cliente por ID' },
      { method: 'PUT', path: '/v1/customers/:id', description: 'Actualizar datos de un cliente' },
      { method: 'DELETE', path: '/v1/customers/:id', description: 'Eliminar un cliente' },
    ],
  },
  {
    group: 'Series',
    items: [
      { method: 'GET', path: '/v1/series', description: 'Listar series disponibles' },
      { method: 'POST', path: '/v1/series', description: 'Crear una nueva serie' },
      { method: 'GET', path: '/v1/series/:id/next-number', description: 'Obtener el siguiente número correlativo' },
    ],
  },
  {
    group: 'Webhooks',
    items: [
      { method: 'GET', path: '/v1/webhooks', description: 'Listar webhooks configurados' },
      { method: 'POST', path: '/v1/webhooks', description: 'Registrar un nuevo webhook endpoint' },
      { method: 'PUT', path: '/v1/webhooks/:id', description: 'Actualizar un webhook' },
      { method: 'DELETE', path: '/v1/webhooks/:id', description: 'Eliminar un webhook' },
      { method: 'POST', path: '/v1/webhooks/:id/test', description: 'Enviar un evento de prueba' },
    ],
  },
  {
    group: 'Notificaciones',
    items: [
      { method: 'GET', path: '/v1/notifications', description: 'Listar notificaciones del sistema' },
      { method: 'POST', path: '/v1/notifications/:id/read', description: 'Marcar notificación como leída' },
    ],
  },
];

const ERROR_CODES = [
  { code: '400', name: 'Bad Request', description: 'Datos inválidos en el cuerpo de la solicitud' },
  { code: '401', name: 'Unauthorized', description: 'API Key ausente, inválida o expirada' },
  { code: '403', name: 'Forbidden', description: 'Sin permisos para este recurso o acción' },
  { code: '404', name: 'Not Found', description: 'El recurso solicitado no existe' },
  { code: '409', name: 'Conflict', description: 'El documento ya fue emitido o hay un conflicto de estado' },
  { code: '422', name: 'Unprocessable Entity', description: 'Error de validación SUNAT en el XML generado' },
  { code: '429', name: 'Too Many Requests', description: 'Límite de requests por hora excedido' },
  { code: '500', name: 'Internal Server Error', description: 'Error interno — contactar soporte' },
  { code: '503', name: 'Service Unavailable', description: 'SUNAT no disponible temporalmente, reintentar' },
];

const RATE_LIMITS = [
  { plan: 'Free', limit: '100 req/hora', monthly: '1,000 documentos' },
  { plan: 'Emprendedor', limit: '500 req/hora', monthly: '10,000 documentos' },
  { plan: 'Negocio', limit: '2,000 req/hora', monthly: '50,000 documentos' },
  { plan: 'Profesional', limit: '5,000 req/hora', monthly: '200,000 documentos' },
  { plan: 'Empresa', limit: '20,000 req/hora', monthly: 'Ilimitado' },
];

function MethodBadge({ method }: { method: string }) {
  const styles: Record<string, string> = {
    GET: 'bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-300',
    POST: 'bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300',
    PUT: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-950 dark:text-yellow-300',
    DELETE: 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300',
    PATCH: 'bg-orange-100 text-orange-700 dark:bg-orange-950 dark:text-orange-300',
  };
  return (
    <span className={`inline-flex items-center rounded px-1.5 py-0.5 text-xs font-bold font-mono ${styles[method] ?? 'bg-muted text-muted-foreground'}`}>
      {method}
    </span>
  );
}

export default function DocsPage() {
  return (
    <div className="space-y-12">
      {/* Header */}
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Documentación de la API</h1>
        <p className="text-muted-foreground text-lg">
          Referencia completa de la API REST de TukiFact para integrar facturación electrónica en tu sistema.
        </p>
      </div>

      {/* Autenticación */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">Autenticación</h2>
        <p className="text-muted-foreground text-sm leading-relaxed">
          Todas las solicitudes a la API requieren autenticación mediante una <strong>API Key</strong>.
          Incluí tu clave en el header <code className="rounded bg-muted px-1.5 py-0.5 text-xs font-mono">Authorization</code> de cada request.
        </p>
        <div className="rounded-xl overflow-hidden border">
          <div className="bg-zinc-900 px-4 py-2 border-b border-zinc-700 flex items-center gap-2">
            <span className="text-xs text-zinc-400 font-mono">Header de autenticación</span>
          </div>
          <pre className="bg-zinc-900 text-green-400 text-sm p-4 overflow-x-auto font-mono">
{`Authorization: Bearer TF_live_xxxxxxxxxxxxxxxxxxxx`}
          </pre>
        </div>
        <Card className="border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-950/30">
          <CardContent className="pt-4 pb-4">
            <p className="text-sm text-amber-800 dark:text-amber-300">
              <strong>⚠ Importante:</strong> Nunca expongas tu API Key en código del lado del cliente (browser). Usá siempre variables de entorno del servidor.
            </p>
          </CardContent>
        </Card>
      </section>

      {/* URLs Base */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">URLs Base</h2>
        <div className="grid sm:grid-cols-2 gap-4">
          <Card>
            <CardHeader className="pb-2">
              <div className="flex items-center gap-2">
                <Badge className="bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-300 hover:bg-green-100">Producción</Badge>
              </div>
              <CardTitle className="text-base">API en vivo</CardTitle>
            </CardHeader>
            <CardContent>
              <code className="text-sm font-mono bg-muted rounded px-2 py-1 block break-all">
                https://api.tukifact.net.pe
              </code>
              <p className="text-xs text-muted-foreground mt-2">Usa API Keys con prefijo <code className="font-mono">TF_live_</code></p>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <div className="flex items-center gap-2">
                <Badge variant="secondary">Sandbox</Badge>
              </div>
              <CardTitle className="text-base">Entorno de pruebas</CardTitle>
            </CardHeader>
            <CardContent>
              <code className="text-sm font-mono bg-muted rounded px-2 py-1 block break-all">
                https://sandbox.tukifact.net.pe
              </code>
              <p className="text-xs text-muted-foreground mt-2">Usa API Keys con prefijo <code className="font-mono">TF_test_</code></p>
            </CardContent>
          </Card>
        </div>
      </section>

      {/* Rate Limits */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">Límites por Plan</h2>
        <p className="text-sm text-muted-foreground">
          Los headers <code className="rounded bg-muted px-1 py-0.5 text-xs font-mono">X-RateLimit-Limit</code>, <code className="rounded bg-muted px-1 py-0.5 text-xs font-mono">X-RateLimit-Remaining</code> y <code className="rounded bg-muted px-1 py-0.5 text-xs font-mono">X-RateLimit-Reset</code> se incluyen en cada respuesta.
        </p>
        <div className="rounded-lg border overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Plan</TableHead>
                <TableHead>Requests/hora</TableHead>
                <TableHead>Documentos/mes</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {RATE_LIMITS.map((row) => (
                <TableRow key={row.plan}>
                  <TableCell className="font-medium">{row.plan}</TableCell>
                  <TableCell className="font-mono text-sm">{row.limit}</TableCell>
                  <TableCell className="text-muted-foreground text-sm">{row.monthly}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </section>

      {/* Formato de respuesta */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">Formato de Respuesta</h2>
        <p className="text-sm text-muted-foreground">
          Todas las respuestas son en <strong>JSON</strong>. Las listas incluyen paginación estándar.
        </p>
        <div className="grid sm:grid-cols-2 gap-4">
          <div className="rounded-xl overflow-hidden border">
            <div className="bg-zinc-900 px-4 py-2 border-b border-zinc-700">
              <span className="text-xs text-zinc-400 font-mono">Respuesta exitosa</span>
            </div>
            <pre className="bg-zinc-900 text-green-400 text-xs p-4 overflow-x-auto font-mono leading-relaxed">
{`{
  "data": { ... },
  "meta": {
    "requestId": "req_xxxx"
  }
}`}
            </pre>
          </div>
          <div className="rounded-xl overflow-hidden border">
            <div className="bg-zinc-900 px-4 py-2 border-b border-zinc-700">
              <span className="text-xs text-zinc-400 font-mono">Lista paginada</span>
            </div>
            <pre className="bg-zinc-900 text-green-400 text-xs p-4 overflow-x-auto font-mono leading-relaxed">
{`{
  "data": [ ... ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "total": 150,
    "totalPages": 8
  }
}`}
            </pre>
          </div>
        </div>
      </section>

      {/* Códigos de error */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">Códigos de Error</h2>
        <div className="rounded-lg border overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-20">Código</TableHead>
                <TableHead className="w-48">Nombre</TableHead>
                <TableHead>Descripción</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {ERROR_CODES.map((err) => (
                <TableRow key={err.code}>
                  <TableCell>
                    <span className={`font-mono font-bold text-sm ${
                      err.code.startsWith('4') ? 'text-yellow-600 dark:text-yellow-400' : 'text-red-600 dark:text-red-400'
                    }`}>
                      {err.code}
                    </span>
                  </TableCell>
                  <TableCell className="font-medium text-sm">{err.name}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{err.description}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </section>

      {/* Endpoints */}
      <section className="space-y-6">
        <h2 className="text-xl font-semibold border-b pb-2">Endpoints</h2>
        {ENDPOINTS.map((group) => (
          <div key={group.group} className="space-y-3">
            <h3 className="text-base font-semibold text-muted-foreground uppercase tracking-wide text-xs">
              {group.group}
            </h3>
            <div className="rounded-lg border overflow-hidden divide-y">
              {group.items.map((endpoint) => (
                <div key={endpoint.path} className="flex items-center gap-4 px-4 py-3 hover:bg-muted/40 transition-colors">
                  <MethodBadge method={endpoint.method} />
                  <code className="text-sm font-mono text-foreground flex-shrink-0">{endpoint.path}</code>
                  <span className="text-sm text-muted-foreground hidden sm:block">{endpoint.description}</span>
                </div>
              ))}
            </div>
          </div>
        ))}
      </section>
    </div>
  );
}
