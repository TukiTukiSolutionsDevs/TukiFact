'use client';

import { useEffect, useState, useMemo } from 'react';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Section } from '@/components/ui/section';
import { Toolbar, ChipGroup } from '@/components/ui/toolbar';
import { StatusBadge } from '@/components/ui/status-badge';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import {
  Plus,
  Trash2,
  Webhook,
  Eye,
  Copy,
  CheckCircle2,
  AlertCircle,
  Search,
  ShieldAlert,
  XCircle,
  Clock,
  Send,
} from 'lucide-react';
import { toast } from 'sonner';

interface WebhookConfig {
  id: string;
  url: string;
  events: string[];
  isActive: boolean;
  maxRetries: number;
  lastTriggeredAt: string | null;
  createdAt: string;
}

interface WebhookDelivery {
  id: string;
  eventType: string;
  status: string;
  attempt: number;
  responseStatus: string | null;
  createdAt: string;
}

type EventType = 'document.created' | 'document.accepted' | 'document.rejected' | 'document.voided';
type StatusFilter = 'all' | 'active' | 'paused';

const EVENT_OPTIONS: { value: EventType; label: string; color: string }[] = [
  { value: 'document.created', label: 'Creado', color: 'var(--info)' },
  { value: 'document.accepted', label: 'Aceptado', color: 'var(--success)' },
  { value: 'document.rejected', label: 'Rechazado', color: 'var(--danger)' },
  { value: 'document.voided', label: 'Anulado', color: 'var(--slate-500)' },
];

const EVENT_MAP = Object.fromEntries(EVENT_OPTIONS.map((e) => [e.value, e])) as Record<
  string,
  (typeof EVENT_OPTIONS)[number]
>;

const STATUS_FILTERS = [
  { value: 'active' as StatusFilter, label: 'Activos' },
  { value: 'paused' as StatusFilter, label: 'Pausados' },
  { value: 'all' as StatusFilter, label: 'Todos' },
] as const;

const DEFAULT_FORM = {
  url: '',
  events: ['document.accepted', 'document.rejected'] as string[],
  maxRetries: 3,
};

function formatDate(iso: string | null) {
  if (!iso) return '—';
  try {
    return new Date(iso).toLocaleString('es-PE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return '—';
  }
}

function isValidHttpsUrl(s: string) {
  try {
    const u = new URL(s);
    return u.protocol === 'https:' || u.protocol === 'http:';
  } catch {
    return false;
  }
}

export default function WebhooksPage() {
  const { user: me } = useAuth();
  const isAdmin = me?.role === 'admin';

  const [webhooks, setWebhooks] = useState<WebhookConfig[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('active');
  const [search, setSearch] = useState('');

  const [createOpen, setCreateOpen] = useState(false);
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState(DEFAULT_FORM);

  const [revealedSecret, setRevealedSecret] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const [deliveriesTarget, setDeliveriesTarget] = useState<WebhookConfig | null>(null);
  const [deliveries, setDeliveries] = useState<WebhookDelivery[]>([]);
  const [loadingDeliveries, setLoadingDeliveries] = useState(false);

  const [deleteTarget, setDeleteTarget] = useState<WebhookConfig | null>(null);
  const [deleting, setDeleting] = useState(false);

  const fetchWebhooks = async () => {
    setIsLoading(true);
    try {
      setWebhooks(await api.get<WebhookConfig[]>('/v1/webhooks'));
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al cargar webhooks');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (isAdmin) fetchWebhooks();
    else setIsLoading(false);
  }, [isAdmin]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return webhooks.filter((w) => {
      if (statusFilter === 'active' && !w.isActive) return false;
      if (statusFilter === 'paused' && w.isActive) return false;
      if (q && !w.url.toLowerCase().includes(q)) return false;
      return true;
    });
  }, [webhooks, statusFilter, search]);

  const openCreate = () => {
    setForm(DEFAULT_FORM);
    setCreateOpen(true);
  };

  const toggleEvent = (e: string) => {
    setForm((prev) => ({
      ...prev,
      events: prev.events.includes(e) ? prev.events.filter((x) => x !== e) : [...prev.events, e],
    }));
  };

  const handleCreate = async () => {
    if (!form.url.trim()) {
      toast.error('URL requerida');
      return;
    }
    if (!isValidHttpsUrl(form.url.trim())) {
      toast.error('La URL debe ser una URL HTTP/HTTPS válida');
      return;
    }
    if (form.events.length === 0) {
      toast.error('Seleccioná al menos un evento');
      return;
    }
    setCreating(true);
    try {
      const res = await api.post<WebhookConfig & { secret: string }>('/v1/webhooks', {
        url: form.url.trim(),
        events: form.events,
        maxRetries: form.maxRetries,
      });
      toast.success('Webhook creado');
      setCreateOpen(false);
      if (res.secret) setRevealedSecret(res.secret);
      fetchWebhooks();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al crear webhook');
    } finally {
      setCreating(false);
    }
  };

  const openDeliveries = async (w: WebhookConfig) => {
    setDeliveriesTarget(w);
    setLoadingDeliveries(true);
    setDeliveries([]);
    try {
      setDeliveries(await api.get<WebhookDelivery[]>(`/v1/webhooks/${w.id}/deliveries`));
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al cargar entregas');
    } finally {
      setLoadingDeliveries(false);
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await api.delete(`/v1/webhooks/${deleteTarget.id}`);
      toast.success('Webhook eliminado');
      setDeleteTarget(null);
      if (deliveriesTarget?.id === deleteTarget.id) {
        setDeliveriesTarget(null);
        setDeliveries([]);
      }
      fetchWebhooks();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al eliminar');
    } finally {
      setDeleting(false);
    }
  };

  const copySecret = async () => {
    if (!revealedSecret) return;
    try {
      await navigator.clipboard.writeText(revealedSecret);
      setCopied(true);
      toast.success('Secret copiado');
      setTimeout(() => setCopied(false), 2500);
    } catch {
      toast.error('No se pudo copiar');
    }
  };

  if (!isAdmin) {
    return (
      <Section className="border-[color:var(--danger)]/40">
        <div className="flex flex-col items-center justify-center py-12 gap-3 text-center">
          <span
            className="flex h-12 w-12 items-center justify-center rounded-full"
            style={{
              background: 'color-mix(in oklch, var(--danger) 14%, transparent)',
              color: 'var(--danger)',
            }}
          >
            <ShieldAlert className="h-6 w-6" />
          </span>
          <h2 className="t-display-md m-0">Acceso restringido</h2>
          <p className="t-body-sm m-0" style={{ color: 'var(--muted-foreground)' }}>
            Solo los administradores pueden configurar webhooks.
          </p>
        </div>
      </Section>
    );
  }

  return (
    <div className="space-y-[var(--gap-cards,1.5rem)]">
      <header className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="t-display-lg m-0">Webhooks</h1>
          <p className="t-body-sm m-0 mt-1" style={{ color: 'var(--muted-foreground)' }}>
            Recibí notificaciones HTTP firmadas (HMAC-SHA256) cuando ocurren eventos en tu cuenta.
          </p>
        </div>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" /> Nuevo webhook
        </Button>
      </header>

      <Toolbar>
        <div className="flex flex-col gap-2">
          <Label className="t-overline" style={{ color: 'var(--muted-foreground)' }}>
            Estado
          </Label>
          <ChipGroup<StatusFilter>
            value={statusFilter}
            onChange={setStatusFilter}
            options={STATUS_FILTERS}
          />
        </div>
        <div className="flex flex-col gap-2 ml-auto min-w-[260px] flex-1">
          <Label
            htmlFor="wh-search"
            className="t-overline"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Buscar
          </Label>
          <div className="relative">
            <Search
              className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
              style={{ color: 'var(--muted-foreground)' }}
            />
            <Input
              id="wh-search"
              placeholder="URL del endpoint"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9"
            />
          </div>
        </div>
      </Toolbar>

      <Section bodyClassName="-mx-6">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b" style={{ borderColor: 'var(--border)' }}>
                <th
                  className="t-overline text-left px-6 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  URL
                </th>
                <th
                  className="t-overline text-left px-3 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Eventos
                </th>
                <th
                  className="t-overline text-left px-3 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Estado
                </th>
                <th
                  className="t-overline text-center px-3 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Reintentos
                </th>
                <th
                  className="t-overline text-left px-3 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Último trigger
                </th>
                <th
                  className="t-overline text-right px-6 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Acciones
                </th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                Array.from({ length: 2 }).map((_, i) => (
                  <tr
                    key={i}
                    className="border-b last:border-0"
                    style={{ borderColor: 'var(--border)' }}
                  >
                    {Array.from({ length: 6 }).map((_, j) => (
                      <td
                        key={j}
                        className={j === 0 || j === 5 ? 'px-6 py-3' : 'px-3 py-3'}
                      >
                        <div
                          className="h-4 rounded animate-pulse"
                          style={{ background: 'var(--muted)', width: '70%' }}
                        />
                      </td>
                    ))}
                  </tr>
                ))
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12">
                    <div className="flex flex-col items-center gap-2 text-center">
                      <span
                        className="flex h-12 w-12 items-center justify-center rounded-full"
                        style={{
                          background: 'var(--muted)',
                          color: 'var(--muted-foreground)',
                        }}
                      >
                        <Webhook className="h-5 w-5" />
                      </span>
                      <p className="t-body font-medium m-0">
                        {webhooks.length === 0
                          ? 'Aún no configuraste webhooks'
                          : 'No hay resultados con esos filtros'}
                      </p>
                      <p
                        className="t-body-sm m-0"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        Configurá un endpoint HTTP y suscribite a eventos para recibir
                        notificaciones automáticas.
                      </p>
                      {webhooks.length === 0 && (
                        <Button onClick={openCreate} className="mt-3">
                          <Plus className="mr-2 h-4 w-4" /> Nuevo webhook
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ) : (
                filtered.map((w) => (
                  <tr
                    key={w.id}
                    onClick={() => openDeliveries(w)}
                    className="border-b last:border-0 hover:bg-[var(--muted)]/40 cursor-pointer"
                    style={{ borderColor: 'var(--border)' }}
                  >
                    <td className="px-6 py-3">
                      <code
                        className="t-caption mono break-all"
                        style={{ color: 'var(--foreground)' }}
                        title={w.url}
                      >
                        {w.url}
                      </code>
                    </td>
                    <td className="px-3 py-3">
                      <div className="flex flex-wrap gap-1.5">
                        {w.events.map((e) => {
                          const meta = EVENT_MAP[e];
                          const color = meta?.color ?? 'var(--muted-foreground)';
                          return (
                            <span
                              key={e}
                              className="inline-flex items-center rounded-full px-2 py-0.5 t-caption font-semibold"
                              style={{
                                color,
                                background: `color-mix(in oklch, ${color} 14%, transparent)`,
                              }}
                            >
                              {meta?.label ?? e.split('.').pop()}
                            </span>
                          );
                        })}
                      </div>
                    </td>
                    <td className="px-3 py-3">
                      {w.isActive ? (
                        <StatusBadge status="active" />
                      ) : (
                        <StatusBadge status="paused" label="Pausado" />
                      )}
                    </td>
                    <td className="px-3 py-3 text-center t-body-sm mono tnum">
                      {w.maxRetries}
                    </td>
                    <td
                      className="px-3 py-3 t-caption tnum"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {formatDate(w.lastTriggeredAt)}
                    </td>
                    <td
                      className="px-6 py-3 text-right"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <div className="flex gap-1 justify-end">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => openDeliveries(w)}
                          title="Ver entregas"
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setDeleteTarget(w)}
                          style={{ color: 'var(--danger)' }}
                          title="Eliminar"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Section>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Webhook className="h-5 w-5" />
              Nuevo webhook
            </DialogTitle>
            <DialogDescription>
              Tu endpoint recibirá los eventos seleccionados firmados con HMAC-SHA256.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-5 pt-2">
            <div>
              <Label
                htmlFor="wh-url"
                className="t-overline mb-2 block"
                style={{ color: 'var(--muted-foreground)' }}
              >
                URL del endpoint
              </Label>
              <Input
                id="wh-url"
                type="url"
                placeholder="https://tu-servidor.com/webhook"
                value={form.url}
                onChange={(e) => setForm((p) => ({ ...p, url: e.target.value }))}
                className="mono"
                autoFocus
              />
              <p
                className="t-caption mt-1.5"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Recomendamos HTTPS. El payload llega en POST JSON.
              </p>
            </div>

            <div>
              <Label
                className="t-overline mb-2 block"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Eventos a suscribir
              </Label>
              <div className="grid grid-cols-2 gap-2">
                {EVENT_OPTIONS.map((e) => {
                  const active = form.events.includes(e.value);
                  return (
                    <button
                      key={e.value}
                      type="button"
                      onClick={() => toggleEvent(e.value)}
                      className="flex items-center justify-between rounded-[var(--radius-md)] border px-3 py-2 transition-colors text-left"
                      style={{
                        background: active
                          ? `color-mix(in oklch, ${e.color} 12%, transparent)`
                          : 'var(--card)',
                        borderColor: active ? e.color : 'var(--border)',
                      }}
                    >
                      <div className="min-w-0">
                        <p className="t-body-sm font-semibold m-0" style={{ color: active ? e.color : 'var(--foreground)' }}>
                          {e.label}
                        </p>
                        <p
                          className="t-caption mono m-0 truncate"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          {e.value}
                        </p>
                      </div>
                      {active && (
                        <CheckCircle2
                          className="h-4 w-4 shrink-0 ml-2"
                          style={{ color: e.color }}
                        />
                      )}
                    </button>
                  );
                })}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label
                  htmlFor="wh-retries"
                  className="t-overline mb-2 block"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Reintentos máximos
                </Label>
                <Input
                  id="wh-retries"
                  type="number"
                  min="0"
                  max="10"
                  value={form.maxRetries}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, maxRetries: parseInt(e.target.value || '0', 10) }))
                  }
                  className="mono tnum text-right"
                />
                <p
                  className="t-caption mt-1.5"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Backoff exponencial: 2^attempt segundos
                </p>
              </div>
              <div
                className="rounded-[var(--radius-md)] p-3 text-xs"
                style={{ background: 'var(--muted)' }}
              >
                <p
                  className="t-caption font-semibold mb-1.5"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Headers enviados
                </p>
                <code
                  className="block mono break-all leading-relaxed"
                  style={{ color: 'var(--foreground)', fontSize: '11px' }}
                >
                  X-TukiFact-Event
                  <br />
                  X-TukiFact-Signature: sha256=…
                  <br />
                  X-TukiFact-Delivery
                </code>
              </div>
            </div>

            <div className="flex gap-2 pt-2">
              <Button
                variant="outline"
                className="flex-1"
                onClick={() => setCreateOpen(false)}
                disabled={creating}
              >
                Cancelar
              </Button>
              <Button className="flex-1" onClick={handleCreate} disabled={creating}>
                {creating ? 'Creando…' : 'Crear webhook'}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={!!revealedSecret} onOpenChange={(o) => !o && setRevealedSecret(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <CheckCircle2 className="h-5 w-5" style={{ color: 'var(--success)' }} />
              Webhook creado
            </DialogTitle>
            <DialogDescription>
              Este es el secret HMAC para validar las firmas de los payloads.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 pt-2">
            <div
              className="flex items-start gap-3 rounded-[var(--radius-md)] p-3"
              style={{
                background: 'color-mix(in oklch, var(--warning) 12%, transparent)',
                border: '1px solid color-mix(in oklch, var(--warning) 40%, transparent)',
              }}
              aria-live="polite"
            >
              <AlertCircle
                className="h-4 w-4 shrink-0 mt-0.5"
                style={{ color: 'var(--warning)' }}
              />
              <p className="t-body-sm m-0">
                <strong>Copialo ahora.</strong> No podremos mostrártelo de nuevo. Lo necesitás
                en tu servidor para verificar HMAC-SHA256.
              </p>
            </div>
            <div>
              <Label
                className="t-overline mb-2 block"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Secret HMAC
              </Label>
              <div className="flex gap-2">
                <code
                  className="flex-1 block rounded-[var(--radius-md)] px-3 py-2 t-body-sm mono break-all select-all"
                  style={{
                    background: 'var(--muted)',
                    color: 'var(--foreground)',
                  }}
                >
                  {revealedSecret}
                </code>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={copySecret}
                  className="shrink-0"
                >
                  {copied ? (
                    <CheckCircle2 className="h-4 w-4" style={{ color: 'var(--success)' }} />
                  ) : (
                    <Copy className="h-4 w-4" />
                  )}
                </Button>
              </div>
            </div>
            <Button className="w-full" onClick={() => setRevealedSecret(null)}>
              Entendido, ya lo guardé
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={!!deleteTarget} onOpenChange={(o) => !o && setDeleteTarget(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Trash2 className="h-5 w-5" style={{ color: 'var(--danger)' }} />
              Eliminar webhook
            </DialogTitle>
            <DialogDescription>
              Se borrará el historial de entregas y no podrás recuperarlo.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 pt-2">
            {deleteTarget && (
              <div
                className="rounded-[var(--radius-md)] p-3 t-body-sm"
                style={{ background: 'var(--muted)' }}
              >
                <p className="m-0 mono break-all">{deleteTarget.url}</p>
              </div>
            )}
            <div className="flex gap-2">
              <Button
                variant="outline"
                className="flex-1"
                onClick={() => setDeleteTarget(null)}
                disabled={deleting}
              >
                Cancelar
              </Button>
              <Button
                className="flex-1"
                onClick={confirmDelete}
                disabled={deleting}
                style={{ background: 'var(--danger)', color: 'white' }}
              >
                {deleting ? 'Eliminando…' : 'Eliminar'}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <Sheet
        open={!!deliveriesTarget}
        onOpenChange={(o) => {
          if (!o) {
            setDeliveriesTarget(null);
            setDeliveries([]);
          }
        }}
      >
        <SheetContent className="w-full sm:max-w-2xl">
          {deliveriesTarget && (
            <>
              <SheetHeader>
                <SheetTitle className="flex items-center gap-2">
                  <Send className="h-5 w-5" />
                  Entregas recientes
                </SheetTitle>
                <SheetDescription>
                  <code className="mono t-caption break-all">{deliveriesTarget.url}</code>
                </SheetDescription>
              </SheetHeader>
              <div className="px-4 py-4 space-y-3 overflow-y-auto">
                {loadingDeliveries ? (
                  <div className="flex justify-center py-8">
                    <div className="h-6 w-6 animate-spin rounded-full border-2 border-current border-t-transparent opacity-60" />
                  </div>
                ) : deliveries.length === 0 ? (
                  <div className="flex flex-col items-center text-center py-12 gap-2">
                    <span
                      className="flex h-12 w-12 items-center justify-center rounded-full"
                      style={{ background: 'var(--muted)', color: 'var(--muted-foreground)' }}
                    >
                      <Clock className="h-5 w-5" />
                    </span>
                    <p className="t-body-sm m-0" style={{ color: 'var(--muted-foreground)' }}>
                      Aún no se enviaron eventos a este endpoint.
                    </p>
                  </div>
                ) : (
                  deliveries.map((d) => {
                    const meta = EVENT_MAP[d.eventType];
                    const ok = d.status === 'delivered';
                    const fail = d.status === 'failed';
                    return (
                      <div
                        key={d.id}
                        className="rounded-[var(--radius-md)] border p-3"
                        style={{ borderColor: 'var(--border)', background: 'var(--card)' }}
                      >
                        <div className="flex items-start justify-between gap-3 mb-2">
                          <div className="flex items-center gap-2">
                            <span
                              className="inline-flex items-center rounded-full px-2 py-0.5 t-caption font-semibold"
                              style={{
                                color: meta?.color ?? 'var(--muted-foreground)',
                                background: `color-mix(in oklch, ${
                                  meta?.color ?? 'var(--muted-foreground)'
                                } 14%, transparent)`,
                              }}
                            >
                              {meta?.label ?? d.eventType}
                            </span>
                            {ok && (
                              <span
                                className="inline-flex items-center gap-1 t-caption font-semibold"
                                style={{ color: 'var(--success)' }}
                              >
                                <CheckCircle2 className="h-3 w-3" /> Entregado
                              </span>
                            )}
                            {fail && (
                              <span
                                className="inline-flex items-center gap-1 t-caption font-semibold"
                                style={{ color: 'var(--danger)' }}
                              >
                                <XCircle className="h-3 w-3" /> Falló
                              </span>
                            )}
                            {!ok && !fail && (
                              <span
                                className="inline-flex items-center gap-1 t-caption font-semibold"
                                style={{ color: 'var(--warning)' }}
                              >
                                <Clock className="h-3 w-3" /> {d.status}
                              </span>
                            )}
                          </div>
                          <span
                            className="t-caption tnum mono"
                            style={{ color: 'var(--muted-foreground)' }}
                          >
                            {formatDate(d.createdAt)}
                          </span>
                        </div>
                        <div className="flex items-center gap-4 t-caption">
                          <span style={{ color: 'var(--muted-foreground)' }}>
                            Intento{' '}
                            <span className="mono tnum" style={{ color: 'var(--foreground)' }}>
                              {d.attempt}
                            </span>
                          </span>
                          <span style={{ color: 'var(--muted-foreground)' }}>
                            HTTP{' '}
                            <span className="mono tnum" style={{ color: 'var(--foreground)' }}>
                              {d.responseStatus ?? '—'}
                            </span>
                          </span>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            </>
          )}
        </SheetContent>
      </Sheet>
    </div>
  );
}
