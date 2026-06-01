'use client';

import { useEffect, useState, useCallback } from 'react';
import Link from 'next/link';
import { api, type RecurringInvoiceResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Plus,
  Repeat,
  Pause,
  Play,
  XCircle,
  ChevronLeft,
  ChevronRight,
  Search,
  Loader2,
  Inbox,
  CheckCircle2,
  CalendarClock,
  FileText,
  AlertTriangle,
} from 'lucide-react';
import { toast } from 'sonner';

interface ListResponse {
  items: RecurringInvoiceResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });

const FREQ_LABEL: Record<string, string> = {
  daily: 'Diaria',
  weekly: 'Semanal',
  biweekly: 'Quincenal',
  monthly: 'Mensual',
  yearly: 'Anual',
};

const DOC_TYPE_LABEL: Record<string, string> = { '01': 'Factura', '03': 'Boleta' };

type StatusInfo = { label: string; color: string; icon: React.ElementType };

const STATUS: Record<string, StatusInfo> = {
  active: { label: 'Activa', color: 'var(--success)', icon: CheckCircle2 },
  paused: { label: 'Pausada', color: 'var(--warning)', icon: Pause },
  cancelled: { label: 'Cancelada', color: 'var(--danger)', icon: XCircle },
  completed: { label: 'Completada', color: 'var(--info)', icon: CheckCircle2 },
};

const statusInfo = (s: string) =>
  STATUS[s] ?? { label: s, color: 'var(--slate-500)', icon: FileText };

const STATUS_FILTERS: { value: string; label: string }[] = [
  { value: '', label: 'Todas' },
  { value: 'active', label: 'Activas' },
  { value: 'paused', label: 'Pausadas' },
  { value: 'completed', label: 'Completadas' },
  { value: 'cancelled', label: 'Canceladas' },
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

export default function RecurringInvoicesPage() {
  const [data, setData] = useState<ListResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '15' });
      if (statusFilter) params.set('status', statusFilter);
      const res = await api.get<ListResponse>(`/v1/recurring-invoices?${params}`);
      setData(res);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, statusFilter]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const updateStatus = async (id: string, status: string) => {
    try {
      await api.put(`/v1/recurring-invoices/${id}`, { status });
      toast.success('Estado actualizado');
      fetchData();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  const visibleItems = (() => {
    if (!data) return [];
    if (!search.trim()) return data.items;
    const q = search.toLowerCase();
    return data.items.filter(
      (r) =>
        r.customerName.toLowerCase().includes(q) ||
        r.customerDocNumber.includes(q) ||
        r.serie.toLowerCase().includes(q)
    );
  })();

  const totalPages = data ? Math.ceil(data.totalCount / 15) : 0;
  const hasAnyFilters = statusFilter !== '' || search !== '';
  const clearFilters = () => {
    setStatusFilter('');
    setSearch('');
    setPage(1);
  };

  const hasNoResults = !isLoading && data?.items.length === 0 && !hasAnyFilters;
  const hasFilteredNoResults =
    !isLoading && (data?.items.length === 0 || visibleItems.length === 0) && hasAnyFilters;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Facturación recurrente</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            {data
              ? `${data.totalCount} ${data.totalCount === 1 ? 'programación' : 'programaciones'} en tu cuenta.`
              : 'Cargando…'}
          </p>
        </div>
        <Button
          asChild
          style={{ background: 'var(--accent)', color: 'var(--accent-foreground)', fontWeight: 600 }}
        >
          <Link href="/recurring-invoices/new">
            <Plus className="h-4 w-4 mr-2" /> Nueva recurrente
          </Link>
        </Button>
      </div>

      {/* Filters toolbar */}
      <div
        className="rounded-[var(--radius-lg)] border bg-card p-4 mb-[var(--gap-cards)] flex flex-wrap items-end gap-3"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        <div className="flex-1 min-w-[200px]">
          <label className="t-caption block mb-1" style={{ color: 'var(--muted-foreground)' }}>
            Buscar
          </label>
          <div className="relative">
            <Search
              className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
              style={{ color: 'var(--muted-foreground)' }}
            />
            <Input
              placeholder="Cliente, RUC/DNI, serie…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9"
            />
          </div>
        </div>
        <div className="flex gap-2 flex-wrap">
          {STATUS_FILTERS.map((f) => {
            const active = statusFilter === f.value;
            return (
              <button
                key={f.value || 'all'}
                type="button"
                onClick={() => {
                  setStatusFilter(f.value);
                  setPage(1);
                }}
                className="t-caption font-semibold px-2.5 py-1.5 rounded-full transition-colors"
                style={{
                  background: active
                    ? 'color-mix(in oklch, var(--accent) 18%, transparent)'
                    : 'var(--muted)',
                  color: active ? 'var(--brand-ink)' : 'var(--muted-foreground)',
                  border: `1px solid ${active ? 'var(--accent)' : 'transparent'}`,
                }}
              >
                {f.label}
              </button>
            );
          })}
          {hasAnyFilters && (
            <Button variant="ghost" size="sm" onClick={clearFilters}>
              Limpiar
            </Button>
          )}
        </div>
      </div>

      {/* Table */}
      <section
        className="rounded-[var(--radius-lg)] border bg-card overflow-hidden mb-[var(--gap-cards)]"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        {isLoading ? (
          <div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span className="t-body-sm">Cargando programaciones…</span>
          </div>
        ) : hasNoResults ? (
          <div className="p-10 text-center">
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{ background: 'color-mix(in oklch, var(--accent) 14%, transparent)' }}
            >
              <Repeat className="h-8 w-8" style={{ color: 'var(--brand-ink)' }} />
            </div>
            <h2 className="t-h1 m-0">Aún no tienes facturación recurrente</h2>
            <p
              className="t-body mt-2 mb-4 max-w-[440px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Programa la emisión automática de comprobantes a tus clientes habituales. Tú
              configuras la frecuencia y nosotros emitimos el comprobante en SUNAT cada vez.
            </p>
            <Button asChild>
              <Link href="/recurring-invoices/new">
                <Plus className="h-4 w-4 mr-2" /> Nueva recurrente
              </Link>
            </Button>
          </div>
        ) : hasFilteredNoResults ? (
          <div className="p-10 text-center">
            <Inbox className="h-10 w-10 mx-auto mb-3" style={{ color: 'var(--slate-400)' }} />
            <p className="t-body m-0 font-semibold">Sin resultados con esos filtros</p>
            <p className="t-body-sm mt-1 mb-4" style={{ color: 'var(--muted-foreground)' }}>
              Prueba con otro estado o limpia los filtros.
            </p>
            <Button variant="outline" onClick={clearFilters}>
              Limpiar filtros
            </Button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr
                  className="t-overline"
                  style={{ color: 'var(--muted-foreground)', background: 'var(--muted)' }}
                >
                  <th className="text-left py-2.5 pl-6 pr-2 w-24">Tipo</th>
                  <th className="text-left py-2.5 px-2 w-20">Serie</th>
                  <th className="text-left py-2.5 px-2">Cliente</th>
                  <th className="text-left py-2.5 px-2 w-28">Frecuencia</th>
                  <th className="text-left py-2.5 px-2 w-36">Próxima emisión</th>
                  <th className="text-right py-2.5 px-2 w-20">Emitidas</th>
                  <th className="text-left py-2.5 px-2 w-32">Estado</th>
                  <th className="text-right py-2.5 pr-6 pl-2 w-28">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {visibleItems.map((r) => (
                  <tr key={r.id} style={{ borderTop: '1px solid var(--border)' }}>
                    <td className="py-3 pl-6 pr-2">
                      <span
                        className="inline-flex items-center gap-1.5 rounded-md px-2 py-0.5 t-caption font-semibold"
                        style={{
                          background: 'var(--muted)',
                          color: 'var(--muted-foreground)',
                        }}
                      >
                        {DOC_TYPE_LABEL[r.documentType] ?? r.documentType}
                      </span>
                    </td>
                    <td className="py-3 px-2 mono t-body-sm font-semibold">{r.serie}</td>
                    <td className="py-3 px-2 max-w-[260px]">
                      <div className="t-body-sm truncate">{r.customerName}</div>
                      <div
                        className="t-caption mono"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        {r.customerDocType === '6' ? 'RUC' : 'DNI'} {r.customerDocNumber}
                      </div>
                      {r.lastError && r.consecutiveFailures > 0 && (
                        <div
                          className="mt-1 inline-flex items-center gap-1 t-caption font-medium"
                          style={{ color: 'var(--danger)' }}
                          title={r.lastError}
                        >
                          <AlertTriangle className="h-3 w-3" />
                          Último intento falló
                          {r.consecutiveFailures > 1 && ` (${r.consecutiveFailures} seguidos)`}
                        </div>
                      )}
                    </td>
                    <td className="py-3 px-2 t-body-sm">
                      {FREQ_LABEL[r.frequency] ?? r.frequency}
                    </td>
                    <td className="py-3 px-2">
                      {r.nextEmissionDate ? (
                        <span className="inline-flex items-center gap-1.5 mono tnum t-body-sm">
                          <CalendarClock
                            className="h-3.5 w-3.5"
                            style={{ color: 'var(--muted-foreground)' }}
                          />
                          {formatDate(r.nextEmissionDate)}
                        </span>
                      ) : (
                        <span style={{ color: 'var(--muted-foreground)' }}>—</span>
                      )}
                    </td>
                    <td className="py-3 px-2 text-right mono tnum t-body-sm font-semibold">
                      {r.emittedCount}
                    </td>
                    <td className="py-3 px-2">
                      <StatusBadge status={r.status} />
                    </td>
                    <td className="py-3 pr-6 pl-2">
                      <div className="flex gap-1 justify-end">
                        {r.status === 'active' && (
                          <Button
                            variant="ghost"
                            size="sm"
                            title="Pausar"
                            onClick={() => updateStatus(r.id, 'paused')}
                          >
                            <Pause className="h-4 w-4" />
                          </Button>
                        )}
                        {r.status === 'paused' && (
                          <Button
                            variant="ghost"
                            size="sm"
                            title="Reanudar"
                            onClick={() => updateStatus(r.id, 'active')}
                          >
                            <Play className="h-4 w-4" />
                          </Button>
                        )}
                        {r.status !== 'cancelled' && r.status !== 'completed' && (
                          <Button
                            variant="ghost"
                            size="sm"
                            title="Cancelar"
                            onClick={() => updateStatus(r.id, 'cancelled')}
                          >
                            <XCircle
                              className="h-4 w-4"
                              style={{ color: 'var(--danger)' }}
                            />
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* Pagination */}
      {data && totalPages > 1 && (
        <div className="flex items-center justify-between flex-wrap gap-3">
          <p className="t-body-sm" style={{ color: 'var(--muted-foreground)' }}>
            Página <span className="mono tnum font-semibold">{page}</span> de{' '}
            <span className="mono tnum font-semibold">{totalPages}</span> ·{' '}
            <span className="mono tnum">{data.totalCount}</span> total
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              <ChevronLeft className="h-4 w-4 mr-1" /> Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Siguiente <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
