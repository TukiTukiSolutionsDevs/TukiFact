'use client';

import { useState, useEffect, useMemo } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import {
  api,
  type SeriesResponse,
  type DocumentResponse,
  type PaginatedResponse,
} from '@/lib/api';
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
  ArrowLeft,
  Search,
  Plus,
  Trash2,
  Send,
  FileText,
  Loader2,
  ShieldCheck,
  Hash,
  CheckCircle2,
  RotateCcw,
  Receipt,
} from 'lucide-react';
import { toast } from 'sonner';

const IGV_RATE = 0.18;

const fmt = (n: number) =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' }).format(n);

const fmtPlain = (n: number) =>
  new Intl.NumberFormat('es-PE', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(n);

const CREDIT_NOTE_REASONS = [
  { code: '01', label: 'Anulación de la operación' },
  { code: '02', label: 'Anulación por error en el RUC' },
  { code: '03', label: 'Corrección por error en la descripción' },
  { code: '04', label: 'Descuento global' },
  { code: '05', label: 'Descuento por ítem' },
  { code: '06', label: 'Devolución total' },
  { code: '07', label: 'Devolución por ítem' },
  { code: '08', label: 'Bonificación' },
  { code: '09', label: 'Disminución en el valor' },
  { code: '10', label: 'Otros conceptos' },
];

interface ItemRow {
  description: string;
  quantity: number;
  unitMeasure: string;
  unitPrice: number;
  igvType: string;
  productCode: string;
}

const calcItem = (item: ItemRow) => {
  const subtotal = item.quantity * item.unitPrice;
  const igvAmount =
    item.igvType === '10' ? Math.round(subtotal * IGV_RATE * 100) / 100 : 0;
  return { subtotal, igvAmount, total: subtotal + igvAmount };
};

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

export default function CreditNotePage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const refId = searchParams.get('ref');

  const [series, setSeries] = useState<SeriesResponse[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  // Reference document search
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<DocumentResponse[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [refDoc, setRefDoc] = useState<DocumentResponse | null>(null);

  // Form
  const [serie, setSerie] = useState('');
  const [creditNoteReason, setCreditNoteReason] = useState('01');
  const [description, setDescription] = useState('');
  const [currency, setCurrency] = useState('PEN');
  const [items, setItems] = useState<ItemRow[]>([]);

  // Load series (NC = documentType 07)
  useEffect(() => {
    api
      .get<SeriesResponse[]>('/v1/series')
      .then((all) => {
        const ncSeries = all.filter(
          (s) => s.documentType === '07' && s.isActive
        );
        setSeries(ncSeries);
        if (ncSeries.length > 0) setSerie(ncSeries[0]!.serie);
      })
      .catch(console.error);
  }, []);

  // Pre-load from ?ref= param
  useEffect(() => {
    if (!refId) return;
    api
      .get<DocumentResponse>(`/v1/documents/${refId}`)
      .then((doc) => {
        setRefDoc(doc);
        loadItemsFromDoc(doc);
        setCurrency(doc.currency);
      })
      .catch(() => toast.error('No se pudo cargar el documento de referencia'));
  }, [refId]);

  const loadItemsFromDoc = (doc: DocumentResponse) => {
    setItems(
      doc.items.map((it) => ({
        description: it.description,
        quantity: it.quantity,
        unitMeasure: it.unitMeasure,
        unitPrice: it.unitPrice,
        igvType: it.igvType,
        productCode: it.productCode ?? '',
      }))
    );
  };

  const handleSearch = async () => {
    if (!searchQuery.trim()) return;
    setIsSearching(true);
    try {
      const res = await api.get<PaginatedResponse<DocumentResponse>>(
        `/v1/documents?page=1&pageSize=20`
      );
      const filtered = res.data.filter(
        (d) =>
          (d.documentType === '01' || d.documentType === '03') &&
          d.status === 'accepted' &&
          d.fullNumber.toLowerCase().includes(searchQuery.toLowerCase())
      );
      setSearchResults(filtered);
      if (filtered.length === 0)
        toast.info('No se encontraron facturas/boletas aceptadas con ese número');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error en búsqueda');
    } finally {
      setIsSearching(false);
    }
  };

  const selectRefDoc = (doc: DocumentResponse) => {
    setRefDoc(doc);
    loadItemsFromDoc(doc);
    setCurrency(doc.currency);
    setSearchResults([]);
    setSearchQuery('');
  };

  const addItem = () =>
    setItems((prev) => [
      ...prev,
      {
        description: '',
        quantity: 1,
        unitMeasure: 'NIU',
        unitPrice: 0,
        igvType: '10',
        productCode: '',
      },
    ]);

  const removeItem = (idx: number) =>
    setItems((prev) => prev.filter((_, i) => i !== idx));

  const updateItem = (idx: number, field: keyof ItemRow, value: string | number) =>
    setItems((prev) =>
      prev.map((it, i) => (i === idx ? { ...it, [field]: value } : it))
    );

  const totals = useMemo(
    () =>
      items.reduce(
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
      ),
    [items]
  );

  const handleSubmit = async () => {
    if (!refDoc) {
      toast.error('Selecciona el documento de referencia');
      return;
    }
    if (!serie) {
      toast.error('Selecciona una serie');
      return;
    }
    if (!description.trim()) {
      toast.error('Ingresa el sustento o motivo de la nota');
      return;
    }
    if (items.length === 0 || items.some((i) => !i.description || i.unitPrice <= 0)) {
      toast.error('Completa todos los items con descripción y precio mayor a 0');
      return;
    }

    setIsLoading(true);
    try {
      const res = await api.post<DocumentResponse>('/v1/documents/credit-note', {
        serie,
        referenceDocumentId: refDoc.id,
        creditNoteReason,
        description,
        currency,
        items: items.map((i) => ({
          productCode: i.productCode || undefined,
          description: i.description,
          quantity: i.quantity,
          unitMeasure: i.unitMeasure,
          unitPrice: i.unitPrice,
          igvType: i.igvType,
        })),
      });
      toast.success(`${res.fullNumber} emitida`);
      router.push(`/documents/${res.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al emitir');
    } finally {
      setIsLoading(false);
    }
  };

  const ncSeries = series.filter((s) => s.documentType === '07' && s.isActive);
  const selectedSeries = ncSeries.find((s) => s.serie === serie);
  const nextNumber = selectedSeries
    ? `${selectedSeries.serie}-${String(selectedSeries.currentCorrelative + 1).padStart(8, '0')}`
    : null;

  const reasonLabel =
    CREDIT_NOTE_REASONS.find((r) => r.code === creditNoteReason)?.label ??
    creditNoteReason;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <Link
            href="/documents"
            className="inline-flex items-center gap-1 t-body-sm mb-2"
            style={{ color: 'var(--muted-foreground)' }}
          >
            <ArrowLeft className="h-3.5 w-3.5" /> Documentos
          </Link>
          <h1 className="t-display-lg m-0">Emitir nota de crédito</h1>
          <p
            className="t-body mt-1.5 mb-0"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Catálogo 09 SUNAT — Tipo de documento 07. Anula, ajusta o devuelve sobre una
            factura o boleta aceptada por SUNAT.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)]">
        {/* Left: Form sections */}
        <div className="lg:col-span-2 flex flex-col gap-[var(--gap-cards)]">
          {/* Section 1: Reference document */}
          <Section
            title="Documento de referencia"
            desc="Busca la factura o boleta original (debe estar aceptada por SUNAT)."
          >
            {refDoc ? (
              <div
                className="rounded-[var(--radius-md)] border p-4 flex items-start gap-3"
                style={{
                  background: 'color-mix(in oklch, var(--success) 6%, var(--card))',
                  borderColor: 'var(--success)',
                }}
              >
                <div
                  className="h-10 w-10 rounded-md flex items-center justify-center shrink-0"
                  style={{
                    background:
                      'color-mix(in oklch, var(--success) 14%, transparent)',
                    color: 'var(--success)',
                  }}
                >
                  <CheckCircle2 className="h-5 w-5" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="t-body font-semibold mono">{refDoc.fullNumber}</div>
                  <p
                    className="t-body-sm m-0 mt-0.5 truncate"
                    style={{ color: 'var(--muted-foreground)' }}
                  >
                    {refDoc.customerName}
                  </p>
                  <p
                    className="t-caption m-0 mt-0.5"
                    style={{ color: 'var(--muted-foreground)' }}
                  >
                    Total original:{' '}
                    <span className="mono tnum font-semibold">
                      {fmt(refDoc.total)}
                    </span>
                  </p>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    setRefDoc(null);
                    setItems([]);
                    setSearchResults([]);
                  }}
                >
                  <RotateCcw className="h-3.5 w-3.5 mr-1" /> Cambiar
                </Button>
              </div>
            ) : (
              <>
                <div className="flex gap-2">
                  <div className="relative flex-1">
                    <Search
                      className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
                      style={{ color: 'var(--muted-foreground)' }}
                    />
                    <Input
                      placeholder="Buscar por número (ej. F001-000001)"
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                      className="pl-9 mono"
                    />
                  </div>
                  <Button
                    onClick={handleSearch}
                    disabled={isSearching || !searchQuery.trim()}
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
                </div>
                {searchResults.length > 0 && (
                  <div className="mt-3 rounded-[var(--radius-md)] border overflow-hidden">
                    {searchResults.map((doc, i) => (
                      <button
                        key={doc.id}
                        type="button"
                        onClick={() => selectRefDoc(doc)}
                        className="w-full text-left px-4 py-3 hover:bg-[var(--muted)] transition-colors flex items-center gap-3"
                        style={{
                          borderTop:
                            i === 0 ? undefined : '1px solid var(--border)',
                        }}
                      >
                        <FileText
                          className="h-4 w-4 shrink-0"
                          style={{ color: 'var(--muted-foreground)' }}
                        />
                        <div className="flex-1 min-w-0">
                          <div className="t-body-sm font-semibold mono">
                            {doc.fullNumber}
                          </div>
                          <div
                            className="t-caption truncate"
                            style={{ color: 'var(--muted-foreground)' }}
                          >
                            {doc.customerName}
                          </div>
                        </div>
                        <span className="mono tnum t-body-sm font-semibold">
                          {fmt(doc.total)}
                        </span>
                      </button>
                    ))}
                  </div>
                )}
              </>
            )}
          </Section>

          {refDoc && (
            <>
              {/* Section 2: Motivo + serie */}
              <Section
                title="Motivo y serie"
                desc="El motivo se reporta a SUNAT en el Catálogo 09."
              >
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div>
                    <Label className="t-label mb-1.5 block">Serie</Label>
                    {ncSeries.length === 0 ? (
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
                            Sin series NC
                          </p>
                          <p
                            className="t-caption m-0"
                            style={{ color: 'var(--muted-foreground)' }}
                          >
                            Crea una serie tipo 07.
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
                        value={serie}
                        onValueChange={(v) => v != null && setSerie(v)}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Seleccionar serie" />
                        </SelectTrigger>
                        <SelectContent>
                          {ncSeries.map((s) => (
                            <SelectItem key={s.id} value={s.serie}>
                              {s.serie} · siguiente{' '}
                              {String(s.currentCorrelative + 1).padStart(8, '0')}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  </div>
                  <div className="md:col-span-2">
                    <Label className="t-label mb-1.5 block">Motivo (Catálogo 09)</Label>
                    <Select
                      value={creditNoteReason}
                      onValueChange={(v) => v != null && setCreditNoteReason(v)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {CREDIT_NOTE_REASONS.map((r) => (
                          <SelectItem key={r.code} value={r.code}>
                            <span className="mono mr-2">{r.code}</span>
                            {r.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="mt-4 grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <Label className="t-label mb-1.5 block">Moneda</Label>
                    <Select
                      value={currency}
                      onValueChange={(v) => v != null && setCurrency(v)}
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
                        <ShieldCheck className="h-3.5 w-3.5" />
                        <span className="t-caption font-semibold mono">
                          Próximo: {nextNumber}
                        </span>
                      </span>
                    </div>
                  )}
                </div>

                <div className="mt-4">
                  <Label className="t-label mb-1.5 block">
                    Descripción / sustento
                  </Label>
                  <Textarea
                    placeholder="Describe el motivo (visible en el comprobante y reportado a SUNAT)…"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    rows={2}
                  />
                </div>
              </Section>

              {/* Section 3: Items */}
              <Section
                title="Items afectados"
                desc={`${items.length} ${items.length === 1 ? 'línea' : 'líneas'} · puedes copiar las del documento original.`}
                right={
                  items.length > 0 ? (
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => refDoc && loadItemsFromDoc(refDoc)}
                      >
                        <RotateCcw className="h-4 w-4 mr-1.5" /> Copiar original
                      </Button>
                      <Button variant="outline" size="sm" onClick={addItem}>
                        <Plus className="h-4 w-4 mr-1.5" /> Agregar línea
                      </Button>
                    </div>
                  ) : undefined
                }
              >
                {items.length === 0 ? (
                  <div
                    className="rounded-[var(--radius-md)] border-2 border-dashed p-8 text-center"
                    style={{ borderColor: 'var(--border)' }}
                  >
                    <Receipt
                      className="h-10 w-10 mx-auto mb-3"
                      style={{ color: 'var(--muted-foreground)' }}
                    />
                    <p className="t-body m-0 font-semibold">Sin items todavía</p>
                    <p
                      className="t-body-sm mt-1 mb-4"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      Copia los items del documento de referencia o agrégalos
                      manualmente.
                    </p>
                    <div className="flex gap-2 justify-center flex-wrap">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => refDoc && loadItemsFromDoc(refDoc)}
                      >
                        <RotateCcw className="h-4 w-4 mr-1.5" /> Copiar del original
                      </Button>
                      <Button size="sm" onClick={addItem}>
                        <Plus className="h-4 w-4 mr-1.5" /> Agregar línea
                      </Button>
                    </div>
                  </div>
                ) : (
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
                          <th
                            className="py-2.5 pr-6 pl-2 w-10"
                            aria-label="acciones"
                          />
                        </tr>
                      </thead>
                      <tbody>
                        {items.map((item, idx) => {
                          const c = calcItem(item);
                          return (
                            <tr
                              key={idx}
                              style={{ borderTop: '1px solid var(--border)' }}
                            >
                              <td
                                className="py-3 pl-6 pr-2 t-body-sm tnum"
                                style={{ color: 'var(--muted-foreground)' }}
                              >
                                {idx + 1}
                              </td>
                              <td className="py-3 px-2">
                                <Input
                                  placeholder="Descripción"
                                  value={item.description}
                                  onChange={(e) =>
                                    updateItem(idx, 'description', e.target.value)
                                  }
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
                                    updateItem(
                                      idx,
                                      'quantity',
                                      parseFloat(e.target.value) || 0
                                    )
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
                                    updateItem(
                                      idx,
                                      'unitPrice',
                                      parseFloat(e.target.value) || 0
                                    )
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
                                <button
                                  type="button"
                                  onClick={() => removeItem(idx)}
                                  className="h-8 w-8 inline-flex items-center justify-center rounded-md hover:bg-[var(--muted)] transition-colors"
                                  aria-label={`Quitar línea ${idx + 1}`}
                                  style={{ color: 'var(--danger)' }}
                                >
                                  <Trash2 className="h-4 w-4" />
                                </button>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                )}
              </Section>
            </>
          )}
        </div>

        {/* Right: Sticky summary */}
        <aside className="lg:col-span-1">
          <div
            className="rounded-[var(--radius-lg)] border bg-card p-6 lg:sticky lg:top-20"
            style={{ boxShadow: 'var(--shadow-xs)' }}
          >
            <h2 className="t-h2 m-0 mb-4">Resumen NC</h2>

            {refDoc && (
              <div
                className="rounded-[var(--radius-md)] p-3 mb-4"
                style={{ background: 'var(--muted)' }}
              >
                <p
                  className="t-caption m-0"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Afecta a
                </p>
                <p className="t-body-sm m-0 mono font-semibold mt-0.5">
                  {refDoc.fullNumber}
                </p>
                <p
                  className="t-caption m-0 mt-1"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Motivo {creditNoteReason}: {reasonLabel}
                </p>
              </div>
            )}

            <div className="flex flex-col gap-2 t-body-sm">
              {totals.gravada > 0 && (
                <div className="flex justify-between">
                  <span style={{ color: 'var(--muted-foreground)' }}>
                    Op. gravada
                  </span>
                  <span className="mono tnum">{fmt(totals.gravada)}</span>
                </div>
              )}
              {totals.exonerada > 0 && (
                <div className="flex justify-between">
                  <span style={{ color: 'var(--muted-foreground)' }}>
                    Op. exonerada
                  </span>
                  <span className="mono tnum">{fmt(totals.exonerada)}</span>
                </div>
              )}
              {totals.inafecta > 0 && (
                <div className="flex justify-between">
                  <span style={{ color: 'var(--muted-foreground)' }}>
                    Op. inafecta
                  </span>
                  <span className="mono tnum">{fmt(totals.inafecta)}</span>
                </div>
              )}
              <div className="flex justify-between">
                <span style={{ color: 'var(--muted-foreground)' }}>IGV 18%</span>
                <span className="mono tnum">{fmt(totals.igv)}</span>
              </div>
            </div>

            <div className="my-4 h-px" style={{ background: 'var(--border)' }} />

            <div className="flex items-baseline justify-between mb-5">
              <span className="t-h2 m-0">Total NC</span>
              <span className="t-num-lg mono">{fmt(totals.total)}</span>
            </div>

            <Button
              size="lg"
              className="w-full h-12"
              onClick={handleSubmit}
              disabled={
                isLoading ||
                !refDoc ||
                ncSeries.length === 0 ||
                totals.total === 0 ||
                !description.trim()
              }
            >
              {isLoading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Emitiendo…
                </>
              ) : (
                <>
                  <Send className="h-4 w-4 mr-2" /> Emitir nota de crédito
                </>
              )}
            </Button>
            {!refDoc && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Selecciona primero el documento de referencia.
              </p>
            )}
            {refDoc && ncSeries.length === 0 && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--warning)' }}
              >
                Crea una serie tipo 07 para poder emitir.
              </p>
            )}
            {refDoc && ncSeries.length > 0 && totals.total === 0 && (
              <p
                className="t-caption mt-2.5 text-center"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Agrega al menos una línea con precio mayor a 0.
              </p>
            )}
            {refDoc &&
              ncSeries.length > 0 &&
              totals.total > 0 &&
              !description.trim() && (
                <p
                  className="t-caption mt-2.5 text-center"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Ingresa el sustento del motivo.
                </p>
              )}
          </div>
        </aside>
      </div>
    </div>
  );
}
