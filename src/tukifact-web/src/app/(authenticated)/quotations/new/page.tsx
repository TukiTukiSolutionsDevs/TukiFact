'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import { ArrowLeft, Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

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
  productCode: '', description: '', quantity: '1',
  unitMeasure: 'NIU', unitPrice: '', igvType: '10', discount: '0',
});

export default function NewQuotationPage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState({
    validUntil: '',
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

  const set = (key: string, value: string | null) => { if (value !== null) setForm(f => ({ ...f, [key]: value })); };

  const setItem = (idx: number, key: string, value: string | null) => {
    if (value === null) return;
    setItems(prev => prev.map((item, i) => i === idx ? { ...item, [key]: value } : item));
  };

  const addItem = () => setItems(prev => [...prev, emptyItem()]);
  const removeItem = (idx: number) => {
    if (items.length <= 1) return;
    setItems(prev => prev.filter((_, i) => i !== idx));
  };

  const calcTotal = () => {
    let subtotal = 0, igv = 0;
    for (const item of items) {
      const qty = parseFloat(item.quantity) || 0;
      const price = parseFloat(item.unitPrice) || 0;
      const disc = parseFloat(item.discount) || 0;
      const taxable = qty * price - disc;
      subtotal += taxable;
      if (item.igvType === '10') igv += taxable * 0.18;
    }
    return { subtotal: subtotal.toFixed(2), igv: igv.toFixed(2), total: (subtotal + igv).toFixed(2) };
  };

  const totals = calcTotal();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.customerDocNumber || !form.customerName || !form.validUntil) {
      toast.error('Completá los datos del cliente y fecha de validez');
      return;
    }
    if (items.some(i => !i.description || !i.unitPrice)) {
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
        items: items.map(i => ({
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
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex items-center gap-4">
        <Button type="button" variant="ghost" size="sm" onClick={() => router.push('/quotations')}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Cotizaciones
        </Button>
        <h1 className="text-2xl font-bold tracking-tight">Nueva Cotización</h1>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardHeader><CardTitle className="text-base">Cliente</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Tipo Doc.</Label>
                <Select value={form.customerDocType} onValueChange={(v) => set('customerDocType', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="6">RUC</SelectItem>
                    <SelectItem value="1">DNI</SelectItem>
                    <SelectItem value="0">Sin documento</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Nro. Documento</Label>
                <Input value={form.customerDocNumber} onChange={e => set('customerDocNumber', e.target.value)} required />
              </div>
            </div>
            <div>
              <Label>Razón Social / Nombre</Label>
              <Input value={form.customerName} onChange={e => set('customerName', e.target.value)} required />
            </div>
            <div>
              <Label>Dirección</Label>
              <Input value={form.customerAddress} onChange={e => set('customerAddress', e.target.value)} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Email</Label>
                <Input type="email" value={form.customerEmail} onChange={e => set('customerEmail', e.target.value)} />
              </div>
              <div>
                <Label>Teléfono</Label>
                <Input value={form.customerPhone} onChange={e => set('customerPhone', e.target.value)} />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle className="text-base">Detalles</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Válida hasta</Label>
                <Input type="date" value={form.validUntil} onChange={e => set('validUntil', e.target.value)} required />
              </div>
              <div>
                <Label>Moneda</Label>
                <Select value={form.currency} onValueChange={(v) => set('currency', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="PEN">PEN — Soles</SelectItem>
                    <SelectItem value="USD">USD — Dólares</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div>
              <Label>Notas</Label>
              <Textarea value={form.notes} onChange={e => set('notes', e.target.value)} rows={2} />
            </div>
            <div>
              <Label>Términos y condiciones</Label>
              <Textarea value={form.termsAndConditions} onChange={e => set('termsAndConditions', e.target.value)} rows={3} />
            </div>
            <Card className="bg-muted/50">
              <CardContent className="pt-4 space-y-1 text-sm">
                <div className="flex justify-between"><span>Subtotal</span><span>{form.currency} {totals.subtotal}</span></div>
                <div className="flex justify-between"><span>IGV (18%)</span><span>{form.currency} {totals.igv}</span></div>
                <div className="flex justify-between font-bold text-base border-t pt-1">
                  <span>Total</span><span>{form.currency} {totals.total}</span>
                </div>
              </CardContent>
            </Card>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="text-base">Items</CardTitle>
            <Button type="button" variant="outline" size="sm" onClick={addItem}>
              <Plus className="h-4 w-4 mr-1" /> Agregar item
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {items.map((item, idx) => (
            <div key={idx} className="grid grid-cols-12 gap-2 items-end border-b pb-3 last:border-0">
              <div className="col-span-4">
                <Label className="text-xs">Descripción</Label>
                <Input value={item.description} onChange={e => setItem(idx, 'description', e.target.value)} required />
              </div>
              <div className="col-span-1">
                <Label className="text-xs">Cant.</Label>
                <Input type="number" step="any" min="0.01" value={item.quantity} onChange={e => setItem(idx, 'quantity', e.target.value)} />
              </div>
              <div className="col-span-1">
                <Label className="text-xs">Unidad</Label>
                <Input value={item.unitMeasure} onChange={e => setItem(idx, 'unitMeasure', e.target.value)} />
              </div>
              <div className="col-span-2">
                <Label className="text-xs">Precio Unit.</Label>
                <Input type="number" step="0.01" min="0" value={item.unitPrice} onChange={e => setItem(idx, 'unitPrice', e.target.value)} required />
              </div>
              <div className="col-span-2">
                <Label className="text-xs">IGV</Label>
                <Select value={item.igvType} onValueChange={v => setItem(idx, 'igvType', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="10">Gravado</SelectItem>
                    <SelectItem value="20">Exonerado</SelectItem>
                    <SelectItem value="30">Inafecto</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="col-span-1">
                <Label className="text-xs">Desc.</Label>
                <Input type="number" step="0.01" min="0" value={item.discount} onChange={e => setItem(idx, 'discount', e.target.value)} />
              </div>
              <div className="col-span-1 flex justify-center">
                <Button type="button" variant="ghost" size="sm" onClick={() => removeItem(idx)} disabled={items.length <= 1}>
                  <Trash2 className="h-4 w-4 text-destructive" />
                </Button>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      <div className="flex justify-end gap-3">
        <Button type="button" variant="outline" onClick={() => router.push('/quotations')}>Cancelar</Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creando...' : 'Crear Cotización'}
        </Button>
      </div>
    </form>
  );
}
