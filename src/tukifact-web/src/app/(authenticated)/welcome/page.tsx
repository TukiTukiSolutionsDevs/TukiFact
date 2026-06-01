'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { api, type SeriesResponse } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import {
  ArrowRight,
  Building2,
  ListOrdered,
  FileText,
  Shield,
  CheckCircle2,
  Loader2,
} from 'lucide-react';

interface Step {
  id: string;
  title: string;
  description: string;
  icon: React.ElementType;
  href: string;
  check: () => Promise<boolean>;
}

export default function WelcomePage() {
  const { user } = useAuth();
  const router = useRouter();
  const [checks, setChecks] = useState<Record<string, boolean>>({});
  const [isLoading, setIsLoading] = useState(true);

  const steps: Step[] = [
    {
      id: 'company',
      title: 'Registrar empresa',
      description: 'Tu empresa ya está registrada con su RUC.',
      icon: Building2,
      href: '/settings',
      check: async () => !!user?.tenantId,
    },
    {
      id: 'certificate',
      title: 'Configurar certificado digital',
      description: 'Sube tu certificado .pfx para firmar comprobantes ante SUNAT.',
      icon: Shield,
      href: '/certificate',
      check: async () => {
        try {
          const res = await api.get<{ hasCertificate: boolean }>('/v1/certificate/status');
          return res.hasCertificate;
        } catch {
          return false;
        }
      },
    },
    {
      id: 'series',
      title: 'Crear series',
      description: 'Define al menos una serie (ej. F001 para facturas, B001 para boletas).',
      icon: ListOrdered,
      href: '/series',
      check: async () => {
        try {
          const series = await api.get<SeriesResponse[]>('/v1/series');
          return series.length > 0;
        } catch {
          return false;
        }
      },
    },
    {
      id: 'document',
      title: 'Emitir primer comprobante',
      description: 'Emite tu primera factura o boleta electrónica.',
      icon: FileText,
      href: '/documents/new',
      check: async () => {
        try {
          const res = await api.get<{ pagination: { totalCount: number } }>(
            '/v1/documents?pageSize=1'
          );
          return res.pagination.totalCount > 0;
        } catch {
          return false;
        }
      },
    },
  ];

  useEffect(() => {
    const runChecks = async () => {
      const results: Record<string, boolean> = {};
      for (const step of steps) {
        results[step.id] = await step.check();
      }
      setChecks(results);
      setIsLoading(false);
    };
    runChecks();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const completedCount = Object.values(checks).filter(Boolean).length;
  const progress = Math.round((completedCount / steps.length) * 100);
  const allDone = completedCount === steps.length;

  return (
    <div className="max-w-2xl mx-auto">
      {/* Hero */}
      <div className="text-center mb-8">
        <div
          className="mx-auto mb-5 flex h-20 w-20 items-center justify-center rounded-3xl"
          style={{
            background: 'color-mix(in oklch, var(--accent) 16%, transparent)',
          }}
        >
          <CheckCircle2 className="h-10 w-10" style={{ color: 'var(--brand-ink)' }} />
        </div>
        <h1 className="t-display-lg m-0">Bienvenido a TukiFact</h1>
        <p className="t-body mt-2 mb-0" style={{ color: 'var(--muted-foreground)' }}>
          Completa estos pasos para empezar a emitir comprobantes electrónicos en SUNAT.
        </p>
      </div>

      {/* Progress */}
      <div
        className="rounded-[var(--radius-lg)] border bg-card p-5 mb-[var(--gap-cards)]"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        <div className="flex items-center justify-between mb-3">
          <span className="t-body-sm font-semibold">Progreso de configuración</span>
          <span className="t-body-sm mono tnum">
            <strong>{completedCount}</strong>
            <span style={{ color: 'var(--muted-foreground)' }}> / {steps.length} completados</span>
          </span>
        </div>
        <div
          className="w-full rounded-full overflow-hidden"
          style={{ height: 6, background: 'var(--muted)' }}
        >
          <div
            className="h-full rounded-full"
            style={{
              width: `${progress}%`,
              background: 'var(--accent)',
              transition: 'width 400ms var(--ease-out)',
            }}
          />
        </div>
        {isLoading && (
          <p
            className="t-caption mt-3 m-0 inline-flex items-center gap-1.5"
            style={{ color: 'var(--muted-foreground)' }}
          >
            <Loader2 className="h-3 w-3 animate-spin" />
            Verificando…
          </p>
        )}
      </div>

      {/* Steps */}
      <div className="flex flex-col gap-3 mb-[var(--gap-cards)]">
        {steps.map((step) => {
          const isDone = checks[step.id];
          const Icon = step.icon;
          return (
            <div
              key={step.id}
              className="rounded-[var(--radius-lg)] border bg-card p-5 flex items-center gap-4 transition-colors"
              style={{
                boxShadow: 'var(--shadow-xs)',
                borderColor: isDone ? 'var(--success)' : 'var(--border)',
                background: isDone
                  ? 'color-mix(in oklch, var(--success) 4%, var(--card))'
                  : 'var(--card)',
              }}
            >
              <div
                className="h-11 w-11 rounded-xl shrink-0 flex items-center justify-center"
                style={{
                  background: isDone
                    ? 'color-mix(in oklch, var(--success) 16%, transparent)'
                    : 'var(--muted)',
                  color: isDone ? 'var(--success)' : 'var(--muted-foreground)',
                }}
              >
                {isDone ? <CheckCircle2 className="h-5 w-5" /> : <Icon className="h-5 w-5" />}
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="t-body font-semibold m-0">{step.title}</h3>
                <p
                  className="t-body-sm m-0 mt-0.5"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  {step.description}
                </p>
              </div>
              {!isDone && !isLoading && (
                <Button variant="outline" size="sm" asChild>
                  <Link href={step.href}>
                    Ir <ArrowRight className="h-4 w-4 ml-1" />
                  </Link>
                </Button>
              )}
            </div>
          );
        })}
      </div>

      {(allDone || completedCount >= 3) && (
        <div className="text-center">
          <Button
            size="lg"
            onClick={() => router.push('/dashboard')}
            style={{
              background: 'var(--accent)',
              color: 'var(--accent-foreground)',
              fontWeight: 600,
            }}
          >
            Ir al dashboard <ArrowRight className="h-4 w-4 ml-2" />
          </Button>
        </div>
      )}
    </div>
  );
}
