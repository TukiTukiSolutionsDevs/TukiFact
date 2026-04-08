'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Plus, Pencil, Trash2, ShieldAlert } from 'lucide-react';
import { toast } from 'sonner';
import { format } from 'date-fns';

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

const roleVariant: Record<string, 'default' | 'secondary' | 'outline'> = {
  admin: 'default',
  emisor: 'secondary',
  consulta: 'outline',
};

function formatDate(iso: string | null) {
  if (!iso) return '—';
  try {
    return format(new Date(iso), 'dd/MM/yyyy HH:mm');
  } catch {
    return '—';
  }
}

export default function UsersPage() {
  const { user: me } = useAuth();
  const [users, setUsers] = useState<UserRecord[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // Create dialog
  const [createOpen, setCreateOpen] = useState(false);
  const [creating, setCreating] = useState(false);
  const [createForm, setCreateForm] = useState({
    email: '',
    password: '',
    fullName: '',
    role: 'emisor' as Role,
  });

  // Edit dialog
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
    if (!createForm.email || !createForm.password) {
      toast.error('Email y contraseña son requeridos');
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
    setEditRole(u.role as Role);
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
    if (!confirm(`¿Desactivar al usuario ${u.email}?`)) return;
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
          <h1 className="text-2xl font-bold tracking-tight">Usuarios</h1>
          <p className="text-muted-foreground">Gestiona los usuarios de tu organización</p>
        </div>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="mr-2 h-4 w-4" /> Crear Usuario
        </Button>
      </div>

      {/* Table */}
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Email</TableHead>
                <TableHead>Nombre</TableHead>
                <TableHead>Rol</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead>Último acceso</TableHead>
                <TableHead>Creado</TableHead>
                <TableHead className="text-right">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading
                ? Array.from({ length: 4 }).map((_, i) => (
                    <TableRow key={i}>
                      {Array.from({ length: 7 }).map((_, j) => (
                        <TableCell key={j}>
                          <div className="h-4 bg-muted animate-pulse rounded w-20" />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                : users.map((u) => (
                    <TableRow key={u.id}>
                      <TableCell className="font-medium">{u.email}</TableCell>
                      <TableCell>{u.fullName ?? '—'}</TableCell>
                      <TableCell>
                        <Badge variant={roleVariant[u.role] ?? 'outline'} className="capitalize">
                          {u.role}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant={u.isActive ? 'default' : 'secondary'}>
                          {u.isActive ? 'Activo' : 'Inactivo'}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {formatDate(u.lastLoginAt)}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {formatDate(u.createdAt)}
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => openEdit(u)}
                            title="Editar rol"
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleToggleActive(u)}
                            title={u.isActive ? 'Desactivar' : 'Activar'}
                          >
                            <Badge variant={u.isActive ? 'destructive' : 'default'} className="cursor-pointer">
                              {u.isActive ? 'Desactivar' : 'Activar'}
                            </Badge>
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(u)}
                            title="Eliminar"
                            className="text-destructive hover:text-destructive"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
              {!isLoading && users.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground py-8">
                    No hay usuarios registrados
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
            <DialogTitle>Crear Usuario</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="usuario@empresa.com"
                value={createForm.email}
                onChange={(e) => setCreateForm((f) => ({ ...f, email: e.target.value }))}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="password">Contraseña</Label>
              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                value={createForm.password}
                onChange={(e) => setCreateForm((f) => ({ ...f, password: e.target.value }))}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="fullName">Nombre completo</Label>
              <Input
                id="fullName"
                placeholder="Juan Pérez"
                value={createForm.fullName}
                onChange={(e) => setCreateForm((f) => ({ ...f, fullName: e.target.value }))}
              />
            </div>
            <div className="space-y-1.5">
              <Label>Rol</Label>
              <Select
                value={createForm.role}
                onValueChange={(v) => v != null && setCreateForm((f) => ({ ...f, role: v as Role }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ROLES.map((r) => (
                    <SelectItem key={r} value={r} className="capitalize">
                      {r}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex gap-2 pt-2">
              <Button variant="outline" className="flex-1" onClick={() => setCreateOpen(false)}>
                Cancelar
              </Button>
              <Button className="flex-1" onClick={handleCreate} disabled={creating}>
                {creating ? 'Creando…' : 'Crear Usuario'}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Edit Role Dialog */}
      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Editar Rol</DialogTitle>
          </DialogHeader>
          {editUser && (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Usuario: <span className="font-medium text-foreground">{editUser.email}</span>
              </p>
              <div className="space-y-1.5">
                <Label>Nuevo Rol</Label>
                <Select
                  value={editRole}
                  onValueChange={(v) => v != null && setEditRole(v as Role)}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {ROLES.map((r) => (
                      <SelectItem key={r} value={r} className="capitalize">
                        {r}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="flex gap-2 pt-2">
                <Button variant="outline" className="flex-1" onClick={() => setEditOpen(false)}>
                  Cancelar
                </Button>
                <Button className="flex-1" onClick={handleEditSave} disabled={saving}>
                  {saving ? 'Guardando…' : 'Guardar'}
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
