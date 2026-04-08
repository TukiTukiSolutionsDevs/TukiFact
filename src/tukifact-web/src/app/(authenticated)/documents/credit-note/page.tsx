'use client';

import { useState, useEffect, useMemo } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { api, type SeriesResponse, type DocumentResponse, type PaginatedResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Separator } from '@/components/ui/separator';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { ArrowLeft, Search, Plus, Trash2, Send, FileText } from 'lucide-react';
import { toast } from 'sonner';

const IGV_RATE = 0.18;
const fmt = (n: number) =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' }).format(n);

const CREDIT_NOTE_REASONS = [
  { code: '01', label: '01 - Anulación de la operación' },
  { code: '02', label: '02 - Anulación por error en el RUC' },
  { code: '03', label: '03 - Corrección por error en la descripción' },
  { code: '04', label: '04 - Descuento global' },
  { code: '05', label: '05 - Descuento por ítem' },
  { code: '06', label: '06 - Devolución total' },
  { code: '07', label: '07 - Devolución por ítem' },
  { code: '08', label: '08 - Bonificación' },
  { code: '09', label: '09 - Disminución en el valor' },
  { code: '10', label: '10 - Otros conceptos' },
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
  const igvAmount = item.igvType === '10' ? Math.round(subtotal * IGV_RATE * 100) / 100 : 0;
  return { subtotal, igvAmount, total: subtotal + igvAmount };
};

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
        const ncSeries = all.filter((s) => s.documentType === '07' && s.isActive);
        setSeries(ncSeries);
        if (ncSeries.length > 0) setSerie(ncSeries[0].serie);
      })
      .catch(console.error);
  }, []);

  // Pre-load from ref param if provided
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
      if (filtered.length === 0) toast.info('No se encontraron documentos con ese criterio');
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
      { description: '', quantity: 1, unitMeasure: 'NIU', unitPrice: 0, igvType: '10', productCode: '' },
    ]);

  const removeItem = (idx: number) =>
    setItems((prev) => prev.filter((_, i) => i !== idx));

  const updateItem = (idx: number, field: keyof ItemRow, value: string | number) =>
    setItems((prev) => prev.map((it, i) => (i === idx ? { ...it, [field]: value } : it)));

  const totals = useMemo(
    () =>
      items.reduce(
        (acc, item) => {
          const c = calcItem(item);
          return {
            gravada: acc.gravada + (item.igvType === '10' ? c.subtotal : 0),
            exonerada: acc.exonerada + (item.igvType === '20' ? c.subtotal : 0),
            igv: acc.igv + c.igvAmount,
            total: acc.total + c.total,
          };
        },
        { gravada: 0, exonerada: 0, igv: 0, total: 0 }
      ),
    [items]
  );

  const handleSubmit = async () => {
    if (!refDoc) { toast.error('Selecciona el documento de referencia'); return; }
    if (!serie) { toast.error('Selecciona una serie'); return; }
    if (!description.trim()) { toast.error('Ingresa una descripción'); return; }
    if (items.length === 0 || items.some((i) => !i.description || i.unitPrice <= 0)) {
      toast.error('Completa todos los items');
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

  return (
    <div className="space-y-6 max-w-5xl">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={() => router.push('/documents')}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Volver
        </Button>
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Emitir Nota de Crédito</h1>
          <p className="text-muted-foreground">Catálogo 09 SUNAT — Tipo de documento 07</p>
        </div>
      </div>

      {/* Step 1: Select reference document */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <span className="flex h-6 w-6 items-center justify-center rounded-full bg-blue-100 text-blue-700 text-xs font-bold">
              1
            </span>
            Documento de Referencia
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {refDoc ? (
            <div className="flex items-center gap-4 p-3 rounded-lg border bg-green-50 border-green-200">
              <FileText className="h-5 w-5 text-green-600 shrink-0" />
              <div className="flex-1 min-w-0">
                <p className="font-medium text-sm">{refDoc.fullNumber}</p>
                <p className="text-xs text-muted-foreground truncate">
                  {refDoc.customerName} — {fmt(refDoc.total)}
                </p>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  setRefDoc(null);
                  setItems([]);
                }}
                className="text-red-500 shrink-0"
              >
                Cambiar
              </Button>
            </div>
          ) : (
            <>
              <div className="flex gap-2">
                <Input
                  placeholder="Buscar por número (ej: F001-000001)"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                />
                <Button onClick={handleSearch} disabled={isSearching}>
                  <Search className="h-4 w-4" />
                </Button>
              </div>
              {searchResults.length > 0 && (
                <div className="border rounded-lg divide-y max-h-48 overflow-y-auto">
                  {searchResults.map((doc) => (
                    <button
                      key={doc.id}
                      onClick={() => selectRefDoc(doc)}
                      className="w-full text-left px-4 py-2 hover:bg-muted text-sm transition-colors"
                    >
                      <span className="font-mono font-medium">{doc.fullNumber}</span>
                      <span className="ml-3 text-muted-foreground">{doc.customerName}</span>
                      <span className="ml-3 font-medium">{fmt(doc.total)}</span>
                    </button>
                  ))}
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>

      {refDoc && (
        <>
          {/* Step 2: NC details */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <span className="flex h-6 w-6 items-center justify-center rounded-full bg-blue-100 text-blue-700 text-xs font-bold">
                  2
                </span>
                Datos de la Nota de Crédito
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <Label>Serie</Label>
                  <Select
                    value={serie}
                    onValueChange={(v) => { if (v != null) setSerie(v); }}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccionar serie" />
                    </SelectTrigger>
                    <SelectContent>
                      {ncSeries.map((s) => (
                        <SelectItem key={s.id} value={s.serie}>
                          {s.serie} (#{s.currentCorrelative + 1})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {ncSeries.length === 0 && (
                    <p className="text-xs text-red-500 mt-1">
                      No hay series activas para NC (tipo 07). Crea una en Configuración.
                    </p>
                  )}
                </div>
                <div>
                  <Label>Motivo (Catálogo 09)</Label>
                  <Select
                    value={creditNoteReason}
                    onValueChange={(v) => { if (v != null) setCreditNoteReason(v); }}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {CREDIT_NOTE_REASONS.map((r) => (
                        <SelectItem key={r.code} value={r.code}>
                          {r.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <Label>Moneda</Label>
                  <Select
                    value={currency}
                    onValueChange={(v) => { if (v != null) setCurrency(v); }}
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
              <div>
                <Label>Descripción / Sustento</Label>
                <Textarea
                  placeholder="Describe el motivo de la nota de crédito..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={2}
                />
              </div>
            </CardContent>
          </Card>

          {/* Step 3: Items */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-base flex items-center gap-2">
                <span className="flex h-6 w-6 items-center justify-center rounded-full bg-blue-100 text-blue-700 text-xs font-bold">
                  3
                </span>
                Items
              </CardTitle>
              <Button variant="outline" size="sm" onClick={addItem}>
                <Plus className="h-4 w-4 mr-1" /> Agregar
              </Button>
            </CardHeader>
            <CardContent className="p-0">
              {items.length === 0 ? (
                <div className="py-8 text-center text-muted-foreground text-sm">
                  <p>No hay items. Agrega items manualmente o</p>
                  <Button
                    variant="link"
                    size="sm"
                    onClick={() => refDoc && loadItemsFromDoc(refDoc)}
                  >
                    copiar desde documento de referencia
                  </Button>
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-10">#</TableHead>
                      <TableHead>Descripción</TableHead>
                      <TableHead className="w-20">Cant.</TableHead>
                      <TableHead className="w-24">P.Unit</TableHead>
                      <TableHead className="w-24">IGV</TableHead>
                      <TableHead className="w-28 text-right">Total</TableHead>
                      <TableHead className="w-10" />
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {items.map((item, idx) => {
                      const c = calcItem(item);
                      return (
                        <TableRow key={idx}>
                          <TableCell className="text-muted-foreground">{idx + 1}</TableCell>
                          <TableCell>
                            <Input
                              value={item.description}
                              onChange={(e) => updateItem(idx, 'description', e.target.value)}
                              className="h-8"
                            />
                          </TableCell>
                          <TableCell>
                            <Input
                              type="number"
                              min={0.01}
                              step={0.01}
                              value={item.quantity}
                              onChange={(e) =>
                                updateItem(idx, 'quantity', parseFloat(e.target.value) || 0)
                              }
                              className="h-8"
                            />
                          </TableCell>
                          <TableCell>
                            <Input
                              type="number"
                              min={0}
                              step={0.01}
                              value={item.unitPrice}
                              onChange={(e) =>
                                updateItem(idx, 'unitPrice', parseFloat(e.target.value) || 0)
                              }
                              className="h-8"
                            />
                          </TableCell>
                          <TableCell>
                            <Select
                              value={item.igvType}
                              onValueChange={(v) => { if (v != null) updateItem(idx, 'igvType', v); }}
                            >
                              <SelectTrigger className="h-8">
                                <SelectValue />
                              </SelectTrigger>
                              <SelectContent>
                                <SelectItem value="10">Gravado</SelectItem>
                                <SelectItem value="20">Exonerado</SelectItem>
                                <SelectItem value="30">Inafecto</SelectItem>
                              </SelectContent>
                            </Select>
                          </TableCell>
                          <TableCell className="text-right font-mono">{fmt(c.total)}</TableCell>
                          <TableCell>
                            {items.length > 0 && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => removeItem(idx)}
                                className="text-red-500 h-8 w-8 p-0"
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            )}
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>

          {/* Totals */}
          <div className="flex justify-end">
            <Card className="w-72">
              <CardContent className="pt-6 space-y-2 text-sm">
                {totals.gravada > 0 && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Op. Gravada</span>
                    <span>{fmt(totals.gravada)}</span>
                  </div>
                )}
                {totals.exonerada > 0 && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Op. Exonerada</span>
                    <span>{fmt(totals.exonerada)}</span>
                  </div>
                )}
                <div className="flex justify-between">
                  <span className="text-muted-foreground">IGV 18%</span>
                  <span>{fmt(totals.igv)}</span>
                </div>
                <Separator />
                <div className="flex justify-between text-xl font-bold">
                  <span>TOTAL NC</span>
                  <span>{fmt(totals.total)}</span>
                </div>
              </CardContent>
            </Card>
          </div>

          <div className="flex justify-end">
            <Button size="lg" onClick={handleSubmit} disabled={isLoading}>
              <Send className="h-4 w-4 mr-2" />
              {isLoading ? 'Emitiendo...' : 'Emitir Nota de Crédito'}
            </Button>
          </div>
        </>
      )}
    </div>
  );
}
