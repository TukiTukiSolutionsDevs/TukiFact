'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { FlaskConical, Play, Copy, CheckCircle2 } from 'lucide-react';

const TEST_RUCS = [
  { ruc: '20000000001', name: 'EMPRESA DE PRUEBA SAC', type: 'RUC activo' },
  { ruc: '20000000002', name: 'COMERCIAL DEMO EIRL', type: 'RUC activo' },
  { ruc: '20000000003', name: 'SERVICIOS TEST SA', type: 'RUC activo' },
  { ruc: '10000000001', name: 'PERSONA NATURAL PRUEBA', type: 'DNI (persona natural)' },
  { ruc: '20999999999', name: 'CLIENTE SIMULADO SRL', type: 'RUC de prueba' },
];

const COMPARISON = [
  { feature: 'Emisión de documentos', sandbox: '✓ Simulado', production: '✓ Real (SUNAT)' },
  { feature: 'Validación SUNAT', sandbox: 'Simulada (siempre OK)', production: 'Real (puede fallar)' },
  { feature: 'Descarga de PDF', sandbox: '✓ PDF de prueba', production: '✓ PDF oficial' },
  { feature: 'XML firmado', sandbox: '✓ XML de prueba', production: '✓ XML con certificado' },
  { feature: 'Webhooks', sandbox: '✓ Funcional', production: '✓ Funcional' },
  { feature: 'Costo', sandbox: 'Gratis', production: 'Según plan' },
  { feature: 'Datos reales', sandbox: 'No', production: 'Sí' },
  { feature: 'API Key prefix', sandbox: 'TF_test_', production: 'TF_live_' },
];

const ENDPOINTS = [
  { value: 'POST /v1/documents', label: 'POST /v1/documents — Crear factura' },
  { value: 'GET /v1/documents', label: 'GET /v1/documents — Listar documentos' },
  { value: 'GET /v1/customers', label: 'GET /v1/customers — Listar clientes' },
  { value: 'POST /v1/customers', label: 'POST /v1/customers — Crear cliente' },
  { value: 'GET /v1/series', label: 'GET /v1/series — Listar series' },
];

export default function SandboxPage() {
  const [copied, setCopied] = useState<string | null>(null);
  const [apiKey, setApiKey] = useState('');
  const [selectedEndpoint, setSelectedEndpoint] = useState('');

  const copyToClipboard = (text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopied(id);
    setTimeout(() => setCopied(null), 2000);
  };

  return (
    <div className="space-y-12">
      {/* Header */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <FlaskConical className="h-7 w-7 text-blue-500" />
          <h1 className="text-3xl font-bold tracking-tight">Sandbox</h1>
        </div>
        <p className="text-muted-foreground text-lg">
          Entorno de pruebas para desarrollar y testear tu integración sin riesgo ni costo.
        </p>
      </div>

      {/* Qué es el sandbox */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">¿Qué es el Sandbox?</h2>
        <p className="text-sm text-muted-foreground leading-relaxed">
          El Sandbox es un entorno aislado que simula el comportamiento de la API de producción.
          Podés crear facturas, consultar documentos y probar webhooks sin enviar datos reales a SUNAT
          ni incurrir en costos. Es el lugar ideal para desarrollar y testear tu integración antes de pasarla a producción.
        </p>
        <div className="grid sm:grid-cols-3 gap-4">
          {[
            { icon: '🔒', title: 'Sin riesgo', desc: 'Los documentos no se envían a SUNAT ni tienen validez legal.' },
            { icon: '💸', title: 'Sin costo', desc: 'El sandbox es completamente gratuito, sin límite de requests.' },
            { icon: '⚡', title: 'Idéntico a producción', desc: 'Misma API, mismos endpoints, misma estructura de respuestas.' },
          ].map((item) => (
            <div key={item.title} className="rounded-lg border bg-muted/30 p-4 space-y-2">
              <span className="text-2xl">{item.icon}</span>
              <p className="font-medium text-sm">{item.title}</p>
              <p className="text-xs text-muted-foreground">{item.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Credenciales */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">Credenciales Sandbox</h2>
        <p className="text-sm text-muted-foreground">
          Para usar el sandbox necesitás una API Key con prefijo <code className="rounded bg-muted px-1.5 py-0.5 text-xs font-mono">TF_test_</code>.
          Podés generarla desde el panel de configuración.
        </p>
        <Card className="border-blue-200 dark:border-blue-800 bg-blue-50/50 dark:bg-blue-950/20">
          <CardContent className="pt-4 space-y-3">
            <div className="space-y-1">
              <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">URL Base Sandbox</p>
              <div className="flex items-center gap-2">
                <code className="flex-1 rounded bg-background border px-3 py-2 text-sm font-mono">
                  https://sandbox.tukifact.net.pe
                </code>
                <button
                  onClick={() => copyToClipboard('https://sandbox.tukifact.net.pe', 'url')}
                  className="rounded-md p-2 hover:bg-muted transition-colors text-muted-foreground hover:text-foreground"
                >
                  {copied === 'url' ? <CheckCircle2 className="h-4 w-4 text-green-500" /> : <Copy className="h-4 w-4" />}
                </button>
              </div>
            </div>
            <p className="text-xs text-blue-700 dark:text-blue-300">
              Obtené tu API Key de sandbox en: Dashboard → Configuración → API Keys → Nueva Key (Sandbox)
            </p>
          </CardContent>
        </Card>
      </section>

      {/* Comparativa */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">Sandbox vs Producción</h2>
        <div className="rounded-lg border overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-1/2">Característica</TableHead>
                <TableHead>
                  <Badge variant="secondary">Sandbox</Badge>
                </TableHead>
                <TableHead>
                  <Badge className="bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-300 hover:bg-green-100">
                    Producción
                  </Badge>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {COMPARISON.map((row) => (
                <TableRow key={row.feature}>
                  <TableCell className="font-medium text-sm">{row.feature}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{row.sandbox}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{row.production}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </section>

      {/* RUCs de prueba */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">RUCs de Prueba</h2>
        <p className="text-sm text-muted-foreground">
          Usá estos RUCs en el sandbox para simular clientes. No corresponden a empresas reales.
        </p>
        <div className="rounded-lg border overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>RUC / DNI</TableHead>
                <TableHead>Razón Social</TableHead>
                <TableHead className="hidden sm:table-cell">Tipo</TableHead>
                <TableHead className="w-16"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {TEST_RUCS.map((item) => (
                <TableRow key={item.ruc}>
                  <TableCell className="font-mono text-sm font-medium">{item.ruc}</TableCell>
                  <TableCell className="text-sm">{item.name}</TableCell>
                  <TableCell className="hidden sm:table-cell">
                    <Badge variant="outline" className="text-xs">{item.type}</Badge>
                  </TableCell>
                  <TableCell>
                    <button
                      onClick={() => copyToClipboard(item.ruc, item.ruc)}
                      className="rounded p-1.5 hover:bg-muted transition-colors text-muted-foreground hover:text-foreground"
                      title="Copiar RUC"
                    >
                      {copied === item.ruc ? <CheckCircle2 className="h-3.5 w-3.5 text-green-500" /> : <Copy className="h-3.5 w-3.5" />}
                    </button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </section>

      {/* Probar API */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">Probar API</h2>
        <p className="text-sm text-muted-foreground">
          Probá un endpoint rápidamente. Ingresá tu API Key de sandbox y seleccioná un endpoint.
        </p>
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-base">Ejecutar Request</CardTitle>
            <CardDescription>Genera el comando cURL listo para copiar y pegar en tu terminal.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="apikey">API Key Sandbox</Label>
              <Input
                id="apikey"
                placeholder="TF_test_xxxxxxxxxxxx"
                value={apiKey}
                onChange={(e) => setApiKey(e.target.value)}
                className="font-mono"
              />
            </div>
            <div className="space-y-2">
              <Label>Endpoint</Label>
              <Select onValueChange={(v: string | null) => setSelectedEndpoint(v ?? '')}>
                <SelectTrigger>
                  <SelectValue placeholder="Seleccioná un endpoint..." />
                </SelectTrigger>
                <SelectContent>
                  {ENDPOINTS.map((ep) => (
                    <SelectItem key={ep.value} value={ep.value}>
                      <span className="font-mono text-sm">{ep.label}</span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {apiKey && selectedEndpoint && (
              <div className="space-y-2">
                <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Comando generado</p>
                <div className="rounded-xl overflow-hidden border">
                  <div className="bg-zinc-900 px-4 py-2 border-b border-zinc-700 flex items-center justify-between">
                    <span className="text-xs text-zinc-400 font-mono">cURL</span>
                    <button
                      onClick={() => copyToClipboard(
                        `curl -X ${selectedEndpoint.split(' ')[0]} https://sandbox.tukifact.net.pe${selectedEndpoint.split(' ')[1]} \\\n  -H "Authorization: Bearer ${apiKey}"`,
                        'generated'
                      )}
                      className="text-xs text-zinc-400 hover:text-zinc-200 flex items-center gap-1"
                    >
                      {copied === 'generated' ? <><CheckCircle2 className="h-3.5 w-3.5 text-green-400" /> Copiado</> : <><Copy className="h-3.5 w-3.5" /> Copiar</>}
                    </button>
                  </div>
                  <pre className="bg-zinc-900 text-green-400 text-xs sm:text-sm p-4 font-mono overflow-x-auto">
                    {`curl -X ${selectedEndpoint.split(' ')[0]} https://sandbox.tukifact.net.pe${selectedEndpoint.split(' ')[1]} \\
  -H "Authorization: Bearer ${apiKey}"`}
                  </pre>
                </div>
              </div>
            )}

            {!apiKey || !selectedEndpoint ? (
              <Button disabled className="w-full sm:w-auto">
                <Play className="mr-2 h-4 w-4" />
                Completá los campos para continuar
              </Button>
            ) : (
              <p className="text-xs text-muted-foreground">
                Copiá el comando y ejecutalo en tu terminal para probar la API.
              </p>
            )}
          </CardContent>
        </Card>
      </section>
    </div>
  );
}
