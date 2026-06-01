'use client';

import { useEffect, useState, useCallback } from 'react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Ban,
  CheckCircle2,
  XCircle,
  Clock,
  AlertTriangle,
  Search,
  Loader2,
  Inbox,
  Hash,
  RefreshCw,
} from 'lucide-react';

interface VoidedDoc {
  id: string;
  ticketNumber: string;
  status: string;
  sunatTicket: string | null;
  sunatResponseCode: string | null;
  sunatResponseDescription: string | null;
  createdAt: string;
}

type StatusInfo = { label: string; color: string; icon: React.ElementType };

const STATUS: Record<string, StatusInfo> = {
  pending: { label: 'Pendiente ticket', color: 'var(--warning)', icon: Clock },
  accepted: { label: 'Aceptada', color: 'var(--success)', icon: CheckCircle2 },
  rejected: { label: 'Rechazada', color: 'var(--danger)', icon: XCircle },
  error: { label: 'Error', color: 'var(--danger)', icon: AlertTriangle },
  sent: { label: 'Enviada', color: 'var(--info)', icon: Clock },
};

const statusInfo = (s: string) => STATUS[s] ?? { label: s, color: 'var(--slate-500)', icon: Ban };

const STATUS_FILTERS: { value: string; label: string }[] = [
  { value: '', label: 'Todas' },
  { value: 'pending', label: 'Pendientes ticket' },
  { value: 'accepted', label: 'Aceptadas' },
  { value: 'rejected', label: 'Rechazadas' },
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

const formatDateTime = (iso: string) =>
  new Date(iso).toLocaleString('es-PE', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

export default function VoidedPage() {
  const [items, setItems] = useState<VoidedDoc[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');

  const fetchData = useCallback(async (silent = false) => {
    if (!silent) setIsLoading(true);
    else setIsRefreshing(true);
    try {
      const res = await api.get<VoidedDoc[]>('/v1/voided-documents');
      setItems(res);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Auto-refresh while any communication is still pending the SUNAT ticket response.
  const hasPending = items.some((v) => v.status === 'pending' || v.status === 'sent');
  useEffect(() => {
    if (!hasPending) return;
    const interval = setInterval(() => fetchData(true), 30_000);
    return () => clearInterval(interval);
  }, [hasPending, fetchData]);

  const visibleItems = items.filter((v) => {
    if (statusFilter && v.status !== statusFilter) return false;
    if (search.trim()) {
      const q = search.toLowerCase();
      const haystack = [
        v.ticketNumber,
        v.sunatTicket ?? '',
        v.sunatResponseDescription ?? '',
      ]
        .join(' ')
        .toLowerCase();
      if (!haystack.includes(q)) return false;
    }
    return true;
  });

  const hasAnyFilters = statusFilter !== '' || search !== '';
  const clearFilters = () => {
    setStatusFilter('');
    setSearch('');
  };

  const hasNoResults = !isLoading && items.length === 0;
  const hasFilteredNoResults = !isLoading && items.length > 0 && visibleItems.length === 0;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Comunicaciones de baja</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Histórico de anulaciones presentadas a SUNAT.{' '}
            {hasPending && (
              <span
                className="inline-flex items-center gap-1 ml-1"
                style={{ color: 'var(--warning)' }}
              >
                <RefreshCw className={isRefreshing ? 'h-3 w-3 animate-spin' : 'h-3 w-3'} />
                actualizando cada 30s mientras hay pendientes
              </span>
            )}
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => fetchData(true)}
          disabled={isLoading || isRefreshing}
        >
          {isRefreshing ? (
            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
          ) : (
            <RefreshCw className="h-4 w-4 mr-2" />
          )}
          Actualizar
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
              placeholder="Ticket interno, ticket SUNAT, descripción…"
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
                onClick={() => setStatusFilter(f.value)}
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
            <span className="t-body-sm">Cargando comunicaciones…</span>
          </div>
        ) : hasNoResults ? (
          <div className="p-10 text-center">
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{ background: 'color-mix(in oklch, var(--accent) 14%, transparent)' }}
            >
              <Ban className="h-8 w-8" style={{ color: 'var(--brand-ink)' }} />
            </div>
            <h2 className="t-h1 m-0">Aún no has anulado comprobantes</h2>
            <p
              className="t-body mt-2 mb-0 max-w-[440px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Cuando anules una factura o boleta desde su detalle, la comunicación de baja a SUNAT
              quedará registrada acá con su ticket y respuesta.
            </p>
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
                  <th className="text-left py-2.5 pl-6 pr-2 w-44">Ticket interno</th>
                  <th className="text-left py-2.5 px-2 w-44">Estado</th>
                  <th className="text-left py-2.5 px-2 w-44">Ticket SUNAT</th>
                  <th className="text-left py-2.5 px-2">Respuesta</th>
                  <th className="text-left py-2.5 pr-6 pl-2 w-44">Fecha</th>
                </tr>
              </thead>
              <tbody>
                {visibleItems.map((v) => (
                  <tr key={v.id} style={{ borderTop: '1px solid var(--border)' }}>
                    <td className="py-3 pl-6 pr-2">
                      <span className="mono t-body-sm font-semibold inline-flex items-center gap-1.5">
                        <Hash
                          className="h-3 w-3"
                          style={{ color: 'var(--muted-foreground)' }}
                        />
                        {v.ticketNumber}
                      </span>
                    </td>
                    <td className="py-3 px-2">
                      <StatusBadge status={v.status} />
                    </td>
                    <td className="py-3 px-2 mono t-caption" style={{ color: 'var(--muted-foreground)' }}>
                      {v.sunatTicket || '—'}
                    </td>
                    <td className="py-3 px-2 max-w-[420px]">
                      {v.sunatResponseCode || v.sunatResponseDescription ? (
                        <div className="t-body-sm">
                          {v.sunatResponseCode && (
                            <span
                              className="mono mr-2 t-caption font-semibold"
                              style={{ color: 'var(--muted-foreground)' }}
                            >
                              {v.sunatResponseCode}
                            </span>
                          )}
                          <span className="truncate">
                            {v.sunatResponseDescription || 'Sin descripción'}
                          </span>
                        </div>
                      ) : (
                        <span style={{ color: 'var(--muted-foreground)' }}>—</span>
                      )}
                    </td>
                    <td className="py-3 pr-6 pl-2 mono tnum t-body-sm">
                      {formatDateTime(v.createdAt)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
