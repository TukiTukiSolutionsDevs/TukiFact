'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Section } from '@/components/ui/section';
import { PillGroup } from '@/components/ui/pill-group';
import { StatusBadge } from '@/components/ui/status-badge';
import {
  Plus,
  Pencil,
  Trash2,
  ShieldAlert,
  Loader2,
  Crown,
  FilePlus,
  BarChart3,
  Mail,
  Power,
} from 'lucide-react';
import { toast } from 'sonner';

interface UserRecord {
  id: string;
  email: string;
  fullName: string | null;
  role: string;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

const ROLES = ['admin', 'emisor', 'consulta'] as const;
type Role = (typeof ROLES)[number];

const ROLE_OPTIONS = [
  { value: 'admin' as const, label: 'Administrador', sub: 'Acceso total, incluye usuarios y facturación', icon: Crown },
  { value: 'emisor' as const, label: 'Emisor', sub: 'Emite y gestiona comprobantes', icon: FilePlus },
  { value: 'consulta' as const, label: 'Consulta', sub: 'Solo lectura y reportes', icon: BarChart3 },
];

const ROLE_LABEL: Record<string, string> = {
  admin: 'Administrador',
  emisor: 'Emisor',
  consulta: 'Consulta',
};

const formatDate = (iso: string | null) => {
  if (!iso) return '—';
  try {
    return new Date(iso).toLocaleString('es-PE', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return '—';
  }
};

const initialsFor = (u: UserRecord) =>
  (u.fullName || u.email)
    .split(/[\s@.]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0])
    .join('')
    .toUpperCase();

export default function UsersPage() {
  const { user: me } = useAuth();
  const [users, setUsers] = useState<UserRecord[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [creating, setCreating] = useState(false);
  const [createForm, setCreateForm] = useState({
    email: '',
    password: '',
    fullName: '',
    role: 'emisor' as Role,
  });

  const [editOpen, setEditOpen] = useState(false);
  const [editUser, setEditUser] = useState<UserRecord | null>(null);
  const [editRole, setEditRole] = useState<Role>('emisor');
  const [saving, setSaving] = useState(false);

  const isAdmin = me?.role === 'admin';

  const fetchUsers = async () => {
    setIsLoading(true);
    try {
      const data = await api.get<UserRecord[]>('/v1/users');
      setUsers(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al cargar usuarios');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (isAdmin) fetchUsers();
    else setIsLoading(false);
  }, [isAdmin]);

  const handleCreate = async () => {
    if (!createForm.email.trim() || !createForm.password) {
      toast.error('Email y contraseña son obligatorios');
      return;
    }
    setCreating(true);
    try {
      await api.post('/v1/users', createForm);
      toast.success(`Usuario ${createForm.email} creado`);
      setCreateOpen(false);
      setCreateForm({ email: '', password: '', fullName: '', role: 'emisor' });
      fetchUsers();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al crear usuario');
    } finally {
      setCreating(false);
    }
  };

  const openEdit = (u: UserRecord) => {
    setEditUser(u);
    setEditRole((u.role as Role) || 'emisor');
    setEditOpen(true);
  };

  const handleEditSave = async () => {
    if (!editUser) return;
    setSaving(true);
    try {
      await api.put(`/v1/users/${editUser.id}`, { role: editRole });
      toast.success('Rol actualizado');
      setEditOpen(false);
      fetchUsers();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al actualizar');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleActive = async (u: UserRecord) => {
    try {
      await api.put(`/v1/users/${u.id}`, { isActive: !u.isActive });
      toast.success(u.isActive ? 'Usuario desactivado' : 'Usuario activado');
      fetchUsers();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  const handleDelete = async (u: UserRecord) => {
    if (!confirm(`¿Eliminar al usuario "${u.email}"? Esta acción no se puede deshacer.`)) return;
    try {
      await api.delete(`/v1/users/${u.id}`);
      toast.success(`Usuario ${u.email} eliminado`);
      fetchUsers();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al eliminar');
    }
  };

  if (!isAdmin) {
    return (
      <div className="max-w-md mx-auto text-center py-24">
        <div
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
          style={{
            background: 'color-mix(in oklch, var(--danger) 14%, transparent)',
            color: 'var(--danger)',
          }}
        >
          <ShieldAlert className="h-8 w-8" />
        </div>
        <h1 className="t-h1 m-0">Acceso restringido</h1>
        <p className="t-body mt-2 m-0" style={{ color: 'var(--muted-foreground)' }}>
          Solo los administradores del tenant pueden gestionar usuarios.
        </p>
      </div>
    );
  }

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Usuarios</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            {users.length === 1
              ? '1 usuario en tu organización.'
              : `${users.length} usuarios en tu organización.`}
          </p>
        </div>
        <Button
          onClick={() => setCreateOpen(true)}
          style={{
            background: 'var(--accent)',
            color: 'var(--accent-foreground)',
            fontWeight: 600,
          }}
        >
          <Plus className="h-4 w-4 mr-2" /> Crear usuario
        </Button>
      </div>

      {/* Table */}
      <section
        className="rounded-[var(--radius-lg)] border bg-card overflow-hidden mb-[var(--gap-cards)]"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        {isLoading ? (
          <div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span className="t-body-sm">Cargando usuarios…</span>
          </div>
        ) : users.length === 0 ? (
          <div className="p-10 text-center">
            <p className="t-body m-0 font-semibold">Aún no hay usuarios</p>
            <p
              className="t-body-sm mt-1 mb-4"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Invita a tu equipo para que pueda emitir y consultar comprobantes.
            </p>
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4 mr-2" /> Crear primer usuario
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
                  <th className="text-left py-2.5 pl-6 pr-2">Usuario</th>
                  <th className="text-left py-2.5 px-2 w-32">Rol</th>
                  <th className="text-left py-2.5 px-2 w-28">Estado</th>
                  <th className="text-left py-2.5 px-2 w-44">Último acceso</th>
                  <th className="text-left py-2.5 px-2 w-36">Creado</th>
                  <th className="py-2.5 pr-6 pl-2 w-32" aria-label="acciones" />
                </tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.id} style={{ borderTop: '1px solid var(--border)' }}>
                    <td className="py-3 pl-6 pr-2">
                      <div className="flex items-center gap-2.5">
                        <span
                          className="flex h-9 w-9 items-center justify-center rounded-[var(--radius-md)] shrink-0 text-[12px] font-bold"
                          style={{
                            background:
                              u.role === 'admin'
                                ? 'color-mix(in oklch, var(--brand-toucan-orange) 14%, transparent)'
                                : u.role === 'emisor'
                                  ? 'color-mix(in oklch, var(--info) 14%, transparent)'
                                  : 'var(--muted)',
                            color:
                              u.role === 'admin'
                                ? 'var(--brand-toucan-orange)'
                                : u.role === 'emisor'
                                  ? 'var(--info)'
                                  : 'var(--muted-foreground)',
                            letterSpacing: '-0.02em',
                          }}
                        >
                          {initialsFor(u)}
                        </span>
                        <div className="min-w-0">
                          <div className="t-body-sm font-semibold truncate">
                            {u.fullName ?? u.email}
                          </div>
                          {u.fullName && (
                            <div
                              className="t-caption truncate"
                              style={{ color: 'var(--muted-foreground)' }}
                            >
                              {u.email}
                            </div>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="py-3 px-2">
                      <span
                        className="inline-flex items-center rounded-full px-2.5 py-0.5 t-caption font-semibold"
                        style={{
                          background:
                            u.role === 'admin'
                              ? 'color-mix(in oklch, var(--brand-toucan-orange) 14%, transparent)'
                              : 'var(--muted)',
                          color:
                            u.role === 'admin'
                              ? 'var(--brand-toucan-orange)'
                              : 'var(--muted-foreground)',
                        }}
                      >
                        {ROLE_LABEL[u.role] ?? u.role}
                      </span>
                    </td>
                    <td className="py-3 px-2">
                      <StatusBadge
                        status={u.isActive ? 'active' : 'paused'}
                        label={u.isActive ? 'Activo' : 'Inactivo'}
                      />
                    </td>
                    <td className="py-3 px-2 mono tnum t-caption" style={{ color: 'var(--muted-foreground)' }}>
                      {formatDate(u.lastLoginAt)}
                    </td>
                    <td className="py-3 px-2 mono tnum t-caption" style={{ color: 'var(--muted-foreground)' }}>
                      {formatDate(u.createdAt)}
                    </td>
                    <td className="py-3 pr-6 pl-2">
                      <div className="flex justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => openEdit(u)}
                          title="Editar rol"
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleToggleActive(u)}
                          title={u.isActive ? 'Desactivar' : 'Activar'}
                        >
                          <Power className="h-3.5 w-3.5" />
                        </Button>
                        {u.id !== me?.id && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(u)}
                            title="Eliminar"
                            style={{ color: 'var(--danger)' }}
                          >
                            <Trash2 className="h-3.5 w-3.5" />
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

      {/* Create dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Crear usuario</DialogTitle>
            <DialogDescription>
              Define el rol del usuario para limitar lo que puede hacer dentro de TukiFact.
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-5">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Nombre completo</Label>
                <Input
                  value={createForm.fullName}
                  onChange={(e) => setCreateForm((f) => ({ ...f, fullName: e.target.value }))}
                  placeholder="Juan Pérez García"
                />
              </div>
              <div>
                <Label className="t-label mb-1.5 block">Email</Label>
                <Input
                  type="email"
                  value={createForm.email}
                  onChange={(e) => setCreateForm((f) => ({ ...f, email: e.target.value }))}
                  placeholder="persona@empresa.pe"
                />
              </div>
            </div>

            <div>
              <Label className="t-label mb-1.5 block">Contraseña temporal</Label>
              <Input
                type="password"
                value={createForm.password}
                onChange={(e) => setCreateForm((f) => ({ ...f, password: e.target.value }))}
                placeholder="••••••••"
              />
              <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                El usuario podrá cambiarla en su próximo inicio de sesión.
              </p>
            </div>

            <div>
              <Label className="t-label mb-2 block">Rol</Label>
              <PillGroup
                value={createForm.role}
                onChange={(v) => setCreateForm((f) => ({ ...f, role: v }))}
                options={ROLE_OPTIONS}
                cols={3}
              />
            </div>

            <div className="flex gap-2 pt-2">
              <Button variant="outline" className="flex-1" onClick={() => setCreateOpen(false)}>
                Cancelar
              </Button>
              <Button className="flex-1" onClick={handleCreate} disabled={creating}>
                {creating ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Creando…
                  </>
                ) : (
                  <>
                    <Mail className="h-4 w-4 mr-2" /> Crear usuario
                  </>
                )}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Edit role dialog */}
      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>Editar rol</DialogTitle>
            {editUser && (
              <DialogDescription>
                Cambia el rol de <strong>{editUser.fullName ?? editUser.email}</strong>. El usuario
                verá el cambio en su próximo refresh.
              </DialogDescription>
            )}
          </DialogHeader>

          <div className="flex flex-col gap-4">
            <PillGroup
              value={editRole}
              onChange={setEditRole}
              options={ROLE_OPTIONS}
              cols={3}
            />
            <div className="flex gap-2 pt-2">
              <Button variant="outline" className="flex-1" onClick={() => setEditOpen(false)}>
                Cancelar
              </Button>
              <Button className="flex-1" onClick={handleEditSave} disabled={saving}>
                {saving ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Guardando…
                  </>
                ) : (
                  'Guardar rol'
                )}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
