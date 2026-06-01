'use client';

import { useEffect, useState, useCallback } from 'react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Toolbar, ChipGroup } from '@/components/ui/toolbar';
import { PaginationFooter } from '@/components/ui/pagination-footer';
import {
  ScrollText,
  Loader2,
  Inbox,
  Search,
  FilePlus,
  FileMinus,
  UserPlus,
  LogIn,
  KeyRound,
  Webhook as WebhookIcon,
  Hash,
  Ban,
  Activity,
  type LucideIcon,
} from 'lucide-react';

interface AuditEntry {
  id: string;
  action: string;
  entityType: string;
  entityId: string | null;
  details: string | null;
  userId: string | null;
  ipAddress: string | null;
  createdAt: string;
}

interface AuditResponse {
  data: AuditEntry[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

const ENTITY_FILTERS: { value: string; label: string }[] = [
  { value: '', label: 'Todas' },
  { value: 'Document', label: 'Documentos' },
  { value: 'User', label: 'Usuarios' },
  { value: 'Auth', label: 'Autenticación' },
  { value: 'ApiKey', label: 'API Keys' },
  { value: 'Webhook', label: 'Webhooks' },
  { value: 'Series', label: 'Series' },
];

const ACTION_META: Record<string, { label: string; color: string; icon: LucideIcon }> = {
  'document.created': { label: 'Comprobante emitido', color: 'var(--info)', icon: FilePlus },
  'creditnote.created': { label: 'Nota de crédito creada', color: 'var(--brand-toucan-orange)', icon: FileMinus },
  'document.voided': { label: 'Comprobante anulado', color: 'var(--danger)', icon: Ban },
  'user.login': { label: 'Inicio de sesión', color: 'var(--success)', icon: LogIn },
  'user.created': { label: 'Usuario creado', color: 'var(--success)', icon: UserPlus },
  'webhook.created': { label: 'Webhook configurado', color: 'var(--warning)', icon: WebhookIcon },
  'apikey.generated': { label: 'API key generada', color: 'var(--brand-toucan-orange)', icon: KeyRound },
  'series.created': { label: 'Serie creada', color: 'var(--info)', icon: Hash },
};

const actionMeta = (action: string) =>
  ACTION_META[action] ?? { label: action, color: 'var(--slate-500)', icon: Activity };

const formatDateTime = (iso: string) =>
  new Date(iso).toLocaleString('es-PE', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

export default function AuditLogPage() {
  const [data, setData] = useState<AuditResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [entityFilter, setEntityFilter] = useState('');
  const [search, setSearch] = useState('');

  const fetchAuditLog = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '20' });
      if (entityFilter) params.set('entityType', entityFilter);
      setData(await api.get<AuditResponse>(`/v1/audit-log?${params}`));
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, entityFilter]);

  useEffect(() => {
    fetchAuditLog();
  }, [fetchAuditLog]);

  const visibleEntries = (() => {
    if (!data) return [];
    if (!search.trim()) return data.data;
    const q = search.toLowerCase();
    return data.data.filter(
      (e) =>
        e.action.toLowerCase().includes(q) ||
        e.entityType.toLowerCase().includes(q) ||
        (e.entityId?.toLowerCase().includes(q) ?? false) ||
        (e.ipAddress?.includes(q) ?? false) ||
        (e.details?.toLowerCase().includes(q) ?? false)
    );
  })();

  const hasAnyFilters = entityFilter !== '' || search !== '';
  const clearFilters = () => {
    setEntityFilter('');
    setSearch('');
    setPage(1);
  };

  const hasNoResults = !isLoading && data?.data.length === 0 && !hasAnyFilters;
  const hasFilteredNoResults =
    !isLoading && (data?.data.length === 0 || visibleEntries.length === 0) && hasAnyFilters;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Registro de auditoría</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Histórico de acciones administrativas realizadas en tu cuenta.{' '}
            {data && (
              <span className="mono tnum" style={{ color: 'var(--foreground)' }}>
                {data.pagination.totalCount} eventos.
              </span>
            )}
          </p>
        </div>
      </div>

      {/* Filters */}
      <Toolbar>
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
              placeholder="Acción, ID, IP, detalles…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9"
            />
          </div>
        </div>
        <ChipGroup
          value={entityFilter}
          onChange={(v) => {
            setEntityFilter(v);
            setPage(1);
          }}
          options={ENTITY_FILTERS}
        />
        {hasAnyFilters && (
          <Button variant="ghost" size="sm" onClick={clearFilters}>
            Limpiar
          </Button>
        )}
      </Toolbar>

      {/* Table */}
      <section
        className="rounded-[var(--radius-lg)] border bg-card overflow-hidden mb-[var(--gap-cards)]"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        {isLoading ? (
          <div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span className="t-body-sm">Cargando registros…</span>
          </div>
        ) : hasNoResults ? (
          <div className="p-10 text-center">
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{ background: 'color-mix(in oklch, var(--accent) 14%, transparent)' }}
            >
              <ScrollText className="h-8 w-8" style={{ color: 'var(--brand-ink)' }} />
            </div>
            <h2 className="t-h1 m-0">Aún no hay eventos registrados</h2>
            <p
              className="t-body mt-2 mb-0 max-w-[440px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Cada acción admin (emisiones, anulaciones, creación de usuarios, generación de keys)
              quedará registrada acá automáticamente.
            </p>
          </div>
        ) : hasFilteredNoResults ? (
          <div className="p-10 text-center">
            <Inbox className="h-10 w-10 mx-auto mb-3" style={{ color: 'var(--slate-400)' }} />
            <p className="t-body m-0 font-semibold">Sin resultados con esos filtros</p>
            <p className="t-body-sm mt-1 mb-4" style={{ color: 'var(--muted-foreground)' }}>
              Prueba con otra categoría o limpia los filtros.
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
                  <th className="text-left py-2.5 pl-6 pr-2">Acción</th>
                  <th className="text-left py-2.5 px-2 w-32">Tipo</th>
                  <th className="text-left py-2.5 px-2">Detalle</th>
                  <th className="text-left py-2.5 px-2 w-32">IP</th>
                  <th className="text-left py-2.5 pr-6 pl-2 w-44">Fecha</th>
                </tr>
              </thead>
              <tbody>
                {visibleEntries.map((entry) => {
                  const meta = actionMeta(entry.action);
                  const Icon = meta.icon;
                  return (
                    <tr key={entry.id} style={{ borderTop: '1px solid var(--border)' }}>
                      <td className="py-3 pl-6 pr-2">
                        <span
                          className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 t-caption font-semibold whitespace-nowrap"
                          style={{
                            color: meta.color,
                            background: `color-mix(in oklch, ${meta.color} 14%, transparent)`,
                          }}
                        >
                          <Icon className="h-3 w-3" />
                          {meta.label}
                        </span>
                      </td>
                      <td className="py-3 px-2">
                        <span
                          className="inline-flex items-center rounded-full px-2 py-0.5 t-caption font-semibold"
                          style={{
                            background: 'var(--muted)',
                            color: 'var(--muted-foreground)',
                          }}
                        >
                          {entry.entityType}
                        </span>
                      </td>
                      <td className="py-3 px-2 max-w-[400px]">
                        {entry.details ? (
                          <span className="t-body-sm truncate inline-block max-w-full">
                            {entry.details}
                          </span>
                        ) : entry.entityId ? (
                          <span
                            className="mono t-caption"
                            style={{ color: 'var(--muted-foreground)' }}
                          >
                            {entry.entityId}
                          </span>
                        ) : (
                          <span style={{ color: 'var(--muted-foreground)' }}>—</span>
                        )}
                      </td>
                      <td className="py-3 px-2 mono t-caption" style={{ color: 'var(--muted-foreground)' }}>
                        {entry.ipAddress || '—'}
                      </td>
                      <td className="py-3 pr-6 pl-2 mono tnum t-body-sm">
                        {formatDateTime(entry.createdAt)}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {data && (
        <PaginationFooter
          page={data.pagination.page}
          totalPages={data.pagination.totalPages}
          totalCount={data.pagination.totalCount}
          onPrev={() => setPage((p) => p - 1)}
          onNext={() => setPage((p) => p + 1)}
        />
      )}
    </div>
  );
}
