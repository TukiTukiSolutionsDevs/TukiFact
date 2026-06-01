'use client';

import { useEffect, useState, useCallback } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { api, type DespatchAdviceResponse, type PaginatedResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Plus,
  Truck,
  Bus,
  ChevronLeft,
  ChevronRight,
  ChevronRight as ChevronRightSmall,
  Search,
  Calendar,
  CheckCircle2,
  Clock,
  XCircle,
  FileText,
  Inbox,
  Loader2,
  ArrowRight,
} from 'lucide-react';

const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });

type StatusInfo = { label: string; color: string; icon: React.ElementType };

const STATUS: Record<string, StatusInfo> = {
  accepted: { label: 'Aceptada', color: 'var(--success)', icon: CheckCircle2 },
  rejected: { label: 'Rechazada', color: 'var(--danger)', icon: XCircle },
  draft: { label: 'Borrador', color: 'var(--slate-500)', icon: FileText },
  signed: { label: 'Firmada', color: 'var(--info)', icon: CheckCircle2 },
  sent: { label: 'Enviada · pendiente CDR', color: 'var(--warning)', icon: Clock },
  pending_ticket: { label: 'Pendiente ticket', color: 'var(--warning)', icon: Clock },
  cancelled: { label: 'Anulada', color: 'var(--slate-500)', icon: XCircle },
};

const statusInfo = (s: string) => STATUS[s] ?? { label: s, color: 'var(--slate-500)', icon: FileText };

const STATUS_FILTERS: { value: string; label: string }[] = [
  { value: '', label: 'Todas' },
  { value: 'draft', label: 'Borradores' },
  { value: 'sent', label: 'Enviadas · pendientes CDR' },
  { value: 'accepted', label: 'Aceptadas' },
  { value: 'rejected', label: 'Rechazadas' },
  { value: 'cancelled', label: 'Anuladas' },
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

export default function DespatchAdvicesPage() {
  const router = useRouter();
  const [data, setData] = useState<PaginatedResponse<DespatchAdviceResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [search, setSearch] = useState('');

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '15' });
      if (statusFilter) params.set('status', statusFilter);
      if (dateFrom) params.set('dateFrom', dateFrom);
      if (dateTo) params.set('dateTo', dateTo);
      const res = await api.get<PaginatedResponse<DespatchAdviceResponse>>(
        `/v1/despatch-advices?${params}`
      );
      setData(res);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, statusFilter, dateFrom, dateTo]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Client-side search filter on top of the server-side results.
  const visibleItems = (() => {
    if (!data) return [];
    if (!search.trim()) return data.data;
    const q = search.toLowerCase();
    return data.data.filter(
      (g) =>
        g.fullNumber.toLowerCase().includes(q) ||
        g.recipientName.toLowerCase().includes(q) ||
        g.recipientDocNumber.includes(q) ||
        g.destinationUbigeo.includes(q) ||
        g.originUbigeo.includes(q)
    );
  })();

  const hasAnyFilters = statusFilter !== '' || dateFrom !== '' || dateTo !== '' || search !== '';
  const clearFilters = () => {
    setStatusFilter('');
    setDateFrom('');
    setDateTo('');
    setSearch('');
    setPage(1);
  };

  const hasNoResults = !isLoading && data?.data.length === 0 && !hasAnyFilters;
  const hasFilteredNoResults =
    !isLoading && (data?.data.length === 0 || visibleItems.length === 0) && hasAnyFilters;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Guías de remisión</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            {data
              ? `${data.pagination.totalCount} ${data.pagination.totalCount === 1 ? 'guía' : 'guías'} en tu cuenta.`
              : 'Cargando…'}
          </p>
        </div>
        <Button
          asChild
          style={{ background: 'var(--accent)', color: 'var(--accent-foreground)', fontWeight: 600 }}
        >
          <Link href="/despatch-advices/new">
            <Plus className="h-4 w-4 mr-2" /> Nueva guía
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
              placeholder="Número, destinatario, RUC, ubigeo…"
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
        </div>
        <div className="flex gap-2 items-end">
          <div>
            <label className="t-caption block mb-1" style={{ color: 'var(--muted-foreground)' }}>
              Desde
            </label>
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
                  setPage(1);
                }}
                className="pl-9 mono"
              />
            </div>
          </div>
          <div>
            <label className="t-caption block mb-1" style={{ color: 'var(--muted-foreground)' }}>
              Hasta
            </label>
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
                  setPage(1);
                }}
                className="pl-9 mono"
              />
            </div>
          </div>
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
            <span className="t-body-sm">Cargando guías…</span>
          </div>
        ) : hasNoResults ? (
          <div className="p-10 text-center">
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{ background: 'color-mix(in oklch, var(--accent) 14%, transparent)' }}
            >
              <Truck className="h-8 w-8" style={{ color: 'var(--brand-ink)' }} />
            </div>
            <h2 className="t-h1 m-0">Aún no has emitido guías</h2>
            <p
              className="t-body mt-2 mb-4 max-w-[420px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Las guías de remisión documentan el traslado de mercadería ante SUNAT. Emite tu primera
              para empezar a registrar tus despachos.
            </p>
            <Button asChild>
              <Link href="/despatch-advices/new">
                <Plus className="h-4 w-4 mr-2" /> Nueva guía
              </Link>
            </Button>
          </div>
        ) : hasFilteredNoResults ? (
          <div className="p-10 text-center">
            <Inbox className="h-10 w-10 mx-auto mb-3" style={{ color: 'var(--slate-400)' }} />
            <p className="t-body m-0 font-semibold">Sin resultados con esos filtros</p>
            <p className="t-body-sm mt-1 mb-4" style={{ color: 'var(--muted-foreground)' }}>
              Prueba con otra fecha o limpia los filtros.
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
                  <th className="text-left py-2.5 pl-6 pr-2">Número</th>
                  <th className="text-left py-2.5 px-2 w-28">Fecha</th>
                  <th className="text-left py-2.5 px-2">Destinatario</th>
                  <th className="text-left py-2.5 px-2 w-32">Modalidad</th>
                  <th className="text-left py-2.5 px-2 w-44">Trayecto</th>
                  <th className="text-left py-2.5 pr-6 pl-2 w-44">Estado</th>
                </tr>
              </thead>
              <tbody>
                {visibleItems.map((g, i) => {
                  const TransportIcon = g.transportMode === '02' ? Truck : Bus;
                  return (
                    <tr
                      key={g.id}
                      className="cursor-pointer hover:bg-[var(--muted)] transition-colors"
                      style={{ borderTop: i > 0 ? '1px solid var(--border)' : '1px solid var(--border)' }}
                      onClick={() => router.push(`/despatch-advices/${g.id}`)}
                    >
                      <td className="py-3 pl-6 pr-2">
                        <div className="mono t-body-sm font-semibold">{g.fullNumber}</div>
                        <div
                          className="t-caption mono"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          {g.documentType === '09' ? 'GRE Remitente' : 'GRE Transportista'}
                        </div>
                      </td>
                      <td className="py-3 px-2 mono t-body-sm">{formatDate(g.issueDate)}</td>
                      <td className="py-3 px-2 max-w-[200px]">
                        <div className="t-body-sm truncate">{g.recipientName}</div>
                        <div
                          className="t-caption mono"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          {g.recipientDocType === '6' ? 'RUC' : 'DNI'} {g.recipientDocNumber}
                        </div>
                      </td>
                      <td className="py-3 px-2">
                        <span
                          className="inline-flex items-center gap-1.5 t-caption font-medium"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          <TransportIcon className="h-3.5 w-3.5" />
                          {g.transportMode === '02' ? 'Privado' : 'Público'}
                        </span>
                      </td>
                      <td className="py-3 px-2">
                        <span className="inline-flex items-center gap-1 mono t-caption">
                          <span style={{ color: 'var(--info)' }}>{g.originUbigeo}</span>
                          <ArrowRight
                            className="h-3 w-3"
                            style={{ color: 'var(--muted-foreground)' }}
                          />
                          <span style={{ color: 'var(--success)' }}>{g.destinationUbigeo}</span>
                        </span>
                      </td>
                      <td className="py-3 pr-6 pl-2">
                        <div className="flex items-center justify-between gap-2">
                          <StatusBadge status={g.status} />
                          <ChevronRightSmall
                            className="h-4 w-4 shrink-0"
                            style={{ color: 'var(--muted-foreground)' }}
                          />
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* Pagination */}
      {data && data.pagination.totalPages > 1 && (
        <div className="flex items-center justify-between flex-wrap gap-3">
          <p className="t-body-sm" style={{ color: 'var(--muted-foreground)' }}>
            Página{' '}
            <span className="mono tnum font-semibold">{data.pagination.page}</span> de{' '}
            <span className="mono tnum font-semibold">{data.pagination.totalPages}</span> ·{' '}
            <span className="mono tnum">{data.pagination.totalCount}</span> total
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
              disabled={page >= data.pagination.totalPages}
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
