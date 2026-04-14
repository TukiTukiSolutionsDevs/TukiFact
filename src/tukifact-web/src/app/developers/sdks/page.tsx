import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { ExternalLink, GitFork } from 'lucide-react';

const SDKS = [
  {
    language: 'TypeScript',
    icon: '🟦',
    package: '@tukifact/sdk',
    packageManager: 'npm',
    installCmd: 'npm install @tukifact/sdk',
    version: '1.0.0',
    status: 'Estable',
    statusVariant: 'default' as const,
    description: 'SDK oficial para TypeScript y JavaScript. Soporte completo para Node.js, Next.js, y cualquier entorno JS moderno.',
    features: ['Tipos TypeScript completos', 'Async/await nativo', 'Reintentos automáticos', 'Paginación automática'],
    github: 'https://github.com/tukifact/tukifact-node',
    code: `import { TukiFact } from '@tukifact/sdk';

const client = new TukiFact({
  apiKey: process.env.TUKIFACT_API_KEY,
  environment: 'sandbox',
});

// Crear factura
const factura = await client.documents.create({
  type: '01',
  series: 'F001',
  customer: {
    documentType: '6',
    documentNumber: '20100066603',
    name: 'EMPRESA CLIENTE SAC',
  },
  items: [{
    description: 'Servicio de consultoría',
    quantity: 1,
    unitPrice: 1000.00,
    igv: 180.00,
  }],
});

console.log(factura.id);     // doc_xxxx
console.log(factura.status); // "accepted"`,
  },
  {
    language: 'Python',
    icon: '🐍',
    package: 'tukifact',
    packageManager: 'pip',
    installCmd: 'pip install tukifact',
    version: '1.0.0',
    status: 'Estable',
    statusVariant: 'default' as const,
    description: 'SDK oficial para Python 3.8+. Compatible con Django, FastAPI, Flask y scripts de automatización.',
    features: ['Type hints completos', 'Soporte sync y async', 'Reintentos con backoff', 'Context managers'],
    github: 'https://github.com/tukifact/tukifact-python',
    code: `from tukifact import TukiFact
import os

client = TukiFact(
    api_key=os.environ["TUKIFACT_API_KEY"],
    environment="sandbox",
)

# Crear factura
factura = client.documents.create(
    type="01",
    series="F001",
    customer={
        "document_type": "6",
        "document_number": "20100066603",
        "name": "EMPRESA CLIENTE SAC",
    },
    items=[{
        "description": "Servicio de consultoría",
        "quantity": 1,
        "unit_price": 1000.00,
        "igv": 180.00,
    }],
)

print(factura.id)      # doc_xxxx
print(factura.status)  # accepted`,
  },
  {
    language: 'REST API',
    icon: '🌐',
    package: 'API Directa',
    packageManager: null,
    installCmd: null,
    version: 'v1',
    status: 'Estable',
    statusVariant: 'default' as const,
    description: 'Integrá directamente con HTTP desde cualquier lenguaje o plataforma. Compatible con cualquier stack tecnológico.',
    features: ['Sin dependencias', 'Compatible con cualquier lenguaje', 'JSON estándar', 'TLS 1.3'],
    github: null,
    code: `# Crear factura con cURL
curl -X POST https://sandbox.tukifact.net.pe/v1/documents \\
  -H "Authorization: Bearer TF_test_xxxx" \\
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
  }'`,
  },
];

export default function SDKsPage() {
  return (
    <div className="space-y-10">
      {/* Header */}
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">SDKs Oficiales</h1>
        <p className="text-muted-foreground text-lg">
          Librerías oficiales de TukiFact. Tipado completo, manejo de errores y ejemplos listos para producción.
        </p>
      </div>

      {/* SDK Cards */}
      <div className="space-y-6">
        {SDKS.map((sdk) => (
          <Card key={sdk.language} className="overflow-hidden">
            <CardHeader className="pb-4">
              <div className="flex items-start justify-between flex-wrap gap-3">
                <div className="flex items-center gap-3">
                  <span className="text-3xl">{sdk.icon}</span>
                  <div>
                    <CardTitle className="text-xl">{sdk.language}</CardTitle>
                    <CardDescription className="mt-0.5">
                      <code className="font-mono text-sm">{sdk.package}</code>
                    </CardDescription>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant="outline" className="font-mono">v{sdk.version}</Badge>
                  <Badge variant={sdk.statusVariant}>{sdk.status}</Badge>
                </div>
              </div>
              <p className="text-sm text-muted-foreground mt-2">{sdk.description}</p>

              {/* Features */}
              <div className="flex flex-wrap gap-2 mt-3">
                {sdk.features.map((f) => (
                  <span key={f} className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs text-muted-foreground">
                    ✓ {f}
                  </span>
                ))}
              </div>
            </CardHeader>

            <CardContent className="space-y-4">
              {/* Install command */}
              {sdk.installCmd && (
                <div>
                  <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">
                    {sdk.packageManager === 'npm' ? 'Instalación' : 'Instalación'}
                  </p>
                  <div className="rounded-lg overflow-hidden border">
                    <div className="bg-zinc-900 px-3 py-1.5 border-b border-zinc-700">
                      <span className="text-xs text-zinc-400 font-mono">{sdk.packageManager}</span>
                    </div>
                    <pre className="bg-zinc-900 text-green-400 text-sm px-4 py-3 font-mono overflow-x-auto">
                      {sdk.installCmd}
                    </pre>
                  </div>
                </div>
              )}

              {/* Code example */}
              <div>
                <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">
                  Ejemplo de uso
                </p>
                <div className="rounded-xl overflow-hidden border">
                  <div className="flex items-center gap-2 bg-zinc-900 px-4 py-2 border-b border-zinc-700">
                    <div className="flex gap-1.5">
                      <span className="h-2.5 w-2.5 rounded-full bg-red-500" />
                      <span className="h-2.5 w-2.5 rounded-full bg-yellow-500" />
                      <span className="h-2.5 w-2.5 rounded-full bg-green-500" />
                    </div>
                    <span className="ml-1 text-xs text-zinc-400 font-mono">{sdk.language.toLowerCase()}</span>
                  </div>
                  <pre className="bg-zinc-900 text-green-400 text-xs sm:text-sm p-4 overflow-x-auto font-mono leading-relaxed">
                    {sdk.code}
                  </pre>
                </div>
              </div>
            </CardContent>

            <CardFooter className="gap-3 pt-0">
              {sdk.github && (
                <a href={sdk.github} target="_blank" rel="noopener noreferrer">
                  <Button variant="outline" size="sm">
                    <GitFork className="mr-2 h-4 w-4" />
                    Ver en GitHub
                    <ExternalLink className="ml-2 h-3.5 w-3.5 text-muted-foreground" />
                  </Button>
                </a>
              )}
              {!sdk.github && (
                <a href="/developers/docs">
                  <Button variant="outline" size="sm">
                    Ver Documentación
                    <ExternalLink className="ml-2 h-3.5 w-3.5" />
                  </Button>
                </a>
              )}
            </CardFooter>
          </Card>
        ))}
      </div>

      {/* Note */}
      <div className="rounded-xl border bg-muted/30 p-5 text-sm text-muted-foreground space-y-2">
        <p className="font-medium text-foreground">¿Usás otro lenguaje?</p>
        <p>
          Podés integrarte directamente con la <a href="/developers/docs" className="text-blue-600 dark:text-blue-400 hover:underline">API REST</a> usando HTTP estándar desde cualquier lenguaje: PHP, Ruby, Go, Java, .NET, y más.
        </p>
        <p>
          Si necesitás un SDK oficial para tu stack, escribinos a <span className="text-blue-600 dark:text-blue-400">developers@tukifact.net.pe</span>
        </p>
      </div>
    </div>
  );
}
