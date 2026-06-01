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
  ShieldCheck,
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
  paymentDate: string;
  paymentNumber: string;
  paymentAmount: string;
  exchangeRate: string;
}

const emptyRef = (): RefForm => ({
  documentType: '01',
  documentNumber: '',
  documentDate: '',
  invoiceAmount: '',
  invoiceCurrency: 'PEN',
  paymentDate: '',
  paymentNumber: '1',
  paymentAmount: '',
  exchangeRate: '',
});

const fmt = (n: number, currency = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency }).format(n);

const SUPPLIER_DOC_TYPES = [
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
  { code: '01', label: 'Tasa estándar', percent: '3' },
  { code: '02', label: 'Tasa especial', percent: '6' },
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

export default function NewRetentionPage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [series, setSeries] = useState<SeriesResponse[]>([]);
  const [seriesLoading, setSeriesLoading] = useState(true);
  const [lookupStatus, setLookupStatus] = useState<LookupStatus | null>(null);
  const [isLookingUp, setIsLookingUp] = useState(false);
  const [form, setForm] = useState({
    serie: '',
    supplierDocType: '6',
    supplierDocNumber: '',
    supplierName: '',
    supplierAddress: '',
    regimeCode: '01',
    retentionPercent: '3',
    currency: 'PEN',
    notes: '',
  });
  const [refs, setRefs] = useState<RefForm[]>([emptyRef()]);

  useEffect(() => {
    // Series tipo 20 (retención) + lookup status
    api
      .get<SeriesResponse[]>('/v1/series')
      .then((all) => setSeries(all.filter((s) => s.documentType === '20' && s.isActive)))
      .catch(console.error)
      .finally(() => setSeriesLoading(false));

    api
      .get<LookupStatus>('/v1/services/lookup/status')
      .then(setLookupStatus)
      .catch(() => {});
  }, []);

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
    if (!(form.supplierDocType === '6' || form.supplierDocType === '1')) return;
    const endpoint = form.supplierDocType === '6' ? 'ruc' : 'dni';
    const expectedLen = form.supplierDocType === '6' ? 11 : 8;
    if (form.supplierDocNumber.length !== expectedLen) {
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
      }>(`/v1/services/lookup/${endpoint}/${form.supplierDocNumber}`);
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
        supplierName: name,
        supplierAddress: data.address ?? f.supplierAddress,
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
    const totalPagado = refs.reduce(
      (sum, r) => sum + (parseFloat(r.paymentAmount) || 0),
      0
    );
    const percent = parseFloat(form.retentionPercent) || 0;
    const totalRetenido =
      Math.round(totalPagado * (percent / 100) * 100) / 100;
    const totalNeto = totalPagado - totalRetenido;
    return { totalPagado, totalRetenido, totalNeto };
  }, [refs, form.retentionPercent]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.serie) {
      toast.error('Selecciona una serie');
      return;
    }
    if (!form.supplierDocNumber || !form.supplierName) {
      toast.error('Completá los datos del proveedor');
      return;
    }
    if (
      refs.some(
        (r) =>
          !r.documentNumber ||
          !r.documentDate ||
          !r.paymentDate ||
          parseFloat(r.invoiceAmount) <= 0 ||
          parseFloat(r.paymentAmount) <= 0
      )
    ) {
      toast.error('Completá todos los documentos relacionados');
      return;
    }

    setIsSubmitting(true);
    try {
      const body = {
        serie: form.serie,
        supplierDocType: form.supplierDocType,
        supplierDocNumber: form.supplierDocNumber,
        supplierName: form.supplierName,
        supplierAddress: form.supplierAddress || null,
        regimeCode: form.regimeCode,
        retentionPercent: parseFloat(form.retentionPercent),
        currency: form.currency,
        notes: form.notes || null,
        references: refs.map((r) => ({
          documentType: r.documentType,
          documentNumber: r.documentNumber,
          documentDate: r.documentDate,
          invoiceAmount: parseFloat(r.invoiceAmount),
          invoiceCurrency: r.invoiceCurrency,
          paymentDate: r.paymentDate,
          paymentNumber: parseInt(r.paymentNumber),
          paymentAmount: parseFloat(r.paymentAmount),
          exchangeRate: r.exchangeRate ? parseFloat(r.exchangeRate) : null,
        })),
      };
      await api.post('/v1/retentions', body);
      toast.success('Retención emitida');
      router.push('/retentions');
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
            href="/retentions"
            className="inline-flex items-center gap-1 t-body-sm mb-2"
            style={{ color: 'var(--muted-foreground)' }}
          >
            <ArrowLeft className="h-3.5 w-3.5" /> Retenciones
          </Link>
          <h1 className="t-display-lg m-0">Emitir retención</h1>
          <p
            className="t-body mt-1.5 mb-0"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Catálogo 23 SUNAT — Tipo de documento 20. Se emite al proveedor sobre
            los pagos retenidos.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)]">
        {/* Left */}
        <div className="lg:col-span-2 flex flex-col gap-[var(--gap-cards)]">
          {/* Proveedor */}
          <Section
            title="Proveedor retenido"
            desc="A quién le emites el comprobante de retención."
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
              value={form.supplierDocType}
              onChange={(v) => set('supplierDocType', v)}
              options={SUPPLIER_DOC_TYPES}
            />

            <div className="mt-5 grid grid-cols-1 gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Número de documento</Label>
                <div className="flex gap-2">
                  <Input
                    placeholder={
                      form.supplierDocType === '6' ? '20XXXXXXXXX' : '4XXXXXXX'
                    }
                    value={form.supplierDocNumber}
                    maxLength={form.supplierDocType === '6' ? 11 : 8}
                    onChange={(e) =>
                      set('supplierDocNumber', e.target.value.replace(/\s/g, ''))
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
                  {form.supplierDocType === '6' ? 'Razón Social' : 'Nombre completo'}
                </Label>
                <Input
                  placeholder={
                    form.supplierDocType === '6' ? 'MI PROVEEDOR SAC' : 'Juan Pérez García'
                  }
                  value={form.supplierName}
                  onChange={(e) => set('supplierName', e.target.value)}
                  style={
                    form.supplierDocType === '6'
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
                  value={form.supplierAddress}
                  onChange={(e) => set('supplierAddress', e.target.value)}
                />
              </div>
            </div>
          </Section>

          {/* Régimen + serie */}
          <Section
            title="Régimen y serie"
            desc="El régimen define la tasa SUNAT aplicada (Catálogo 23)."
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
                        Sin series retención
                      </p>
                      <p
                        className="t-caption m-0"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        Crea serie tipo 20.
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
                <Label className="t-label mb-1.5 block">Régimen (Catálogo 23)</Label>
                <Select
                  value={form.regimeCode}
                  onValueChange={(v) => {
                    if (!v) return;
                    set('regimeCode', v);
                    const r = REGIMES.find((x) => x.code === v);
                    if (r) set('retentionPercent', r.percent);
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
                <Label className="t-label mb-1.5 block">% Retención</Label>
                <div
                  className="h-10 rounded-[var(--radius-md)] border flex items-center justify-end px-3 mono tnum t-body-sm font-semibold"
                  style={{
                    background: 'var(--muted)',
                    borderColor: 'var(--input)',
                    color: 'var(--foreground)',
                  }}
                  title="El % se deriva del régimen y no se puede editar (SUNAT lo exige fijo)"
                >
                  {form.retentionPercent}%
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
            desc={`${refs.length} ${refs.length === 1 ? 'documento' : 'documentos'} sobre los que se aplica la retención.`}
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
                      <Label className="t-label mb-1 block">Fecha pago</Label>
                      <Input
                        type="date"
                        value={ref.paymentDate}
                        onChange={(e) =>
                          setRef(idx, 'paymentDate', e.target.value)
                        }
                        required
                      />
                    </div>
                    <div>
                      <Label className="t-label mb-1 block">Nro. pago</Label>
                      <Input
                        type="number"
                        min="1"
                        value={ref.paymentNumber}
                        onChange={(e) =>
                          setRef(idx, 'paymentNumber', e.target.value)
                        }
                        className="mono tnum text-right"
                      />
                    </div>
                    <div>
                      <Label className="t-label mb-1 block">Monto pagado</Label>
                      <Input
                        type="number"
                        step="0.01"
                        min={0}
                        value={ref.paymentAmount}
                        onChange={(e) =>
                          setRef(idx, 'paymentAmount', e.target.value)
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
              <ShieldCheck
                className="h-5 w-5"
                style={{ color: 'var(--brand-ink)' }}
              />
              Resumen retención
            </h2>

            <div className="flex flex-col gap-2 t-body-sm">
              <div className="flex justify-between">
                <span style={{ color: 'var(--muted-foreground)' }}>
                  Total pagado
                </span>
                <span className="mono tnum">
                  {fmt(totals.totalPagado, form.currency)}
                </span>
              </div>
              <div className="flex justify-between">
                <span style={{ color: 'var(--muted-foreground)' }}>
                  % aplicado
                </span>
                <span className="mono tnum">{form.retentionPercent}%</span>
              </div>
            </div>

            <div className="my-4 h-px" style={{ background: 'var(--border)' }} />

            <div className="flex items-baseline justify-between mb-1">
              <span className="t-h2 m-0">Retenido</span>
              <span className="t-num-lg mono">
                {fmt(totals.totalRetenido, form.currency)}
              </span>
            </div>
            <p
              className="t-caption m-0 mb-5"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Neto a entregar al proveedor:{' '}
              <span className="mono tnum font-semibold">
                {fmt(totals.totalNeto, form.currency)}
              </span>
            </p>

            <Button
              type="submit"
              size="lg"
              className="w-full h-12"
              disabled={
                isSubmitting ||
                hasNoSeries ||
                totals.totalRetenido === 0 ||
                !form.supplierName
              }
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Emitiendo…
                </>
              ) : (
                <>
                  <Send className="h-4 w-4 mr-2" /> Emitir retención
                </>
              )}
            </Button>

            {hasNoSeries && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--warning)' }}
              >
                Crea una serie tipo 20 antes de emitir.
              </p>
            )}
            {!hasNoSeries && totals.totalRetenido === 0 && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Ingresa los montos pagados de los documentos.
              </p>
            )}
          </div>
        </aside>
      </div>
    </form>
  );
}
