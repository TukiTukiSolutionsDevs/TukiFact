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
  Calendar,
  Repeat,
  CalendarDays,
  CalendarRange,
  CalendarClock,
  CalendarCheck,
  FileText,
  Receipt,
  Hash,
  Send,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

interface ItemForm {
  description: string;
  quantity: string;
  unitMeasure: string;
  unitPrice: string;
  igvType: string;
}

const emptyItem = (): ItemForm => ({
  description: '',
  quantity: '1',
  unitMeasure: 'NIU',
  unitPrice: '',
  igvType: '10',
});

const CUSTOMER_DOC_TYPES = [
  { value: '6', label: 'RUC', sub: '11 dígitos', icon: Building2, length: 11, placeholder: '20XXXXXXXXX' },
  { value: '1', label: 'DNI', sub: '8 dígitos', icon: UserIcon, length: 8, placeholder: '4XXXXXXX' },
  { value: '4', label: 'CE', sub: 'Carné extranjería', icon: Globe, length: 12, placeholder: 'CE12345...' },
  { value: '7', label: 'Pasaporte', sub: 'Internacional', icon: Globe, length: 12, placeholder: 'P12345...' },
] as const;

const DOC_TYPES = [
  { value: '01', label: 'Factura', sub: 'Cliente con RUC', icon: Receipt },
  { value: '03', label: 'Boleta', sub: 'Consumidor final', icon: FileText },
] as const;

const FREQUENCIES = [
  { value: 'daily', label: 'Diaria', sub: 'Cada día', icon: CalendarDays },
  { value: 'weekly', label: 'Semanal', sub: 'Un día por semana', icon: CalendarRange },
  { value: 'biweekly', label: 'Quincenal', sub: 'Cada 14 días', icon: CalendarClock },
  { value: 'monthly', label: 'Mensual', sub: 'Un día por mes', icon: Calendar },
  { value: 'yearly', label: 'Anual', sub: 'Una vez al año', icon: CalendarCheck },
] as const;

const CURRENCIES = [
  { value: 'PEN', label: 'PEN', sub: 'Soles', icon: () => <span className="font-bold">S/</span> },
  { value: 'USD', label: 'USD', sub: 'Dólares', icon: () => <span className="font-bold">$</span> },
] as const;

const IGV_TYPES: { value: string; label: string }[] = [
  { value: '10', label: 'Gravado' },
  { value: '20', label: 'Exonerado' },
  { value: '30', label: 'Inafecto' },
];

const WEEKDAYS: { value: string; label: string }[] = [
  { value: '1', label: 'Lunes' },
  { value: '2', label: 'Martes' },
  { value: '3', label: 'Miércoles' },
  { value: '4', label: 'Jueves' },
  { value: '5', label: 'Viernes' },
  { value: '6', label: 'Sábado' },
  { value: '0', label: 'Domingo' },
];

interface LookupStatus {
  configured: boolean;
  provider: string;
  providerName: string;
}

function PillGroup<T extends string>({
  value,
  onChange,
  options,
  cols = 2,
}: {
  value: T;
  onChange: (v: T) => void;
  options: readonly { value: T; label: string; sub?: string; icon: React.ElementType }[];
  cols?: 2 | 3 | 4 | 5;
}) {
  const colsClass: Record<number, string> = {
    2: 'grid-cols-2',
    3: 'grid-cols-2 md:grid-cols-3',
    4: 'grid-cols-2 md:grid-cols-4',
    5: 'grid-cols-2 md:grid-cols-5',
  };
  return (
    <div className={cn('grid gap-2', colsClass[cols])}>
      {options.map((o) => {
        const Icon = o.icon;
        const active = value === o.value;
        return (
          <button
            key={o.value}
            type="button"
            onClick={() => onChange(o.value)}
            className={cn(
              'relative flex items-center gap-2.5 rounded-[var(--radius-md)] border px-3 py-2.5 transition-colors text-left min-w-0'
            )}
            style={{
              background: active ? 'color-mix(in oklch, var(--accent) 18%, transparent)' : 'var(--card)',
              borderColor: active ? 'var(--accent)' : 'var(--border)',
            }}
          >
            <span
              className="flex h-8 w-8 items-center justify-center rounded-md shrink-0"
              style={{
                background: active ? 'var(--accent)' : 'var(--muted)',
                color: active ? 'var(--accent-foreground)' : 'var(--muted-foreground)',
              }}
            >
              <Icon className="h-4 w-4" />
            </span>
            <span className="min-w-0 flex-1 whitespace-normal">
              <span className="block t-body-sm font-semibold leading-tight whitespace-normal">
                {o.label}
              </span>
              {o.sub && (
                <span
                  className="block t-caption leading-tight mt-0.5 whitespace-normal"
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

const FREQ_LABEL: Record<string, string> = {
  daily: 'Diaria',
  weekly: 'Semanal',
  biweekly: 'Quincenal',
  monthly: 'Mensual',
  yearly: 'Anual',
};

const WEEKDAY_LABEL: Record<string, string> = {
  '0': 'domingo',
  '1': 'lunes',
  '2': 'martes',
  '3': 'miércoles',
  '4': 'jueves',
  '5': 'viernes',
  '6': 'sábado',
};

export default function NewRecurringInvoicePage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSearching, setIsSearching] = useState(false);
  const [lookupStatus, setLookupStatus] = useState<LookupStatus | null>(null);
  const [form, setForm] = useState({
    documentType: '01',
    serie: 'F001',
    customerDocType: '6',
    customerDocNumber: '',
    customerName: '',
    customerAddress: '',
    customerEmail: '',
    currency: 'PEN',
    frequency: 'monthly',
    dayOfMonth: '1',
    dayOfWeek: '1',
    startDate: '',
    endDate: '',
    notes: '',
  });
  const [items, setItems] = useState<ItemForm[]>([emptyItem()]);

  useEffect(() => {
    api.get<LookupStatus>('/v1/services/lookup/status').then(setLookupStatus).catch(() => {});
  }, []);

  // Auto-align serie prefix to document type
  useEffect(() => {
    setForm((f) => {
      const expectedPrefix = f.documentType === '01' ? 'F' : 'B';
      if (f.serie.startsWith(expectedPrefix)) return f;
      return { ...f, serie: `${expectedPrefix}001` };
    });
  }, [form.documentType]);

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

  const selectedDocType =
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
    if (!form.customerDocNumber || !form.customerName) {
      toast.error('Completa los datos del cliente');
      return;
    }
    if (!form.startDate) {
      toast.error('Indica la fecha de inicio');
      return;
    }
    if (items.some((i) => !i.description || !i.unitPrice)) {
      toast.error('Todos los items necesitan descripción y precio');
      return;
    }

    setIsSubmitting(true);
    try {
      const body = {
        documentType: form.documentType,
        serie: form.serie,
        customerDocType: form.customerDocType,
        customerDocNumber: form.customerDocNumber,
        customerName: form.customerName,
        customerAddress: form.customerAddress || null,
        customerEmail: form.customerEmail || null,
        currency: form.currency,
        frequency: form.frequency,
        dayOfMonth: form.frequency === 'monthly' ? parseInt(form.dayOfMonth) : null,
        dayOfWeek: form.frequency === 'weekly' ? parseInt(form.dayOfWeek) : null,
        startDate: form.startDate,
        endDate: form.endDate || null,
        notes: form.notes || null,
        items: items.map((i) => ({
          description: i.description,
          quantity: parseFloat(i.quantity),
          unitMeasure: i.unitMeasure,
          unitPrice: parseFloat(i.unitPrice),
          igvType: i.igvType,
        })),
      };
      await api.post('/v1/recurring-invoices', body);
      toast.success('Facturación recurrente creada');
      router.push('/recurring-invoices');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al crear');
    } finally {
      setIsSubmitting(false);
    }
  };

  const currencySymbol = form.currency === 'USD' ? '$' : 'S/';

  // Totals preview
  const totals = items.reduce(
    (acc, it) => {
      const qty = parseFloat(it.quantity) || 0;
      const price = parseFloat(it.unitPrice) || 0;
      const line = qty * price;
      if (it.igvType === '10') {
        // Gravado — price is interpreted as gross including IGV in many flows; we follow the
        // convention used in the rest of the app: price is net, IGV is added on top.
        acc.taxable += line;
        acc.igv += line * 0.18;
      } else if (it.igvType === '20') {
        acc.exempt += line;
      } else if (it.igvType === '30') {
        acc.unaffected += line;
      }
      return acc;
    },
    { taxable: 0, exempt: 0, unaffected: 0, igv: 0 }
  );
  const total = totals.taxable + totals.exempt + totals.unaffected + totals.igv;

  const cadenceText = (() => {
    if (form.frequency === 'monthly') return `Cada mes, día ${form.dayOfMonth}`;
    if (form.frequency === 'weekly')
      return `Cada semana, ${WEEKDAY_LABEL[form.dayOfWeek] ?? 'lunes'}`;
    if (form.frequency === 'biweekly') return 'Cada 14 días';
    if (form.frequency === 'daily') return 'Todos los días';
    if (form.frequency === 'yearly') return 'Una vez al año';
    return '—';
  })();

  return (
    <form onSubmit={handleSubmit}>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Nueva facturación recurrente</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Programa la emisión automática de un comprobante para un cliente habitual.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)]">
        {/* Left: form */}
        <div className="lg:col-span-2 flex flex-col gap-[var(--gap-cards)]">
          {/* Comprobante */}
          <Section
            title="Comprobante"
            desc="Tipo de comprobante a emitir y serie correlativa."
          >
            <PillGroup
              value={form.documentType}
              onChange={(v) => set('documentType', v)}
              options={DOC_TYPES}
            />

            <div className="mt-5 grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Serie</Label>
                <Input
                  value={form.serie}
                  onChange={(e) => set('serie', e.target.value.toUpperCase().slice(0, 4))}
                  maxLength={4}
                  className="mono"
                />
                <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                  Las facturas empiezan con <strong>F</strong>, las boletas con <strong>B</strong>.
                </p>
              </div>
              <div>
                <Label className="t-label mb-2 block">Moneda</Label>
                <PillGroup
                  value={form.currency}
                  onChange={(v) => set('currency', v)}
                  options={CURRENCIES}
                />
              </div>
            </div>
          </Section>

          {/* Cliente */}
          <Section
            title="Cliente"
            desc="A quién se le va a facturar cada vez que se ejecute la programación."
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
              cols={4}
            />

            <div className="mt-5 flex flex-col gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Número de documento</Label>
                <div className="flex gap-2">
                  <Input
                    placeholder={selectedDocType.placeholder}
                    value={form.customerDocNumber}
                    maxLength={selectedDocType.length || undefined}
                    onChange={(e) => set('customerDocNumber', e.target.value.replace(/\s/g, ''))}
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

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Dirección</Label>
                  <Input
                    value={form.customerAddress}
                    onChange={(e) => set('customerAddress', e.target.value)}
                    placeholder="Av. Principal 123"
                  />
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Email</Label>
                  <Input
                    type="email"
                    value={form.customerEmail}
                    onChange={(e) => set('customerEmail', e.target.value)}
                    placeholder="cliente@ejemplo.com"
                  />
                  <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                    Se le enviará el comprobante por correo cuando se emita.
                  </p>
                </div>
              </div>
            </div>
          </Section>

          {/* Programación */}
          <Section
            title="Programación"
            desc="Cada cuánto y desde cuándo se emitirá el comprobante."
          >
            <Label className="t-label mb-2 block">Frecuencia</Label>
            <PillGroup
              value={form.frequency}
              onChange={(v) => set('frequency', v)}
              options={FREQUENCIES}
              cols={5}
            />

            <div className="mt-5 grid grid-cols-1 md:grid-cols-2 gap-4">
              {form.frequency === 'monthly' && (
                <div>
                  <Label className="t-label mb-1.5 block">Día del mes</Label>
                  <Input
                    type="number"
                    min="1"
                    max="28"
                    value={form.dayOfMonth}
                    onChange={(e) => set('dayOfMonth', e.target.value)}
                    className="mono tnum"
                  />
                  <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                    Entre 1 y 28 para asegurar que cae todos los meses.
                  </p>
                </div>
              )}
              {form.frequency === 'weekly' && (
                <div>
                  <Label className="t-label mb-1.5 block">Día de la semana</Label>
                  <Select
                    value={form.dayOfWeek}
                    onValueChange={(v) => v && set('dayOfWeek', v)}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {WEEKDAYS.map((d) => (
                        <SelectItem key={d.value} value={d.value}>
                          {d.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}

              <div>
                <Label className="t-label mb-1.5 block">Fecha de inicio</Label>
                <div className="relative">
                  <Calendar
                    className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
                    style={{ color: 'var(--muted-foreground)' }}
                  />
                  <Input
                    type="date"
                    value={form.startDate}
                    onChange={(e) => set('startDate', e.target.value)}
                    className="pl-9 mono"
                    required
                  />
                </div>
              </div>
              <div>
                <Label className="t-label mb-1.5 block">Fecha de fin (opcional)</Label>
                <div className="relative">
                  <Calendar
                    className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
                    style={{ color: 'var(--muted-foreground)' }}
                  />
                  <Input
                    type="date"
                    value={form.endDate}
                    onChange={(e) => set('endDate', e.target.value)}
                    className="pl-9 mono"
                  />
                </div>
                <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                  Si no la indicas, la programación seguirá indefinidamente.
                </p>
              </div>
            </div>
          </Section>

          {/* Items */}
          <Section
            title="Plantilla de items"
            desc={`${items.length} ${items.length === 1 ? 'línea' : 'líneas'} se replicarán en cada emisión.`}
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
                    <th className="text-right py-2.5 px-2 w-24">Cantidad</th>
                    <th className="text-right py-2.5 px-2 w-32">Precio unit.</th>
                    <th className="text-left py-2.5 px-2 w-32">IGV</th>
                    <th className="py-2.5 pr-6 pl-2 w-10" aria-label="acciones" />
                  </tr>
                </thead>
                <tbody>
                  {items.map((item, idx) => (
                    <tr key={idx} style={{ borderTop: '1px solid var(--border)' }}>
                      <td
                        className="py-3 pl-6 pr-2 t-body-sm tnum"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        {idx + 1}
                      </td>
                      <td className="py-3 px-2">
                        <Input
                          value={item.description}
                          onChange={(e) => setItem(idx, 'description', e.target.value)}
                          placeholder="Ej. Servicio de mantenimiento mensual"
                          required
                        />
                      </td>
                      <td className="py-3 px-2">
                        <Input
                          type="number"
                          step="any"
                          min="0.01"
                          inputMode="decimal"
                          value={item.quantity || ''}
                          onChange={(e) => setItem(idx, 'quantity', e.target.value)}
                          className="mono tnum text-right"
                        />
                      </td>
                      <td className="py-3 px-2">
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
                            value={item.unitPrice}
                            onChange={(e) => setItem(idx, 'unitPrice', e.target.value)}
                            className="mono tnum text-right pl-8"
                            placeholder="0.00"
                            required
                          />
                        </div>
                      </td>
                      <td className="py-3 px-2">
                        <Select
                          value={item.igvType}
                          onValueChange={(v) => v && setItem(idx, 'igvType', v)}
                        >
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            {IGV_TYPES.map((t) => (
                              <SelectItem key={t.value} value={t.value}>
                                {t.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
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
                  ))}
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

          {/* Notas */}
          <Section title="Notas" desc="Opcional. Aparecerá en cada comprobante emitido.">
            <Textarea
              value={form.notes}
              onChange={(e) => set('notes', e.target.value)}
              placeholder="Ej. Servicio correspondiente al periodo de facturación…"
              rows={3}
            />
          </Section>
        </div>

        {/* Right: sticky summary */}
        <aside className="lg:col-span-1">
          <div
            className="rounded-[var(--radius-lg)] border bg-card p-6 lg:sticky lg:top-20"
            style={{ boxShadow: 'var(--shadow-xs)' }}
          >
            <h2 className="t-h2 m-0 mb-4">Resumen</h2>

            <div className="flex flex-col gap-3 t-body-sm">
              <div>
                <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                  Tipo de comprobante
                </div>
                <div className="mt-0.5 font-semibold flex items-center gap-1.5">
                  {form.documentType === '01' ? (
                    <Receipt
                      className="h-3.5 w-3.5"
                      style={{ color: 'var(--brand-toucan-orange)' }}
                    />
                  ) : (
                    <FileText
                      className="h-3.5 w-3.5"
                      style={{ color: 'var(--brand-toucan-orange)' }}
                    />
                  )}
                  {form.documentType === '01' ? 'Factura' : 'Boleta'}{' '}
                  <span className="mono ml-1">{form.serie}</span>
                </div>
              </div>

              <div>
                <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                  Cliente
                </div>
                <div className="mt-0.5 font-semibold truncate">
                  {form.customerName || '—'}
                </div>
                {form.customerDocNumber && (
                  <div className="mono t-caption" style={{ color: 'var(--muted-foreground)' }}>
                    {form.customerDocType === '6' ? 'RUC' : 'Doc.'} {form.customerDocNumber}
                  </div>
                )}
              </div>

              <div
                className="rounded-[var(--radius-md)] border p-3 flex items-center gap-3"
                style={{ borderColor: 'var(--border)' }}
              >
                <span
                  className="flex h-8 w-8 items-center justify-center rounded-md shrink-0"
                  style={{
                    background: 'color-mix(in oklch, var(--accent) 18%, transparent)',
                    color: 'var(--brand-ink)',
                  }}
                >
                  <Repeat className="h-4 w-4" />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                    Cadencia
                  </div>
                  <div className="t-body-sm font-semibold">{cadenceText}</div>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div
                  className="rounded-[var(--radius-md)] border p-3"
                  style={{ borderColor: 'var(--border)' }}
                >
                  <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                    Inicio
                  </div>
                  <div className="mono tnum t-body-sm mt-0.5 font-semibold">
                    {form.startDate || '—'}
                  </div>
                </div>
                <div
                  className="rounded-[var(--radius-md)] border p-3"
                  style={{ borderColor: 'var(--border)' }}
                >
                  <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                    Fin
                  </div>
                  <div className="mono tnum t-body-sm mt-0.5 font-semibold">
                    {form.endDate || 'Sin límite'}
                  </div>
                </div>
              </div>

              <div
                className="rounded-[var(--radius-md)] border p-3 flex items-center justify-between"
                style={{ borderColor: 'var(--border)' }}
              >
                <span className="inline-flex items-center gap-1.5">
                  <Hash className="h-3.5 w-3.5" style={{ color: 'var(--muted-foreground)' }} />
                  Líneas
                </span>
                <span className="mono tnum font-semibold">{items.length}</span>
              </div>

              <div
                className="rounded-[var(--radius-md)] border p-3 space-y-1.5"
                style={{ borderColor: 'var(--border)' }}
              >
                <div className="flex items-center justify-between">
                  <span style={{ color: 'var(--muted-foreground)' }}>Gravado</span>
                  <span className="mono tnum">
                    {currencySymbol} {totals.taxable.toFixed(2)}
                  </span>
                </div>
                {totals.exempt > 0 && (
                  <div className="flex items-center justify-between">
                    <span style={{ color: 'var(--muted-foreground)' }}>Exonerado</span>
                    <span className="mono tnum">
                      {currencySymbol} {totals.exempt.toFixed(2)}
                    </span>
                  </div>
                )}
                {totals.unaffected > 0 && (
                  <div className="flex items-center justify-between">
                    <span style={{ color: 'var(--muted-foreground)' }}>Inafecto</span>
                    <span className="mono tnum">
                      {currencySymbol} {totals.unaffected.toFixed(2)}
                    </span>
                  </div>
                )}
                <div className="flex items-center justify-between">
                  <span style={{ color: 'var(--muted-foreground)' }}>IGV (18%)</span>
                  <span className="mono tnum">
                    {currencySymbol} {totals.igv.toFixed(2)}
                  </span>
                </div>
                <div
                  className="flex items-center justify-between pt-1.5 mt-1"
                  style={{ borderTop: '1px solid var(--border)' }}
                >
                  <span className="font-semibold">Total por emisión</span>
                  <span className="mono tnum font-bold t-num-md">
                    {currencySymbol} {total.toFixed(2)}
                  </span>
                </div>
              </div>
            </div>

            <div className="my-4 h-px" style={{ background: 'var(--border)' }} />

            <div className="flex flex-col gap-2">
              <Button type="submit" size="lg" className="w-full h-12" disabled={isSubmitting}>
                {isSubmitting ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Creando…
                  </>
                ) : (
                  <>
                    <Send className="h-4 w-4 mr-2" /> Crear recurrente
                  </>
                )}
              </Button>
              <Button
                type="button"
                variant="outline"
                size="lg"
                className="w-full h-12"
                onClick={() => router.push('/recurring-invoices')}
              >
                Cancelar
              </Button>
            </div>
            <p
              className="t-caption mt-2.5 text-center"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Quedará activa. La primera emisión se ejecutará en la fecha de inicio.
            </p>
          </div>
        </aside>
      </div>

    </form>
  );
}
