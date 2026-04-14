'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { api, type QuotationResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ArrowLeft, FileText, Send, XCircle, CheckCircle } from 'lucide-react';
import { toast } from 'sonner';

const formatCurrency = (amount: number, currency = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency }).format(amount);

const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit', month: '2-digit', year: 'numeric',
  });

const statusMap: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
  draft: { label: 'Borrador', variant: 'outline' },
  sent: { label: 'Enviada', variant: 'secondary' },
  approved: { label: 'Aprobada', variant: 'default' },
  invoiced: { label: 'Facturada', variant: 'default' },
  cancelled: { label: 'Cancelada', variant: 'destructive' },
  expired: { label: 'Vencida', variant: 'secondary' },
};

export default function QuotationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [quotation, setQuotation] = useState<QuotationResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [convertSerie, setConvertSerie] = useState('F001');
  const [isConverting, setIsConverting] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);

  useEffect(() => {
    api.get<QuotationResponse>(`/v1/quotations/${id}`)
      .then(setQuotation)
      .catch(console.error)
      .finally(() => setIsLoading(false));
  }, [id]);

  const updateStatus = async (status: string) => {
    try {
      const res = await api.put<QuotationResponse>(`/v1/quotations/${id}/status`, { status });
      setQuotation(res);
      toast.success(`Estado actualizado a "${statusMap[status]?.label || status}"`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al actualizar');
    }
  };

  const convertToInvoice = async () => {
    setIsConverting(true);
    try {
      const res = await api.post<{ quotation: QuotationResponse; invoice: { id: string; fullNumber: string } }>(
        `/v1/quotations/${id}/convert-to-invoice`,
        { serie: convertSerie, documentType: '01' }
      );
      setQuotation(res.quotation);
      setDialogOpen(false);
      toast.success(`Factura ${res.invoice.fullNumber} creada exitosamente`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al convertir');
    } finally {
      setIsConverting(false);
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

  if (!quotation) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">Cotización no encontrada</p>
        <Button variant="outline" className="mt-4" onClick={() => router.push('/quotations')}>
          Volver
        </Button>
      </div>
    );
  }

  const s = statusMap[quotation.status] || { label: quotation.status, variant: 'outline' as const };
  const canConvert = ['draft', 'sent', 'approved'].includes(quotation.status);

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={() => router.push('/quotations')}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Cotizaciones
        </Button>
      </div>

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{quotation.quotationNumber}</h1>
          <p className="text-muted-foreground">
            Emitida el {formatDate(quotation.issueDate)} — Válida hasta {formatDate(quotation.validUntil)}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={s.variant} className="text-sm">{s.label}</Badge>
          {quotation.status === 'draft' && (
            <Button size="sm" variant="outline" onClick={() => updateStatus('sent')}>
              <Send className="h-4 w-4 mr-1" /> Enviar
            </Button>
          )}
          {quotation.status === 'sent' && (
            <Button size="sm" variant="outline" onClick={() => updateStatus('approved')}>
              <CheckCircle className="h-4 w-4 mr-1" /> Aprobar
            </Button>
          )}
          {canConvert && (
            <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
              <DialogTrigger render={<Button size="sm" />}>
                <FileText className="h-4 w-4 mr-1" /> Convertir a Factura
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Convertir a Factura</DialogTitle>
                </DialogHeader>
                <div className="space-y-4 py-4">
                  <p className="text-sm text-muted-foreground">
                    Se creará una factura con los mismos items de la cotización {quotation.quotationNumber}.
                  </p>
                  <div className="space-y-2">
                    <Label htmlFor="serie">Serie</Label>
                    <Input
                      id="serie"
                      value={convertSerie}
                      onChange={(e) => setConvertSerie(e.target.value)}
                      placeholder="F001"
                      maxLength={4}
                    />
                  </div>
                </div>
                <DialogFooter>
                  <Button variant="outline" onClick={() => setDialogOpen(false)}>Cancelar</Button>
                  <Button onClick={convertToInvoice} disabled={isConverting}>
                    {isConverting ? 'Convirtiendo...' : 'Convertir'}
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          )}
          {quotation.status !== 'cancelled' && quotation.status !== 'invoiced' && (
            <Button size="sm" variant="destructive" onClick={() => updateStatus('cancelled')}>
              <XCircle className="h-4 w-4 mr-1" /> Cancelar
            </Button>
          )}
        </div>
      </div>

      {quotation.invoiceDocumentNumber && (
        <Card className="border-green-200 bg-green-50 dark:border-green-900 dark:bg-green-950">
          <CardContent className="pt-4">
            <p className="text-sm font-medium text-green-800 dark:text-green-200">
              ✅ Convertida a factura: <span className="font-mono">{quotation.invoiceDocumentNumber}</span>
            </p>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardHeader><CardTitle className="text-base">Cliente</CardTitle></CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div><span className="text-muted-foreground">Nombre:</span> {quotation.customerName}</div>
            <div><span className="text-muted-foreground">Documento:</span> {quotation.customerDocNumber}</div>
            {quotation.customerEmail && (
              <div><span className="text-muted-foreground">Email:</span> {quotation.customerEmail}</div>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle className="text-base">Totales</CardTitle></CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Subtotal</span>
              <span>{formatCurrency(quotation.subtotal, quotation.currency)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">IGV (18%)</span>
              <span>{formatCurrency(quotation.igv, quotation.currency)}</span>
            </div>
            <div className="flex justify-between font-bold text-base border-t pt-2">
              <span>Total</span>
              <span>{formatCurrency(quotation.total, quotation.currency)}</span>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader><CardTitle className="text-base">Items</CardTitle></CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>#</TableHead>
                <TableHead>Descripción</TableHead>
                <TableHead className="text-right">Cant.</TableHead>
                <TableHead className="text-right">P. Unit.</TableHead>
                <TableHead className="text-right">IGV</TableHead>
                <TableHead className="text-right">Total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {quotation.items.map((item) => (
                <TableRow key={item.sequence}>
                  <TableCell>{item.sequence}</TableCell>
                  <TableCell>{item.description}</TableCell>
                  <TableCell className="text-right">{item.quantity}</TableCell>
                  <TableCell className="text-right">
                    {formatCurrency(item.unitPrice, quotation.currency)}
                  </TableCell>
                  <TableCell className="text-right">
                    {formatCurrency(item.igvAmount, quotation.currency)}
                  </TableCell>
                  <TableCell className="text-right font-medium">
                    {formatCurrency(item.total, quotation.currency)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {quotation.notes && (
        <Card>
          <CardHeader><CardTitle className="text-base">Notas</CardTitle></CardHeader>
          <CardContent><p className="text-sm text-muted-foreground">{quotation.notes}</p></CardContent>
        </Card>
      )}
    </div>
  );
}
