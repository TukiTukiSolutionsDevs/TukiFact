'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ArrowRight, CheckCircle2 } from 'lucide-react';

const STEPS = [
  {
    number: 1,
    title: 'Crear cuenta y obtener API Key',
    description: 'Registrate en TukiFact y generá tu primera API Key desde el panel de configuración.',
    content: null,
  },
  {
    number: 2,
    title: 'Elegir entorno',
    description: 'Usá el sandbox para probar sin riesgo. Cuando estés listo, cambiá a producción.',
    content: {
      type: 'table',
      rows: [
        ['', 'Sandbox', 'Producción'],
        ['URL', 'sandbox.tukifact.net.pe', 'api.tukifact.net.pe'],
        ['API Key', 'TF_test_xxxx', 'TF_live_xxxx'],
        ['Envío a SUNAT', 'No (simulado)', 'Sí (real)'],
        ['Costo', 'Gratis', 'Según plan'],
      ],
    },
  },
  {
    number: 3,
    title: 'Crear tu primera factura',
    description: 'Emití una factura electrónica con el tipo de documento 01 (Factura).',
    content: { type: 'tabs' },
  },
  {
    number: 4,
    title: 'Consultar estado del documento',
    description: 'Verificá si SUNAT procesó correctamente tu comprobante.',
    content: { type: 'status' },
  },
  {
    number: 5,
    title: 'Descargar el PDF',
    description: 'Obtené el PDF firmado para enviar a tu cliente.',
    content: { type: 'pdf' },
  },
  {
    number: 6,
    title: 'Configurar webhooks',
    description: 'Recibí notificaciones automáticas cuando SUNAT procese tus documentos.',
    content: { type: 'webhook' },
  },
];

const CODE_EXAMPLES: Record<string, Record<string, string>> = {
  typescript: {
    create: `import { TukiFact } from '@tukifact/sdk';

const client = new TukiFact({
  apiKey: process.env.TUKIFACT_API_KEY,
  environment: 'sandbox', // 'production' para producción
});

const factura = await client.documents.create({
  type: '01', // Factura
  series: 'F001',
  customer: {
    documentType: '6',       // RUC
    documentNumber: '20100066603',
    name: 'EMPRESA CLIENTE SAC',
    address: 'Av. Javier Prado 123, Lima',
  },
  items: [
    {
      description: 'Servicio de desarrollo de software',
      quantity: 1,
      unitPrice: 1000.00,
      igv: 180.00,
    },
  ],
});

console.log(factura.id);      // doc_xxxxxxxxxxxx
console.log(factura.status);  // "accepted"`,
    status: `const documento = await client.documents.getStatus(
  'doc_xxxxxxxxxxxx'
);

console.log(documento.sunatStatus); // "ACEPTADO"
console.log(documento.cdrCode);     // "0"`,
    pdf: `const pdf = await client.documents.getPdf(
  'doc_xxxxxxxxxxxx'
);

// pdf es un Buffer con el contenido del PDF
fs.writeFileSync('factura.pdf', pdf);`,
    webhook: `const webhook = await client.webhooks.create({
  url: 'https://miapp.com/api/webhooks/tukifact',
  events: ['document.accepted', 'document.rejected'],
  secret: 'mi_secreto_para_validar',
});

console.log(webhook.id);`,
  },
  python: {
    create: `from tukifact import TukiFact
import os

client = TukiFact(
    api_key=os.environ["TUKIFACT_API_KEY"],
    environment="sandbox",  # "production" para producción
)

factura = client.documents.create(
    type="01",
    series="F001",
    customer={
        "document_type": "6",
        "document_number": "20100066603",
        "name": "EMPRESA CLIENTE SAC",
        "address": "Av. Javier Prado 123, Lima",
    },
    items=[
        {
            "description": "Servicio de desarrollo de software",
            "quantity": 1,
            "unit_price": 1000.00,
            "igv": 180.00,
        }
    ],
)

print(factura.id)      # doc_xxxxxxxxxxxx
print(factura.status)  # accepted`,
    status: `documento = client.documents.get_status(
    "doc_xxxxxxxxxxxx"
)

print(documento.sunat_status)  # ACEPTADO
print(documento.cdr_code)      # 0`,
    pdf: `pdf_content = client.documents.get_pdf(
    "doc_xxxxxxxxxxxx"
)

with open("factura.pdf", "wb") as f:
    f.write(pdf_content)`,
    webhook: `webhook = client.webhooks.create(
    url="https://miapp.com/api/webhooks/tukifact",
    events=["document.accepted", "document.rejected"],
    secret="mi_secreto_para_validar",
)

print(webhook.id)`,
  },
  curl: {
    create: `curl -X POST https://sandbox.tukifact.net.pe/v1/documents \\
  -H "Authorization: Bearer TF_test_xxxxxxxxxxxx" \\
  -H "Content-Type: application/json" \\
  -d '{
    "type": "01",
    "series": "F001",
    "customer": {
      "documentType": "6",
      "documentNumber": "20100066603",
      "name": "EMPRESA CLIENTE SAC",
      "address": "Av. Javier Prado 123, Lima"
    },
    "items": [{
      "description": "Servicio de desarrollo de software",
      "quantity": 1,
      "unitPrice": 1000.00,
      "igv": 180.00
    }]
  }'`,
    status: `curl https://sandbox.tukifact.net.pe/v1/documents/doc_xxxxxxxxxxxx/status \\
  -H "Authorization: Bearer TF_test_xxxxxxxxxxxx"`,
    pdf: `curl https://sandbox.tukifact.net.pe/v1/documents/doc_xxxxxxxxxxxx/pdf \\
  -H "Authorization: Bearer TF_test_xxxxxxxxxxxx" \\
  --output factura.pdf`,
    webhook: `curl -X POST https://sandbox.tukifact.net.pe/v1/webhooks \\
  -H "Authorization: Bearer TF_test_xxxxxxxxxxxx" \\
  -H "Content-Type: application/json" \\
  -d '{
    "url": "https://miapp.com/api/webhooks/tukifact",
    "events": ["document.accepted", "document.rejected"],
    "secret": "mi_secreto_para_validar"
  }'`,
  },
};

const codeKeyByStep: Record<number, string> = {
  3: 'create',
  4: 'status',
  5: 'pdf',
  6: 'webhook',
};

export default function QuickstartPage() {
  const [activeTab, setActiveTab] = useState<'typescript' | 'python' | 'curl'>('typescript');

  const tabs = [
    { id: 'typescript' as const, label: 'TypeScript' },
    { id: 'python' as const, label: 'Python' },
    { id: 'curl' as const, label: 'cURL' },
  ];

  return (
    <div className="space-y-10">
      {/* Header */}
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Guía Rápida</h1>
        <p className="text-muted-foreground text-lg">
          Integrá la API de TukiFact en tu aplicación siguiendo estos 6 pasos.
          Tiempo estimado: <strong>15 minutos</strong>.
        </p>
      </div>

      {/* Language selector */}
      <div className="sticky top-14 z-10 bg-background/95 backdrop-blur py-3 border-b -mx-4 sm:-mx-6 px-4 sm:px-6">
        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground mr-1 hidden sm:block">Lenguaje:</span>
          <div className="flex rounded-lg border p-1 gap-1">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                  activeTab === tab.id
                    ? 'bg-primary text-primary-foreground shadow-sm'
                    : 'text-muted-foreground hover:text-foreground'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Steps */}
      <div className="space-y-8">
        {STEPS.map((step, idx) => {
          const isLast = idx === STEPS.length - 1;
          const codeKey = codeKeyByStep[step.number];
          const code = codeKey ? CODE_EXAMPLES[activeTab][codeKey] : null;

          return (
            <div key={step.number} className="flex gap-4 sm:gap-6">
              {/* Step indicator */}
              <div className="flex flex-col items-center">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-blue-600 text-white text-sm font-bold shadow-sm">
                  {step.number}
                </div>
                {!isLast && (
                  <div className="mt-2 w-0.5 flex-1 bg-border min-h-[2rem]" />
                )}
              </div>

              {/* Content */}
              <div className="flex-1 pb-6 space-y-4 min-w-0">
                <div>
                  <h3 className="text-lg font-semibold">{step.title}</h3>
                  <p className="text-muted-foreground text-sm mt-1">{step.description}</p>
                </div>

                {/* Step 1: link */}
                {step.number === 1 && (
                  <Link href="/login">
                    <Button variant="outline" size="sm">
                      Crear cuenta gratis <ArrowRight className="ml-2 h-3.5 w-3.5" />
                    </Button>
                  </Link>
                )}

                {/* Step 2: table */}
                {step.number === 2 && step.content?.type === 'table' && (
                  <div className="rounded-lg border overflow-hidden text-sm">
                    <table className="w-full">
                      <thead className="bg-muted/50">
                        <tr>
                          {(step.content.rows as string[][])[0].map((cell, i) => (
                            <th key={i} className="text-left px-4 py-2.5 font-semibold text-xs uppercase tracking-wider text-muted-foreground">
                              {cell}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody className="divide-y">
                        {(step.content.rows as string[][]).slice(1).map((row, ri) => (
                          <tr key={ri} className="hover:bg-muted/30 transition-colors">
                            {row.map((cell, ci) => (
                              <td key={ci} className={`px-4 py-2.5 ${ci === 0 ? 'font-medium' : 'text-muted-foreground font-mono text-xs'}`}>
                                {cell}
                              </td>
                            ))}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                {/* Steps 3-6: code */}
                {code && (
                  <div className="rounded-xl overflow-hidden border">
                    <div className="flex items-center gap-2 bg-zinc-900 px-4 py-2 border-b border-zinc-700">
                      <div className="flex gap-1.5">
                        <span className="h-2.5 w-2.5 rounded-full bg-red-500" />
                        <span className="h-2.5 w-2.5 rounded-full bg-yellow-500" />
                        <span className="h-2.5 w-2.5 rounded-full bg-green-500" />
                      </div>
                      <span className="ml-1 text-xs text-zinc-400 font-mono capitalize">{activeTab}</span>
                      {step.number === 3 && codeKey === 'create' && (
                        <Badge variant="secondary" className="ml-auto text-xs">Paso principal</Badge>
                      )}
                    </div>
                    <pre className="bg-zinc-900 text-green-400 text-xs sm:text-sm p-4 overflow-x-auto font-mono leading-relaxed">
                      {code}
                    </pre>
                  </div>
                )}

                {step.number === 6 && (
                  <div className="flex items-start gap-3 rounded-lg border bg-green-50 dark:bg-green-950/30 border-green-200 dark:border-green-800 p-4">
                    <CheckCircle2 className="h-5 w-5 text-green-600 dark:text-green-400 shrink-0 mt-0.5" />
                    <div className="text-sm">
                      <p className="font-medium text-green-800 dark:text-green-300">¡Integración completa!</p>
                      <p className="text-green-700 dark:text-green-400 mt-0.5 text-xs">
                        Tu sistema ya puede emitir facturas electrónicas, consultar su estado y recibir notificaciones automáticas.
                      </p>
                    </div>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Next steps */}
      <div className="rounded-xl border bg-muted/30 p-6 space-y-4">
        <h2 className="font-semibold">Próximos pasos</h2>
        <div className="grid sm:grid-cols-3 gap-3">
          {[
            { label: 'Documentación completa', href: '/developers/docs', desc: 'Todos los endpoints y parámetros' },
            { label: 'Explorar SDKs', href: '/developers/sdks', desc: 'Librerías oficiales listas para usar' },
            { label: 'Probar en Sandbox', href: '/developers/sandbox', desc: 'RUCs de prueba y ejemplos' },
          ].map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="rounded-lg border bg-background p-4 hover:shadow-md transition-shadow group"
            >
              <p className="font-medium text-sm group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                {item.label} <ArrowRight className="inline h-3.5 w-3.5 ml-1" />
              </p>
              <p className="text-xs text-muted-foreground mt-1">{item.desc}</p>
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}
