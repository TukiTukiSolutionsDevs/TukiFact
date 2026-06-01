'use client';

import { useEffect, useState, useMemo } from 'react';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Section } from '@/components/ui/section';
import { Toolbar, ChipGroup } from '@/components/ui/toolbar';
import { PillGroup, type PillOption } from '@/components/ui/pill-group';
import { StatusBadge } from '@/components/ui/status-badge';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';
import {
  Plus,
  Copy,
  ShieldAlert,
  Trash2,
  CheckCircle2,
  AlertCircle,
  KeyRound,
  Send,
  Search,
  Ban,
  Inbox,
} from 'lucide-react';
import { toast } from 'sonner';
import { format } from 'date-fns';

interface ApiKeyRecord {
  id: string;
  keyPrefix: string;
  name: string;
  permissions: string[];
  isActive: boolean;
  lastUsedAt: string | null;
  createdAt: string;
  plainTextKey?: string;
}

type Permission = 'emit' | 'query' | 'void';
type StatusFilter = 'all' | 'active' | 'revoked';

const PERMISSION_OPTIONS: readonly PillOption<Permission>[] = [
  { value: 'query', label: 'Consultar', sub: 'GET / read-only', icon: Search },
  { value: 'emit', label: 'Emitir', sub: 'POST documents', icon: Send },
  { value: 'void', label: 'Anular', sub: 'Comunicación de baja', icon: Ban },
];

const STATUS_FILTERS = [
  { value: 'active' as StatusFilter, label: 'Activas' },
  { value: 'revoked' as StatusFilter, label: 'Revocadas' },
  { value: 'all' as StatusFilter, label: 'Todas' },
] as const;

const permissionLabel: Record<Permission, string> = {
  query: 'Consultar',
  emit: 'Emitir',
  void: 'Anular',
};

const permissionColor: Record<Permission, string> = {
  query: 'var(--info)',
  emit: 'var(--success)',
  void: 'var(--warning)',
};

function formatDate(iso: string | null) {
  if (!iso) return '—';
  try {
    return format(new Date(iso), 'dd/MM/yyyy HH:mm');
  } catch {
    return '—';
  }
}

export default function ApiKeysPage() {
  const { user: me } = useAuth();
  const [keys, setKeys] = useState<ApiKeyRecord[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('active');
  const [search, setSearch] = useState('');

  const [createOpen, setCreateOpen] = useState(false);
  const [creating, setCreating] = useState(false);
  const [keyName, setKeyName] = useState('');
  const [primaryPerm, setPrimaryPerm] = useState<Permission>('query');
  const [extraPerms, setExtraPerms] = useState<Set<Permission>>(new Set());

  const [revealOpen, setRevealOpen] = useState(false);
  const [newPlainKey, setNewPlainKey] = useState('');
  const [copied, setCopied] = useState(false);

  const [revokeTarget, setRevokeTarget] = useState<ApiKeyRecord | null>(null);
  const [revoking, setRevoking] = useState(false);

  const isAdmin = me?.role === 'admin';

  const fetchKeys = async () => {
    setIsLoading(true);
    try {
      const data = await api.get<ApiKeyRecord[]>('/v1/api-keys');
      setKeys(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al cargar API Keys');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (isAdmin) fetchKeys();
    else setIsLoading(false);
  }, [isAdmin]);

  const filteredKeys = useMemo(() => {
    const q = search.trim().toLowerCase();
    return keys.filter((k) => {
      if (statusFilter === 'active' && !k.isActive) return false;
      if (statusFilter === 'revoked' && k.isActive) return false;
      if (q && !k.name.toLowerCase().includes(q) && !k.keyPrefix.toLowerCase().includes(q))
        return false;
      return true;
    });
  }, [keys, statusFilter, search]);

  const openCreate = () => {
    setKeyName('');
    setPrimaryPerm('query');
    setExtraPerms(new Set());
    setCreateOpen(true);
  };

  const toggleExtra = (p: Permission) => {
    if (p === primaryPerm) return;
    setExtraPerms((prev) => {
      const next = new Set(prev);
      if (next.has(p)) next.delete(p);
      else next.add(p);
      return next;
    });
  };

  const handleCreate = async () => {
    if (!keyName.trim()) {
      toast.error('El nombre es requerido');
      return;
    }
    const allPerms = new Set<Permission>([primaryPerm, ...extraPerms]);
    setCreating(true);
    try {
      const res = await api.post<ApiKeyRecord>('/v1/api-keys', {
        name: keyName.trim(),
        permissions: Array.from(allPerms),
      });
      toast.success('API Key generada');
      setCreateOpen(false);
      if (res.plainTextKey) {
        setNewPlainKey(res.plainTextKey);
        setRevealOpen(true);
      }
      fetchKeys();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al generar API Key');
    } finally {
      setCreating(false);
    }
  };

  const handleCopy = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopied(true);
      toast.success('Copiado al portapapeles');
      setTimeout(() => setCopied(false), 2500);
    } catch {
      toast.error('No se pudo copiar al portapapeles');
    }
  };

  const handleRevealClose = () => {
    setRevealOpen(false);
    setNewPlainKey('');
    setCopied(false);
  };

  const confirmRevoke = async () => {
    if (!revokeTarget) return;
    setRevoking(true);
    try {
      await api.delete(`/v1/api-keys/${revokeTarget.id}`);
      toast.success(`API Key "${revokeTarget.name}" revocada`);
      setRevokeTarget(null);
      fetchKeys();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al revocar');
    } finally {
      setRevoking(false);
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
            Solo los administradores pueden gestionar API Keys.
          </p>
        </div>
      </Section>
    );
  }

  return (
    <div className="space-y-[var(--gap-cards,1.5rem)]">
      <header className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="t-display-lg m-0">API Keys</h1>
          <p className="t-body-sm m-0 mt-1" style={{ color: 'var(--muted-foreground)' }}>
            Gestioná las claves de integración a la API REST. Cada secreto se muestra una sola vez.
          </p>
        </div>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" /> Generar API Key
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
            htmlFor="api-search"
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
              id="api-search"
              placeholder="Nombre o prefijo (tk_…)"
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
                  Prefijo
                </th>
                <th
                  className="t-overline text-left px-3 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Nombre
                </th>
                <th
                  className="t-overline text-left px-3 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Permisos
                </th>
                <th
                  className="t-overline text-left px-3 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Estado
                </th>
                <th
                  className="t-overline text-left px-3 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Último uso
                </th>
                <th
                  className="t-overline text-left px-3 py-3"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Creada
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
                Array.from({ length: 3 }).map((_, i) => (
                  <tr
                    key={i}
                    className="border-b last:border-0"
                    style={{ borderColor: 'var(--border)' }}
                  >
                    {Array.from({ length: 7 }).map((_, j) => (
                      <td
                        key={j}
                        className={j === 0 ? 'px-6 py-3' : j === 6 ? 'px-6 py-3' : 'px-3 py-3'}
                      >
                        <div
                          className="h-4 rounded animate-pulse"
                          style={{ background: 'var(--muted)', width: '70%' }}
                        />
                      </td>
                    ))}
                  </tr>
                ))
              ) : filteredKeys.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-6 py-12">
                    <div className="flex flex-col items-center gap-2 text-center">
                      <span
                        className="flex h-12 w-12 items-center justify-center rounded-full"
                        style={{
                          background: 'var(--muted)',
                          color: 'var(--muted-foreground)',
                        }}
                      >
                        <Inbox className="h-5 w-5" />
                      </span>
                      <p className="t-body font-medium m-0">
                        {keys.length === 0
                          ? 'Aún no generaste ninguna API Key'
                          : 'No hay resultados con esos filtros'}
                      </p>
                      <p
                        className="t-body-sm m-0"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        Las API Keys permiten que tus integraciones (ERP, SDK, scripts) emitan
                        comprobantes contra la API REST.
                      </p>
                      {keys.length === 0 && (
                        <Button onClick={openCreate} className="mt-3">
                          <Plus className="mr-2 h-4 w-4" /> Generar primera key
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ) : (
                filteredKeys.map((k) => (
                  <tr
                    key={k.id}
                    className="border-b last:border-0 hover:bg-[var(--muted)]/40"
                    style={{ borderColor: 'var(--border)' }}
                  >
                    <td className="px-6 py-3">
                      <button
                        type="button"
                        onClick={() => handleCopy(k.keyPrefix)}
                        className="t-caption mono tnum rounded-md px-2 py-0.5 hover:opacity-80 transition"
                        style={{
                          background: 'var(--muted)',
                          color: 'var(--foreground)',
                        }}
                        title="Copiar prefijo"
                      >
                        {k.keyPrefix}…
                      </button>
                    </td>
                    <td className="px-3 py-3 t-body-sm font-medium">{k.name}</td>
                    <td className="px-3 py-3">
                      <div className="flex flex-wrap gap-1.5">
                        {k.permissions.map((p) => {
                          const perm = p as Permission;
                          const color = permissionColor[perm] ?? 'var(--muted-foreground)';
                          return (
                            <span
                              key={p}
                              className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 t-caption font-semibold"
                              style={{
                                color,
                                background: `color-mix(in oklch, ${color} 14%, transparent)`,
                              }}
                            >
                              {permissionLabel[perm] ?? p}
                            </span>
                          );
                        })}
                      </div>
                    </td>
                    <td className="px-3 py-3">
                      {k.isActive ? (
                        <StatusBadge status="active" />
                      ) : (
                        <span
                          className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 t-caption font-semibold"
                          style={{
                            color: 'var(--slate-500)',
                            background: 'color-mix(in oklch, var(--slate-500) 14%, transparent)',
                          }}
                        >
                          <Ban className="h-3 w-3" /> Revocada
                        </span>
                      )}
                    </td>
                    <td
                      className="px-3 py-3 t-caption tnum"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {formatDate(k.lastUsedAt)}
                    </td>
                    <td
                      className="px-3 py-3 t-caption tnum"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {formatDate(k.createdAt)}
                    </td>
                    <td className="px-6 py-3 text-right">
                      {k.isActive && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setRevokeTarget(k)}
                          style={{ color: 'var(--danger)' }}
                          title="Revocar"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Section>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <KeyRound className="h-5 w-5" />
              Generar API Key
            </DialogTitle>
            <DialogDescription>
              El secreto se muestra una sola vez. Guardalo en tu bóveda antes de cerrar.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-5 pt-2">
            <div>
              <Label
                htmlFor="key-name"
                className="t-overline mb-2 block"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Nombre
              </Label>
              <Input
                id="key-name"
                placeholder="Ej.: ERP producción"
                value={keyName}
                onChange={(e) => setKeyName(e.target.value)}
                autoFocus
              />
              <p
                className="t-caption mt-1.5"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Te ayuda a identificar qué integración usa esta clave.
              </p>
            </div>

            <div>
              <Label
                className="t-overline mb-2 block"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Permiso principal
              </Label>
              <PillGroup<Permission>
                value={primaryPerm}
                onChange={setPrimaryPerm}
                options={PERMISSION_OPTIONS}
                cols={3}
              />
            </div>

            <div>
              <Label
                className="t-overline mb-2 block"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Permisos adicionales
              </Label>
              <div className="flex flex-wrap gap-2">
                {PERMISSION_OPTIONS.filter((p) => p.value !== primaryPerm).map((p) => {
                  const checked = extraPerms.has(p.value);
                  return (
                    <button
                      key={p.value}
                      type="button"
                      onClick={() => toggleExtra(p.value)}
                      className="inline-flex items-center gap-2 rounded-full px-3 py-1.5 t-caption font-semibold transition-colors border"
                      style={{
                        background: checked
                          ? 'color-mix(in oklch, var(--accent) 18%, transparent)'
                          : 'var(--card)',
                        borderColor: checked ? 'var(--accent)' : 'var(--border)',
                        color: checked ? 'var(--brand-ink)' : 'var(--muted-foreground)',
                      }}
                    >
                      {checked && <CheckCircle2 className="h-3 w-3" />}
                      {p.label}
                    </button>
                  );
                })}
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
                {creating ? 'Generando…' : 'Generar API Key'}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={revealOpen} onOpenChange={(o) => !o && handleRevealClose()}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <CheckCircle2 className="h-5 w-5" style={{ color: 'var(--success)' }} />
              API Key generada
            </DialogTitle>
            <DialogDescription>
              Esta es la única vez que verás la clave completa.
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
                <strong>Guardá esta clave ahora.</strong> Por seguridad no podremos
                mostrártela de nuevo.
              </p>
            </div>
            <div>
              <Label
                className="t-overline mb-2 block"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Tu API Key
              </Label>
              <div className="flex gap-2">
                <code
                  className="flex-1 block rounded-[var(--radius-md)] px-3 py-2 t-body-sm mono break-all select-all"
                  style={{
                    background: 'var(--muted)',
                    color: 'var(--foreground)',
                  }}
                >
                  {newPlainKey}
                </code>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleCopy(newPlainKey)}
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
            <Button className="w-full" onClick={handleRevealClose}>
              Entendido, ya la guardé
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={!!revokeTarget} onOpenChange={(o) => !o && setRevokeTarget(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Trash2 className="h-5 w-5" style={{ color: 'var(--danger)' }} />
              Revocar API Key
            </DialogTitle>
            <DialogDescription>
              Las integraciones que la usen dejarán de poder autenticarse de inmediato.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 pt-2">
            {revokeTarget && (
              <div
                className="rounded-[var(--radius-md)] p-3 t-body-sm"
                style={{ background: 'var(--muted)' }}
              >
                <p className="m-0">
                  Vas a revocar <strong>{revokeTarget.name}</strong>{' '}
                  <span className="mono tnum opacity-70">({revokeTarget.keyPrefix}…)</span>.
                </p>
              </div>
            )}
            <div className="flex gap-2">
              <Button
                variant="outline"
                className="flex-1"
                onClick={() => setRevokeTarget(null)}
                disabled={revoking}
              >
                Cancelar
              </Button>
              <Button
                className="flex-1"
                onClick={confirmRevoke}
                disabled={revoking}
                style={{
                  background: 'var(--danger)',
                  color: 'white',
                }}
              >
                {revoking ? 'Revocando…' : 'Revocar'}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
