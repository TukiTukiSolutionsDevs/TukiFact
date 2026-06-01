'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Plus,
  Trash2,
  Search,
  Loader2,
  Building2,
  User as UserIcon,
  Globe,
  CircleSlash,
  Calendar,
  Send,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

interface ItemForm {
  productCode: string;
  description: string;
  quantity: string;
  unitMeasure: string;
  unitPrice: string;
  igvType: string;
  discount: string;
}

const emptyItem = (): ItemForm => ({
  productCode: '',
  description: '',
  quantity: '1',
  unitMeasure: 'NIU',
  unitPrice: '',
  igvType: '10',
  discount: '0',
});

const CUSTOMER_DOC_TYPES = [
  { value: '6', label: 'RUC', sub: '11 dígitos', icon: Building2, length: 11, placeholder: '20XXXXXXXXX' },
  { value: '1', label: 'DNI', sub: '8 dígitos', icon: UserIcon, length: 8, placeholder: '4XXXXXXX' },
  { value: '4', label: 'CE', sub: 'Carné extranjería', icon: Globe, length: 12, placeholder: 'CE12345...' },
  { value: '7', label: 'Pasaporte', sub: 'Internacional', icon: Globe, length: 12, placeholder: 'P12345...' },
  { value: '0', label: 'Sin doc.', sub: 'Cliente eventual', icon: CircleSlash, length: 0, placeholder: '' },
] as const;

interface LookupStatus { configured: boolean; provider: string; providerName: string; }

const fmtMoney = (n: number, currency: string) =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency, maximumFractionDigits: 2 }).format(n);

function defaultValidUntil() {
  const d = new Date();
  d.setDate(d.getDate() + 15);
  return d.toISOString().slice(0, 10);
}

function PillGroup<T extends string>({
  value,
  onChange,
  options,
}: {
  value: T;
  onChange: (v: T) => void;
  options: readonly {
    value: T;
    label: string;
    sub?: string;
    icon: React.ElementType;
  }[];
}) {
  return (
    <div className="flex gap-2 flex-wrap">
      {options.map((o) => {
        const Icon = o.icon;
        const active = value === o.value;
        return (
          <button
            key={o.value}
            type="button"
            onClick={() => onChange(o.value)}
            className={cn(
              'flex items-center gap-2.5 rounded-[var(--radius-md)] border px-3 py-2 transition-colors text-left',
              active && 'shadow-[var(--shadow-xs)]'
            )}
            style={{
              background: active ? 'color-mix(in oklch, var(--accent) 18%, transparent)' : 'var(--card)',
              borderColor: active ? 'var(--accent)' : 'var(--border)',
            }}
          >
            <span
              className="flex h-8 w-8 items-center justify-center rounded-md"
              style={{
                background: active ? 'var(--accent)' : 'var(--muted)',
                color: active ? 'var(--accent-foreground)' : 'var(--muted-foreground)',
              }}
            >
              <Icon className="h-4 w-4" />
            </span>
            <span>
              <span className="block t-body-sm font-semibold leading-tight">{o.label}</span>
              {o.sub && (
                <span className="block t-caption leading-tight" style={{ color: 'var(--muted-foreground)' }}>
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

function Section({
  title,
  desc,
  right,
  children,
}: {
  title: string;
  desc?: string;
  right?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section
      className="rounded-[var(--radius-lg)] border bg-card p-6"
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      <div className="flex items-start justify-between gap-3 mb-4">
        <div>
          <h2 className="t-h2 m-0">{title}</h2>
          {desc && (
            <p className="t-body-sm m-0 mt-0.5" style={{ color: 'var(--muted-foreground)' }}>
              {desc}
            </p>
          )}
        </div>
        {right}
      </div>
      {children}
    </section>
  );
}

export default function NewQuotationPage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSearching, setIsSearching] = useState(false);
  const [lookupStatus, setLookupStatus] = useState<LookupStatus | null>(null);
  const [form, setForm] = useState({
    validUntil: defaultValidUntil(),
    customerDocType: '6',
    customerDocNumber: '',
    customerName: '',
    customerAddress: '',
    customerEmail: '',
    customerPhone: '',
    currency: 'PEN',
    notes: '',
    termsAndConditions: '',
  });
  const [items, setItems] = useState<ItemForm[]>([emptyItem()]);

  useEffect(() => {
    api.get<LookupStatus>('/v1/services/lookup/status').then(setLookupStatus).catch(() => {});
  }, []);

  const set = (key: string, value: string | null) => {
    if (value !== null) setForm((f) => ({ ...f, [key]: value }));
  };

  const setItem = (idx: number, key: string, value: string | null) => {
    if (value === null) return;
    setItems((prev) => prev.map((it, i) => (i === idx ? { ...it, [key]: value } : it)));
  };

  const addItem = () => setItems((prev) => [...prev, emptyItem()]);
  const removeItem = (idx: number) => {
    if (items.length <= 1) return;
    setItems((prev) => prev.filter((_, i) => i !== idx));
  };

  const calcTotal = () => {
    let subtotal = 0;
    let igv = 0;
    for (const item of items) {
      const qty = parseFloat(item.quantity) || 0;
      const price = parseFloat(item.unitPrice) || 0;
      const disc = parseFloat(item.discount) || 0;
      const taxable = qty * price - disc;
      subtotal += taxable;
      if (item.igvType === '10') igv += taxable * 0.18;
    }
    return { subtotal, igv, total: subtotal + igv };
  };

  const totals = calcTotal();

  const selectedCustomerDocType =
    CUSTOMER_DOC_TYPES.find((d) => d.value === form.customerDocType) ?? CUSTOMER_DOC_TYPES[0];

  const lookup = async () => {
    if (!(form.customerDocType === '6' || form.customerDocType === '1')) return;
    const endpoint = form.customerDocType === '6' ? 'ruc' : 'dni';
    const expectedLen = form.customerDocType === '6' ? 11 : 8;
    if (form.customerDocNumber.length !== expectedLen) {
      toast.error(`El número debe tener ${expectedLen} dígitos`);
      return;
    }
    setIsSearching(true);
    try {
      const data = await api.get<{
        name?: string;
        fullName?: string;
        firstName?: string;
        lastName?: string;
        motherLastName?: string;
        address?: string;
      }>(`/v1/services/lookup/${endpoint}/${form.customerDocNumber}`);
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
        customerName: name,
        customerAddress: data.address ?? f.customerAddress,
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
      setIsSearching(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.customerDocNumber && form.customerDocType !== '0') {
      toast.error('Ingresa el documento del cliente');
      return;
    }
    if (!form.customerName) {
      toast.error('Ingresa el nombre del cliente');
      return;
    }
    if (!form.validUntil) {
      toast.error('Indica hasta qué fecha es válida la cotización');
      return;
    }
    if (items.some((i) => !i.description || !i.unitPrice)) {
      toast.error('Todos los items necesitan descripción y precio');
      return;
    }

    setIsSubmitting(true);
    try {
      const body = {
        validUntil: form.validUntil,
        customerDocType: form.customerDocType,
        customerDocNumber: form.customerDocNumber,
        customerName: form.customerName,
        customerAddress: form.customerAddress || null,
        customerEmail: form.customerEmail || null,
        customerPhone: form.customerPhone || null,
        currency: form.currency,
        notes: form.notes || null,
        termsAndConditions: form.termsAndConditions || null,
        items: items.map((i) => ({
          productCode: i.productCode || null,
          description: i.description,
          quantity: parseFloat(i.quantity),
          unitMeasure: i.unitMeasure,
          unitPrice: parseFloat(i.unitPrice),
          igvType: i.igvType,
          discount: parseFloat(i.discount) || 0,
        })),
      };
      const res = await api.post<{ id: string }>('/v1/quotations', body);
      toast.success('Cotización creada');
      router.push(`/quotations/${res.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al crear');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Nueva cotización</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Prepara una propuesta de venta y envíala al cliente antes de emitir.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)]">
        {/* Left column: sections */}
        <div className="lg:col-span-2 flex flex-col gap-[var(--gap-cards)]">
          {/* Cliente */}
          <Section
            title="Cliente"
            desc="A quién va dirigida la cotización."
            right={
              lookupStatus?.configured ? (
                <span
                  className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 t-caption"
                  style={{ background: 'var(--muted)', color: 'var(--muted-foreground)' }}
                >
                  <Search className="h-3 w-3" />
                  {lookupStatus.providerName}
                </span>
              ) : undefined
            }
          >
            <PillGroup
              value={form.customerDocType}
              onChange={(v) => set('customerDocType', v)}
              options={CUSTOMER_DOC_TYPES}
            />

            <div className="mt-5 flex flex-col gap-4">
              {form.customerDocType !== '0' && (
                <div>
                  <Label className="t-label mb-1.5 block">Número de documento</Label>
                  <div className="flex gap-2">
                    <Input
                      placeholder={selectedCustomerDocType.placeholder}
                      value={form.customerDocNumber}
                      maxLength={selectedCustomerDocType.length || undefined}
                      onChange={(e) =>
                        set('customerDocNumber', e.target.value.replace(/\s/g, ''))
                      }
                      className="mono"
                    />
                    {(form.customerDocType === '6' || form.customerDocType === '1') && (
                      <Button
                        type="button"
                        variant="outline"
                        disabled={isSearching || !lookupStatus?.configured}
                        title={
                          lookupStatus?.configured
                            ? `Buscar con ${lookupStatus.providerName}`
                            : 'Configura un proveedor en Ajustes → Servicios Externos'
                        }
                        onClick={lookup}
                      >
                        {isSearching ? (
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
                </div>
              )}

              <div>
                <Label className="t-label mb-1.5 block">
                  {form.customerDocType === '6' ? 'Razón Social' : 'Nombre del cliente'}
                </Label>
                <Input
                  placeholder={
                    form.customerDocType === '6' ? 'MI EMPRESA SAC' : 'Juan Pérez García'
                  }
                  value={form.customerName}
                  onChange={(e) => set('customerName', e.target.value)}
                  style={form.customerDocType === '6' ? { textTransform: 'uppercase' } : undefined}
                  required
                />
              </div>

              <div>
                <Label className="t-label mb-1.5 block">Dirección (opcional)</Label>
                <Input
                  placeholder="Av. Principal 123, Lima"
                  value={form.customerAddress}
                  onChange={(e) => set('customerAddress', e.target.value)}
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Email (opcional)</Label>
                  <Input
                    type="email"
                    placeholder="cliente@empresa.pe"
                    value={form.customerEmail}
                    onChange={(e) => set('customerEmail', e.target.value)}
                  />
                  <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                    Si lo agregas, le enviaremos la cotización por correo.
                  </p>
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Teléfono (opcional)</Label>
                  <Input
                    placeholder="+51 9XX XXX XXX"
                    value={form.customerPhone}
                    onChange={(e) => set('customerPhone', e.target.value)}
                  />
                </div>
              </div>
            </div>
          </Section>

          {/* Detalles */}
          <Section
            title="Vigencia y moneda"
            desc="Hasta cuándo es válida la propuesta y en qué moneda la cotizas."
          >
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Válida hasta</Label>
                <div className="relative">
                  <Calendar
                    className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
                    style={{ color: 'var(--muted-foreground)' }}
                  />
                  <Input
                    type="date"
                    value={form.validUntil}
                    onChange={(e) => set('validUntil', e.target.value)}
                    required
                    className="pl-9 mono"
                  />
                </div>
                <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                  Por defecto sugerimos 15 días desde hoy.
                </p>
              </div>
              <div>
                <Label className="t-label mb-1.5 block">Moneda</Label>
                <Select value={form.currency} onValueChange={(v) => set('currency', v)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="PEN">Soles (PEN)</SelectItem>
                    <SelectItem value="USD">Dólares (USD)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </Section>

          {/* Items */}
          <Section
            title="Productos y servicios"
            desc={`${items.length} ${items.length === 1 ? 'línea' : 'líneas'}`}
            right={
              <Button type="button" variant="outline" size="sm" onClick={addItem}>
                <Plus className="h-4 w-4 mr-1.5" /> Agregar línea
              </Button>
            }
          >
            <div className="-mx-6 overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr
                    className="t-overline"
                    style={{ color: 'var(--muted-foreground)', background: 'var(--muted)' }}
                  >
                    <th className="text-left py-2.5 pl-6 pr-2 w-10">#</th>
                    <th className="text-left py-2.5 px-2">Descripción</th>
                    <th className="text-right py-2.5 px-2 w-24">Cant.</th>
                    <th className="text-left py-2.5 px-2 w-24">Unidad</th>
                    <th className="text-right py-2.5 px-2 w-32">P. Unit.</th>
                    <th className="text-left py-2.5 px-2 w-32">IGV</th>
                    <th className="text-right py-2.5 px-2 w-28">Desc.</th>
                    <th className="text-right py-2.5 px-2 w-32">Subtotal</th>
                    <th className="py-2.5 pr-6 pl-2 w-10" aria-label="acciones" />
                  </tr>
                </thead>
                <tbody>
                  {items.map((item, idx) => {
                    const qty = parseFloat(item.quantity) || 0;
                    const price = parseFloat(item.unitPrice) || 0;
                    const disc = parseFloat(item.discount) || 0;
                    const taxable = qty * price - disc;
                    const igvLine = item.igvType === '10' ? taxable * 0.18 : 0;
                    const lineTotal = taxable + igvLine;
                    return (
                      <tr key={idx} style={{ borderTop: '1px solid var(--border)' }}>
                        <td
                          className="py-3 pl-6 pr-2 t-body-sm tnum"
                          style={{ color: 'var(--muted-foreground)' }}
                        >
                          {idx + 1}
                        </td>
                        <td className="py-3 px-2">
                          <Input
                            placeholder="Ej. Diseño de identidad visual"
                            value={item.description}
                            onChange={(e) => setItem(idx, 'description', e.target.value)}
                            required
                          />
                        </td>
                        <td className="py-3 px-2">
                          <Input
                            type="number"
                            min={0.01}
                            step={1}
                            inputMode="decimal"
                            value={item.quantity || ''}
                            onChange={(e) => setItem(idx, 'quantity', e.target.value)}
                            className="mono tnum text-right"
                          />
                        </td>
                        <td className="py-3 px-2">
                          <Input
                            value={item.unitMeasure}
                            onChange={(e) => setItem(idx, 'unitMeasure', e.target.value)}
                            className="mono text-center"
                          />
                        </td>
                        <td className="py-3 px-2">
                          <Input
                            type="number"
                            min={0}
                            step={0.01}
                            inputMode="decimal"
                            value={item.unitPrice || ''}
                            onChange={(e) => setItem(idx, 'unitPrice', e.target.value)}
                            className="mono tnum text-right"
                            required
                          />
                        </td>
                        <td className="py-3 px-2">
                          <Select
                            value={item.igvType}
                            onValueChange={(v) => v != null && setItem(idx, 'igvType', v)}
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="10">Gravado (18%)</SelectItem>
                              <SelectItem value="20">Exonerado</SelectItem>
                              <SelectItem value="30">Inafecto</SelectItem>
                            </SelectContent>
                          </Select>
                        </td>
                        <td className="py-3 px-2">
                          <Input
                            type="number"
                            min={0}
                            step={0.01}
                            inputMode="decimal"
                            value={item.discount || ''}
                            onChange={(e) => setItem(idx, 'discount', e.target.value)}
                            className="mono tnum text-right"
                          />
                        </td>
                        <td className="py-3 px-2 text-right mono tnum t-body-sm font-semibold">
                          {lineTotal.toFixed(2)}
                        </td>
                        <td className="py-3 pl-2 pr-6 text-right">
                          {items.length > 1 && (
                            <button
                              type="button"
                              onClick={() => removeItem(idx)}
                              className="h-8 w-8 inline-flex items-center justify-center rounded-md hover:bg-[var(--muted)] transition-colors"
                              aria-label={`Quitar línea ${idx + 1}`}
                              style={{ color: 'var(--danger)' }}
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            <button
              type="button"
              onClick={addItem}
              className="mt-4 w-full rounded-[var(--radius-md)] border-2 border-dashed py-3 t-body-sm font-medium transition-colors hover:bg-[var(--muted)] flex items-center justify-center gap-2"
              style={{ borderColor: 'var(--border)', color: 'var(--muted-foreground)' }}
            >
              <Plus className="h-4 w-4" /> Agregar otra línea
            </button>
          </Section>

          {/* Notas + T&C */}
          <Section
            title="Notas y condiciones"
            desc="Aparecerán en el PDF de la cotización."
          >
            <div className="grid grid-cols-1 gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Notas (opcional)</Label>
                <Textarea
                  placeholder="Ej. Entrega en 7 días hábiles, traslado incluido…"
                  value={form.notes}
                  onChange={(e) => set('notes', e.target.value)}
                  rows={2}
                />
              </div>
              <div>
                <Label className="t-label mb-1.5 block">Términos y condiciones (opcional)</Label>
                <Textarea
                  placeholder="Forma de pago, garantía, política de devoluciones…"
                  value={form.termsAndConditions}
                  onChange={(e) => set('termsAndConditions', e.target.value)}
                  rows={3}
                />
              </div>
            </div>
          </Section>
        </div>

        {/* Right: sticky summary */}
        <aside className="lg:col-span-1">
          <div
            className="rounded-[var(--radius-lg)] border bg-card p-6 lg:sticky lg:top-20"
            style={{ boxShadow: 'var(--shadow-xs)' }}
          >
            <h2 className="t-h2 m-0 mb-4">Resumen</h2>

            <div className="flex flex-col gap-2 t-body-sm">
              <div className="flex justify-between">
                <span style={{ color: 'var(--muted-foreground)' }}>Subtotal</span>
                <span className="mono tnum">{fmtMoney(totals.subtotal, form.currency)}</span>
              </div>
              <div className="flex justify-between">
                <span style={{ color: 'var(--muted-foreground)' }}>IGV 18%</span>
                <span className="mono tnum">{fmtMoney(totals.igv, form.currency)}</span>
              </div>
            </div>

            <div className="my-4 h-px" style={{ background: 'var(--border)' }} />

            <div className="flex items-baseline justify-between mb-5">
              <span className="t-h2 m-0">Total</span>
              <span className="t-num-lg mono">{fmtMoney(totals.total, form.currency)}</span>
            </div>

            <div className="flex flex-col gap-2">
              <Button
                type="submit"
                size="lg"
                className="w-full h-12"
                disabled={isSubmitting || totals.total === 0}
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Creando…
                  </>
                ) : (
                  <>
                    <Send className="h-4 w-4 mr-2" /> Crear cotización
                  </>
                )}
              </Button>
              <Button
                type="button"
                variant="outline"
                size="lg"
                className="w-full h-12"
                onClick={() => router.push('/quotations')}
              >
                Cancelar
              </Button>
            </div>
            {totals.total === 0 && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Agrega al menos una línea con precio mayor a 0.
              </p>
            )}
          </div>
        </aside>
      </div>
    </form>
  );
}
