'use client';

import { useEffect, useState, useCallback } from 'react';
import { api, type PaginatedResponse } from '@/lib/api';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Search, Plus, Users, Pencil, Trash2, ChevronLeft, ChevronRight } from 'lucide-react';
import { toast } from 'sonner';

interface Customer {
  id: string;
  docType: string;
  docNumber: string;
  name: string;
  email: string | null;
  phone: string | null;
  address: string | null;
  category: string | null;
  isActive: boolean;
  createdAt: string;
}

const EMPTY_FORM = {
  docType: '6', docNumber: '', name: '',
  email: '', phone: '', address: '',
  category: '', notes: '',
};

export default function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [pagination, setPagination] = useState({ page: 1, pageSize: 50, totalCount: 0, totalPages: 0 });
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);
  const [editId, setEditId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const fetchCustomers = useCallback(async (page: number) => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '50' });
      if (search) params.set('search', search);
      const res = await api.get<PaginatedResponse<Customer>>(`/v1/customers?${params}`);
      setCustomers(res.data);
      setPagination(res.pagination);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  }, [search]);

  useEffect(() => { fetchCustomers(1); }, [fetchCustomers]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const body = {
        docType: form.docType, docNumber: form.docNumber, name: form.name,
        email: form.email || null, phone: form.phone || null,
        address: form.address || null, category: form.category || null,
        notes: form.notes || null,
      };

      if (editId) {
        await api.put(`/v1/customers/${editId}`, body);
        toast.success('Cliente actualizado');
      } else {
        await api.post('/v1/customers', body);
        toast.success('Cliente creado');
      }
      setDialogOpen(false);
      setForm(EMPTY_FORM);
      setEditId(null);
      fetchCustomers(pagination.page);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally { setSaving(false); }
  };

  const handleEdit = (c: Customer) => {
    setEditId(c.id);
    setForm({
      docType: c.docType, docNumber: c.docNumber, name: c.name,
      email: c.email ?? '', phone: c.phone ?? '', address: c.address ?? '',
      category: c.category ?? '', notes: '',
    });
    setDialogOpen(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Eliminar cliente?')) return;
    try {
      await api.delete(`/v1/customers/${id}`);
      toast.success('Cliente eliminado');
      fetchCustomers(pagination.page);
    } catch (err) { toast.error(err instanceof Error ? err.message : 'Error'); }
  };

  const docTypeLabel = (t: string) => ({ '6': 'RUC', '1': 'DNI', '4': 'CE', '7': 'Pasaporte', '0': 'Sin doc' })[t] ?? t;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Directorio de Clientes</h1>
          <p className="text-sm text-muted-foreground">{pagination.totalCount} cliente(s)</p>
        </div>
        <Dialog open={dialogOpen} onOpenChange={(o) => { setDialogOpen(o); if (!o) { setEditId(null); setForm(EMPTY_FORM); } }}>
          <DialogTrigger render={<Button />}>
            <Plus className="h-4 w-4 mr-2" /> Nuevo Cliente
          </DialogTrigger>
          <DialogContent className="max-w-lg">
            <DialogHeader>
              <DialogTitle>{editId ? 'Editar' : 'Nuevo'} Cliente</DialogTitle>
              <DialogDescription>Datos del cliente o proveedor</DialogDescription>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-3 gap-3">
                <div className="space-y-1">
                  <Label>Tipo Doc.</Label>
                  <select className="w-full rounded-md border bg-background px-3 py-2 text-sm" value={form.docType}
                    onChange={(e) => setForm(f => ({ ...f, docType: e.target.value }))}>
                    <option value="6">RUC</option>
                    <option value="1">DNI</option>
                    <option value="4">CE</option>
                    <option value="7">Pasaporte</option>
                    <option value="0">Sin doc.</option>
                  </select>
                </div>
                <div className="col-span-2 space-y-1">
                  <Label>N° Documento</Label>
                  <Input value={form.docNumber} onChange={(e) => setForm(f => ({ ...f, docNumber: e.target.value }))} required disabled={!!editId} />
                </div>
              </div>
              <div className="space-y-1">
                <Label>Nombre / Razón Social</Label>
                <Input value={form.name} onChange={(e) => setForm(f => ({ ...f, name: e.target.value }))} required />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label>Email</Label>
                  <Input type="email" value={form.email} onChange={(e) => setForm(f => ({ ...f, email: e.target.value }))} placeholder="Opcional" />
                </div>
                <div className="space-y-1">
                  <Label>Teléfono</Label>
                  <Input value={form.phone} onChange={(e) => setForm(f => ({ ...f, phone: e.target.value }))} placeholder="Opcional" />
                </div>
              </div>
              <div className="space-y-1">
                <Label>Dirección</Label>
                <Input value={form.address} onChange={(e) => setForm(f => ({ ...f, address: e.target.value }))} placeholder="Opcional" />
              </div>
              <div className="space-y-1">
                <Label>Categoría</Label>
                <Input value={form.category} onChange={(e) => setForm(f => ({ ...f, category: e.target.value }))} placeholder="VIP, Regular, etc." />
              </div>
              <Button type="submit" className="w-full" disabled={saving}>
                {saving ? 'Guardando...' : editId ? 'Actualizar' : 'Crear Cliente'}
              </Button>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <div className="flex gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input placeholder="Buscar por nombre o documento..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-9"
            onKeyDown={(e) => e.key === 'Enter' && fetchCustomers(1)} />
        </div>
      </div>

      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="p-8 text-center"><div className="h-6 w-6 animate-spin rounded-full border-2 border-blue-500 border-t-transparent mx-auto" /></div>
          ) : customers.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground">
              <Users className="h-10 w-10 mx-auto mb-2 opacity-50" />
              <p>No hay clientes</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Documento</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Nombre</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Email</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Teléfono</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase">Categoría</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {customers.map((c) => (
                    <tr key={c.id} className="hover:bg-muted/50 transition-colors">
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center rounded-md bg-muted px-1.5 py-0.5 text-xs mr-1">{docTypeLabel(c.docType)}</span>
                        <span className="font-mono text-xs">{c.docNumber}</span>
                      </td>
                      <td className="px-4 py-3 font-medium">{c.name}</td>
                      <td className="px-4 py-3 text-muted-foreground text-sm">{c.email ?? '—'}</td>
                      <td className="px-4 py-3 text-muted-foreground text-sm">{c.phone ?? '—'}</td>
                      <td className="px-4 py-3 text-center">
                        {c.category ? <span className="inline-flex items-center rounded-full bg-blue-50 dark:bg-blue-950 px-2 py-0.5 text-xs text-blue-700 dark:text-blue-300">{c.category}</span> : '—'}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          <Button variant="ghost" size="sm" onClick={() => handleEdit(c)}><Pencil className="h-3 w-3" /></Button>
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(c.id)} className="text-destructive"><Trash2 className="h-3 w-3" /></Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {pagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">Página {pagination.page} de {pagination.totalPages}</p>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={pagination.page <= 1} onClick={() => fetchCustomers(pagination.page - 1)}><ChevronLeft className="h-4 w-4" /></Button>
            <Button variant="outline" size="sm" disabled={pagination.page >= pagination.totalPages} onClick={() => fetchCustomers(pagination.page + 1)}><ChevronRight className="h-4 w-4" /></Button>
          </div>
        </div>
      )}
    </div>
  );
}
