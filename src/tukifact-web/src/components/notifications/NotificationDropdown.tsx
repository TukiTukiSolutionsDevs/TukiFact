'use client';

import { useRouter } from 'next/navigation';
import { CheckCheck, FileText, AlertTriangle, Truck, ShieldCheck, ShieldAlert, FileSpreadsheet, Ban } from 'lucide-react';
import type { Notification } from '@/lib/useNotifications';
import { cn } from '@/lib/utils';

interface NotificationDropdownProps {
  notifications: Notification[];
  unreadCount: number;
  onMarkAsRead: (id: string) => void;
  onMarkAllAsRead: () => void;
  onClose: () => void;
}

const EVENT_ICONS: Record<string, typeof FileText> = {
  'document.created': FileText,
  'document.sent': FileText,
  'document.failed': AlertTriangle,
  'document.voided': Ban,
  'quotation.created': FileSpreadsheet,
  'quotation.converted': FileSpreadsheet,
  'retention.created': ShieldCheck,
  'perception.created': ShieldAlert,
  'despatch.emitted': Truck,
};

const EVENT_COLORS: Record<string, string> = {
  'document.created': 'text-blue-500',
  'document.sent': 'text-green-500',
  'document.failed': 'text-red-500',
  'document.voided': 'text-orange-500',
  'quotation.created': 'text-purple-500',
  'quotation.converted': 'text-purple-600',
  'retention.created': 'text-teal-500',
  'perception.created': 'text-teal-600',
  'despatch.emitted': 'text-indigo-500',
};

function getEntityRoute(entityType: string | null, entityId: string | null): string | null {
  if (!entityType || !entityId) return null;
  switch (entityType) {
    case 'Document': return `/documents/${entityId}`;
    case 'Quotation': return `/quotations/${entityId}`;
    case 'DespatchAdvice': return `/despatch-advices/${entityId}`;
    case 'Retention': return `/retentions`;
    case 'Perception': return `/perceptions`;
    default: return null;
  }
}

function timeAgo(dateStr: string): string {
  const now = new Date();
  const date = new Date(dateStr);
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (seconds < 60) return 'ahora';
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h`;
  return `${Math.floor(seconds / 86400)}d`;
}

export function NotificationDropdown({
  notifications,
  unreadCount,
  onMarkAsRead,
  onMarkAllAsRead,
  onClose,
}: NotificationDropdownProps) {
  const router = useRouter();

  const handleClick = (notification: Notification) => {
    if (!notification.isRead) {
      onMarkAsRead(notification.id);
    }
    const route = getEntityRoute(notification.entityType, notification.entityId);
    if (route) {
      router.push(route);
      onClose();
    }
  };

  return (
    <div className="absolute right-0 top-full z-50 mt-2 w-96 rounded-lg border bg-card shadow-lg">
      {/* Header */}
      <div className="flex items-center justify-between border-b px-4 py-3">
        <h3 className="text-sm font-semibold">
          Notificaciones
          {unreadCount > 0 && (
            <span className="ml-2 rounded-full bg-red-100 px-2 py-0.5 text-xs font-bold text-red-600 dark:bg-red-900 dark:text-red-300">
              {unreadCount}
            </span>
          )}
        </h3>
        {unreadCount > 0 && (
          <button
            onClick={onMarkAllAsRead}
            className="flex items-center gap-1 text-xs text-blue-600 hover:text-blue-800 dark:text-blue-400"
          >
            <CheckCheck className="h-3.5 w-3.5" />
            Marcar todas
          </button>
        )}
      </div>

      {/* Notifications list */}
      <div className="max-h-96 overflow-y-auto">
        {notifications.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8 text-muted-foreground">
            <FileText className="mb-2 h-8 w-8 opacity-50" />
            <p className="text-sm">Sin notificaciones</p>
          </div>
        ) : (
          notifications.map((notification) => {
            const Icon = EVENT_ICONS[notification.type] ?? FileText;
            const color = EVENT_COLORS[notification.type] ?? 'text-gray-500';
            const route = getEntityRoute(notification.entityType, notification.entityId);

            return (
              <button
                key={notification.id}
                onClick={() => handleClick(notification)}
                className={cn(
                  'flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/50',
                  !notification.isRead && 'bg-blue-50/50 dark:bg-blue-950/20',
                  route && 'cursor-pointer'
                )}
              >
                <div className={cn('mt-0.5 shrink-0', color)}>
                  <Icon className="h-4 w-4" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className={cn(
                    'text-sm leading-tight',
                    !notification.isRead ? 'font-semibold' : 'font-medium text-muted-foreground'
                  )}>
                    {notification.title}
                  </p>
                  {notification.body && (
                    <p className="mt-0.5 text-xs text-muted-foreground line-clamp-2">
                      {notification.body}
                    </p>
                  )}
                  <p className="mt-1 text-[10px] text-muted-foreground/70">
                    {timeAgo(notification.createdAt)}
                  </p>
                </div>
                {!notification.isRead && (
                  <div className="mt-2 h-2 w-2 shrink-0 rounded-full bg-blue-500" />
                )}
              </button>
            );
          })
        )}
      </div>
    </div>
  );
}
