'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import {
  api,
  type DocumentResponse,
  type PaginatedResponse,
} from '@/lib/api';
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
  FileText,
  Download,
  Eye,
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
  voided: {
    label: 'Anulado',
    fg: 'var(--muted-foreground)',
    bg: 'var(--muted)',
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

const DOC_TYPE_LABEL: Record<string, string> = {
  '01': 'Factura',
  '03': 'Boleta',
  '07': 'NC',
  '08': 'ND',
};

const PAGE_SIZE = 15;

export default function DocumentsPage() {
  const router = useRouter();
  const [data, setData] = useState<PaginatedResponse<DocumentResponse> | null>(
    null
  );
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [documentType, setDocumentType] = useState('all');
  const [status, setStatus] = useState('all');

  const fetchDocuments = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: String(PAGE_SIZE),
      });
      if (documentType !== 'all') params.set('documentType', documentType);
      if (status !== 'all') params.set('status', status);
      const res = await api.get<PaginatedResponse<DocumentResponse>>(
        `/v1/documents?${params}`
      );
      setData(res);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, documentType, status]);

  useEffect(() => {
    fetchDocuments();
  }, [fetchDocuments]);

  const downloadPdf = async (id: string, fullNumber: string) => {
    const token = api.getToken();
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL || ''}/v1/documents/${id}/pdf`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${fullNumber}.pdf`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const hasFilter = documentType !== 'all' || status !== 'all';
  const hasNoResults =
    !isLoading && data?.data.length === 0 && !hasFilter;
  const hasFilteredNoResults =
    !isLoading && data?.data.length === 0 && hasFilter;

  const clearFilters = () => {
    setDocumentType('all');
    setStatus('all');
    setPage(1);
  };

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Comprobantes</h1>
          <p
            className="t-body mt-1.5 mb-0"
            style={{ color: 'var(--muted-foreground)' }}
          >
            {data
              ? data.pagination.totalCount === 1
                ? '1 comprobante emitido.'
                : `${data.pagination.totalCount} comprobantes emitidos.`
              : 'Cargando…'}
          </p>
        </div>
        <Button
          onClick={() => router.push('/documents/new')}
          style={{
            background: 'var(--accent)',
            color: 'var(--accent-foreground)',
            fontWeight: 600,
          }}
        >
          <Plus className="h-4 w-4 mr-2" /> Emitir comprobante
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
            Filtros
          </span>
        </div>
        <Select
          value={documentType}
          onValueChange={(v) => {
            if (v == null) return;
            setDocumentType(v);
            setPage(1);
          }}
        >
          <SelectTrigger className="w-[180px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los tipos</SelectItem>
            <SelectItem value="01">Factura</SelectItem>
            <SelectItem value="03">Boleta</SelectItem>
            <SelectItem value="07">Nota de crédito</SelectItem>
            <SelectItem value="08">Nota de débito</SelectItem>
          </SelectContent>
        </Select>
        <Select
          value={status}
          onValueChange={(v) => {
            if (v == null) return;
            setStatus(v);
            setPage(1);
          }}
        >
          <SelectTrigger className="w-[180px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los estados</SelectItem>
            <SelectItem value="accepted">Aceptado</SelectItem>
            <SelectItem value="rejected">Rechazado</SelectItem>
            <SelectItem value="voided">Anulado</SelectItem>
            <SelectItem value="sent">Enviado</SelectItem>
            <SelectItem value="signed">Firmado</SelectItem>
            <SelectItem value="draft">Borrador</SelectItem>
          </SelectContent>
        </Select>
        {hasFilter && (
          <Button variant="ghost" size="sm" onClick={clearFilters}>
            Limpiar
          </Button>
        )}
      </div>

      {/* Table / states */}
      <section
        className="rounded-[var(--radius-lg)] border bg-card overflow-hidden mb-[var(--gap-cards)]"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        {isLoading ? (
          <div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span className="t-body-sm">Cargando comprobantes…</span>
          </div>
        ) : hasNoResults ? (
          <div className="p-10 text-center">
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{
                background: 'color-mix(in oklch, var(--accent) 14%, transparent)',
              }}
            >
              <FileText
                className="h-8 w-8"
                style={{ color: 'var(--brand-ink)' }}
              />
            </div>
            <h2 className="t-h1 m-0">Aún no emitiste comprobantes</h2>
            <p
              className="t-body mt-2 mb-4 max-w-[460px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Emite tu primera factura o boleta electrónica. La firma digital y el
              envío a SUNAT son automáticos.
            </p>
            <Button
              onClick={() => router.push('/documents/new')}
              style={{
                background: 'var(--accent)',
                color: 'var(--accent-foreground)',
                fontWeight: 600,
              }}
            >
              <Plus className="h-4 w-4 mr-2" /> Emitir primer comprobante
            </Button>
          </div>
        ) : hasFilteredNoResults ? (
          <div className="p-10 text-center">
            <Inbox
              className="h-10 w-10 mx-auto mb-3"
              style={{ color: 'var(--slate-400)' }}
            />
            <p className="t-body m-0 font-semibold">
              Sin comprobantes para esos filtros
            </p>
            <p
              className="t-body-sm mt-1 mb-4"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Cambia los filtros o límpialos para ver todos.
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
                  style={{
                    color: 'var(--muted-foreground)',
                    background: 'var(--muted)',
                  }}
                >
                  <th className="text-left py-2.5 pl-6 pr-2 w-44">Número</th>
                  <th className="text-left py-2.5 px-2 w-24">Tipo</th>
                  <th className="text-left py-2.5 px-2 w-28">Fecha</th>
                  <th className="text-left py-2.5 px-2">Cliente</th>
                  <th className="text-right py-2.5 px-2 w-36">Total</th>
                  <th className="text-left py-2.5 px-2 w-28">Estado</th>
                  <th
                    className="py-2.5 pr-6 pl-2 w-24"
                    aria-label="acciones"
                  />
                </tr>
              </thead>
              <tbody>
                {data!.data.map((doc) => (
                  <tr
                    key={doc.id}
                    className="cursor-pointer transition-colors hover:bg-[var(--muted)]"
                    style={{ borderTop: '1px solid var(--border)' }}
                    onClick={() => router.push(`/documents/${doc.id}`)}
                  >
                    <td className="py-3 pl-6 pr-2 mono t-body-sm font-semibold">
                      {doc.fullNumber}
                    </td>
                    <td className="py-3 px-2">
                      <span
                        className="inline-flex items-center rounded-md px-1.5 py-0.5 t-caption font-semibold"
                        style={{
                          background: 'var(--muted)',
                          color: 'var(--muted-foreground)',
                        }}
                      >
                        {DOC_TYPE_LABEL[doc.documentType] ??
                          doc.documentTypeName}
                      </span>
                    </td>
                    <td
                      className="py-3 px-2 t-body-sm mono tnum"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {fmtDate(doc.issueDate)}
                    </td>
                    <td className="py-3 px-2 max-w-[340px] t-body-sm truncate">
                      {doc.customerName}
                    </td>
                    <td className="py-3 px-2 text-right mono tnum t-body-sm font-semibold">
                      {fmt(doc.total, doc.currency)}
                    </td>
                    <td className="py-3 px-2">
                      <StatusPill status={doc.status} />
                    </td>
                    <td
                      className="py-3 pr-6 pl-2"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <div className="flex gap-1 justify-end">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => router.push(`/documents/${doc.id}`)}
                          title="Ver"
                        >
                          <Eye className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => downloadPdf(doc.id, doc.fullNumber)}
                          title="Descargar PDF"
                        >
                          <Download className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {data && data.pagination.totalPages > 1 && (
        <div className="flex items-center justify-between flex-wrap gap-3">
          <p
            className="t-body-sm"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Página{' '}
            <span className="mono tnum font-semibold">
              {data.pagination.page}
            </span>{' '}
            de{' '}
            <span className="mono tnum font-semibold">
              {data.pagination.totalPages}
            </span>{' '}
            ·{' '}
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
