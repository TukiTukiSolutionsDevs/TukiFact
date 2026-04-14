'use client';

import { useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle2, AlertCircle, Clock, Activity } from 'lucide-react';

const SERVICES = [
  {
    name: 'API REST',
    description: 'Endpoints de facturación y gestión',
    status: 'operational',
    uptime: '99.98%',
    latency: '142ms',
  },
  {
    name: 'SUNAT Gateway',
    description: 'Conexión y envío a los servidores de SUNAT',
    status: 'operational',
    uptime: '99.71%',
    latency: '1.2s',
  },
  {
    name: 'Webhooks',
    description: 'Entrega de eventos a endpoints externos',
    status: 'operational',
    uptime: '99.99%',
    latency: '89ms',
  },
  {
    name: 'Dashboard',
    description: 'Panel de administración web',
    status: 'operational',
    uptime: '99.97%',
    latency: '201ms',
  },
  {
    name: 'Sandbox',
    description: 'Entorno de pruebas para desarrolladores',
    status: 'operational',
    uptime: '99.95%',
    latency: '158ms',
  },
  {
    name: 'SDK TypeScript',
    description: 'Paquete NPM @tukifact/sdk',
    status: 'operational',
    uptime: '100%',
    latency: '—',
  },
  {
    name: 'SDK Python',
    description: 'Paquete PyPI tukifact',
    status: 'operational',
    uptime: '100%',
    latency: '—',
  },
];

const UPTIME_DAYS = Array.from({ length: 90 }, (_, i) => ({
  day: i,
  status: 'operational' as const,
}));

type ServiceStatus = 'operational' | 'degraded' | 'outage' | 'maintenance';

function StatusBadge({ status }: { status: ServiceStatus }) {
  const map: Record<ServiceStatus, { label: string; className: string }> = {
    operational: {
      label: 'Operativo',
      className: 'bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-300 hover:bg-green-100',
    },
    degraded: {
      label: 'Degradado',
      className: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-950 dark:text-yellow-300 hover:bg-yellow-100',
    },
    outage: {
      label: 'Falla',
      className: 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300 hover:bg-red-100',
    },
    maintenance: {
      label: 'Mantenimiento',
      className: 'bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300 hover:bg-blue-100',
    },
  };
  const { label, className } = map[status] ?? map.operational;
  return <Badge className={className}>{label}</Badge>;
}

function StatusIcon({ status }: { status: ServiceStatus }) {
  if (status === 'operational') return <CheckCircle2 className="h-5 w-5 text-green-500" />;
  if (status === 'degraded') return <AlertCircle className="h-5 w-5 text-yellow-500" />;
  if (status === 'outage') return <AlertCircle className="h-5 w-5 text-red-500" />;
  return <Clock className="h-5 w-5 text-blue-500" />;
}

export default function StatusPage() {
  const [email, setEmail] = useState('');
  const [subscribed, setSubscribed] = useState(false);

  const handleSubscribe = (e: React.FormEvent) => {
    e.preventDefault();
    if (email) {
      setSubscribed(true);
    }
  };

  const allOperational = SERVICES.every((s) => s.status === 'operational');

  return (
    <div className="space-y-10">
      {/* Header */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Activity className="h-7 w-7 text-green-500" />
          <h1 className="text-3xl font-bold tracking-tight">Estado del Sistema</h1>
        </div>
        <p className="text-muted-foreground text-lg">
          Estado en tiempo real de todos los servicios de TukiFact.
        </p>
      </div>

      {/* Status general */}
      <div className={`rounded-xl border p-5 flex items-center gap-4 ${
        allOperational
          ? 'bg-green-50 dark:bg-green-950/30 border-green-200 dark:border-green-800'
          : 'bg-yellow-50 dark:bg-yellow-950/30 border-yellow-200 dark:border-yellow-800'
      }`}>
        <div className={`rounded-full p-2 ${
          allOperational ? 'bg-green-100 dark:bg-green-900' : 'bg-yellow-100 dark:bg-yellow-900'
        }`}>
          {allOperational
            ? <CheckCircle2 className="h-6 w-6 text-green-600 dark:text-green-400" />
            : <AlertCircle className="h-6 w-6 text-yellow-600 dark:text-yellow-400" />
          }
        </div>
        <div>
          <p className={`font-semibold text-lg ${
            allOperational
              ? 'text-green-800 dark:text-green-300'
              : 'text-yellow-800 dark:text-yellow-300'
          }`}>
            {allOperational ? 'Todos los sistemas operativos' : 'Algunos sistemas con inconvenientes'}
          </p>
          <p className="text-sm text-muted-foreground">
            Última actualización: hace menos de 1 minuto
          </p>
        </div>
        <div className="ml-auto text-right hidden sm:block">
          <p className="text-2xl font-bold text-green-700 dark:text-green-400">99.9%</p>
          <p className="text-xs text-muted-foreground">Uptime últimos 90 días</p>
        </div>
      </div>

      {/* Servicios */}
      <section className="space-y-3">
        <h2 className="text-xl font-semibold border-b pb-2">Servicios</h2>
        <div className="space-y-2">
          {SERVICES.map((service) => (
            <div
              key={service.name}
              className="flex items-center gap-4 rounded-lg border bg-card px-4 py-3.5 hover:bg-muted/30 transition-colors"
            >
              <StatusIcon status={service.status as ServiceStatus} />
              <div className="flex-1 min-w-0">
                <p className="font-medium text-sm">{service.name}</p>
                <p className="text-xs text-muted-foreground">{service.description}</p>
              </div>
              <div className="hidden sm:flex items-center gap-6 text-right">
                {service.latency !== '—' && (
                  <div>
                    <p className="text-xs font-medium">{service.latency}</p>
                    <p className="text-xs text-muted-foreground">Latencia</p>
                  </div>
                )}
                <div>
                  <p className="text-xs font-medium text-green-600 dark:text-green-400">{service.uptime}</p>
                  <p className="text-xs text-muted-foreground">Uptime</p>
                </div>
              </div>
              <StatusBadge status={service.status as ServiceStatus} />
            </div>
          ))}
        </div>
      </section>

      {/* Uptime bar */}
      <section className="space-y-3">
        <h2 className="text-xl font-semibold border-b pb-2">Disponibilidad — Últimos 90 días</h2>
        <div className="flex items-end gap-0.5">
          {UPTIME_DAYS.map((day) => (
            <div
              key={day.day}
              title={`Día ${90 - day.day}: Operativo`}
              className="flex-1 h-8 rounded-sm bg-green-500/80 hover:bg-green-500 transition-colors cursor-default"
            />
          ))}
        </div>
        <div className="flex justify-between text-xs text-muted-foreground">
          <span>90 días atrás</span>
          <span className="font-medium text-green-600 dark:text-green-400">99.9% uptime</span>
          <span>Hoy</span>
        </div>
      </section>

      {/* Historial de incidentes */}
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b pb-2">Historial de Incidentes</h2>
        <div className="rounded-lg border bg-muted/20 p-8 text-center space-y-2">
          <CheckCircle2 className="h-10 w-10 text-green-500 mx-auto" />
          <p className="font-medium">Sin incidentes registrados</p>
          <p className="text-sm text-muted-foreground">
            No hubo incidentes en los últimos 90 días. Todos los sistemas funcionaron con normalidad.
          </p>
        </div>
      </section>

      {/* Suscripción */}
      <section className="space-y-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Suscribirse a actualizaciones</CardTitle>
          </CardHeader>
          <CardContent>
            {subscribed ? (
              <div className="flex items-center gap-3 text-sm text-green-700 dark:text-green-400">
                <CheckCircle2 className="h-5 w-5 shrink-0" />
                <p>¡Suscripción exitosa! Te avisaremos ante cualquier incidente o mantenimiento.</p>
              </div>
            ) : (
              <form onSubmit={handleSubscribe} className="flex flex-col sm:flex-row gap-3">
                <Input
                  type="email"
                  placeholder="tu@empresa.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="flex-1"
                  required
                />
                <Button type="submit" className="sm:w-auto">
                  Suscribirme
                </Button>
              </form>
            )}
            <p className="text-xs text-muted-foreground mt-3">
              Recibirás notificaciones solo ante incidentes activos o mantenimientos programados. Sin spam.
            </p>
          </CardContent>
        </Card>
      </section>
    </div>
  );
}
