'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { api, type DocumentResponse } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Separator } from '@/components/ui/separator';
import { ArrowLeft, Download, FileText, FileCode, CheckCircle, XCircle, Ban, ReceiptText } from 'lucide-react';
import { toast } from 'sonner';

const fmt = (n: number, c = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency: c }).format(n);

const statusConfig: Record<
  string,
  { label: string; icon: React.ElementType; color: string }
> = {
  accepted: { label: 'Aceptado por SUNAT', icon: CheckCircle, color: 'text-green-600' },
  rejected: { label: 'Rechazado por SUNAT', icon: XCircle, color: 'text-red-600' },
  voided: { label: 'Anulado', icon: Ban, color: 'text-gray-500' },
  draft: { label: 'Borrador', icon: FileText, color: 'text-yellow-600' },
  sent: { label: 'Enviado a SUNAT', icon: FileText, color: 'text-blue-600' },
};

export default function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { user } = useAuth();
  const [doc, setDoc] = useState<DocumentResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Void dialog state
  const [voidDialogOpen, setVoidDialogOpen] = useState(false);
  const [voidReason, setVoidReason] = useState('');
  const [isVoiding, setIsVoiding] = useState(false);

  const loadDoc = () => {
    api
      .get<DocumentResponse>(`/v1/documents/${id}`)
      .then(setDoc)
      .catch(() => router.push('/documents'))
      .finally(() => setIsLoading(false));
  };

  useEffect(() => {
    loadDoc();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleVoid = async () => {
    if (!voidReason.trim()) { toast.error('Ingresa el motivo de anulación'); return; }
    setIsVoiding(true);
    try {
      await api.post('/v1/voided-documents', { documentId: id, voidReason });
      toast.success('Documento anulado correctamente');
      setVoidDialogOpen(false);
      setVoidReason('');
      setIsLoading(true);
      loadDoc();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al anular');
    } finally {
      setIsVoiding(false);
    }
  };

  const isAdmin = user?.role === 'admin';
  const canVoid = isAdmin && doc?.status === 'accepted';
  const canCreditNote =
    doc?.status === 'accepted' &&
    (doc?.documentType === '01' || doc?.documentType === '03');

  if (isLoading) return <div className="h-96 bg-muted animate-pulse rounded-lg" />;
  if (!doc) return null;

  const status = statusConfig[doc.status] || statusConfig.draft;
  const StatusIcon = status.icon;

  const downloadFile = async (type: 'pdf' | 'xml') => {
    const token = api.getToken();
    const baseUrl = process.env.NEXT_PUBLIC_API_URL || '';
    const res = await fetch(`${baseUrl}/v1/documents/${id}/${type}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${doc.fullNumber}.${type}`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={() => router.push('/documents')}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Volver
        </Button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold">{doc.fullNumber}</h1>
          <p className="text-muted-foreground">{doc.documentTypeName}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {canCreditNote && (
            <Button
              variant="outline"
              onClick={() => router.push(`/documents/credit-note?ref=${id}`)}
            >
              <ReceiptText className="h-4 w-4 mr-2" /> Nota de Crédito
            </Button>
          )}
          {canVoid && (
            <Button
              variant="outline"
              className="border-red-200 text-red-600 hover:bg-red-50"
              onClick={() => setVoidDialogOpen(true)}
            >
              <Ban className="h-4 w-4 mr-2" /> Anular
            </Button>
          )}
          <Button variant="outline" onClick={() => downloadFile('xml')}>
            <FileCode className="h-4 w-4 mr-2" /> XML
          </Button>
          <Button onClick={() => downloadFile('pdf')}>
            <Download className="h-4 w-4 mr-2" /> PDF
          </Button>
        </div>
      </div>

      {/* Void Dialog */}
      <Dialog open={voidDialogOpen} onOpenChange={setVoidDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Anular Documento</DialogTitle>
            <DialogDescription>
              Esta acción no se puede deshacer. El documento{' '}
              <strong>{doc?.fullNumber}</strong> será anulado ante SUNAT.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3 py-2">
            <Label>Motivo de Anulación</Label>
            <Textarea
              placeholder="Describe el motivo de la anulación..."
              value={voidReason}
              onChange={(e) => setVoidReason(e.target.value)}
              rows={3}
            />
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setVoidDialogOpen(false)}
              disabled={isVoiding}
            >
              Cancelar
            </Button>
            <Button
              variant="destructive"
              onClick={handleVoid}
              disabled={isVoiding || !voidReason.trim()}
            >
              {isVoiding ? 'Anulando...' : 'Confirmar Anulación'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Status + SUNAT */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex items-center gap-3">
            <StatusIcon className={`h-6 w-6 ${status.color}`} />
            <div>
              <p className={`font-semibold ${status.color}`}>{status.label}</p>
              {doc.sunatResponseDescription && (
                <p className="text-sm text-muted-foreground">
                  {doc.sunatResponseCode}: {doc.sunatResponseDescription}
                </p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Receptor */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Receptor</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">
                {doc.customerDocType === '6' ? 'RUC' : 'DNI'}
              </span>
              <span className="font-mono">{doc.customerDocNumber}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Nombre</span>
              <span className="font-medium">{doc.customerName}</span>
            </div>
          </CardContent>
        </Card>

        {/* Detalle */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Detalle</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Fecha</span>
              <span>
                {new Date(doc.issueDate + 'T00:00:00').toLocaleDateString('es-PE')}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Moneda</span>
              <span>{doc.currency === 'PEN' ? 'Soles (PEN)' : 'Dólares (USD)'}</span>
            </div>
            {doc.hashCode && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Hash</span>
                <span className="font-mono text-xs truncate max-w-[200px]">{doc.hashCode}</span>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Items */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Items</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-10">#</TableHead>
                <TableHead>Descripción</TableHead>
                <TableHead className="text-right">Cant.</TableHead>
                <TableHead className="text-right">P.Unit</TableHead>
                <TableHead className="text-right">IGV</TableHead>
                <TableHead className="text-right">Total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {doc.items.map((item) => (
                <TableRow key={item.sequence}>
                  <TableCell>{item.sequence}</TableCell>
                  <TableCell>{item.description}</TableCell>
                  <TableCell className="text-right">{item.quantity}</TableCell>
                  <TableCell className="text-right">{fmt(item.unitPrice, doc.currency)}</TableCell>
                  <TableCell className="text-right">{fmt(item.igvAmount, doc.currency)}</TableCell>
                  <TableCell className="text-right font-medium">
                    {fmt(item.total, doc.currency)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Totals */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex justify-end">
            <div className="w-64 space-y-2 text-sm">
              {doc.operacionGravada > 0 && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Op. Gravada</span>
                  <span>{fmt(doc.operacionGravada, doc.currency)}</span>
                </div>
              )}
              {doc.operacionExonerada > 0 && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Op. Exonerada</span>
                  <span>{fmt(doc.operacionExonerada, doc.currency)}</span>
                </div>
              )}
              <div className="flex justify-between">
                <span className="text-muted-foreground">IGV 18%</span>
                <span>{fmt(doc.igv, doc.currency)}</span>
              </div>
              <Separator />
              <div className="flex justify-between text-lg font-bold">
                <span>TOTAL</span>
                <span>{fmt(doc.total, doc.currency)}</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {doc.notes && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-sm">
              <span className="font-medium">Observaciones:</span> {doc.notes}
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
