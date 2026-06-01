'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { api, type DespatchAdviceResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import {
  ArrowLeft,
  Send,
  Download,
  RefreshCw,
  CheckCircle2,
  XCircle,
  Loader2,
  AlertCircle,
  Clock,
  MapPin,
  PackageCheck,
  Truck,
  Bus,
  Hash,
  FileText,
  FileArchive,
  Calendar,
  Building2,
  User as UserIcon,
  Ban,
} from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { toast } from 'sonner';

const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });

const formatDateTime = (iso: string) =>
  new Date(iso).toLocaleString('es-PE', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

type StatusInfo = {
  label: string;
  color: string;
  icon: React.ElementType;
};

const STATUS: Record<string, StatusInfo> = {
  accepted: { label: 'Aceptada', color: 'var(--success)', icon: CheckCircle2 },
  rejected: { label: 'Rechazada', color: 'var(--danger)', icon: XCircle },
  draft: { label: 'Borrador', color: 'var(--slate-500)', icon: FileText },
  signed: { label: 'Firmada', color: 'var(--info)', icon: CheckCircle2 },
  sent: { label: 'Enviada · pendiente CDR', color: 'var(--warning)', icon: Clock },
  pending_ticket: { label: 'Pendiente ticket', color: 'var(--warning)', icon: Clock },
  cancelled: { label: 'Anulada', color: 'var(--slate-500)', icon: Ban },
};

const statusInfo = (s: string) => STATUS[s] ?? { label: s, color: 'var(--slate-500)', icon: FileText };

const transportLabel: Record<string, string> = {
  '01': 'Transporte público',
  '02': 'Transporte privado',
};

function Section({
  title,
  desc,
  right,
  children,
}: {
  title: string;
  desc?: string;
  right?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section
      className="rounded-[var(--radius-lg)] border bg-card p-6"
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      <div className="flex items-start justify-between gap-3 mb-4">
        <div>
          <h2 className="t-h2 m-0">{title}</h2>
          {desc && (
            <p className="t-body-sm m-0 mt-0.5" style={{ color: 'var(--muted-foreground)' }}>
              {desc}
            </p>
          )}
        </div>
        {right}
      </div>
      {children}
    </section>
  );
}

function Field({ label, children, mono }: { label: string; children: React.ReactNode; mono?: boolean }) {
  return (
    <div>
      <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
        {label}
      </div>
      <div className={`t-body-sm font-semibold mt-0.5 ${mono ? 'mono tnum' : ''}`}>{children}</div>
    </div>
  );
}

export default function DespatchAdviceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [gre, setGre] = useState<DespatchAdviceResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isEmitting, setIsEmitting] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [cancelOpen, setCancelOpen] = useState(false);
  const [cancelReason, setCancelReason] = useState('');
  const [isCancelling, setIsCancelling] = useState(false);

  useEffect(() => {
    api
      .get<DespatchAdviceResponse>(`/v1/despatch-advices/${id}`)
      .then(setGre)
      .catch((err) => {
        console.error(err);
        toast.error(err instanceof Error ? err.message : 'Error cargando la guía');
      })
      .finally(() => setIsLoading(false));
  }, [id]);

  const emitToSunat = async () => {
    setIsEmitting(true);
    try {
      const res = await api.post<DespatchAdviceResponse>(
        `/v1/despatch-advices/${id}/emit`,
        {}
      );
      setGre(res);
      toast.success(`GRE emitida — Estado: ${statusInfo(res.status).label}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al emitir');
    } finally {
      setIsEmitting(false);
    }
  };

  const cancel = async () => {
    setIsCancelling(true);
    try {
      const res = await api.post<DespatchAdviceResponse>(
        `/v1/despatch-advices/${id}/cancel`,
        { reason: cancelReason.trim() || null }
      );
      setGre(res);
      setCancelOpen(false);
      setCancelReason('');
      toast.success('Guía anulada localmente. Confirma la anulación en el portal SOL.');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al anular');
    } finally {
      setIsCancelling(false);
    }
  };

  const refreshStatus = async () => {
    setIsRefreshing(true);
    try {
      const res = await api.post<DespatchAdviceResponse>(
        `/v1/despatch-advices/${id}/refresh-status`,
        {}
      );
      setGre(res);
      toast.success(`Estado actualizado: ${statusInfo(res.status).label}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al actualizar estado');
    } finally {
      setIsRefreshing(false);
    }
  };

  if (isLoading) {
    return (
      <div
        className="rounded-[var(--radius-lg)] border bg-card p-6"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        <div className="flex items-center gap-3" style={{ color: 'var(--muted-foreground)' }}>
          <Loader2 className="h-4 w-4 animate-spin" />
          <span className="t-body-sm">Cargando guía…</span>
        </div>
      </div>
    );
  }

  if (!gre) {
    return (
      <div
        className="rounded-[var(--radius-lg)] border bg-card p-8 text-center"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        <AlertCircle className="h-8 w-8 mx-auto mb-2" style={{ color: 'var(--slate-400)' }} />
        <p className="t-body m-0 font-semibold">Guía no encontrada</p>
        <p
          className="t-body-sm mt-1 mb-4"
          style={{ color: 'var(--muted-foreground)' }}
        >
          La guía no existe o ya no tienes permisos para verla.
        </p>
        <Button variant="outline" onClick={() => router.push('/despatch-advices')}>
          <ArrowLeft className="h-4 w-4 mr-2" /> Volver a guías
        </Button>
      </div>
    );
  }

  const st = statusInfo(gre.status);
  const StIcon = st.icon;
  const canEmit = gre.status === 'draft';
  const canRefresh = gre.status === 'sent' && !!gre.sunatTicket;
  const canCancel = gre.status === 'accepted' || gre.status === 'sent';
  const transportIcon = gre.transportMode === '02' ? Truck : Bus;
  const TransportIcon = transportIcon;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <div className="flex items-center gap-2 mb-2">
            <span
              className="mono tnum t-h3"
              style={{ color: 'var(--foreground)' }}
            >
              {gre.fullNumber}
            </span>
            <span
              className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 t-caption font-semibold"
              style={{
                color: st.color,
                background: `color-mix(in oklch, ${st.color} 14%, transparent)`,
              }}
            >
              <StIcon className="h-3.5 w-3.5" />
              {st.label}
            </span>
          </div>
          <p className="t-body m-0" style={{ color: 'var(--muted-foreground)' }}>
            Guía de remisión electrónica · Emitida {formatDate(gre.issueDate)} · Traslado{' '}
            {formatDate(gre.transferStartDate)}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2 shrink-0">
          {gre.xmlUrl && (
            <Button asChild variant="outline" size="sm">
              <a href={gre.xmlUrl} target="_blank" rel="noreferrer">
                <FileText className="h-4 w-4 mr-1.5" /> XML
              </a>
            </Button>
          )}
          {gre.cdrUrl && (
            <Button asChild variant="outline" size="sm">
              <a href={gre.cdrUrl} target="_blank" rel="noreferrer">
                <FileArchive className="h-4 w-4 mr-1.5" /> CDR
              </a>
            </Button>
          )}
          {gre.pdfUrl && (
            <Button asChild variant="outline" size="sm">
              <a href={gre.pdfUrl} target="_blank" rel="noreferrer">
                <Download className="h-4 w-4 mr-1.5" /> PDF
              </a>
            </Button>
          )}
          {canRefresh && (
            <Button
              variant="outline"
              onClick={refreshStatus}
              disabled={isRefreshing}
            >
              <RefreshCw className={`h-4 w-4 mr-1.5 ${isRefreshing ? 'animate-spin' : ''}`} />
              {isRefreshing ? 'Consultando…' : 'Refrescar estado'}
            </Button>
          )}
          {canCancel && (
            <Button
              variant="outline"
              onClick={() => setCancelOpen(true)}
              style={{ color: 'var(--danger)', borderColor: 'color-mix(in oklch, var(--danger) 35%, transparent)' }}
            >
              <Ban className="h-4 w-4 mr-1.5" /> Anular
            </Button>
          )}
          {canEmit && (
            <Button
              onClick={emitToSunat}
              disabled={isEmitting}
              style={{ background: 'var(--accent)', color: 'var(--accent-foreground)', fontWeight: 600 }}
            >
              {isEmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-1.5 animate-spin" /> Emitiendo…
                </>
              ) : (
                <>
                  <Send className="h-4 w-4 mr-1.5" /> Emitir a SUNAT
                </>
              )}
            </Button>
          )}
        </div>
      </div>

      {/* SUNAT response banner */}
      {gre.sunatResponseCode && (
        <div
          className="rounded-[var(--radius-lg)] border p-4 mb-[var(--gap-cards)] flex items-start gap-3"
          style={{
            background: `color-mix(in oklch, ${st.color} 6%, transparent)`,
            borderColor: `color-mix(in oklch, ${st.color} 25%, transparent)`,
          }}
        >
          <StIcon
            className="h-5 w-5 shrink-0 mt-0.5"
            style={{ color: st.color }}
          />
          <div className="flex-1 min-w-0">
            <p className="t-body-sm m-0 font-semibold">
              SUNAT respondió{' '}
              <span className="mono">{gre.sunatResponseCode}</span>
              {gre.sunatResponseMessage ? ` — ${gre.sunatResponseMessage}` : ''}
            </p>
            {gre.sunatTicket && (
              <p
                className="t-caption m-0 mt-1 mono"
                style={{ color: 'var(--muted-foreground)' }}
              >
                Ticket: {gre.sunatTicket}
              </p>
            )}
            {gre.status === 'sent' && (
              <p
                className="t-caption m-0 mt-1"
                style={{ color: 'var(--muted-foreground)' }}
              >
                SUNAT está procesando la guía. Pulsa "Refrescar estado" para consultar el CDR.
              </p>
            )}
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)]">
        {/* Left: detail sections */}
        <div className="lg:col-span-2 flex flex-col gap-[var(--gap-cards)]">
          {/* Destinatario */}
          <Section title="Destinatario">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Field label="Tipo de documento" mono>
                {gre.recipientDocType}
              </Field>
              <Field label="Número de documento" mono>
                {gre.recipientDocNumber}
              </Field>
              <Field label="Razón social / Nombre">{gre.recipientName}</Field>
            </div>
          </Section>

          {/* Trayecto */}
          <Section title="Trayecto" desc="Origen y destino del traslado.">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
              <div className="rounded-[var(--radius-md)] border p-4" style={{ borderColor: 'var(--border)' }}>
                <div className="flex items-center gap-2 mb-3">
                  <span
                    className="flex h-8 w-8 items-center justify-center rounded-md"
                    style={{
                      background: 'color-mix(in oklch, var(--info) 14%, transparent)',
                      color: 'var(--info)',
                    }}
                  >
                    <MapPin className="h-4 w-4" />
                  </span>
                  <span className="t-h3 m-0">Origen</span>
                </div>
                <Field label="Dirección">{gre.originAddress}</Field>
                <div className="mt-3">
                  <Field label="Ubigeo" mono>
                    {gre.originUbigeo}
                  </Field>
                </div>
              </div>

              <div className="rounded-[var(--radius-md)] border p-4" style={{ borderColor: 'var(--border)' }}>
                <div className="flex items-center gap-2 mb-3">
                  <span
                    className="flex h-8 w-8 items-center justify-center rounded-md"
                    style={{
                      background: 'color-mix(in oklch, var(--success) 14%, transparent)',
                      color: 'var(--success)',
                    }}
                  >
                    <PackageCheck className="h-4 w-4" />
                  </span>
                  <span className="t-h3 m-0">Destino</span>
                </div>
                <Field label="Dirección">{gre.destinationAddress}</Field>
                <div className="mt-3">
                  <Field label="Ubigeo" mono>
                    {gre.destinationUbigeo}
                  </Field>
                </div>
              </div>
            </div>
          </Section>

          {/* Traslado */}
          <Section title="Detalles del traslado">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Field label="Motivo">
                <span className="mono mr-1.5">{gre.transferReasonCode}</span>
                {gre.transferReasonDescription}
              </Field>
              <Field label="Modalidad">
                <span className="inline-flex items-center gap-1.5">
                  <TransportIcon className="h-3.5 w-3.5" />
                  {transportLabel[gre.transportMode] || gre.transportMode}
                </span>
              </Field>
              <Field label="Fecha de inicio" mono>
                {formatDate(gre.transferStartDate)}
              </Field>
              <Field label="Peso bruto" mono>
                {gre.grossWeight} {gre.weightUnitCode}
              </Field>
              <Field label="Número de bultos" mono>
                {gre.totalPackages}
              </Field>
            </div>
          </Section>

          {/* Conductor / Transportista */}
          {(gre.driverName || gre.carrierName) && (
            <Section
              title={gre.transportMode === '02' ? 'Conductor y vehículo' : 'Transportista'}
            >
              {gre.transportMode === '02' ? (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {gre.driverName && (
                    <>
                      <Field label="Nombre completo">{gre.driverName}</Field>
                      <Field label="DNI" mono>
                        {gre.driverDocNumber}
                      </Field>
                      {gre.driverLicense && (
                        <Field label="Licencia" mono>
                          {gre.driverLicense}
                        </Field>
                      )}
                      {gre.vehiclePlate && (
                        <Field label="Placa" mono>
                          {gre.vehiclePlate}
                        </Field>
                      )}
                    </>
                  )}
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {gre.carrierName && (
                    <>
                      <Field label="Razón Social">{gre.carrierName}</Field>
                      <Field label="RUC" mono>
                        {gre.carrierDocNumber}
                      </Field>
                    </>
                  )}
                </div>
              )}
            </Section>
          )}

          {/* Items */}
          <Section
            title="Mercadería"
            desc={`${gre.items.length} ${gre.items.length === 1 ? 'línea' : 'líneas'}`}
          >
            <div className="-mx-6 overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr
                    className="t-overline"
                    style={{ color: 'var(--muted-foreground)', background: 'var(--muted)' }}
                  >
                    <th className="text-left py-2.5 pl-6 pr-2 w-10">#</th>
                    <th className="text-left py-2.5 px-2 w-32">Código</th>
                    <th className="text-left py-2.5 px-2">Descripción</th>
                    <th className="text-right py-2.5 px-2 w-28">Cantidad</th>
                    <th className="text-left py-2.5 pr-6 pl-2 w-24">Unidad</th>
                  </tr>
                </thead>
                <tbody>
                  {gre.items.map((item, i) => (
                    <tr
                      key={item.lineNumber}
                      style={{ borderTop: i > 0 ? '1px solid var(--border)' : '1px solid var(--border)' }}
                    >
                      <td
                        className="py-3 pl-6 pr-2 t-body-sm tnum"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        {item.lineNumber}
                      </td>
                      <td className="py-3 px-2 mono t-body-sm">{item.productCode || '—'}</td>
                      <td className="py-3 px-2 t-body-sm">{item.description}</td>
                      <td className="py-3 px-2 text-right mono tnum t-body-sm font-semibold">
                        {item.quantity}
                      </td>
                      <td className="py-3 pr-6 pl-2 mono t-body-sm">{item.unitCode}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Section>
        </div>

        {/* Right: sticky summary */}
        <aside className="lg:col-span-1">
          <div
            className="rounded-[var(--radius-lg)] border bg-card p-6 lg:sticky lg:top-20 flex flex-col gap-3"
            style={{ boxShadow: 'var(--shadow-xs)' }}
          >
            <h2 className="t-h2 m-0 mb-1">Resumen</h2>

            <div
              className="rounded-[var(--radius-md)] p-3 flex items-center gap-3"
              style={{
                background: `color-mix(in oklch, ${st.color} 10%, transparent)`,
                border: `1px solid color-mix(in oklch, ${st.color} 30%, transparent)`,
              }}
            >
              <StIcon className="h-5 w-5 shrink-0" style={{ color: st.color }} />
              <div className="min-w-0">
                <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                  Estado actual
                </div>
                <div className="t-body-sm font-semibold" style={{ color: st.color }}>
                  {st.label}
                </div>
              </div>
            </div>

            <Field label="Número">
              <span className="mono tnum">{gre.fullNumber}</span>
            </Field>

            <Field label="Tipo">
              {gre.documentType === '09' ? 'GRE Remitente' : 'GRE Transportista'} ·{' '}
              <span className="mono">{gre.documentType}</span>
            </Field>

            <Field label="Emitida" mono>
              {formatDate(gre.issueDate)} {gre.issueTime}
            </Field>

            <Field label="Traslado">
              <span className="inline-flex items-center gap-1.5">
                <Calendar className="h-3.5 w-3.5" style={{ color: 'var(--muted-foreground)' }} />
                <span className="mono tnum">{formatDate(gre.transferStartDate)}</span>
              </span>
            </Field>

            {gre.sunatTicket && (
              <Field label="Ticket SUNAT" mono>
                {gre.sunatTicket}
              </Field>
            )}

            <Field label="Creado">{formatDateTime(gre.createdAt)}</Field>

            <div className="my-2 h-px" style={{ background: 'var(--border)' }} />

            <Field label="Destinatario">
              <span className="flex items-start gap-2">
                {gre.recipientDocType === '6' ? (
                  <Building2 className="h-3.5 w-3.5 mt-0.5" style={{ color: 'var(--muted-foreground)' }} />
                ) : (
                  <UserIcon className="h-3.5 w-3.5 mt-0.5" style={{ color: 'var(--muted-foreground)' }} />
                )}
                <span className="min-w-0">
                  {gre.recipientName}
                  <br />
                  <span className="t-caption mono" style={{ color: 'var(--muted-foreground)' }}>
                    {gre.recipientDocType === '6' ? 'RUC' : 'DNI'} {gre.recipientDocNumber}
                  </span>
                </span>
              </span>
            </Field>

            <div
              className="rounded-[var(--radius-md)] border p-3 flex items-center gap-3"
              style={{ borderColor: 'var(--border)' }}
            >
              <span
                className="flex h-7 w-7 items-center justify-center rounded-md shrink-0"
                style={{
                  background: 'color-mix(in oklch, var(--info) 14%, transparent)',
                  color: 'var(--info)',
                }}
              >
                <MapPin className="h-3.5 w-3.5" />
              </span>
              <div className="min-w-0 flex-1">
                <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                  Origen
                </div>
                <div className="mono tnum t-body-sm font-semibold">{gre.originUbigeo}</div>
              </div>
              <span style={{ color: 'var(--muted-foreground)' }}>→</span>
              <span
                className="flex h-7 w-7 items-center justify-center rounded-md shrink-0"
                style={{
                  background: 'color-mix(in oklch, var(--success) 14%, transparent)',
                  color: 'var(--success)',
                }}
              >
                <PackageCheck className="h-3.5 w-3.5" />
              </span>
              <div className="min-w-0 flex-1 text-right">
                <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                  Destino
                </div>
                <div className="mono tnum t-body-sm font-semibold">{gre.destinationUbigeo}</div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div
                className="rounded-[var(--radius-md)] border p-3"
                style={{ borderColor: 'var(--border)' }}
              >
                <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                  Bultos
                </div>
                <div className="t-num-md mono mt-0.5">{gre.totalPackages}</div>
              </div>
              <div
                className="rounded-[var(--radius-md)] border p-3"
                style={{ borderColor: 'var(--border)' }}
              >
                <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                  Peso ({gre.weightUnitCode})
                </div>
                <div className="t-num-md mono mt-0.5">{gre.grossWeight.toFixed(2)}</div>
              </div>
            </div>

            <div className="my-2 h-px" style={{ background: 'var(--border)' }} />

            <div
              className="rounded-[var(--radius-md)] border p-3 flex items-center justify-between"
              style={{ borderColor: 'var(--border)' }}
            >
              <span className="inline-flex items-center gap-1.5 t-body-sm">
                <Hash className="h-3.5 w-3.5" style={{ color: 'var(--muted-foreground)' }} />
                Mercadería
              </span>
              <span className="mono tnum font-semibold t-body-sm">
                {gre.items.length} {gre.items.length === 1 ? 'línea' : 'líneas'}
              </span>
            </div>
          </div>
        </aside>
      </div>

      <Dialog open={cancelOpen} onOpenChange={setCancelOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Anular guía de remisión</DialogTitle>
            <DialogDescription>
              Esto marcará la guía como anulada en TukiFact y dejará el registro en el audit log.
              Recuerda que para anular formalmente ante SUNAT debes hacerlo desde el portal SOL.
            </DialogDescription>
          </DialogHeader>

          <div className="mt-2">
            <label className="t-label mb-1.5 block">Motivo (opcional)</label>
            <Textarea
              placeholder="Ej. cliente canceló el pedido, mercadería dañada en almacén…"
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
              rows={3}
            />
          </div>

          <DialogFooter className="gap-2">
            <Button variant="outline" onClick={() => setCancelOpen(false)} disabled={isCancelling}>
              Volver
            </Button>
            <Button
              variant="destructive"
              onClick={cancel}
              disabled={isCancelling}
            >
              {isCancelling ? (
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
    </div>
  );
}
