import {
  CheckCircle2,
  XCircle,
  Clock,
  Ban,
  FileText,
  AlertTriangle,
  Pause,
  type LucideIcon,
} from 'lucide-react';

export type StatusKey =
  | 'draft'
  | 'signed'
  | 'sent'
  | 'accepted'
  | 'rejected'
  | 'voided'
  | 'pending'
  | 'pending_ticket'
  | 'expired'
  | 'active'
  | 'paused'
  | 'cancelled'
  | 'completed'
  | 'error'
  | string;

type StatusInfo = { label: string; color: string; icon: LucideIcon };

const STATUS: Record<string, StatusInfo> = {
  draft: { label: 'Borrador', color: 'var(--slate-500)', icon: FileText },
  signed: { label: 'Firmado', color: 'var(--info)', icon: CheckCircle2 },
  sent: { label: 'Enviado', color: 'var(--warning)', icon: Clock },
  accepted: { label: 'Aceptado', color: 'var(--success)', icon: CheckCircle2 },
  rejected: { label: 'Rechazado', color: 'var(--danger)', icon: XCircle },
  voided: { label: 'Anulado', color: 'var(--slate-500)', icon: Ban },
  pending: { label: 'Pendiente', color: 'var(--warning)', icon: Clock },
  pending_ticket: { label: 'Pendiente ticket', color: 'var(--warning)', icon: Clock },
  expired: { label: 'Vencida', color: 'var(--slate-500)', icon: Ban },
  active: { label: 'Activa', color: 'var(--success)', icon: CheckCircle2 },
  paused: { label: 'Pausada', color: 'var(--warning)', icon: Pause },
  cancelled: { label: 'Cancelada', color: 'var(--danger)', icon: XCircle },
  completed: { label: 'Completada', color: 'var(--info)', icon: CheckCircle2 },
  error: { label: 'Error', color: 'var(--danger)', icon: AlertTriangle },
};

export function statusInfoFor(status: string): StatusInfo {
  return STATUS[status] ?? { label: status, color: 'var(--slate-500)', icon: FileText };
}

interface StatusBadgeProps {
  status: string;
  label?: string;
  className?: string;
}

export function StatusBadge({ status, label, className }: StatusBadgeProps) {
  const info = statusInfoFor(status);
  const Icon = info.icon;
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 t-caption font-semibold whitespace-nowrap ${className ?? ''}`}
      style={{
        color: info.color,
        background: `color-mix(in oklch, ${info.color} 14%, transparent)`,
      }}
    >
      <Icon className="h-3 w-3" />
      {label ?? info.label}
    </span>
  );
}
