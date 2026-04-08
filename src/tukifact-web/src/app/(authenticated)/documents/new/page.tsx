'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { api, type SeriesResponse, type DocumentResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Separator } from '@/components/ui/separator';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Plus, Trash2, Send, Search, Loader2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { toast } from 'sonner';

interface ItemRow {
  description: string;
  productCode: string;
  quantity: number;
  unitMeasure: string;
  unitPrice: number;
  igvType: string;
}

const emptyItem: ItemRow = { description: '', productCode: '', quantity: 1, unitMeasure: 'NIU', unitPrice: 0, igvType: '10' };
const IGV_RATE = 0.18;
const fmt = (n: number) => new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' }).format(n);

interface LookupStatus { configured: boolean; provider: string; providerName: string; }

function useLookup() {
  const [isSearching, setIsSearching] = useState(false);
  const [lookupStatus, setLookupStatus] = useState<LookupStatus | null>(null);

  useEffect(() => {
    api.get<LookupStatus>('/v1/services/lookup/status').then(setLookupStatus).catch(() => {});
  }, []);

  const lookup = async (docType: string, docNumber: string): Promise<{ name: string; address?: string } | null> => {
    if (docType === '0') return null;
    const endpoint = docType === '6' ? 'ruc' : 'dni';
    const expectedLen = docType === '6' ? 11 : 8;
    if (docNumber.length !== expectedLen) {
      toast.error(`El número debe tener ${expectedLen} dígitos`);
      return null;
    }

    setIsSearching(true);
    try {
      const data = await api.get<{ name?: string; fullName?: string; firstName?: string; lastName?: string; motherLastName?: string; address?: string }>(`/v1/services/lookup/${endpoint}/${docNumber}`);
      const name = data.name || data.fullName || [data.firstName, data.lastName, data.motherLastName].filter(Boolean).join(' ') || '';
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

export default function NewDocumentPage() {
  const router = useRouter();
  const { lookup, isSearching, lookupStatus } = useLookup();
  const [isLoading, setIsLoading] = useState(false);
  const [series, setSeries] = useState<SeriesResponse[]>([]);
  const [form, setForm] = useState({
    documentType: '01',
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
    api.get<SeriesResponse[]>('/v1/series').then(setSeries).catch(console.error);
  }, []);

  const filteredSeries = series.filter(s => s.documentType === form.documentType && s.isActive);

  useEffect(() => {
    if (filteredSeries.length > 0 && !form.serie) {
      setForm(f => ({ ...f, serie: filteredSeries[0].serie }));
    }
  }, [filteredSeries, form.serie]);

  const addItem = () => setItems(prev => [...prev, { ...emptyItem }]);
  const removeItem = (idx: number) => setItems(prev => prev.filter((_, i) => i !== idx));
  const updateItem = (idx: number, field: keyof ItemRow, value: string | number) => {
    setItems(prev => prev.map((item, i) => i === idx ? { ...item, [field]: value } : item));
  };

  // Calculations
  const calcItem = (item: ItemRow) => {
    const subtotal = item.quantity * item.unitPrice;
    const igvAmount = item.igvType === '10' ? Math.round(subtotal * IGV_RATE * 100) / 100 : 0;
    return { subtotal, igvAmount, total: subtotal + igvAmount };
  };

  const totals = items.reduce((acc, item) => {
    const c = calcItem(item);
    return {
      gravada: acc.gravada + (item.igvType === '10' ? c.subtotal : 0),
      exonerada: acc.exonerada + (item.igvType === '20' ? c.subtotal : 0),
      inafecta: acc.inafecta + (item.igvType === '30' ? c.subtotal : 0),
      igv: acc.igv + c.igvAmount,
      total: acc.total + c.total,
    };
  }, { gravada: 0, exonerada: 0, inafecta: 0, igv: 0, total: 0 });

  const handleSubmit = async () => {
    if (!form.serie) { toast.error('Selecciona una serie'); return; }
    if (!form.customerDocNumber) { toast.error('Ingresa el documento del cliente'); return; }
    if (!form.customerName) { toast.error('Ingresa el nombre del cliente'); return; }
    if (items.some(i => !i.description || i.unitPrice <= 0)) { toast.error('Completa todos los items'); return; }

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
        items: items.map(i => ({
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
    } finally { setIsLoading(false); }
  };

  return (
    <div className="space-y-6 max-w-5xl">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Emitir Comprobante</h1>
        <p className="text-muted-foreground">Genera un nuevo comprobante electrónico</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Document Type */}
        <Card>
          <CardHeader><CardTitle className="text-base">Documento</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Tipo</Label>
                <Select value={form.documentType} onValueChange={v => v != null && setForm(f => ({ ...f, documentType: v, serie: '' }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="01">Factura</SelectItem>
                    <SelectItem value="03">Boleta de Venta</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Serie</Label>
                <Select value={form.serie} onValueChange={v => v != null && setForm(f => ({ ...f, serie: v }))}>
                  <SelectTrigger><SelectValue placeholder="Seleccionar" /></SelectTrigger>
                  <SelectContent>
                    {filteredSeries.map(s => (
                      <SelectItem key={s.id} value={s.serie}>{s.serie} (#{s.currentCorrelative + 1})</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div>
              <Label>Moneda</Label>
              <Select value={form.currency} onValueChange={v => v != null && setForm(f => ({ ...f, currency: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="PEN">Soles (PEN)</SelectItem>
                  <SelectItem value="USD">Dólares (USD)</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </CardContent>
        </Card>

        {/* Customer */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="text-base">Cliente</CardTitle>
              {lookupStatus?.configured && (
                <Badge variant="secondary" className="text-[10px] font-normal">
                  <Search className="h-3 w-3 mr-1" /> {lookupStatus.providerName}
                </Badge>
              )}
            </div>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-3 gap-3">
              <div>
                <Label>Doc</Label>
                <Select value={form.customerDocType} onValueChange={v => v != null && setForm(f => ({ ...f, customerDocType: v }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="6">RUC</SelectItem>
                    <SelectItem value="1">DNI</SelectItem>
                    <SelectItem value="0">Sin doc</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="col-span-2">
                <Label>Número</Label>
                <div className="flex gap-2">
                  <Input placeholder={form.customerDocType === '6' ? '20XXXXXXXXX' : '4XXXXXXX'}
                    value={form.customerDocNumber}
                    maxLength={form.customerDocType === '6' ? 11 : 8}
                    onChange={e => setForm(f => ({ ...f, customerDocNumber: e.target.value }))} />
                  {form.customerDocType !== '0' && (
                    <Button type="button" variant="outline" size="icon" disabled={isSearching || !lookupStatus?.configured}
                      title={lookupStatus?.configured ? `Buscar con ${lookupStatus.providerName}` : 'Configura un proveedor en Ajustes → Servicios Externos'}
                      onClick={async () => {
                        const result = await lookup(form.customerDocType, form.customerDocNumber);
                        if (result) {
                          setForm(f => ({ ...f, customerName: result.name }));
                        }
                      }}>
                      {isSearching ? <Loader2 className="h-4 w-4 animate-spin" /> : <Search className="h-4 w-4" />}
                    </Button>
                  )}
                </div>
              </div>
            </div>
            <div>
              <Label>Razón Social / Nombre</Label>
              <Input placeholder="Nombre del cliente" value={form.customerName}
                onChange={e => setForm(f => ({ ...f, customerName: e.target.value }))} />
            </div>
            <div>
              <Label>Email (opcional)</Label>
              <Input type="email" placeholder="cliente@empresa.pe" value={form.customerEmail}
                onChange={e => setForm(f => ({ ...f, customerEmail: e.target.value }))} />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Items */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base">Items</CardTitle>
          <Button variant="outline" size="sm" onClick={addItem}><Plus className="h-4 w-4 mr-1" /> Agregar</Button>
        </CardHeader>
        <CardContent className="p-0">
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
                      <Input placeholder="Descripción del producto/servicio" value={item.description}
                        onChange={e => updateItem(idx, 'description', e.target.value)} className="h-8" />
                    </TableCell>
                    <TableCell>
                      <Input type="number" min={0.01} step={0.01} value={item.quantity}
                        onChange={e => updateItem(idx, 'quantity', parseFloat(e.target.value) || 0)} className="h-8" />
                    </TableCell>
                    <TableCell>
                      <Input type="number" min={0} step={0.01} value={item.unitPrice}
                        onChange={e => updateItem(idx, 'unitPrice', parseFloat(e.target.value) || 0)} className="h-8" />
                    </TableCell>
                    <TableCell>
                      <Select value={item.igvType} onValueChange={v => v != null && updateItem(idx, 'igvType', v)}>
                        <SelectTrigger className="h-8"><SelectValue /></SelectTrigger>
                        <SelectContent>
                          <SelectItem value="10">Gravado</SelectItem>
                          <SelectItem value="20">Exonerado</SelectItem>
                          <SelectItem value="30">Inafecto</SelectItem>
                        </SelectContent>
                      </Select>
                    </TableCell>
                    <TableCell className="text-right font-mono">{fmt(c.total)}</TableCell>
                    <TableCell>
                      {items.length > 1 && (
                        <Button variant="ghost" size="sm" onClick={() => removeItem(idx)} className="text-red-500 h-8 w-8 p-0">
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Notes + Totals */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Card>
          <CardContent className="pt-6">
            <Label>Observaciones (opcional)</Label>
            <Textarea placeholder="Notas adicionales..." value={form.notes}
              onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} rows={3} />
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6 space-y-2 text-sm">
            {totals.gravada > 0 && <div className="flex justify-between"><span className="text-muted-foreground">Op. Gravada</span><span>{fmt(totals.gravada)}</span></div>}
            {totals.exonerada > 0 && <div className="flex justify-between"><span className="text-muted-foreground">Op. Exonerada</span><span>{fmt(totals.exonerada)}</span></div>}
            {totals.inafecta > 0 && <div className="flex justify-between"><span className="text-muted-foreground">Op. Inafecta</span><span>{fmt(totals.inafecta)}</span></div>}
            <div className="flex justify-between"><span className="text-muted-foreground">IGV 18%</span><span>{fmt(totals.igv)}</span></div>
            <Separator />
            <div className="flex justify-between text-xl font-bold"><span>TOTAL</span><span>{fmt(totals.total)}</span></div>
          </CardContent>
        </Card>
      </div>

      <div className="flex justify-end">
        <Button size="lg" onClick={handleSubmit} disabled={isLoading}>
          <Send className="h-4 w-4 mr-2" />
          {isLoading ? 'Emitiendo...' : 'Emitir Comprobante'}
        </Button>
      </div>
    </div>
  );
}
