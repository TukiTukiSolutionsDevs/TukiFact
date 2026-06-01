'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { api, type SeriesResponse, type DocumentResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import {
  Plus,
  Trash2,
  Send,
  Search,
  Loader2,
  ShieldCheck,
  Hash,
  FileSpreadsheet,
  Receipt,
  ArrowUpRight,
  Building2,
  User as UserIcon,
  Globe,
  CircleSlash,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

interface ItemRow {
  description: string;
  productCode: string;
  quantity: number;
  unitMeasure: string;
  unitPrice: number;
  igvType: string;
}

const emptyItem: ItemRow = {
  description: '',
  productCode: '',
  quantity: 1,
  unitMeasure: 'NIU',
  unitPrice: 0,
  igvType: '10',
};
const IGV_RATE = 0.18;

const fmt = (n: number) =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' }).format(n);

const fmtPlain = (n: number) =>
  new Intl.NumberFormat('es-PE', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n);

interface LookupStatus {
  configured: boolean;
  provider: string;
  providerName: string;
}

const DOC_TYPES = [
  { value: '01', label: 'Factura', sub: 'Para empresas (con RUC)', icon: FileSpreadsheet, defaultCustomerDoc: '6' },
  { value: '03', label: 'Boleta', sub: 'Para consumidor final', icon: Receipt, defaultCustomerDoc: '1' },
] as const;

const CUSTOMER_DOC_TYPES = [
  { value: '6', label: 'RUC', sub: '11 dígitos', icon: Building2, length: 11, placeholder: '20XXXXXXXXX' },
  { value: '1', label: 'DNI', sub: '8 dígitos', icon: UserIcon, length: 8, placeholder: '4XXXXXXX' },
  { value: '4', label: 'CE', sub: 'Carné extranjería', icon: Globe, length: 12, placeholder: 'CE12345...' },
  { value: '7', label: 'Pasaporte', sub: 'Internacional', icon: Globe, length: 12, placeholder: 'P12345...' },
  { value: '0', label: 'Sin doc.', sub: 'Boleta < S/ 700', icon: CircleSlash, length: 0, placeholder: '' },
] as const;

function useLookup() {
  const [isSearching, setIsSearching] = useState(false);
  const [lookupStatus, setLookupStatus] = useState<LookupStatus | null>(null);

  useEffect(() => {
    api
      .get<LookupStatus>('/v1/services/lookup/status')
      .then(setLookupStatus)
      .catch(() => {});
  }, []);

  const lookup = async (
    docType: string,
    docNumber: string
  ): Promise<{ name: string; address?: string } | null> => {
    if (docType === '0') return null;
    const endpoint = docType === '6' ? 'ruc' : 'dni';
    const expectedLen = docType === '6' ? 11 : 8;
    if (docNumber.length !== expectedLen) {
      toast.error(`El número debe tener ${expectedLen} dígitos`);
      return null;
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
      }>(`/v1/services/lookup/${endpoint}/${docNumber}`);
      const name =
        data.name ||
        data.fullName ||
        [data.firstName, data.lastName, data.motherLastName].filter(Boolean).join(' ') ||
        '';
      if (!name) {
        toast.error('No se encontraron datos para ese número');
        return null;
      }
      toast.success(`Datos encontrados: ${name}`);
      return { name, address: data.address };
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al consultar datos';
      if (msg.includes('No hay proveedor')) {
        toast.error('Configura un proveedor de datos en Configuración → Servicios Externos');
      } else {
        toast.error(msg);
      }
      return null;
    } finally {
      setIsSearching(false);
    }
  };

  return { lookup, isSearching, lookupStatus };
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

export default function NewDocumentPage() {
  const router = useRouter();
  const { lookup, isSearching, lookupStatus } = useLookup();
  const [isLoading, setIsLoading] = useState(false);
  const [series, setSeries] = useState<SeriesResponse[]>([]);
  const [seriesLoading, setSeriesLoading] = useState(true);
  const [form, setForm] = useState({
    documentType: '01' as '01' | '03',
    serie: '',
    currency: 'PEN',
    customerDocType: '6',
    customerDocNumber: '',
    customerName: '',
    customerAddress: '',
    customerEmail: '',
    notes: '',
  });
  const [items, setItems] = useState<ItemRow[]>([{ ...emptyItem }]);

  useEffect(() => {
    api
      .get<SeriesResponse[]>('/v1/series')
      .then(setSeries)
      .catch(console.error)
      .finally(() => setSeriesLoading(false));
  }, []);

  const filteredSeries = series.filter(
    (s) => s.documentType === form.documentType && s.isActive
  );

  useEffect(() => {
    if (filteredSeries.length > 0 && !form.serie) {
      setForm((f) => ({ ...f, serie: filteredSeries[0]!.serie }));
    } else if (filteredSeries.length === 0 && form.serie) {
      setForm((f) => ({ ...f, serie: '' }));
    }
  }, [filteredSeries, form.serie]);

  const selectedSeries = filteredSeries.find((s) => s.serie === form.serie);
  const nextNumber = selectedSeries
    ? `${selectedSeries.serie}-${String(selectedSeries.currentCorrelative + 1).padStart(8, '0')}`
    : null;

  const selectedDocType = DOC_TYPES.find((d) => d.value === form.documentType)!;
  const selectedCustomerDocType =
    CUSTOMER_DOC_TYPES.find((d) => d.value === form.customerDocType) ?? CUSTOMER_DOC_TYPES[0];

  const onTypeChange = (v: '01' | '03') => {
    const dt = DOC_TYPES.find((d) => d.value === v)!;
    setForm((f) => ({
      ...f,
      documentType: v,
      serie: '',
      customerDocType: dt.defaultCustomerDoc,
    }));
  };

  const addItem = () => setItems((prev) => [...prev, { ...emptyItem }]);
  const removeItem = (idx: number) =>
    setItems((prev) => (prev.length > 1 ? prev.filter((_, i) => i !== idx) : prev));
  const updateItem = (idx: number, field: keyof ItemRow, value: string | number) => {
    setItems((prev) =>
      prev.map((item, i) => (i === idx ? { ...item, [field]: value } : item))
    );
  };

  const calcItem = (item: ItemRow) => {
    const subtotal = item.quantity * item.unitPrice;
    const igvAmount = item.igvType === '10' ? Math.round(subtotal * IGV_RATE * 100) / 100 : 0;
    return { subtotal, igvAmount, total: subtotal + igvAmount };
  };

  const totals = items.reduce(
    (acc, item) => {
      const c = calcItem(item);
      return {
        gravada: acc.gravada + (item.igvType === '10' ? c.subtotal : 0),
        exonerada: acc.exonerada + (item.igvType === '20' ? c.subtotal : 0),
        inafecta: acc.inafecta + (item.igvType === '30' ? c.subtotal : 0),
        igv: acc.igv + c.igvAmount,
        total: acc.total + c.total,
      };
    },
    { gravada: 0, exonerada: 0, inafecta: 0, igv: 0, total: 0 }
  );

  const handleSubmit = async () => {
    if (!form.serie) {
      toast.error('Selecciona una serie');
      return;
    }
    if (!form.customerDocNumber) {
      toast.error('Ingresa el documento del cliente');
      return;
    }
    if (!form.customerName) {
      toast.error('Ingresa el nombre del cliente');
      return;
    }
    if (items.some((i) => !i.description || i.unitPrice <= 0)) {
      toast.error('Completa todos los items');
      return;
    }

    setIsLoading(true);
    try {
      const res = await api.post<DocumentResponse>('/v1/documents', {
        documentType: form.documentType,
        serie: form.serie,
        currency: form.currency,
        customerDocType: form.customerDocType,
        customerDocNumber: form.customerDocNumber,
        customerName: form.customerName,
        customerAddress: form.customerAddress || undefined,
        customerEmail: form.customerEmail || undefined,
        notes: form.notes || undefined,
        items: items.map((i) => ({
          productCode: i.productCode || undefined,
          description: i.description,
          quantity: i.quantity,
          unitMeasure: i.unitMeasure,
          unitPrice: i.unitPrice,
          igvType: i.igvType,
        })),
      });
      toast.success(`${res.fullNumber} emitido — ${res.status}`);
      router.push(`/documents/${res.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al emitir');
    } finally {
      setIsLoading(false);
    }
  };

  const hasNoSeries = !seriesLoading && filteredSeries.length === 0;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Emitir comprobante</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Crea una factura o boleta y envíala a SUNAT.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)]">
        {/* Left: Form sections */}
        <div className="lg:col-span-2 flex flex-col gap-[var(--gap-cards)]">
          {/* Tipo + serie */}
          <Section
            title="¿Qué vas a emitir?"
            desc="Elige el tipo de comprobante. La serie y correlativo se asignan automáticamente."
          >
            <PillGroup<'01' | '03'>
              value={form.documentType}
              onChange={onTypeChange}
              options={DOC_TYPES}
            />

            <div className="mt-5 grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Serie</Label>
                {seriesLoading ? (
                  <div
                    className="h-10 rounded-[var(--radius-md)] border flex items-center px-3 t-body-sm"
                    style={{ borderColor: 'var(--input)', color: 'var(--muted-foreground)' }}
                  >
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Cargando series…
                  </div>
                ) : hasNoSeries ? (
                  <div
                    className="rounded-[var(--radius-md)] border-dashed border-2 p-3 flex items-start gap-3"
                    style={{ borderColor: 'var(--border)' }}
                  >
                    <Hash
                      className="h-5 w-5 shrink-0 mt-0.5"
                      style={{ color: 'var(--warning)' }}
                    />
                    <div className="flex-1 min-w-0">
                      <p className="t-body-sm m-0 font-semibold">Sin series para {selectedDocType.label}</p>
                      <p className="t-caption m-0" style={{ color: 'var(--muted-foreground)' }}>
                        Necesitas crear una serie antes de emitir.
                      </p>
                    </div>
                    <Button asChild size="sm">
                      <Link href="/series">
                        <Plus className="h-3.5 w-3.5 mr-1" /> Crear
                      </Link>
                    </Button>
                  </div>
                ) : (
                  <Select
                    value={form.serie}
                    onValueChange={(v) => v != null && setForm((f) => ({ ...f, serie: v }))}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccionar serie" />
                    </SelectTrigger>
                    <SelectContent>
                      {filteredSeries.map((s) => (
                        <SelectItem key={s.id} value={s.serie}>
                          {s.serie} · siguiente {String(s.currentCorrelative + 1).padStart(8, '0')}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              </div>
              <div>
                <Label className="t-label mb-1.5 block">Moneda</Label>
                <Select
                  value={form.currency}
                  onValueChange={(v) => v != null && setForm((f) => ({ ...f, currency: v }))}
                >
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

            {nextNumber && (
              <div
                className="mt-4 inline-flex items-center gap-2 rounded-full px-3 py-1.5"
                style={{
                  background: 'color-mix(in oklch, var(--success) 12%, transparent)',
                  color: 'var(--success)',
                }}
              >
                <ShieldCheck className="h-3.5 w-3.5" />
                <span className="t-caption font-semibold mono">
                  Próximo: {nextNumber}
                </span>
              </div>
            )}
          </Section>

          {/* Cliente */}
          <Section
            title="Cliente"
            desc="Si tu proveedor de búsqueda está configurado, podemos traer los datos automáticamente."
            right={
              lookupStatus?.configured ? (
                <span
                  className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 t-caption"
                  style={{
                    background: 'var(--muted)',
                    color: 'var(--muted-foreground)',
                  }}
                >
                  <Search className="h-3 w-3" />
                  {lookupStatus.providerName}
                </span>
              ) : undefined
            }
          >
            <PillGroup
              value={form.customerDocType}
              onChange={(v) => setForm((f) => ({ ...f, customerDocType: v }))}
              options={CUSTOMER_DOC_TYPES}
            />

            <div className="mt-5 grid grid-cols-1 gap-4">
              {form.customerDocType !== '0' && (
                <div>
                  <Label className="t-label mb-1.5 block">Número de documento</Label>
                  <div className="flex gap-2">
                    <Input
                      placeholder={selectedCustomerDocType.placeholder}
                      value={form.customerDocNumber}
                      maxLength={selectedCustomerDocType.length || undefined}
                      onChange={(e) =>
                        setForm((f) => ({
                          ...f,
                          customerDocNumber: e.target.value.replace(/\s/g, ''),
                        }))
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
                        onClick={async () => {
                          const result = await lookup(form.customerDocType, form.customerDocNumber);
                          if (result) {
                            setForm((f) => ({
                              ...f,
                              customerName: result.name,
                              customerAddress: result.address ?? f.customerAddress,
                            }));
                          }
                        }}
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
                  onChange={(e) => setForm((f) => ({ ...f, customerName: e.target.value }))}
                  style={form.customerDocType === '6' ? { textTransform: 'uppercase' } : undefined}
                />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Dirección (opcional)</Label>
                  <Input
                    placeholder="Av. Principal 123, Lima"
                    value={form.customerAddress}
                    onChange={(e) => setForm((f) => ({ ...f, customerAddress: e.target.value }))}
                  />
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Email (opcional)</Label>
                  <Input
                    type="email"
                    placeholder="cliente@empresa.pe"
                    value={form.customerEmail}
                    onChange={(e) => setForm((f) => ({ ...f, customerEmail: e.target.value }))}
                  />
                  <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                    Si lo agregas, le enviaremos el PDF y XML al cliente.
                  </p>
                </div>
              </div>
            </div>
          </Section>

          {/* Items */}
          <Section
            title="Productos y servicios"
            desc={`${items.length} ${items.length === 1 ? 'línea' : 'líneas'}`}
            right={
              <Button variant="outline" size="sm" onClick={addItem}>
                <Plus className="h-4 w-4 mr-1.5" /> Agregar línea
              </Button>
            }
          >
            <div className="-mx-6 overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr
                    className="t-overline"
                    style={{
                      color: 'var(--muted-foreground)',
                      background: 'var(--muted)',
                    }}
                  >
                    <th className="text-left py-2.5 pl-6 pr-2 w-10">#</th>
                    <th className="text-left py-2.5 px-2">Descripción</th>
                    <th className="text-right py-2.5 px-2 w-24">Cant.</th>
                    <th className="text-right py-2.5 px-2 w-32">P. Unit.</th>
                    <th className="text-left py-2.5 px-2 w-32">IGV</th>
                    <th className="text-right py-2.5 px-2 w-32">Subtotal</th>
                    <th className="py-2.5 pr-6 pl-2 w-10" aria-label="acciones" />
                  </tr>
                </thead>
                <tbody>
                  {items.map((item, idx) => {
                    const c = calcItem(item);
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
                            placeholder="Ej. Consultoría mes de mayo"
                            value={item.description}
                            onChange={(e) => updateItem(idx, 'description', e.target.value)}
                          />
                        </td>
                        <td className="py-3 px-2">
                          <Input
                            type="number"
                            min={0.01}
                            step={1}
                            inputMode="decimal"
                            value={item.quantity || ''}
                            onChange={(e) =>
                              updateItem(idx, 'quantity', parseFloat(e.target.value) || 0)
                            }
                            className="mono tnum text-right"
                          />
                        </td>
                        <td className="py-3 px-2">
                          <Input
                            type="number"
                            min={0}
                            step={0.01}
                            inputMode="decimal"
                            value={item.unitPrice || ''}
                            onChange={(e) =>
                              updateItem(idx, 'unitPrice', parseFloat(e.target.value) || 0)
                            }
                            className="mono tnum text-right"
                          />
                        </td>
                        <td className="py-3 px-2">
                          <Select
                            value={item.igvType}
                            onValueChange={(v) =>
                              v != null && updateItem(idx, 'igvType', v)
                            }
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
                        <td className="py-3 px-2 text-right mono tnum t-body-sm font-semibold">
                          {fmtPlain(c.total)}
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

          {/* Notas */}
          <Section title="Observaciones" desc="Opcional. Aparecerán en el PDF del comprobante.">
            <Textarea
              placeholder="Ej. Pago a 30 días, transferencia BCP cta 194-XXXXXXX..."
              value={form.notes}
              onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
              rows={3}
            />
          </Section>
        </div>

        {/* Right: Sticky summary */}
        <aside className="lg:col-span-1">
          <div
            className="rounded-[var(--radius-lg)] border bg-card p-6 lg:sticky lg:top-20"
            style={{ boxShadow: 'var(--shadow-xs)' }}
          >
            <h2 className="t-h2 m-0 mb-4">Resumen</h2>

            <div className="flex flex-col gap-2 t-body-sm">
              {totals.gravada > 0 && (
                <div className="flex justify-between">
                  <span style={{ color: 'var(--muted-foreground)' }}>Op. gravada</span>
                  <span className="mono tnum">{fmt(totals.gravada)}</span>
                </div>
              )}
              {totals.exonerada > 0 && (
                <div className="flex justify-between">
                  <span style={{ color: 'var(--muted-foreground)' }}>Op. exonerada</span>
                  <span className="mono tnum">{fmt(totals.exonerada)}</span>
                </div>
              )}
              {totals.inafecta > 0 && (
                <div className="flex justify-between">
                  <span style={{ color: 'var(--muted-foreground)' }}>Op. inafecta</span>
                  <span className="mono tnum">{fmt(totals.inafecta)}</span>
                </div>
              )}
              <div className="flex justify-between">
                <span style={{ color: 'var(--muted-foreground)' }}>IGV 18%</span>
                <span className="mono tnum">{fmt(totals.igv)}</span>
              </div>
            </div>

            <div
              className="my-4 h-px"
              style={{ background: 'var(--border)' }}
            />

            <div className="flex items-baseline justify-between mb-5">
              <span className="t-h2 m-0">Total</span>
              <span className="t-num-lg mono">{fmt(totals.total)}</span>
            </div>

            <Button
              size="lg"
              className="w-full h-12"
              onClick={handleSubmit}
              disabled={isLoading || totals.total === 0 || hasNoSeries}
            >
              {isLoading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Emitiendo…
                </>
              ) : (
                <>
                  <Send className="h-4 w-4 mr-2" /> Emitir comprobante
                </>
              )}
            </Button>
            {totals.total === 0 && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Agrega al menos una línea con precio mayor a 0.
              </p>
            )}
            {hasNoSeries && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--warning)' }}
              >
                Primero crea una serie para emitir.
              </p>
            )}

            <Link
              href="/series"
              className="inline-flex items-center gap-1 mt-4 t-body-sm font-medium"
              style={{ color: 'var(--info)' }}
            >
              Administrar series <ArrowUpRight className="h-3.5 w-3.5" />
            </Link>
          </div>
        </aside>
      </div>
    </div>
  );
}
