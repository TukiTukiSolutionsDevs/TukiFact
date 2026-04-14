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
  description: string;
  quantity: string;
  unitMeasure: string;
  unitPrice: string;
  igvType: string;
}

const emptyItem = (): ItemForm => ({
  description: '', quantity: '1', unitMeasure: 'NIU', unitPrice: '', igvType: '10',
});

export default function NewRecurringInvoicePage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
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
    dayOfWeek: '',
    startDate: '',
    endDate: '',
    notes: '',
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.customerDocNumber || !form.customerName || !form.startDate) {
      toast.error('Completá cliente y fecha de inicio');
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
        items: items.map(i => ({
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

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex items-center gap-4">
        <Button type="button" variant="ghost" size="sm" onClick={() => router.push('/recurring-invoices')}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Recurrentes
        </Button>
        <h1 className="text-2xl font-bold tracking-tight">Nueva Facturación Recurrente</h1>
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
            <div>
              <Label>Email</Label>
              <Input type="email" value={form.customerEmail} onChange={e => set('customerEmail', e.target.value)} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle className="text-base">Programación</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Tipo Comprobante</Label>
                <Select value={form.documentType} onValueChange={(v) => set('documentType', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="01">Factura</SelectItem>
                    <SelectItem value="03">Boleta</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Serie</Label>
                <Input value={form.serie} onChange={e => set('serie', e.target.value)} maxLength={4} required />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Frecuencia</Label>
                <Select value={form.frequency} onValueChange={(v) => set('frequency', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="daily">Diaria</SelectItem>
                    <SelectItem value="weekly">Semanal</SelectItem>
                    <SelectItem value="biweekly">Quincenal</SelectItem>
                    <SelectItem value="monthly">Mensual</SelectItem>
                    <SelectItem value="yearly">Anual</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              {form.frequency === 'monthly' && (
                <div>
                  <Label>Día del mes</Label>
                  <Input type="number" min="1" max="28" value={form.dayOfMonth} onChange={e => set('dayOfMonth', e.target.value)} />
                </div>
              )}
              {form.frequency === 'weekly' && (
                <div>
                  <Label>Día de la semana</Label>
                  <Select value={form.dayOfWeek} onValueChange={(v) => set('dayOfWeek', v)}>
                    <SelectTrigger><SelectValue placeholder="Seleccionar" /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="1">Lunes</SelectItem>
                      <SelectItem value="2">Martes</SelectItem>
                      <SelectItem value="3">Miércoles</SelectItem>
                      <SelectItem value="4">Jueves</SelectItem>
                      <SelectItem value="5">Viernes</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              )}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Fecha inicio</Label>
                <Input type="date" value={form.startDate} onChange={e => set('startDate', e.target.value)} required />
              </div>
              <div>
                <Label>Fecha fin (opcional)</Label>
                <Input type="date" value={form.endDate} onChange={e => set('endDate', e.target.value)} />
              </div>
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
            <div>
              <Label>Notas</Label>
              <Textarea value={form.notes} onChange={e => set('notes', e.target.value)} rows={2} />
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="text-base">Items (plantilla)</CardTitle>
            <Button type="button" variant="outline" size="sm" onClick={addItem}>
              <Plus className="h-4 w-4 mr-1" /> Agregar
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {items.map((item, idx) => (
            <div key={idx} className="grid grid-cols-12 gap-2 items-end border-b pb-3 last:border-0">
              <div className="col-span-5">
                <Label className="text-xs">Descripción</Label>
                <Input value={item.description} onChange={e => setItem(idx, 'description', e.target.value)} required />
              </div>
              <div className="col-span-2">
                <Label className="text-xs">Cantidad</Label>
                <Input type="number" step="any" min="0.01" value={item.quantity} onChange={e => setItem(idx, 'quantity', e.target.value)} />
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
        <Button type="button" variant="outline" onClick={() => router.push('/recurring-invoices')}>Cancelar</Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creando...' : 'Crear Recurrente'}
        </Button>
      </div>
    </form>
  );
}
