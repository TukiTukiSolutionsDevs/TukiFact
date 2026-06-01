'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { api, type PerceptionResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Plus,
  ShieldAlert,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Inbox,
  Filter,
} from 'lucide-react';

const fmt = (amount: number, currency = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency }).format(amount);

const fmtDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });

interface StatusInfo {
  label: string;
  fg: string;
  bg: string;
}

const STATUS_MAP: Record<string, StatusInfo> = {
  accepted: {
    label: 'Aceptado',
    fg: 'var(--success)',
    bg: 'color-mix(in oklch, var(--success) 14%, transparent)',
  },
  rejected: {
    label: 'Rechazado',
    fg: 'var(--danger)',
    bg: 'color-mix(in oklch, var(--danger) 14%, transparent)',
  },
  sent: {
    label: 'Enviado',
    fg: 'var(--info)',
    bg: 'color-mix(in oklch, var(--info) 14%, transparent)',
  },
  signed: {
    label: 'Firmado',
    fg: 'var(--info)',
    bg: 'color-mix(in oklch, var(--info) 14%, transparent)',
  },
  draft: {
    label: 'Borrador',
    fg: 'var(--muted-foreground)',
    bg: 'var(--muted)',
  },
};

function StatusPill({ status }: { status: string }) {
  const info = STATUS_MAP[status] ?? {
    label: status,
    fg: 'var(--muted-foreground)',
    bg: 'var(--muted)',
  };
  return (
    <span
      className="inline-flex items-center rounded-full px-2.5 py-0.5 t-caption font-semibold"
      style={{ background: info.bg, color: info.fg }}
    >
      {info.label}
    </span>
  );
}

interface ListResponse {
  items: PerceptionResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const PAGE_SIZE = 15;

export default function PerceptionsPage() {
  const router = useRouter();
  const [data, setData] = useState<ListResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('all');

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: String(PAGE_SIZE),
      });
      if (statusFilter !== 'all') params.set('status', statusFilter);
      const res = await api.get<ListResponse>(`/v1/perceptions?${params}`);
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

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 0;
  const hasFilter = statusFilter !== 'all';
  const hasNoResults = !isLoading && !data?.items.length && !hasFilter;
  const hasFilteredNoResults = !isLoading && !data?.items.length && hasFilter;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Percepciones</h1>
          <p
            className="t-body mt-1.5 mb-0"
            style={{ color: 'var(--muted-foreground)' }}
          >
            {data
              ? data.totalCount === 1
                ? '1 comprobante de percepción emitido.'
                : `${data.totalCount} comprobantes de percepción emitidos.`
              : 'Cargando…'}
          </p>
        </div>
        <Button
          onClick={() => router.push('/perceptions/new')}
          style={{
            background: 'var(--accent)',
            color: 'var(--accent-foreground)',
            fontWeight: 600,
          }}
        >
          <Plus className="h-4 w-4 mr-2" /> Nueva percepción
        </Button>
      </div>

      {/* Filter toolbar */}
      <div
        className="rounded-[var(--radius-lg)] border bg-card p-4 mb-[var(--gap-cards)] flex items-center gap-3 flex-wrap"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        <div className="flex items-center gap-2">
          <Filter
            className="h-4 w-4"
            style={{ color: 'var(--muted-foreground)' }}
          />
          <span
            className="t-body-sm font-medium"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Estado
          </span>
        </div>
        <Select
          value={statusFilter}
          onValueChange={(v) => {
            if (!v) return;
            setStatusFilter(v);
            setPage(1);
          }}
        >
          <SelectTrigger className="w-[200px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los estados</SelectItem>
            <SelectItem value="accepted">Aceptado</SelectItem>
            <SelectItem value="rejected">Rechazado</SelectItem>
            <SelectItem value="sent">Enviado</SelectItem>
            <SelectItem value="signed">Firmado</SelectItem>
            <SelectItem value="draft">Borrador</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Table / empty / loading */}
      <section
        className="rounded-[var(--radius-lg)] border bg-card overflow-hidden mb-[var(--gap-cards)]"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        {isLoading ? (
          <div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span className="t-body-sm">Cargando percepciones…</span>
          </div>
        ) : hasNoResults ? (
          <div className="p-10 text-center">
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{
                background: 'color-mix(in oklch, var(--accent) 14%, transparent)',
              }}
            >
              <ShieldAlert
                className="h-8 w-8"
                style={{ color: 'var(--brand-ink)' }}
              />
            </div>
            <h2 className="t-h1 m-0">Aún no emitiste percepciones</h2>
            <p
              className="t-body mt-2 mb-4 max-w-[480px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Los comprobantes de percepción se emiten cuando vendes a clientes
              sujetos al régimen de percepciones SUNAT. Se asocian a una factura
              o boleta original.
            </p>
            <Button
              onClick={() => router.push('/perceptions/new')}
              style={{
                background: 'var(--accent)',
                color: 'var(--accent-foreground)',
                fontWeight: 600,
              }}
            >
              <Plus className="h-4 w-4 mr-2" /> Emitir primera percepción
            </Button>
          </div>
        ) : hasFilteredNoResults ? (
          <div className="p-10 text-center">
            <Inbox
              className="h-10 w-10 mx-auto mb-3"
              style={{ color: 'var(--slate-400)' }}
            />
            <p className="t-body m-0 font-semibold">
              Sin percepciones con ese estado
            </p>
            <p
              className="t-body-sm mt-1 mb-4"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Prueba quitando el filtro.
            </p>
            <Button variant="outline" onClick={() => setStatusFilter('all')}>
              Ver todos los estados
            </Button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr
                  className="t-overline"
                  style={{
                    color: 'var(--muted-foreground)',
                    background: 'var(--muted)',
                  }}
                >
                  <th className="text-left py-2.5 pl-6 pr-2 w-44">Número</th>
                  <th className="text-left py-2.5 px-2 w-28">Fecha</th>
                  <th className="text-left py-2.5 px-2">Cliente</th>
                  <th className="text-left py-2.5 px-2 w-24">Régimen</th>
                  <th className="text-right py-2.5 px-2 w-40">Total percibido</th>
                  <th className="text-left py-2.5 pr-6 pl-2 w-28">Estado</th>
                </tr>
              </thead>
              <tbody>
                {data!.items.map((p) => (
                  <tr
                    key={p.id}
                    className="cursor-pointer transition-colors hover:bg-[var(--muted)]"
                    style={{ borderTop: '1px solid var(--border)' }}
                    onClick={() => router.push(`/perceptions/${p.id}`)}
                  >
                    <td className="py-3 pl-6 pr-2 mono t-body-sm font-semibold">
                      {p.fullNumber}
                    </td>
                    <td
                      className="py-3 px-2 t-body-sm mono tnum"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {fmtDate(p.issueDate)}
                    </td>
                    <td className="py-3 px-2 max-w-[340px] t-body-sm truncate">
                      {p.customerName}
                    </td>
                    <td className="py-3 px-2">
                      <span
                        className="inline-flex items-center rounded-md px-1.5 py-0.5 t-caption font-semibold mono tnum"
                        style={{
                          background: 'var(--muted)',
                          color: 'var(--muted-foreground)',
                        }}
                      >
                        {p.perceptionPercent}%
                      </span>
                    </td>
                    <td className="py-3 px-2 text-right mono tnum t-body-sm font-semibold">
                      {fmt(p.totalPerceived, p.currency)}
                    </td>
                    <td className="py-3 pr-6 pl-2">
                      <StatusPill status={p.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {totalPages > 1 && (
        <div className="flex items-center justify-between flex-wrap gap-3">
          <p
            className="t-body-sm"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Página <span className="mono tnum font-semibold">{page}</span> de{' '}
            <span className="mono tnum font-semibold">{totalPages}</span> ·{' '}
            <span className="mono tnum">{data!.totalCount}</span> total
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
