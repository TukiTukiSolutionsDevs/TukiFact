'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Plus, Copy, ShieldAlert, Trash2, CheckCircle2, AlertCircle } from 'lucide-react';
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

const ALL_PERMISSIONS = ['emit', 'query', 'void'] as const;
type Permission = (typeof ALL_PERMISSIONS)[number];

const permissionLabels: Record<Permission, string> = {
  emit: 'Emitir',
  query: 'Consultar',
  void: 'Anular',
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

  // Create dialog
  const [createOpen, setCreateOpen] = useState(false);
  const [creating, setCreating] = useState(false);
  const [keyName, setKeyName] = useState('');
  const [selectedPerms, setSelectedPerms] = useState<Set<Permission>>(new Set(['query']));

  // Reveal dialog (show plaintext key after creation)
  const [revealOpen, setRevealOpen] = useState(false);
  const [newPlainKey, setNewPlainKey] = useState('');
  const [copied, setCopied] = useState(false);

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

  const togglePerm = (p: Permission) => {
    setSelectedPerms((prev) => {
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
    if (selectedPerms.size === 0) {
      toast.error('Seleccioná al menos un permiso');
      return;
    }
    setCreating(true);
    try {
      const res = await api.post<ApiKeyRecord>('/v1/api-keys', {
        name: keyName.trim(),
        permissions: Array.from(selectedPerms),
      });
      toast.success('API Key generada');
      setCreateOpen(false);
      setKeyName('');
      setSelectedPerms(new Set(['query']));
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

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(newPlainKey);
      setCopied(true);
      toast.success('API Key copiada al portapapeles');
      setTimeout(() => setCopied(false), 3000);
    } catch {
      toast.error('No se pudo copiar al portapapeles');
    }
  };

  const handleRevealClose = () => {
    setRevealOpen(false);
    setNewPlainKey('');
    setCopied(false);
  };

  const handleRevoke = async (k: ApiKeyRecord) => {
    if (!confirm(`¿Revocar la API Key "${k.name}" (${k.keyPrefix}...)? Esta acción no se puede deshacer.`)) return;
    try {
      await api.delete(`/v1/api-keys/${k.id}`);
      toast.success(`API Key "${k.name}" revocada`);
      fetchKeys();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al revocar');
    }
  };

  if (!isAdmin) {
    return (
      <div className="flex flex-col items-center justify-center py-24 gap-4">
        <ShieldAlert className="h-12 w-12 text-destructive" />
        <h2 className="text-xl font-semibold">Acceso restringido</h2>
        <p className="text-muted-foreground">Solo los administradores pueden ver esta página.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">API Keys</h1>
          <p className="text-muted-foreground">Gestiona las claves de acceso a la API</p>
        </div>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="mr-2 h-4 w-4" /> Generar API Key
        </Button>
      </div>

      {/* Table */}
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Prefijo</TableHead>
                <TableHead>Nombre</TableHead>
                <TableHead>Permisos</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead>Último uso</TableHead>
                <TableHead>Creada</TableHead>
                <TableHead className="text-right">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading
                ? Array.from({ length: 3 }).map((_, i) => (
                    <TableRow key={i}>
                      {Array.from({ length: 7 }).map((_, j) => (
                        <TableCell key={j}>
                          <div className="h-4 bg-muted animate-pulse rounded w-20" />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                : keys.map((k) => (
                    <TableRow key={k.id}>
                      <TableCell>
                        <span className="font-mono text-xs bg-muted px-2 py-0.5 rounded">
                          {k.keyPrefix}…
                        </span>
                      </TableCell>
                      <TableCell className="font-medium">{k.name}</TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-1">
                          {k.permissions.map((p) => (
                            <Badge key={p} variant="outline" className="text-xs capitalize">
                              {permissionLabels[p as Permission] ?? p}
                            </Badge>
                          ))}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={k.isActive ? 'default' : 'secondary'}>
                          {k.isActive ? 'Activa' : 'Revocada'}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {formatDate(k.lastUsedAt)}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {formatDate(k.createdAt)}
                      </TableCell>
                      <TableCell className="text-right">
                        {k.isActive && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleRevoke(k)}
                            className="text-destructive hover:text-destructive"
                            title="Revocar"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
              {!isLoading && keys.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground py-8">
                    No hay API Keys generadas
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Generar API Key</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="key-name">Nombre</Label>
              <Input
                id="key-name"
                placeholder="Mi integración"
                value={keyName}
                onChange={(e) => setKeyName(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label>Permisos</Label>
              <div className="space-y-2">
                {ALL_PERMISSIONS.map((p) => (
                  <label
                    key={p}
                    className="flex items-center gap-3 rounded-lg border px-3 py-2 cursor-pointer hover:bg-muted transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={selectedPerms.has(p)}
                      onChange={() => togglePerm(p)}
                      className="rounded"
                    />
                    <div>
                      <p className="text-sm font-medium">{permissionLabels[p]}</p>
                      <p className="text-xs text-muted-foreground">
                        {p === 'emit' && 'Permite emitir comprobantes electrónicos'}
                        {p === 'query' && 'Permite consultar y listar comprobantes'}
                        {p === 'void' && 'Permite anular comprobantes emitidos'}
                      </p>
                    </div>
                  </label>
                ))}
              </div>
            </div>
            <div className="flex gap-2 pt-2">
              <Button variant="outline" className="flex-1" onClick={() => setCreateOpen(false)}>
                Cancelar
              </Button>
              <Button className="flex-1" onClick={handleCreate} disabled={creating}>
                {creating ? 'Generando…' : 'Generar'}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Reveal Key Dialog */}
      <Dialog open={revealOpen} onOpenChange={handleRevealClose}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>¡API Key generada!</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="flex items-start gap-3 rounded-lg bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 p-4">
              <AlertCircle className="h-5 w-5 text-amber-600 shrink-0 mt-0.5" />
              <p className="text-sm text-amber-800 dark:text-amber-200">
                <strong>Guardá esta clave ahora.</strong> Por seguridad, no podremos mostrártela de nuevo.
              </p>
            </div>
            <div className="space-y-1.5">
              <Label>Tu API Key</Label>
              <div className="flex gap-2">
                <code className="flex-1 block rounded-lg bg-muted px-3 py-2 text-sm font-mono break-all select-all">
                  {newPlainKey}
                </code>
                <Button variant="outline" size="sm" onClick={handleCopy} className="shrink-0">
                  {copied ? (
                    <CheckCircle2 className="h-4 w-4 text-green-500" />
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
    </div>
  );
}
