'use client';

import { useEffect, useState, useCallback, useRef } from 'react';
import { api, type PaginatedResponse } from '@/lib/api';
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Search,
  Plus,
  Package,
  Pencil,
  Trash2,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Inbox,
  Wrench,
  Scale,
  Beaker,
  Ruler,
  Boxes,
  CheckCircle2,
  Ban,
  Shield,
  Tag,
  Hash,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

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
  code: '',
  description: '',
  unitPrice: '',
  unitPriceWithIgv: '',
  sunatCode: '',
  currency: 'PEN',
  igvType: '10',
  unitMeasure: 'NIU',
  category: '',
  brand: '',
};

const IGV_TYPES = [
  { value: '10', label: 'Gravado', sub: 'Aplica IGV 18%', icon: CheckCircle2 },
  { value: '20', label: 'Exonerado', sub: 'Sin IGV', icon: Ban },
  { value: '30', label: 'Inafecto', sub: 'Fuera del ámbito', icon: Shield },
] as const;

const CURRENCIES = [
  { value: 'PEN', label: 'PEN', sub: 'Soles', icon: () => <span className="font-bold">S/</span> },
  { value: 'USD', label: 'USD', sub: 'Dólares', icon: () => <span className="font-bold">$</span> },
] as const;

const UNITS: { value: string; label: string; icon: React.ElementType }[] = [
  { value: 'NIU', label: 'Unidad (NIU)', icon: Boxes },
  { value: 'ZZ', label: 'Servicio (ZZ)', icon: Wrench },
  { value: 'KGM', label: 'Kilogramo (KGM)', icon: Scale },
  { value: 'LTR', label: 'Litro (LTR)', icon: Beaker },
  { value: 'MTR', label: 'Metro (MTR)', icon: Ruler },
];

const IGV_RATE = 0.18;

const igvLabel: Record<string, string> = {
  '10': 'Gravado',
  '20': 'Exonerado',
  '30': 'Inafecto',
};

function PillGroup<T extends string>({
  value,
  onChange,
  options,
  cols = 2,
}: {
  value: T;
  onChange: (v: T) => void;
  options: readonly { value: T; label: string; sub?: string; icon: React.ElementType }[];
  cols?: 2 | 3;
}) {
  const colsClass = cols === 3 ? 'grid-cols-3' : 'grid-cols-2';
  return (
    <div className={cn('grid gap-2', colsClass)}>
      {options.map((o) => {
        const Icon = o.icon;
        const active = value === o.value;
        return (
          <button
            key={o.value}
            type="button"
            onClick={() => onChange(o.value)}
            className="relative flex items-center gap-2 rounded-[var(--radius-md)] border px-3 py-2 transition-colors text-left min-w-0"
            style={{
              background: active ? 'color-mix(in oklch, var(--accent) 18%, transparent)' : 'var(--card)',
              borderColor: active ? 'var(--accent)' : 'var(--border)',
            }}
          >
            <span
              className="flex h-7 w-7 items-center justify-center rounded-md shrink-0"
              style={{
                background: active ? 'var(--accent)' : 'var(--muted)',
                color: active ? 'var(--accent-foreground)' : 'var(--muted-foreground)',
              }}
            >
              <Icon className="h-3.5 w-3.5" />
            </span>
            <span className="min-w-0 flex-1">
              <span className="block t-body-sm font-semibold leading-tight">{o.label}</span>
              {o.sub && (
                <span
                  className="block t-caption leading-tight mt-0.5"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  {o.sub}
                </span>
              )}
            </span>
          </button>
        );
      })}
    </div>
  );
}

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [pagination, setPagination] = useState({ page: 1, pageSize: 50, totalCount: 0, totalPages: 0 });
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);
  const [editId, setEditId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  // Tracks which price field the user is editing so the other one auto-derives.
  const lastEditedPriceRef = useRef<'net' | 'gross' | null>(null);

  // Debounce search → server query (300ms).
  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(t);
  }, [search]);

  const fetchProducts = useCallback(
    async (page: number) => {
      setLoading(true);
      try {
        const params = new URLSearchParams({ page: String(page), pageSize: '50' });
        if (debouncedSearch) params.set('search', debouncedSearch);
        const res = await api.get<PaginatedResponse<Product>>(`/v1/products?${params}`);
        setProducts(res.data);
        setPagination(res.pagination);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    },
    [debouncedSearch]
  );

  useEffect(() => {
    fetchProducts(1);
  }, [fetchProducts]);

  // Auto-bridge net ↔ gross price when IGV type or one of the prices changes.
  useEffect(() => {
    if (!dialogOpen) return;
    const net = parseFloat(form.unitPrice);
    const gross = parseFloat(form.unitPriceWithIgv);
    const rate = form.igvType === '10' ? 1 + IGV_RATE : 1;

    if (lastEditedPriceRef.current === 'net' && !isNaN(net)) {
      const computed = (net * rate).toFixed(2);
      if (computed !== form.unitPriceWithIgv) {
        setForm((f) => ({ ...f, unitPriceWithIgv: computed }));
      }
    } else if (lastEditedPriceRef.current === 'gross' && !isNaN(gross)) {
      const computed = (gross / rate).toFixed(2);
      if (computed !== form.unitPrice) {
        setForm((f) => ({ ...f, unitPrice: computed }));
      }
    }
  }, [form.unitPrice, form.unitPriceWithIgv, form.igvType, dialogOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.code.trim() || !form.description.trim()) {
      toast.error('Completa código y descripción');
      return;
    }
    const net = parseFloat(form.unitPrice);
    const gross = parseFloat(form.unitPriceWithIgv);
    if (isNaN(net) || isNaN(gross) || net < 0 || gross < 0) {
      toast.error('Los precios deben ser números válidos no negativos');
      return;
    }

    setSaving(true);
    try {
      const body = {
        code: form.code.trim(),
        description: form.description.trim(),
        unitPrice: net,
        unitPriceWithIgv: gross,
        sunatCode: form.sunatCode.trim() || null,
        currency: form.currency,
        igvType: form.igvType,
        unitMeasure: form.unitMeasure,
        category: form.category.trim() || null,
        brand: form.brand.trim() || null,
      };

      if (editId) {
        await api.put(`/v1/products/${editId}`, body);
        toast.success('Producto actualizado');
      } else {
        await api.post('/v1/products', body);
        toast.success('Producto creado');
      }
      closeDialog();
      fetchProducts(pagination.page);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (p: Product) => {
    setEditId(p.id);
    setForm({
      code: p.code,
      description: p.description,
      unitPrice: String(p.unitPrice),
      unitPriceWithIgv: String(p.unitPriceWithIgv),
      sunatCode: p.sunatCode ?? '',
      currency: p.currency,
      igvType: p.igvType,
      unitMeasure: p.unitMeasure,
      category: p.category ?? '',
      brand: p.brand ?? '',
    });
    lastEditedPriceRef.current = null;
    setDialogOpen(true);
  };

  const handleDelete = async (id: string, code: string) => {
    if (!confirm(`¿Eliminar el producto "${code}"? Esta acción no se puede deshacer.`)) return;
    try {
      await api.delete(`/v1/products/${id}`);
      toast.success('Producto eliminado');
      fetchProducts(pagination.page);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  const closeDialog = () => {
    setDialogOpen(false);
    setEditId(null);
    setForm(EMPTY_FORM);
    lastEditedPriceRef.current = null;
  };

  const currencySymbol = form.currency === 'USD' ? '$' : 'S/';
  const hasActiveSearch = debouncedSearch.length > 0;
  const hasNoResults = !loading && products.length === 0 && !hasActiveSearch;
  const hasFilteredNoResults = !loading && products.length === 0 && hasActiveSearch;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Catálogo de productos</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            {pagination.totalCount === 1
              ? '1 producto en tu catálogo.'
              : `${pagination.totalCount} productos en tu catálogo.`}
          </p>
        </div>
        <Dialog
          open={dialogOpen}
          onOpenChange={(o) => {
            if (!o) closeDialog();
            else setDialogOpen(true);
          }}
        >
          <DialogTrigger
            render={
              <Button
                style={{
                  background: 'var(--accent)',
                  color: 'var(--accent-foreground)',
                  fontWeight: 600,
                }}
              />
            }
          >
            <Plus className="h-4 w-4 mr-2" /> Nuevo producto
          </DialogTrigger>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>
                {editId ? 'Editar producto' : 'Nuevo producto'}
              </DialogTitle>
              <DialogDescription>
                Datos del producto o servicio. Tipo de IGV y unidad determinan cómo se calcula en
                cada comprobante.
              </DialogDescription>
            </DialogHeader>

            <form onSubmit={handleSubmit} className="flex flex-col gap-5">
              {/* Identificación */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Código (SKU)</Label>
                  <Input
                    value={form.code}
                    onChange={(e) => setForm((f) => ({ ...f, code: e.target.value }))}
                    placeholder="PROD-001"
                    className="mono"
                    required
                    disabled={!!editId}
                  />
                  {editId && (
                    <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                      El SKU no se puede modificar una vez creado.
                    </p>
                  )}
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Código SUNAT (UNSPSC)</Label>
                  <Input
                    value={form.sunatCode}
                    onChange={(e) => setForm((f) => ({ ...f, sunatCode: e.target.value }))}
                    placeholder="Opcional · ej. 78111801"
                    className="mono"
                  />
                </div>
              </div>

              <div>
                <Label className="t-label mb-1.5 block">Descripción</Label>
                <Input
                  value={form.description}
                  onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                  placeholder="Cómo aparece en el comprobante"
                  required
                />
              </div>

              {/* Tipo de IGV */}
              <div>
                <Label className="t-label mb-2 block">Tipo de IGV</Label>
                <PillGroup
                  value={form.igvType}
                  onChange={(v) => setForm((f) => ({ ...f, igvType: v }))}
                  options={IGV_TYPES}
                  cols={3}
                />
              </div>

              {/* Precios con auto-bridge */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Precio sin IGV</Label>
                  <div className="relative">
                    <span
                      className="absolute left-3 top-1/2 -translate-y-1/2 t-body-sm mono pointer-events-none"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {currencySymbol}
                    </span>
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      inputMode="decimal"
                      value={form.unitPrice}
                      onChange={(e) => {
                        lastEditedPriceRef.current = 'net';
                        setForm((f) => ({ ...f, unitPrice: e.target.value }));
                      }}
                      className="mono tnum text-right pl-8"
                      placeholder="0.00"
                      required
                    />
                  </div>
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Precio con IGV</Label>
                  <div className="relative">
                    <span
                      className="absolute left-3 top-1/2 -translate-y-1/2 t-body-sm mono pointer-events-none"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {currencySymbol}
                    </span>
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      inputMode="decimal"
                      value={form.unitPriceWithIgv}
                      onChange={(e) => {
                        lastEditedPriceRef.current = 'gross';
                        setForm((f) => ({ ...f, unitPriceWithIgv: e.target.value }));
                      }}
                      className="mono tnum text-right pl-8"
                      placeholder="0.00"
                      required
                    />
                  </div>
                </div>
              </div>
              <p
                className="t-caption -mt-2"
                style={{ color: 'var(--muted-foreground)' }}
              >
                {form.igvType === '10'
                  ? 'Editas uno y el otro se calcula con IGV 18%.'
                  : 'Sin IGV: ambos precios son iguales.'}
              </p>

              {/* Moneda + Unidad */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-2 block">Moneda</Label>
                  <PillGroup
                    value={form.currency}
                    onChange={(v) => setForm((f) => ({ ...f, currency: v }))}
                    options={CURRENCIES}
                  />
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Unidad de medida</Label>
                  <Select
                    value={form.unitMeasure}
                    onValueChange={(v) => v && setForm((f) => ({ ...f, unitMeasure: v }))}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {UNITS.map((u) => (
                        <SelectItem key={u.value} value={u.value}>
                          {u.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              {/* Clasificación */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Categoría</Label>
                  <Input
                    value={form.category}
                    onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))}
                    placeholder="Opcional · ej. Bebidas"
                  />
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Marca</Label>
                  <Input
                    value={form.brand}
                    onChange={(e) => setForm((f) => ({ ...f, brand: e.target.value }))}
                    placeholder="Opcional · ej. San Mateo"
                  />
                </div>
              </div>

              <div className="flex gap-2 pt-2">
                <Button type="button" variant="outline" className="flex-1" onClick={closeDialog}>
                  Cancelar
                </Button>
                <Button type="submit" className="flex-1" disabled={saving}>
                  {saving ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Guardando…
                    </>
                  ) : editId ? (
                    'Guardar cambios'
                  ) : (
                    'Crear producto'
                  )}
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {/* Search toolbar */}
      <div
        className="rounded-[var(--radius-lg)] border bg-card p-4 mb-[var(--gap-cards)]"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        <div className="relative">
          <Search
            className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
            style={{ color: 'var(--muted-foreground)' }}
          />
          <Input
            placeholder="Buscar por código, descripción, código SUNAT…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      {/* Table */}
      <section
        className="rounded-[var(--radius-lg)] border bg-card overflow-hidden mb-[var(--gap-cards)]"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        {loading ? (
          <div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span className="t-body-sm">Cargando productos…</span>
          </div>
        ) : hasNoResults ? (
          <div className="p-10 text-center">
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{ background: 'color-mix(in oklch, var(--accent) 14%, transparent)' }}
            >
              <Package className="h-8 w-8" style={{ color: 'var(--brand-ink)' }} />
            </div>
            <h2 className="t-h1 m-0">Tu catálogo está vacío</h2>
            <p
              className="t-body mt-2 mb-4 max-w-[440px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Carga aquí los productos o servicios que vendes para reutilizarlos en facturas, boletas
              y cotizaciones sin tener que tipearlos cada vez.
            </p>
            <Button
              onClick={() => setDialogOpen(true)}
              style={{
                background: 'var(--accent)',
                color: 'var(--accent-foreground)',
                fontWeight: 600,
              }}
            >
              <Plus className="h-4 w-4 mr-2" /> Crear primer producto
            </Button>
          </div>
        ) : hasFilteredNoResults ? (
          <div className="p-10 text-center">
            <Inbox className="h-10 w-10 mx-auto mb-3" style={{ color: 'var(--slate-400)' }} />
            <p className="t-body m-0 font-semibold">Sin resultados para “{debouncedSearch}”</p>
            <p className="t-body-sm mt-1 mb-4" style={{ color: 'var(--muted-foreground)' }}>
              Prueba con otro término o limpia la búsqueda.
            </p>
            <Button variant="outline" onClick={() => setSearch('')}>
              Limpiar búsqueda
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
                  <th className="text-left py-2.5 pl-6 pr-2 w-36">Código</th>
                  <th className="text-left py-2.5 px-2">Descripción</th>
                  <th className="text-right py-2.5 px-2 w-36">Precio</th>
                  <th className="text-left py-2.5 px-2 w-28">IGV</th>
                  <th className="text-center py-2.5 px-2 w-20">Unidad</th>
                  <th className="py-2.5 pr-6 pl-2 w-24" aria-label="acciones" />
                </tr>
              </thead>
              <tbody>
                {products.map((p) => (
                  <tr key={p.id} style={{ borderTop: '1px solid var(--border)' }}>
                    <td className="py-3 pl-6 pr-2">
                      <div className="mono t-body-sm font-semibold">{p.code}</div>
                      {p.sunatCode && (
                        <div
                          className="t-caption mono inline-flex items-center gap-1 mt-0.5"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          <Hash className="h-3 w-3" />
                          {p.sunatCode}
                        </div>
                      )}
                    </td>
                    <td className="py-3 px-2 max-w-[400px]">
                      <div className="t-body-sm">{p.description}</div>
                      {(p.category || p.brand) && (
                        <div
                          className="t-caption mt-0.5 inline-flex items-center gap-1"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          <Tag className="h-3 w-3" />
                          {[p.category, p.brand].filter(Boolean).join(' · ')}
                        </div>
                      )}
                    </td>
                    <td className="py-3 px-2 text-right">
                      <div className="mono tnum t-body-sm font-semibold">
                        {p.currency === 'USD' ? '$' : 'S/'}{' '}
                        {p.unitPriceWithIgv.toLocaleString('es-PE', { minimumFractionDigits: 2 })}
                      </div>
                      <div
                        className="t-caption mono tnum"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        Sin IGV: {p.unitPrice.toLocaleString('es-PE', { minimumFractionDigits: 2 })}
                      </div>
                    </td>
                    <td className="py-3 px-2">
                      <span
                        className="inline-flex items-center rounded-full px-2 py-0.5 t-caption font-semibold"
                        style={{
                          background: 'var(--muted)',
                          color: 'var(--muted-foreground)',
                        }}
                      >
                        {igvLabel[p.igvType] ?? p.igvType}
                      </span>
                    </td>
                    <td className="py-3 px-2 text-center mono t-caption">{p.unitMeasure}</td>
                    <td className="py-3 pr-6 pl-2">
                      <div className="flex justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleEdit(p)}
                          title="Editar"
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(p.id, p.code)}
                          title="Eliminar"
                          style={{ color: 'var(--danger)' }}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
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

      {/* Pagination */}
      {pagination.totalPages > 1 && (
        <div className="flex items-center justify-between flex-wrap gap-3">
          <p className="t-body-sm" style={{ color: 'var(--muted-foreground)' }}>
            Página <span className="mono tnum font-semibold">{pagination.page}</span> de{' '}
            <span className="mono tnum font-semibold">{pagination.totalPages}</span> ·{' '}
            <span className="mono tnum">{pagination.totalCount}</span> total
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={pagination.page <= 1}
              onClick={() => fetchProducts(pagination.page - 1)}
            >
              <ChevronLeft className="h-4 w-4 mr-1" /> Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={pagination.page >= pagination.totalPages}
              onClick={() => fetchProducts(pagination.page + 1)}
            >
              Siguiente <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}

    </div>
  );
}
