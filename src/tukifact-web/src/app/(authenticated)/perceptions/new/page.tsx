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
  documentType: '01', documentNumber: '', documentDate: '',
  invoiceAmount: '', invoiceCurrency: 'PEN', collectionDate: '',
  collectionNumber: '1', collectionAmount: '', exchangeRate: '',
});

export default function NewPerceptionPage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState({
    serie: 'P001',
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

  const set = (key: string, value: string | null) => { if (value !== null) setForm(f => ({ ...f, [key]: value })); };
  const setRef = (idx: number, key: string, value: string | null) => {
    if (value === null) return;
    setRefs(prev => prev.map((r, i) => i === idx ? { ...r, [key]: value } : r));
  };
  const addRef = () => setRefs(prev => [...prev, emptyRef()]);
  const removeRef = (idx: number) => {
    if (refs.length <= 1) return;
    setRefs(prev => prev.filter((_, i) => i !== idx));
  };

  const regimePercent: Record<string, string> = { '01': '2', '02': '1', '03': '0.5' };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.customerDocNumber || !form.customerName) {
      toast.error('Completá los datos del cliente');
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
        references: refs.map(r => ({
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
      toast.success('Percepción emitida exitosamente');
      router.push('/perceptions');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al crear');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex items-center gap-4">
        <Button type="button" variant="ghost" size="sm" onClick={() => router.push('/perceptions')}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Percepciones
        </Button>
        <h1 className="text-2xl font-bold tracking-tight">Nueva Percepción</h1>
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
                <Label>RUC / DNI</Label>
                <Input value={form.customerDocNumber} onChange={e => set('customerDocNumber', e.target.value)} required />
              </div>
            </div>
            <div>
              <Label>Razón Social</Label>
              <Input value={form.customerName} onChange={e => set('customerName', e.target.value)} required />
            </div>
            <div>
              <Label>Dirección</Label>
              <Input value={form.customerAddress} onChange={e => set('customerAddress', e.target.value)} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle className="text-base">Datos de Percepción</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Serie</Label>
                <Input value={form.serie} onChange={e => set('serie', e.target.value)} maxLength={4} required />
              </div>
              <div>
                <Label>Moneda</Label>
                <Select value={form.currency} onValueChange={(v) => set('currency', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="PEN">PEN</SelectItem>
                    <SelectItem value="USD">USD</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Régimen (Catálogo 22)</Label>
                <Select value={form.regimeCode} onValueChange={(v) => {
                  if (!v) return;
                  set('regimeCode', v);
                  set('perceptionPercent', regimePercent[v] || '2');
                }}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="01">01 — Venta interna 2%</SelectItem>
                    <SelectItem value="02">02 — Combustible 1%</SelectItem>
                    <SelectItem value="03">03 — CdP 0.5%</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>% Percepción</Label>
                <Input type="number" step="0.01" value={form.perceptionPercent} onChange={e => set('perceptionPercent', e.target.value)} />
              </div>
            </div>
            <div>
              <Label>Observaciones</Label>
              <Textarea value={form.notes} onChange={e => set('notes', e.target.value)} rows={2} />
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="text-base">Documentos Relacionados</CardTitle>
            <Button type="button" variant="outline" size="sm" onClick={addRef}>
              <Plus className="h-4 w-4 mr-1" /> Agregar documento
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {refs.map((ref, idx) => (
            <div key={idx} className="border rounded-lg p-4 space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Documento {idx + 1}</span>
                <Button type="button" variant="ghost" size="sm" onClick={() => removeRef(idx)} disabled={refs.length <= 1}>
                  <Trash2 className="h-4 w-4 text-destructive" />
                </Button>
              </div>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <div>
                  <Label className="text-xs">Tipo</Label>
                  <Select value={ref.documentType} onValueChange={v => setRef(idx, 'documentType', v)}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="01">Factura</SelectItem>
                      <SelectItem value="03">Boleta</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <Label className="text-xs">Número</Label>
                  <Input placeholder="F001-00000001" value={ref.documentNumber} onChange={e => setRef(idx, 'documentNumber', e.target.value)} required />
                </div>
                <div>
                  <Label className="text-xs">Fecha documento</Label>
                  <Input type="date" value={ref.documentDate} onChange={e => setRef(idx, 'documentDate', e.target.value)} required />
                </div>
                <div>
                  <Label className="text-xs">Monto factura</Label>
                  <Input type="number" step="0.01" value={ref.invoiceAmount} onChange={e => setRef(idx, 'invoiceAmount', e.target.value)} required />
                </div>
                <div>
                  <Label className="text-xs">Fecha cobro</Label>
                  <Input type="date" value={ref.collectionDate} onChange={e => setRef(idx, 'collectionDate', e.target.value)} required />
                </div>
                <div>
                  <Label className="text-xs">Nro. cobro</Label>
                  <Input type="number" min="1" value={ref.collectionNumber} onChange={e => setRef(idx, 'collectionNumber', e.target.value)} />
                </div>
                <div>
                  <Label className="text-xs">Monto cobrado</Label>
                  <Input type="number" step="0.01" value={ref.collectionAmount} onChange={e => setRef(idx, 'collectionAmount', e.target.value)} required />
                </div>
                <div>
                  <Label className="text-xs">T.C. (opcional)</Label>
                  <Input type="number" step="0.0001" value={ref.exchangeRate} onChange={e => setRef(idx, 'exchangeRate', e.target.value)} placeholder="3.5270" />
                </div>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      <div className="flex justify-end gap-3">
        <Button type="button" variant="outline" onClick={() => router.push('/perceptions')}>Cancelar</Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Emitiendo...' : 'Emitir Percepción'}
        </Button>
      </div>
    </form>
  );
}
