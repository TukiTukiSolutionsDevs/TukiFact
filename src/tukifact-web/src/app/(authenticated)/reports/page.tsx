'use client';

import { useState, useMemo } from 'react';
import { api, type DocumentResponse, type PaginatedResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Separator } from '@/components/ui/separator';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts';
import {
  FileText,
  TrendingUp,
  Receipt,
  CheckCircle,
  Download,
  Search,
} from 'lucide-react';
import { toast } from 'sonner';

const fmt = (n: number, c = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency: c }).format(n);

const STATUS_LABELS: Record<string, string> = {
  accepted: 'Aceptado',
  rejected: 'Rechazado',
  voided: 'Anulado',
  draft: 'Borrador',
  sent: 'Enviado',
};

const DOC_TYPE_LABELS: Record<string, string> = {
  '01': 'Factura',
  '03': 'Boleta',
  '07': 'Nota Crédito',
  '08': 'Nota Débito',
};

const STATUS_COLORS: Record<string, string> = {
  accepted: 'bg-green-100 text-green-700',
  rejected: 'bg-red-100 text-red-700',
  voided: 'bg-gray-100 text-gray-600',
  draft: 'bg-yellow-100 text-yellow-700',
  sent: 'bg-blue-100 text-blue-700',
};

export default function ReportsPage() {
  const today = new Date();
  const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
  const formatDate = (d: Date) => d.toISOString().split('T')[0];

  const [dateFrom, setDateFrom] = useState(formatDate(firstDay));
  const [dateTo, setDateTo] = useState(formatDate(today));
  const [documents, setDocuments] = useState<DocumentResponse[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [hasFetched, setHasFetched] = useState(false);

  const fetchDocuments = async () => {
    if (!dateFrom || !dateTo) {
      toast.error('Selecciona un rango de fechas');
      return;
    }
    setIsLoading(true);
    try {
      const res = await api.get<PaginatedResponse<DocumentResponse>>(
        `/v1/documents?page=1&pageSize=500&dateFrom=${dateFrom}&dateTo=${dateTo}`
      );
      setDocuments(res.data);
      setHasFetched(true);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al cargar datos');
    } finally {
      setIsLoading(false);
    }
  };

  // KPI aggregations
  const kpis = useMemo(() => {
    const accepted = documents.filter((d) => d.status === 'accepted');
    return {
      totalVentas: accepted.reduce((s, d) => s + d.total, 0),
      totalIgv: accepted.reduce((s, d) => s + d.igv, 0),
      totalDocs: documents.length,
      totalAceptados: accepted.length,
      totalAnulados: documents.filter((d) => d.status === 'voided').length,
      totalRechazados: documents.filter((d) => d.status === 'rejected').length,
    };
  }, [documents]);

  // Chart data: ventas por tipo
  const chartData = useMemo(() => {
    const byType: Record<string, { count: number; total: number }> = {};
    for (const doc of documents) {
      const label = DOC_TYPE_LABELS[doc.documentType] ?? doc.documentType;
      if (!byType[label]) byType[label] = { count: 0, total: 0 };
      byType[label].count += 1;
      byType[label].total += doc.total;
    }
    return Object.entries(byType).map(([name, v]) => ({
      name,
      'Documentos': v.count,
      'Total (S/)': parseFloat(v.total.toFixed(2)),
    }));
  }, [documents]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Reportes</h1>
        <p className="text-muted-foreground">
          Análisis de comprobantes por periodo
        </p>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-wrap gap-4 items-end">
            <div>
              <Label>Fecha Desde</Label>
              <Input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="w-40"
              />
            </div>
            <div>
              <Label>Fecha Hasta</Label>
              <Input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="w-40"
              />
            </div>
            <Button onClick={fetchDocuments} disabled={isLoading}>
              <Search className="h-4 w-4 mr-2" />
              {isLoading ? 'Cargando...' : 'Filtrar'}
            </Button>
            <Button variant="outline" disabled className="ml-auto">
              <Download className="h-4 w-4 mr-2" />
              Exportar
            </Button>
          </div>
        </CardContent>
      </Card>

      {hasFetched && (
        <>
          {/* KPI Cards */}
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
            <Card className="col-span-2">
              <CardContent className="pt-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-100">
                    <TrendingUp className="h-5 w-5 text-blue-600" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Total Ventas</p>
                    <p className="text-xl font-bold">{fmt(kpis.totalVentas)}</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-orange-100">
                    <Receipt className="h-5 w-5 text-orange-600" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Total IGV</p>
                    <p className="text-lg font-bold">{fmt(kpis.totalIgv)}</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-100">
                    <FileText className="h-5 w-5 text-purple-600" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Total Docs</p>
                    <p className="text-2xl font-bold">{kpis.totalDocs}</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-green-100">
                    <CheckCircle className="h-5 w-5 text-green-600" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Aceptados</p>
                    <p className="text-2xl font-bold text-green-600">{kpis.totalAceptados}</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6">
                <div className="space-y-1 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Anulados</span>
                    <span className="font-medium text-gray-500">{kpis.totalAnulados}</span>
                  </div>
                  <Separator />
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Rechazados</span>
                    <span className="font-medium text-red-500">{kpis.totalRechazados}</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Chart */}
          {chartData.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Ventas por Tipo de Documento</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={280}>
                  <BarChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                    <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                    <YAxis yAxisId="left" tick={{ fontSize: 12 }} />
                    <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 12 }} />
                    <Tooltip
                      formatter={(value, name) =>
                        name === 'Total (S/)' ? fmt(value as number) : value
                      }
                    />
                    <Legend />
                    <Bar yAxisId="right" dataKey="Total (S/)" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                    <Bar yAxisId="left" dataKey="Documentos" fill="#93c5fd" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          {/* Table */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">
                Comprobantes del Periodo
                <span className="ml-2 text-sm font-normal text-muted-foreground">
                  ({documents.length} registros)
                </span>
              </CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              {documents.length === 0 ? (
                <div className="py-12 text-center text-muted-foreground">
                  No hay comprobantes en el período seleccionado
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Número</TableHead>
                        <TableHead>Tipo</TableHead>
                        <TableHead>Fecha</TableHead>
                        <TableHead>Cliente</TableHead>
                        <TableHead className="text-right">Base Imponible</TableHead>
                        <TableHead className="text-right">IGV</TableHead>
                        <TableHead className="text-right">Total</TableHead>
                        <TableHead>Estado</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {documents.map((doc) => (
                        <TableRow key={doc.id}>
                          <TableCell className="font-mono text-sm">{doc.fullNumber}</TableCell>
                          <TableCell>{DOC_TYPE_LABELS[doc.documentType] ?? doc.documentType}</TableCell>
                          <TableCell>
                            {new Date(doc.issueDate + 'T00:00:00').toLocaleDateString('es-PE')}
                          </TableCell>
                          <TableCell className="max-w-[180px] truncate">{doc.customerName}</TableCell>
                          <TableCell className="text-right font-mono text-sm">
                            {fmt(doc.operacionGravada + doc.operacionExonerada + doc.operacionInafecta, doc.currency)}
                          </TableCell>
                          <TableCell className="text-right font-mono text-sm">
                            {fmt(doc.igv, doc.currency)}
                          </TableCell>
                          <TableCell className="text-right font-mono font-medium">
                            {fmt(doc.total, doc.currency)}
                          </TableCell>
                          <TableCell>
                            <span
                              className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_COLORS[doc.status] ?? 'bg-gray-100 text-gray-600'}`}
                            >
                              {STATUS_LABELS[doc.status] ?? doc.status}
                            </span>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>

                  {/* Subtotals footer */}
                  <div className="border-t bg-muted/30 px-4 py-3 flex justify-end">
                    <div className="space-y-1 text-sm w-64">
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Subtotal (aceptados)</span>
                        <span>{fmt(documents.filter(d => d.status === 'accepted').reduce((s, d) => s + d.operacionGravada, 0))}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">IGV total</span>
                        <span>{fmt(kpis.totalIgv)}</span>
                      </div>
                      <Separator />
                      <div className="flex justify-between font-bold">
                        <span>TOTAL</span>
                        <span>{fmt(kpis.totalVentas)}</span>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </>
      )}

      {!hasFetched && (
        <div className="flex flex-col items-center justify-center py-24 text-muted-foreground gap-3">
          <FileText className="h-12 w-12 opacity-30" />
          <p>Selecciona un rango de fechas y presiona Filtrar para ver el reporte</p>
        </div>
      )}
    </div>
  );
}
