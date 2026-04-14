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
import { Search, Plus, Package, Pencil, Trash2, ChevronLeft, ChevronRight } from 'lucide-react';
import { toast } from 'sonner';

interface Product {
  id: string;
  code: string;
  sunatCode: string | null;
  description: string;
  unitPrice: number;
  unitPriceWithIgv: number;
  currency: string;
  igvType: string;
  unitMeasure: string;
  category: string | null;
  brand: string | null;
  isActive: boolean;
  createdAt: string;
}

const EMPTY_FORM = {
  code: '', description: '', unitPrice: '', unitPriceWithIgv: '',
  sunatCode: '', currency: 'PEN', igvType: '10', unitMeasure: 'NIU',
  category: '', brand: '',
};

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [pagination, setPagination] = useState({ page: 1, pageSize: 50, totalCount: 0, totalPages: 0 });
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);
  const [editId, setEditId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const fetchProducts = useCallback(async (page: number) => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '50' });
      if (search) params.set('search', search);
      const res = await api.get<PaginatedResponse<Product>>(`/v1/products?${params}`);
      setProducts(res.data);
      setPagination(res.pagination);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  }, [search]);

  useEffect(() => { fetchProducts(1); }, [fetchProducts]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const body = {
        code: form.code, description: form.description,
        unitPrice: parseFloat(form.unitPrice), unitPriceWithIgv: parseFloat(form.unitPriceWithIgv),
        sunatCode: form.sunatCode || null, currency: form.currency,
        igvType: form.igvType, unitMeasure: form.unitMeasure,
        category: form.category || null, brand: form.brand || null,
      };

      if (editId) {
        await api.put(`/v1/products/${editId}`, body);
        toast.success('Producto actualizado');
      } else {
        await api.post('/v1/products', body);
        toast.success('Producto creado');
      }
      setDialogOpen(false);
      setForm(EMPTY_FORM);
      setEditId(null);
      fetchProducts(pagination.page);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally { setSaving(false); }
  };

  const handleEdit = (p: Product) => {
    setEditId(p.id);
    setForm({
      code: p.code, description: p.description,
      unitPrice: String(p.unitPrice), unitPriceWithIgv: String(p.unitPriceWithIgv),
      sunatCode: p.sunatCode ?? '', currency: p.currency,
      igvType: p.igvType, unitMeasure: p.unitMeasure,
      category: p.category ?? '', brand: p.brand ?? '',
    });
    setDialogOpen(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Eliminar producto?')) return;
    try {
      await api.delete(`/v1/products/${id}`);
      toast.success('Producto eliminado');
      fetchProducts(pagination.page);
    } catch (err) { toast.error(err instanceof Error ? err.message : 'Error'); }
  };

  const igvLabel = (t: string) => ({ '10': 'Gravado', '20': 'Exonerado', '30': 'Inafecto' })[t] ?? t;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Catálogo de Productos</h1>
          <p className="text-sm text-muted-foreground">{pagination.totalCount} producto(s)</p>
        </div>
        <Dialog open={dialogOpen} onOpenChange={(o) => { setDialogOpen(o); if (!o) { setEditId(null); setForm(EMPTY_FORM); } }}>
          <DialogTrigger render={<Button />}>
            <Plus className="h-4 w-4 mr-2" /> Nuevo Producto
          </DialogTrigger>
          <DialogContent className="max-w-lg">
            <DialogHeader>
              <DialogTitle>{editId ? 'Editar' : 'Nuevo'} Producto</DialogTitle>
              <DialogDescription>Complete los datos del producto o servicio</DialogDescription>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label>Código (SKU)</Label>
                  <Input value={form.code} onChange={(e) => setForm(f => ({ ...f, code: e.target.value }))} required disabled={!!editId} />
                </div>
                <div className="space-y-1">
                  <Label>Código SUNAT</Label>
                  <Input value={form.sunatCode} onChange={(e) => setForm(f => ({ ...f, sunatCode: e.target.value }))} placeholder="Opcional" />
                </div>
              </div>
              <div className="space-y-1">
                <Label>Descripción</Label>
                <Input value={form.description} onChange={(e) => setForm(f => ({ ...f, description: e.target.value }))} required />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label>Precio sin IGV</Label>
                  <Input type="number" step="0.01" value={form.unitPrice} onChange={(e) => setForm(f => ({ ...f, unitPrice: e.target.value }))} required />
                </div>
                <div className="space-y-1">
                  <Label>Precio con IGV</Label>
                  <Input type="number" step="0.01" value={form.unitPriceWithIgv} onChange={(e) => setForm(f => ({ ...f, unitPriceWithIgv: e.target.value }))} required />
                </div>
              </div>
              <div className="grid grid-cols-3 gap-3">
                <div className="space-y-1">
                  <Label>Tipo IGV</Label>
                  <select className="w-full rounded-md border bg-background px-3 py-2 text-sm" value={form.igvType} onChange={(e) => setForm(f => ({ ...f, igvType: e.target.value }))}>
                    <option value="10">Gravado</option>
                    <option value="20">Exonerado</option>
                    <option value="30">Inafecto</option>
                  </select>
                </div>
                <div className="space-y-1">
                  <Label>Unidad</Label>
                  <select className="w-full rounded-md border bg-background px-3 py-2 text-sm" value={form.unitMeasure} onChange={(e) => setForm(f => ({ ...f, unitMeasure: e.target.value }))}>
                    <option value="NIU">Unidad (NIU)</option>
                    <option value="ZZ">Servicio (ZZ)</option>
                    <option value="KGM">Kilogramo</option>
                    <option value="LTR">Litro</option>
                    <option value="MTR">Metro</option>
                  </select>
                </div>
                <div className="space-y-1">
                  <Label>Moneda</Label>
                  <select className="w-full rounded-md border bg-background px-3 py-2 text-sm" value={form.currency} onChange={(e) => setForm(f => ({ ...f, currency: e.target.value }))}>
                    <option value="PEN">PEN</option>
                    <option value="USD">USD</option>
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label>Categoría</Label>
                  <Input value={form.category} onChange={(e) => setForm(f => ({ ...f, category: e.target.value }))} placeholder="Opcional" />
                </div>
                <div className="space-y-1">
                  <Label>Marca</Label>
                  <Input value={form.brand} onChange={(e) => setForm(f => ({ ...f, brand: e.target.value }))} placeholder="Opcional" />
                </div>
              </div>
              <Button type="submit" className="w-full" disabled={saving}>
                {saving ? 'Guardando...' : editId ? 'Actualizar' : 'Crear Producto'}
              </Button>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <div className="flex gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input placeholder="Buscar por código o descripción..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-9"
            onKeyDown={(e) => e.key === 'Enter' && fetchProducts(1)} />
        </div>
      </div>

      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="p-8 text-center"><div className="h-6 w-6 animate-spin rounded-full border-2 border-blue-500 border-t-transparent mx-auto" /></div>
          ) : products.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground">
              <Package className="h-10 w-10 mx-auto mb-2 opacity-50" />
              <p>No hay productos</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Código</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">Descripción</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase">Precio</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase">IGV</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase">Unidad</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {products.map((p) => (
                    <tr key={p.id} className="hover:bg-muted/50 transition-colors">
                      <td className="px-4 py-3 font-mono text-xs">{p.code}</td>
                      <td className="px-4 py-3">
                        <p className="font-medium">{p.description}</p>
                        {p.category && <p className="text-xs text-muted-foreground">{p.category}{p.brand ? ` / ${p.brand}` : ''}</p>}
                      </td>
                      <td className="px-4 py-3 text-right font-mono">
                        S/ {p.unitPriceWithIgv.toLocaleString('es-PE', { minimumFractionDigits: 2 })}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <span className="inline-flex items-center rounded-md bg-muted px-2 py-0.5 text-xs">{igvLabel(p.igvType)}</span>
                      </td>
                      <td className="px-4 py-3 text-center text-xs">{p.unitMeasure}</td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          <Button variant="ghost" size="sm" onClick={() => handleEdit(p)}><Pencil className="h-3 w-3" /></Button>
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(p.id)} className="text-destructive"><Trash2 className="h-3 w-3" /></Button>
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
            <Button variant="outline" size="sm" disabled={pagination.page <= 1} onClick={() => fetchProducts(pagination.page - 1)}><ChevronLeft className="h-4 w-4" /></Button>
            <Button variant="outline" size="sm" disabled={pagination.page >= pagination.totalPages} onClick={() => fetchProducts(pagination.page + 1)}><ChevronRight className="h-4 w-4" /></Button>
          </div>
        </div>
      )}
    </div>
  );
}
