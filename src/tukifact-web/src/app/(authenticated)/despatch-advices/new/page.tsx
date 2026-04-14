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
  unitCode: string;
}

const emptyItem = (): ItemForm => ({
  productCode: '', description: '', quantity: '1', unitCode: 'NIU',
});

export default function NewDespatchAdvicePage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState({
    documentType: '09',
    serie: 'T001',
    recipientDocType: '6',
    recipientDocNumber: '',
    recipientName: '',
    transferReasonCode: '01',
    transferReasonDescription: 'Venta',
    transferStartDate: '',
    transportMode: '02',
    originAddress: '',
    originUbigeo: '',
    destinationAddress: '',
    destinationUbigeo: '',
    grossWeight: '',
    totalPackages: '1',
    vehiclePlate: '',
    driverDocType: '1',
    driverDocNumber: '',
    driverName: '',
    driverLicense: '',
    carrierDocType: '',
    carrierDocNumber: '',
    carrierName: '',
    note: '',
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

  const transferReasons: Record<string, string> = {
    '01': 'Venta', '02': 'Compra', '04': 'Traslado entre establecimientos',
    '08': 'Importación', '09': 'Exportación', '13': 'Otros',
    '14': 'Venta sujeta a confirmación', '18': 'Traslado emisor itinerante',
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.recipientDocNumber || !form.recipientName) {
      toast.error('Completá los datos del destinatario');
      return;
    }

    setIsSubmitting(true);
    try {
      const body = {
        documentType: form.documentType,
        serie: form.serie,
        recipientDocType: form.recipientDocType,
        recipientDocNumber: form.recipientDocNumber,
        recipientName: form.recipientName,
        transferReasonCode: form.transferReasonCode,
        transferReasonDescription: form.transferReasonDescription,
        transferStartDate: form.transferStartDate,
        transportMode: form.transportMode,
        originAddress: form.originAddress,
        originUbigeo: form.originUbigeo,
        destinationAddress: form.destinationAddress,
        destinationUbigeo: form.destinationUbigeo,
        grossWeight: parseFloat(form.grossWeight) || 0,
        totalPackages: parseInt(form.totalPackages) || 1,
        vehiclePlate: form.vehiclePlate || null,
        driverDocType: form.transportMode === '02' ? form.driverDocType : null,
        driverDocNumber: form.transportMode === '02' ? form.driverDocNumber : null,
        driverName: form.transportMode === '02' ? form.driverName : null,
        driverLicense: form.transportMode === '02' ? (form.driverLicense || null) : null,
        carrierDocType: form.transportMode === '01' ? form.carrierDocType : null,
        carrierDocNumber: form.transportMode === '01' ? form.carrierDocNumber : null,
        carrierName: form.transportMode === '01' ? form.carrierName : null,
        note: form.note || null,
        items: items.map((i, idx) => ({
          lineNumber: idx + 1,
          productCode: i.productCode || null,
          description: i.description,
          quantity: parseFloat(i.quantity),
          unitCode: i.unitCode,
        })),
      };
      const res = await api.post<{ id: string }>('/v1/despatch-advices', body);
      toast.success('Guía de remisión creada como borrador');
      router.push(`/despatch-advices/${res.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al crear');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="flex items-center gap-4">
        <Button type="button" variant="ghost" size="sm" onClick={() => router.push('/despatch-advices')}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Guías
        </Button>
        <h1 className="text-2xl font-bold tracking-tight">Nueva Guía de Remisión</h1>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardHeader><CardTitle className="text-base">Destinatario</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Serie</Label>
                <Input value={form.serie} onChange={e => set('serie', e.target.value)} maxLength={4} required />
              </div>
              <div>
                <Label>Tipo Doc.</Label>
                <Select value={form.recipientDocType} onValueChange={(v) => set('recipientDocType', v)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="6">RUC</SelectItem>
                    <SelectItem value="1">DNI</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div>
              <Label>Nro. Documento</Label>
              <Input value={form.recipientDocNumber} onChange={e => set('recipientDocNumber', e.target.value)} required />
            </div>
            <div>
              <Label>Razón Social / Nombre</Label>
              <Input value={form.recipientName} onChange={e => set('recipientName', e.target.value)} required />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle className="text-base">Traslado</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Motivo</Label>
                <Select value={form.transferReasonCode} onValueChange={(v) => {
                  if (!v) return;
                  set('transferReasonCode', v);
                  set('transferReasonDescription', transferReasons[v] || '');
                }}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {Object.entries(transferReasons).map(([code, label]) => (
                      <SelectItem key={code} value={code}>{code} — {label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Fecha traslado</Label>
                <Input type="date" value={form.transferStartDate} onChange={e => set('transferStartDate', e.target.value)} required />
              </div>
            </div>
            <div>
              <Label>Modalidad transporte</Label>
              <Select value={form.transportMode} onValueChange={(v) => set('transportMode', v)}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="01">Transporte público</SelectItem>
                  <SelectItem value="02">Transporte privado</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Peso bruto (KG)</Label>
                <Input type="number" step="0.01" value={form.grossWeight} onChange={e => set('grossWeight', e.target.value)} required />
              </div>
              <div>
                <Label>Nro. bultos</Label>
                <Input type="number" min="1" value={form.totalPackages} onChange={e => set('totalPackages', e.target.value)} />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardHeader><CardTitle className="text-base">Origen</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div>
              <Label>Dirección</Label>
              <Input value={form.originAddress} onChange={e => set('originAddress', e.target.value)} required />
            </div>
            <div>
              <Label>Ubigeo</Label>
              <Input value={form.originUbigeo} onChange={e => set('originUbigeo', e.target.value)} maxLength={6} placeholder="150101" required />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle className="text-base">Destino</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div>
              <Label>Dirección</Label>
              <Input value={form.destinationAddress} onChange={e => set('destinationAddress', e.target.value)} required />
            </div>
            <div>
              <Label>Ubigeo</Label>
              <Input value={form.destinationUbigeo} onChange={e => set('destinationUbigeo', e.target.value)} maxLength={6} placeholder="150101" required />
            </div>
          </CardContent>
        </Card>
      </div>

      {form.transportMode === '02' ? (
        <Card>
          <CardHeader><CardTitle className="text-base">Conductor y Vehículo</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              <div>
                <Label>DNI Conductor</Label>
                <Input value={form.driverDocNumber} onChange={e => set('driverDocNumber', e.target.value)} />
              </div>
              <div>
                <Label>Nombre</Label>
                <Input value={form.driverName} onChange={e => set('driverName', e.target.value)} />
              </div>
              <div>
                <Label>Licencia</Label>
                <Input value={form.driverLicense} onChange={e => set('driverLicense', e.target.value)} />
              </div>
              <div>
                <Label>Placa</Label>
                <Input value={form.vehiclePlate} onChange={e => set('vehiclePlate', e.target.value)} placeholder="ABC-123" />
              </div>
            </div>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader><CardTitle className="text-base">Transportista</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-3 gap-3">
              <div>
                <Label>RUC Transportista</Label>
                <Input value={form.carrierDocNumber} onChange={e => set('carrierDocNumber', e.target.value)} />
              </div>
              <div className="col-span-2">
                <Label>Razón Social</Label>
                <Input value={form.carrierName} onChange={e => set('carrierName', e.target.value)} />
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="text-base">Items a trasladar</CardTitle>
            <Button type="button" variant="outline" size="sm" onClick={addItem}>
              <Plus className="h-4 w-4 mr-1" /> Agregar
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {items.map((item, idx) => (
            <div key={idx} className="grid grid-cols-12 gap-2 items-end border-b pb-3 last:border-0">
              <div className="col-span-2">
                <Label className="text-xs">Código</Label>
                <Input value={item.productCode} onChange={e => setItem(idx, 'productCode', e.target.value)} />
              </div>
              <div className="col-span-5">
                <Label className="text-xs">Descripción</Label>
                <Input value={item.description} onChange={e => setItem(idx, 'description', e.target.value)} required />
              </div>
              <div className="col-span-2">
                <Label className="text-xs">Cantidad</Label>
                <Input type="number" step="any" min="0.01" value={item.quantity} onChange={e => setItem(idx, 'quantity', e.target.value)} />
              </div>
              <div className="col-span-2">
                <Label className="text-xs">Unidad</Label>
                <Input value={item.unitCode} onChange={e => setItem(idx, 'unitCode', e.target.value)} />
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

      <div>
        <Label>Observaciones</Label>
        <Textarea value={form.note} onChange={e => set('note', e.target.value)} rows={2} />
      </div>

      <div className="flex justify-end gap-3">
        <Button type="button" variant="outline" onClick={() => router.push('/despatch-advices')}>Cancelar</Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creando...' : 'Crear como Borrador'}
        </Button>
      </div>
    </form>
  );
}
