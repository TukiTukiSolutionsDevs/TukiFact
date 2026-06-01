'use client';

import { useState, useMemo } from 'react';
import { api, type DocumentResponse, type PaginatedResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
  CheckCircle2,
  XCircle,
  Ban,
  Clock,
  Download,
  Search,
  Calendar,
  BarChart3,
  Loader2,
  Inbox,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

const fmt = (n: number, c = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency: c }).format(n);

const DOC_TYPE_LABELS: Record<string, string> = {
  '01': 'Factura',
  '03': 'Boleta',
  '07': 'Nota crédito',
  '08': 'Nota débito',
};

type StatusInfo = { label: string; color: string; icon: React.ElementType };

const STATUS: Record<string, StatusInfo> = {
  accepted: { label: 'Aceptado', color: 'var(--success)', icon: CheckCircle2 },
  rejected: { label: 'Rechazado', color: 'var(--danger)', icon: XCircle },
  voided: { label: 'Anulado', color: 'var(--slate-500)', icon: Ban },
  draft: { label: 'Borrador', color: 'var(--slate-500)', icon: FileText },
  sent: { label: 'Enviado', color: 'var(--warning)', icon: Clock },
  signed: { label: 'Firmado', color: 'var(--info)', icon: CheckCircle2 },
};

const statusInfo = (s: string) => STATUS[s] ?? { label: s, color: 'var(--slate-500)', icon: FileText };

const formatDate = (d: Date) => d.toISOString().split('T')[0];

type Preset = { key: string; label: string; from: () => Date; to: () => Date };

const PRESETS: Preset[] = [
  {
    key: 'today',
    label: 'Hoy',
    from: () => new Date(),
    to: () => new Date(),
  },
  {
    key: 'last7',
    label: 'Últimos 7 días',
    from: () => {
      const d = new Date();
      d.setDate(d.getDate() - 6);
      return d;
    },
    to: () => new Date(),
  },
  {
    key: 'month',
    label: 'Este mes',
    from: () => {
      const d = new Date();
      return new Date(d.getFullYear(), d.getMonth(), 1);
    },
    to: () => new Date(),
  },
  {
    key: 'last30',
    label: 'Últimos 30 días',
    from: () => {
      const d = new Date();
      d.setDate(d.getDate() - 29);
      return d;
    },
    to: () => new Date(),
  },
  {
    key: 'prevMonth',
    label: 'Mes pasado',
    from: () => {
      const d = new Date();
      return new Date(d.getFullYear(), d.getMonth() - 1, 1);
    },
    to: () => {
      const d = new Date();
      return new Date(d.getFullYear(), d.getMonth(), 0);
    },
  },
  {
    key: 'year',
    label: 'Este año',
    from: () => {
      const d = new Date();
      return new Date(d.getFullYear(), 0, 1);
    },
    to: () => new Date(),
  },
];

function StatusBadge({ status }: { status: string }) {
  const info = statusInfo(status);
  const Icon = info.icon;
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 t-caption font-semibold whitespace-nowrap"
      style={{
        color: info.color,
        background: `color-mix(in oklch, ${info.color} 14%, transparent)`,
      }}
    >
      <Icon className="h-3 w-3" />
      {info.label}
    </span>
  );
}

function KpiCard({
  label,
  value,
  icon: Icon,
  accent,
  span = 1,
}: {
  label: string;
  value: string;
  icon: React.ElementType;
  accent: string;
  span?: number;
}) {
  const spanClass: Record<number, string> = {
    1: '',
    2: 'sm:col-span-2',
  };
  return (
    <div
      className={cn(
        'rounded-[var(--radius-lg)] border bg-card p-5 flex items-center gap-3.5',
        spanClass[span]
      )}
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      <span
        className="flex h-11 w-11 items-center justify-center rounded-xl shrink-0"
        style={{
          background: `color-mix(in oklch, ${accent} 14%, transparent)`,
          color: accent,
        }}
      >
        <Icon className="h-5 w-5" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
          {label}
        </p>
        <p className="t-num-md mono tnum mt-0.5">{value}</p>
      </div>
    </div>
  );
}

function escapeCsv(value: unknown): string {
  if (value == null) return '';
  const s = String(value);
  if (/[",\n;]/.test(s)) return `"${s.replace(/"/g, '""')}"`;
  return s;
}

function downloadCsv(rows: DocumentResponse[], from: string, to: string) {
  const header = [
    'Numero',
    'Tipo',
    'Fecha emision',
    'Cliente Doc',
    'Cliente Nombre',
    'Moneda',
    'Gravado',
    'Exonerado',
    'Inafecto',
    'IGV',
    'Total',
    'Estado',
  ];
  const lines = rows.map((d) =>
    [
      d.fullNumber,
      DOC_TYPE_LABELS[d.documentType] ?? d.documentType,
      d.issueDate,
      `${d.customerDocType} ${d.customerDocNumber}`,
      d.customerName,
      d.currency,
      d.operacionGravada.toFixed(2),
      d.operacionExonerada.toFixed(2),
      d.operacionInafecta.toFixed(2),
      d.igv.toFixed(2),
      d.total.toFixed(2),
      d.status,
    ]
      .map(escapeCsv)
      .join(',')
  );
  const csv = ['\uFEFF' + header.join(','), ...lines].join('\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `reporte-comprobantes-${from}_${to}.csv`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

export default function ReportsPage() {
  const today = new Date();
  const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);

  const [dateFrom, setDateFrom] = useState(formatDate(firstDay));
  const [dateTo, setDateTo] = useState(formatDate(today));
  const [activePreset, setActivePreset] = useState<string>('month');
  const [documents, setDocuments] = useState<DocumentResponse[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [hasFetched, setHasFetched] = useState(false);

  const applyPreset = (p: Preset) => {
    setDateFrom(formatDate(p.from()));
    setDateTo(formatDate(p.to()));
    setActivePreset(p.key);
  };

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

  const handleExport = () => {
    if (documents.length === 0) {
      toast.error('No hay datos para exportar');
      return;
    }
    downloadCsv(documents, dateFrom, dateTo);
    toast.success(`Exportadas ${documents.length} filas a CSV`);
  };

  const kpis = useMemo(() => {
    const accepted = documents.filter((d) => d.status === 'accepted');
    return {
      totalVentas: accepted.reduce((s, d) => s + d.total, 0),
      totalIgv: accepted.reduce((s, d) => s + d.igv, 0),
      totalGravada: accepted.reduce((s, d) => s + d.operacionGravada, 0),
      totalDocs: documents.length,
      totalAceptados: accepted.length,
      totalAnulados: documents.filter((d) => d.status === 'voided').length,
      totalRechazados: documents.filter((d) => d.status === 'rejected').length,
    };
  }, [documents]);

  const chartData = useMemo(() => {
    const byType: Record<string, { count: number; total: number }> = {};
    for (const doc of documents.filter((d) => d.status === 'accepted')) {
      const label = DOC_TYPE_LABELS[doc.documentType] ?? doc.documentType;
      if (!byType[label]) byType[label] = { count: 0, total: 0 };
      byType[label].count += 1;
      byType[label].total += doc.total;
    }
    return Object.entries(byType).map(([name, v]) => ({
      name,
      Documentos: v.count,
      Total: parseFloat(v.total.toFixed(2)),
    }));
  }, [documents]);

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Reportes</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Análisis de tus comprobantes emitidos por periodo.
          </p>
        </div>
      </div>

      {/* Filters toolbar */}
      <div
        className="rounded-[var(--radius-lg)] border bg-card p-4 mb-[var(--gap-cards)] flex flex-wrap items-end gap-3"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        <div className="flex gap-2 items-end">
          <div>
            <Label className="t-caption block mb-1" style={{ color: 'var(--muted-foreground)' }}>
              Desde
            </Label>
            <div className="relative">
              <Calendar
                className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 pointer-events-none"
                style={{ color: 'var(--muted-foreground)' }}
              />
              <Input
                type="date"
                value={dateFrom}
                onChange={(e) => {
                  setDateFrom(e.target.value);
                  setActivePreset('');
                }}
                className="pl-9 mono w-44"
              />
            </div>
          </div>
          <div>
            <Label className="t-caption block mb-1" style={{ color: 'var(--muted-foreground)' }}>
              Hasta
            </Label>
            <div className="relative">
              <Calendar
                className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 pointer-events-none"
                style={{ color: 'var(--muted-foreground)' }}
              />
              <Input
                type="date"
                value={dateTo}
                onChange={(e) => {
                  setDateTo(e.target.value);
                  setActivePreset('');
                }}
                className="pl-9 mono w-44"
              />
            </div>
          </div>
          <Button onClick={fetchDocuments} disabled={isLoading}>
            {isLoading ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Cargando…
              </>
            ) : (
              <>
                <Search className="h-4 w-4 mr-2" /> Filtrar
              </>
            )}
          </Button>
        </div>

        <div className="flex gap-2 flex-wrap items-center">
          {PRESETS.map((p) => {
            const active = activePreset === p.key;
            return (
              <button
                key={p.key}
                type="button"
                onClick={() => applyPreset(p)}
                className="t-caption font-semibold px-2.5 py-1.5 rounded-full transition-colors"
                style={{
                  background: active
                    ? 'color-mix(in oklch, var(--accent) 18%, transparent)'
                    : 'var(--muted)',
                  color: active ? 'var(--brand-ink)' : 'var(--muted-foreground)',
                  border: `1px solid ${active ? 'var(--accent)' : 'transparent'}`,
                }}
              >
                {p.label}
              </button>
            );
          })}
        </div>

        <Button
          variant="outline"
          disabled={!hasFetched || documents.length === 0}
          onClick={handleExport}
          className="ml-auto"
        >
          <Download className="h-4 w-4 mr-2" />
          Exportar CSV
        </Button>
      </div>

      {!hasFetched && !isLoading && (
        <section
          className="rounded-[var(--radius-lg)] border bg-card p-12 text-center"
          style={{ boxShadow: 'var(--shadow-xs)' }}
        >
          <div
            className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
            style={{ background: 'color-mix(in oklch, var(--accent) 14%, transparent)' }}
          >
            <BarChart3 className="h-8 w-8" style={{ color: 'var(--brand-ink)' }} />
          </div>
          <h2 className="t-h1 m-0">Elige un periodo para generar tu reporte</h2>
          <p
            className="t-body mt-2 mb-0 max-w-[500px] mx-auto"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Usa los rangos rápidos o selecciona fechas manuales y presiona{' '}
            <strong>Filtrar</strong>. Vas a ver tus KPIs, distribución por tipo de comprobante y la
            tabla completa lista para descargar como CSV.
          </p>
        </section>
      )}

      {hasFetched && (
        <>
          {/* KPI Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-6 gap-[var(--gap-cards)] mb-[var(--gap-cards)]">
            <KpiCard
              label="Total ventas (aceptadas)"
              value={fmt(kpis.totalVentas)}
              icon={TrendingUp}
              accent="var(--success)"
              span={2}
            />
            <KpiCard
              label="IGV recaudado"
              value={fmt(kpis.totalIgv)}
              icon={Receipt}
              accent="var(--brand-toucan-orange)"
              span={2}
            />
            <KpiCard
              label="Aceptados"
              value={String(kpis.totalAceptados)}
              icon={CheckCircle2}
              accent="var(--success)"
            />
            <KpiCard
              label="Anulados / rechazados"
              value={`${kpis.totalAnulados} / ${kpis.totalRechazados}`}
              icon={XCircle}
              accent="var(--danger)"
            />
          </div>

          {/* Chart */}
          {chartData.length > 0 && (
            <section
              className="rounded-[var(--radius-lg)] border bg-card p-6 mb-[var(--gap-cards)]"
              style={{ boxShadow: 'var(--shadow-xs)' }}
            >
              <div className="flex items-start justify-between gap-3 mb-4">
                <div>
                  <h2 className="t-h2 m-0">Ventas por tipo de comprobante</h2>
                  <p
                    className="t-body-sm m-0 mt-0.5"
                    style={{ color: 'var(--muted-foreground)' }}
                  >
                    Solo comprobantes aceptados por SUNAT en el periodo.
                  </p>
                </div>
              </div>
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={chartData} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                  <XAxis
                    dataKey="name"
                    tick={{ fontSize: 12, fill: 'var(--muted-foreground)' }}
                    stroke="var(--border)"
                  />
                  <YAxis
                    yAxisId="left"
                    tick={{ fontSize: 11, fill: 'var(--muted-foreground)' }}
                    stroke="var(--border)"
                  />
                  <YAxis
                    yAxisId="right"
                    orientation="right"
                    tick={{ fontSize: 11, fill: 'var(--muted-foreground)' }}
                    stroke="var(--border)"
                  />
                  <Tooltip
                    contentStyle={{
                      background: 'var(--card)',
                      border: '1px solid var(--border)',
                      borderRadius: 'var(--radius-md)',
                      fontSize: 12,
                    }}
                    formatter={(value, name) =>
                      name === 'Total' ? fmt(value as number) : value
                    }
                  />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Bar
                    yAxisId="right"
                    dataKey="Total"
                    fill="var(--brand-toucan-orange)"
                    radius={[4, 4, 0, 0]}
                  />
                  <Bar
                    yAxisId="left"
                    dataKey="Documentos"
                    fill="var(--brand-toucan-yellow)"
                    radius={[4, 4, 0, 0]}
                  />
                </BarChart>
              </ResponsiveContainer>
            </section>
          )}

          {/* Table */}
          <section
            className="rounded-[var(--radius-lg)] border bg-card overflow-hidden mb-[var(--gap-cards)]"
            style={{ boxShadow: 'var(--shadow-xs)' }}
          >
            <div className="flex items-start justify-between gap-3 p-6">
              <div>
                <h2 className="t-h2 m-0">Comprobantes del periodo</h2>
                <p
                  className="t-body-sm m-0 mt-0.5"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  {documents.length} {documents.length === 1 ? 'registro' : 'registros'} entre{' '}
                  <span className="mono tnum">{dateFrom}</span> y{' '}
                  <span className="mono tnum">{dateTo}</span>.
                </p>
              </div>
            </div>

            {documents.length === 0 ? (
              <div className="p-10 text-center">
                <Inbox className="h-10 w-10 mx-auto mb-3" style={{ color: 'var(--slate-400)' }} />
                <p className="t-body m-0 font-semibold">Sin comprobantes en este periodo</p>
                <p
                  className="t-body-sm mt-1 mb-0"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Prueba con otro rango o un preset más amplio.
                </p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr
                      className="t-overline"
                      style={{ color: 'var(--muted-foreground)', background: 'var(--muted)' }}
                    >
                      <th className="text-left py-2.5 pl-6 pr-2">Número</th>
                      <th className="text-left py-2.5 px-2 w-28">Tipo</th>
                      <th className="text-left py-2.5 px-2 w-28">Fecha</th>
                      <th className="text-left py-2.5 px-2">Cliente</th>
                      <th className="text-right py-2.5 px-2 w-32">Base imponible</th>
                      <th className="text-right py-2.5 px-2 w-28">IGV</th>
                      <th className="text-right py-2.5 px-2 w-32">Total</th>
                      <th className="text-left py-2.5 pr-6 pl-2 w-32">Estado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {documents.map((doc) => (
                      <tr key={doc.id} style={{ borderTop: '1px solid var(--border)' }}>
                        <td className="py-3 pl-6 pr-2 mono t-body-sm font-semibold">
                          {doc.fullNumber}
                        </td>
                        <td className="py-3 px-2 t-body-sm">
                          {DOC_TYPE_LABELS[doc.documentType] ?? doc.documentType}
                        </td>
                        <td className="py-3 px-2 mono tnum t-body-sm">
                          {new Date(doc.issueDate + 'T00:00:00').toLocaleDateString('es-PE')}
                        </td>
                        <td className="py-3 px-2 max-w-[280px]">
                          <div className="t-body-sm truncate">{doc.customerName}</div>
                          <div
                            className="t-caption mono"
                            style={{ color: 'var(--muted-foreground)' }}
                          >
                            {doc.customerDocNumber}
                          </div>
                        </td>
                        <td className="py-3 px-2 text-right mono tnum t-body-sm">
                          {fmt(
                            doc.operacionGravada + doc.operacionExonerada + doc.operacionInafecta,
                            doc.currency
                          )}
                        </td>
                        <td className="py-3 px-2 text-right mono tnum t-body-sm">
                          {fmt(doc.igv, doc.currency)}
                        </td>
                        <td className="py-3 px-2 text-right mono tnum t-body-sm font-semibold">
                          {fmt(doc.total, doc.currency)}
                        </td>
                        <td className="py-3 pr-6 pl-2">
                          <StatusBadge status={doc.status} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr style={{ borderTop: '1px solid var(--border)', background: 'var(--muted)' }}>
                      <td colSpan={4} className="py-3 pl-6 pr-2 t-body-sm font-semibold">
                        Totales (solo aceptados)
                      </td>
                      <td className="py-3 px-2 text-right mono tnum t-body-sm">
                        {fmt(kpis.totalGravada)}
                      </td>
                      <td className="py-3 px-2 text-right mono tnum t-body-sm">
                        {fmt(kpis.totalIgv)}
                      </td>
                      <td className="py-3 px-2 text-right mono tnum t-body-sm font-bold">
                        {fmt(kpis.totalVentas)}
                      </td>
                      <td className="py-3 pr-6 pl-2" />
                    </tr>
                  </tfoot>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
