'use client';

import { useEffect, useState, useMemo } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { api, type DocumentResponse } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
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
import { Section } from '@/components/ui/section';
import { StatusBadge } from '@/components/ui/status-badge';
import { Timeline, type TimelineItem } from '@/components/ui/timeline';
import {
  ChevronLeft,
  Download,
  FileCode,
  Ban,
  ReceiptText,
  Mail,
  Loader2,
  CheckCircle2,
  XCircle,
  FilePlus,
  Send,
  Clock,
  ShieldCheck,
} from 'lucide-react';
import { toast } from 'sonner';

const fmt = (n: number, c = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency: c }).format(n);

const formatDate = (iso: string) =>
  new Date(iso).toLocaleString('es-PE', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

const formatDateOnly = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });

function buildTimeline(doc: DocumentResponse): TimelineItem[] {
  const items: TimelineItem[] = [];
  items.push({
    title: 'Comprobante creado',
    body: `${doc.documentTypeName} ${doc.fullNumber}`,
    time: formatDateOnly(doc.issueDate),
    icon: FilePlus,
    color: 'var(--info)',
  });

  if (doc.hashCode) {
    items.push({
      title: 'XML firmado',
      body: `Hash: ${doc.hashCode.slice(0, 24)}…`,
      icon: ShieldCheck,
      color: 'var(--info)',
    });
  }

  if (doc.status === 'sent' || doc.status === 'accepted' || doc.status === 'rejected' || doc.status === 'voided') {
    items.push({
      title: 'Enviado a SUNAT',
      body: 'Aguardando respuesta',
      icon: Send,
      color: 'var(--warning)',
    });
  }

  if (doc.status === 'accepted') {
    items.push({
      title: 'Aceptado por SUNAT',
      body: doc.sunatResponseDescription || `Código ${doc.sunatResponseCode ?? '0'}`,
      icon: CheckCircle2,
      color: 'var(--success)',
    });
  }
  if (doc.status === 'rejected') {
    items.push({
      title: 'Rechazado por SUNAT',
      body: doc.sunatResponseDescription || `Código ${doc.sunatResponseCode ?? '—'}`,
      icon: XCircle,
      color: 'var(--danger)',
    });
  }
  if (doc.status === 'voided') {
    items.push({
      title: 'Anulado',
      body: 'Comunicación de baja enviada a SUNAT',
      icon: Ban,
      color: 'var(--slate-500)',
    });
  }
  if (doc.status === 'draft') {
    items.push({
      title: 'Pendiente de envío',
      body: 'Aún en estado borrador',
      icon: Clock,
      color: 'var(--slate-500)',
    });
  }

  return items;
}

function DL({
  label,
  value,
  span,
}: {
  label: string;
  value: React.ReactNode;
  span?: boolean;
}) {
  return (
    <div className={span ? 'md:col-span-2' : undefined}>
      <div className="t-caption mb-0.5" style={{ color: 'var(--muted-foreground)' }}>
        {label}
      </div>
      <div className="t-body-sm font-medium">{value}</div>
    </div>
  );
}

export default function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { user } = useAuth();
  const [doc, setDoc] = useState<DocumentResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [voidDialogOpen, setVoidDialogOpen] = useState(false);
  const [voidReason, setVoidReason] = useState('');
  const [isVoiding, setIsVoiding] = useState(false);
  const [downloading, setDownloading] = useState<'pdf' | 'xml' | 'cdr' | null>(null);

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

  const timeline = useMemo(() => (doc ? buildTimeline(doc) : []), [doc]);

  const handleVoid = async () => {
    if (!voidReason.trim()) {
      toast.error('Ingresa el motivo de anulación');
      return;
    }
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

  const downloadFile = async (type: 'pdf' | 'xml' | 'cdr') => {
    if (!doc) return;
    setDownloading(type);
    try {
      const token = api.getToken();
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || '';
      const res = await fetch(`${baseUrl}/v1/documents/${id}/${type}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error(`No se pudo descargar el ${type.toUpperCase()}`);
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${doc.fullNumber}.${type === 'cdr' ? 'zip' : type}`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
      toast.success(`${type.toUpperCase()} descargado`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : `Error al descargar ${type.toUpperCase()}`);
    } finally {
      setDownloading(null);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span className="t-body-sm">Cargando comprobante…</span>
      </div>
    );
  }
  if (!doc) return null;

  const isAdmin = user?.role === 'admin';
  const canVoid = isAdmin && doc.status === 'accepted';
  const canCreditNote = doc.status === 'accepted' && (doc.documentType === '01' || doc.documentType === '03');
  const hasCdr = doc.status === 'accepted';

  return (
    <div>
      {/* Breadcrumb + header */}
      <div className="flex items-center gap-2 mb-2">
        <Button variant="ghost" size="sm" asChild>
          <Link href="/documents">
            <ChevronLeft className="h-4 w-4 mr-1" /> Volver
          </Link>
        </Button>
        <span className="t-body-sm" style={{ color: 'var(--muted-foreground)' }}>
          Documentos /{' '}
          <strong className="mono" style={{ color: 'var(--foreground)' }}>
            {doc.fullNumber}
          </strong>
        </span>
      </div>

      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0 inline-flex items-center gap-3 flex-wrap">
            <span className="mono">{doc.fullNumber}</span>
            <StatusBadge status={doc.status} />
          </h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            {doc.documentTypeName} · Emitida el {formatDateOnly(doc.issueDate)}
          </p>
        </div>
      </div>

      <Dialog open={voidDialogOpen} onOpenChange={setVoidDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Anular comprobante</DialogTitle>
            <DialogDescription>
              Esta acción no se puede deshacer. Se generará una comunicación de baja a SUNAT para{' '}
              <strong className="mono">{doc.fullNumber}</strong>.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3 py-2">
            <Label>Motivo de anulación</Label>
            <Textarea
              placeholder="Describe el motivo de la anulación..."
              value={voidReason}
              onChange={(e) => setVoidReason(e.target.value)}
              rows={3}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setVoidDialogOpen(false)} disabled={isVoiding}>
              Cancelar
            </Button>
            <Button variant="destructive" onClick={handleVoid} disabled={isVoiding || !voidReason.trim()}>
              {isVoiding ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Anulando…
                </>
              ) : (
                <>
                  <Ban className="h-4 w-4 mr-2" /> Confirmar anulación
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)] items-start">
        {/* Left column */}
        <div className="lg:col-span-2 flex flex-col gap-[var(--gap-cards)] min-w-0">
          <Section title="Datos del cliente">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-3.5">
              <DL label="Razón social / Nombre" value={doc.customerName} span />
              <DL
                label={doc.customerDocType === '6' ? 'RUC' : doc.customerDocType === '1' ? 'DNI' : 'Doc.'}
                value={<span className="mono tnum">{doc.customerDocNumber}</span>}
              />
              <DL label="Moneda" value={`${doc.currency} · ${doc.currency === 'USD' ? '$' : 'S/'}`} />
              {doc.hashCode && (
                <DL
                  label="Hash"
                  value={
                    <span
                      className="mono t-caption truncate inline-block max-w-full"
                      style={{ color: 'var(--muted-foreground)' }}
                      title={doc.hashCode}
                    >
                      {doc.hashCode.slice(0, 32)}…
                    </span>
                  }
                  span
                />
              )}
            </div>
          </Section>

          <Section title="Items" desc={`${doc.items.length} ${doc.items.length === 1 ? 'línea' : 'líneas'}`}>
            <div className="-mx-6 overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr
                    className="t-overline"
                    style={{ color: 'var(--muted-foreground)', background: 'var(--muted)' }}
                  >
                    <th className="text-left py-2.5 pl-6 pr-2 w-10">#</th>
                    <th className="text-left py-2.5 px-2">Descripción</th>
                    <th className="text-right py-2.5 px-2 w-20">Cant.</th>
                    <th className="text-right py-2.5 px-2 w-28">P. unit.</th>
                    <th className="text-right py-2.5 px-2 w-28">IGV</th>
                    <th className="text-right py-2.5 pr-6 pl-2 w-32">Importe</th>
                  </tr>
                </thead>
                <tbody>
                  {doc.items.map((item) => (
                    <tr key={item.sequence} style={{ borderTop: '1px solid var(--border)' }}>
                      <td
                        className="py-3 pl-6 pr-2 t-body-sm mono tnum"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        {item.sequence}
                      </td>
                      <td className="py-3 px-2">
                        <div className="t-body-sm">{item.description}</div>
                        {item.unitMeasure && (
                          <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                            {item.unitMeasure}
                          </div>
                        )}
                      </td>
                      <td className="py-3 px-2 text-right mono tnum t-body-sm">{item.quantity}</td>
                      <td className="py-3 px-2 text-right mono tnum t-body-sm">
                        {fmt(item.unitPrice, doc.currency)}
                      </td>
                      <td className="py-3 px-2 text-right mono tnum t-body-sm">
                        {fmt(item.igvAmount, doc.currency)}
                      </td>
                      <td className="py-3 pr-6 pl-2 text-right mono tnum t-body-sm font-semibold">
                        {fmt(item.total, doc.currency)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="flex justify-end mt-4">
              <div className="w-full max-w-[280px] flex flex-col gap-2">
                {doc.operacionGravada > 0 && (
                  <div className="flex justify-between t-body-sm">
                    <span style={{ color: 'var(--muted-foreground)' }}>Op. gravada</span>
                    <span className="mono tnum">{fmt(doc.operacionGravada, doc.currency)}</span>
                  </div>
                )}
                {doc.operacionExonerada > 0 && (
                  <div className="flex justify-between t-body-sm">
                    <span style={{ color: 'var(--muted-foreground)' }}>Op. exonerada</span>
                    <span className="mono tnum">{fmt(doc.operacionExonerada, doc.currency)}</span>
                  </div>
                )}
                <div className="flex justify-between t-body-sm">
                  <span style={{ color: 'var(--muted-foreground)' }}>IGV (18%)</span>
                  <span className="mono tnum">{fmt(doc.igv, doc.currency)}</span>
                </div>
                <div
                  className="flex justify-between items-baseline pt-2 mt-1"
                  style={{ borderTop: '1px solid var(--border)' }}
                >
                  <span className="t-h3">Total</span>
                  <span className="t-num-md mono">{fmt(doc.total, doc.currency)}</span>
                </div>
              </div>
            </div>
          </Section>

          <Section title="Historial SUNAT">
            <Timeline items={timeline} />
          </Section>

          {doc.notes && (
            <Section title="Observaciones">
              <p className="t-body-sm m-0">{doc.notes}</p>
            </Section>
          )}
        </div>

        {/* Right: sticky actions */}
        <aside className="lg:col-span-1 min-w-0">
          <div className="lg:sticky lg:top-20 flex flex-col gap-[var(--gap-cards)]">
            <Section title="Acciones">
              <div className="flex flex-col gap-2.5">
                <Button onClick={() => downloadFile('pdf')} disabled={downloading === 'pdf'}>
                  {downloading === 'pdf' ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Descargando…
                    </>
                  ) : (
                    <>
                      <Download className="h-4 w-4 mr-2" /> Descargar PDF
                    </>
                  )}
                </Button>
                <Button variant="outline" onClick={() => downloadFile('xml')} disabled={downloading === 'xml'}>
                  {downloading === 'xml' ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Descargando…
                    </>
                  ) : (
                    <>
                      <FileCode className="h-4 w-4 mr-2" /> Descargar XML
                    </>
                  )}
                </Button>
                {hasCdr && (
                  <Button
                    variant="outline"
                    onClick={() => downloadFile('cdr')}
                    disabled={downloading === 'cdr'}
                  >
                    {downloading === 'cdr' ? (
                      <>
                        <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Descargando…
                      </>
                    ) : (
                      <>
                        <ShieldCheck className="h-4 w-4 mr-2" /> Descargar CDR
                      </>
                    )}
                  </Button>
                )}
                <Button
                  variant="outline"
                  onClick={() =>
                    toast.info(
                      'Envío por email pendiente — configura un proveedor SMTP en Servicios externos.'
                    )
                  }
                >
                  <Mail className="h-4 w-4 mr-2" /> Enviar por email
                </Button>
                {canCreditNote && (
                  <Button variant="outline" asChild>
                    <Link href={`/documents/credit-note?ref=${id}`}>
                      <ReceiptText className="h-4 w-4 mr-2" /> Emitir nota de crédito
                    </Link>
                  </Button>
                )}
                {canVoid && (
                  <>
                    <div className="h-px my-1" style={{ background: 'var(--border)' }} />
                    <Button variant="destructive" onClick={() => setVoidDialogOpen(true)}>
                      <Ban className="h-4 w-4 mr-2" /> Anular comprobante
                    </Button>
                  </>
                )}
              </div>
            </Section>

            <Section title="Resumen">
              <div className="flex flex-col gap-2.5 t-body-sm">
                <SumRow label="Tipo" value={<span>{doc.documentTypeName}</span>} />
                <SumRow label="Estado" value={<StatusBadge status={doc.status} />} />
                <SumRow label="Emisión" value={<span className="mono tnum">{formatDateOnly(doc.issueDate)}</span>} />
                {doc.sunatResponseCode && (
                  <SumRow
                    label="Código SUNAT"
                    value={
                      <span className="mono tnum font-semibold">{doc.sunatResponseCode}</span>
                    }
                  />
                )}
                <div className="h-px" style={{ background: 'var(--border)' }} />
                <SumRow
                  label="Total"
                  value={<span className="t-num-md mono">{fmt(doc.total, doc.currency)}</span>}
                />
              </div>
            </Section>
          </div>
        </aside>
      </div>
    </div>
  );
}

function SumRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <span style={{ color: 'var(--muted-foreground)' }}>{label}</span>
      <span className="text-right">{value}</span>
    </div>
  );
}
