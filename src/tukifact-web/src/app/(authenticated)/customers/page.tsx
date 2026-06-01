'use client';

import { useEffect, useState, useCallback } from 'react';
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
  Search,
  Plus,
  Users,
  Pencil,
  Trash2,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Inbox,
  Building2,
  User as UserIcon,
  Globe,
  CircleSlash,
  Mail,
  Phone,
  MapPin,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

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

interface LookupStatus {
  configured: boolean;
  provider: string;
  providerName: string;
}

const EMPTY_FORM = {
  docType: '6',
  docNumber: '',
  name: '',
  email: '',
  phone: '',
  address: '',
  category: '',
  notes: '',
};

const DOC_TYPES = [
  { value: '6', label: 'RUC', sub: '11 dígitos', icon: Building2, length: 11, placeholder: '20XXXXXXXXX' },
  { value: '1', label: 'DNI', sub: '8 dígitos', icon: UserIcon, length: 8, placeholder: '4XXXXXXX' },
  { value: '4', label: 'CE', sub: 'Carné extranjería', icon: Globe, length: 12, placeholder: 'CE12345...' },
  { value: '7', label: 'Pasaporte', sub: 'Internacional', icon: Globe, length: 12, placeholder: 'P12345...' },
  { value: '0', label: 'Sin doc.', sub: 'Cliente eventual', icon: CircleSlash, length: 0, placeholder: '' },
] as const;

const docTypeLabel: Record<string, string> = {
  '6': 'RUC',
  '1': 'DNI',
  '4': 'CE',
  '7': 'Pasaporte',
  '0': 'Sin doc',
};

function PillGroup<T extends string>({
  value,
  onChange,
  options,
}: {
  value: T;
  onChange: (v: T) => void;
  options: readonly { value: T; label: string; sub?: string; icon: React.ElementType }[];
}) {
  return (
    <div className="grid grid-cols-2 md:grid-cols-5 gap-2">
      {options.map((o) => {
        const Icon = o.icon;
        const active = value === o.value;
        return (
          <button
            key={o.value}
            type="button"
            onClick={() => onChange(o.value)}
            className={cn(
              'relative flex flex-col items-center gap-1 rounded-[var(--radius-md)] border px-2.5 py-2 transition-colors text-center min-w-0'
            )}
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
            <span className="t-body-sm font-semibold leading-tight">{o.label}</span>
            {o.sub && (
              <span
                className="t-caption leading-tight"
                style={{ color: 'var(--muted-foreground)' }}
              >
                {o.sub}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}

export default function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [pagination, setPagination] = useState({ page: 1, pageSize: 50, totalCount: 0, totalPages: 0 });
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);
  const [editId, setEditId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [isLookingUp, setIsLookingUp] = useState(false);
  const [lookupStatus, setLookupStatus] = useState<LookupStatus | null>(null);

  useEffect(() => {
    api.get<LookupStatus>('/v1/services/lookup/status').then(setLookupStatus).catch(() => {});
  }, []);

  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(t);
  }, [search]);

  const fetchCustomers = useCallback(
    async (page: number) => {
      setLoading(true);
      try {
        const params = new URLSearchParams({ page: String(page), pageSize: '50' });
        if (debouncedSearch) params.set('search', debouncedSearch);
        const res = await api.get<PaginatedResponse<Customer>>(`/v1/customers?${params}`);
        setCustomers(res.data);
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
    fetchCustomers(1);
  }, [fetchCustomers]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim()) {
      toast.error('El nombre o razón social es obligatorio');
      return;
    }
    if (form.docType !== '0' && !form.docNumber.trim()) {
      toast.error('El número de documento es obligatorio');
      return;
    }

    setSaving(true);
    try {
      const body = {
        docType: form.docType,
        docNumber: form.docNumber.trim(),
        name: form.name.trim(),
        email: form.email.trim() || null,
        phone: form.phone.trim() || null,
        address: form.address.trim() || null,
        category: form.category.trim() || null,
        notes: form.notes.trim() || null,
      };

      if (editId) {
        await api.put(`/v1/customers/${editId}`, body);
        toast.success('Cliente actualizado');
      } else {
        await api.post('/v1/customers', body);
        toast.success('Cliente creado');
      }
      closeDialog();
      fetchCustomers(pagination.page);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (c: Customer) => {
    setEditId(c.id);
    setForm({
      docType: c.docType,
      docNumber: c.docNumber,
      name: c.name,
      email: c.email ?? '',
      phone: c.phone ?? '',
      address: c.address ?? '',
      category: c.category ?? '',
      notes: '',
    });
    setDialogOpen(true);
  };

  const handleDelete = async (id: string, name: string) => {
    if (!confirm(`¿Eliminar el cliente "${name}"? Esta acción no se puede deshacer.`)) return;
    try {
      await api.delete(`/v1/customers/${id}`);
      toast.success('Cliente eliminado');
      fetchCustomers(pagination.page);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  const closeDialog = () => {
    setDialogOpen(false);
    setEditId(null);
    setForm(EMPTY_FORM);
  };

  const lookup = async () => {
    if (!(form.docType === '6' || form.docType === '1')) return;
    const endpoint = form.docType === '6' ? 'ruc' : 'dni';
    const expectedLen = form.docType === '6' ? 11 : 8;
    if (form.docNumber.length !== expectedLen) {
      toast.error(`El número debe tener ${expectedLen} dígitos`);
      return;
    }
    setIsLookingUp(true);
    try {
      const data = await api.get<{
        name?: string;
        fullName?: string;
        firstName?: string;
        lastName?: string;
        motherLastName?: string;
        address?: string;
      }>(`/v1/services/lookup/${endpoint}/${form.docNumber}`);
      const name =
        data.name ||
        data.fullName ||
        [data.firstName, data.lastName, data.motherLastName].filter(Boolean).join(' ') ||
        '';
      if (!name) {
        toast.error('No se encontraron datos para ese número');
        return;
      }
      setForm((f) => ({
        ...f,
        name,
        address: data.address ?? f.address,
      }));
      toast.success(`Datos encontrados: ${name}`);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al consultar datos';
      if (msg.includes('No hay proveedor')) {
        toast.error('Configura un proveedor de datos en Configuración → Servicios Externos');
      } else {
        toast.error(msg);
      }
    } finally {
      setIsLookingUp(false);
    }
  };

  const selectedDocType =
    DOC_TYPES.find((d) => d.value === form.docType) ?? DOC_TYPES[0];

  const hasActiveSearch = debouncedSearch.length > 0;
  const hasNoResults = !loading && customers.length === 0 && !hasActiveSearch;
  const hasFilteredNoResults = !loading && customers.length === 0 && hasActiveSearch;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Directorio de clientes</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            {pagination.totalCount === 1
              ? '1 cliente registrado.'
              : `${pagination.totalCount} clientes registrados.`}
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
            <Plus className="h-4 w-4 mr-2" /> Nuevo cliente
          </DialogTrigger>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>{editId ? 'Editar cliente' : 'Nuevo cliente'}</DialogTitle>
              <DialogDescription>
                Datos del cliente o proveedor. {lookupStatus?.configured && form.docType !== '0'
                  ? 'Puedes auto-completar nombre y dirección con el lookup SUNAT/RENIEC.'
                  : 'Se reutiliza para autocompletar comprobantes.'}
              </DialogDescription>
            </DialogHeader>

            <form onSubmit={handleSubmit} className="flex flex-col gap-5">
              {/* Tipo de documento */}
              <div>
                <Label className="t-label mb-2 block">Tipo de documento</Label>
                <PillGroup
                  value={form.docType}
                  onChange={(v) => setForm((f) => ({ ...f, docType: v }))}
                  options={DOC_TYPES}
                />
              </div>

              {/* Número de documento + lookup */}
              {form.docType !== '0' && (
                <div>
                  <Label className="t-label mb-1.5 block">Número de documento</Label>
                  <div className="flex gap-2">
                    <Input
                      placeholder={selectedDocType.placeholder}
                      value={form.docNumber}
                      maxLength={selectedDocType.length || undefined}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, docNumber: e.target.value.replace(/\s/g, '') }))
                      }
                      className="mono"
                      disabled={!!editId}
                      required={form.docType !== '0'}
                    />
                    {(form.docType === '6' || form.docType === '1') && (
                      <Button
                        type="button"
                        variant="outline"
                        disabled={isLookingUp || !lookupStatus?.configured}
                        title={
                          lookupStatus?.configured
                            ? `Buscar con ${lookupStatus.providerName}`
                            : 'Configura un proveedor en Ajustes → Servicios Externos'
                        }
                        onClick={lookup}
                      >
                        {isLookingUp ? (
                          <>
                            <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Buscando…
                          </>
                        ) : (
                          <>
                            <Search className="h-4 w-4 mr-2" /> Buscar
                          </>
                        )}
                      </Button>
                    )}
                  </div>
                  {editId && (
                    <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                      El número de documento no se puede modificar una vez creado.
                    </p>
                  )}
                </div>
              )}

              {/* Nombre */}
              <div>
                <Label className="t-label mb-1.5 block">
                  {form.docType === '6' ? 'Razón Social' : 'Nombre completo'}
                </Label>
                <Input
                  placeholder={form.docType === '6' ? 'MI EMPRESA SAC' : 'Juan Pérez García'}
                  value={form.name}
                  onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  style={form.docType === '6' ? { textTransform: 'uppercase' } : undefined}
                  required
                />
              </div>

              {/* Contacto */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Email</Label>
                  <div className="relative">
                    <Mail
                      className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
                      style={{ color: 'var(--muted-foreground)' }}
                    />
                    <Input
                      type="email"
                      value={form.email}
                      onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
                      placeholder="cliente@ejemplo.com"
                      className="pl-9"
                    />
                  </div>
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Teléfono</Label>
                  <div className="relative">
                    <Phone
                      className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
                      style={{ color: 'var(--muted-foreground)' }}
                    />
                    <Input
                      value={form.phone}
                      onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))}
                      placeholder="+51 999 999 999"
                      className="pl-9"
                    />
                  </div>
                </div>
              </div>

              {/* Dirección */}
              <div>
                <Label className="t-label mb-1.5 block">Dirección</Label>
                <div className="relative">
                  <MapPin
                    className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
                    style={{ color: 'var(--muted-foreground)' }}
                  />
                  <Input
                    value={form.address}
                    onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))}
                    placeholder="Av. Principal 123, Distrito"
                    className="pl-9"
                  />
                </div>
              </div>

              {/* Categoría */}
              <div>
                <Label className="t-label mb-1.5 block">Categoría</Label>
                <Input
                  value={form.category}
                  onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))}
                  placeholder="Opcional · VIP, Regular, Mayorista…"
                />
                <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                  Sirve para filtrar después en reportes y comprobantes.
                </p>
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
                    'Crear cliente'
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
            placeholder="Buscar por nombre, RUC, DNI…"
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
            <span className="t-body-sm">Cargando clientes…</span>
          </div>
        ) : hasNoResults ? (
          <div className="p-10 text-center">
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{ background: 'color-mix(in oklch, var(--accent) 14%, transparent)' }}
            >
              <Users className="h-8 w-8" style={{ color: 'var(--brand-ink)' }} />
            </div>
            <h2 className="t-h1 m-0">Aún no tienes clientes guardados</h2>
            <p
              className="t-body mt-2 mb-4 max-w-[440px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Guarda a tus clientes habituales para autocompletar comprobantes con un click —
              razón social, dirección, email y categoría.
            </p>
            <Button
              onClick={() => setDialogOpen(true)}
              style={{
                background: 'var(--accent)',
                color: 'var(--accent-foreground)',
                fontWeight: 600,
              }}
            >
              <Plus className="h-4 w-4 mr-2" /> Crear primer cliente
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
                  <th className="text-left py-2.5 pl-6 pr-2 w-44">Documento</th>
                  <th className="text-left py-2.5 px-2">Nombre / Razón Social</th>
                  <th className="text-left py-2.5 px-2 w-56">Contacto</th>
                  <th className="text-left py-2.5 px-2 w-32">Categoría</th>
                  <th className="py-2.5 pr-6 pl-2 w-24" aria-label="acciones" />
                </tr>
              </thead>
              <tbody>
                {customers.map((c) => (
                  <tr key={c.id} style={{ borderTop: '1px solid var(--border)' }}>
                    <td className="py-3 pl-6 pr-2">
                      <span
                        className="inline-flex items-center rounded-md px-1.5 py-0.5 t-caption font-semibold mr-1.5"
                        style={{
                          background: 'var(--muted)',
                          color: 'var(--muted-foreground)',
                        }}
                      >
                        {docTypeLabel[c.docType] ?? c.docType}
                      </span>
                      <span className="mono t-body-sm">{c.docNumber || '—'}</span>
                    </td>
                    <td className="py-3 px-2 max-w-[360px]">
                      <div className="t-body-sm font-medium truncate">{c.name}</div>
                      {c.address && (
                        <div
                          className="t-caption mt-0.5 truncate inline-flex items-center gap-1"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          <MapPin className="h-3 w-3 shrink-0" />
                          {c.address}
                        </div>
                      )}
                    </td>
                    <td className="py-3 px-2 max-w-[220px]">
                      {c.email && (
                        <div
                          className="t-body-sm truncate inline-flex items-center gap-1 max-w-full"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          <Mail className="h-3 w-3 shrink-0" />
                          {c.email}
                        </div>
                      )}
                      {c.phone && (
                        <div
                          className="t-caption mono mt-0.5 inline-flex items-center gap-1"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          <Phone className="h-3 w-3 shrink-0" />
                          {c.phone}
                        </div>
                      )}
                      {!c.email && !c.phone && (
                        <span style={{ color: 'var(--muted-foreground)' }}>—</span>
                      )}
                    </td>
                    <td className="py-3 px-2">
                      {c.category ? (
                        <span
                          className="inline-flex items-center rounded-full px-2.5 py-0.5 t-caption font-semibold"
                          style={{
                            background: 'color-mix(in oklch, var(--info) 14%, transparent)',
                            color: 'var(--info)',
                          }}
                        >
                          {c.category}
                        </span>
                      ) : (
                        <span style={{ color: 'var(--muted-foreground)' }}>—</span>
                      )}
                    </td>
                    <td className="py-3 pr-6 pl-2">
                      <div className="flex justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleEdit(c)}
                          title="Editar"
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(c.id, c.name)}
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
              onClick={() => fetchCustomers(pagination.page - 1)}
            >
              <ChevronLeft className="h-4 w-4 mr-1" /> Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={pagination.page >= pagination.totalPages}
              onClick={() => fetchCustomers(pagination.page + 1)}
            >
              Siguiente <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
