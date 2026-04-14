'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { api, type DespatchAdviceResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { ArrowLeft, Send, Download } from 'lucide-react';
import { toast } from 'sonner';

const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit', month: '2-digit', year: 'numeric',
  });

const statusMap: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
  accepted: { label: 'Aceptado', variant: 'default' },
  rejected: { label: 'Rechazado', variant: 'destructive' },
  draft: { label: 'Borrador', variant: 'outline' },
  signed: { label: 'Firmado', variant: 'outline' },
  sent: { label: 'Enviado', variant: 'secondary' },
  pending_ticket: { label: 'Pendiente ticket', variant: 'secondary' },
};

const transportLabel: Record<string, string> = {
  '01': 'Transporte público',
  '02': 'Transporte privado',
};

export default function DespatchAdviceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [gre, setGre] = useState<DespatchAdviceResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isEmitting, setIsEmitting] = useState(false);

  useEffect(() => {
    api.get<DespatchAdviceResponse>(`/v1/despatch-advices/${id}`)
      .then(setGre)
      .catch(console.error)
      .finally(() => setIsLoading(false));
  }, [id]);

  const emitToSunat = async () => {
    setIsEmitting(true);
    try {
      const res = await api.post<DespatchAdviceResponse>(`/v1/despatch-advices/${id}/emit`, {});
      setGre(res);
      toast.success(`GRE emitida — Estado: ${statusMap[res.status]?.label || res.status}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al emitir');
    } finally {
      setIsEmitting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 bg-muted animate-pulse rounded w-48" />
        <div className="h-64 bg-muted animate-pulse rounded" />
      </div>
    );
  }

  if (!gre) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">Guía no encontrada</p>
        <Button variant="outline" className="mt-4" onClick={() => router.push('/despatch-advices')}>
          Volver
        </Button>
      </div>
    );
  }

  const s = statusMap[gre.status] || { label: gre.status, variant: 'outline' as const };
  const canEmit = gre.status === 'draft';

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={() => router.push('/despatch-advices')}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Guías
        </Button>
      </div>

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{gre.fullNumber}</h1>
          <p className="text-muted-foreground">
            Fecha: {formatDate(gre.issueDate)} — Traslado: {formatDate(gre.transferStartDate)}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={s.variant} className="text-sm">{s.label}</Badge>
          {canEmit && (
            <Button onClick={emitToSunat} disabled={isEmitting}>
              <Send className="h-4 w-4 mr-1" />
              {isEmitting ? 'Emitiendo...' : 'Emitir a SUNAT'}
            </Button>
          )}
          {gre.pdfUrl && (
            <Button variant="outline" size="sm" render={<a href={gre.pdfUrl} target="_blank" rel="noreferrer" />}>
              <Download className="h-4 w-4 mr-1" /> PDF
            </Button>
          )}
        </div>
      </div>

      {gre.sunatResponseCode && (
        <Card className={gre.status === 'accepted'
          ? 'border-green-200 bg-green-50 dark:border-green-900 dark:bg-green-950'
          : 'border-red-200 bg-red-50 dark:border-red-900 dark:bg-red-950'}>
          <CardContent className="pt-4">
            <p className="text-sm font-medium">
              SUNAT: {gre.sunatResponseCode} — {gre.sunatResponseMessage}
            </p>
            {gre.sunatTicket && (
              <p className="text-xs text-muted-foreground mt-1">Ticket: {gre.sunatTicket}</p>
            )}
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <Card>
          <CardHeader><CardTitle className="text-base">Destinatario</CardTitle></CardHeader>
          <CardContent className="space-y-1 text-sm">
            <div><span className="text-muted-foreground">Doc:</span> {gre.recipientDocNumber}</div>
            <div><span className="text-muted-foreground">Nombre:</span> {gre.recipientName}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle className="text-base">Traslado</CardTitle></CardHeader>
          <CardContent className="space-y-1 text-sm">
            <div><span className="text-muted-foreground">Motivo:</span> {gre.transferReasonDescription}</div>
            <div><span className="text-muted-foreground">Transporte:</span> {transportLabel[gre.transportMode] || gre.transportMode}</div>
            <div><span className="text-muted-foreground">Peso:</span> {gre.grossWeight} {gre.weightUnitCode}</div>
            <div><span className="text-muted-foreground">Bultos:</span> {gre.totalPackages}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle className="text-base">Ruta</CardTitle></CardHeader>
          <CardContent className="space-y-1 text-sm">
            <div>
              <span className="text-muted-foreground">Origen:</span>
              <p className="text-xs">{gre.originAddress} ({gre.originUbigeo})</p>
            </div>
            <div className="text-center text-muted-foreground">↓</div>
            <div>
              <span className="text-muted-foreground">Destino:</span>
              <p className="text-xs">{gre.destinationAddress} ({gre.destinationUbigeo})</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {(gre.driverName || gre.carrierName) && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">
              {gre.transportMode === '02' ? 'Conductor' : 'Transportista'}
            </CardTitle>
          </CardHeader>
          <CardContent className="text-sm space-y-1">
            {gre.driverName && (
              <>
                <div><span className="text-muted-foreground">Nombre:</span> {gre.driverName}</div>
                <div><span className="text-muted-foreground">Doc:</span> {gre.driverDocNumber}</div>
                {gre.driverLicense && <div><span className="text-muted-foreground">Licencia:</span> {gre.driverLicense}</div>}
                {gre.vehiclePlate && <div><span className="text-muted-foreground">Placa:</span> {gre.vehiclePlate}</div>}
              </>
            )}
            {gre.carrierName && (
              <>
                <div><span className="text-muted-foreground">Razón Social:</span> {gre.carrierName}</div>
                <div><span className="text-muted-foreground">RUC:</span> {gre.carrierDocNumber}</div>
              </>
            )}
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader><CardTitle className="text-base">Items</CardTitle></CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>#</TableHead>
                <TableHead>Código</TableHead>
                <TableHead>Descripción</TableHead>
                <TableHead className="text-right">Cantidad</TableHead>
                <TableHead>Unidad</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {gre.items.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.lineNumber}</TableCell>
                  <TableCell className="font-mono">{item.productCode || '—'}</TableCell>
                  <TableCell>{item.description}</TableCell>
                  <TableCell className="text-right">{item.quantity}</TableCell>
                  <TableCell>{item.unitCode}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
