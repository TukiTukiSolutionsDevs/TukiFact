'use client';

import { useState, useEffect, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { api, type SeriesResponse } from '@/lib/api';
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
  ArrowLeft,
  Plus,
  Trash2,
  Send,
  Search,
  Loader2,
  Hash,
  ShieldAlert,
  Building2,
  User as UserIcon,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

interface RefForm {
  documentType: string;
  documentNumber: string;
  documentDate: string;
  invoiceAmount: string;
  invoiceCurrency: string;
  collectionDate: string;
  collectionNumber: string;
  collectionAmount: string;
  exchangeRate: string;
}

const emptyRef = (): RefForm => ({
  documentType: '01',
  documentNumber: '',
  documentDate: '',
  invoiceAmount: '',
  invoiceCurrency: 'PEN',
  collectionDate: '',
  collectionNumber: '1',
  collectionAmount: '',
  exchangeRate: '',
});

const fmt = (n: number, currency = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency }).format(n);

const CUSTOMER_DOC_TYPES = [
  {
    value: '6',
    label: 'RUC',
    sub: '11 dígitos',
    icon: Building2,
    length: 11,
    placeholder: '20XXXXXXXXX',
  },
  {
    value: '1',
    label: 'DNI',
    sub: '8 dígitos',
    icon: UserIcon,
    length: 8,
    placeholder: '4XXXXXXX',
  },
] as const;

const REGIMES = [
  { code: '01', label: 'Venta interna', percent: '2' },
  { code: '02', label: 'Combustible', percent: '1' },
  { code: '03', label: 'CdP imp.', percent: '0.5' },
];

interface LookupStatus {
  configured: boolean;
  provider: string;
  providerName: string;
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
            <p
              className="t-body-sm m-0 mt-0.5"
              style={{ color: 'var(--muted-foreground)' }}
            >
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
              background: active
                ? 'color-mix(in oklch, var(--accent) 18%, transparent)'
                : 'var(--card)',
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
              <span className="block t-body-sm font-semibold leading-tight">
                {o.label}
              </span>
              {o.sub && (
                <span
                  className="block t-caption leading-tight"
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

export default function NewPerceptionPage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [series, setSeries] = useState<SeriesResponse[]>([]);
  const [seriesLoading, setSeriesLoading] = useState(true);
  const [lookupStatus, setLookupStatus] = useState<LookupStatus | null>(null);
  const [isLookingUp, setIsLookingUp] = useState(false);
  const [form, setForm] = useState({
    serie: '',
    customerDocType: '6',
    customerDocNumber: '',
    customerName: '',
    customerAddress: '',
    regimeCode: '01',
    perceptionPercent: '2',
    currency: 'PEN',
    notes: '',
  });
  const [refs, setRefs] = useState<RefForm[]>([emptyRef()]);

  useEffect(() => {
    // Load series tipo 40 (percepción) + lookup status
    api
      .get<SeriesResponse[]>('/v1/series')
      .then((all) => setSeries(all.filter((s) => s.documentType === '40' && s.isActive)))
      .catch(console.error)
      .finally(() => setSeriesLoading(false));

    api
      .get<LookupStatus>('/v1/services/lookup/status')
      .then(setLookupStatus)
      .catch(() => {});
  }, []);

  // Auto-select first series
  useEffect(() => {
    if (series.length > 0 && !form.serie) {
      setForm((f) => ({ ...f, serie: series[0]!.serie }));
    }
  }, [series, form.serie]);

  const set = (key: string, value: string | null) => {
    if (value !== null) setForm((f) => ({ ...f, [key]: value }));
  };

  const setRef = (idx: number, key: string, value: string | null) => {
    if (value === null) return;
    setRefs((prev) =>
      prev.map((r, i) => (i === idx ? { ...r, [key]: value } : r))
    );
  };

  const addRef = () => setRefs((prev) => [...prev, emptyRef()]);
  const removeRef = (idx: number) => {
    if (refs.length <= 1) return;
    setRefs((prev) => prev.filter((_, i) => i !== idx));
  };

  const lookup = async () => {
    if (!(form.customerDocType === '6' || form.customerDocType === '1')) return;
    const endpoint = form.customerDocType === '6' ? 'ruc' : 'dni';
    const expectedLen = form.customerDocType === '6' ? 11 : 8;
    if (form.customerDocNumber.length !== expectedLen) {
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
      }>(`/v1/services/lookup/${endpoint}/${form.customerDocNumber}`);
      const name =
        data.name ||
        data.fullName ||
        [data.firstName, data.lastName, data.motherLastName]
          .filter(Boolean)
          .join(' ') ||
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
      const msg = err instanceof Error ? err.message : 'Error al consultar';
      toast.error(msg);
    } finally {
      setIsLookingUp(false);
    }
  };

  const totals = useMemo(() => {
    const totalCobrado = refs.reduce(
      (sum, r) => sum + (parseFloat(r.collectionAmount) || 0),
      0
    );
    const percent = parseFloat(form.perceptionPercent) || 0;
    const totalPercibido =
      Math.round(totalCobrado * (percent / 100) * 100) / 100;
    const totalAEntregar = totalCobrado + totalPercibido;
    return { totalCobrado, totalPercibido, totalAEntregar };
  }, [refs, form.perceptionPercent]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.serie) {
      toast.error('Selecciona una serie');
      return;
    }
    if (!form.customerDocNumber || !form.customerName) {
      toast.error('Completá los datos del cliente');
      return;
    }
    if (
      refs.some(
        (r) =>
          !r.documentNumber ||
          !r.documentDate ||
          !r.collectionDate ||
          parseFloat(r.invoiceAmount) <= 0 ||
          parseFloat(r.collectionAmount) <= 0
      )
    ) {
      toast.error('Completá todos los documentos relacionados');
      return;
    }

    setIsSubmitting(true);
    try {
      const body = {
        serie: form.serie,
        customerDocType: form.customerDocType,
        customerDocNumber: form.customerDocNumber,
        customerName: form.customerName,
        customerAddress: form.customerAddress || null,
        regimeCode: form.regimeCode,
        perceptionPercent: parseFloat(form.perceptionPercent),
        currency: form.currency,
        notes: form.notes || null,
        references: refs.map((r) => ({
          documentType: r.documentType,
          documentNumber: r.documentNumber,
          documentDate: r.documentDate,
          invoiceAmount: parseFloat(r.invoiceAmount),
          invoiceCurrency: r.invoiceCurrency,
          collectionDate: r.collectionDate,
          collectionNumber: parseInt(r.collectionNumber),
          collectionAmount: parseFloat(r.collectionAmount),
          exchangeRate: r.exchangeRate ? parseFloat(r.exchangeRate) : null,
        })),
      };
      await api.post('/v1/perceptions', body);
      toast.success('Percepción emitida');
      router.push('/perceptions');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al emitir');
    } finally {
      setIsSubmitting(false);
    }
  };

  const selectedSeries = series.find((s) => s.serie === form.serie);
  const nextNumber = selectedSeries
    ? `${selectedSeries.serie}-${String(selectedSeries.currentCorrelative + 1).padStart(8, '0')}`
    : null;

  const hasNoSeries = !seriesLoading && series.length === 0;

  return (
    <form onSubmit={handleSubmit}>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <Link
            href="/perceptions"
            className="inline-flex items-center gap-1 t-body-sm mb-2"
            style={{ color: 'var(--muted-foreground)' }}
          >
            <ArrowLeft className="h-3.5 w-3.5" /> Percepciones
          </Link>
          <h1 className="t-display-lg m-0">Emitir percepción</h1>
          <p
            className="t-body mt-1.5 mb-0"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Catálogo 22 SUNAT — Tipo de documento 40. Se emite al cliente percibido
            sobre los pagos cobrados.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)]">
        {/* Left */}
        <div className="lg:col-span-2 flex flex-col gap-[var(--gap-cards)]">
          {/* Cliente */}
          <Section
            title="Cliente percibido"
            desc="A quién le emites el comprobante de percepción."
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
              onChange={(v) => set('customerDocType', v)}
              options={CUSTOMER_DOC_TYPES}
            />

            <div className="mt-5 grid grid-cols-1 gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Número de documento</Label>
                <div className="flex gap-2">
                  <Input
                    placeholder={
                      form.customerDocType === '6' ? '20XXXXXXXXX' : '4XXXXXXX'
                    }
                    value={form.customerDocNumber}
                    maxLength={form.customerDocType === '6' ? 11 : 8}
                    onChange={(e) =>
                      set('customerDocNumber', e.target.value.replace(/\s/g, ''))
                    }
                    className="mono"
                    required
                  />
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
                </div>
              </div>
              <div>
                <Label className="t-label mb-1.5 block">
                  {form.customerDocType === '6' ? 'Razón Social' : 'Nombre completo'}
                </Label>
                <Input
                  placeholder={
                    form.customerDocType === '6' ? 'MI CLIENTE SAC' : 'Juan Pérez García'
                  }
                  value={form.customerName}
                  onChange={(e) => set('customerName', e.target.value)}
                  style={
                    form.customerDocType === '6'
                      ? { textTransform: 'uppercase' }
                      : undefined
                  }
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
            </div>
          </Section>

          {/* Régimen + serie */}
          <Section
            title="Régimen y serie"
            desc="El régimen define la tasa SUNAT aplicada (Catálogo 22)."
          >
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Serie</Label>
                {seriesLoading ? (
                  <div
                    className="h-10 rounded-[var(--radius-md)] border flex items-center px-3 t-body-sm"
                    style={{
                      borderColor: 'var(--input)',
                      color: 'var(--muted-foreground)',
                    }}
                  >
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Cargando…
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
                      <p className="t-body-sm m-0 font-semibold">
                        Sin series percepción
                      </p>
                      <p
                        className="t-caption m-0"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        Crea serie tipo 40.
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
                    onValueChange={(v) => v != null && set('serie', v)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccionar" />
                    </SelectTrigger>
                    <SelectContent>
                      {series.map((s) => (
                        <SelectItem key={s.id} value={s.serie}>
                          {s.serie} · siguiente{' '}
                          {String(s.currentCorrelative + 1).padStart(8, '0')}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              </div>
              <div>
                <Label className="t-label mb-1.5 block">Régimen (Catálogo 22)</Label>
                <Select
                  value={form.regimeCode}
                  onValueChange={(v) => {
                    if (!v) return;
                    set('regimeCode', v);
                    const r = REGIMES.find((x) => x.code === v);
                    if (r) set('perceptionPercent', r.percent);
                  }}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {REGIMES.map((r) => (
                      <SelectItem key={r.code} value={r.code}>
                        <span className="mono mr-2">{r.code}</span>
                        {r.label} · {r.percent}%
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label className="t-label mb-1.5 block">% Percepción</Label>
                <div
                  className="h-10 rounded-[var(--radius-md)] border flex items-center justify-end px-3 mono tnum t-body-sm font-semibold"
                  style={{
                    background: 'var(--muted)',
                    borderColor: 'var(--input)',
                    color: 'var(--foreground)',
                  }}
                  title="El % se deriva del régimen y no se puede editar (SUNAT lo exige fijo)"
                >
                  {form.perceptionPercent}%
                </div>
              </div>
            </div>

            <div className="mt-4 grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Moneda</Label>
                <Select
                  value={form.currency}
                  onValueChange={(v) => v != null && set('currency', v)}
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
              {nextNumber && (
                <div className="flex items-end">
                  <span
                    className="inline-flex items-center gap-2 rounded-full px-3 py-1.5"
                    style={{
                      background:
                        'color-mix(in oklch, var(--success) 12%, transparent)',
                      color: 'var(--success)',
                    }}
                  >
                    <Hash className="h-3.5 w-3.5" />
                    <span className="t-caption font-semibold mono">
                      Próximo: {nextNumber}
                    </span>
                  </span>
                </div>
              )}
            </div>

            <div className="mt-4">
              <Label className="t-label mb-1.5 block">Observaciones (opcional)</Label>
              <Textarea
                value={form.notes}
                onChange={(e) => set('notes', e.target.value)}
                rows={2}
                placeholder="Notas internas para el comprobante…"
              />
            </div>
          </Section>

          {/* Documentos relacionados */}
          <Section
            title="Documentos relacionados"
            desc={`${refs.length} ${refs.length === 1 ? 'documento' : 'documentos'} sobre los que se aplica la percepción.`}
            right={
              <Button type="button" variant="outline" size="sm" onClick={addRef}>
                <Plus className="h-4 w-4 mr-1.5" /> Agregar
              </Button>
            }
          >
            <div className="flex flex-col gap-4">
              {refs.map((ref, idx) => (
                <div
                  key={idx}
                  className="rounded-[var(--radius-md)] border p-4"
                  style={{ borderColor: 'var(--border)' }}
                >
                  <div className="flex items-center justify-between mb-3">
                    <span
                      className="inline-flex items-center gap-2 t-body-sm font-semibold"
                    >
                      <span
                        className="inline-flex h-6 w-6 items-center justify-center rounded-md mono tnum t-caption font-bold"
                        style={{
                          background:
                            'color-mix(in oklch, var(--accent) 16%, transparent)',
                          color: 'var(--brand-ink)',
                        }}
                      >
                        {idx + 1}
                      </span>
                      Documento {idx + 1}
                    </span>
                    {refs.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeRef(idx)}
                        className="h-8 w-8 inline-flex items-center justify-center rounded-md hover:bg-[var(--muted)] transition-colors"
                        aria-label={`Quitar documento ${idx + 1}`}
                        style={{ color: 'var(--danger)' }}
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                    <div>
                      <Label className="t-label mb-1 block">Tipo</Label>
                      <Select
                        value={ref.documentType}
                        onValueChange={(v) => setRef(idx, 'documentType', v)}
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="01">Factura</SelectItem>
                          <SelectItem value="03">Boleta</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                    <div>
                      <Label className="t-label mb-1 block">Número</Label>
                      <Input
                        placeholder="F001-00000001"
                        value={ref.documentNumber}
                        onChange={(e) =>
                          setRef(idx, 'documentNumber', e.target.value)
                        }
                        className="mono"
                        required
                      />
                    </div>
                    <div>
                      <Label className="t-label mb-1 block">Fecha</Label>
                      <Input
                        type="date"
                        value={ref.documentDate}
                        onChange={(e) =>
                          setRef(idx, 'documentDate', e.target.value)
                        }
                        required
                      />
                    </div>
                    <div>
                      <Label className="t-label mb-1 block">Monto factura</Label>
                      <Input
                        type="number"
                        step="0.01"
                        min={0}
                        value={ref.invoiceAmount}
                        onChange={(e) =>
                          setRef(idx, 'invoiceAmount', e.target.value)
                        }
                        className="mono tnum text-right"
                        required
                      />
                    </div>
                    <div>
                      <Label className="t-label mb-1 block">Fecha cobro</Label>
                      <Input
                        type="date"
                        value={ref.collectionDate}
                        onChange={(e) =>
                          setRef(idx, 'collectionDate', e.target.value)
                        }
                        required
                      />
                    </div>
                    <div>
                      <Label className="t-label mb-1 block">Nro. cobro</Label>
                      <Input
                        type="number"
                        min="1"
                        value={ref.collectionNumber}
                        onChange={(e) =>
                          setRef(idx, 'collectionNumber', e.target.value)
                        }
                        className="mono tnum text-right"
                      />
                    </div>
                    <div>
                      <Label className="t-label mb-1 block">Monto cobrado</Label>
                      <Input
                        type="number"
                        step="0.01"
                        min={0}
                        value={ref.collectionAmount}
                        onChange={(e) =>
                          setRef(idx, 'collectionAmount', e.target.value)
                        }
                        className="mono tnum text-right"
                        required
                      />
                    </div>
                    <div>
                      <Label className="t-label mb-1 block">T.C. (opcional)</Label>
                      <Input
                        type="number"
                        step="0.0001"
                        value={ref.exchangeRate}
                        onChange={(e) =>
                          setRef(idx, 'exchangeRate', e.target.value)
                        }
                        placeholder="3.5270"
                        className="mono tnum text-right"
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </Section>
        </div>

        {/* Right: Sticky summary */}
        <aside className="lg:col-span-1">
          <div
            className="rounded-[var(--radius-lg)] border bg-card p-6 lg:sticky lg:top-20"
            style={{ boxShadow: 'var(--shadow-xs)' }}
          >
            <h2 className="t-h2 m-0 mb-4 flex items-center gap-2">
              <ShieldAlert
                className="h-5 w-5"
                style={{ color: 'var(--brand-ink)' }}
              />
              Resumen percepción
            </h2>

            <div className="flex flex-col gap-2 t-body-sm">
              <div className="flex justify-between">
                <span style={{ color: 'var(--muted-foreground)' }}>
                  Total cobrado
                </span>
                <span className="mono tnum">
                  {fmt(totals.totalCobrado, form.currency)}
                </span>
              </div>
              <div className="flex justify-between">
                <span style={{ color: 'var(--muted-foreground)' }}>
                  % aplicado
                </span>
                <span className="mono tnum">
                  {form.perceptionPercent}%
                </span>
              </div>
            </div>

            <div className="my-4 h-px" style={{ background: 'var(--border)' }} />

            <div className="flex items-baseline justify-between mb-1">
              <span className="t-h2 m-0">Percibido</span>
              <span className="t-num-lg mono">
                {fmt(totals.totalPercibido, form.currency)}
              </span>
            </div>
            <p
              className="t-caption m-0 mb-5"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Total a entregar (cobrado + percibido):{' '}
              <span className="mono tnum font-semibold">
                {fmt(totals.totalAEntregar, form.currency)}
              </span>
            </p>

            <Button
              type="submit"
              size="lg"
              className="w-full h-12"
              disabled={
                isSubmitting ||
                hasNoSeries ||
                totals.totalPercibido === 0 ||
                !form.customerName
              }
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Emitiendo…
                </>
              ) : (
                <>
                  <Send className="h-4 w-4 mr-2" /> Emitir percepción
                </>
              )}
            </Button>

            {hasNoSeries && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--warning)' }}
              >
                Crea una serie tipo 40 antes de emitir.
              </p>
            )}
            {!hasNoSeries && totals.totalPercibido === 0 && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Ingresa los montos cobrados de los documentos.
              </p>
            )}
          </div>
        </aside>
      </div>
    </form>
  );
}
